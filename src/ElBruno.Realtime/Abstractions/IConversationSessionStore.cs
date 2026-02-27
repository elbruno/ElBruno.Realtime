using Microsoft.Extensions.AI;

namespace ElBruno.Realtime;

/// <summary>
/// Manages per-session conversation history.
/// Each session is identified by a string ID (e.g., SignalR connection ID, user ID).
/// </summary>
public interface IConversationSessionStore
{
    /// <summary>Gets or creates the message history for the given session.</summary>
    Task<IList<ChatMessage>> GetOrCreateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Removes a session's history (e.g., on disconnect).</summary>
    Task RemoveSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
