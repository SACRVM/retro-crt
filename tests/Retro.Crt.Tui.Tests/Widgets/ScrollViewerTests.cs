using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class ScrollViewerTests
{
    [Fact]
    public void ScrollOffset_clamps_to_max()
    {
        var v = new TestScroll(contentHeight: 100) { Bounds = new Rect(0, 0, 10, 10) };
        v.ScrollOffset = 999;
        Assert.Equal(90, v.ScrollOffset);

        v.ScrollOffset = -5;
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Down_arrow_scrolls_one_row_and_returns_handled()
    {
        var v = new TestScroll(contentHeight: 100) { Bounds = new Rect(0, 0, 10, 10) };
        var app = new Application(v);

        var handled = v.OnKey(new KeyEvent(Key.Down), app);

        Assert.True(handled);
        Assert.Equal(1, v.ScrollOffset);
    }

    [Fact]
    public void Wheel_down_scrolls_three_rows()
    {
        var v = new TestScroll(contentHeight: 100) { Bounds = new Rect(0, 0, 10, 10) };
        var app = new Application(v);

        v.OnMouse(new MouseEvent(MouseButton.WheelDown, MouseEventKind.Wheel, 5, 5), app);

        Assert.Equal(3, v.ScrollOffset);
    }

    [Fact]
    public void DrawContent_receives_scrollbar_excluded_rect_when_overflowing()
    {
        var v = new TestScroll(contentHeight: 100) { Bounds = new Rect(0, 0, 10, 10) };
        var screen = new ScreenBuffer(20, 20);
        v.OnDraw(screen);

        Assert.NotNull(v.LastContentRect);
        Assert.Equal(9, v.LastContentRect!.Value.Width); // 10 - scrollbar
    }

    [Fact]
    public void DrawContent_receives_full_width_when_content_fits()
    {
        var v = new TestScroll(contentHeight: 5) { Bounds = new Rect(0, 0, 10, 10) };
        var screen = new ScreenBuffer(20, 20);
        v.OnDraw(screen);

        Assert.NotNull(v.LastContentRect);
        Assert.Equal(10, v.LastContentRect!.Value.Width);
    }

    private sealed class TestScroll : ScrollViewer
    {
        private readonly int _contentHeight;
        public Rect? LastContentRect;
        public TestScroll(int contentHeight) { _contentHeight = contentHeight; }
        public override int ContentHeight => _contentHeight;
        protected override void DrawContent(ScreenBuffer screen, Rect content)
            => LastContentRect = content;
    }
}
