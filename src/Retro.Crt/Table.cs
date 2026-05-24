using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Tiny aligned-column table renderer. Box-drawing borders by default,
/// header in bold, optional foreground colors for header, borders, and
/// individual body cells. One column auto-resizes to its widest cell —
/// no manual width configuration. Single-line cells only.
/// </summary>
/// <remarks>
/// Deliberately small surface: no row borders between body rows, no
/// alignment options (always left), no multi-line cells, no col-spans,
/// no live updates. If you need any of that, reach for
/// <c>Spectre.Console</c>.
/// </remarks>
public static class Table
{
    /// <summary>
    /// Render a table to <see cref="Console.Out"/>.
    /// </summary>
    /// <param name="headers">Header labels. <c>null</c> or empty for a header-less table.</param>
    /// <param name="rows">Data rows. Rows shorter than the header are right-padded with empty cells.</param>
    /// <param name="border">Border style. Defaults to full <see cref="TableBorder.Box"/>.</param>
    /// <param name="headerColor">Optional foreground for the header row. Header is also rendered bold.</param>
    /// <param name="borderColor">Optional foreground for the border glyphs (only meaningful with <see cref="TableBorder.Box"/>).</param>
    public static void Print(
        string[]? headers,
        string[][] rows,
        TableBorder border = TableBorder.Box,
        Color? headerColor = null,
        Color? borderColor = null)
        => Print(headers, rows, cellColors: null, border, headerColor, borderColor);

    /// <summary>
    /// Render a table whose body cells carry optional per-cell foreground
    /// colors — the canonical use is a status column (green <c>Running</c>,
    /// red <c>No listener</c>). <paramref name="cellColors"/> is a jagged
    /// array aligned to <paramref name="rows"/>: <c>cellColors[r][c]</c> is
    /// the foreground for cell <c>rows[r][c]</c>, or <c>null</c> to leave it
    /// uncolored. Missing / short rows and columns default to uncolored, so
    /// you only fill the entries you actually want to tint.
    /// </summary>
    /// <param name="headers">Header labels. <c>null</c> or empty for a header-less table.</param>
    /// <param name="rows">Data rows. Rows shorter than the header are right-padded with empty cells.</param>
    /// <param name="cellColors">Per-cell foreground colors aligned to <paramref name="rows"/>; <c>null</c> entries (or a <c>null</c> array) mean uncolored.</param>
    /// <param name="border">Border style. Defaults to full <see cref="TableBorder.Box"/>.</param>
    /// <param name="headerColor">Optional foreground for the header row. Header is also rendered bold.</param>
    /// <param name="borderColor">Optional foreground for the border glyphs (only meaningful with <see cref="TableBorder.Box"/>).</param>
    public static void Print(
        string[]? headers,
        string[][] rows,
        Color?[][]? cellColors,
        TableBorder border = TableBorder.Box,
        Color? headerColor = null,
        Color? borderColor = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        // Theme fallbacks: Accent for headers, Muted for the box frame.
        // Caller-supplied colors always win. (Per-cell body colors come
        // from cellColors and never fall back to a theme slot.)
        if (Crt.CurrentTheme is { } t)
        {
            headerColor ??= t.Accent;
            borderColor ??= t.Muted;
        }

        var widths = TableRenderer.ComputeWidths(headers, rows);
        if (widths.Length == 0) return;

        var hasHeader = headers is not null && headers.Length > 0;
        var boxBorders = border == TableBorder.Box;

        // Without ANSI we cannot color anything — emit the plain text
        // so redirection / dumb terminals stay readable.
        if (!Crt.ColorEnabled)
        {
            Crt.Sink.Write(TableRenderer.RenderPlain(headers, rows, boxBorders));
            return;
        }

        if (boxBorders)
            WriteBorder(widths, Glyphs.BoxTopLeft, Glyphs.BoxTeeTop, Glyphs.BoxTopRight, Glyphs.BoxHorizontal, borderColor);

        if (hasHeader)
        {
            WriteRow(headers!, widths, boxBorders, uniformFg: headerColor, bold: true, rowColors: null, borderColor);

            if (boxBorders)
                WriteBorder(widths, Glyphs.BoxTeeLeft, Glyphs.BoxCross, Glyphs.BoxTeeRight, Glyphs.BoxHorizontal, borderColor);
        }

        for (var r = 0; r < rows.Length; r++)
            WriteRow(rows[r], widths, boxBorders, uniformFg: null, bold: false, RowColors(cellColors, r), borderColor);

        if (boxBorders)
            WriteBorder(widths, Glyphs.BoxBottomLeft, Glyphs.BoxTeeBottom, Glyphs.BoxBottomRight, Glyphs.BoxHorizontal, borderColor);
    }

    private static void WriteBorder(int[] widths, char left, char junction, char right, char horizontal, Color? borderColor)
    {
        var line = TableRenderer.BuildBorder(widths, left, junction, right, horizontal);
        WriteColored(line, borderColor);
        Crt.Sink.WriteLine();
    }

    // Renders one row. <paramref name="uniformFg"/> colors the whole row
    // (header use); <paramref name="rowColors"/> colors per cell (body use,
    // wins over uniformFg when present for a given column).
    private static void WriteRow(string[] cells, int[] widths, bool boxBorders,
                                 Color? uniformFg, bool bold, Color?[]? rowColors, Color? borderColor)
    {
        var pad = new string(' ', TableRenderer.CellPadding);

        if (!boxBorders)
        {
            for (var c = 0; c < widths.Length; c++)
            {
                var content = (c < cells.Length ? cells[c] : "").PadRight(widths[c]);
                Crt.Sink.Write(pad);
                WriteCell(content, CellFg(rowColors, c) ?? uniformFg, bold);
                Crt.Sink.Write(pad);
            }
            Crt.Sink.WriteLine();
            return;
        }

        // Box-bordered: vertical bars get borderColor, cell content gets
        // its per-cell color (or the uniform fg) and bold.
        WriteColored(Glyphs.BoxVertical.ToString(), borderColor);
        for (var c = 0; c < widths.Length; c++)
        {
            var content = (c < cells.Length ? cells[c] : "").PadRight(widths[c]);
            Crt.Sink.Write(pad);
            WriteCell(content, CellFg(rowColors, c) ?? uniformFg, bold);
            Crt.Sink.Write(pad);
            WriteColored(Glyphs.BoxVertical.ToString(), borderColor);
        }
        Crt.Sink.WriteLine();
    }

    private static void WriteCell(string text, Color? fg, bool bold)
    {
        if (fg is not null || bold)
        {
            using (Crt.WithStyle(fg: fg, bold: bold))
                Crt.Sink.Write(text);
        }
        else
        {
            Crt.Sink.Write(text);
        }
    }

    private static void WriteColored(string text, Color? fg)
    {
        if (fg is { } c)
        {
            using (Crt.WithStyle(fg: c))
                Crt.Sink.Write(text);
        }
        else
        {
            Crt.Sink.Write(text);
        }
    }

    private static Color?[]? RowColors(Color?[][]? all, int rowIndex)
        => all is not null && rowIndex < all.Length ? all[rowIndex] : null;

    private static Color? CellFg(Color?[]? rowColors, int col)
        => rowColors is not null && col < rowColors.Length ? rowColors[col] : null;
}
