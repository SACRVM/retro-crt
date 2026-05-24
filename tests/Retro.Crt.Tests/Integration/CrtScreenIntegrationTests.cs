using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class CrtScreenIntegrationTests
{
    [Fact]
    public void Bell_emits_BEL_when_interactive()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        Crt.Bell();

        Assert.Contains("\a", c.Out);
    }

    [Fact]
    public void Bell_is_silent_when_output_is_redirected()
    {
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        Crt.Bell();

        Assert.DoesNotContain("\a", c.Out);
    }

    [Fact]
    public void Bell_works_without_color_as_long_as_interactive()
    {
        // NO_COLOR strips SGR but still leaves a real terminal — the bell
        // is a pre-ANSI control char and must keep ringing in that mode.
        using var c = ConsoleCapture.Start(ansi: false, interactive: true);

        Crt.Bell();

        Assert.Contains("\a", c.Out);
    }

    [Fact]
    public void UseAlternateScreen_emits_enter_and_leave()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        using (Crt.UseAlternateScreen())
        {
            Crt.Write("payload");
        }

        Assert.Contains("\x1b[?1049h", c.Out);
        Assert.Contains("\x1b[?1049l", c.Out);
        // The enter must precede the payload, and leave must follow it.
        var enterIdx   = c.Out.IndexOf("\x1b[?1049h", StringComparison.Ordinal);
        var payloadIdx = c.Out.IndexOf("payload",     StringComparison.Ordinal);
        var leaveIdx   = c.Out.IndexOf("\x1b[?1049l", StringComparison.Ordinal);
        Assert.True(enterIdx < payloadIdx, "enter must come before payload");
        Assert.True(payloadIdx < leaveIdx, "leave must come after payload");
    }

    [Fact]
    public void UseAlternateScreen_is_noop_when_not_interactive()
    {
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        using (Crt.UseAlternateScreen())
            Crt.Write("payload");

        Assert.DoesNotContain("\x1b[?1049", c.Out);
        Assert.Contains("payload", c.Out);
    }

    [Fact]
    public void UseAlternateScreen_nests_by_reference_count()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        using (Crt.UseAlternateScreen())
        {
            using (Crt.UseAlternateScreen())
            {
                Crt.Write("inner");
            }
            Crt.Write("middle");
        }

        // Only the outermost transition should flip the buffer: exactly
        // one enter and exactly one leave end up on the wire.
        var enterCount = CountOccurrences(c.Out, "\x1b[?1049h");
        var leaveCount = CountOccurrences(c.Out, "\x1b[?1049l");
        Assert.Equal(1, enterCount);
        Assert.Equal(1, leaveCount);
    }

    [Fact]
    public void UseAlternateScreen_dispose_is_idempotent()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        var scope = Crt.UseAlternateScreen();
        scope.Dispose();
        scope.Dispose();

        Assert.Equal(1, CountOccurrences(c.Out, "\x1b[?1049l"));
    }

    [Fact]
    public void PaintBackground_writes_one_blank_per_visible_row_and_returns_cursor_to_origin()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        Crt.PaintBackground();

        // Cursor must be parked at (1, 1) at the end so subsequent
        // writes start in the top-left of the painted area.
        Assert.EndsWith("\x1b[1;1H", c.Out);
        // The first emitted escape is also a goto-(1,1) so the fill
        // begins from the origin even if the cursor sat elsewhere.
        Assert.StartsWith("\x1b[1;1H", c.Out);
        // Spaces dominate the output — at least one blank row's worth.
        Assert.Contains(new string(' ', 60), c.Out);
    }

    [Fact]
    public void PaintBackground_renders_under_an_explicit_bg()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        using (Crt.WithStyle(bg: Color.Rgb(20, 10, 0)))
            Crt.PaintBackground();

        // The explicit WithStyle bg must be in the stream and
        // PaintBackground must follow with at least one row of spaces
        // under that active SGR.
        Assert.Contains("\x1b[48;2;20;10;0m", c.Out);
        Assert.Contains(new string(' ', 60), c.Out);
    }

    [Fact]
    public void PaintBackground_is_noop_when_color_is_disabled()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Crt.PaintBackground();

        Assert.DoesNotContain("\x1b", c.Out);
        Assert.Equal(string.Empty, c.Out);
    }

    [Fact]
    public void SetWindowTitle_emits_osc_when_interactive()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        Crt.SetWindowTitle("Husky: app");

        Assert.Contains("\x1b]2;Husky: app\a", c.Out);
    }

    [Fact]
    public void SetWindowTitle_is_noop_when_not_interactive()
    {
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        Crt.SetWindowTitle("Husky: app");

        Assert.DoesNotContain("\x1b]", c.Out);
    }

    [Fact]
    public void UseWindowTitle_pushes_sets_and_pops_around_the_scope()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        using (Crt.UseWindowTitle("inner"))
            Crt.Write("payload");

        var pushIdx    = c.Out.IndexOf("\x1b[22;0t",     StringComparison.Ordinal);
        var titleIdx   = c.Out.IndexOf("\x1b]2;inner\a", StringComparison.Ordinal);
        var payloadIdx = c.Out.IndexOf("payload",        StringComparison.Ordinal);
        var popIdx     = c.Out.IndexOf("\x1b[23;0t",     StringComparison.Ordinal);

        Assert.True(pushIdx >= 0 && titleIdx > pushIdx, "push must precede the title set");
        Assert.True(titleIdx < payloadIdx, "title must be set before the payload");
        Assert.True(payloadIdx < popIdx,  "pop must follow the payload on dispose");
    }

    [Fact]
    public void UseWindowTitle_is_noop_when_not_interactive()
    {
        using var c = ConsoleCapture.Start(ansi: false, interactive: false);

        using (Crt.UseWindowTitle("inner"))
            Crt.Write("payload");

        Assert.DoesNotContain("\x1b[22;0t", c.Out);
        Assert.DoesNotContain("\x1b[23;0t", c.Out);
        Assert.Contains("payload", c.Out);
    }

    [Fact]
    public void UseWindowTitle_nested_scopes_pop_in_lifo_order()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        using (Crt.UseWindowTitle("outer"))
        using (Crt.UseWindowTitle("inner"))
            Crt.Write("payload");

        // Two pushes and two pops — one per scope.
        Assert.Equal(2, CountOccurrences(c.Out, "\x1b[22;0t"));
        Assert.Equal(2, CountOccurrences(c.Out, "\x1b[23;0t"));
    }

    [Fact]
    public void UseWindowTitle_dispose_is_idempotent()
    {
        using var c = ConsoleCapture.Start(ansi: true, interactive: true);

        var scope = Crt.UseWindowTitle("inner");
        scope.Dispose();
        scope.Dispose();

        Assert.Equal(1, CountOccurrences(c.Out, "\x1b[23;0t"));
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
