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
    private bool _running;

    public Application(View root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    /// <summary>The root view this application drives.</summary>
    public View Root => _root;

    /// <summary>Request the loop to terminate after the current event is handled.</summary>
    public void Exit() => _running = false;

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
                case InputEventKind.Key:   _root.OnKey(ev.Key, this);     break;
                case InputEventKind.Mouse: _root.OnMouse(ev.Mouse, this); break;
            }
        }
    }
}
