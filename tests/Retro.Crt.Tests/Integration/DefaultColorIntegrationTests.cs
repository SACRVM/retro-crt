using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class DefaultColorIntegrationTests
{
    [Fact]
    public void Default_background_cell_emits_sgr_49_so_it_inherits_the_terminal_bg()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        var current = new ScreenBuffer(1, 1);
        current[0, 0] = new Cell(' ', Color.Default, Color.Default);

        var sw = new StringWriter();
        ScreenRenderer.Render(previous: null, current, sw);
        var output = sw.ToString();

        // Default fg/bg → SGR 39 / 49 rather than a concrete color.
        Assert.Contains("\x1b[39m", output);
        Assert.Contains("\x1b[49m", output);
        // Crucially, no concrete black background (SGR 40 / 48;...) leaked.
        Assert.DoesNotContain("\x1b[40m", output);
    }

    [Fact]
    public void Default_color_is_distinct_from_black()
    {
        Assert.NotEqual(Color.Black, Color.Default);
        Assert.Equal(ColorMode.Default, Color.Default.Mode);
    }
}
