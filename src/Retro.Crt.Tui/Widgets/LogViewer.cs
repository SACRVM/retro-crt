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
/// time. <see cref="ScrollViewer.AutoScroll"/> follows new entries as
/// they arrive — the keystone widget for log panes, REPL output, and
/// chat-style histories.
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
