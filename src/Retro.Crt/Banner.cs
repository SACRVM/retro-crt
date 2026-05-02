using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Pascal-style banners. Two flavours: a framed <see cref="Box"/> for titles,
/// and a per-line <see cref="Gradient"/> for fancier startup screens.
/// </summary>
public static class Banner
{
    /// <summary>
    /// Print <paramref name="text"/> wrapped in a single-line frame. Uses
    /// box-drawing glyphs when the terminal can render them, falls back to
    /// <c>+--+</c> otherwise.
    /// </summary>
    public static void Box(string text, Color? fg = null, int padding = 1)
        => Box([text], fg, padding);

    /// <summary>
    /// Print one frame around all <paramref name="lines"/>. The box is sized
    /// to the longest line.
    /// </summary>
    public static void Box(string[] lines, Color? fg = null, int padding = 1)
    {
        var framed = BoxBuilder.Build(
            lines,
            padding,
            Glyphs.BoxTopLeft, Glyphs.BoxTopRight,
            Glyphs.BoxBottomLeft, Glyphs.BoxBottomRight,
            Glyphs.BoxHorizontal, Glyphs.BoxVertical);

        if (fg is { } color)
        {
            using (Crt.WithStyle(fg: color))
                for (var i = 0; i < framed.Length; i++)
                    Crt.WriteLine(framed[i]);
        }
        else
        {
            for (var i = 0; i < framed.Length; i++)
                Crt.WriteLine(framed[i]);
        }
    }

    /// <summary>
    /// Print <paramref name="lines"/> with a per-line color interpolated from
    /// <paramref name="from"/> to <paramref name="to"/>. Both colors must be
    /// truecolor; if either is a Standard16 entry the gradient collapses to
    /// <paramref name="from"/> for every line.
    /// </summary>
    public static void Gradient(string[] lines, Color from, Color to, bool bold = true)
    {
        if (lines.Length == 0) return;

        var canInterpolate =
            from.Mode == ColorMode.Truecolor && to.Mode == ColorMode.Truecolor;

        for (var i = 0; i < lines.Length; i++)
        {
            var color = canInterpolate ? Interpolate(from, to, i, lines.Length) : from;
            using (Crt.WithStyle(fg: color, bold: bold))
                Crt.WriteLine(lines[i]);
        }
    }

    internal static Color Interpolate(Color from, Color to, int index, int count)
    {
        var t = count <= 1 ? 0.0 : (double)index / (count - 1);
        return Color.Rgb(
            Lerp(from.R, to.R, t),
            Lerp(from.G, to.G, t),
            Lerp(from.B, to.B, t));
    }

    private static byte Lerp(byte a, byte b, double t)
    {
        var v = a + (b - a) * t;
        return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
    }
}
