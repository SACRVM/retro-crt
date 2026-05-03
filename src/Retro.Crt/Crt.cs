using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Pascal CRT-Unit-style console. Static, stateless from the caller's view —
/// just the verbs, plus a small <see cref="WithStyle"/> scope for transient
/// styling.
/// </summary>
public static class Crt
{
    /// <summary>
    /// True when ANSI escapes will actually reach the terminal. False when
    /// output is redirected, <c>NO_COLOR</c> is set, or VT enablement failed
    /// on Windows. Equivalent to <c>Depth != ColorDepth.None</c>.
    /// </summary>
    public static bool ColorEnabled => TerminalCapabilities.SupportsAnsi;

    /// <summary>
    /// What this terminal can render. <see cref="ColorDepth.Truecolor"/>
    /// on modern terminals (Windows Terminal, iTerm2, kitty, …);
    /// <see cref="ColorDepth.Xterm256"/> on most legacy 256-color
    /// terminals; <see cref="ColorDepth.Standard16"/> on basic VTs;
    /// <see cref="ColorDepth.None"/> when output is redirected, dumb,
    /// or <c>NO_COLOR</c> is set. Truecolor and 256-color values are
    /// quantized down to whatever this depth supports before emission.
    /// </summary>
    public static ColorDepth Depth => TerminalCapabilities.CurrentDepth;

    /// <summary>
    /// True when stdout is a real, escape-processing terminal —
    /// independent of <see cref="ColorEnabled"/>. Use this to gate
    /// animation (in-place redraws via CR, hide/show cursor, anchor
    /// indents): a terminal with <c>NO_COLOR=1</c> is still
    /// interactive, just without colors. False for redirected output,
    /// <c>TERM=dumb</c>, and Windows hosts where VT enablement failed.
    /// </summary>
    public static bool IsInteractive => TerminalCapabilities.IsInteractive;

    /// <summary>
    /// Visible terminal width in cells, with a sensible default when the
    /// host can't report it (output redirected, no console attached,
    /// daemon, …). Defaults to <see cref="DefaultWidth"/>; never throws.
    /// </summary>
    public static int WindowWidth
    {
        get
        {
            try
            {
                var w = Console.WindowWidth;
                return w > 0 ? w : DefaultWidth;
            }
            catch { return DefaultWidth; }
        }
    }

    /// <summary>
    /// Visible terminal height in cells, with a sensible default when
    /// the host can't report it. Defaults to <see cref="DefaultHeight"/>;
    /// never throws.
    /// </summary>
    public static int WindowHeight
    {
        get
        {
            try
            {
                var h = Console.WindowHeight;
                return h > 0 ? h : DefaultHeight;
            }
            catch { return DefaultHeight; }
        }
    }

    /// <summary>
    /// Current cursor column (0-based), or 0 when the host can't report
    /// it (output redirected, no console attached). Use this if you want
    /// to anchor your own multi-line layout to wherever the cursor sits
    /// after a <see cref="GotoXY"/>. Never throws.
    /// </summary>
    public static int CursorLeft => CursorState.GetLeft();

    /// <summary>Width fallback used when the terminal is not measurable.</summary>
    public const int DefaultWidth = 80;

    /// <summary>Height fallback used when the terminal is not measurable.</summary>
    public const int DefaultHeight = 24;

    /// <summary>
    /// Sentinel for the <c>width</c> parameter on widgets that support
    /// "fill the terminal". Banner and ProgressBar treat this as
    /// <see cref="WindowWidth"/> at render time.
    /// </summary>
    public const int FillWidth = -1;

    private static TextWriter? _sinkOverride;

    /// <summary>
    /// The <see cref="TextWriter"/> Retro.Crt currently writes to.
    /// Defaults to <see cref="Console.Out"/> (so <c>Console.SetOut</c>
    /// transparently re-targets us); a <see cref="WithSink"/> scope
    /// overrides it for the scope's lifetime so all widgets — Crt,
    /// Banner, ProgressBar, Spinner, Prompt, Table, Typewriter — emit
    /// to the same destination.
    /// </summary>
    public static TextWriter Sink => _sinkOverride ?? Console.Out;

    /// <summary>
    /// Route all Retro.Crt output to <paramref name="sink"/> for the
    /// duration of the returned scope. Also routes <see cref="Log"/>
    /// (both <see cref="Log.OutSink"/> and <see cref="Log.ErrSink"/>)
    /// so a single scope captures everything. Restores the previous
    /// overrides on dispose; nests cleanly.
    /// </summary>
    public static IDisposable WithSink(TextWriter sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        var previousCrt   = _sinkOverride;
        var previousLogOut = Log.OutSink;
        var previousLogErr = Log.ErrSink;
        _sinkOverride = sink;
        Log.OutSink   = sink;
        Log.ErrSink   = sink;
        return new SinkScope(previousCrt, previousLogOut, previousLogErr);
    }

    private static Theme? _currentTheme;

