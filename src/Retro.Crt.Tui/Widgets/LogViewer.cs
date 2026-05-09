using Retro.Crt.Tui.Layout;

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
/// time. <see cref="ScrollViewer.AutoScroll"/> follows new entries
/// while the user is pinned to the tail
/// (<see cref="ScrollViewer.IsPinnedToTail"/>); once they scroll up
/// to read past output, fresh rows append silently behind them.
/// Pressing <c>End</c> re-pins to live tail. Keystone widget for log
/// panes, REPL output, and chat-style histories.
/// </summary>
/// <remarks>
/// v1 has no per-row selection and no filter predicate; both can land
/// in a follow-up once a real consumer asks. Items are stored as a
/// plain <see cref="IList{T}"/> so callers can manipulate them
/// directly when needed — call <see cref="View.MarkDirty"/> after
/// out-of-band edits.
/// </remarks>
public class LogViewer : ScrollViewer
{
    public IList<LogEntry> Items { get; } = new List<LogEntry>();

    public override int ContentHeight => Items.Count;

    /// <summary>
    /// Maximum number of entries kept in <see cref="Items"/>. When
    /// <see cref="Append(Retro.Crt.Tui.Widgets.LogEntry)"/> would push
    /// the count past this cap, the oldest entries are dropped (head
    /// of the list) until the cap holds. Default <c>0</c> = unbounded
    /// (the historic behaviour).
    /// </summary>
    /// <remarks>
    /// Trim only fires on <see cref="Append(Retro.Crt.Tui.Widgets.LogEntry)"/> —
    /// lowering <see cref="MaxItems"/> on a viewer that already exceeds
    /// it does NOT retroactively shrink the list. Call
    /// <see cref="Clear"/> if you want an immediate flush. Sticky-tail
    /// is preserved across trims: a pinned viewport stays pinned to
    /// the new (lower) <see cref="ScrollViewer.MaxScrollOffset"/>, so
    /// fresh entries keep following the tail without the user dropping
    /// off-pin.
    /// </remarks>
    public int MaxItems { get; set; }

    public void Append(LogEntry entry)
    {
        Items.Add(entry);
        TrimToMaxItems();
        AutoScrollOnContentGrowth();
        MarkDirty();
    }

    private void TrimToMaxItems()
    {
        if (MaxItems <= 0) return;
        while (Items.Count > MaxItems)
            Items.RemoveAt(0);
    }

    public void Append(string text, Color? foreground = null)
        => Append(new LogEntry(text, foreground));

    /// <summary>
    /// Replace the last entry's text (and optionally its foreground
    /// color) without growing the list. No-op when <see cref="Items"/>
    /// is empty. <see cref="ScrollViewer.ContentHeight"/> stays the
    /// same, so the viewport does not auto-follow — use this for
    /// in-place tail rewrites like spinner frames, download bars, or
    /// graceful-shutdown countdowns. The caller dispatches between
    /// <see cref="Append(string, Color?)"/> (new line) and
    /// <see cref="UpdateLast"/> (rewrite tail); the widget does not
    /// model an "in-place entry is held" state.
    /// </summary>
    /// <remarks>
    /// Call from the application thread. The Tui model is
    /// single-threaded — view mutations during a draw pass would
    /// race regardless of the API shape, so a timer-driven caller
    /// (e.g. a spinner ticking on a <see cref="System.Threading.Timer"/>)
    /// must either marshal back to the app thread or accept that a
    /// torn read is theoretically possible across one frame.
    /// </remarks>
    public void UpdateLast(string text, Color? foreground = null)
    {
        if (Items.Count == 0) return;
        Items[^1] = new LogEntry(text, foreground);
        MarkDirty();
    }

    public void Clear()
    {
        Items.Clear();
        ScrollOffset = 0;
        MarkDirty();
    }

    protected override void DrawContent(ScreenBuffer screen, Rect content)
    {
        var rows = Math.Min(content.Height, Items.Count - ScrollOffset);
        for (var row = 0; row < rows; row++)
        {
            var item = Items[ScrollOffset + row];
            var fg   = item.Foreground ?? Foreground;
            var text = item.Text ?? string.Empty;
            screen.PutString(content.X, content.Y + row, text.AsSpan(), fg, Background);
        }
    }
}
