using BenchmarkDotNet.Attributes;
using ElBruno.Realtime;
using System.Runtime.CompilerServices;

namespace ElBruno.Realtime.Benchmarks;

/// <summary>
/// VAD (Voice Activity Detection) throughput and allocation benchmarks.
/// </summary>
/// <remarks>
/// Measures VAD options and memory allocations (without model download).
/// 
/// Expected baselines (before optimizations):
/// - Option creation: &lt;1 μs
/// - Allocations: minimal (options are value-like)
/// 
/// Note: Full VAD benchmarking requires model download which is skipped here.
/// For comprehensive VAD benchmarking, run manually with pre-downloaded models.
/// </remarks>
[MemoryDiagnoser]
public class VadBenchmark
{
    private VadOptions? _defaultOptions;

    [GlobalSetup]
    public void Setup()
    {
        _defaultOptions = new VadOptions
        {
            SampleRate = 16000,
            Threshold = 0.5f,
            MinSpeechDurationMs = 250,
            MaxSpeechDurationMs = 30000,
            MinSilenceDurationMs = 500,
            SpeechPad = 30
        };
    }

    [Benchmark]
    public VadOptions CreateDefaultOptions()
    {
        return new VadOptions();
    }

    [Benchmark]
    public VadOptions CreateCustomOptions()
    {
        return new VadOptions
        {
            SampleRate = 16000,
            Threshold = 0.7f,
            MinSpeechDurationMs = 300,
            MaxSpeechDurationMs = 20000,
            MinSilenceDurationMs = 600,
            SpeechPad = 50
        };
    }

    [Benchmark]
    public VadOptions CloneOptions()
    {
        return new VadOptions
        {
            SampleRate = _defaultOptions!.SampleRate,
            Threshold = _defaultOptions.Threshold,
            MinSpeechDurationMs = _defaultOptions.MinSpeechDurationMs,
            MaxSpeechDurationMs = _defaultOptions.MaxSpeechDurationMs,
            MinSilenceDurationMs = _defaultOptions.MinSilenceDurationMs,
            SpeechPad = _defaultOptions.SpeechPad
        };
    }
}