    /// <summary>
    /// The active theme, or <c>null</c> when none is in effect. Set via
    /// <see cref="UseTheme"/> for the duration of the returned scope.
    /// Widgets that accept an optional color (Banner, Log, ProgressBar,
    /// Spinner, Table, Prompt, Typewriter) fall back to a sensible slot
    /// from this theme when the caller doesn't supply one, so themed
    /// output stays consistent without threading colors through every
    /// call site.
    /// </summary>
    public static Theme? CurrentTheme => _currentTheme;

    /// <summary>
    /// Apply <paramref name="theme"/> as the implicit color source for
    /// the duration of the returned scope. Emits the theme's foreground
    /// and background as SGR so subsequent plain
    /// <c>Crt.Write</c>/<c>WriteLine</c> calls render in those colors;
    /// widgets with a <c>color</c>/<c>fg</c> parameter fall back to the
    /// matching theme slot when their argument is <c>null</c>.
    /// Disposing the scope emits <c>RESET</c> and restores the previous
    /// theme, if any. Nests cleanly.
    /// </summary>
    public static IDisposable UseTheme(Theme theme)
    {
        var previous = _currentTheme;
        _currentTheme = theme;

        if (ColorEnabled)
        {
            Sink.Write(Emit.Fg(theme.Foreground));
            Sink.Write(Emit.Bg(theme.Background));
        }

        return new ThemeScope(previous);
    }

    public static void TextColor(Color color)
    {
        if (ColorEnabled) Sink.Write(Emit.Fg(color));
    }

    public static void TextBackground(Color color)
    {
        if (ColorEnabled) Sink.Write(Emit.Bg(color));
    }

    public static void ResetColor()
    {
        if (ColorEnabled) Sink.Write(AnsiCodes.Reset);
    }

    public static void Write(string s) => Sink.Write(s);

    public static void WriteLine() => Sink.WriteLine();

    public static void WriteLine(string s) => Sink.WriteLine(s);

    /// <summary>
    /// Apply optional foreground/background/bold for the duration of the
    /// returned scope. Disposing restores the previous styling via a single
    /// <c>RESET</c>.
    /// </summary>
    public static IDisposable WithStyle(Color? fg = null, Color? bg = null, bool bold = false)
    {
        if (!ColorEnabled) return NullScope.Instance;

        if (fg is { } f) Sink.Write(Emit.Fg(f));
        if (bg is { } b) Sink.Write(Emit.Bg(b));
        if (bold)        Sink.Write(AnsiCodes.Bold);

        return new StyleScope();
    }

    // ─── Pascal CRT classics ─────────────────────────────────────────────

    /// <summary>1-based cursor position, like the original CRT unit.</summary>
    public static void GotoXY(int column, int row)
    {
        if (ColorEnabled) Sink.Write(AnsiCodes.GotoXY(column, row));
    }

    public static void ClrScr()
    {
        if (ColorEnabled) Sink.Write(AnsiCodes.ClearScreen);
    }

    public static void ClrEol()
    {
        if (ColorEnabled) Sink.Write(AnsiCodes.ClearToEol);
    }

    private sealed class StyleScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Sink.Write(AnsiCodes.Reset);

            // Reset clears every SGR including the active theme's
            // foreground / background. Re-apply them so subsequent
            // plain Crt.Write calls keep rendering in the theme's
            // colors — the README promises "Crt.Write inside a theme
            // renders in theme.Foreground on theme.Background", and
            // a Banner / WithStyle nest must not break that promise.
            if (_currentTheme is { } t)
            {
                Sink.Write(Emit.Fg(t.Foreground));
                Sink.Write(Emit.Bg(t.Background));
            }
        }
    }

    private sealed class ThemeScope : IDisposable
    {
        private readonly Theme? _previous;
        private bool _disposed;

        public ThemeScope(Theme? previous) { _previous = previous; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _currentTheme = _previous;

            if (!ColorEnabled) return;

            // Reset first to clear our SGR state, then re-apply the
            // outer theme's colors if there is one. That way nested
            // themes restore cleanly without leaking SGR state.
            Sink.Write(AnsiCodes.Reset);
            if (_previous is { } outer)
            {
                Sink.Write(Emit.Fg(outer.Foreground));
                Sink.Write(Emit.Bg(outer.Background));
            }
        }
    }

    private sealed class SinkScope : IDisposable
    {
        private readonly TextWriter? _previousCrt;
        private readonly TextWriter? _previousLogOut;
        private readonly TextWriter? _previousLogErr;
        private bool _disposed;

        public SinkScope(TextWriter? previousCrt, TextWriter? previousLogOut, TextWriter? previousLogErr)
        {
            _previousCrt = previousCrt;
            _previousLogOut = previousLogOut;
            _previousLogErr = previousLogErr;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _sinkOverride = _previousCrt;
            Log.OutSink   = _previousLogOut;
            Log.ErrSink   = _previousLogErr;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
