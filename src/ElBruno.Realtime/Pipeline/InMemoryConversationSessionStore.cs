using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace ElBruno.Realtime.Pipeline;

/// <summary>
/// Default in-memory implementation of <see cref="IConversationSessionStore"/>.
/// Thread-safe. Sessions are lost on app restart.
/// </summary>
public class InMemoryConversationSessionStore : IConversationSessionStore
{
    private readonly ConcurrentDictionary<string, IList<ChatMessage>> _sessions = new();

    public Task<IList<ChatMessage>> GetOrCreateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var history = _sessions.GetOrAdd(sessionId, _ => new List<ChatMessage>());
        return Task.FromResult(history);
    }

    public Task RemoveSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
