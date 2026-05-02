using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt.Tests.Support;

/// <summary>
/// Replaces <see cref="Console.Out"/> and (optionally)
/// <see cref="Console.Error"/> with <see cref="StringWriter"/>s and resets
/// <see cref="TerminalCapabilities"/>'s cache so tests can pick a known
/// ANSI / unicode mode. Restores everything on dispose.
/// </summary>
internal sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _previousOut;
    private readonly TextWriter _previousErr;
    private readonly string? _previousNoColor;
    private readonly string? _previousForceColor;
    private readonly string? _previousTerm;
    private readonly StringWriter _out;
    private readonly StringWriter _err;
    private bool _disposed;

    private ConsoleCapture(StringWriter @out, StringWriter err,
                           string? prevNoColor, string? prevForceColor, string? prevTerm)
    {
        _previousOut = Console.Out;
        _previousErr = Console.Error;
        _previousNoColor = prevNoColor;
        _previousForceColor = prevForceColor;
        _previousTerm = prevTerm;
        _out = @out;
        _err = err;
    }

    public string Out => _out.ToString();
    public string Err => _err.ToString();

    public static ConsoleCapture Start(bool ansi, bool unicode = true)
    {
        var prevNoColor    = Environment.GetEnvironmentVariable("NO_COLOR");
        var prevForceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
        var prevTerm       = Environment.GetEnvironmentVariable("TERM");

        if (ansi)
        {
            Environment.SetEnvironmentVariable("NO_COLOR", null);
            Environment.SetEnvironmentVariable("FORCE_COLOR", "1");
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");
        }
        else
        {
            Environment.SetEnvironmentVariable("NO_COLOR", "1");
            Environment.SetEnvironmentVariable("FORCE_COLOR", null);
        }

        TerminalCapabilities.Reset();
        // Hard override — bypasses Windows VT enablement on redirected
        // stdout, which always reports false in test runners.
        TerminalCapabilities.OverrideForTests(ansi: ansi, unicode: unicode);

        var @out = new StringWriter { NewLine = "\n" };
        var err  = new StringWriter { NewLine = "\n" };
        var capture = new ConsoleCapture(@out, err, prevNoColor, prevForceColor, prevTerm);
        Console.SetOut(@out);
        Console.SetError(err);
        return capture;
    }

    /// <summary>
    /// Force <see cref="TerminalCapabilities.SupportsUnicode"/> via a known
    /// <see cref="Console.OutputEncoding"/>. Useful for snapshotting box
    /// glyphs.
    /// </summary>
    public static void ForceUnicode(bool unicode)
    {
        try
        {
            Console.OutputEncoding = unicode ? Encoding.UTF8 : Encoding.ASCII;
        }
        catch
        {
            // Some hosts forbid setting OutputEncoding; tests calling this
            // should be in collections that don't run there.
        }
        TerminalCapabilities.Reset();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Console.SetOut(_previousOut);
        Console.SetError(_previousErr);

        Environment.SetEnvironmentVariable("NO_COLOR",    _previousNoColor);
        Environment.SetEnvironmentVariable("FORCE_COLOR", _previousForceColor);
        Environment.SetEnvironmentVariable("TERM",        _previousTerm);

        TerminalCapabilities.Reset();
        _out.Dispose();
        _err.Dispose();
    }
}
