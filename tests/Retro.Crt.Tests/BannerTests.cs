namespace Retro.Crt.Tests;

public class BannerTests
{
    [Fact]
    public void Interpolate_endpoints_match_inputs()
    {
        var from = Color.Rgb(0, 0, 0);
        var to   = Color.Rgb(255, 128, 64);

        var first = Banner.Interpolate(from, to, 0, 5);
        var last  = Banner.Interpolate(from, to, 4, 5);

        Assert.Equal(from, first);
        Assert.Equal(to, last);
    }

    [Fact]
    public void Interpolate_midpoint_is_halfway()
    {
        var from = Color.Rgb(0, 0, 0);
        var to   = Color.Rgb(200, 100, 50);

        var mid = Banner.Interpolate(from, to, 1, 3);

        Assert.Equal((byte)100, mid.R);
        Assert.Equal((byte)50,  mid.G);
        Assert.Equal((byte)25,  mid.B);
    }

    [Fact]
    public void Interpolate_single_step_returns_from()
    {
        var from = Color.Rgb(10, 20, 30);
        var to   = Color.Rgb(200, 100, 50);

        var only = Banner.Interpolate(from, to, 0, 1);

        Assert.Equal(from, only);
    }
}
