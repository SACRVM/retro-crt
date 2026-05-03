using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Tiny aligned-column table renderer. Box-drawing borders by default,
/// header in bold, optional foreground colors for header and borders.
/// One column auto-resizes to its widest cell — no manual width
/// configuration. Single-line cells only.
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
    {
        ArgumentNullException.ThrowIfNull(rows);

        var widths = TableRenderer.ComputeWidths(headers, rows);
        if (widths.Length == 0) return;

        var hasHeader = headers is not null && headers.Length > 0;
        var boxBorders = border == TableBorder.Box;

        // Without ANSI we cannot color anything — emit the plain text
        // so redirection / dumb terminals stay readable.
        if (!Crt.ColorEnabled)
        {
            Console.Out.Write(TableRenderer.RenderPlain(headers, rows, boxBorders));
            return;
        }

        if (boxBorders)
            WriteBorder(widths, Glyphs.BoxTopLeft, Glyphs.BoxTeeTop, Glyphs.BoxTopRight, Glyphs.BoxHorizontal, borderColor);

        if (hasHeader)
        {
            WriteRow(headers!, widths, boxBorders, headerColor, bold: true, borderColor);

            if (boxBorders)
                WriteBorder(widths, Glyphs.BoxTeeLeft, Glyphs.BoxCross, Glyphs.BoxTeeRight, Glyphs.BoxHorizontal, borderColor);
        }

        foreach (var row in rows)
            WriteRow(row, widths, boxBorders, fg: null, bold: false, borderColor);

        if (boxBorders)
            WriteBorder(widths, Glyphs.BoxBottomLeft, Glyphs.BoxTeeBottom, Glyphs.BoxBottomRight, Glyphs.BoxHorizontal, borderColor);
    }

    private static void WriteBorder(int[] widths, char left, char junction, char right, char horizontal, Color? borderColor)
    {
        var line = TableRenderer.BuildBorder(widths, left, junction, right, horizontal);
        WriteColored(line, borderColor);
        Console.Out.WriteLine();
    }

    private static void WriteRow(string[] cells, int[] widths, bool boxBorders,
                                 Color? fg, bool bold, Color? borderColor)
    {
        if (!boxBorders)
        {
            // Borderless: cells flow with just padding. Color the whole
            // row in fg / bold without emitting any vertical bars.
            var line = TableRenderer.BuildRow(cells, widths, boxBorders: false);
            using (Crt.WithStyle(fg: fg, bold: bold))
                Console.Out.Write(line);
            Console.Out.WriteLine();
            return;
        }

        // Box-bordered: vertical bars get borderColor, cell content gets fg/bold.
        var pad = new string(' ', TableRenderer.CellPadding);
        WriteColored(Glyphs.BoxVertical.ToString(), borderColor);

        for (var c = 0; c < widths.Length; c++)
        {
            var content = c < cells.Length ? cells[c] : "";
            using (Crt.WithStyle(fg: fg, bold: bold))
                Console.Out.Write(pad + content.PadRight(widths[c]) + pad);

            WriteColored(Glyphs.BoxVertical.ToString(), borderColor);
        }

        Console.Out.WriteLine();
    }

    private static void WriteColored(string text, Color? fg)
    {
        if (fg is { } c)
        {
            using (Crt.WithStyle(fg: c))
                Console.Out.Write(text);
        }
        else
        {
            Console.Out.Write(text);
        }
    }
}
