using Retro.Crt.Input;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// One row in a <see cref="LogViewer"/>. <see cref="Foreground"/> is
/// optional; <c>null</c> means "use the viewer's default".
/// </summary>
public readonly record struct LogEntry(string Text, Color? Foreground = null);

/// <summary>
/// Vertically scrollable list of text rows with a thin track-and-thumb
/// scrollbar on the right edge. Focusable; arrow keys / Page / Home /
/// End scroll the viewport, the mouse wheel scrolls three rows at a
/// time. <see cref="AutoScroll"/> follows new entries as they arrive —
/// the keystone widget for log panes, REPL output, and chat-style
/// histories.
/// </summary>
/// <remarks>
/// v1 has no per-row selection and no filter predicate; both can land
/// in a follow-up once a real consumer asks. Items are stored as a
/// plain <see cref="IList{T}"/> so callers can manipulate them
/// directly when needed — call <see cref="View.MarkDirty"/> after
/// out-of-band edits.
/// </remarks>
public class LogViewer : View
{
    public LogViewer() { IsFocusable = true; }

    public IList<LogEntry> Items { get; } = new List<LogEntry>();

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public Color ScrollbarTrack { get; set; } = Color.DarkGray;

    public Color ScrollbarThumb { get; set; } = Color.LightGray;

    /// <summary>
    /// When true, <see cref="Append(LogEntry)"/> calls jump the
    /// viewport to the bottom so new entries stay visible. Defaults to
    /// <c>true</c> — typical log-pane behavior.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    private int _scrollOffset;
    private int _dragGrabOffset;

