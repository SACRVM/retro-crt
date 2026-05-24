using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class TableCellIntegrationTests
{
    [Fact]
    public void Per_cell_foreground_colors_only_the_targeted_cell()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(
            ["Service", "Status"],
            [
                ["web", "Running"],
                ["db",  "No listener"],
            ],
            cellColors:
            [
                [null, Color.LightGreen],
                [null, Color.LightRed],
            ]);

        // LightGreen (SGR 92) and LightRed (SGR 91) appear for the status cells.
        Assert.Contains("\x1b[92m", c.Out);
        Assert.Contains("\x1b[91m", c.Out);
        Assert.Contains("Running",     c.Out);
        Assert.Contains("No listener", c.Out);
        Assert.Contains("web", c.Out);
    }

    [Fact]
    public void Short_and_missing_color_rows_default_to_uncolored()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        // Only the first row + first cell has a color; everything else is
        // out of range and must render without error.
        Table.Print(
            ["A", "B"],
            [
                ["1", "2"],
                ["3", "4"],
            ],
            cellColors: [[Color.LightGreen]]);

        Assert.Contains("\x1b[92m", c.Out);
        Assert.Contains("3", c.Out);
        Assert.Contains("4", c.Out);
    }

    [Fact]
    public void Plain_fallback_ignores_cell_colors_without_escapes()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Table.Print(
            ["Service", "Status"],
            [["web", "Running"]],
            cellColors: [[null, Color.LightGreen]]);

        Assert.DoesNotContain("\x1b", c.Out);
        Assert.Contains("Running", c.Out);
        Assert.Contains("web", c.Out);
    }

    [Fact]
    public void Collection_expression_rows_still_resolve_to_the_plain_overload()
    {
        // Guards against the overload ambiguity that a string→cell implicit
        // conversion would have introduced: this must compile and run.
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(["A", "B"], [["1", "2"]]);

        Assert.Contains("1", c.Out);
        Assert.Contains("2", c.Out);
    }
}
