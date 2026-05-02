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
        char horizontal = '-', char vertical = '|')
    {
        if (lines.Length == 0)
        {
            string[] empty = [""];
            lines = empty;
        }
        if (padding < 0) padding = 0;

        var width = 0;
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
            var trailing = new string(' ', width - line.Length);
            result[i + 1] = vertical + pad + line + trailing + pad + vertical;
        }

        result[^1] = bl + new string(horizontal, inner) + br;
        return result;
    }
}
