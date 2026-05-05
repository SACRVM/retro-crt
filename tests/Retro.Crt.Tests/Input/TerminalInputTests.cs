using System.Text;
using Retro.Crt.Input;
using Retro.Crt.Internals;
using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Input;

[Collection(EnvMutatingCollection.Name)]
public class TerminalInputTests : IDisposable
{
    private readonly FakeSource _source = new();

    public TerminalInputTests() => TerminalInput.OverrideSourceForTests(_source);

    public void Dispose()
    {
        TerminalInput.OverrideSourceForTests(null);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ReadEvent_decodes_a_single_glyph()
    {
        _source.FeedAscii("A");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(InputEventKind.Key, ev.Kind);
        Assert.Equal(Key.Glyph, ev.Key.Key);
        Assert.Equal('A', ev.Key.Glyph);
    }

    [Fact]
    public void ReadEvent_decodes_arrow_key_in_one_read()
    {
        _source.FeedAscii("\x1b[A");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(Key.Up, ev.Key.Key);
    }

    [Fact]
    public void ReadEvent_reassembles_escape_sequence_split_across_reads()
    {
        // First read delivers "ESC [", second read delivers "B" — the
        // parser sees Incomplete on read 1 and only emits Down after the
        // second byte arrives.
        _source.FeedAscii("\x1b[");
        _source.FeedAscii("B");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(Key.Down, ev.Key.Key);
    }

    [Fact]
    public void ReadEvent_drains_two_events_from_one_read()
    {
        _source.FeedAscii("AB");

        var first  = TerminalInput.ReadEvent();
        var second = TerminalInput.ReadEvent();

        Assert.Equal('A', first.Key.Glyph);
        Assert.Equal('B', second.Key.Glyph);
    }

    [Fact]
    public void ReadEvent_throws_on_EOF()
    {
        _source.SignalEof();

        Assert.Throws<EndOfStreamException>(() => TerminalInput.ReadEvent());
    }

    [Fact]
    public void ReadEvent_decodes_multibyte_utf8_split_across_reads()
    {
        // "ä" = 0xC3 0xA4. Split between the two bytes; the decoder
        // must hold the partial byte across reads.
        _source.FeedBytes([0xC3]);
        _source.FeedBytes([0xA4]);

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(Key.Glyph, ev.Key.Key);
        Assert.Equal('ä', ev.Key.Glyph);
    }

    [Fact]
    public void TryReadEvent_returns_false_when_no_bytes_available()
    {
        var got = TerminalInput.TryReadEvent(out _);

        Assert.False(got);
    }

    [Fact]
    public void TryReadEvent_returns_buffered_event()
    {
        _source.FeedAscii("X");
        // Drain via blocking call so the byte sits in the parser char
        // buffer — except ReadEvent already consumed it, so feed two:
        _source.FeedAscii("YZ");
        TerminalInput.ReadEvent();   // consumes 'X'

        var got = TerminalInput.TryReadEvent(out var ev);

        Assert.True(got);
        Assert.Equal('Y', ev.Key.Glyph);
    }

    [Fact]
    public void TryReadEvent_returns_false_after_EOF()
    {
        _source.SignalEof();
        // Drain once via the non-blocking path: it must observe EOF
        // without throwing.
        var first = TerminalInput.TryReadEvent(out _);
        var second = TerminalInput.TryReadEvent(out _);

        Assert.False(first);
        Assert.False(second);
    }

}
