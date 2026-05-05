using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class ButtonTests
{
    [Fact]
    public void Defaults_to_focusable()
    {
        Assert.True(new Button().IsFocusable);
    }

    [Fact]
    public void Renders_brackets_and_label()
    {
        var screen = new ScreenBuffer(12, 1);
        var b = new Button("Quit") { Bounds = new Rect(0, 0, 12, 1) };

        b.OnDraw(screen);

        Assert.Equal('[', screen[0,  0].Glyph);
        Assert.Equal(']', screen[11, 0].Glyph);
        // " Quit " centered in width 12 → label starts at col 4.
        Assert.Equal('Q', screen[4, 0].Glyph);
        Assert.Equal('u', screen[5, 0].Glyph);
        Assert.Equal('i', screen[6, 0].Glyph);
        Assert.Equal('t', screen[7, 0].Glyph);
    }

    [Fact]
    public void Enter_invokes_click()
    {
        var clicked = 0;
        var b = new Button("Go", () => clicked++);

        b.OnKey(new KeyEvent(Key.Enter), MakeApp(b));

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Space_invokes_click()
    {
        var clicked = 0;
        var b = new Button("Go", () => clicked++);

        b.OnKey(new KeyEvent(Key.Glyph, ' '), MakeApp(b));

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Other_keys_do_not_invoke_click()
    {
        var clicked = 0;
        var b = new Button("Go", () => clicked++);

        b.OnKey(new KeyEvent(Key.Glyph, 'x'), MakeApp(b));
        b.OnKey(new KeyEvent(Key.Tab), MakeApp(b));
        b.OnKey(new KeyEvent(Key.Up), MakeApp(b));

        Assert.Equal(0, clicked);
    }

    [Fact]
    public void Mouse_press_invokes_click()
    {
        var clicked = 0;
        var b = new Button("Go", () => clicked++);

        b.OnMouse(
            new MouseEvent(MouseButton.Left, MouseEventKind.Press, 1, 1),
            MakeApp(b));

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Mouse_release_does_not_invoke_click()
    {
        var clicked = 0;
        var b = new Button("Go", () => clicked++);

        b.OnMouse(
            new MouseEvent(MouseButton.Left, MouseEventKind.Release, 1, 1),
            MakeApp(b));

        Assert.Equal(0, clicked);
    }

    private static Application MakeApp(View root) => new(root);
}
