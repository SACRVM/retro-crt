using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class TypewriterIntegrationTests
{
    [Fact]
    public void Empty_text_writes_nothing()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("", msPerChar: 0);

        Assert.Equal(string.Empty, c.Out);
    }

    [Fact]
    public void Zero_pace_dumps_text_instantly_without_cursor_moves()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("hi", msPerChar: 0);

        Assert.Equal("hi", c.Out);
    }

    [Fact]
    public void Zero_pace_with_color_wraps_in_sgr_then_reset()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("ok", msPerChar: 0, fg: Color.LightCyan);

        Assert.Equal("\x1b[96mok\x1b[0m", c.Out);
    }

    [Fact]
    public void Animated_hides_and_restores_cursor()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("ok", msPerChar: 1);

        Assert.Contains("\x1b[?25l", c.Out);
        Assert.Contains("\x1b[?25h", c.Out);
        // Hide must come before show.
        Assert.True(c.Out.IndexOf("\x1b[?25l", StringComparison.Ordinal)
                  < c.Out.IndexOf("\x1b[?25h", StringComparison.Ordinal));
    }

    [Fact]
    public void Animated_emits_each_character()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("ab", msPerChar: 1);

        Assert.Contains("a", c.Out);
        Assert.Contains("b", c.Out);
    }

    [Fact]
    public void Cursor_block_emits_cursor_glyph_between_chars()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Typewriter.Type("ab", msPerChar: 1, cursor: TypewriterCursor.Block);

        Assert.Contains("▌", c.Out);
        // Cursor must have been erased: trailing CursorLeft + space + CursorLeft.
        var endsClean = !c.Out.TrimEnd().EndsWith('▌');
        Assert.True(endsClean);
    }

    [Fact]
    public void Cursor_underline_emits_underscore_between_chars()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("ab", msPerChar: 1, cursor: TypewriterCursor.Underline);

        Assert.Contains("_", c.Out);
    }

    [Fact]
    public void Newline_does_not_emit_cursor_glyph_after_it()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Typewriter.Type("a\nb", msPerChar: 1, cursor: TypewriterCursor.Block);

        // The exactly-one-cursor expectation: between 'a' and '\n', no cursor
        // after '\n', no cursor after 'b' (last char).
        var cursorCount = c.Out.Count(ch => ch == '▌');
        Assert.Equal(1, cursorCount);
    }

    [Fact]
    public void Alpha_fade_emits_four_color_frames_per_glyph()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("X", msPerChar: 4,
            fg: Color.Rgb(200, 100, 50),
            fade: TypewriterFade.Alpha);

        // Four foreground SGR sequences for the four ramp frames.
        var fgCount = c.Out.Split("\x1b[38;2;").Length - 1;
        Assert.Equal(4, fgCount);

        // Final frame must land on the target color.
        Assert.Contains("\x1b[38;2;200;100;50m", c.Out);
    }

    [Fact]
    public void Alpha_fade_uses_cursor_left_to_overwrite_previous_frame()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("X", msPerChar: 4,
            fg: Color.Rgb(200, 100, 50),
            fade: TypewriterFade.Alpha);

        // 3 cursor-left moves (between 4 frames).
        var leftCount = c.Out.Split("\x1b[D").Length - 1;
        Assert.Equal(3, leftCount);
    }

    [Fact]
    public void Alpha_fade_falls_back_to_no_fade_on_standard16()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("X", msPerChar: 4,
            fg: Color.LightCyan,
            fade: TypewriterFade.Alpha);

        // Only one foreground SGR — no four-frame ramp.
        var fgCount = c.Out.Split("\x1b[96m").Length - 1;
        Assert.Equal(1, fgCount);
    }

    [Fact]
    public void Without_ansi_animation_collapses_to_plain_text()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Typewriter.Type("hello", msPerChar: 50,
            cursor: TypewriterCursor.Block,
            fade: TypewriterFade.Alpha);

        Assert.Equal("hello", c.Out);
        Assert.DoesNotContain("\x1b", c.Out);
    }

    [Fact]
    public void TypeLine_appends_a_newline()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.TypeLine("ok", msPerChar: 0);

        Assert.EndsWith("\n", c.Out);
    }

    [Fact]
    public async Task TypeAsync_writes_the_text()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        await Typewriter.TypeAsync("ok", msPerChar: 1);

        Assert.Contains("o", c.Out);
        Assert.Contains("k", c.Out);
    }

    [Fact]
    public async Task TypeAsync_respects_cancellation_and_restores_cursor()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        using var cts = new CancellationTokenSource();

        // Cancel almost immediately so we abort mid-string.
        cts.CancelAfter(20);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Typewriter.TypeAsync(
                "this is a long enough string to not finish in 20ms",
                msPerChar: 100,
                cancellationToken: cts.Token));

        // Cursor must be re-shown despite cancellation.
        Assert.Contains("\x1b[?25l", c.Out);
        Assert.Contains("\x1b[?25h", c.Out);
    }

    [Fact]
    public async Task TypeLineAsync_appends_a_newline()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        await Typewriter.TypeLineAsync("hi", msPerChar: 0);

        Assert.EndsWith("\n", c.Out);
    }

    [Fact]
    public void Gradient_uses_per_char_truecolor()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Typewriter.Type("AB", msPerChar: 0,
            gradient: (Color.Rgb(0, 0, 0), Color.Rgb(255, 255, 255)));

        Assert.Contains("\x1b[38;2;0;0;0m", c.Out);
        Assert.Contains("\x1b[38;2;255;255;255m", c.Out);
    }
}
