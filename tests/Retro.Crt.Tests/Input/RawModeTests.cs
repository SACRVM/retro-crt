using Retro.Crt.Input;

namespace Retro.Crt.Tests.Input;

public class RawModeTests
{
    [Fact]
    public void Enter_throws_when_already_active()
    {
        // We can't actually enter raw mode in the test runner (stdin is
        // not a TTY) — but the nesting guard runs before any PInvoke,
        // so we exercise it with a fake "active" scope set via
        // reflection.
        var field = typeof(RawMode).GetField(
            "_activeScope",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        var sentinel = new DummyScope();
        field.SetValue(null, sentinel);
        try
        {
            Assert.Throws<InvalidOperationException>(() => RawMode.Enter());
        }
        finally
        {
            field.SetValue(null, null);
        }
    }

    private sealed class DummyScope : IDisposable
    {
        public void Dispose() { }
    }
}
