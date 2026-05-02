using BenchmarkDotNet.Attributes;
using Retro.Crt.Internals;

namespace Retro.Crt.Bench;

/// <summary>
/// Pure formatting cost — no I/O. These are called extremely often by
/// Typewriter.AlphaFade (4× per character) and ProgressBar.Redraw (every
/// frame), so allocation per call adds up fast.
/// </summary>
[MemoryDiagnoser]
public class AnsiCodesBenchmarks
{
    private readonly Color _truecolor = Color.Rgb(200, 100, 50);
    private readonly Color _standard16 = Color.LightCyan;

    [Benchmark]
    public string Foreground_Truecolor()
        => AnsiCodes.Foreground(_truecolor);

    [Benchmark]
    public string Foreground_Standard16()
        => AnsiCodes.Foreground(_standard16);

    [Benchmark]
    public string Background_Truecolor()
        => AnsiCodes.Background(_truecolor);

    [Benchmark]
    public string GotoXY_call()
        => AnsiCodes.GotoXY(40, 12);

    [Benchmark]
    public string CursorLeft_one()
        => AnsiCodes.CursorLeft(1);

    [Benchmark]
    public string CursorLeft_n()
        => AnsiCodes.CursorLeft(7);
}
