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
    /// on Windows.
    /// </summary>
    public static bool ColorEnabled => TerminalCapabilities.SupportsAnsi;

    public static void TextColor(Color color)
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.Foreground(color));
    }

    public static void TextBackground(Color color)
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.Background(color));
    }

    public static void ResetColor()
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.Reset);
    }

    public static void Write(string s) => Console.Out.Write(s);

    public static void WriteLine() => Console.Out.WriteLine();

    public static void WriteLine(string s) => Console.Out.WriteLine(s);

    /// <summary>
    /// Apply optional foreground/background/bold for the duration of the
    /// returned scope. Disposing restores the previous styling via a single
    /// <c>RESET</c>.
    /// </summary>
    public static IDisposable WithStyle(Color? fg = null, Color? bg = null, bool bold = false)
    {
        if (!ColorEnabled) return NullScope.Instance;

        if (fg is { } f) Console.Out.Write(AnsiCodes.Foreground(f));
        if (bg is { } b) Console.Out.Write(AnsiCodes.Background(b));
        if (bold)        Console.Out.Write(AnsiCodes.Bold);

        return new StyleScope();
    }

    // ─── Pascal CRT classics ─────────────────────────────────────────────

    /// <summary>1-based cursor position, like the original CRT unit.</summary>
    public static void GotoXY(int column, int row)
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.GotoXY(column, row));
    }

    public static void ClrScr()
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.ClearScreen);
    }

    public static void ClrEol()
    {
        if (ColorEnabled) Console.Out.Write(AnsiCodes.ClearToEol);
    }

    private sealed class StyleScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Console.Out.Write(AnsiCodes.Reset);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
