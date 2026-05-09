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

    [Fact]
    public void Sticky_tail_holds_viewport_after_user_scrolls_up()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        var app = new Application(v);

        // Fill enough to overflow so the viewport actually scrolls.
        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        Assert.True(v.IsPinnedToTail);
        Assert.Equal(7, v.ScrollOffset);

        // User pages up to read past output.
        v.OnKey(new KeyEvent(Key.PageUp), app);
        Assert.False(v.IsPinnedToTail);
        var savedOffset = v.ScrollOffset;

        // Fresh entries arrive while user is reading. They should NOT
        // drag the viewport away.
        for (var i = 10; i < 20; i++) v.Append($"L{i}");

        Assert.Equal(savedOffset, v.ScrollOffset);
        Assert.False(v.IsPinnedToTail);
    }

    [Fact]
    public void Pressing_End_re_pins_user_to_live_tail()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        var app = new Application(v);

        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        v.OnKey(new KeyEvent(Key.PageUp), app);
        Assert.False(v.IsPinnedToTail);

        // Append one while unpinned — viewport stays put.
        v.Append("L10");
        Assert.False(v.IsPinnedToTail);

        // User presses End → re-pins.
        v.OnKey(new KeyEvent(Key.End), app);
        Assert.True(v.IsPinnedToTail);

        // Now appended entries follow again.
        v.Append("L11");
        Assert.True(v.IsPinnedToTail);
        Assert.Equal(v.MaxScrollOffset, v.ScrollOffset);
    }

    [Fact]
    public void Manually_scrolling_back_to_bottom_re_pins()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        var app = new Application(v);

        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        // Scroll up two rows: pin breaks.
        v.OnKey(new KeyEvent(Key.Up), app);
        v.OnKey(new KeyEvent(Key.Up), app);
        Assert.False(v.IsPinnedToTail);

        // Walk back down to the bottom one row at a time.
        v.OnKey(new KeyEvent(Key.Down), app);
        Assert.False(v.IsPinnedToTail);
        v.OnKey(new KeyEvent(Key.Down), app);
        Assert.True(v.IsPinnedToTail);

        // Subsequent appends follow again.
        v.Append("L10");
        Assert.True(v.IsPinnedToTail);
    }

    [Fact]
    public void Wheel_up_off_the_tail_breaks_pin_and_freezes_viewport()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        var app = new Application(v);

        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        Assert.True(v.IsPinnedToTail);

        v.OnMouse(new MouseEvent(MouseButton.WheelUp, MouseEventKind.Wheel, 1, 1), app);
        Assert.False(v.IsPinnedToTail);
        var saved = v.ScrollOffset;

        for (var i = 20; i < 25; i++) v.Append($"L{i}");
        Assert.Equal(saved, v.ScrollOffset);
    }

    [Fact]
    public void Drag_to_bottom_of_track_re_pins()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 4) };
        var app = new Application(v);

        for (var i = 0; i < 20; i++) v.Append($"L{i}");
        v.OnKey(new KeyEvent(Key.PageUp), app);
        Assert.False(v.IsPinnedToTail);

        // Click bottom of scrollbar (column 9, row 3 → localY = 3,
        // 100% of MaxOffset).
        v.OnMouse(
            new MouseEvent(MouseButton.Left, MouseEventKind.Press, 10, 4),
            app);

        Assert.True(v.IsPinnedToTail);
        Assert.Equal(v.MaxScrollOffset, v.ScrollOffset);
    }

    [Fact]
    public void Empty_viewer_starts_pinned_so_first_entry_is_visible()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        Assert.True(v.IsPinnedToTail);

        v.Append("first");
        Assert.True(v.IsPinnedToTail);
        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void AutoScroll_off_does_not_follow_even_when_pinned()
    {
        var v = new LogViewer
        {
            Bounds     = new Rect(0, 0, 10, 3),
            AutoScroll = false,
        };
        Assert.True(v.IsPinnedToTail);

        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        Assert.Equal(0, v.ScrollOffset);
    }

    [Fact]
    public void UpdateLast_on_empty_viewer_is_a_noop()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };

        v.UpdateLast("nothing");

        Assert.Empty(v.Items);
    }

    [Fact]
    public void UpdateLast_rewrites_tail_text_in_place()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        v.Append("first");
        v.Append("second");

        v.UpdateLast("second-edited");

        Assert.Equal(2, v.Items.Count);
        Assert.Equal("first",          v.Items[0].Text);
        Assert.Equal("second-edited",  v.Items[1].Text);
    }

    [Fact]
    public void UpdateLast_respects_foreground_override()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        v.Append("plain");

        v.UpdateLast("colored", Color.LightRed);

        Assert.Equal(Color.LightRed, v.Items[0].Foreground);
    }

    [Fact]
    public void UpdateLast_does_not_grow_content()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        // Pin scroll mid-content.
        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        v.OnKey(new KeyEvent(Key.Up), new Application(v));
        var beforeOffset = v.ScrollOffset;
        var beforeMax    = v.MaxScrollOffset;

        v.UpdateLast("rewritten");

        // ContentHeight didn't change → MaxScrollOffset unchanged →
        // user's saved offset preserved.
        Assert.Equal(beforeMax,    v.MaxScrollOffset);
        Assert.Equal(beforeOffset, v.ScrollOffset);
    }

    [Fact]
    public void UpdateLast_marks_view_dirty()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        v.Append("x");
        var screen = new ScreenBuffer(10, 3);
        v.OnDraw(screen);
        // ClearDirty isn't part of the public API, but a draw isn't
        // automatic — just check dirty toggles when content changes.
        v.UpdateLast("y");
        Assert.True(v.IsDirty);
    }

    [Fact]
    public void UpdateLast_followed_by_Append_creates_a_new_line()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        v.Append("downloading...");

        // Mutate the in-place line a few times, then "complete" with a
        // fresh entry below — this is the spinner-driver workflow.
        v.UpdateLast("downloading 25%");
        v.UpdateLast("downloading 50%");
        v.UpdateLast("downloading 100%");
        v.Append("done");

        Assert.Equal(2, v.Items.Count);
        Assert.Equal("downloading 100%", v.Items[0].Text);
        Assert.Equal("done",             v.Items[1].Text);
    }

    [Fact]
    public void MaxItems_defaults_to_zero_meaning_unbounded()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };
        Assert.Equal(0, v.MaxItems);

        for (var i = 0; i < 1000; i++) v.Append($"L{i}");
        Assert.Equal(1000, v.Items.Count);
    }

    [Fact]
    public void MaxItems_caps_count_and_drops_oldest()
    {
        var v = new LogViewer
        {
            Bounds   = new Rect(0, 0, 10, 3),
            MaxItems = 5,
        };

        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        Assert.Equal(5, v.Items.Count);
        // Newest five survive; head of the list is the 5th-newest.
        Assert.Equal("L5", v.Items[0].Text);
        Assert.Equal("L9", v.Items[4].Text);
    }

    [Fact]
    public void MaxItems_keeps_sticky_tail_pinned_across_trims()
    {
        var v = new LogViewer
        {
            Bounds   = new Rect(0, 0, 10, 3),
            MaxItems = 5,
        };

        for (var i = 0; i < 10; i++) v.Append($"L{i}");

        // ContentHeight = 5, viewport = 3 → MaxScrollOffset = 2.
        Assert.True(v.IsPinnedToTail);
        Assert.Equal(v.MaxScrollOffset, v.ScrollOffset);
    }

    [Fact]
    public void Lowering_MaxItems_does_not_retroactively_trim()
    {
        var v = new LogViewer { Bounds = new Rect(0, 0, 10, 3) };

        for (var i = 0; i < 10; i++) v.Append($"L{i}");
        Assert.Equal(10, v.Items.Count);

        v.MaxItems = 3;
        Assert.Equal(10, v.Items.Count);   // no reactive trim

        v.Append("L10");
        Assert.Equal(3, v.Items.Count);    // trim fires on next Append
        Assert.Equal("L8",  v.Items[0].Text);
        Assert.Equal("L10", v.Items[2].Text);
    }

    [Fact]
    public void MaxItems_one_keeps_only_the_latest_entry()
    {
        var v = new LogViewer
        {
            Bounds   = new Rect(0, 0, 10, 3),
            MaxItems = 1,
        };

        v.Append("a");
        v.Append("b");
        v.Append("c");

        Assert.Single(v.Items);
        Assert.Equal("c", v.Items[0].Text);
    }

    [Fact]
    public void MaxItems_does_not_trim_below_count_via_UpdateLast()
    {
        // UpdateLast doesn't grow the list, so it never triggers trim.
        // The active in-place tail entry is therefore safe — it lives
        // at Items[^1] and only the head is ever dropped.
        var v = new LogViewer
        {
            Bounds   = new Rect(0, 0, 10, 3),
            MaxItems = 3,
        };

        for (var i = 0; i < 5; i++) v.Append($"L{i}");
        Assert.Equal(3, v.Items.Count);

        v.UpdateLast("rewritten");
        Assert.Equal(3,            v.Items.Count);
        Assert.Equal("rewritten",  v.Items[2].Text);
    }
}
