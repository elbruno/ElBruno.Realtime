using BenchmarkDotNet.Attributes;

namespace ElBruno.Realtime.Benchmarks;

/// <summary>
/// STT (Speech-to-Text) throughput benchmarks.
/// </summary>
/// <remarks>
/// Baseline expectations: Whisper.net latency on CPU.
/// Note: STT benchmarking requires pre-downloaded models and typically takes several seconds.
/// Run with: dotnet run -c Release -p src/ElBruno.Realtime.Benchmarks --filter '*SttBenchmark*'
/// </remarks>
[MemoryDiagnoser]
public class SttBenchmark
{
    [Benchmark(Description = "Placeholder: STT benchmarks require model downloads")]
    public void Placeholder()
    {
        // STT benchmarks skipped due to model download complexity.
        // Use production audio files in real profiling scenarios.
    }
}
