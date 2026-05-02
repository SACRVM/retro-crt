using BenchmarkDotNet.Attributes;
using Retro.Crt.Internals;

namespace Retro.Crt.Bench;

/// <summary>
/// ProgressBar.Set is the one frame-rate-critical hot path — a download
/// loop calls it on every chunk. We benchmark the pure renderer (no I/O)
/// to isolate string-building cost from the terminal write itself.
/// </summary>
[MemoryDiagnoser]
public class ProgressBarBenchmarks
{
    private const int Width = 30;

    [Benchmark]
    public string RenderFrame_no_label()
        => ProgressBarRenderer.RenderFrame(null, 0.42, Width, '█', '░', showPercent: true);

    [Benchmark]
    public string RenderFrame_with_label()
        => ProgressBarRenderer.RenderFrame(" download", 0.42, Width, '█', '░', showPercent: true);

    [Benchmark]
    public string RenderBar_only()
        => ProgressBarRenderer.RenderBar(filled: 12, width: Width, fullChar: '█', emptyChar: '░');

    /// <summary>
    /// FilledCells is called twice per Set (renderer + throttle check),
    /// must be effectively free.
    /// </summary>
    [Benchmark]
    public int FilledCells_call()
        => ProgressBarRenderer.FilledCells(0.42, Width);
}
