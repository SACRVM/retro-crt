namespace Retro.Crt.Tui.Tests;

public class PackageSmokeTests
{
    [Fact]
    public void Tui_assembly_loads_and_references_core()
    {
        // Confirms the package builds, references Retro.Crt, and the
        // test runner can resolve types from both assemblies. Once
        // real widgets ship, this trivial test gets replaced by
        // proper widget tests.
        var tuiAssembly  = typeof(TuiAssemblyMarker).Assembly;
        var coreAssembly = typeof(Retro.Crt.Cell).Assembly;

        Assert.NotEqual(tuiAssembly, coreAssembly);
        Assert.Equal("Retro.Crt.Tui", tuiAssembly.GetName().Name);
        Assert.Equal("Retro.Crt",     coreAssembly.GetName().Name);
    }
}
