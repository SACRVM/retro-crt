using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class ProgressBarIntegrationTests
{
    [Fact]
    public void Animated_start_writes_hide_cursor_and_initial_frame()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 10, label: null, color: null);

        Assert.Contains("\x1b[?25l", c.Out);     // hide cursor
        Assert.Contains("\r", c.Out);             // initial CR
        Assert.Contains("░░░░░░░░░░", c.Out);     // empty bar
    }

    [Fact]
    public void FillWidth_resolves_to_a_positive_clamped_width()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        // The actual terminal width during tests can be anything (or
        // unmeasurable → DefaultWidth). We don't pin a number; we just
        // verify the bar renders and stays within MaxWidth.
        using var bar = ProgressBar.Start(
            total: 100,
            width: Crt.FillWidth,
            label: "load",
            showPercent: true);

        // CR + bar render. The bar must contain at least one fill cell
        // worth of empty glyph and never exceed MaxWidth glyphs in a row.
        Assert.Contains("░", c.Out);
        var streak = LongestEmptyStreak(c.Out);
        Assert.InRange(streak, 1, 200); // ProgressBarRenderer.MaxWidth = 200
    }

    private static int LongestEmptyStreak(string s)
    {
        var best = 0;
        var run = 0;
        foreach (var ch in s)
        {
            if (ch == '░') { run++; if (run > best) best = run; }
            else run = 0;
        }
        return best;
    }

    [Fact]
    public void Anchored_redraw_emits_indent_after_carriage_return()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        ConsoleCapture.OverrideCursorLeft(7);
        try
        {
            using var bar = ProgressBar.Start(total: 100, width: 5);
            bar.Set(50);
        }
        finally { ConsoleCapture.OverrideCursorLeft(null); }

        // Each redraw is "\r" + 7 spaces + frame. We don't pin the exact
        // count, but every CR in the stream must be followed by exactly
        // seven spaces before any non-space char.
        AssertEveryCarriageReturnPrefixedBySpaces(c.Out, expectedSpaces: 7);
    }

    [Fact]
    public void Unanchored_redraw_uses_plain_carriage_return()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        // No cursor override → CursorLeft falls back to 0 in tests.

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Set(50);

        // No indent should be emitted after the CR — the byte right
        // after every CR must NOT be a space.
        AssertCarriageReturnNotFollowedBySpace(c.Out);
    }

    private static void AssertEveryCarriageReturnPrefixedBySpaces(string s, int expectedSpaces)
    {
        var i = 0;
        while ((i = s.IndexOf('\r', i)) >= 0)
        {
            for (var j = 0; j < expectedSpaces; j++)
            {
                var pos = i + 1 + j;
                Assert.True(pos < s.Length, "CR has no payload after it");
                Assert.True(s[pos] == ' ',
                    $"CR at {i} expected space at offset {j+1}, got '{s[pos]}'");
            }
            i++;
        }
    }

    private static void AssertCarriageReturnNotFollowedBySpace(string s)
    {
        var i = 0;
        while ((i = s.IndexOf('\r', i)) >= 0)
        {
            if (i + 1 < s.Length)
                Assert.True(s[i + 1] != ' ',
                    $"unanchored CR at {i} should not be followed by an indent space");
            i++;
        }
    }

    [Fact]
    public void Animated_dispose_writes_show_cursor_and_newline()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var bar = ProgressBar.Start(total: 100, width: 10);
        bar.Dispose();

        Assert.Contains("\x1b[?25h", c.Out);     // show cursor
        Assert.EndsWith("\n", c.Out);
    }

    [Fact]
    public void Set_throttles_redraw_when_filled_cells_unchanged()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 1000, width: 10);

        // Capture the length after the initial frame.
        var initial = c.Out.Length;

        // 0..9 is still 0 cells filled and 0%, so no redraw.
        for (long v = 1; v < 10; v++) bar.Set(v);

        Assert.Equal(initial, c.Out.Length);
    }

    [Fact]
    public void Set_redraws_when_percent_changes()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 10, showPercent: true);

        bar.Set(50);

        Assert.Contains(" 50%", c.Out);
    }

    [Fact]
    public void Final_frame_has_full_fill()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var bar = ProgressBar.Start(total: 100, width: 5, showPercent: true);
        bar.Dispose();

        Assert.Contains("█████", c.Out);
        Assert.Contains("100%", c.Out);
    }

    [Fact]
    public void Non_animated_emits_no_intermediate_frames()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        using (var bar = ProgressBar.Start(total: 100, width: 5))
        {
            bar.Set(25);
            bar.Set(50);
            bar.Set(75);
        }

        // Only one final frame should appear, terminated by a single newline.
        var lines = c.Out.Split('\n');
        Assert.Equal(2, lines.Length);   // "frame" + ""
        // No CR (would mean an in-place redraw was emitted).
        Assert.DoesNotContain("\r", c.Out);
        // The bar shows 100% — five filled cells either as # (ascii) or █.
        Assert.True(lines[0].Contains("#####", StringComparison.Ordinal)
                 || lines[0].Contains("█████", StringComparison.Ordinal));
    }

    [Fact]
    public void Non_animated_uses_ascii_glyph_when_unicode_off()
    {
        using var c = ConsoleCapture.Start(ansi: false, unicode: false);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Dispose();

        Assert.Contains("#####", c.Out);
    }

    [Fact]
    public void Non_animated_emits_no_ansi_escapes()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Set(50);

        Assert.DoesNotContain("\x1b", c.Out);
    }

    [Fact]
    public void Negative_set_clamps_to_zero()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Set(-10);

        Assert.Equal(0, bar.Value);
    }

    [Fact]
    public void Overflow_set_clamps_to_total()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Set(999);

        Assert.Equal(100, bar.Value);
    }

    [Fact]
    public void Tick_advances_by_delta()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Tick(40);
        bar.Tick(10);

        Assert.Equal(50, bar.Value);
    }

    [Fact]
    public void Color_emits_truecolor_then_reset_per_frame()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var bar = ProgressBar.Start(total: 100, width: 5, color: Color.Rgb(100, 200, 50));
        bar.Set(50);

        Assert.Contains("\x1b[38;2;100;200;50m", c.Out);
        Assert.Contains("\x1b[0m", c.Out);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Dispose();
        var lengthAfterFirst = c.Out.Length;
        bar.Dispose();

        Assert.Equal(lengthAfterFirst, c.Out.Length);
    }

    [Fact]
    public void Interactive_terminal_without_color_still_animates()
    {
        // NO_COLOR (or similar) on a real TTY: the user wants no SGR,
        // but the bar should still redraw in place via CR.
        using var c = ConsoleCapture.Start(ansi: false, interactive: true);

        using var bar = ProgressBar.Start(total: 100, width: 5);
        bar.Set(50);
        bar.Set(100);

        // Multiple CRs prove the bar redrew in place rather than
        // emitting one final line.
        var crCount = c.Out.Count(ch => ch == '\r');
        Assert.True(crCount >= 2, $"expected at least 2 CRs (animation), saw {crCount}");

        // No SGR escapes: NO_COLOR semantics still hold.
        Assert.DoesNotContain("\x1b[38", c.Out); // foreground SGR
        Assert.DoesNotContain("\x1b[0m", c.Out); // reset
    }

    [Fact]
    public void Non_interactive_pipe_emits_only_final_frame()
    {
        // Output redirected to a file: don't spam intermediate frames.
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        using (var bar = ProgressBar.Start(total: 100, width: 5))
        {
            bar.Set(25);
            bar.Set(50);
            bar.Set(75);
        }

        // Exactly one frame should appear (the final 100% one), with a
        // trailing newline. No CRs at all.
        Assert.DoesNotContain("\r", c.Out);
        Assert.Contains("100%", c.Out);
    }
}
