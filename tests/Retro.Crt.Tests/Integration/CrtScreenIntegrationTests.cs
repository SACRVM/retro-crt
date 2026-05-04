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
