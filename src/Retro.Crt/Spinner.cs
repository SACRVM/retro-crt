using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Single-line animated spinner. Use once via
/// <c>using var s = Spinner.Show("loading…");</c> — disposing stops the
/// animation, clears the spinner line, and emits a newline.
/// </summary>
/// <remarks>
/// Without ANSI support (output redirected, <c>NO_COLOR</c>, dumb
/// terminal) the spinner does not animate: it writes the label once on
/// <see cref="Show"/> and a newline on <see cref="Stop()"/>. This keeps
/// log files free of dozens of overwrite frames.
///
/// The spinner owns its line for its lifetime — concurrent writes from
/// other code while the spinner is active will be clobbered by the next
/// repaint. Either route writes through <see cref="Update"/> /
/// <see cref="Stop(string?, Color?)"/>, or stop the spinner first.
/// </remarks>
public sealed class Spinner : IDisposable
{
    private readonly string[] _frames;
    private readonly Color? _color;
    private readonly bool _animated;
    private readonly Lock _gate = new();
    private readonly Timer? _timer;

    private string _label;
    private int _frameIndex;
    private int _lastVisibleLength;
    private bool _disposed;

    private Spinner(string label, string[] frames, Color? color, int msPerFrame)
    {
        _label = label;
        _frames = frames;
        _color = color;
        _animated = Crt.ColorEnabled;

        if (_animated)
        {
            Console.Out.Write(AnsiCodes.HideCursor);
            RenderLocked();
            _timer = new Timer(OnTick, null, msPerFrame, msPerFrame);
        }
        else
        {
            // Non-animated: write the label once, no spinner glyph. The
            // final newline is emitted on Stop / Dispose.
            Console.Out.Write(label);
        }
    }

    /// <summary>Begin a new spinner.</summary>
    /// <param name="label">Text shown to the right of the animated glyph.</param>
    /// <param name="style">Frame set. Defaults to <see cref="SpinnerStyle.Pipe"/>.</param>
    /// <param name="color">Optional foreground color for the glyph.</param>
    /// <param name="msPerFrame">Frame duration in milliseconds. Clamped to ≥1.</param>
    public static Spinner Show(
        string label,
        SpinnerStyle style = SpinnerStyle.Pipe,
        Color? color = null,
        int msPerFrame = 80)
    {
        var frames = ResolveFrames(style);
        if (msPerFrame < 1) msPerFrame = 1;
        return new Spinner(label, frames, color, msPerFrame);
    }

    /// <summary>Replace the label without stopping the animation.</summary>
    public void Update(string label)
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            _label = label;
            if (_animated) RenderLocked();
        }
    }

    /// <summary>Stop the spinner with no final state.</summary>
    public void Stop() => Stop(finalLabel: null, finalColor: null);

    /// <summary>
    /// Stop the spinner and write a final state in place — useful for
    /// "✓ done" / "✗ failed" indications. The final label fully replaces
    /// the spinner line; bring your own glyph.
    /// </summary>
    public void Stop(string? finalLabel, Color? finalColor = null)
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();

            if (_animated)
            {
                // Erase the spinner line: CR, fill with spaces up to the
                // last visible width, CR again. Then optionally print
                // finalLabel, restore the cursor, newline.
                var sb = new StringBuilder(64);
                sb.Append('\r');
                if (_lastVisibleLength > 0) sb.Append(' ', _lastVisibleLength);
                sb.Append('\r');

                if (finalLabel is not null)
                {
                    if (finalColor is { } fc) sb.Append(AnsiCodes.Foreground(fc));
                    sb.Append(finalLabel);
                    if (finalColor is not null) sb.Append(AnsiCodes.Reset);
                }

                sb.Append(AnsiCodes.ShowCursor);
                Console.Out.Write(sb.ToString());
                Console.Out.WriteLine();
            }
            else
            {
                // Non-animated: cannot erase the static label. Drop a
                // newline; if finalLabel is set, write it on the next
                // line so logs stay readable.
                Console.Out.WriteLine();
                if (finalLabel is not null)
                    Console.Out.WriteLine(finalLabel);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private void OnTick(object? state)
    {
        if (_disposed) return;
        lock (_gate)
        {
            if (_disposed) return;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            RenderLocked();
        }
    }

    // Caller must hold _gate.
    private void RenderLocked()
    {
        var glyph = _frames[_frameIndex];
        var sb = new StringBuilder(64);

        sb.Append('\r');
        if (_color is { } c) sb.Append(AnsiCodes.Foreground(c));
        sb.Append(glyph);
        if (_color is not null) sb.Append(AnsiCodes.Reset);
        sb.Append(' ');
        sb.Append(_label);

        var newLength = glyph.Length + 1 + _label.Length;
        if (newLength < _lastVisibleLength)
            sb.Append(' ', _lastVisibleLength - newLength);

        _lastVisibleLength = newLength;
        Console.Out.Write(sb.ToString());
    }

    private static string[] ResolveFrames(SpinnerStyle s) => s switch
    {
        SpinnerStyle.Pipe    => PipeFrames,
        SpinnerStyle.Dots    => DotsFrames,
        SpinnerStyle.Braille => TerminalCapabilities.SupportsUnicode ? BrailleFrames : PipeFrames,
        SpinnerStyle.Block   => TerminalCapabilities.SupportsUnicode ? BlockFrames   : PipeFrames,
        SpinnerStyle.Arc     => TerminalCapabilities.SupportsUnicode ? ArcFrames     : PipeFrames,
        _                    => PipeFrames,
    };

    private static readonly string[] PipeFrames    = ["|", "/", "-", "\\"];
    private static readonly string[] DotsFrames    = [".  ", ".. ", "..."];
    private static readonly string[] BrailleFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private static readonly string[] BlockFrames   = ["▖", "▘", "▝", "▗"];
    private static readonly string[] ArcFrames     = ["◜", "◝", "◞", "◟"];
}
