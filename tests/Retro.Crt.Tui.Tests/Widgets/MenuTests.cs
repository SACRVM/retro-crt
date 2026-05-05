using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class MenuTests
{
    [Fact]
    public void Defaults_to_focusable()
    {
        Assert.True(new Menu().IsFocusable);
    }

    [Fact]
    public void Down_advances_selection_and_wraps()
    {
        var m = new Menu { Items = { new("a"), new("b"), new("c") } };

        m.OnKey(new KeyEvent(Key.Down), MakeApp(m));
        Assert.Equal(1, m.SelectedIndex);

        m.OnKey(new KeyEvent(Key.Down), MakeApp(m));
        Assert.Equal(2, m.SelectedIndex);

        m.OnKey(new KeyEvent(Key.Down), MakeApp(m));
        Assert.Equal(0, m.SelectedIndex);
    }

    [Fact]
    public void Up_retreats_selection_and_wraps()
    {
        var m = new Menu { Items = { new("a"), new("b"), new("c") } };

        m.OnKey(new KeyEvent(Key.Up), MakeApp(m));
        Assert.Equal(2, m.SelectedIndex);
    }

    [Fact]
    public void Disabled_items_are_skipped_by_arrows()
    {
        var m = new Menu { Items =
        {
            new("a"),
            new("b", IsEnabled: false),
            new("c"),
        } };

        m.OnKey(new KeyEvent(Key.Down), MakeApp(m));
        Assert.Equal(2, m.SelectedIndex);

        m.OnKey(new KeyEvent(Key.Up), MakeApp(m));
        Assert.Equal(0, m.SelectedIndex);
    }

    [Fact]
    public void Enter_runs_selected_action()
    {
        var hit = 0;
        var m = new Menu { Items = { new("a", () => hit++), new("b") } };

        m.OnKey(new KeyEvent(Key.Enter), MakeApp(m));

        Assert.Equal(1, hit);
    }

    [Fact]
    public void Disabled_selected_item_does_not_activate()
    {
        var hit = 0;
        var m = new Menu { Items = { new("a", () => hit++, IsEnabled: false) } };

        m.OnKey(new KeyEvent(Key.Enter), MakeApp(m));

        Assert.Equal(0, hit);
    }

    [Fact]
    public void SelectionChanged_fires_on_navigation()
    {
        var changes = 0;
        var m = new Menu { Items = { new("a"), new("b") } };
        m.SelectionChanged += () => changes++;

        m.OnKey(new KeyEvent(Key.Down), MakeApp(m));

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Mouse_click_selects_and_activates()
    {
        var hit = 0;
        var m = new Menu
        {
            Bounds = new Rect(0, 0, 10, 5),
            Items  = { new("a"), new("b", () => hit++), new("c") },
        };

        m.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 5, 2), MakeApp(m));

        Assert.Equal(1, m.SelectedIndex);
        Assert.Equal(1, hit);
    }

    [Fact]
    public void Mouse_click_on_disabled_row_is_noop()
    {
        var hit = 0;
        var m = new Menu
        {
            Bounds = new Rect(0, 0, 10, 5),
            Items  = { new("a"), new("b", () => hit++, IsEnabled: false) },
        };

        m.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 5, 2), MakeApp(m));

        Assert.Equal(0, m.SelectedIndex);
        Assert.Equal(0, hit);
    }

    [Fact]
    public void Renders_each_label_one_row_per_item()
    {
        var screen = new ScreenBuffer(10, 3);
        var m = new Menu
        {
            Bounds = new Rect(0, 0, 10, 3),
            Items  = { new("aa"), new("bb"), new("cc") },
        };

        m.OnDraw(screen);

        Assert.Equal('a', screen[1, 0].Glyph);
        Assert.Equal('b', screen[1, 1].Glyph);
        Assert.Equal('c', screen[1, 2].Glyph);
    }

    private static Application MakeApp(View root) => new(root);
}
