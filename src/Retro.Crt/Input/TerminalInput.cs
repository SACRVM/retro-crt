using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt.Input;

/// <summary>
/// Read decoded <see cref="InputEvent"/>s from stdin. Pair with
/// <see cref="RawMode"/> so the terminal actually delivers bytes instead
/// of cooked, line-buffered input.
/// </summary>
/// <remarks>
/// <para>
/// Maintains a small UTF-8 byte buffer and a parser char buffer between
/// calls so escape sequences split across reads still parse correctly.
/// All state is process-global because stdin is.
/// </para>
/// <para>
/// On Unix, <see cref="TryReadEvent"/> uses <c>poll(2)</c> with timeout 0
/// to check for available bytes without blocking. On Windows the
/// underlying <c>WaitForSingleObject</c> is best-effort: a console
/// handle becomes signaled on any input event (including key-up events
/// that produce no VT bytes), so an occasional false positive followed
/// by a short blocking <c>ReadFile</c> is possible. For strict
/// non-blocking semantics on Windows, run <see cref="ReadEvent"/> on a
/// background thread and post events to your main loop yourself.
/// </para>
/// </remarks>
public static class TerminalInput
{
    private static readonly Lock Gate = new();
    private static IInputSource? _source;
    private static readonly byte[] ByteBuf = new byte[256];
    private static readonly char[] CharBuf = new char[512];
    private static int _charLen;
    private static Decoder _decoder = Encoding.UTF8.GetDecoder();
    private static bool _eof;

    /// <summary>
    /// Block until one <see cref="InputEvent"/> can be parsed, then return
    /// it. Throws <see cref="EndOfStreamException"/> if stdin closes
    /// before any further event arrives.
    /// </summary>
    public static InputEvent ReadEvent()
    {
        lock (Gate)
        {
            while (true)
            {
                if (TryParseFromBuffer(out var ev)) return ev;
                if (_eof) throw new EndOfStreamException("stdin closed");
                FillBufferBlocking();
            }
        }
    }

    /// <summary>
    /// Try to return the next event without blocking. Returns
    /// <c>false</c> when no complete event is currently buffered AND no
    /// new bytes are available (or stdin is at EOF).
    /// </summary>
    public static bool TryReadEvent(out InputEvent ev)
        => WaitForEvent(0, out ev);

    /// <summary>
    /// Wait up to <paramref name="timeoutMs"/> milliseconds for an
    /// event. Returns <c>true</c> when an event was decoded, <c>false</c>
    /// when the timeout expires or stdin reaches EOF. Pass <c>0</c> for
    /// a strictly non-blocking poll, or a negative number to wait
    /// indefinitely (equivalent to <see cref="ReadEvent"/> minus the
    /// EOF exception).
    /// </summary>
    public static bool WaitForEvent(int timeoutMs, out InputEvent ev)
    {
        lock (Gate)
        {
            if (TryParseFromBuffer(out ev)) return true;
            if (_eof) return false;

            if (Source.TryWait(timeoutMs))
                FillBufferBlocking();

            return TryParseFromBuffer(out ev);
        }
    }

    private static IInputSource Source
    {
        get
        {
            _source ??= OsInputSource.Create();
            return _source;
        }
    }

    /// <summary>
    /// Test-only hook: replace the byte source and reset all decoder
    /// state. Pass <c>null</c> to restore the OS-backed default.
    /// </summary>
    internal static void OverrideSourceForTests(IInputSource? source)
    {
        lock (Gate)
        {
            _source = source;
            _charLen = 0;
            _decoder = Encoding.UTF8.GetDecoder();
            _eof = false;
        }
    }

    private static bool TryParseFromBuffer(out InputEvent ev)
    {
        ev = default;
        while (_charLen > 0)
        {
            var status = InputParser.TryParseEvent(
                CharBuf.AsSpan(0, _charLen), out ev, out var consumed);

            if (status == InputParseStatus.Ok)
            {
                ShiftChars(consumed);
                return true;
            }

            if (status == InputParseStatus.Invalid)
            {
                ShiftChars(consumed > 0 ? consumed : 1);
                continue;
            }

            // Incomplete — caller should pull more bytes.
            return false;
        }
        return false;
    }

    private static void ShiftChars(int count)
    {
        if (count <= 0) return;
        if (count >= _charLen) { _charLen = 0; return; }
        Array.Copy(CharBuf, count, CharBuf, 0, _charLen - count);
        _charLen -= count;
    }

    private static void FillBufferBlocking()
    {
        var read = Source.Read(ByteBuf);
        if (read <= 0) { _eof = true; return; }

        var charsFree = CharBuf.Length - _charLen;
        if (charsFree == 0)
        {
            // Pathological: the parser hasn't drained anything in a
            // 512-char buffer. Drop the oldest 1 char to keep moving;
            // this only happens on garbage input that's also
            // unparseable, so a one-byte slip is harmless.
            ShiftChars(1);
            charsFree = 1;
        }

        _decoder.Convert(
            ByteBuf.AsSpan(0, read),
            CharBuf.AsSpan(_charLen, charsFree),
            flush: false,
            out _,
            out var charsUsed,
            out _);
        _charLen += charsUsed;
    }
}
