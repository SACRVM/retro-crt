using System.Text;

namespace Retro.Crt.Internals;

/// <summary>
/// Pure string-builders for <see cref="Table"/>. No I/O, no colors —
/// the public <see cref="Table"/> wraps these with ANSI styling.
/// Splitting it out keeps the rendering snapshot-testable.
/// </summary>
internal static class TableRenderer
{
    /// <summary>
    /// Cell padding on each side of every column. One space on each
    /// side reads cleanly without burning horizontal real estate.
    /// </summary>
    public const int CellPadding = 1;

    /// <summary>
    /// Compute the visible width of every column from the header (if
    /// any) plus all data rows. Rows shorter than the header are
    /// treated as if missing trailing cells were empty strings.
    /// </summary>
    public static int[] ComputeWidths(string[]? headers, string[][] rows)
    {
        var cols = ColumnCount(headers, rows);
        if (cols == 0) return [];

        var widths = new int[cols];

        if (headers is not null)
            for (var c = 0; c < headers.Length && c < cols; c++)
            {
                var w = AnsiText.VisibleWidth(headers[c]);
                if (w > widths[c]) widths[c] = w;
            }

        foreach (var row in rows)
            for (var c = 0; c < row.Length && c < cols; c++)
            {
                var w = AnsiText.VisibleWidth(row[c]);
                if (w > widths[c]) widths[c] = w;
            }

        return widths;
    }

    /// <summary>
    /// Render the full table to a single plain-text string (no colors,
    /// no ANSI). Used by tests and by <see cref="Table"/>'s
    /// non-ANSI fallback path.
    /// </summary>
    public static string RenderPlain(
        string[]? headers,
        string[][] rows,
        bool boxBorders)
    {
        var widths = ComputeWidths(headers, rows);
        if (widths.Length == 0) return string.Empty;

        var sb = new StringBuilder();

        if (boxBorders)
        {
            sb.Append(BuildBorder(widths, Glyphs.BoxTopLeft, Glyphs.BoxTeeTop, Glyphs.BoxTopRight, Glyphs.BoxHorizontal));
            sb.Append('\n');
        }

        if (headers is not null && headers.Length > 0)
        {
            sb.Append(BuildRow(headers, widths, boxBorders));
            sb.Append('\n');

            if (boxBorders)
            {
                sb.Append(BuildBorder(widths, Glyphs.BoxTeeLeft, Glyphs.BoxCross, Glyphs.BoxTeeRight, Glyphs.BoxHorizontal));
                sb.Append('\n');
            }
        }

        foreach (var row in rows)
        {
            sb.Append(BuildRow(row, widths, boxBorders));
            sb.Append('\n');
        }

        if (boxBorders)
        {
            sb.Append(BuildBorder(widths, Glyphs.BoxBottomLeft, Glyphs.BoxTeeBottom, Glyphs.BoxBottomRight, Glyphs.BoxHorizontal));
            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Render a single row of cells with the given widths. <paramref name="boxBorders"/>
    /// picks between vertical bars (<c>│</c>/<c>|</c>) and plain spaces.
    /// </summary>
    public static string BuildRow(string[] cells, int[] widths, bool boxBorders)
    {
        var sb = new StringBuilder();
        var pad = new string(' ', CellPadding);

        if (boxBorders) sb.Append(Glyphs.BoxVertical);

        for (var c = 0; c < widths.Length; c++)
        {
            var content = c < cells.Length ? cells[c] : "";
            sb.Append(pad);
            AnsiText.AppendPadded(sb, content, widths[c]);
            sb.Append(pad);
            // Cell padding already creates a visual gap between
            // borderless cells; only the box-bordered variant gets
            // an explicit vertical separator.
            if (boxBorders) sb.Append(Glyphs.BoxVertical);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Build a horizontal border with custom corner / junction glyphs.
    /// Junctions sit between columns; the left corner opens the line
    /// and the right corner closes it.
    /// </summary>
    public static string BuildBorder(int[] widths, char left, char junction, char right, char horizontal)
    {
        var sb = new StringBuilder();
        sb.Append(left);
        for (var c = 0; c < widths.Length; c++)
        {
            sb.Append(horizontal, widths[c] + CellPadding * 2);
            sb.Append(c < widths.Length - 1 ? junction : right);
        }
        return sb.ToString();
    }

    private static int ColumnCount(string[]? headers, string[][] rows)
    {
        var max = headers?.Length ?? 0;
        foreach (var row in rows)
            if (row.Length > max) max = row.Length;
        return max;
    }
}
