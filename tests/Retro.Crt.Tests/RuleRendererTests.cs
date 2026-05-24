using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class RuleRendererTests
{
    [Fact]
    public void Untitled_rule_is_an_unbroken_line()
    {
        Assert.Equal("------", RuleRenderer.Build(null, 6, '-'));
    }

    [Fact]
    public void Titled_rule_centers_the_label_with_breathing_room()
    {
        // width 13, label " AB " is 4 → 9 remaining → 4 left, 5 right.
        var s = RuleRenderer.Build("AB", 13, '-');
        Assert.Equal("---- AB -----", s);
        Assert.Equal(13, s.Length);
    }

    [Fact]
    public void Title_wider_than_the_rule_is_truncated_to_width()
    {
        var s = RuleRenderer.Build("a very long title", 6, '-');
        Assert.Equal(6, s.Length);
    }

    [Fact]
    public void Width_below_one_is_clamped()
    {
        Assert.Equal("-", RuleRenderer.Build(null, 0, '-'));
    }

    [Fact]
    public void Uses_the_supplied_glyph()
    {
        Assert.Equal("══════", RuleRenderer.Build(null, 6, '═'));
    }
}
