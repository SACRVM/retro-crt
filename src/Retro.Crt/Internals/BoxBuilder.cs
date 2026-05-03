namespace Retro.Crt.Internals;

/// <summary>
/// Pure builder for a Pascal-style framed box around one or more text lines.
/// No I/O. The returned array always has at least three entries: top edge,
/// content lines, bottom edge.
/// </summary>
internal static class BoxBuilder
{
    public static string[] Build(
        ReadOnlySpan<string> lines,
        int padding = 1,
        char tl = '+', char tr = '+', char bl = '+', char br = '+',
        char horizontal = '-', char vertical = '|',
        int minContentWidth = 0,
        BoxAlign align = BoxAlign.Left)
    {
        if (lines.Length == 0)
        {
            string[] empty = [""];
            lines = empty;
        }
        if (padding < 0) padding = 0;
        if (minContentWidth < 0) minContentWidth = 0;

        var width = minContentWidth;
        for (var i = 0; i < lines.Length; i++)
        {
            var len = lines[i].Length;
            if (len > width) width = len;
        }
        var inner = width + 2 * padding;

        var result = new string[lines.Length + 2];
        result[0] = tl + new string(horizontal, inner) + tr;

        var pad = new string(' ', padding);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var slack = width - line.Length;
            var (left, right) = SplitSlack(slack, align);
            var leftFill  = left  == 0 ? "" : new string(' ', left);
            var rightFill = right == 0 ? "" : new string(' ', right);
            result[i + 1] = vertical + pad + leftFill + line + rightFill + pad + vertical;
        }

        result[^1] = bl + new string(horizontal, inner) + br;
        return result;
    }

    /// <summary>
    /// Distribute leftover horizontal space between the two sides of a
    /// content line. <see cref="BoxAlign.Center"/> sends odd leftovers
    /// to the right so multi-line banners stay visually anchored.
    /// </summary>
    private static (int Left, int Right) SplitSlack(int slack, BoxAlign align) => align switch
    {
        BoxAlign.Right  => (slack, 0),
        BoxAlign.Center => (slack / 2, slack - slack / 2),
        _               => (0, slack),
    };
}
