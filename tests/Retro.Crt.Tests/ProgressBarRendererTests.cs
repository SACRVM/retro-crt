using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class ProgressBarRendererTests
{
    [Theory]
    [InlineData(0.0,  10, 0)]
    [InlineData(0.5,  10, 5)]
    [InlineData(1.0,  10, 10)]
    [InlineData(-0.1, 10, 0)]
    [InlineData(1.5,  10, 10)]
    public void FilledCells_clamps_to_range(double ratio, int width, int expected)
    {
        Assert.Equal(expected, ProgressBarRenderer.FilledCells(ratio, width));
    }

    [Fact]
    public void RenderBar_emits_filled_then_empty_glyphs()
    {
        var s = ProgressBarRenderer.RenderBar(filled: 3, width: 10, fullChar: '#', emptyChar: '-');
        Assert.Equal("###-------", s);
    }

    [Fact]
    public void RenderBar_empty_width_is_empty_string()
    {
        var s = ProgressBarRenderer.RenderBar(filled: 0, width: 0, fullChar: '#', emptyChar: '-');
        Assert.Equal("", s);
    }

    [Fact]
    public void RenderFrame_includes_label_and_percent()
    {
        var s = ProgressBarRenderer.RenderFrame(
            label: "load",
            ratio: 0.5,
            width: 4,
            fullChar: '#',
            emptyChar: '-',
            showPercent: true);
        Assert.Equal("load ##--  50%", s);
    }

    [Fact]
    public void RenderFrame_without_label_or_percent_is_just_the_bar()
    {
        var s = ProgressBarRenderer.RenderFrame(
            label: null,
            ratio: 0.25,
            width: 4,
            fullChar: '#',
            emptyChar: '-',
            showPercent: false);
        Assert.Equal("#---", s);
    }

    [Fact]
    public void RenderFrame_percent_is_right_aligned_three_wide()
    {
        var s5 = ProgressBarRenderer.RenderFrame(null, 0.05, 4, '#', '-', showPercent: true);
        Assert.EndsWith("  5%", s5);
        var s100 = ProgressBarRenderer.RenderFrame(null, 1.0, 4, '#', '-', showPercent: true);
        Assert.EndsWith("100%", s100);
    }
}
