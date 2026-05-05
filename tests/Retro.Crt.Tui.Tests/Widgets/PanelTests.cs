using Retro.Crt;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class PanelTests
{
    [Fact]
    public void Draws_box_corners_at_bounds_extents()
    {
        var screen = new ScreenBuffer(20, 10);
        var panel  = new Panel { Bounds = new Rect(2, 1, 8, 5) };

        panel.OnDraw(screen);

        Assert.Equal('┌', screen[2, 1].Glyph);
        Assert.Equal('┐', screen[9, 1].Glyph);
        Assert.Equal('└', screen[2, 5].Glyph);
        Assert.Equal('┘', screen[9, 5].Glyph);
    }

    [Fact]
    public void Draws_horizontal_and_vertical_edges()
    {
        var screen = new ScreenBuffer(10, 6);
        var panel  = new Panel { Bounds = new Rect(0, 0, 10, 6) };

        panel.OnDraw(screen);

        Assert.Equal('─', screen[1, 0].Glyph);
        Assert.Equal('─', screen[1, 5].Glyph);
        Assert.Equal('│', screen[0, 1].Glyph);
        Assert.Equal('│', screen[9, 1].Glyph);
    }

    [Fact]
    public void Fills_interior_with_background()
    {
        var screen = new ScreenBuffer(10, 6);
        var panel = new Panel
        {
            Bounds     = new Rect(0, 0, 10, 6),
            Background = Color.DarkBlue,
        };

        panel.OnDraw(screen);

        var inner = screen[3, 3];
        Assert.Equal(' ',            inner.Glyph);
        Assert.Equal(Color.DarkBlue, inner.Bg);
    }

    [Fact]
    public void Renders_title_inside_top_edge()
    {
        var screen = new ScreenBuffer(20, 4);
        var panel  = new Panel { Bounds = new Rect(0, 0, 20, 4), Title = "Hi" };

        panel.OnDraw(screen);

        // " Hi " centered in a 20-wide top edge → starts at col (20-4)/2 = 8.
        Assert.Equal(' ', screen[8,  0].Glyph);
        Assert.Equal('H', screen[9,  0].Glyph);
        Assert.Equal('i', screen[10, 0].Glyph);
        Assert.Equal(' ', screen[11, 0].Glyph);
    }

    [Fact]
    public void IsActive_swaps_border_color_to_accent()
    {
        var screen = new ScreenBuffer(6, 4);
        var panel  = new Panel
        {
            Bounds     = new Rect(0, 0, 6, 4),
            Foreground = Color.LightGray,
            Accent     = Color.LightCyan,
            IsActive   = true,
        };

        panel.OnDraw(screen);

        Assert.Equal(Color.LightCyan, screen[0, 0].Fg);
    }

    [Fact]
    public void Skips_drawing_when_bounds_too_small()
    {
        var screen = new ScreenBuffer(5, 5);
        var panel  = new Panel { Bounds = new Rect(0, 0, 1, 1) };

        panel.OnDraw(screen);

        // Nothing painted — every cell stays Cell.Empty.
        Assert.Equal(Cell.Empty, screen[0, 0]);
    }
}
