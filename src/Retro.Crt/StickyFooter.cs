using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// A persistent N-row region pinned to the bottom of the terminal. While
/// alive, <see cref="Crt.Write(string)"/> / <see cref="Crt.WriteLine(string)"/>
/// output scrolls in the region <em>above</em> it (native scrollback
/// intact); the footer rows are reserved and repainted on demand. Closes
/// the gap between line-mode and the full alt-screen <c>Application</c>:
/// a downstream app gets a fixed footer / status bar without giving up
/// the terminal's native selection, wheel, or history.
/// </summary>
/// <remarks>
/// <para>
/// Mechanism is DECSTBM (CSI <c>top;bottom r</c>) — the scroll region is
/// pinned to rows 1..H-N so terminal-level line feeds keep new output
/// inside that band, while the bottom N rows are out-of-band canvas the
/// footer owns. Disposing resets the scroll region and parks the cursor
/// below the last scrolled line so the user's shell prompt lands cleanly.
/// </para>
/// <para>
/// Use once via <c>using var footer = StickyFooter.Start(3, Paint);</c>.
/// One footer per process at a time is the assumed model: nesting is not
/// supported because the terminal scroll region is global.
/// </para>
/// <para>
/// No-op when <see cref="Crt.IsInteractive"/> is false (redirected /
/// dumb / no-VT) or the terminal is too small to host both the footer
/// and a scroll region: <see cref="Start"/> returns a disposed scope
/// that ignores <see cref="Refresh"/>. Consumers that care about a
/// redirected footer should print it themselves.
/// </para>
/// <para>
/// Thread-safe: <see cref="Refresh"/> may be called from any thread.
/// A background timer also calls it on terminal resize (SIGWINCH on
/// Unix, polled width / height on Windows), so the footer reflows
/// without the caller having to drive it.
/// </para>
/// </remarks>
public sealed class StickyFooter : IDisposable
{
    private readonly int _rows;
    private readonly Action<ScreenBuffer> _paint;
    private readonly Lock _gate = new();
    private ScreenBuffer? _buffer;
    private Timer? _resizeWatcher;
    private int _width;
    private int _height;
    private bool _disposed;

    private static int _activeCount;
    private static bool _exitHandlerRegistered;
    private static readonly Lock ExitHandlerGate = new();

    private StickyFooter(int rows, Action<ScreenBuffer> paint)
    {
        _rows = rows;
        _paint = paint;
    }

    /// <summary>
    /// Reserve <paramref name="rows"/> rows at the bottom of the terminal,
    /// pin the scroll region to the area above (DECSTBM), and paint the
    /// footer once.
    /// </summary>
    /// <param name="rows">Footer height. Must be ≥ 1.</param>
    /// <param name="paint">
    /// Paints the footer. Invoked on <see cref="Start"/>, on every
    /// <see cref="Refresh"/>, and after a terminal resize. Receives a
    /// <see cref="ScreenBuffer"/> sized <c>Width × rows</c>; the footer
    /// blits its content into the reserved rows.
    /// </param>
    public static StickyFooter Start(int rows, Action<ScreenBuffer> paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        if (rows < 1)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Footer must reserve at least one row.");

        // Non-interactive (redirected, dumb, NO_COLOR on a non-tty,
        // Windows-no-VT). Return a pre-disposed scope so the caller's
        // `using` block is a no-op and Refresh does nothing — they can
        // emit a plain header line themselves if they care.
        if (!Crt.IsInteractive)
            return new StickyFooter(rows, paint) { _disposed = true };

        var footer = new StickyFooter(rows, paint);
        footer.Setup();
        return footer;
    }

