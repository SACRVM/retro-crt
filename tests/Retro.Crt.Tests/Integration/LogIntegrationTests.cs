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

    [Fact]
    public void MinLevel_drops_lines_below_threshold()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        var previous = Log.MinLevel;
        try
        {
            Log.MinLevel = LogLevel.Warn;

            Log.Debug("trace");
            Log.Info("ready");
            Log.Success("ok");
            Log.Warn("careful");
            Log.Error("boom");
        }
        finally { Log.MinLevel = previous; }

        // Below-threshold lines must not appear anywhere.
        Assert.DoesNotContain("DEBUG", c.Out + c.Err);
        Assert.DoesNotContain("INFO",  c.Out + c.Err);
        Assert.DoesNotContain("OK   ", c.Out + c.Err);
        // At-or-above-threshold lines must.
        Assert.Contains("WARN",  c.Err);
        Assert.Contains("ERROR", c.Err);
    }

    [Fact]
    public void IsEnabled_matches_MinLevel_threshold()
    {
        var previous = Log.MinLevel;
        try
        {
            Log.MinLevel = LogLevel.Warn;

            Assert.False(Log.IsEnabled(LogLevel.Debug));
            Assert.False(Log.IsEnabled(LogLevel.Info));
            Assert.False(Log.IsEnabled(LogLevel.Success));
            Assert.True(Log.IsEnabled(LogLevel.Warn));
            Assert.True(Log.IsEnabled(LogLevel.Error));
        }
        finally { Log.MinLevel = previous; }
    }

    [Fact]
    public void OutSink_redirects_info_lines_to_a_textwriter()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        using var custom = new StringWriter();
        var previous = Log.OutSink;
        try
        {
            Log.OutSink = custom;

            Log.Info("hello");
        }
        finally { Log.OutSink = previous; }

        // Info routed to the override, NOT to the captured Console.Out.
        Assert.Contains("INFO", custom.ToString());
        Assert.Contains("hello", custom.ToString());
        Assert.DoesNotContain("INFO", c.Out);
    }

    [Fact]
    public void ErrSink_redirects_error_lines_to_a_textwriter()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        using var custom = new StringWriter();
        var previous = Log.ErrSink;
        try
        {
            Log.ErrSink = custom;

            Log.Error("boom");
        }
        finally { Log.ErrSink = previous; }

        Assert.Contains("ERROR", custom.ToString());
        Assert.DoesNotContain("ERROR", c.Err);
    }

    [Fact]
    public void UseSink_routes_both_streams_for_the_scope()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        using var sink = new StringWriter();

        using (Log.UseSink(sink))
        {
            Log.Info("ready");
            Log.Error("boom");
        }

        Assert.Contains("INFO",  sink.ToString());
        Assert.Contains("ERROR", sink.ToString());
        Assert.DoesNotContain("INFO",  c.Out);
        Assert.DoesNotContain("ERROR", c.Err);

        // After the scope, Console redirection is back in effect.
        Log.Info("after");
        Assert.Contains("INFO", c.Out);
    }

    [Fact]
    public void UseSink_nests_and_restores()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        using var outer = new StringWriter();
        using var inner = new StringWriter();

        using (Log.UseSink(outer))
        {
            Log.Info("o1");
            using (Log.UseSink(inner))
                Log.Info("inner");
            Log.Info("o2");
        }

        Assert.Contains("o1", outer.ToString());
        Assert.Contains("o2", outer.ToString());
        Assert.Contains("inner", inner.ToString());
        Assert.DoesNotContain("inner", outer.ToString());
    }
}
