using BenchmarkDotNet.Attributes;
using Retro.Crt.Internals;

namespace Retro.Crt.Bench;

/// <summary>
/// LogFormatter.Format produces the visible line; if it allocates more
/// than a single string per call, frequent logging in tight loops gets
/// expensive.
/// </summary>
[MemoryDiagnoser]
public class LogBenchmarks
{
    private readonly TimeSpan _time = new(13, 27, 42);

    [Benchmark]
    public string Format_full_line()
        => LogFormatter.Format(LogLevel.Info, "ready", _time);

    [Benchmark]
    public string FormatTime_call()
        => LogFormatter.FormatTime(_time);

    [Benchmark]
    public string Tag_call()
        => LogFormatter.Tag(LogLevel.Info);
}
