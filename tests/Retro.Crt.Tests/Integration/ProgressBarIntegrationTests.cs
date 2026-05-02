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
}
