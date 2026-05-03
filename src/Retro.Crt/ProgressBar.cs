using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Single-line progress bar that redraws in place via carriage return. Use
/// once via <c>using var bar = ProgressBar.Start(total);</c> — disposing
/// finishes the bar (full fill + newline).
/// </summary>
public sealed class ProgressBar : IDisposable
{
    private readonly long _total;
    private readonly int _width;
    private readonly string? _label;
    private readonly Color? _color;
    private readonly bool _showPercent;
    private readonly char _full;
    private readonly char _empty;

    private readonly bool _animated;
    private readonly int _anchorColumn;
    private long _value;
    private int _lastFilled = -1;
    private int _lastPercent = -1;
    private int _lastFrameLength;
    private bool _disposed;

    private ProgressBar(long total, int width, string? label, Color? color, bool showPercent)
    {
        _total = total < 1 ? 1 : total;
        _width = ResolveWidth(width, label, showPercent);
        _label = label;
        // No explicit color → pick the active theme's accent.
        _color = color ?? Crt.CurrentTheme?.Accent;
        _showPercent = showPercent;
        _full = Glyphs.BarFull;
        _empty = Glyphs.BarEmpty;
        // Animation needs cursor / CR, not colors. NO_COLOR=1 in a
        // real TTY still redraws in place; only redirected /
        // dumb-terminal hosts skip intermediate frames so logs don't
        // collect 60 progress lines in a row.
        _animated = Crt.IsInteractive;
        // Anchor the bar's redraw to wherever the cursor sits at Start.
        // GotoXY(20,8) before Start → bar redraws keep their left edge at
        // column 20 across the bar's lifetime.
        _anchorColumn = CursorState.GetLeft();
    }

    /// <summary>
    /// Begin a new progress bar and render the empty frame.
    /// </summary>
    /// <param name="total">Total work units the bar represents (≥1).</param>
    /// <param name="width">
    /// Bar cells. Defaults to 30. Pass <see cref="Crt.FillWidth"/> to
    /// span the terminal — the bar sizes itself to the available space
    /// after subtracting the label and percent suffix. Clamped to
    /// <c>[1, ProgressBarRenderer.MaxWidth]</c>.
    /// </param>
    /// <param name="label">Optional text written before the bar, with a
    /// single trailing space. Falls back to no label when null.</param>
    /// <param name="color">Optional foreground for the bar fill. When
    /// null, falls back to the active theme's accent slot, or no color
    /// if no theme is active.</param>
    /// <param name="showPercent">Append a 5-character percent suffix
    /// (<c>" ddd%"</c>) to every frame. Defaults to true.</param>
    public static ProgressBar Start(
        long total,
        int width = 30,
        string? label = null,
        Color? color = null,
        bool showPercent = true)
    {
        var bar = new ProgressBar(total, width, label, color, showPercent);
        if (bar._animated) Crt.Write(AnsiCodes.HideCursor);
        bar.Redraw(force: true);
        return bar;
    }

    /// <summary>
    /// Resolve the requested bar-cell count, honoring the
    /// <see cref="Crt.FillWidth"/> sentinel.
    /// </summary>
    private static int ResolveWidth(int width, string? label, bool showPercent)
    {
        if (width == Crt.FillWidth)
        {
            // Total line = label + " " + bar + " ddd%". Reserve those
            // around the bar; minimum bar width is 1.
            var reserved = (string.IsNullOrEmpty(label) ? 0 : label!.Length + 1)
                         + (showPercent ? 5 : 0)
                         + 1; // safety cell so we don't trigger wrap
            var w = Crt.WindowWidth - reserved;
            if (w < 1) w = 1;
            if (w > ProgressBarRenderer.MaxWidth) w = ProgressBarRenderer.MaxWidth;
            return w;
        }
        if (width < 1) return 1;
        if (width > ProgressBarRenderer.MaxWidth) return ProgressBarRenderer.MaxWidth;
        return width;
    }

    public long Value => _value;
    public long Total => _total;

    public void Set(long value)
    {
        if (_disposed) return;
        if (value < 0) value = 0;
        if (value > _total) value = _total;
        _value = value;
        Redraw(force: false);
    }

    public void Tick(long delta = 1) => Set(_value + delta);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _value = _total;
        Redraw(force: true);
        if (_animated) Crt.Write(AnsiCodes.ShowCursor);
        Crt.WriteLine();
    }

    private void Redraw(bool force)
    {
        var ratio = (double)_value / _total;
        var filled = ProgressBarRenderer.FilledCells(ratio, _width);
        var percent = (int)(ratio * 100);

        if (!force && filled == _lastFilled && percent == _lastPercent) return;

        // Non-animated mode: only emit a single final frame on Dispose, and
        // never the empty starter frame. Otherwise log files end up with
        // sixty progress lines in a row.
        if (!_animated && !(_disposed && force)) return;

        var frame = ProgressBarRenderer.RenderFrame(
            _label, ratio, _width, _full, _empty, _showPercent);

        // Frame length is constant (label fixed, width fixed, percent right-
        // padded to three chars), so a CR + overwrite is enough — no CSI K
        // clear, which would otherwise blank the line and flicker. We also
        // emit prefix + colors + frame + reset as one Write so the terminal
        // never sees a half-painted line.
        if (_animated)
        {
            var ansi = Crt.ColorEnabled;
            // CR snaps the cursor to column 1; the indent shifts it back
            // to whatever column the bar was anchored at, so a bar
            // started after GotoXY(20,8) keeps redrawing at column 20.
            var prefix = _anchorColumn > 0
                ? "\r" + new string(' ', _anchorColumn)
                : "\r";
            var fgOn = ansi && _color is { } c ? Emit.Fg(c) : "";
            var fgOff = ansi && _color is not null ? AnsiCodes.Reset : "";

            // Pad if the new frame is shorter than the previous one (only
            // possible if a caller varies the label between calls).
            var padding = "";
            if (frame.Length < _lastFrameLength)
                padding = new string(' ', _lastFrameLength - frame.Length);

            Crt.Sink.Write(prefix + fgOn + frame + padding + fgOff);
        }
        else
        {
            // Non-animated path: single final line on Dispose.
            if (_color is { } c)
            {
                using (Crt.WithStyle(fg: c))
                    Crt.Write(frame);
            }
            else
            {
                Crt.Write(frame);
            }
        }

        _lastFilled = filled;
        _lastPercent = percent;
        _lastFrameLength = frame.Length;
    }
}
