using System.Text;

namespace Retro.Crt.Internals;

/// <summary>
/// Decides whether to emit ANSI escapes based on environment, redirection, and
/// (on Windows) successful VT-mode activation. Result is cached; tests can
/// reset via <see cref="Reset"/>.
/// </summary>
internal static class TerminalCapabilities
{
    private static bool? _supportsAnsi;
    private static bool? _supportsUnicode;
    private static readonly Lock Gate = new();

    public static bool SupportsAnsi
    {
        get
        {
            if (_supportsAnsi is { } cached) return cached;
            lock (Gate)
            {
                _supportsAnsi ??= Detect();
                return _supportsAnsi.Value;
            }
        }
    }

    /// <summary>
    /// True when <see cref="Console.OutputEncoding"/> can encode the box-
    /// drawing and shading glyphs Retro.Crt likes to use (UTF-8, cp437, etc.).
    /// False on legacy ANSI/ASCII code pages — caller should fall back to
    /// 7-bit safe glyphs.
    /// </summary>
    public static bool SupportsUnicode
    {
        get
        {
            if (_supportsUnicode is { } cached) return cached;
            lock (Gate)
            {
                _supportsUnicode ??= DetectUnicode();
                return _supportsUnicode.Value;
            }
        }
    }

    /// <summary>Force the next access to re-detect. Test-only.</summary>
    internal static void Reset()
    {
        lock (Gate)
        {
            _supportsAnsi = null;
            _supportsUnicode = null;
        }
    }

    private static bool Detect()
    {
        // https://no-color.org — if set (any value), opt out.
        if (HasEnv("NO_COLOR")) return false;

        // Explicit dumb terminal opts out.
        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase)) return false;

        // FORCE_COLOR (any value) overrides redirection detection. Useful in
        // CI logs or when piping into a tool that strips/keeps escapes itself.
        if (HasEnv("FORCE_COLOR")) return EnableOnWindowsIfNeeded();

        // Output redirected (e.g. piped, captured) — no escapes by default.
        if (Console.IsOutputRedirected) return false;

        return EnableOnWindowsIfNeeded();
    }

    private static bool EnableOnWindowsIfNeeded()
    {
        if (!OperatingSystem.IsWindows()) return true;
        return WindowsVt.TryEnable();
    }

    // The set of glyphs that have to round-trip cleanly for SupportsUnicode to
    // be true. Covers box drawing, the progress bar fill, and the typewriter
    // block cursor.
    private const string ProbeGlyphs = "─│┌┐└┘█░▌";

    private static bool DetectUnicode()
    {
        Encoding enc;
        try { enc = Console.OutputEncoding; }
        catch { return false; }

        // UTF-8 / UTF-16 / UTF-32 always handle BMP punctuation cleanly.
        var cp = enc.CodePage;
        if (cp is 65001 or 1200 or 1201 or 12000 or 12001) return true;

        // Probe-encode under a strict fallback: any unrepresentable char
        // raises EncoderFallbackException, which we treat as "no unicode".
        try
        {
            var strict = (Encoding)enc.Clone();
            strict.EncoderFallback = EncoderFallback.ExceptionFallback;
            _ = strict.GetByteCount(ProbeGlyphs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasEnv(string name)
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));
}
