namespace Retro.Crt.Tests;

public class ColorParseTests
{
    [Theory]
    [InlineData("#FF8800", 255, 136, 0)]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("ff8800",  255, 136, 0)]   // no leading hash
    [InlineData("FF8800",  255, 136, 0)]   // upper case
    [InlineData("#aBcDeF", 0xAB, 0xCD, 0xEF)]
    public void TryFromHex_accepts_six_digit_form(string input, int r, int g, int b)
    {
        Assert.True(Color.TryFromHex(input, out var color));
        Assert.Equal(ColorMode.Truecolor, color.Mode);
        Assert.Equal((byte)r, color.R);
        Assert.Equal((byte)g, color.G);
        Assert.Equal((byte)b, color.B);
    }

    [Theory]
    [InlineData("#F80", 255, 136, 0)]
    [InlineData("#000", 0, 0, 0)]
    [InlineData("#FFF", 255, 255, 255)]
    [InlineData("F80",  255, 136, 0)]
    public void TryFromHex_accepts_three_digit_form(string input, int r, int g, int b)
    {
        Assert.True(Color.TryFromHex(input, out var color));
        Assert.Equal((byte)r, color.R);
        Assert.Equal((byte)g, color.G);
        Assert.Equal((byte)b, color.B);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("#")]
    [InlineData("#XYZXYZ")]
    [InlineData("#FFFF")]            // 4 chars, not 3 or 6
    [InlineData("orange")]
    public void TryFromHex_rejects_garbage(string? input)
    {
        Assert.False(Color.TryFromHex(input, out _));
    }

    [Fact]
    public void FromHex_throws_on_invalid()
    {
        Assert.Throws<FormatException>(() => Color.FromHex("nope"));
    }

    [Theory]
    [InlineData("LightCyan",    11)]
    [InlineData("lightcyan",    11)]
    [InlineData("LIGHTCYAN",    11)]
    [InlineData(" lightcyan ",  11)]   // whitespace trimmed
    [InlineData("Black",         0)]
    [InlineData("White",        15)]
    [InlineData("Brown",         6)]
    [InlineData("DarkMagenta",   5)]
    public void TryFromName_accepts_palette_names_case_insensitive(string name, int expectedIndex)
    {
        Assert.True(Color.TryFromName(name, out var color));
        Assert.Equal(ColorMode.Standard16, color.Mode);
        Assert.Equal((byte)expectedIndex, color.Index);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("orange")]
    [InlineData("rgb(1,2,3)")]
    public void TryFromName_rejects_unknown(string? name)
    {
        Assert.False(Color.TryFromName(name, out _));
    }

    [Theory]
    [InlineData("#FF8800",   ColorMode.Truecolor)]
    [InlineData("FF8800",    ColorMode.Truecolor)]
    [InlineData("LightCyan", ColorMode.Standard16)]
    public void TryParse_dispatches_by_format(string input, ColorMode expected)
    {
        Assert.True(Color.TryParse(input, out var color));
        Assert.Equal(expected, color.Mode);
    }

    [Fact]
    public void TryParse_prefers_hex_when_input_starts_with_hash()
    {
        Assert.True(Color.TryParse("#FF8800", out var color));
        Assert.Equal(ColorMode.Truecolor, color.Mode);
    }

    [Fact]
    public void TryParse_falls_back_to_bare_hex_when_name_lookup_fails()
    {
        Assert.True(Color.TryParse("FF8800", out var color));
        Assert.Equal(ColorMode.Truecolor, color.Mode);
    }

    [Fact]
    public void TryParse_rejects_completely_unknown_input()
    {
        Assert.False(Color.TryParse("not-a-color", out _));
    }
}
