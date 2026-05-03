using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class BannerIntegrationTests
{
    [Fact]
    public void Box_with_unicode_uses_box_drawing_glyphs()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("hi");

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines[0], "┌────┐");
        Assert.Contains("│ hi │", lines[1]);
        Assert.Contains(lines[2], "└────┘");
    }

    [Fact]
    public void Box_falls_back_to_ascii_without_unicode()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: false);

        Banner.Box("hi");

        Assert.Contains("+----+", c.Out);
        Assert.Contains("| hi |", c.Out);
    }

    [Fact]
    public void Box_with_color_wraps_output_in_sgr_then_reset()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("ok", fg: Color.LightCyan);

        Assert.StartsWith("\x1b[96m", c.Out);
        Assert.EndsWith("\x1b[0m", c.Out.TrimEnd('\n'));
    }

    [Fact]
    public void Box_without_ansi_emits_no_escapes()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Banner.Box("hi", fg: Color.LightCyan);

        Assert.DoesNotContain("\x1b", c.Out);
    }

    [Fact]
    public void Gradient_emits_one_color_change_per_line()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Gradient(
            ["a", "b", "c"],
            from: Color.Rgb(0, 0, 0),
            to:   Color.Rgb(255, 255, 255),
            bold: false);

        // Three lines, three foreground SGR sequences.
        var fgCount = c.Out.Split("\x1b[38;2;").Length - 1;
        Assert.Equal(3, fgCount);
    }

    [Fact]
    public void Gradient_endpoints_match_inputs()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Gradient(
            ["one", "two", "three"],
            from: Color.Rgb(0, 0, 0),
            to:   Color.Rgb(200, 100, 50),
            bold: false);

        Assert.Contains("\x1b[38;2;0;0;0m", c.Out);
        Assert.Contains("\x1b[38;2;200;100;50m", c.Out);
    }

    [Fact]
    public void Gradient_empty_lines_emits_nothing()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Banner.Gradient([], Color.Rgb(0, 0, 0), Color.Rgb(255, 255, 255));

        Assert.Equal(string.Empty, c.Out);
    }

    [Fact]
    public void Box_with_explicit_width_pads_lines_to_that_size()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("hi", width: 20);

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Outer corners + horizontal fill = 20 cells; inner content row
        // also 20 cells.
        Assert.Equal(20, lines[0].Length);
        Assert.Equal(20, lines[1].Length);
        Assert.StartsWith("│ hi", lines[1]);
        Assert.EndsWith("│", lines[1]);
    }

    [Fact]
    public void Box_with_width_smaller_than_content_falls_back_to_content_width()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("hello world", width: 5);

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // "hello world" = 11 + 2 padding + 2 corners = 15
        Assert.Equal(15, lines[0].Length);
    }

    [Fact]
    public void Box_with_center_alignment_centres_short_text_in_explicit_width()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("hi", width: 12, align: BoxAlign.Center);

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // total=12, padding=1 → inner content area=8; "hi" (slack=6) →
        // 3 left, 3 right; the row is │ + " " + "   hi   " + " " + │.
        Assert.Equal("│    hi    │", lines[1]);
    }

    [Fact]
    public void Box_with_right_alignment_pushes_text_to_the_right()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box("hi", width: 12, align: BoxAlign.Right);

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // slack=6 all on the left, plus the standard one-space padding
        // each side → seven spaces before "hi", one after.
        Assert.Equal("│       hi │", lines[1]);
    }

    [Fact]
    public void Box_indents_subsequent_lines_to_anchor_column()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);
        ConsoleCapture.OverrideCursorLeft(5);
        try
        {
            // Multi-line box; first line is written wherever the cursor
            // is (caller's responsibility / GotoXY). Lines 2..n must be
            // indented to the same column so the left edge stays
            // vertical.
            Banner.Box(["aa", "bbbb"]);
        }
        finally { ConsoleCapture.OverrideCursorLeft(null); }

        var raw = c.Out;
        // First line: no leading spaces (caller already positioned).
        var firstNl = raw.IndexOf('\n');
        Assert.True(firstNl > 0);
        Assert.StartsWith("┌", raw);

        // Second line: five leading spaces, then the vertical bar.
        var secondLine = raw[(firstNl + 1)..raw.IndexOf('\n', firstNl + 1)];
        Assert.StartsWith("     │", secondLine);
    }

    [Fact]
    public void Box_without_anchor_writes_each_line_unindented()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Banner.Box(["a", "b"]);

        var lines = c.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("┌", lines[0]);
        Assert.StartsWith("│", lines[1]);
        Assert.StartsWith("│", lines[2]);
        Assert.StartsWith("└", lines[3]);
    }
}
