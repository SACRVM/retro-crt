using System.Globalization;
using System.Text;
using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Snapshot of the capability flags Retro.Crt detected on this process.
/// Useful for "why don't I see colors" support questions — log this once
/// at startup and the answer is usually obvious.
/// </summary>
public readonly record struct TerminalReport
{
    public bool SupportsAnsi { get; init; }
    public ColorDepth ColorDepth { get; init; }
    public bool IsInteractive { get; init; }
    public bool SupportsUnicode { get; init; }
    public bool IsOutputRedirected { get; init; }
    public bool IsErrorRedirected { get; init; }
    public string? TermEnv { get; init; }
    public string? ColorTermEnv { get; init; }
    public bool NoColorSet { get; init; }
    public bool ForceColorSet { get; init; }
    public string OutputEncoding { get; init; } = "";
    public int OutputCodePage { get; init; }
    public string OperatingSystem { get; init; } = "";

    public TerminalReport() { }

    /// <summary>One-line dense summary, friendly for log output.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder(180);
        sb.Append("ansi=").Append(SupportsAnsi ? "on" : "off");
        sb.Append(" depth=").Append(DepthLabel(ColorDepth));
        sb.Append(" interactive=").Append(IsInteractive ? "yes" : "no");
        sb.Append(" unicode=").Append(SupportsUnicode ? "on" : "off");
        sb.Append(" redirected=").Append(IsOutputRedirected ? "stdout" : "no");
        if (IsErrorRedirected) sb.Append("+stderr");
        sb.Append(" TERM=").Append(string.IsNullOrEmpty(TermEnv) ? "<unset>" : TermEnv);
        if (!string.IsNullOrEmpty(ColorTermEnv)) sb.Append(" COLORTERM=").Append(ColorTermEnv);
        if (NoColorSet) sb.Append(" NO_COLOR=set");
        if (ForceColorSet) sb.Append(" FORCE_COLOR=set");
        sb.Append(" enc=").Append(OutputEncoding)
          .Append('(').Append(OutputCodePage.ToString(CultureInfo.InvariantCulture)).Append(')');
        sb.Append(" os=").Append(OperatingSystem);
        return sb.ToString();
    }

    private static string DepthLabel(ColorDepth d) => d switch
    {
        ColorDepth.Truecolor  => "truecolor",
        ColorDepth.Xterm256   => "256",
        ColorDepth.Standard16 => "16",
        _                     => "none",
    };
}

/// <summary>
/// Helpers for inspecting and logging Retro.Crt's runtime capability
/// detection.
/// </summary>
public static class Diagnostics
{
    /// <summary>
    /// Snapshot the current detection state — what <see cref="Crt"/> would
    /// emit and why. Cheap; safe to call from a startup hook.
    /// </summary>
    public static TerminalReport Capture()
    {
        Encoding enc;
        try { enc = Console.OutputEncoding; }
        catch { enc = Encoding.ASCII; }

        return new TerminalReport
        {
            SupportsAnsi = TerminalCapabilities.SupportsAnsi,
            ColorDepth = TerminalCapabilities.CurrentDepth,
            IsInteractive = TerminalCapabilities.IsInteractive,
            SupportsUnicode = TerminalCapabilities.SupportsUnicode,
            IsOutputRedirected = SafeIsOutRedirected(),
            IsErrorRedirected = SafeIsErrRedirected(),
            TermEnv = Environment.GetEnvironmentVariable("TERM"),
            ColorTermEnv = Environment.GetEnvironmentVariable("COLORTERM"),
            NoColorSet = HasEnv("NO_COLOR"),
            ForceColorSet = HasEnv("FORCE_COLOR"),
            OutputEncoding = enc.WebName,
            OutputCodePage = enc.CodePage,
            OperatingSystem = OsName(),
        };
    }

    private static bool SafeIsOutRedirected()
    {
        try { return Console.IsOutputRedirected; }
        catch { return false; }
    }

    private static bool SafeIsErrRedirected()
    {
        try { return Console.IsErrorRedirected; }
        catch { return false; }
    }

    private static bool HasEnv(string name)
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));

    private static string OsName()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux())   return "linux";
        if (OperatingSystem.IsMacOS())   return "macos";
        if (OperatingSystem.IsFreeBSD()) return "freebsd";
        return "other";
    }
}
