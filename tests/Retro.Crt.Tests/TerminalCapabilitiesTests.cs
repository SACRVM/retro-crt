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
