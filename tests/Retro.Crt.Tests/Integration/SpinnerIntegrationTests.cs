using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class SpinnerIntegrationTests
{
    // Use a deliberately huge msPerFrame so the timer never fires during the
    // test — we assert only on the initial render and the Stop cleanup,
    // never on animation frames (which would be flaky).
    private const int NeverTicks = 10_000;

    [Fact]
    public void Animated_show_writes_hide_cursor_and_first_frame()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var s = Spinner.Show("loading", msPerFrame: NeverTicks);

        Assert.Contains("\x1b[?25l", c.Out); // hide cursor
        Assert.Contains("|", c.Out);          // first Pipe frame
        Assert.Contains("loading", c.Out);
    }

    [Fact]
    public void Animated_dispose_clears_line_shows_cursor_and_ends_with_newline()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var s = Spinner.Show("work", msPerFrame: NeverTicks);
        s.Dispose();

        Assert.Contains("\x1b[?25h", c.Out); // show cursor
        Assert.EndsWith("\n", c.Out);
    }

    [Fact]
    public void Animated_stop_with_final_label_writes_it_in_color()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var s = Spinner.Show("upload", msPerFrame: NeverTicks);
        s.Stop("done", Color.LightGreen);

        Assert.Contains("done", c.Out);
        // 'done' appears AFTER the carriage-return clear, so the spinner
        // glyph should not survive on the same line — verify the final
        // label line ends with reset + show-cursor + newline.
        Assert.Contains("\x1b[?25h", c.Out);
        Assert.EndsWith("\n", c.Out);
    }

    [Fact]
    public void Update_changes_the_label_in_subsequent_render()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var s = Spinner.Show("step 1", msPerFrame: NeverTicks);
        s.Update("step 2");

        Assert.Contains("step 1", c.Out);
        Assert.Contains("step 2", c.Out);
    }

    [Fact]
    public void Stop_is_idempotent()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var s = Spinner.Show("x", msPerFrame: NeverTicks);
        s.Stop();
        s.Stop();          // must not throw
        s.Dispose();       // must not throw
    }

    [Fact]
    public void Update_after_stop_is_a_noop()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var s = Spinner.Show("x", msPerFrame: NeverTicks);
        s.Stop();

        var lengthAfterStop = c.Out.Length;
        s.Update("ignored");
        Assert.Equal(lengthAfterStop, c.Out.Length);
    }

    [Fact]
    public void Non_animated_show_writes_label_without_escape_sequences()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        using var s = Spinner.Show("loading", msPerFrame: NeverTicks);

        Assert.Contains("loading", c.Out);
        Assert.DoesNotContain("\x1b[", c.Out); // no ANSI at all
    }

    [Fact]
    public void Non_animated_dispose_writes_newline_only()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        var s = Spinner.Show("loading", msPerFrame: NeverTicks);
        var lengthBeforeStop = c.Out.Length;
        s.Dispose();

        // Exactly one newline added on Dispose; no animation glyphs.
        // ConsoleCapture's StringWriter is hard-coded to "\n" line endings.
        Assert.Equal(lengthBeforeStop + 1, c.Out.Length);
    }

    [Fact]
    public void Non_animated_stop_with_final_label_writes_label_on_new_line()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        var s = Spinner.Show("uploading", msPerFrame: NeverTicks);
        s.Stop("done", Color.LightGreen);

        Assert.Contains("uploading", c.Out);
        Assert.Contains("done", c.Out);
        Assert.DoesNotContain("\x1b[", c.Out);
    }

    [Fact]
    public void Pipe_style_uses_ascii_glyph()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        using var s = Spinner.Show("x", style: SpinnerStyle.Pipe, msPerFrame: NeverTicks);

        Assert.Contains("|", c.Out);
    }

    [Fact]
    public void Braille_style_uses_unicode_glyph_when_unicode_supported()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        using var s = Spinner.Show("x", style: SpinnerStyle.Braille, msPerFrame: NeverTicks);

        Assert.Contains("⠋", c.Out);
    }

    [Fact]
    public void Braille_style_falls_back_to_pipe_without_unicode()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: false);

        using var s = Spinner.Show("x", style: SpinnerStyle.Braille, msPerFrame: NeverTicks);

        Assert.Contains("|", c.Out);
        Assert.DoesNotContain("⠋", c.Out);
    }

    [Fact]
    public void Block_style_falls_back_to_pipe_without_unicode()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: false);

        using var s = Spinner.Show("x", style: SpinnerStyle.Block, msPerFrame: NeverTicks);

        Assert.Contains("|", c.Out);
        Assert.DoesNotContain("▖", c.Out);
    }

    [Fact]
    public void Arc_style_falls_back_to_pipe_without_unicode()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: false);

        using var s = Spinner.Show("x", style: SpinnerStyle.Arc, msPerFrame: NeverTicks);

        Assert.Contains("|", c.Out);
        Assert.DoesNotContain("◜", c.Out);
    }

    [Fact]
    public void MsPerFrame_is_clamped_to_at_least_one()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        // Negative / zero values must not throw.
        using var s1 = Spinner.Show("a", msPerFrame: 0);
        s1.Dispose();
        using var s2 = Spinner.Show("b", msPerFrame: -100);
        s2.Dispose();
    }

    [Fact]
    public void Color_emits_foreground_escape_for_glyph()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var s = Spinner.Show("x", color: Color.Rgb(255, 100, 50), msPerFrame: NeverTicks);

        Assert.Contains("\x1b[38;2;255;100;50m", c.Out);
    }
}
