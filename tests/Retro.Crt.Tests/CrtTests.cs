namespace Retro.Crt.Tests;

public class CrtTests
{
    [Fact]
    public void WindowWidth_returns_a_positive_value_or_default()
    {
        var w = Crt.WindowWidth;

        // Either the host reports a real width or we get the documented
        // fallback. Never zero or negative.
        Assert.True(w > 0, $"WindowWidth must be > 0, got {w}");
    }

    [Fact]
    public void WindowHeight_returns_a_positive_value_or_default()
    {
        var h = Crt.WindowHeight;

        Assert.True(h > 0, $"WindowHeight must be > 0, got {h}");
    }

    [Fact]
    public void DefaultWidth_is_80()
    {
        Assert.Equal(80, Crt.DefaultWidth);
    }

    [Fact]
    public void DefaultHeight_is_24()
    {
        Assert.Equal(24, Crt.DefaultHeight);
    }

    [Fact]
    public void FillWidth_is_negative_one_sentinel()
    {
        Assert.Equal(-1, Crt.FillWidth);
    }
}
