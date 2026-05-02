using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests;

[Collection(EnvMutatingCollection.Name)]
public class DiagnosticsTests
{
    [Fact]
    public void Capture_returns_current_capability_flags()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        var report = Diagnostics.Capture();

        Assert.True(report.SupportsAnsi);
        Assert.True(report.SupportsUnicode);
    }

    [Fact]
    public void Capture_reports_no_color_env()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        var report = Diagnostics.Capture();

        Assert.True(report.NoColorSet);
        Assert.False(report.SupportsAnsi);
    }

    [Fact]
    public void Capture_reports_force_color_env()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        var report = Diagnostics.Capture();

        Assert.True(report.ForceColorSet);
    }

    [Fact]
    public void ToString_packs_relevant_flags()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);

        var report = Diagnostics.Capture();
        var s = report.ToString();

        Assert.Contains("ansi=on", s);
        Assert.Contains("unicode=on", s);
        Assert.Contains("os=", s);
        Assert.Contains("enc=", s);
    }

    [Fact]
    public void ToString_marks_off_states()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        var report = Diagnostics.Capture();
        var s = report.ToString();

        Assert.Contains("ansi=off", s);
        Assert.Contains("NO_COLOR=set", s);
    }
}