    /// <summary>
    /// Index of the topmost item currently visible. Clamped to the
    /// valid range on read; assignments outside the range are clamped
    /// silently.
    /// </summary>
    public int ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            var clamped = ClampOffset(value);
            if (clamped == _scrollOffset) return;
            _scrollOffset = clamped;
            MarkDirty();
        }
    }

    public void Append(LogEntry entry)
    {
        Items.Add(entry);
        if (AutoScroll) ScrollToEnd();
        MarkDirty();
    }

    public void Append(string text, Color? foreground = null)
        => Append(new LogEntry(text, foreground));

    public void Clear()
    {
        Items.Clear();
        _scrollOffset = 0;
        MarkDirty();
    }

    public void ScrollToStart() => ScrollOffset = 0;
    public void ScrollToEnd()   => ScrollOffset = MaxOffset;

    public void ScrollBy(int delta) => ScrollOffset = _scrollOffset + delta;

    public void PageUp()   => ScrollBy(-Math.Max(1, Bounds.Height));
    public void PageDown() => ScrollBy( Math.Max(1, Bounds.Height));

    private int MaxOffset => Math.Max(0, Items.Count - Math.Max(0, Bounds.Height));

    private int ClampOffset(int value) => Math.Clamp(value, 0, MaxOffset);

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        // Re-clamp now that bounds are known. Append + ScrollOffset set
        // run before layout knows the height, so a value bigger than
        // MaxOffset can sneak in (e.g., AutoScroll on first append
        // before Run() set the bounds).
        if (_scrollOffset > MaxOffset) _scrollOffset = MaxOffset;
        if (_scrollOffset < 0)         _scrollOffset = 0;

        var hasScrollbar = Items.Count > b.Height;
        var contentWidth = hasScrollbar ? b.Width - 1 : b.Width;
        if (contentWidth <= 0) return;

        screen.FillRect(b.X, b.Y, contentWidth, b.Height,
            new Cell(' ', Foreground, Background));

        var rows = Math.Min(b.Height, Items.Count - _scrollOffset);
        for (var row = 0; row < rows; row++)
        {
            var item = Items[_scrollOffset + row];
            var fg   = item.Foreground ?? Foreground;
            var text = item.Text ?? string.Empty;
            screen.PutString(b.X, b.Y + row, text.AsSpan(), fg, Background);
        }

        if (!hasScrollbar) return;

        var sx = b.X + b.Width - 1;
        screen.FillRect(sx, b.Y, 1, b.Height,
            new Cell('░', ScrollbarTrack, Background));

        // Thumb size is proportional to viewport / total, clamped to
        // ≥1 cell. Position is proportional to scroll progress.
        var thumbSize = Math.Max(1, b.Height * b.Height / Items.Count);
        var maxThumbY = b.Height - thumbSize;
        var thumbY    = MaxOffset > 0
            ? maxThumbY * _scrollOffset / MaxOffset
            : 0;
        for (var i = 0; i < thumbSize; i++)
            screen[sx, b.Y + thumbY + i] = new Cell('█', ScrollbarThumb, Background);
    }

    public override bool OnKey(KeyEvent key, Application app)
    {
        switch (key.Key)
        {
            case Key.Up:       ScrollBy(-1);    return true;
            case Key.Down:     ScrollBy( 1);    return true;
            case Key.PageUp:   PageUp();        return true;
            case Key.PageDown: PageDown();      return true;
            case Key.Home:     ScrollToStart(); return true;
            case Key.End:      ScrollToEnd();   return true;
        }
        return false;
    }

    public override void OnMouse(MouseEvent mouse, Application app)
    {
        if (mouse.Kind == MouseEventKind.Wheel)
        {
            switch (mouse.Button)
            {
                case MouseButton.WheelUp:   ScrollBy(-3); break;
                case MouseButton.WheelDown: ScrollBy( 3); break;
            }
            return;
        }

        // Scrollbar interaction — Press anywhere on the track jumps
        // there; Drag continues. Both rely on Application's mouse
        // capture so the cursor straying off the track keeps
        // scrolling.
        if (mouse.Kind != MouseEventKind.Press &&
            mouse.Kind != MouseEventKind.Drag) return;
        if (mouse.Button != MouseButton.Left) return;

        var b = Bounds;
        if (Items.Count <= b.Height || b.Height <= 0) return;

        var sx = b.X + b.Width - 1;       // 0-based scrollbar column
        var mouseX = mouse.X - 1;         // 0-based cursor column
        var mouseY = mouse.Y - 1;         // 0-based cursor row

        // Press is acted on when it lands in the rightmost two cells
        // — a one-cell tolerance for users aiming at the visible
        // track. Drag (which runs under capture) updates regardless
        // of horizontal position so the cursor can drift away
        // without losing grip.
        var inTrackZone = mouseX >= sx - 1 && mouseX <= sx;
        if (mouse.Kind == MouseEventKind.Press && !inTrackZone) return;

        var thumbSize = Math.Max(1, b.Height * b.Height / Items.Count);
        var maxThumbY = b.Height - thumbSize;
        if (maxThumbY <= 0) { ScrollOffset = MaxOffset; return; }

        var localY = mouseY - b.Y;
        var maxOffset = MaxOffset;

        if (mouse.Kind == MouseEventKind.Press)
        {
            // If the click lands on the thumb, remember where inside
            // the thumb the user grabbed; subsequent drags keep that
            // relative grip. A click off the thumb behaves like
            // "snap thumb top to cursor."
            var currentThumbY = maxOffset > 0
                ? (int)((long)maxThumbY * _scrollOffset / maxOffset)
                : 0;
            _dragGrabOffset = (localY >= currentThumbY && localY < currentThumbY + thumbSize)
                ? localY - currentThumbY
                : 0;
        }

        // Map the (cursor - grab offset) row directly onto the thumb's
        // travel range. 1 cell of cursor movement = 1 cell of thumb
        // movement, regardless of how big the visible thumb is.
        var newThumbY = Math.Clamp(localY - _dragGrabOffset, 0, maxThumbY);
        ScrollOffset = (int)((long)maxOffset * newThumbY / maxThumbY);
    }
}
