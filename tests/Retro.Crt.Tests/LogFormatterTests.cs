using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class LogFormatterTests
{
    [Theory]
    [InlineData(LogLevel.Info,    "INFO ")]
    [InlineData(LogLevel.Warn,    "WARN ")]
    [InlineData(LogLevel.Error,   "ERROR")]
    [InlineData(LogLevel.Debug,   "DEBUG")]
    [InlineData(LogLevel.Success, "OK   ")]
    public void Tag_is_fixed_width_five_chars(LogLevel level, string expected)
    {
        var tag = LogFormatter.Tag(level);
        Assert.Equal(expected, tag);
        Assert.Equal(5, tag.Length);
    }

    [Fact]
    public void FormatTime_pads_components_to_two_digits()
    {
        var s = LogFormatter.FormatTime(new TimeSpan(3, 7, 9));
        Assert.Equal("03:07:09", s);
    }

    [Fact]
    public void Format_lays_out_time_two_spaces_tag_two_spaces_message()
    {
        var s = LogFormatter.Format(LogLevel.Info, "ready", new TimeSpan(12, 34, 56));
        Assert.Equal("12:34:56  INFO   ready", s);
    }
}
