using BenchmarkDotNet.Attributes;
using ElBruno.Realtime.Pipeline;

namespace ElBruno.Realtime.Benchmarks;

/// <summary>
/// End-to-end pipeline throughput benchmarks.
/// </summary>
/// <remarks>
/// Measures conversation session store and history management performance.
/// 
/// Expected baselines (before optimizations):
/// - Session creation: &lt;1 ms
/// - History append: &lt;1 ms per message
/// - Session removal: &lt;1 ms
/// 
/// Uses in-memory store for baseline measurements.
/// </remarks>
[MemoryDiagnoser]
public class PipelineBenchmark
{
    private InMemoryConversationSessionStore? _sessionStore;
    private string _sessionId = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _sessionStore = new InMemoryConversationSessionStore();
        _sessionId = Guid.NewGuid().ToString();
    }

    [Benchmark(Description = "Create conversation session")]
    public void CreateSession()
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessionStore?.CreateSessionAsync(sessionId).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Get session")]
    public void GetSession()
    {
        _sessionStore?.GetSessionAsync(_sessionId).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Remove session")]
    public void RemoveSession()
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessionStore?.CreateSessionAsync(sessionId).GetAwaiter().GetResult();
        _sessionStore?.RemoveSessionAsync(sessionId).GetAwaiter().GetResult();
    }
}
