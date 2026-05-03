using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Pascal-style banners. Two flavours: a framed <c>Box</c> for titles,
/// and a per-line <see cref="Gradient"/> for fancier startup screens.
/// </summary>
public static class Banner
{
    /// <summary>
    /// Print <paramref name="text"/> wrapped in a single-line frame. Uses
    /// box-drawing glyphs when the terminal can render them, falls back to
    /// <c>+--+</c> otherwise.
    /// </summary>
    public static void Box(string text, Color? fg = null, int padding = 1, int width = 0, BoxAlign align = BoxAlign.Left)
        => Box([text], fg, padding, width, align);

    /// <summary>
    /// Print one frame around all <paramref name="lines"/>. The box is
    /// sized to the longest line, or to <paramref name="width"/> total
    /// cells if larger. Pass <see cref="Crt.FillWidth"/> to span the
    /// current terminal width. <paramref name="align"/> picks how lines
    /// shorter than the inner content width are positioned.
    /// </summary>
    public static void Box(string[] lines, Color? fg = null, int padding = 1, int width = 0, BoxAlign align = BoxAlign.Left)
    {
        var minContent = ResolveContentWidth(width, padding);

        var framed = BoxBuilder.Build(
            lines,
            padding,
            Glyphs.BoxTopLeft, Glyphs.BoxTopRight,
            Glyphs.BoxBottomLeft, Glyphs.BoxBottomRight,
            Glyphs.BoxHorizontal, Glyphs.BoxVertical,
            minContentWidth: minContent,
            align: align);

        // Anchor the frame at whatever column the cursor sits in. The
        // first line writes inline (caller-positioned); lines 2..n need
        // explicit leading spaces so the left edge of the frame stays
        // vertical. CursorLeft returns 0 in pipes/tests, so the default
        // path is unchanged.
        var anchor = CursorState.GetLeft();
        var indent = anchor > 0 ? new string(' ', anchor) : null;

        // No explicit fg → fall back to the active theme's accent slot
        // so themed output stays consistent without threading colors
        // through every call site.
        var resolved = fg ?? Crt.CurrentTheme?.Accent;

        if (resolved is { } color)
        {
            using (Crt.WithStyle(fg: color))
                WriteFrame(framed, indent);
        }
        else
        {
            WriteFrame(framed, indent);
        }
    }

    private static void WriteFrame(string[] framed, string? indent)
    {
        Crt.WriteLine(framed[0]);
        for (var i = 1; i < framed.Length; i++)
        {
            if (indent is not null) Crt.Write(indent);
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

    /// <summary>
    /// Convert a desired total visible width into a min content width
    /// for <see cref="BoxBuilder"/> (subtracts the two corner glyphs and
    /// the left/right padding). Returns 0 — no override — when the
    /// caller passed 0.
    /// </summary>
    private static int ResolveContentWidth(int desired, int padding)
    {
        if (desired == 0) return 0;
        var total = desired == Crt.FillWidth ? Crt.WindowWidth : desired;
        // 2 corner glyphs + 2*padding live outside the inner content.
        var content = total - 2 - 2 * padding;
        return content < 0 ? 0 : content;
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
