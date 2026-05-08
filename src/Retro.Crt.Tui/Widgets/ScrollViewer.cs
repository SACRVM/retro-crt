using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Generic vertical-scroll container. Subclasses provide
/// <see cref="ContentHeight"/> (the logical row count of whatever they
/// render) and <see cref="DrawContent"/> (paint into the content rect
/// using <see cref="ScrollOffset"/> as the first visible row).
/// <see cref="ScrollViewer"/> handles the scrollbar, the keyboard / wheel
/// scroll bindings, and the click-and-drag thumb interaction; the
/// subclass only worries about its rows.
/// </summary>
/// <remarks>
/// The viewport reserves the rightmost column for the scrollbar
/// whenever <see cref="ContentHeight"/> exceeds <c>Bounds.Height</c>.
/// When everything fits the bar disappears and the subclass gets the
/// full width. <see cref="AutoScroll"/> follows new content **only
/// while the user is pinned to the tail** (sticky-tail semantics):
/// once they scroll up to read past output, new entries no longer
/// drag the viewport away. Subclasses opt in by calling
/// <see cref="AutoScrollOnContentGrowth"/> after appending rows.
/// </remarks>
public abstract class ScrollViewer : View
{
    protected ScrollViewer() { IsFocusable = true; }

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public Color ScrollbarTrack { get; set; } = Color.DarkGray;

    public Color ScrollbarThumb { get; set; } = Color.LightGray;

    /// <summary>
    /// When true, <see cref="AutoScrollOnContentGrowth"/> follows
    /// freshly-appended content — but only while
    /// <see cref="IsPinnedToTail"/> is also true. The sticky-tail bit
    /// flips off the moment the user scrolls up; pressing <c>End</c>
    /// (or scrolling back down to the bottom) re-pins them.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    private int _scrollOffset;
    private int _dragGrabOffset;
    // Fresh viewers start pinned: the first item that arrives gets
    // followed automatically, matching `tail -f` defaults.
    private bool _pinnedToTail = true;

    /// <summary>
    /// True when the most recent scroll write left
    /// <see cref="ScrollOffset"/> at <see cref="MaxScrollOffset"/> —
    /// the user is sitting on the last row and wants new content to
    /// follow. Updated by every <see cref="ScrollOffset"/> write
    /// (user input, programmatic, or <see cref="AutoScrollOnContentGrowth"/>);
    /// content growth alone does NOT silently un-pin them.
    /// </summary>
    public bool IsPinnedToTail => _pinnedToTail;

    /// <summary>
    /// Logical row count of the rendered content. Drives the scrollbar
    /// thumb size and clamps <see cref="ScrollOffset"/>.
    /// </summary>
    public abstract int ContentHeight { get; }

