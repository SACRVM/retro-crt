namespace Retro.Crt.Tests;

public class ThemeTests
{
    [Fact]
    public void All_themes_have_non_empty_name()
    {
        foreach (var t in Themes.All)
            Assert.False(string.IsNullOrWhiteSpace(t.Name), $"Theme {t} has empty Name");
    }

    [Fact]
    public void All_themes_use_truecolor_for_every_slot()
    {
        foreach (var t in Themes.All)
        {
            Assert.Equal(ColorMode.Truecolor, t.Background.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Foreground.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Accent.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Muted.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Success.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Warn.Mode);
            Assert.Equal(ColorMode.Truecolor, t.Error.Mode);
        }
    }

    [Fact]
    public void Theme_names_are_unique()
    {
        var names = Themes.All.Select(t => t.Name).ToList();
        var distinct = names.Distinct().ToList();
        Assert.Equal(names.Count, distinct.Count);
    }

    [Fact]
    public void All_array_includes_every_named_preset()
    {
        // Sanity: someone added a preset and forgot to register it.
        Assert.Contains(Themes.Dos, Themes.All);
        Assert.Contains(Themes.AmberCrt, Themes.All);
        Assert.Contains(Themes.GreenCrt, Themes.All);
        Assert.Contains(Themes.Amiga, Themes.All);
        Assert.Contains(Themes.C64, Themes.All);
        Assert.Contains(Themes.NortonCommander, Themes.All);
        Assert.Equal(6, Themes.All.Length);
    }

    [Fact]
    public void Themes_are_records_and_compare_by_value()
    {
        // Useful for tests / DI overrides — a copied theme equals its source.
        var clone = Themes.Dos with { };
        Assert.Equal(Themes.Dos, clone);

        var tweaked = Themes.Dos with { Accent = Color.Rgb(1, 2, 3) };
        Assert.NotEqual(Themes.Dos, tweaked);
    }

    [Fact]
    public void AmberCrt_has_orange_dominated_palette()
    {
        // Sanity-check the era: amber phosphor must read warm.
        var t = Themes.AmberCrt;
        Assert.True(t.Foreground.R >= t.Foreground.B, "amber foreground should be red-leaning");
        Assert.True(t.Foreground.G < t.Foreground.R,  "amber foreground should not be green-dominant");
    }

    [Fact]
    public void GreenCrt_has_green_dominated_palette()
    {
        var t = Themes.GreenCrt;
        Assert.True(t.Foreground.G >= t.Foreground.R, "green CRT foreground should be green-leaning");
        Assert.True(t.Foreground.G >= t.Foreground.B, "green CRT foreground should be green-leaning");
    }
}
