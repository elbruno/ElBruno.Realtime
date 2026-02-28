using BenchmarkDotNet.Running;

namespace ElBruno.Realtime.Benchmarks;

/// <summary>
/// BenchmarkDotNet runner for ElBruno.Realtime performance baselines.
/// </summary>
/// <remarks>
/// Run with: dotnet run -c Release -p src/ElBruno.Realtime.Benchmarks
/// 
/// DO NOT run benchmarks in CI (too slow). Run manually for baseline establishment
/// and performance regression testing.
/// </remarks>
class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
