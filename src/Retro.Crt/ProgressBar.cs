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
    private long _value;
    private int _lastFilled = -1;
    private int _lastPercent = -1;
    private int _lastFrameLength;
    private bool _disposed;

    private ProgressBar(long total, int width, string? label, Color? color, bool showPercent)
    {
        _total = total < 1 ? 1 : total;
        _width = width < 1 ? 1 : width > ProgressBarRenderer.MaxWidth ? ProgressBarRenderer.MaxWidth : width;
        _label = label;
        _color = color;
        _showPercent = showPercent;
        _full = Glyphs.BarFull;
        _empty = Glyphs.BarEmpty;
        // Without ANSI we cannot redraw in place — so emit only the final
        // frame on Dispose and skip intermediate updates.
        _animated = Crt.ColorEnabled;
    }

    /// <summary>
    /// Begin a new progress bar and render the empty frame.
    /// </summary>
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
            var prefix = "\r";
            var fgOn = ansi && _color is { } c ? AnsiCodes.Foreground(c) : "";
            var fgOff = ansi && _color is not null ? AnsiCodes.Reset : "";

            // Pad if the new frame is shorter than the previous one (only
            // possible if a caller varies the label between calls).
            var padding = "";
            if (frame.Length < _lastFrameLength)
                padding = new string(' ', _lastFrameLength - frame.Length);

            Console.Out.Write(prefix + fgOn + frame + padding + fgOff);
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
