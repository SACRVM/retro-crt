using BenchmarkDotNet.Running;

namespace Retro.Crt.Bench;

public static class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
