using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class UseThemeIntegrationTests
{
    [Fact]
    public void UseTheme_emits_theme_foreground_and_background()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var t = Themes.AmberCrt;

        using (Crt.UseTheme(t))
        {
            // Just opening the scope should have emitted the theme's
            // fg + bg before any payload writes.
        }

        // Foreground SGR for theme.Foreground (255, 176, 0).
        Assert.Contains("\x1b[38;2;255;176;0m", c.Out);
        // Background SGR for theme.Background (20, 10, 0).
        Assert.Contains("\x1b[48;2;20;10;0m", c.Out);
    }

    [Fact]
    public void UseTheme_dispose_resets_styling()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using (Crt.UseTheme(Themes.AmberCrt))
        {
            Crt.Write("payload");
        }

        // After the scope, RESET must have been emitted; the very last
        // SGR-style escape in the stream is the reset.
        Assert.Contains("\x1b[0m", c.Out);
        Assert.EndsWith("\x1b[0m", c.Out);
    }

    [Fact]
    public void CurrentTheme_tracks_active_scope()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Assert.Null(Crt.CurrentTheme);

        using (Crt.UseTheme(Themes.AmberCrt))
        {
            Assert.Equal("AmberCrt", Crt.CurrentTheme?.Name);

            using (Crt.UseTheme(Themes.GreenCrt))
                Assert.Equal("GreenCrt", Crt.CurrentTheme?.Name);

            // Outer theme restored on inner dispose.
            Assert.Equal("AmberCrt", Crt.CurrentTheme?.Name);
        }

        Assert.Null(Crt.CurrentTheme);
    }

    [Fact]
    public void Banner_box_inside_theme_picks_accent_color()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var t = Themes.AmberCrt;

        using (Crt.UseTheme(t))
            Banner.Box("hi");

        // Accent (255, 220, 80) must appear at the start of the framed
        // body — the color is emitted via WithStyle just before the box
        // glyphs.
        Assert.Contains("\x1b[38;2;255;220;80m", c.Out);
    }

    [Fact]
    public void Banner_explicit_fg_wins_over_theme()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using (Crt.UseTheme(Themes.AmberCrt))
            Banner.Box("hi", fg: Color.Rgb(1, 2, 3));

        // The explicit fg must be emitted, not the theme's accent.
        Assert.Contains("\x1b[38;2;1;2;3m", c.Out);
        Assert.DoesNotContain("\x1b[38;2;255;220;80m", c.Out);
    }

    [Fact]
    public void Log_inside_theme_routes_levels_to_semantic_slots()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using (Crt.UseTheme(Themes.AmberCrt))
        {
            Log.Warn("careful");
            Log.Error("nope");
        }

        // Warn = (255, 140, 0), Error = (255, 60, 0).
        var combined = c.Out + c.Err;
        Assert.Contains("\x1b[38;2;255;140;0m", combined);
        Assert.Contains("\x1b[38;2;255;60;0m",  combined);
    }

    [Fact]
    public void Log_outside_theme_keeps_classic_palette()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Log.Warn("careful");

        // Yellow SGR (BIOS index 14) — this is the un-themed default.
        var combined = c.Out + c.Err;
        Assert.Contains("\x1b[93m", combined);
    }

    [Fact]
    public void ProgressBar_without_color_picks_theme_accent()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        using (Crt.UseTheme(Themes.AmberCrt))
        {
            using var bar = ProgressBar.Start(total: 100, width: 5);
            bar.Set(50);
        }

        // Accent (255, 220, 80) should appear in the bar's redraws.
        Assert.Contains("\x1b[38;2;255;220;80m", c.Out);
    }

    [Fact]
    public void Table_without_colors_picks_accent_for_header_and_muted_for_borders()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        using (Crt.UseTheme(Themes.AmberCrt))
            Table.Print(headers: ["A"], rows: [["1"]]);

        // Header color = Accent (255, 220, 80); border color = Muted (140, 90, 0).
        Assert.Contains("\x1b[38;2;255;220;80m", c.Out);
        Assert.Contains("\x1b[38;2;140;90;0m",  c.Out);
    }

    [Fact]
    public void UseTheme_without_ansi_only_tracks_state_no_escapes()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        using (Crt.UseTheme(Themes.AmberCrt))
        {
            Assert.NotNull(Crt.CurrentTheme);
            Crt.Write("hi");
        }

        Assert.DoesNotContain("\x1b", c.Out);
        Assert.Null(Crt.CurrentTheme);
    }
}
