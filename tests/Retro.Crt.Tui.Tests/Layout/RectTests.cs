using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests.Layout;

public class RectTests
{
    [Fact]
    public void Default_rect_is_empty()
    {
        Assert.True(default(Rect).IsEmpty);
        Assert.True(Rect.Empty.IsEmpty);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 5)]
    [InlineData(5, 0)]
    public void IsEmpty_when_either_extent_is_non_positive(int w, int h)
    {
        Assert.True(new Rect(0, 0, w, h).IsEmpty);
    }

    [Fact]
    public void Right_and_Bottom_are_one_past_the_last_cell()
    {
        var r = new Rect(2, 3, 10, 4);
        Assert.Equal(12, r.Right);
        Assert.Equal(7,  r.Bottom);
    }

    [Fact]
    public void Inset_uniform_shrinks_all_sides()
    {
        var r = new Rect(0, 0, 10, 10).Inset(2);
        Assert.Equal(new Rect(2, 2, 6, 6), r);
    }

    [Fact]
    public void Inset_per_side_shrinks_independently()
    {
        var r = new Rect(0, 0, 10, 10).Inset(1, 2, 3, 4);
        Assert.Equal(new Rect(1, 2, 6, 4), r);
    }

    [Fact]
    public void Inset_clamps_extents_at_zero()
    {
        var r = new Rect(0, 0, 4, 4).Inset(10);
        Assert.True(r.IsEmpty);
        Assert.Equal(0, r.Width);
        Assert.Equal(0, r.Height);
    }

    [Theory]
    [InlineData(0,  0,  true)]
    [InlineData(9,  4,  true)]
    [InlineData(10, 4,  false)] // x == Right
    [InlineData(4,  5,  false)] // y == Bottom
    [InlineData(-1, 0,  false)]
    public void Contains_uses_half_open_intervals(int x, int y, bool expected)
    {
        Assert.Equal(expected, new Rect(0, 0, 10, 5).Contains(x, y));
    }
}
