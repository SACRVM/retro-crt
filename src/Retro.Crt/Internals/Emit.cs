namespace Retro.Crt.Internals;

/// <summary>
/// Depth-aware ANSI emit helpers. Quantizes a <see cref="Color"/> to
/// what the current terminal can actually render, then encodes it. All
/// public Crt entry points should go through here so a truecolor value
/// never reaches a 16-color terminal raw.
/// </summary>
internal static class Emit
{
    public static string Fg(Color c)
    {
        var depth = TerminalCapabilities.CurrentDepth;
        if (depth == ColorDepth.None) return "";
        return AnsiCodes.Foreground(c.For(depth));
    }

    public static string Bg(Color c)
    {
        var depth = TerminalCapabilities.CurrentDepth;
        if (depth == ColorDepth.None) return "";
        return AnsiCodes.Background(c.For(depth));
    }
}
