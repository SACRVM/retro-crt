using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui;

/// <summary>
/// Tiny event loop on top of the core's <c>ScreenBuffer</c> +
/// <c>ScreenRenderer</c> + <see cref="TerminalInput"/>: enters the alt
/// screen, raw mode, and mouse tracking; reads events and dispatches
/// them to a single root <see cref="View"/>; redraws via diff whenever
/// any view in the tree marks itself dirty. Exits when
/// <see cref="Exit"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// Resize: the loop re-samples the terminal size at the top of every
/// iteration and reallocates buffers when the size changes. Because
/// <see cref="TerminalInput.ReadEvent"/> blocks, a resize that happens
/// while no input is arriving only takes effect on the next event;
/// nudging the terminal usually delivers one. SIGWINCH integration is
/// still on the roadmap.
/// </para>
/// <para>
/// One application per process at a time; nesting is not supported
/// because terminal state (alt screen, raw mode) is global.
/// </para>
/// </remarks>
public sealed class Application
{
    private readonly View _root;
    private View? _focus;
    private View? _mouseCapture;
    private bool _running;

    public Application(View root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    /// <summary>The root view this application drives.</summary>
    public View Root => _root;

    /// <summary>The view currently owning keyboard focus, or <c>null</c>.</summary>
    public View? Focus => _focus;

    /// <summary>Request the loop to terminate after the current event is handled.</summary>
    public void Exit() => _running = false;

    /// <summary>
    /// Move focus to the next focusable view in tree order, wrapping
    /// at the end. No-op when the tree contains no focusable views.
    /// </summary>
    public void FocusNext() => MoveFocus(forward: true);

    /// <summary>Move focus to the previous focusable view, wrapping at the start.</summary>
    public void FocusPrevious() => MoveFocus(forward: false);

    private void MoveFocus(bool forward)
    {
        var list = new List<View>();
        foreach (var f in _root.EnumerateFocusable()) list.Add(f);
        if (list.Count == 0) { SetFocus(null); return; }

        int idx;
        if (_focus is null)
        {
            idx = forward ? 0 : list.Count - 1;
        }
        else
        {
            var current = list.IndexOf(_focus);
            idx = forward
                ? (current + 1)             % list.Count
                : (current - 1 + list.Count) % list.Count;
        }
        SetFocus(list[idx]);
    }

    private void SetFocus(View? next)
    {
        if (ReferenceEquals(next, _focus)) return;
        _focus?.SetFocus(false);
        _focus = next;
        _focus?.SetFocus(true);
    }

    /// <summary>
    /// Block until <see cref="Exit"/> is called or stdin closes. Sets
    /// up alt-screen / raw / mouse scopes that are torn down on return,
    /// even on exception, so the user's shell is restored cleanly.
    /// </summary>
    public void Run()
    {
        var width  = Crt.WindowWidth;
        var height = Crt.WindowHeight;
        if (width < 1 || height < 1)
            throw new InvalidOperationException(
                "Cannot run an Application without a measurable terminal size; is stdout redirected?");

        _root.Bounds = new Rect(0, 0, width, height);
        MarkDirtyAll(_root);

        // Pick the first focusable view as the initial focus so Tab
        // has something to cycle from on the very first key press.
        SetFocus(FirstFocusable());

        using var alt   = Crt.UseAlternateScreen();
        using var raw   = RawMode.Enter();
        using var mouse = Crt.UseMouse();

        var bufA = new ScreenBuffer(width, height);
        var bufB = new ScreenBuffer(width, height);
        var current = bufA;
        ScreenBuffer? previous = null;

        _running = true;
        while (_running)
        {
            // Resize check — sample current terminal size; if it
            // changed, reallocate the cell buffers and force a full
            // repaint by nulling `previous`.
            var nowW = Crt.WindowWidth;
            var nowH = Crt.WindowHeight;
            if (nowW > 0 && nowH > 0 && (nowW != width || nowH != height))
            {
                width  = nowW;
                height = nowH;
                bufA = new ScreenBuffer(width, height);
                bufB = new ScreenBuffer(width, height);
                current = bufA;
                previous = null;
                _root.Bounds = new Rect(0, 0, width, height);
                MarkDirtyAll(_root);
            }

            if (AnyDirty(_root))
            {
                current.Clear();
                _root.OnDraw(current);
                ClearDirtyAll(_root);

                ScreenRenderer.Render(previous, current, Crt.Sink);
                Crt.Sink.Flush();

                previous = current;
                current = ReferenceEquals(current, bufA) ? bufB : bufA;
            }

            // Poll with a short timeout instead of blocking forever:
            // lets the loop come back around so the resize check at
            // the top runs even when the user isn't pressing keys.
            // 50 ms ≈ 20 Hz — quick enough to feel live during a
            // window resize, gentle enough on idle CPU.
            if (!TerminalInput.WaitForEvent(50, out var ev))
                continue;

            switch (ev.Kind)
            {
                case InputEventKind.Key:   DispatchKey(ev.Key);     break;
                case InputEventKind.Mouse: DispatchMouse(ev.Mouse); break;
            }
        }
    }

    private void DispatchKey(KeyEvent key)
    {
        // Tab and Shift+Tab are application-level — they always run
        // before any view sees the key, so a focused widget can't
        // accidentally swallow navigation.
        if (key.Key == Key.Tab)
        {
            if ((key.Modifiers & KeyModifiers.Shift) != 0) FocusPrevious();
            else                                          FocusNext();
            return;
        }

        // Focused view gets first crack at the key; the root view
        // always sees it second, acting as a bubble-up handler for
        // app-level shortcuts like quit. Skipped when focus IS the
        // root, to avoid double-delivery.
        if (_focus is { } f && !ReferenceEquals(f, _root))
            f.OnKey(key, this);

        _root.OnKey(key, this);
    }

    private void DispatchMouse(MouseEvent mouse)
    {
        // Mouse coordinates from InputParser are 1-based; our Rect
        // model is 0-based.
        var x = mouse.X - 1;
        var y = mouse.Y - 1;

        // Wheel events go to the view directly under the cursor —
        // matches browser / desktop convention ("scroll the thing
        // I'm hovering") and avoids the surprise where wheel did
        // nothing because the focus was elsewhere.
        if (mouse.Kind == MouseEventKind.Wheel)
        {
            var hovered = _root.HitTest(x, y) ?? _root;
            hovered.OnMouse(mouse, this);
            return;
        }

        // While the mouse is captured (between a Press and its
        // Release), Drag and Release are routed back to the press
        // target so a scrollbar drag still scrolls when the cursor
        // strays off the track.
        View target;
        if (_mouseCapture is { } cap &&
            (mouse.Kind == MouseEventKind.Drag || mouse.Kind == MouseEventKind.Release))
            target = cap;
        else
            target = _root.HitTest(x, y) ?? _root;

        if (mouse.Kind == MouseEventKind.Press)
        {
            if (target.IsFocusable) SetFocus(target);
            _mouseCapture = target;
        }
        else if (mouse.Kind == MouseEventKind.Release)
        {
            _mouseCapture = null;
        }

        target.OnMouse(mouse, this);
    }

    private View? FirstFocusable()
    {
        foreach (var f in _root.EnumerateFocusable()) return f;
        return null;
    }

    private static bool AnyDirty(View v)
    {
        if (v.IsDirty) return true;
        if (v is Container c)
        {
            for (var i = 0; i < c.Children.Count; i++)
                if (AnyDirty(c.Children[i])) return true;
        }
        return false;
    }

    private static void ClearDirtyAll(View v)
    {
        v.ClearDirty();
        if (v is Container c)
        {
            for (var i = 0; i < c.Children.Count; i++)
                ClearDirtyAll(c.Children[i]);
        }
    }

    private static void MarkDirtyAll(View v)
    {
        v.MarkDirty();
        if (v is Container c)
        {
            for (var i = 0; i < c.Children.Count; i++)
                MarkDirtyAll(c.Children[i]);
        }
    }
}
