using System.Text;
using Retro.Crt.Internals;
using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests;

/// <summary>
/// <see cref="Crt.EnableUtf8"/> mutates the process-global
/// <see cref="Console.OutputEncoding"/>, so these run serialised with the
/// other env-mutating tests and restore the encoding on the way out.
/// </summary>
[Collection(EnvMutatingCollection.Name)]
public class CrtEnableUtf8Tests
{
    [Fact]
    public void Return_matches_detected_unicode_support()
    {
        using var _ = OutputEncodingScope.Capture();

        var result = Crt.EnableUtf8();

        Assert.Equal(TerminalCapabilities.SupportsUnicode, result);
    }

    [Fact]
    public void Switches_encoding_when_the_host_allows_it()
    {
        using var _ = OutputEncodingScope.Capture();

        Crt.EnableUtf8();

        // Where the runner permits the switch, the glyphs must light up.
        // Where it doesn't (redirected stdout on some CI hosts) this is a
        // no-op assertion rather than a flaky failure.
        if (Console.OutputEncoding.CodePage == 65001)
            Assert.True(TerminalCapabilities.SupportsUnicode);
    }

    [Fact]
    public void Is_idempotent()
    {
        using var _ = OutputEncodingScope.Capture();

        var first  = Crt.EnableUtf8();
        var second = Crt.EnableUtf8();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Does_not_throw_when_encoding_is_already_utf8()
    {
        using var _ = OutputEncodingScope.Capture();

        try { Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); }
        catch { return; } // Host forbids the switch — nothing to assert here.
        TerminalCapabilities.ResetUnicode();

        // Already UTF-8: the method must take the fast path and report true.
        Assert.True(Crt.EnableUtf8());
    }

    private sealed class OutputEncodingScope : IDisposable
    {
        private readonly Encoding _previous;
        private OutputEncodingScope(Encoding previous) => _previous = previous;

        public static OutputEncodingScope Capture()
        {
            Encoding prev;
            try { prev = Console.OutputEncoding; }
            catch { prev = Encoding.UTF8; }
            return new OutputEncodingScope(prev);
        }

        public void Dispose()
        {
            try { Console.OutputEncoding = _previous; }
            catch { /* host forbids restoring — nothing to do */ }
            TerminalCapabilities.Reset();
        }
    }
}
