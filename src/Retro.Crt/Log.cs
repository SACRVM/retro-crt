using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Tiny semantic logger — not an <c>ILogger</c> replacement. Prints
/// <c>HH:MM:SS  LEVEL  message</c> with a colored level tag. Warn/Error go
/// to <see cref="Console.Error"/>; everything else to <see cref="Console.Out"/>.
/// </summary>
public static class Log
{
    public static void Info(string message)    => Write(LogLevel.Info,    message);
    public static void Warn(string message)    => Write(LogLevel.Warn,    message);
    public static void Error(string message)   => Write(LogLevel.Error,   message);
    public static void Debug(string message)   => Write(LogLevel.Debug,   message);
    public static void Success(string message) => Write(LogLevel.Success, message);

    /// <summary>Write a single log line at the given level.</summary>
    public static void Write(LogLevel level, string message)
    {
        var time = DateTime.Now.TimeOfDay;
        var sink = level is LogLevel.Warn or LogLevel.Error
            ? Console.Error
            : Console.Out;

        var prefix = LogFormatter.FormatTime(time) + "  ";
        var tag = LogFormatter.Tag(level);
        var suffix = "  " + message;

        sink.Write(prefix);
        if (Crt.ColorEnabled)
        {
            sink.Write(AnsiCodes.Foreground(ColorFor(level)));
            sink.Write(AnsiCodes.Bold);
            sink.Write(tag);
            sink.Write(AnsiCodes.Reset);
        }
        else
        {
            sink.Write(tag);
        }
        sink.WriteLine(suffix);
    }

    private static Color ColorFor(LogLevel level) => level switch
    {
        LogLevel.Info    => Color.LightCyan,
        LogLevel.Warn    => Color.Yellow,
        LogLevel.Error   => Color.LightRed,
        LogLevel.Debug   => Color.DarkGray,
        LogLevel.Success => Color.LightGreen,
        _                => Color.LightGray,
    };
}
