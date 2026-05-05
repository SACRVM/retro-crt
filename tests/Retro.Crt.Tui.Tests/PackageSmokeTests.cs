using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests;

public class PackageSmokeTests
{
    [Fact]
    public void Tui_assembly_loads_and_references_core()
    {
        var tuiAssembly  = typeof(Rect).Assembly;
        var coreAssembly = typeof(Retro.Crt.Cell).Assembly;

        Assert.NotEqual(tuiAssembly, coreAssembly);
        Assert.Equal("Retro.Crt.Tui", tuiAssembly.GetName().Name);
        Assert.Equal("Retro.Crt",     coreAssembly.GetName().Name);
    }
}
