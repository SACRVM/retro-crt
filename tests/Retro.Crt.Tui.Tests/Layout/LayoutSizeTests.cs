using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests.Layout;

public class LayoutSizeTests
{
    [Fact]
    public void Cells_factory_creates_a_fixed_track()
    {
        var s = LayoutSize.Cells(7);
        Assert.True(s.IsFixed);
        Assert.False(s.IsStar);
        Assert.Equal(7, s.FixedCells);
        Assert.Equal(0, s.StarWeight);
    }

    [Fact]
    public void Star_factory_creates_a_star_track()
    {
        var s = LayoutSize.Star(3);
        Assert.True(s.IsStar);
        Assert.False(s.IsFixed);
        Assert.Equal(3, s.StarWeight);
        Assert.Equal(0, s.FixedCells);
    }

    [Fact]
    public void Star_default_weight_is_one()
    {
        Assert.Equal(1, LayoutSize.Star().StarWeight);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0,  0)]
    public void Cells_clamps_negatives_at_zero(int input, int expected)
    {
        Assert.Equal(expected, LayoutSize.Cells(input).FixedCells);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0,  1)]
    [InlineData(7,  7)]
    public void Star_clamps_below_one_to_one(int input, int expected)
    {
        Assert.Equal(expected, LayoutSize.Star(input).StarWeight);
    }

    [Fact]
    public void Equality_is_value_based()
    {
        Assert.Equal(LayoutSize.Cells(5), LayoutSize.Cells(5));
        Assert.NotEqual(LayoutSize.Cells(5), LayoutSize.Cells(6));
        Assert.NotEqual(LayoutSize.Cells(3), LayoutSize.Star(3));
    }
}
