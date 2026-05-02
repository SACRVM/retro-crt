using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class LogIntegrationTests
{
    [Fact]
    public void Info_goes_to_stdout()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Info("ready");

        Assert.Contains("INFO", c.Out);
        Assert.Contains("ready", c.Out);
        Assert.Equal(string.Empty, c.Err);
    }

    [Fact]
    public void Warn_goes_to_stderr()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Warn("careful");

        Assert.Contains("WARN", c.Err);
        Assert.Contains("careful", c.Err);
        Assert.Equal(string.Empty, c.Out);
    }

    [Fact]
    public void Error_goes_to_stderr()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Error("nope");

        Assert.Contains("ERROR", c.Err);
        Assert.Contains("nope", c.Err);
    }

    [Fact]
    public void Debug_and_success_go_to_stdout()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Debug("trace");
        Log.Success("good");

        Assert.Contains("DEBUG", c.Out);
        Assert.Contains("OK   ", c.Out);
        Assert.Equal(string.Empty, c.Err);
    }

    [Fact]
    public void Format_is_time_two_spaces_tag_two_spaces_message()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Info("ready");

        // HH:MM:SS  TAG   message  followed by newline
        var line = c.Out.TrimEnd('\n');
        Assert.Matches(@"^\d{2}:\d{2}:\d{2}  INFO   ready$", line);
    }

    [Fact]
    public void With_ansi_level_tag_is_colored_and_bold()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Log.Info("ready");

        // Foreground for Info -> LightCyan -> SGR 96, plus bold (1).
        Assert.Contains("\x1b[96m", c.Out);
        Assert.Contains("\x1b[1m", c.Out);
        Assert.Contains("\x1b[0m", c.Out);
    }

    [Fact]
    public void Error_color_is_lightred()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        Log.Error("boom");

        Assert.Contains("\x1b[91m", c.Err);
    }

    [Fact]
    public void Without_ansi_no_escapes_appear()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Info("hello");
        Log.Warn("careful");

        Assert.DoesNotContain("\x1b", c.Out);
        Assert.DoesNotContain("\x1b", c.Err);
    }

    [Fact]
    public void Write_with_explicit_level_routes_correctly()
    {
        using var c = ConsoleCapture.Start(ansi: false);

        Log.Write(LogLevel.Warn, "message");

        Assert.Contains("WARN", c.Err);
    }
}
