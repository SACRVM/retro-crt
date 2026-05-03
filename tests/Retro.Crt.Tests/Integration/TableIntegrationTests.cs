using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class TableIntegrationTests
{
    [Fact]
    public void Animated_print_emits_unicode_table_with_bold_header()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(
            ["Demo", "Time"],
            [
                ["tour",   "24s"],
                ["matrix", "25s"],
            ]);

        // Box glyphs present.
        Assert.Contains("┌", c.Out);
        Assert.Contains("┬", c.Out);
        Assert.Contains("┼", c.Out);
        Assert.Contains("└", c.Out);
        // Header rendered bold (data rows are not).
        Assert.Contains("\x1b[1m", c.Out);
        // Cell content present.
        Assert.Contains("Demo",   c.Out);
        Assert.Contains("matrix", c.Out);
        // Vertical bars present.
        Assert.Contains("│", c.Out);
    }

    [Fact]
    public void Animated_print_borderless_skips_box_glyphs()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(
            ["A", "B"],
            [["1", "2"]],
            border: TableBorder.None);

        Assert.DoesNotContain("┌", c.Out);
        Assert.DoesNotContain("│", c.Out);
        Assert.DoesNotContain("─", c.Out);
        // Header still bold even without borders.
        Assert.Contains("\x1b[1m", c.Out);
        Assert.Contains("A", c.Out);
        Assert.Contains("B", c.Out);
    }

    [Fact]
    public void Animated_print_applies_header_and_border_colors()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(
            ["A", "B"],
            [["1", "2"]],
            headerColor: Color.LightCyan,
            borderColor: Color.DarkGray);

        // Both colors must show up in the output stream.
        Assert.Contains("\x1b[96m", c.Out); // LightCyan fg
        Assert.Contains("\x1b[90m", c.Out); // DarkGray fg
    }

    [Fact]
    public void Non_ansi_print_emits_plain_text_only()
    {
        using var c = ConsoleCapture.Start(ansi: false, unicode: true);

        Table.Print(
            ["A", "B"],
            [["1", "2"]],
            headerColor: Color.LightCyan,
            borderColor: Color.DarkGray);

        Assert.DoesNotContain("\x1b[", c.Out);     // no ANSI at all
        Assert.Contains("│", c.Out);                // box glyphs still drawn
        Assert.Contains("A", c.Out);
        Assert.Contains("1", c.Out);
    }

    [Fact]
    public void Empty_rows_array_with_header_still_renders_header_block()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(["A", "B"], rows: []);

        // Top border, header row, separator, bottom border — three ─ runs.
        Assert.Contains("┌", c.Out);
        Assert.Contains("├", c.Out);
        Assert.Contains("└", c.Out);
        Assert.Contains("│", c.Out);
        Assert.Contains("A", c.Out);
        Assert.Contains("B", c.Out);
    }

    [Fact]
    public void No_header_no_rows_emits_nothing()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Table.Print(null, []);
        Table.Print([], []);

        Assert.Equal(string.Empty, c.Out);
    }

    [Fact]
    public void Null_rows_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Table.Print(["A"], rows: null!));
    }
}
