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
/// </remarks>
public static class ScreenRenderer
{
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

        // Different (or absent) prev → every cell is "dirty". A null
        // span compares unequal to every cell because we set hasPrev=false
        // and skip the cell-equality check below.
        var hasPrev = previous is not null
                   && previous.Width  == w
                   && previous.Height == h;
        var prevCells = hasPrev ? previous!.AsSpan() : default;

        // Pen state we believe the terminal currently holds. Starts as
        // "unknown" — first painted cell forces an SGR emission.
        var penKnown = false;
        Color penFg = default, penBg = default;
        var penAttrs = CellAttrs.None;

        // Cursor position we believe the terminal currently has. -1 means
        // "unknown" — first painted cell forces a goto-XY.
        var cursorX = -1;
        var cursorY = -1;

        var painted = false;

        for (var y = 0; y < h; y++)
        {
            var rowBase = y * w;
            for (var x = 0; x < w; x++)
            {
                var idx  = rowBase + x;
                var cell = curCells[idx];

                if (hasPrev && cell == prevCells[idx]) continue;

                // Move cursor if we're not already on this column. After
                // emitting a char the terminal advances the cursor by 1,
                // so only the FIRST dirty cell of a run pays the goto.
                if (cursorX != x || cursorY != y)
                {
                    sink.Write(AnsiCodes.GotoXY(x + 1, y + 1));
                    cursorX = x;
                    cursorY = y;
                }

                // Re-emit SGR if any of fg / bg / attrs differ from the
                // pen we last set. Cheap to overcompare on the
                // record-struct equality, expensive to re-encode if we
                // skip it incorrectly.
                if (!penKnown
                    || penFg    != cell.Fg
                    || penBg    != cell.Bg
                    || penAttrs != cell.Attrs)
                {
                    // Reset clears prior bold/underline that wouldn't go
                    // away on their own. After Reset the fg/bg are also
                    // gone, so we re-emit them too.
                    sink.Write(AnsiCodes.Reset);
                    sink.Write(Emit.Fg(cell.Fg));
                    sink.Write(Emit.Bg(cell.Bg));
                    if ((cell.Attrs & CellAttrs.Bold)      != 0) sink.Write(AnsiCodes.Bold);
                    if ((cell.Attrs & CellAttrs.Underline) != 0) sink.Write(AnsiCodes.Underline);

                    penKnown = true;
                    penFg    = cell.Fg;
                    penBg    = cell.Bg;
                    penAttrs = cell.Attrs;
                }

                sink.Write(cell.Glyph);
                cursorX++;
                painted = true;
            }
        }

        if (painted)
        {
            // Park the cursor and clear the pen so the caller's next
            // Crt.Write doesn't inherit our colors / position.
            sink.Write(AnsiCodes.Reset);
            sink.Write(AnsiCodes.GotoXY(1, 1));
        }
    }
}
