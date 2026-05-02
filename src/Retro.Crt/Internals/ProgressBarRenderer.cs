using System.Globalization;

namespace Retro.Crt.Internals;

/// <summary>
/// Pure renderer for a single progress-bar frame. No I/O, no allocations
/// beyond the returned string. The caller owns positioning (carriage return,
/// cursor moves, color escapes).
/// </summary>
internal static class ProgressBarRenderer
{
    /// <summary>
    /// Maximum permitted bar width. Anything larger is clamped — guards
    /// against accidental terminal-width overflow and string-build blowups.
    /// </summary>
    public const int MaxWidth = 1024;

    /// <summary>
    /// How many of <paramref name="width"/> cells should be filled for a
    /// given progress ratio. Always 0..width inclusive.
    /// </summary>
    public static int FilledCells(double ratio, int width)
    {
        if (width <= 0) return 0;
        if (ratio <= 0) return 0;
        if (ratio >= 1) return width;
        var cells = (int)(ratio * width);
        return cells < 0 ? 0 : cells > width ? width : cells;
    }

    /// <summary>
    /// Renders <c>####------</c>. Use <see cref="RenderFrame"/> for the
    /// full label + percent line.
    /// </summary>
    public static string RenderBar(int filled, int width, char fullChar, char emptyChar)
    {
        if (width <= 0) return "";
        if (width > MaxWidth) width = MaxWidth;
        if (filled < 0) filled = 0;
        if (filled > width) filled = width;

        return string.Create(width, (filled, width, fullChar, emptyChar), static (span, state) =>
        {
            for (var i = 0; i < state.width; i++)
                span[i] = i < state.filled ? state.fullChar : state.emptyChar;
        });
    }

    /// <summary>
    /// Optional label, then the bar, then optional percent. Leading space
    /// after the label so callers don't need to.
    /// </summary>
    public static string RenderFrame(
        string? label,
        double ratio,
        int width,
        char fullChar,
        char emptyChar,
        bool showPercent)
    {
        var filled = FilledCells(ratio, width);
        var bar = RenderBar(filled, width, fullChar, emptyChar);

        if (string.IsNullOrEmpty(label) && !showPercent) return bar;

        var pct = showPercent
            ? " " + ((int)(ratio * 100)).ToString(CultureInfo.InvariantCulture).PadLeft(3) + "%"
            : "";
        var lbl = string.IsNullOrEmpty(label) ? "" : label + " ";
        return lbl + bar + pct;
    }
}
