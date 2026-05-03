using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Tiny semantic logger — not an <c>ILogger</c> replacement. Prints
/// <c>HH:MM:SS  LEVEL  message</c> with a colored level tag. Warn/Error go
/// to <see cref="Console.Error"/>; everything else to <see cref="Console.Out"/>,
/// unless redirected via <see cref="OutSink"/> / <see cref="ErrSink"/>.
/// </summary>
public static class Log
{
    /// <summary>
    /// Lines below this level are dropped on the floor. Defaults to
    /// <see cref="LogLevel.Debug"/> (everything passes). Set to
    /// <see cref="LogLevel.Warn"/> in production / quiet modes.
    /// </summary>
    public static LogLevel MinLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Override for the <see cref="LogLevel.Info"/>, <see cref="LogLevel.Debug"/>,
    /// and <see cref="LogLevel.Success"/> destination. <c>null</c> uses
    /// <see cref="Console.Out"/>. Useful for tests, log files, or routing
    /// to a specific TextWriter.
    /// </summary>
    public static TextWriter? OutSink { get; set; }

    /// <summary>
    /// Override for the <see cref="LogLevel.Warn"/> and
    /// <see cref="LogLevel.Error"/> destination. <c>null</c> uses
    /// <see cref="Console.Error"/>.
    /// </summary>
    public static TextWriter? ErrSink { get; set; }

    /// <summary>
    /// True when <paramref name="level"/> is at or above
    /// <see cref="MinLevel"/>. Use to gate expensive message formatting.
    /// </summary>
    public static bool IsEnabled(LogLevel level)
        => (byte)level >= (byte)MinLevel;

    public static void Info(string message)    => Write(LogLevel.Info,    message);
    public static void Warn(string message)    => Write(LogLevel.Warn,    message);
    public static void Error(string message)   => Write(LogLevel.Error,   message);
    public static void Debug(string message)   => Write(LogLevel.Debug,   message);
    public static void Success(string message) => Write(LogLevel.Success, message);

    /// <summary>Write a single log line at the given level.</summary>
    public static void Write(LogLevel level, string message)
    {
        if (!IsEnabled(level)) return;

        var time = DateTime.Now.TimeOfDay;
        var sink = ResolveSink(level);

        var prefix = LogFormatter.FormatTime(time) + "  ";
        var tag = LogFormatter.Tag(level);
        var suffix = "  " + message;

        sink.Write(prefix);
        if (Crt.ColorEnabled)
        {
            sink.Write(Emit.Fg(ColorFor(level)));
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

    /// <summary>
    /// Route both <see cref="OutSink"/> and <see cref="ErrSink"/> to
    /// <paramref name="sink"/> for the duration of the returned scope.
    /// Restores the previous overrides on dispose. Useful for capturing
    /// log output into a string, file, or test buffer without touching
    /// the global Console redirection.
    /// </summary>
    public static IDisposable UseSink(TextWriter sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        var previousOut = OutSink;
        var previousErr = ErrSink;
        OutSink = sink;
        ErrSink = sink;
        return new SinkScope(previousOut, previousErr);
    }

    private static TextWriter ResolveSink(LogLevel level)
    {
        if (level is LogLevel.Warn or LogLevel.Error)
            return ErrSink ?? Console.Error;
        return OutSink ?? Console.Out;
    }

    private static Color ColorFor(LogLevel level)
    {
        // When a theme is active, route levels through its semantic
        // slots so log lines stay on-brand. Falls back to the classic
        // BIOS-flavoured palette otherwise.
        if (Crt.CurrentTheme is { } t)
        {
            return level switch
            {
                LogLevel.Info    => t.Foreground,
                LogLevel.Warn    => t.Warn,
                LogLevel.Error   => t.Error,
                LogLevel.Debug   => t.Muted,
                LogLevel.Success => t.Success,
                _                => t.Foreground,
            };
        }

        return level switch
        {
            LogLevel.Info    => Color.LightCyan,
            LogLevel.Warn    => Color.Yellow,
            LogLevel.Error   => Color.LightRed,
            LogLevel.Debug   => Color.DarkGray,
            LogLevel.Success => Color.LightGreen,
            _                => Color.LightGray,
        };
    }

    private sealed class SinkScope : IDisposable
    {
        private readonly TextWriter? _previousOut;
        private readonly TextWriter? _previousErr;
        private bool _disposed;

        public SinkScope(TextWriter? previousOut, TextWriter? previousErr)
        {
            _previousOut = previousOut;
            _previousErr = previousErr;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OutSink = _previousOut;
            ErrSink = _previousErr;
        }
    }
}
