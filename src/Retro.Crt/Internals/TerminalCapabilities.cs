namespace Retro.Crt.Internals;

/// <summary>
/// Decides whether to emit ANSI escapes based on environment, redirection, and
/// (on Windows) successful VT-mode activation. Result is cached; tests can
/// reset via <see cref="Reset"/>.
/// </summary>
internal static class TerminalCapabilities
{
    private static bool? _supportsAnsi;
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

    /// <summary>Force the next access to re-detect. Test-only.</summary>
    internal static void Reset()
    {
        lock (Gate) _supportsAnsi = null;
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

    private static bool HasEnv(string name)
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));
}
