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
    /// against accidental terminal-width overflow (no real terminal is
    /// wider than ~150 columns) and string-build blowups.
    /// </summary>
    public const int MaxWidth = 200;

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
    /// after the label so callers don't need to. Single allocation per
    /// call (the returned string itself).
    /// </summary>
    public static string RenderFrame(
        string? label,
        double ratio,
        int width,
        char fullChar,
        char emptyChar,
        bool showPercent)
    {
        if (width <= 0) width = 0;
        if (width > MaxWidth) width = MaxWidth;

        var filled = FilledCells(ratio, width);
        var labelLen = string.IsNullOrEmpty(label) ? 0 : label!.Length;
        var labelSpace = labelLen > 0 ? 1 : 0;
        var pct = showPercent ? Clamp01Percent(ratio) : 0;
        // Percent suffix is " ddd%" — five chars total, with the percent
        // value right-aligned in three.
        const int pctLen = 5;
        var pctTotal = showPercent ? pctLen : 0;
        var totalLen = labelLen + labelSpace + width + pctTotal;

        if (totalLen == 0) return string.Empty;

        return string.Create(totalLen,
            new FrameState(label, labelLen, width, filled, fullChar, emptyChar, showPercent, pct),
            static (span, s) =>
            {
                var pos = 0;

                if (s.LabelLen > 0)
                {
                    s.Label!.AsSpan().CopyTo(span);
                    pos += s.LabelLen;
                    span[pos++] = ' ';
                }

                for (var i = 0; i < s.Width; i++)
                    span[pos + i] = i < s.Filled ? s.FullChar : s.EmptyChar;
                pos += s.Width;

                if (s.ShowPercent)
                {
                    var p = s.Pct;
                    span[pos++] = ' ';
                    if (p < 10)
                    {
                        span[pos++] = ' ';
                        span[pos++] = ' ';
                        span[pos++] = (char)('0' + p);
                    }
                    else if (p < 100)
                    {
                        span[pos++] = ' ';
                        span[pos++] = (char)('0' + p / 10);
                        span[pos++] = (char)('0' + p % 10);
                    }
                    else
                    {
                        span[pos++] = (char)('0' + p / 100);
                        span[pos++] = (char)('0' + (p / 10) % 10);
                        span[pos++] = (char)('0' + p % 10);
                    }
                    span[pos++] = '%';
                }
            });
    }

    private static int Clamp01Percent(double ratio)
    {
        if (ratio <= 0) return 0;
        if (ratio >= 1) return 100;
        var p = (int)(ratio * 100);
        return p < 0 ? 0 : p > 100 ? 100 : p;
    }

    private readonly record struct FrameState(
        string? Label,
        int LabelLen,
        int Width,
        int Filled,
        char FullChar,
        char EmptyChar,
        bool ShowPercent,
        int Pct);
}
