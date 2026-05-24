using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class RuleIntegrationTests
{
    [Fact]
    public void Titled_rule_centers_the_label_and_applies_color()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Rule.Print("THE FISHBOWL", Color.LightCyan, width: 30);

        // Box-drawing line glyph + the centered title.
        Assert.Contains("─", c.Out);
        Assert.Contains("THE FISHBOWL", c.Out);
        // LightCyan == SGR 96.
        Assert.Contains("\x1b[96m", c.Out);
    }

    [Fact]
    public void Untitled_rule_is_a_plain_divider()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        Rule.Print(width: 10);

        Assert.Contains("──────────", c.Out);
        Assert.DoesNotContain("THE", c.Out);
    }

    [Fact]
    public void Rule_falls_back_to_ascii_without_unicode()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: false);

        Rule.Print("X", width: 8);

        Assert.Contains("-", c.Out);
        Assert.DoesNotContain("─", c.Out);
    }

    [Fact]
    public void Rule_without_color_emits_no_escapes()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Rule.Print("X", width: 8);

        Assert.DoesNotContain("\x1b", c.Out);
        Assert.Contains("X", c.Out);
    }
}
