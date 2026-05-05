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

    public override void OnKey(KeyEvent key, Application app)
    {
        switch (key.Key)
        {
            case Key.Up:       ScrollBy(-1); break;
            case Key.Down:     ScrollBy( 1); break;
            case Key.PageUp:   PageUp();     break;
            case Key.PageDown: PageDown();   break;
            case Key.Home:     ScrollToStart(); break;
            case Key.End:      ScrollToEnd();   break;
        }
    }

    public override void OnMouse(MouseEvent mouse, Application app)
    {
        if (mouse.Kind != MouseEventKind.Wheel) return;
        switch (mouse.Button)
        {
            case MouseButton.WheelUp:   ScrollBy(-3); break;
            case MouseButton.WheelDown: ScrollBy( 3); break;
        }
    }
}
