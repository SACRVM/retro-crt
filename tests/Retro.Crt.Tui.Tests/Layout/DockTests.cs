using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests.Layout;

public class DockTests
{
    [Fact]
    public void Top_strip_peels_from_top_and_shrinks_center_downward()
    {
        Span<Rect> strips = stackalloc Rect[1];

        var center = Dock.Peel(new Rect(0, 0, 80, 25), [new DockSpec(DockSide.Top, 1)], strips);

        Assert.Equal(new Rect(0, 0, 80, 1),  strips[0]);
        Assert.Equal(new Rect(0, 1, 80, 24), center);
    }

    [Fact]
    public void Bottom_strip_peels_from_bottom()
    {
        Span<Rect> strips = stackalloc Rect[1];

        var center = Dock.Peel(new Rect(0, 0, 80, 25), [new DockSpec(DockSide.Bottom, 1)], strips);

        Assert.Equal(new Rect(0, 24, 80, 1),  strips[0]);
        Assert.Equal(new Rect(0, 0,  80, 24), center);
    }

    [Fact]
    public void Left_and_right_strips_peel_from_their_sides()
    {
        Span<Rect> strips = stackalloc Rect[2];

        var center = Dock.Peel(
            new Rect(0, 0, 80, 25),
            [new DockSpec(DockSide.Left, 20), new DockSpec(DockSide.Right, 10)],
            strips);

        Assert.Equal(new Rect(0,  0, 20, 25), strips[0]);
        Assert.Equal(new Rect(70, 0, 10, 25), strips[1]);
        Assert.Equal(new Rect(20, 0, 50, 25), center);
    }

    [Fact]
    public void Header_body_footer_layout_consumes_space_in_order()
    {
        Span<Rect> strips = stackalloc Rect[2];

        var body = Dock.Peel(
            new Rect(0, 0, 80, 25),
            [new DockSpec(DockSide.Top, 3), new DockSpec(DockSide.Bottom, 1)],
            strips);

        Assert.Equal(new Rect(0, 0,  80, 3),  strips[0]);  // header
        Assert.Equal(new Rect(0, 24, 80, 1),  strips[1]);  // statusbar
        Assert.Equal(new Rect(0, 3,  80, 21), body);
    }

    [Fact]
    public void Strip_larger_than_remaining_space_clips_to_what_is_left()
    {
        Span<Rect> strips = stackalloc Rect[2];

        var center = Dock.Peel(
            new Rect(0, 0, 10, 5),
            [new DockSpec(DockSide.Top, 4), new DockSpec(DockSide.Top, 99)],
            strips);

        Assert.Equal(new Rect(0, 0, 10, 4), strips[0]);
        Assert.Equal(new Rect(0, 4, 10, 1), strips[1]);
        Assert.True(center.IsEmpty);
    }

    [Fact]
    public void Output_length_mismatch_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<Rect> strips = stackalloc Rect[1];
            Dock.Peel(
                new Rect(0, 0, 10, 10),
                [new DockSpec(DockSide.Top, 1), new DockSpec(DockSide.Bottom, 1)],
                strips);
        });
    }
}
