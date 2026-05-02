using System.Globalization;

namespace Retro.Crt.Internals;

/// <summary>
/// Pure formatter for log lines. Emits <c>HH:MM:SS  LEVEL  message</c> with
/// a fixed five-char level tag so columns line up across calls.
/// </summary>
internal static class LogFormatter
{
    public static string Tag(LogLevel level) => level switch
    {
        LogLevel.Info    => "INFO ",
        LogLevel.Warn    => "WARN ",
        LogLevel.Error   => "ERROR",
        LogLevel.Debug   => "DEBUG",
        LogLevel.Success => "OK   ",
        _                => "?????",
    };

    public static string FormatTime(TimeSpan time)
    {
        // 24h, fixed width, invariant.
        var hh = ((int)time.Hours).ToString("D2", CultureInfo.InvariantCulture);
        var mm = ((int)time.Minutes).ToString("D2", CultureInfo.InvariantCulture);
        var ss = ((int)time.Seconds).ToString("D2", CultureInfo.InvariantCulture);
        return hh + ":" + mm + ":" + ss;
    }

    public static string Format(LogLevel level, string message, TimeSpan time)
        => FormatTime(time) + "  " + Tag(level) + "  " + message;
}
