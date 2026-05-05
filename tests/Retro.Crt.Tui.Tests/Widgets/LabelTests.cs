using Retro.Crt;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class LabelTests
{
    [Fact]
    public void Not_focusable_by_default()
    {
        Assert.False(new Label("hi").IsFocusable);
    }

    [Fact]
    public void Renders_left_aligned_by_default()
    {
        var screen = new ScreenBuffer(10, 1);
        var lbl = new Label("hi") { Bounds = new Rect(0, 0, 10, 1) };

        lbl.OnDraw(screen);

        Assert.Equal('h', screen[0, 0].Glyph);
        Assert.Equal('i', screen[1, 0].Glyph);
        Assert.Equal(' ', screen[2, 0].Glyph);
    }

    [Fact]
    public void Center_aligns_text()
    {
        var screen = new ScreenBuffer(10, 1);
        var lbl = new Label("hi") { Align = TextAlign.Center, Bounds = new Rect(0, 0, 10, 1) };

        lbl.OnDraw(screen);

        Assert.Equal('h', screen[4, 0].Glyph);
        Assert.Equal('i', screen[5, 0].Glyph);
    }

    [Fact]
    public void Right_aligns_text()
    {
        var screen = new ScreenBuffer(10, 1);
        var lbl = new Label("hi") { Align = TextAlign.Right, Bounds = new Rect(0, 0, 10, 1) };

        lbl.OnDraw(screen);

        Assert.Equal('h', screen[8, 0].Glyph);
        Assert.Equal('i', screen[9, 0].Glyph);
    }

    [Fact]
    public void Splits_on_newlines()
    {
        var screen = new ScreenBuffer(10, 3);
        var lbl = new Label("a\nb\nc") { Bounds = new Rect(0, 0, 10, 3) };

        lbl.OnDraw(screen);

        Assert.Equal('a', screen[0, 0].Glyph);
        Assert.Equal('b', screen[0, 1].Glyph);
        Assert.Equal('c', screen[0, 2].Glyph);
    }

    [Fact]
    public void Clips_lines_longer_than_width()
    {
        var screen = new ScreenBuffer(3, 1);
        var lbl = new Label("hello") { Bounds = new Rect(0, 0, 3, 1) };

        lbl.OnDraw(screen);

        Assert.Equal('h', screen[0, 0].Glyph);
        Assert.Equal('e', screen[1, 0].Glyph);
        Assert.Equal('l', screen[2, 0].Glyph);
    }

    [Fact]
    public void Skips_lines_below_bounds_height()
    {
        var screen = new ScreenBuffer(5, 1);
        var lbl = new Label("a\nb\nc") { Bounds = new Rect(0, 0, 5, 1) };

        lbl.OnDraw(screen);

        Assert.Equal('a', screen[0, 0].Glyph);
    }
}
