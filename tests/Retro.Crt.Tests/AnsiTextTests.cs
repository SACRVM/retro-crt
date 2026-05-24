using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class AnsiTextTests
{
    [Fact]
    public void Plain_text_counts_every_char()
    {
        Assert.Equal(5, AnsiText.VisibleWidth("hello"));
    }

    [Fact]
    public void Sgr_sequences_do_not_count()
    {
        // "\x1b[92mok\x1b[0m" → visible "ok".
        Assert.Equal(2, AnsiText.VisibleWidth("\x1b[92mok\x1b[0m"));
    }

    [Fact]
    public void Osc_8_hyperlink_counts_only_the_label()
    {
        var link = AnsiCodes.Hyperlink("docs", "https://example.com");
        Assert.Equal(4, AnsiText.VisibleWidth(link));
    }

    [Fact]
    public void Bare_two_char_escape_is_skipped()
    {
        // DECSC (ESC 7) then "X".
        Assert.Equal(1, AnsiText.VisibleWidth("\x1b" + "7X"));
    }

    [Fact]
    public void Osc_terminated_by_st_is_skipped()
    {
        // OSC ... ST (ESC backslash) then visible "Y".
        var s = "\x1b]2;title\x1b\\Y";
        Assert.Equal(1, AnsiText.VisibleWidth(s));
    }
}
