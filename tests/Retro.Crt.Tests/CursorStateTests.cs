using Retro.Crt.Internals;
using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests;

[Collection(EnvMutatingCollection.Name)]
public class CursorStateTests
{
    [Fact]
    public void GetLeft_returns_zero_when_host_cant_report()
    {
        // Test runners always have stdout redirected, so the underlying
        // Console.CursorLeft will throw or return 0; the helper must
        // swallow that and return 0.
        Assert.Equal(0, CursorState.GetLeft());
    }

    [Fact]
    public void Override_pins_value_until_cleared()
    {
        try
        {
            CursorState.OverrideForTests(42);
            Assert.Equal(42, CursorState.GetLeft());
            Assert.Equal(42, Crt.CursorLeft);
        }
        finally
        {
            CursorState.OverrideForTests(null);
        }
    }

    [Fact]
    public void CursorLeft_is_a_pass_through_to_the_helper()
    {
        try
        {
            CursorState.OverrideForTests(7);
            Assert.Equal(7, Crt.CursorLeft);
        }
        finally
        {
            CursorState.OverrideForTests(null);
        }
    }
}
