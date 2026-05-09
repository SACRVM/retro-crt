using Retro.Crt;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class SeparatorTests
{
    [Fact]
    public void Defaults_to_box_drawing_glyph_and_not_focusable()
    {
        var s = new Separator();
        Assert.Equal('─', s.Glyph);
        Assert.False(s.IsFocusable);
    }

    [Fact]
    public void Fills_every_cell_in_bounds_with_glyph()
    {
        var s = new Separator { Bounds = new Rect(2, 1, 5, 1) };
        var screen = new ScreenBuffer(10, 5);

        s.OnDraw(screen);

        for (var x = 2; x < 7; x++)
            Assert.Equal('─', screen[x, 1].Glyph);
        // Outside the bounds is untouched.
        Assert.Equal(' ', screen[1, 1].Glyph);
        Assert.Equal(' ', screen[7, 1].Glyph);
    }

    [Fact]
    public void Custom_glyph_is_respected()
    {
        var s = new Separator { Glyph = '═', Bounds = new Rect(0, 0, 4, 1) };
        var screen = new ScreenBuffer(4, 1);

        s.OnDraw(screen);

        for (var x = 0; x < 4; x++)
            Assert.Equal('═', screen[x, 0].Glyph);
    }

    [Fact]
    public void Foreground_and_background_are_painted()
    {
        var s = new Separator
        {
            Bounds     = new Rect(0, 0, 3, 1),
            Foreground = Color.LightCyan,
            Background = Color.DarkGray,
        };
        var screen = new ScreenBuffer(3, 1);

        s.OnDraw(screen);

        Assert.Equal(Color.LightCyan, screen[0, 0].Fg);
        Assert.Equal(Color.DarkGray,  screen[0, 0].Bg);
    }

    [Fact]
    public void Vertical_rule_is_a_one_column_separator()
    {
        var s = new Separator { Glyph = '│', Bounds = new Rect(3, 0, 1, 4) };
        var screen = new ScreenBuffer(5, 4);

        s.OnDraw(screen);

        for (var y = 0; y < 4; y++)
            Assert.Equal('│', screen[3, y].Glyph);
    }

    [Fact]
    public void Empty_bounds_is_a_noop()
    {
        var s = new Separator { Bounds = new Rect(0, 0, 0, 0) };
        var screen = new ScreenBuffer(4, 1);

        s.OnDraw(screen);

        Assert.Equal(' ', screen[0, 0].Glyph);
    }
}
