using Retro.Crt;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class StackPanelTests
{
    [Fact]
    public void Horizontal_lays_children_left_to_right_with_fixed_sizes()
    {
        var a = new BoundsCapturingView();
        var b = new BoundsCapturingView();
        var stack = new StackPanel
        {
            Bounds      = new Rect(0, 0, 30, 5),
            Orientation = Orientation.Horizontal,
            Sizes       = { LayoutSize.Cells(10), LayoutSize.Cells(20) },
            Children    = { a, b },
        };

        stack.OnDraw(new ScreenBuffer(30, 5));

        Assert.Equal(new Rect(0,  0, 10, 5), a.Bounds);
        Assert.Equal(new Rect(10, 0, 20, 5), b.Bounds);
    }

    [Fact]
    public void Vertical_lays_children_top_to_bottom_with_star_sizes()
    {
        var a = new BoundsCapturingView();
        var b = new BoundsCapturingView();
        var stack = new StackPanel
        {
            Bounds      = new Rect(0, 0, 10, 12),
            Orientation = Orientation.Vertical,
            Sizes       = { LayoutSize.Star(1), LayoutSize.Star(2) },
            Children    = { a, b },
        };

        stack.OnDraw(new ScreenBuffer(10, 12));

        Assert.Equal(4, a.Bounds.Height);
        Assert.Equal(8, b.Bounds.Height);
        Assert.Equal(0, a.Bounds.Y);
        Assert.Equal(4, b.Bounds.Y);
    }

    [Fact]
    public void Missing_size_entries_default_to_star()
    {
        var a = new BoundsCapturingView();
        var b = new BoundsCapturingView();
        var stack = new StackPanel
        {
            Bounds      = new Rect(0, 0, 10, 5),
            Orientation = Orientation.Horizontal,
            // no sizes at all
            Children    = { a, b },
        };

        stack.OnDraw(new ScreenBuffer(10, 5));

        Assert.Equal(5, a.Bounds.Width);
        Assert.Equal(5, b.Bounds.Width);
    }

    [Fact]
    public void Empty_children_is_a_no_op()
    {
        var stack = new StackPanel { Bounds = new Rect(0, 0, 10, 5) };

        // Should not throw despite no children.
        stack.OnDraw(new ScreenBuffer(10, 5));
    }

    private sealed class BoundsCapturingView : View
    {
        public override void OnDraw(ScreenBuffer screen) { /* no-op */ }
    }
}
