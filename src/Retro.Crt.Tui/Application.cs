using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui;

/// <summary>
/// Tiny event loop on top of the core's <c>ScreenBuffer</c> +
/// <c>ScreenRenderer</c> + <see cref="TerminalInput"/>: enters the alt
/// screen, raw mode, and mouse tracking; reads events and dispatches
/// them to a single root <see cref="View"/>; redraws via diff whenever
/// the root marks itself dirty. Exits when <see cref="Exit"/> is
/// called.
/// </summary>
/// <remarks>
/// <para>
/// v1 does not handle terminal resize — the screen size is sampled
/// once at <see cref="Run"/> and the root's <see cref="View.Bounds"/>
/// stays fixed for the lifetime of the loop. SIGWINCH support is
/// deferred (see Stage-2 decisions).
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
        _root.MarkDirty();

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
            if (_root.IsDirty)
            {
                current.Clear();
                _root.OnDraw(current);
                _root.ClearDirty();

                ScreenRenderer.Render(previous, current, Crt.Sink);
                Crt.Sink.Flush();

                // Ping-pong the two buffers so the diff renderer always
                // sees the actually-displayed state as `previous`. The
                // new `current` will be cleared and redrawn next frame.
                previous = current;
                current = ReferenceEquals(current, bufA) ? bufB : bufA;
            }

            InputEvent ev;
            try { ev = TerminalInput.ReadEvent(); }
            catch (EndOfStreamException) { break; }

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
        var hit = _root.HitTest(x, y) ?? _root;

        // A click also moves focus, so widgets feel "live" without
        // every Panel having to handle press itself. Motion / wheel
        // events leave focus alone.
        if (mouse.Kind == MouseEventKind.Press && hit.IsFocusable)
            SetFocus(hit);

        hit.OnMouse(mouse, this);
    }

    private View? FirstFocusable()
    {
        foreach (var f in _root.EnumerateFocusable()) return f;
        return null;
    }
}