    /// <summary>
    /// Repaint the footer now — call after caller state (status text,
    /// progress numbers, …) changes. Cheap; safe to call from any thread.
    /// </summary>
    public void Refresh()
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            var prevHeight = _height;
            if (PollResizeLocked())
                RestakeAfterResizeLocked(prevHeight);
            PaintFooterLocked();
            Crt.Sink.Flush();
        }
    }

    /// <summary>
    /// Reset the scroll region to the full screen and park the cursor
    /// below the last scrolled line. Safe to call multiple times — only
    /// the first call emits escapes; further calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;

            _resizeWatcher?.Dispose();
            _resizeWatcher = null;

            try
            {
                // 1. Move into the scroll region so the reset doesn't
                //    leave the cursor stranded inside the footer.
                // 2. ResetScrollRegion — DECSTBM back to full screen.
                // 3. Move to the very bottom row and emit one LF so a
                //    following shell prompt lands on a fresh line below
                //    the footer (which becomes the prior scrollback row).
                var sb = new StringBuilder(48);
                sb.Append(AnsiCodes.GotoXY(1, _height - _rows));
                sb.Append(AnsiCodes.ResetScrollRegion);
                sb.Append(AnsiCodes.GotoXY(1, _height));
                sb.Append('\n');
                Crt.Sink.Write(sb.ToString());
                Crt.Sink.Flush();
            }
            catch
            {
                // Sink may already be torn down during process exit —
                // swallow rather than crash on top of the original
                // termination cause.
            }

            DecrementActive();
        }
    }

    private void Setup()
    {
        WindowResize.Install();
        _width = Crt.WindowWidth;
        _height = Crt.WindowHeight;

        // Refuse to install when the terminal can't host both a scroll
        // region AND the reserved rows. Caller's `using` becomes a no-op;
        // they keep printing in line mode without a footer.
        if (_width < 1 || _height <= _rows)
        {
            _disposed = true;
            return;
        }

        IncrementActive();
        _buffer = new ScreenBuffer(_width, _rows);

        // 1. SU — scroll existing content up by `_rows` so the bottom
        //    rows are blank without losing scrollback.
        // 2. DECSTBM — pin the scroll region to the upper band.
        // 3. Park the cursor at the bottom of the scroll region so the
        //    caller's first Crt.WriteLine lands there and pushes any
        //    further output upward naturally.
        var sb = new StringBuilder(64);
        sb.Append(AnsiCodes.ScrollUp(_rows));
        sb.Append(AnsiCodes.SetScrollRegion(1, _height - _rows));
        sb.Append(AnsiCodes.GotoXY(1, _height - _rows));
        Crt.Sink.Write(sb.ToString());

        PaintFooterLocked();
        Crt.Sink.Flush();

        // Background resize watcher: SIGWINCH on Unix (drained via
        // WindowResize.ConsumePending), polled WindowWidth/Height on
        // Windows where SIGWINCH is a no-op. 250 ms is snappy enough to
        // feel responsive during an interactive resize without burning
        // a wakeup six times a second on an idle UI.
        _resizeWatcher = new Timer(OnResizeTick, null, 250, 250);
    }

    private void OnResizeTick(object? state)
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            var prevHeight = _height;
            if (!PollResizeLocked()) return;
            RestakeAfterResizeLocked(prevHeight);
            PaintFooterLocked();
            Crt.Sink.Flush();
        }
    }

    // Caller must hold _gate. Returns true when the terminal size has
    // changed AND the new size is large enough to keep hosting the
    // footer; on undersized terminals returns false so the existing
    // setup limps along rather than tearing itself down.
    private bool PollResizeLocked()
    {
        // Always drain the SIGWINCH flag so it doesn't pile up across
        // ticks; the source of truth is still the sampled width / height.
        WindowResize.ConsumePending();
        var w = Crt.WindowWidth;
        var h = Crt.WindowHeight;

        if (w == _width && h == _height) return false;
        if (w < 1 || h <= _rows) return false;

        _width = w;
        _height = h;
        _buffer = new ScreenBuffer(_width, _rows);
        return true;
    }

    // Caller must hold _gate. Re-pins the scroll region to the new geometry
    // and, when the terminal grew, erases the stale footer band left at the
    // old bottom rows (now inside the enlarged scroll region) before it
    // lingers mid-screen until output scrolls past it.
    //
    // The whole sequence is wrapped in SaveCursor/RestoreCursor: re-pinning
    // the scroll region (DECSTBM) homes the cursor as a side effect, so the
    // old unconditional GotoXY existed only to undo that. A host that anchors
    // an in-place line to the cursor (a \r-rewritten progress line) saw that
    // teleport land its next repaint on a fresh row every resize tick, one
    // ghost frame per step. Saving and restoring keeps the host's cursor put.
    // DECSC coordinates are absolute and may be slightly off after a reflow,
    // but that is strictly better than moving the cursor on every tick.
    private void RestakeAfterResizeLocked(int prevHeight)
    {
        var sb = new StringBuilder(64);
        sb.Append(AnsiCodes.SaveCursor);
        sb.Append(AnsiCodes.SetScrollRegion(1, _height - _rows));

        // On grow, the previous footer occupied rows prevHeight-_rows+1 ..
        // prevHeight; those now sit above the new footer inside the scroll
        // region and show a stale band. Erase only those old footer rows
        // that are still on screen and now above the new band (rows the
        // new footer repaints, or rows scrolled off on shrink, are skipped).
        var newFooterTop = _height - _rows + 1;
        for (var row = prevHeight - _rows + 1; row <= prevHeight; row++)
        {
            if (row < 1 || row >= newFooterTop || row > _height) continue;
            sb.Append(AnsiCodes.GotoXY(1, row));
            sb.Append(AnsiCodes.ClearToEol);
        }

        sb.Append(AnsiCodes.RestoreCursor);
        Crt.Sink.Write(sb.ToString());
    }

    // Caller must hold _gate.
    private void PaintFooterLocked()
    {
        var buf = _buffer!;
        buf.Clear();
        _paint(buf);

        var topRow = _height - _rows + 1;
        var sb = new StringBuilder(buf.Width * buf.Height * 6 + 32);
        sb.Append(AnsiCodes.SaveCursor);

        Color penFg = default, penBg = default;
        var penAttrs = CellAttrs.None;
        var penKnown = false;

        for (var y = 0; y < buf.Height; y++)
        {
            sb.Append(AnsiCodes.GotoXY(1, topRow + y));
            for (var x = 0; x < buf.Width; x++)
            {
                var cell = buf[x, y];
                if (!penKnown
                    || penFg    != cell.Fg
                    || penBg    != cell.Bg
                    || penAttrs != cell.Attrs)
                {
                    sb.Append(AnsiCodes.Reset);
                    sb.Append(Emit.Fg(cell.Fg));
                    sb.Append(Emit.Bg(cell.Bg));
                    if ((cell.Attrs & CellAttrs.Bold)      != 0) sb.Append(AnsiCodes.Bold);
                    if ((cell.Attrs & CellAttrs.Underline) != 0) sb.Append(AnsiCodes.Underline);

                    penKnown = true;
                    penFg    = cell.Fg;
                    penBg    = cell.Bg;
                    penAttrs = cell.Attrs;
                }
                sb.Append(cell.Glyph);
            }
        }

        sb.Append(AnsiCodes.Reset);
        sb.Append(AnsiCodes.RestoreCursor);
        Crt.Sink.Write(sb.ToString());
    }

    private static void IncrementActive()
    {
        lock (ExitHandlerGate)
        {
            _activeCount++;
            if (_exitHandlerRegistered) return;
            _exitHandlerRegistered = true;

            // Same two-handler shape as UseAlternateScreen: CancelKeyPress
            // catches Ctrl-C, ProcessExit catches graceful exit + SIGTERM.
            // Both must reset DECSTBM or the user's shell stays trapped
            // in the old scroll region for the rest of the session.
            Console.CancelKeyPress += (_, _) => RestoreOnExit();
            AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreOnExit();
        }
    }

    private static void DecrementActive()
    {
        lock (ExitHandlerGate)
        {
            if (_activeCount > 0) _activeCount--;
        }
    }

    private static void RestoreOnExit()
    {
        lock (ExitHandlerGate)
        {
            if (_activeCount <= 0) return;
            try
            {
                Crt.Sink.Write(AnsiCodes.ResetScrollRegion);
                Crt.Sink.Flush();
            }
            catch
            {
                // Sink may be disposed during shutdown.
            }
            _activeCount = 0;
        }
    }
}
