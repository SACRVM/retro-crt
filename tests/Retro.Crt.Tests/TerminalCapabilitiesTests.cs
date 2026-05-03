using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

/// <summary>
/// Capability detection reads process-level env vars and console redirection.
/// We mutate env vars per test and reset the cached result. Tests run on the
/// same process so they are not parallel-safe with each other; xUnit runs
/// tests in the same class sequentially by default.
/// </summary>
[Collection("env-mutating")]
public class TerminalCapabilitiesTests
{
    [Fact]
    public void NO_COLOR_disables_ansi()
    {
        using var _ = EnvScope.Set("NO_COLOR", "1");
        TerminalCapabilities.Reset();

        Assert.False(TerminalCapabilities.SupportsAnsi);
    }

    [Fact]
    public void TERM_dumb_disables_ansi()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", null);
        using var _term  = EnvScope.Set("TERM", "dumb");
        TerminalCapabilities.Reset();

        Assert.False(TerminalCapabilities.SupportsAnsi);
    }

    [Fact]
    public void Result_is_cached()
    {
        using var _ = EnvScope.Set("NO_COLOR", "1");
        TerminalCapabilities.Reset();

        var first = TerminalCapabilities.SupportsAnsi;

        // Flip the env without resetting — cached value must stick.
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        var second = TerminalCapabilities.SupportsAnsi;

        Assert.Equal(first, second);
    }

    [Fact]
    public void NO_COLOR_yields_depth_None()
    {
        using var _ = EnvScope.Set("NO_COLOR", "1");
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.None, TerminalCapabilities.CurrentDepth);
    }

    [Fact]
    public void COLORTERM_truecolor_picks_truecolor()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "1");
        using var _ct    = EnvScope.Set("COLORTERM", "truecolor");
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.Truecolor, TerminalCapabilities.CurrentDepth);
    }

    [Fact]
    public void COLORTERM_24bit_picks_truecolor()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "1");
        using var _ct    = EnvScope.Set("COLORTERM", "24bit");
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.Truecolor, TerminalCapabilities.CurrentDepth);
    }

    [Fact]
    public void TERM_xterm_256color_without_colorterm_picks_256()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "1");
        using var _ct    = EnvScope.Set("COLORTERM", null);
        using var _wt    = EnvScope.Set("WT_SESSION", null);
        using var _term  = EnvScope.Set("TERM", "xterm-256color");
        TerminalCapabilities.Reset();

        // On non-Windows hosts the TERM signal alone picks 256. On Windows
        // the VT-enable path will upgrade to 256 via the same code path,
        // unless WT_SESSION pushes us to truecolor — explicitly cleared
        // above.
        var depth = TerminalCapabilities.CurrentDepth;
        Assert.True(depth == ColorDepth.Xterm256 || depth == ColorDepth.Truecolor,
            $"expected 256 or truecolor, got {depth}");
    }

    [Fact]
    public void FORCE_COLOR_3_forces_truecolor()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "3");
        using var _ct    = EnvScope.Set("COLORTERM", null);
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.Truecolor, TerminalCapabilities.CurrentDepth);
    }

    [Fact]
    public void FORCE_COLOR_2_forces_xterm256()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "2");
        using var _ct    = EnvScope.Set("COLORTERM", null);
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.Xterm256, TerminalCapabilities.CurrentDepth);
    }

    [Fact]
    public void FORCE_COLOR_0_forces_none()
    {
        using var _no    = EnvScope.Set("NO_COLOR", null);
        using var _force = EnvScope.Set("FORCE_COLOR", "0");
        TerminalCapabilities.Reset();

        Assert.Equal(ColorDepth.None, TerminalCapabilities.CurrentDepth);
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        private EnvScope(string name, string? previous)
        {
            _name = name;
            _previous = previous;
        }

        public static EnvScope Set(string name, string? value)
        {
            var prev = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvScope(name, prev);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _previous);
            TerminalCapabilities.Reset();
        }
    }
}
