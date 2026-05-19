using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class StickyFooterIntegrationTests
{
    //  is the ESC byte (0x1B). We avoid \x1b in test strings:
    // `\x` is greedy and would consume any following hex digit, so
    // "\x1b7" parses as the single code point 0x1B7, not ESC + '7'.

    [Fact]
    public void Start_argument_validation_rejects_zero_rows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StickyFooter.Start(0, _ => { }));
    }

    [Fact]
    public void Start_argument_validation_rejects_null_paint()
    {
        Assert.Throws<ArgumentNullException>(
            () => StickyFooter.Start(1, null!));
    }

    [Fact]
    public void Non_interactive_returns_disposed_noop_scope()
    {
        // Output redirected / dumb terminal — no escapes should ever
        // hit the sink, and Refresh must be a quiet no-op.
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        var paintCalls = 0;
        using var footer = StickyFooter.Start(2, _ => paintCalls++);
        footer.Refresh();

        Assert.Equal(0, paintCalls);
        Assert.DoesNotContain("", c.Out);
    }

    [Fact]
    public void Interactive_start_emits_scroll_region_setup_and_paints_once()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var paintCalls = 0;
        using var footer = StickyFooter.Start(2, buf =>
        {
            paintCalls++;
            buf.PutString(0, 0, "ready".AsSpan(), Color.White, Color.Black);
        });

        // Setup escapes: SU (scroll content up by N), DECSTBM
        // (top;bottom r), and a GotoXY parking the cursor at the
        // bottom of the scroll region.
        Assert.Contains("[2S", c.Out);
        Assert.Contains("[1;22r", c.Out); // 24 - 2 = 22 with the test default WindowHeight=24
        Assert.Equal(1, paintCalls);
        // Paint emits DECSC (ESC 7) and DECRC (ESC 8) around the footer.
        Assert.Contains("7", c.Out);
        Assert.Contains("8", c.Out);
        // Footer content lands in the buffer.
        Assert.Contains("ready", c.Out);
    }

    [Fact]
    public void Paint_buffer_is_sized_width_x_rows()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var observedWidth = 0;
        var observedHeight = 0;
        using var footer = StickyFooter.Start(3, buf =>
        {
            observedWidth  = buf.Width;
            observedHeight = buf.Height;
        });

        Assert.Equal(Crt.WindowWidth, observedWidth);
        Assert.Equal(3, observedHeight);
    }

    [Fact]
    public void Refresh_invokes_paint_again_and_emits_save_restore_cursor()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var paintCalls = 0;
        using var footer = StickyFooter.Start(1, _ => paintCalls++);

        // Initial Start paint counts as 1 — Refresh must add another.
        var before = paintCalls;
        var outBefore = c.Out.Length;
        footer.Refresh();

        Assert.Equal(before + 1, paintCalls);
        // Each Refresh emits its own DECSC/DECRC pair around the paint.
        var saveCount    = CountOccurrences(c.Out[outBefore..], "7");
        var restoreCount = CountOccurrences(c.Out[outBefore..], "8");
        Assert.Equal(1, saveCount);
        Assert.Equal(1, restoreCount);
    }

    [Fact]
    public void Dispose_resets_scroll_region()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var footer = StickyFooter.Start(2, _ => { });
        var outBefore = c.Out.Length;
        footer.Dispose();

        // ResetScrollRegion is bare CSI r — verify the suffix appears
        // after Dispose, not just from the SetScrollRegion at Start.
        Assert.Contains("[r", c.Out[outBefore..]);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var footer = StickyFooter.Start(2, _ => { });
        footer.Dispose();
        var afterFirst = c.Out.Length;
        footer.Dispose();

        Assert.Equal(afterFirst, c.Out.Length);
    }

    [Fact]
    public void Refresh_after_dispose_is_noop()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var paintCalls = 0;
        var footer = StickyFooter.Start(1, _ => paintCalls++);
        footer.Dispose();
        var afterDispose = paintCalls;
        var outAfterDispose = c.Out.Length;
        footer.Refresh();

        Assert.Equal(afterDispose, paintCalls);
        Assert.Equal(outAfterDispose, c.Out.Length);
    }

    [Fact]
    public void Footer_too_tall_for_terminal_returns_noop_scope()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        // Default test WindowHeight is 24; ask for a footer that
        // leaves no scroll region (rows == height) and the footer
        // should degrade to no-op instead of producing a bogus
        // DECSTBM like `ESC[1;0r`.
        using var footer = StickyFooter.Start(Crt.WindowHeight, _ => { });

        Assert.DoesNotContain("[1;0r", c.Out);
        Assert.DoesNotContain("[1;-", c.Out);
    }

    [Fact]
    public void Multiple_paints_inside_refresh_target_the_correct_rows()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using var footer = StickyFooter.Start(2, buf =>
        {
            buf.PutString(0, 0, "top".AsSpan(),    Color.White, Color.Black);
            buf.PutString(0, 1, "bottom".AsSpan(), Color.White, Color.Black);
        });

        // 24 rows total, 2 reserved → footer at rows 23 and 24.
        Assert.Contains("[23;1H", c.Out);
        Assert.Contains("[24;1H", c.Out);
        Assert.Contains("top",    c.Out);
        Assert.Contains("bottom", c.Out);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
