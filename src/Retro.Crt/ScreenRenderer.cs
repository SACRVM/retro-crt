using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Diff renderer: emits the minimal stream of ANSI sequences that turns
/// a previous frame into a current frame on the terminal. Pair with
/// <see cref="ScreenBuffer"/> to drive flicker-free game loops or TUI
/// redraws.
/// </summary>
/// <remarks>
/// <para>
/// On the very first frame (or whenever the dimensions differ between
/// frames) <see cref="Render"/> repaints every cell. Subsequent frames
/// only emit cells that actually changed plus the cursor moves to reach
/// them — a typical incremental update on a 80×25 buffer is a few
/// hundred bytes.
/// </para>
/// <para>
/// The renderer parks the cursor at <c>(1, 1)</c> and emits a final
/// SGR <c>RESET</c> when at least one cell was painted, so callers don't
/// inherit the last cell's pen state. No-op when nothing changed.
/// </para>
/// <para>
/// The whole frame is built into a process-static <see cref="StringBuilder"/>
/// and flushed to the sink in a single <c>Write</c>. On Windows
/// <c>Console.Out</c> takes a per-call lock; batching cuts what would
/// otherwise be tens of thousands of calls per high-churn frame down to
/// one. Render is not re-entrant — that's fine, callers serialize on
/// their own frame loop.
/// </para>
/// </remarks>
public static class ScreenRenderer
{
    private static readonly StringBuilder Frame = new(8192);

    /// <summary>
    /// Render the diff between <paramref name="previous"/> and
    /// <paramref name="current"/> into <paramref name="sink"/>. Pass
    /// <c>null</c> for <paramref name="previous"/> on the very first
    /// frame to force a full repaint.
    /// </summary>
    public static void Render(ScreenBuffer? previous, ScreenBuffer current, TextWriter sink)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(sink);

        var w = current.Width;
        var h = current.Height;
        var curCells = current.AsSpan();

        var hasPrev = previous is not null
                   && previous.Width  == w
                   && previous.Height == h;
        var prevCells = hasPrev ? previous!.AsSpan() : default;

        var penKnown = false;
        Color penFg = default, penBg = default;
        var penAttrs = CellAttrs.None;

        var cursorX = -1;
        var cursorY = -1;

        var painted = false;
        Frame.Clear();

        for (var y = 0; y < h; y++)
        {
            var rowBase = y * w;
            for (var x = 0; x < w; x++)
            {
                var idx  = rowBase + x;
                var cell = curCells[idx];

                if (hasPrev && cell == prevCells[idx]) continue;

                if (cursorX != x || cursorY != y)
                {
                    Frame.Append(AnsiCodes.GotoXY(x + 1, y + 1));
                    cursorX = x;
                    cursorY = y;
                }

                if (!penKnown
                    || penFg    != cell.Fg
                    || penBg    != cell.Bg
                    || penAttrs != cell.Attrs)
                {
                    Frame.Append(AnsiCodes.Reset);
                    Frame.Append(Emit.Fg(cell.Fg));
                    Frame.Append(Emit.Bg(cell.Bg));
                    if ((cell.Attrs & CellAttrs.Bold)      != 0) Frame.Append(AnsiCodes.Bold);
                    if ((cell.Attrs & CellAttrs.Underline) != 0) Frame.Append(AnsiCodes.Underline);

                    penKnown = true;
                    penFg    = cell.Fg;
                    penBg    = cell.Bg;
                    penAttrs = cell.Attrs;
                }

                Frame.Append(cell.Glyph);
                cursorX++;
                painted = true;
            }
        }

        if (painted)
        {
            Frame.Append(AnsiCodes.Reset);
            Frame.Append(AnsiCodes.GotoXY(1, 1));
        }

        if (Frame.Length > 0)
            sink.Write(Frame);
    }
}
