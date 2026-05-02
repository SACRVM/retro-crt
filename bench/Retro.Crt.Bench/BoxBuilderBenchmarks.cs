using BenchmarkDotNet.Attributes;
using Retro.Crt.Internals;

namespace Retro.Crt.Bench;

[MemoryDiagnoser]
public class BoxBuilderBenchmarks
{
    private readonly string[] _lines = ["Retro.Crt 0.2", "Banner / Bar / Log / Typewriter"];

    [Benchmark]
    public string[] Build_two_line_box()
        => BoxBuilder.Build(_lines, padding: 1,
            tl: '┌', tr: '┐', bl: '└', br: '┘',
            horizontal: '─', vertical: '│');
}
