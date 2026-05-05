using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class TextBoxTests
{
    [Fact]
    public void Defaults_to_focusable()
    {
        Assert.True(new TextBox().IsFocusable);
    }

    [Fact]
    public void Ctor_seeds_text_and_parks_cursor_at_end()
    {
        var t = new TextBox("hello");

        Assert.Equal("hello", t.Text);
        Assert.Equal(5, t.CursorPosition);
    }

    [Fact]
    public void Glyph_inserts_at_cursor()
    {
        var t = new TextBox { Bounds = new Rect(0, 0, 10, 1) };
        var app = MakeApp(t);

        t.OnKey(new KeyEvent(Key.Glyph, 'h'), app);
        t.OnKey(new KeyEvent(Key.Glyph, 'i'), app);

        Assert.Equal("hi", t.Text);
        Assert.Equal(2, t.CursorPosition);
    }

    [Fact]
    public void Backspace_removes_char_left_of_cursor()
    {
        var t = new TextBox("abc");

        t.OnKey(new KeyEvent(Key.Backspace), MakeApp(t));

        Assert.Equal("ab", t.Text);
        Assert.Equal(2, t.CursorPosition);
    }

    [Fact]
    public void Backspace_at_start_is_noop()
    {
        var t = new TextBox("abc") { CursorPosition = 0 };

        t.OnKey(new KeyEvent(Key.Backspace), MakeApp(t));

        Assert.Equal("abc", t.Text);
        Assert.Equal(0, t.CursorPosition);
    }

    [Fact]
    public void Delete_removes_char_under_cursor()
    {
        var t = new TextBox("abc") { CursorPosition = 1 };

        t.OnKey(new KeyEvent(Key.Delete), MakeApp(t));

        Assert.Equal("ac", t.Text);
        Assert.Equal(1, t.CursorPosition);
    }

    [Fact]
    public void Arrows_move_cursor_within_bounds()
    {
        var t = new TextBox("abc");
        var app = MakeApp(t);

        t.OnKey(new KeyEvent(Key.Left), app);
        Assert.Equal(2, t.CursorPosition);

        t.OnKey(new KeyEvent(Key.Right), app);
        Assert.Equal(3, t.CursorPosition);

        t.OnKey(new KeyEvent(Key.Right), app);
        Assert.Equal(3, t.CursorPosition);

        t.OnKey(new KeyEvent(Key.Home), app);
        Assert.Equal(0, t.CursorPosition);

        t.OnKey(new KeyEvent(Key.Left), app);
        Assert.Equal(0, t.CursorPosition);

        t.OnKey(new KeyEvent(Key.End), app);
        Assert.Equal(3, t.CursorPosition);
    }

    [Fact]
    public void Enter_fires_submit()
    {
        var fired = 0;
        var t = new TextBox("ok");
        t.Submit += () => fired++;

        t.OnKey(new KeyEvent(Key.Enter), MakeApp(t));

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Insert_respects_max_length()
    {
        var t = new TextBox { MaxLength = 2 };
        var app = MakeApp(t);

        t.OnKey(new KeyEvent(Key.Glyph, 'a'), app);
        t.OnKey(new KeyEvent(Key.Glyph, 'b'), app);
        t.OnKey(new KeyEvent(Key.Glyph, 'c'), app);

        Assert.Equal("ab", t.Text);
    }

    [Fact]
    public void TextChanged_fires_on_edits()
    {
        var fired = 0;
        var t = new TextBox();
        t.TextChanged += () => fired++;

        t.OnKey(new KeyEvent(Key.Glyph, 'a'), MakeApp(t));
        t.OnKey(new KeyEvent(Key.Backspace), MakeApp(t));

        Assert.Equal(2, fired);
    }

    [Fact]
    public void Renders_text_at_vertical_middle()
    {
        var screen = new ScreenBuffer(10, 3);
        var t = new TextBox("hi") { Bounds = new Rect(0, 0, 10, 3) };

        t.OnDraw(screen);

        Assert.Equal('h', screen[0, 1].Glyph);
        Assert.Equal('i', screen[1, 1].Glyph);
    }

    [Fact]
    public void Renders_placeholder_when_empty_and_unfocused()
    {
        var screen = new ScreenBuffer(10, 1);
        var t = new TextBox { Placeholder = "name", Bounds = new Rect(0, 0, 10, 1) };

        t.OnDraw(screen);

        Assert.Equal('n', screen[0, 0].Glyph);
        Assert.Equal('a', screen[1, 0].Glyph);
        Assert.Equal('m', screen[2, 0].Glyph);
        Assert.Equal('e', screen[3, 0].Glyph);
    }

    [Fact]
    public void Long_text_scrolls_to_keep_cursor_visible()
    {
        var screen = new ScreenBuffer(4, 1);
        var t = new TextBox("0123456789") { Bounds = new Rect(0, 0, 4, 1) };

        t.OnDraw(screen);

        Assert.Equal('7', screen[0, 0].Glyph);
        Assert.Equal('8', screen[1, 0].Glyph);
        Assert.Equal('9', screen[2, 0].Glyph);
    }

    [Fact]
    public void Mouse_press_positions_cursor_at_clicked_column()
    {
        var t = new TextBox("hello") { Bounds = new Rect(0, 0, 10, 1) };

        t.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 3, 1), MakeApp(t));

        Assert.Equal(2, t.CursorPosition);
    }

    [Fact]
    public void Mouse_press_clamps_cursor_to_text_length()
    {
        var t = new TextBox("hi") { Bounds = new Rect(0, 0, 10, 1) };

        t.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 9, 1), MakeApp(t));

        Assert.Equal(2, t.CursorPosition);
    }

    private static Application MakeApp(View root) => new(root);
}
