using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class LogViewerTests
{
    [Fact]
    public void Defaults_to_focusable_with_auto_scroll()
    {
        var v = new LogViewer();
        Assert.True(v.IsFocusable);
        Assert.True(v.AutoScroll);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Append_with_auto_scroll_keeps_tail_visible()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        var screen = new ScreenBuffer(10, 3);

        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        v.OnDraw(screen);

        // 10 entries, 3 rows of viewport → top index = 7.
        Assert.Equal(7, v.ScrollOffset);
        Assert.Equal('L', screen[0, 0].Glyph);
        Assert.Equal('7', screen[1, 0].Glyph);
        Assert.Equal('9', screen[1, 2].Glyph);
    }

    [Fact]
    public void Append_without_auto_scroll_leaves_offset_alone()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };

        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void ScrollOffset_setter_clamps_to_valid_range()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        v.ScrollOffset = 999;
        Assert.Equal(7, v.ScrollOffset);

        v.ScrollOffset = -5;
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Up_and_Down_keys_move_one_row()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        var app = new Application(v);

        v.OnKey(new KeyEvent(Key.Down), app);
        Assert.Equal(1, v.ScrollOffset);

        v.OnKey(new KeyEvent(Key.Up), app);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void PageDown_moves_a_full_viewport_height()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        var app = new Application(v);

        v.OnKey(new KeyEvent(Key.PageDown), app);
        Assert.Equal(3, v.ScrollOffset);

        v.OnKey(new KeyEvent(Key.PageDown), app);
        Assert.Equal(6, v.ScrollOffset);
    }

    [Fact]
    public void Home_and_End_jump_to_extents()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        var app = new Application(v);

        v.OnKey(new KeyEvent(Key.End), app);
        Assert.Equal(7, v.ScrollOffset);

        v.OnKey(new KeyEvent(Key.Home), app);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Wheel_up_and_down_scroll_three_rows()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        var app = new Application(v);

        v.OnMouse(new MouseEvent(MouseButton.WheelDown, MouseEventKind.Wheel, 1, 1), app);
        Assert.Equal(3, v.ScrollOffset);

        v.OnMouse(new MouseEvent(MouseButton.WheelUp, MouseEventKind.Wheel, 1, 1), app);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Scrollbar_appears_only_when_overflow()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 4), AutoScroll = false };
        var screen = new ScreenBuffer(10, 4);

        for (var i = 0; i < 4; i++) v.Append($"L{i}");
        v.OnDraw(screen);
        // 4 items in 4 rows — no scrollbar; rightmost column is content.
        Assert.NotEqual('░', screen[9, 0].Glyph);

        v.Append("L4");
        v.OnDraw(screen);
        // Now overflowing — track visible. Thumb at top because we scrolled
        // to end (auto), but here AutoScroll is false → still at top.
        var glyph = screen[9, 0].Glyph;
        Assert.True(glyph == '░' || glyph == '█',
            $"expected scrollbar glyph, got '{glyph}'");
    }

    [Fact]
    public void Clear_resets_scroll_and_items()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        v.Clear();

        Assert.Empty(v.Items);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void Press_on_scrollbar_jumps_to_proportional_offset()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 4),
            AutoScroll = false,
        };
        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        var app = new Application(v);

        // Click on bottom of scrollbar track at column 9, row 3.
        // localY = 3, height-1 = 3 → 100% of MaxOffset.
        v.OnMouse(
            new MouseEvent(MouseButton.Left, MouseEventKind.Press, 10, 4),
            app);

        Assert.Equal(16, v.ScrollOffset); // MaxOffset = 20 - 4 = 16
    }

    [Fact]
    public void Drag_tracks_cursor_one_to_one_within_thumb_travel()
    {
        // 100 items, 10 rows → thumbSize = 100/100 = 1, maxThumbY = 9.
        // Dragging from cursor row 0 to row 9 should walk the full
        // ScrollOffset range (0 .. 90), 1 cell of cursor = 1 cell of
        // thumb top. Earlier math (cursor / (height - 1)) made the
        // user travel ~3× the thumb distance.
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 10),
            AutoScroll = false,
        };
        for (var i = 0; i < 100; i++) v.Append($"L{i}");
        var app = new Application(v);

        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 10, 1), app);
        Assert.Equal(0, v.ScrollOffset);

        // Drag cursor down by 1 row → thumb top moves 1 row → ScrollOffset
        // walks 1 step of the 9-step thumb-travel range, i.e. ~10 of 90.
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Drag, 10, 2), app);
        Assert.Equal(10, v.ScrollOffset);

        // Drag to bottom of track → max offset.
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Drag, 10, 10), app);
        Assert.Equal(90, v.ScrollOffset);
    }

    [Fact]
    public void Press_on_thumb_keeps_grab_offset_during_drag()
    {
        // 20 items, 4 rows → thumbSize=4*4/20=0→1, maxThumbY=3.
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 4),
            AutoScroll = false,
        };
        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        var app = new Application(v);

        // Park scroll at offset 8 → thumbY = 3*8/16 ≈ 1.
        v.ScrollOffset = 8;
        Assert.Equal(8, v.ScrollOffset);

        // Press exactly on the thumb — grab offset = 0 (thumbSize=1).
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 10, 2), app);
        // Drag down by 1 row → thumbY = 2 → ScrollOffset ≈ 16*2/3 ≈ 10.
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Drag, 10, 3), app);

        Assert.InRange(v.ScrollOffset, 9, 11);
    }

    [Fact]
    public void Drag_continues_scrolling_off_track_horizontally()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 4),
            AutoScroll = false,
        };
        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        var app = new Application(v);

        // Press on track first to satisfy "in track column" check.
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Press, 10, 1), app);
        // Drag wanders left of the track but still updates.
        v.OnMouse(new MouseEvent(MouseButton.Left, MouseEventKind.Drag, 1, 4), app);

        Assert.Equal(16, v.ScrollOffset); // bottom row → max offset
    }

    [Fact]
    public void Per_entry_foreground_overrides_default()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 1),
            Foreground = Color.LightGray,
        };
        var screen = new ScreenBuffer(10, 1);

        v.Append("R", Color.LightRed);
        v.OnDraw(screen);

        Assert.Equal(Color.LightRed, screen[0, 0].Fg);
    }
}
