using System.Text;

namespace Retro.Crt.Internals;

/// <summary>
/// Decides whether to emit ANSI escapes and at what color depth based on
/// environment, redirection, and (on Windows) successful VT-mode
/// activation. Result is cached; tests can reset via <see cref="Reset"/>.
/// </summary>
internal static class TerminalCapabilities
{
    private static ColorDepth? _depth;
    private static bool? _supportsUnicode;
    private static bool? _isInteractive;
    private static readonly Lock Gate = new();

    /// <summary>
    /// What the terminal can render. <see cref="ColorDepth.None"/> when
    /// output is redirected, <c>NO_COLOR</c> is set, or VT enablement
    /// failed on Windows.
    /// </summary>
    public static ColorDepth CurrentDepth
    {
        get
        {
            if (_depth is { } cached) return cached;
            lock (Gate)
            {
                _depth ??= DetectDepth();
                return _depth.Value;
            }
        }
    }

    /// <summary>
    /// True when ANSI escapes will actually reach the terminal.
    /// Equivalent to <c>CurrentDepth != ColorDepth.None</c>.
    /// </summary>
    public static bool SupportsAnsi => CurrentDepth != ColorDepth.None;

    /// <summary>
    /// True when stdout is a real, escape-processing terminal — not a
    /// pipe / file, not <c>TERM=dumb</c>, and (on Windows) VT mode
    /// activated successfully. Independent of <see cref="CurrentDepth"/>:
    /// a terminal with <c>NO_COLOR=1</c> still counts as interactive
    /// because cursor moves and CR-redraws (used by ProgressBar /
    /// Spinner animations) still work — only the SGR colors are off.
    /// </summary>
    public static bool IsInteractive
    {
        get
        {
            if (_isInteractive is { } cached) return cached;
            lock (Gate)
            {
                _isInteractive ??= DetectInteractive();
                return _isInteractive.Value;
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
            _depth = null;
            _supportsUnicode = null;
            _isInteractive = null;
        }
    }

    /// <summary>
    /// Drop only the cached Unicode result so the next access re-detects
    /// against the current <see cref="Console.OutputEncoding"/>. Called by
    /// <see cref="Crt.EnableUtf8"/> after it switches the encoding — the
    /// color depth and interactivity are unaffected by an encoding change,
    /// so there's no need to re-run VT enablement.
    /// </summary>
    internal static void ResetUnicode()
    {
        lock (Gate) _supportsUnicode = null;
    }

    /// <summary>
    /// Force the cached values for testing — bypasses real detection.
    /// Pass <c>null</c> to keep the default behaviour for that capability.
    /// When <paramref name="ansi"/> is supplied without an explicit
    /// <paramref name="depth"/>, true picks <see cref="ColorDepth.Truecolor"/>
    /// and false picks <see cref="ColorDepth.None"/>.
    /// </summary>
    internal static void OverrideForTests(bool? ansi = null, bool? unicode = null,
                                          ColorDepth? depth = null, bool? interactive = null)
    {
        lock (Gate)
        {
            if (depth is { } d) _depth = d;
            else if (ansi is { } a) _depth = a ? ColorDepth.Truecolor : ColorDepth.None;

            if (unicode is { } u) _supportsUnicode = u;
            if (interactive is { } i) _isInteractive = i;
        }
    }

    private static ColorDepth DetectDepth()
    {
        // https://no-color.org — if set (any value), opt out.
        if (HasEnv("NO_COLOR")) return ColorDepth.None;

        // Explicit dumb terminal opts out.
        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase)) return ColorDepth.None;

        // FORCE_COLOR follows the npm convention:
        //   "0"   → off
        //   "1"   → at least Standard16
        //   "2"   → Xterm256
        //   "3"   → Truecolor
        //   other → equivalent to "1" (force on)
        var forceRaw = Environment.GetEnvironmentVariable("FORCE_COLOR");
        var forced = ParseForceColor(forceRaw);
        if (forced == ColorDepth.None && forceRaw is not null && forceRaw == "0")
            return ColorDepth.None;

        if (forced != ColorDepth.None)
        {
            EnableOnWindowsIfNeeded(); // Best-effort, ignore result under FORCE_COLOR.
            // FORCE_COLOR=1 means "at least 16"; let signal-based detection
            // upgrade to 256/truecolor where it can.
            if (forced == ColorDepth.Standard16)
                return UpgradeFromSignals(ColorDepth.Standard16, term);
            return forced;
        }

        // Output redirected (e.g. piped, captured) — no escapes by default.
        if (Console.IsOutputRedirected) return ColorDepth.None;

        if (!EnableOnWindowsIfNeeded()) return ColorDepth.None;

        return UpgradeFromSignals(ColorDepth.Standard16, term);
    }

    private static ColorDepth ParseForceColor(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return ColorDepth.None;
        return raw switch
        {
            "0"   => ColorDepth.None,
            "2"   => ColorDepth.Xterm256,
            "3"   => ColorDepth.Truecolor,
            _     => ColorDepth.Standard16,
        };
    }

    private static ColorDepth UpgradeFromSignals(ColorDepth floor, string? term)
    {
        // COLORTERM is the most reliable truecolor signal.
        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        if (!string.IsNullOrEmpty(colorTerm))
        {
            if (string.Equals(colorTerm, "truecolor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(colorTerm, "24bit",     StringComparison.OrdinalIgnoreCase))
                return ColorDepth.Truecolor;
        }

        // Windows Terminal sets WT_SESSION; ConPTY supports truecolor
        // since Windows 10 1909. Other Windows hosts (legacy conhost) get
        // 256-color at most.
        if (OperatingSystem.IsWindows())
        {
            if (HasEnv("WT_SESSION")) return ColorDepth.Truecolor;
            // Modern Windows Terminal also signals via COLORTERM above; in
            // its absence assume 256 for any host that took our VT
            // enablement (real conhost on supported builds).
            return Atop(floor, ColorDepth.Xterm256);
        }

        if (!string.IsNullOrEmpty(term))
        {
            if (term.Contains("truecolor", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("direct",    StringComparison.OrdinalIgnoreCase))
                return ColorDepth.Truecolor;
            if (term.Contains("256", StringComparison.OrdinalIgnoreCase))
                return Atop(floor, ColorDepth.Xterm256);
        }

        return floor;
    }

    private static ColorDepth Atop(ColorDepth floor, ColorDepth candidate)
        => (byte)candidate > (byte)floor ? candidate : floor;

    private static bool DetectInteractive()
    {
        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase)) return false;

        bool redirected;
        try { redirected = Console.IsOutputRedirected; }
        catch { return false; }
        if (redirected) return false;

        // On Windows, "interactive" requires the conhost to actually
        // process VT escapes. If enablement fails, even \r might not
        // behave the way we expect on legacy hosts.
        if (OperatingSystem.IsWindows())
            return WindowsVt.TryEnable();

        return true;
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
