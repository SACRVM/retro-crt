using Retro.Crt.Input;
using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Input;

[Collection(EnvMutatingCollection.Name)]
public class BracketedPasteTests : IDisposable
{
    private readonly FakeSource _source = new();

    public BracketedPasteTests() => TerminalInput.OverrideSourceForTests(_source);

    public void Dispose()
    {
        TerminalInput.OverrideSourceForTests(null);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ReadEvent_emits_paste_event_for_full_bracketed_block()
    {
        _source.FeedAscii("\x1b[200~hello world\x1b[201~");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(InputEventKind.Paste, ev.Kind);
        Assert.Equal("hello world", ev.Paste);
    }

    [Fact]
    public void Paste_split_across_reads_concatenates()
    {
        _source.FeedAscii("\x1b[200~he");
        _source.FeedAscii("llo\x1b[201~");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal("hello", ev.Paste);
    }

    [Fact]
    public void Paste_terminator_split_across_reads_still_terminates()
    {
        _source.FeedAscii("\x1b[200~hi\x1b[20");
        _source.FeedAscii("1~");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal("hi", ev.Paste);
    }

    [Fact]
    public void Esc_sequences_inside_paste_are_not_reinterpreted_as_keys()
    {
        // Pasting text that happens to contain an arrow-key escape
        // (ESC [ A) should yield the literal characters in the paste
        // string, NOT a separate Key.Up event.
        _source.FeedAscii("\x1b[200~a\x1b[Ab\x1b[201~");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(InputEventKind.Paste, ev.Kind);
        Assert.Equal("a\x1b[Ab", ev.Paste);
    }

    [Fact]
    public void Key_after_paste_decodes_normally()
    {
        _source.FeedAscii("\x1b[200~hi\x1b[201~X");

        var first  = TerminalInput.ReadEvent();
        var second = TerminalInput.ReadEvent();

        Assert.Equal(InputEventKind.Paste, first.Kind);
        Assert.Equal('X', second.Key.Glyph);
    }

    [Fact]
    public void Paste_start_split_across_reads_waits_for_full_sentinel()
    {
        // First chunk is just "\x1b[2" — same prefix as Insert/F9/etc.
        // The reader must defer until enough bytes arrive to either
        // confirm or reject the paste-start sentinel.
        _source.FeedAscii("\x1b[2");
        var got = TerminalInput.TryReadEvent(out _);
        Assert.False(got);

        _source.FeedAscii("00~payload\x1b[201~");
        var ev = TerminalInput.ReadEvent();
        Assert.Equal("payload", ev.Paste);
    }

    [Fact]
    public void Empty_paste_yields_empty_string()
    {
        _source.FeedAscii("\x1b[200~\x1b[201~");

        var ev = TerminalInput.ReadEvent();

        Assert.Equal(InputEventKind.Paste, ev.Kind);
        Assert.Equal(string.Empty, ev.Paste);
    }
}
