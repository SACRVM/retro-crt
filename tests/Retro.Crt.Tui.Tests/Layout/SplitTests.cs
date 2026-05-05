using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests.Layout;

public class SplitTests
{
    [Fact]
    public void Horizontal_with_only_fixed_tracks_lays_them_left_to_right()
    {
        var bounds = new Rect(0, 0, 30, 10);
        Span<Rect> output = stackalloc Rect[3];

        Split.Horizontal(bounds, [LayoutSize.Cells(5), LayoutSize.Cells(15), LayoutSize.Cells(10)], output);

        Assert.Equal(new Rect(0,  0, 5,  10), output[0]);
        Assert.Equal(new Rect(5,  0, 15, 10), output[1]);
        Assert.Equal(new Rect(20, 0, 10, 10), output[2]);
    }

    [Fact]
    public void Vertical_with_only_fixed_tracks_lays_them_top_to_bottom()
    {
        var bounds = new Rect(2, 4, 8, 12);
        Span<Rect> output = stackalloc Rect[2];

        Split.Vertical(bounds, [LayoutSize.Cells(3), LayoutSize.Cells(5)], output);

        Assert.Equal(new Rect(2, 4, 8, 3), output[0]);
        Assert.Equal(new Rect(2, 7, 8, 5), output[1]);
    }

    [Fact]
    public void Horizontal_with_only_star_tracks_divides_proportionally()
    {
        var bounds = new Rect(0, 0, 12, 5);
        Span<Rect> output = stackalloc Rect[3];

        Split.Horizontal(bounds, [LayoutSize.Star(1), LayoutSize.Star(2), LayoutSize.Star(1)], output);

        // 1:2:1 of 12 cells = 3:6:3
        Assert.Equal(3, output[0].Width);
        Assert.Equal(6, output[1].Width);
        Assert.Equal(3, output[2].Width);
        Assert.Equal(0,  output[0].X);
        Assert.Equal(3,  output[1].X);
        Assert.Equal(9,  output[2].X);
    }

    [Fact]
    public void Mixed_fixed_and_star_subtracts_fixed_first()
    {
        var bounds = new Rect(0, 0, 20, 5);
        Span<Rect> output = stackalloc Rect[3];

        // 6 fixed + 14 star → split 1:1 = 7 each
        Split.Horizontal(
            bounds,
            [LayoutSize.Cells(6), LayoutSize.Star(1), LayoutSize.Star(1)],
            output);

        Assert.Equal(6, output[0].Width);
        Assert.Equal(7, output[1].Width);
        Assert.Equal(7, output[2].Width);
    }

    [Fact]
    public void Star_distribution_uses_largest_remainder_rounding()
    {
        var bounds = new Rect(0, 0, 10, 1);
        Span<Rect> output = stackalloc Rect[3];

        // 10 / (1+1+1) = 3 r1 — leftover cell goes to the first track
        // (largest remainder, ties broken by index).
        Split.Horizontal(
            bounds,
            [LayoutSize.Star(1), LayoutSize.Star(1), LayoutSize.Star(1)],
            output);

        Assert.Equal(10, output[0].Width + output[1].Width + output[2].Width);
        Assert.Equal(4, output[0].Width);
        Assert.Equal(3, output[1].Width);
        Assert.Equal(3, output[2].Width);
    }

    [Fact]
    public void Fixed_overflow_scales_tracks_pro_rata()
    {
        var bounds = new Rect(0, 0, 5, 1);
        Span<Rect> output = stackalloc Rect[2];

        // Asks for 6 + 4 = 10 cells inside 5; should get scaled down to fit.
        Split.Horizontal(bounds, [LayoutSize.Cells(6), LayoutSize.Cells(4)], output);

        Assert.Equal(5, output[0].Width + output[1].Width);
        Assert.True(output[0].Width >= output[1].Width); // bigger track stays bigger
    }

    [Fact]
    public void Empty_bounds_produces_empty_rects()
    {
        Span<Rect> output = stackalloc Rect[2];

        Split.Horizontal(Rect.Empty, [LayoutSize.Star(1), LayoutSize.Star(1)], output);

        Assert.True(output[0].IsEmpty);
        Assert.True(output[1].IsEmpty);
    }

    [Fact]
    public void Output_length_mismatch_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<Rect> output = stackalloc Rect[1];
            Split.Horizontal(new Rect(0, 0, 10, 10), [LayoutSize.Star(1), LayoutSize.Star(1)], output);
        });
    }
}
