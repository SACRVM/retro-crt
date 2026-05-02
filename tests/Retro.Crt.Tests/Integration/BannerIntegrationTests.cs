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
}
