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
}
