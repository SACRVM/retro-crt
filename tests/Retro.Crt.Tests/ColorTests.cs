namespace Retro.Crt.Tests;

public class ColorTests
{
    [Fact]
    public void Rgb_creates_truecolor()
    {
        var c = Color.Rgb(10, 20, 30);

        Assert.Equal(ColorMode.Truecolor, c.Mode);
        Assert.Equal((byte)10, c.R);
        Assert.Equal((byte)20, c.G);
        Assert.Equal((byte)30, c.B);
    }

    [Fact]
    public void Named_palette_uses_standard16()
    {
        Assert.Equal(ColorMode.Standard16, Color.LightCyan.Mode);
        Assert.Equal((byte)11, Color.LightCyan.Index);
        Assert.Equal((byte)0,  Color.Black.Index);
        Assert.Equal((byte)15, Color.White.Index);
    }

    [Fact]
    public void Two_colors_with_same_components_are_equal()
    {
        var a = Color.Rgb(255, 0, 0);
        var b = Color.Rgb(255, 0, 0);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Indexed256_creates_xterm256_with_index()
    {
        var c = Color.Indexed256(196);

        Assert.Equal(ColorMode.Xterm256, c.Mode);
        Assert.Equal((byte)196, c.Index);
    }

    [Theory]
    // The 16 BIOS palette slots carry their canonical RGB anchors so
    // .R/.G/.B reflect what the slot paints, not zero.
    [InlineData(0,    0,   0,   0)]   // Black
    [InlineData(14, 255, 255,  85)]   // Yellow
    [InlineData(15, 255, 255, 255)]   // White
    [InlineData(11,  85, 255, 255)]   // LightCyan
    [InlineData( 6, 170,  85,   0)]   // Brown
    public void Standard16_carries_bios_rgb(int index, int r, int g, int b)
    {
        var c = Color.Standard((byte)index);

        Assert.Equal((byte)r, c.R);
        Assert.Equal((byte)g, c.G);
        Assert.Equal((byte)b, c.B);
    }

    [Fact]
    public void For_truecolor_keeps_truecolor_unchanged()
    {
        var c = Color.Rgb(123, 45, 67);

        var adapted = c.For(ColorDepth.Truecolor);

        Assert.Equal(c, adapted);
    }

    [Fact]
    public void For_xterm256_quantizes_truecolor()
    {
        var pure_red = Color.Rgb(255, 0, 0);

        var adapted = pure_red.For(ColorDepth.Xterm256);

        Assert.Equal(ColorMode.Xterm256, adapted.Mode);
        // Pure red lands on cube entry 196 (16 + 36*5 + 6*0 + 0).
        Assert.Equal((byte)196, adapted.Index);
    }

    [Fact]
    public void For_xterm256_picks_grayscale_ramp_for_neutral_rgb()
    {
        var mid_gray = Color.Rgb(128, 128, 128);

        var adapted = mid_gray.For(ColorDepth.Xterm256);

        Assert.Equal(ColorMode.Xterm256, adapted.Mode);
        // 128 falls in the grayscale ramp 232..255 (gray steps 8..238).
        Assert.InRange(adapted.Index, (byte)232, (byte)255);
    }

    [Fact]
    public void For_standard16_quantizes_truecolor_to_nearest_bios_anchor()
    {
        var pure_red = Color.Rgb(255, 0, 0);

        var adapted = pure_red.For(ColorDepth.Standard16);

        Assert.Equal(ColorMode.Standard16, adapted.Mode);
        // Closest BIOS anchor to (255,0,0): DarkRed (4) at (170,0,0)
        // beats LightRed (12) at (255,85,85) on Euclidean distance —
        // 85² < 85²+85².
        Assert.Equal((byte)4, adapted.Index);
    }

    [Theory]
    [InlineData(255, 255, 255, 15)]   // pure white → White
    [InlineData(  0,   0,   0,  0)]   // pure black → Black
    [InlineData(255, 255,  85, 14)]   // exact Yellow anchor
    [InlineData( 80, 250,  80, 10)]   // close-to LightGreen
    public void For_standard16_anchors_round_trip(int r, int g, int b, int expectedIndex)
    {
        var c = Color.Rgb((byte)r, (byte)g, (byte)b);

        var adapted = c.For(ColorDepth.Standard16);

        Assert.Equal((byte)expectedIndex, adapted.Index);
    }

    [Fact]
    public void For_standard16_passes_through_standard16_unchanged()
    {
        var adapted = Color.LightCyan.For(ColorDepth.Standard16);

        Assert.Equal(Color.LightCyan, adapted);
    }

    [Fact]
    public void For_none_returns_default_sentinel()
    {
        var adapted = Color.LightCyan.For(ColorDepth.None);

        Assert.Equal(default(Color), adapted);
    }

    [Fact]
    public void QuantizeToStandard16_matches_For_path()
    {
        var idx = Color.QuantizeToStandard16(255, 0, 0);

        Assert.Equal((byte)4, idx);
    }

    [Fact]
    public void QuantizeToXterm256_returns_cube_or_grayscale_index()
    {
        // Pure red → cube entry 196.
        Assert.Equal((byte)196, Color.QuantizeToXterm256(255, 0, 0));

        // Mid gray → some grayscale-ramp entry in 232..255.
        var gray = Color.QuantizeToXterm256(128, 128, 128);
        Assert.InRange(gray, (byte)232, (byte)255);
    }
}