    /// <summary>
    /// Index of the topmost visible row. Clamped to
    /// <c>[0, MaxScrollOffset]</c> on read; assignments outside the
    /// range are clamped silently.
    /// </summary>
    public int ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            var clamped = ClampOffset(value);
            // Pin recomputation runs even on idempotent writes: an
            // explicit `ScrollOffset = MaxScrollOffset` is the user
            // saying 'pin me to the tail' regardless of where they
            // already were.
            _pinnedToTail = clamped >= MaxScrollOffset;
            if (clamped == _scrollOffset) return;
            _scrollOffset = clamped;
            MarkDirty();
        }
    }

    public int MaxScrollOffset => Math.Max(0, ContentHeight - Math.Max(0, Bounds.Height));

    private int ClampOffset(int v) => Math.Clamp(v, 0, MaxScrollOffset);

    public void ScrollToStart() => ScrollOffset = 0;
    public void ScrollToEnd()   => ScrollOffset = MaxScrollOffset;
    public void ScrollBy(int delta) => ScrollOffset = _scrollOffset + delta;
    public void PageUp()   => ScrollBy(-Math.Max(1, Bounds.Height));
    public void PageDown() => ScrollBy( Math.Max(1, Bounds.Height));

    /// <summary>
    /// Subclass hook for log-pane-style 'follow the tail when fresh
    /// rows arrive'. No-op when <see cref="AutoScroll"/> is off OR the
    /// user has scrolled up (<see cref="IsPinnedToTail"/> is false).
    /// Otherwise jumps to <see cref="MaxScrollOffset"/> so the new
    /// last row is on screen. Call after the underlying content
    /// (<see cref="ContentHeight"/>) has grown.
    /// </summary>
    protected void AutoScrollOnContentGrowth()
    {
        if (!AutoScroll || !_pinnedToTail) return;
        ScrollOffset = MaxScrollOffset;
    }

    /// <summary>
    /// Paint the visible rows into <paramref name="content"/>.
    /// <paramref name="content"/> already excludes the scrollbar
    /// column. Subclasses read <see cref="ScrollOffset"/> to know which
    /// row maps to the top of the rect.
    /// </summary>
    protected abstract void DrawContent(ScreenBuffer screen, Rect content);

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        // Re-clamp now that bounds are known. Subclasses (or callers)
        // can mutate ContentHeight or ScrollOffset before layout has
        // set the height — e.g., AutoScroll-on-append before Run().
        if (_scrollOffset > MaxScrollOffset) _scrollOffset = MaxScrollOffset;
        if (_scrollOffset < 0)               _scrollOffset = 0;

        var hasScrollbar = ContentHeight > b.Height;
        var contentWidth = hasScrollbar ? b.Width - 1 : b.Width;
        if (contentWidth <= 0) return;

        screen.FillRect(b.X, b.Y, contentWidth, b.Height,
            new Cell(' ', Foreground, Background));

        var contentRect = new Rect(b.X, b.Y, contentWidth, b.Height);
        DrawContent(screen, contentRect);

        if (!hasScrollbar) return;

        var sx = b.X + b.Width - 1;
        screen.FillRect(sx, b.Y, 1, b.Height,
            new Cell('░', ScrollbarTrack, Background));

        // Thumb size proportional to viewport / total, ≥1. Position
        // proportional to scroll progress.
        var thumbSize = Math.Max(1, b.Height * b.Height / ContentHeight);
        var maxThumbY = b.Height - thumbSize;
        var thumbY    = MaxScrollOffset > 0
            ? maxThumbY * _scrollOffset / MaxScrollOffset
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

        if (mouse.Kind != MouseEventKind.Press &&
            mouse.Kind != MouseEventKind.Drag) return;
        if (mouse.Button != MouseButton.Left) return;

        var b = Bounds;
        if (ContentHeight <= b.Height || b.Height <= 0) return;

        var sx     = b.X + b.Width - 1;   // 0-based scrollbar column
        var mouseX = mouse.X - 1;         // 0-based cursor column
        var mouseY = mouse.Y - 1;

        // Press requires landing in the rightmost two cells (1-cell
        // tolerance). Drag (under capture) is unconditional so the
        // cursor can drift off the track without losing grip.
        var inTrackZone = mouseX >= sx - 1 && mouseX <= sx;
        if (mouse.Kind == MouseEventKind.Press && !inTrackZone) return;

        var thumbSize = Math.Max(1, b.Height * b.Height / ContentHeight);
        var maxThumbY = b.Height - thumbSize;
        if (maxThumbY <= 0) { ScrollOffset = MaxScrollOffset; return; }

        var localY    = mouseY - b.Y;
        var maxOffset = MaxScrollOffset;

        if (mouse.Kind == MouseEventKind.Press)
        {
            // Click on the thumb: remember the relative grip so drag
            // keeps the same offset. Click off the thumb: snap thumb
            // top to cursor (grip = 0).
            var currentThumbY = maxOffset > 0
                ? (int)((long)maxThumbY * _scrollOffset / maxOffset)
                : 0;
            _dragGrabOffset = (localY >= currentThumbY && localY < currentThumbY + thumbSize)
                ? localY - currentThumbY
                : 0;
        }

        var newThumbY = Math.Clamp(localY - _dragGrabOffset, 0, maxThumbY);
        ScrollOffset = (int)((long)maxOffset * newThumbY / maxThumbY);
    }
}
