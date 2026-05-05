using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt.Tests.Support;

/// <summary>
/// Test-only <see cref="IInputSource"/> that hands out queued byte
/// chunks (one per <c>FeedXxx</c> call) and reports EOF when
/// <see cref="SignalEof"/> has been called and the queue is empty.
/// Shared by TerminalInputTests and BracketedPasteTests.
/// </summary>
internal sealed class FakeSource : IInputSource
{
    private readonly Queue<byte[]> _chunks = new();
    private bool _eof;

    public void FeedAscii(string s) => _chunks.Enqueue(Encoding.ASCII.GetBytes(s));
    public void FeedBytes(byte[] b) => _chunks.Enqueue(b);
    public void SignalEof() => _eof = true;

    public int Read(Span<byte> buffer)
    {
        if (_chunks.Count == 0) return 0;
        var chunk = _chunks.Dequeue();
        var n = Math.Min(buffer.Length, chunk.Length);
        chunk.AsSpan(0, n).CopyTo(buffer);
        if (n < chunk.Length)
        {
            var rest = chunk.AsSpan(n).ToArray();
            var pending = new Queue<byte[]>();
            pending.Enqueue(rest);
            while (_chunks.Count > 0) pending.Enqueue(_chunks.Dequeue());
            foreach (var c in pending) _chunks.Enqueue(c);
        }
        return n;
    }

    public bool TryWait(int timeoutMs) => _chunks.Count > 0 || _eof;
}
