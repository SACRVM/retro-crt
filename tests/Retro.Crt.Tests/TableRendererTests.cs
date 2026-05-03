using Retro.Crt.Internals;
using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests;

[Collection(EnvMutatingCollection.Name)]
public class TableRendererTests
{
    public TableRendererTests()
    {
        // Force unicode for snapshot comparisons. Tests for the ASCII
        // fallback flip this back via OverrideForTests.
        TerminalCapabilities.OverrideForTests(unicode: true);
    }

    [Fact]
    public void ComputeWidths_takes_max_of_header_and_row_cells()
    {
        var widths = TableRenderer.ComputeWidths(
            ["Demo", "Time"],
            [
                ["matrix", "25s"],
                ["boot", "22s"],
                ["x", "longer than header"],
            ]);

        Assert.Equal([6, "longer than header".Length], widths);
    }

    [Fact]
    public void ComputeWidths_handles_no_header()
    {
        var widths = TableRenderer.ComputeWidths(null, [["abc", "de"]]);
        Assert.Equal([3, 2], widths);
    }

    [Fact]
    public void ComputeWidths_handles_ragged_rows_with_zero_pad()
    {
        var widths = TableRenderer.ComputeWidths(
            ["A", "B", "C"],
            [
                ["12", "x"],          // shorter than header
                ["3", "yy", "longer"], // full row
            ]);

        Assert.Equal([2, 2, 6], widths);
    }

    [Fact]
    public void RenderPlain_produces_unicode_box_table()
    {
        var actual = TableRenderer.RenderPlain(
            ["Name", "Age"],
            [
                ["Chloe", "26"],
                ["Wolf", "5"],
            ],
            boxBorders: true);

        var expected =
            "┌───────┬─────┐\n" +
            "│ Name  │ Age │\n" +
            "├───────┼─────┤\n" +
            "│ Chloe │ 26  │\n" +
            "│ Wolf  │ 5   │\n" +
            "└───────┴─────┘\n";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenderPlain_no_border_omits_box_glyphs()
    {
        var actual = TableRenderer.RenderPlain(
            ["Name", "Age"],
            [
                ["Chloe", "26"],
            ],
            boxBorders: false);

        var expected =
            " Name   Age \n" +
            " Chloe  26  \n";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenderPlain_without_header_skips_separator_line()
    {
        var actual = TableRenderer.RenderPlain(
            null,
            [
                ["A", "1"],
                ["B", "2"],
            ],
            boxBorders: true);

        var expected =
            "┌───┬───┐\n" +
            "│ A │ 1 │\n" +
            "│ B │ 2 │\n" +
            "└───┴───┘\n";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenderPlain_with_no_columns_returns_empty()
    {
        Assert.Equal("", TableRenderer.RenderPlain(null, [], boxBorders: true));
        Assert.Equal("", TableRenderer.RenderPlain([], [], boxBorders: true));
    }

    [Fact]
    public void RenderPlain_falls_back_to_ascii_glyphs_without_unicode()
    {
        TerminalCapabilities.OverrideForTests(unicode: false);

        var actual = TableRenderer.RenderPlain(
            ["A", "B"],
            [["1", "2"]],
            boxBorders: true);

        var expected =
            "+---+---+\n" +
            "| A | B |\n" +
            "+---+---+\n" +
            "| 1 | 2 |\n" +
            "+---+---+\n";

        Assert.Equal(expected, actual);
    }
}
