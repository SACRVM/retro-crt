using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// A thin full-width horizontal rule with an optional centered title —
/// the lightweight "section header" companion to <see cref="Banner"/>.
/// Where <c>Box</c> draws a full frame, <c>Rule</c> draws a single line
/// (<c>── THE FISHBOWL ──────</c>) for separating regions of output.
/// </summary>
public static class Rule
{
    /// <summary>
    /// Print a horizontal rule to <see cref="Console.Out"/>. With a
    /// <paramref name="title"/> the label is centered in the line; without
    /// one it's an unbroken divider. Uses the box-drawing glyph when the
    /// terminal can render it, falls back to <c>-</c> otherwise.
    /// </summary>
    /// <param name="title">Optional centered label. <c>null</c> / empty for a plain divider.</param>
    /// <param name="color">Optional foreground. Falls back to the active theme's <c>Muted</c> slot.</param>
    /// <param name="width">Total width in cells. Defaults to <see cref="Crt.FillWidth"/> (the current terminal width).</param>
    public static void Print(string? title = null, Color? color = null, int width = Crt.FillWidth)
    {
        var resolvedWidth = width == Crt.FillWidth ? Crt.WindowWidth : width;
        var line = RuleRenderer.Build(title, resolvedWidth, Glyphs.BoxHorizontal);

        var resolved = color ?? Crt.CurrentTheme?.Muted;

        if (Crt.ColorEnabled && resolved is { } c)
        {
            using (Crt.WithStyle(fg: c))
                Crt.Sink.Write(line);
            Crt.Sink.WriteLine();
        }
        else
        {
            Crt.Sink.WriteLine(line);
        }
    }
}
