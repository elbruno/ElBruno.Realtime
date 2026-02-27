namespace ElBruno.Realtime;

/// <summary>
/// High-level orchestration client for real-time audio conversations.
/// Chains VAD → STT → LLM → TTS transparently, providing a single
/// entry point for .NET developers.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ConverseAsync"/> for full-duplex streaming conversations
/// (e.g., always-on microphone with continuous responses).
/// </para>
/// <para>
/// Use <see cref="ProcessTurnAsync"/> for simple one-shot turn-based
/// interactions (e.g., user speaks, wait for complete response).
/// </para>
/// </remarks>
public interface IRealtimeConversationClient : IDisposable
{
    /// <summary>
    /// Full-duplex streaming conversation: continuously processes incoming audio
    /// and yields conversation events (transcription, LLM response, audio output).
    /// </summary>
    /// <param name="audioInput">Continuous stream of PCM audio chunks from microphone.</param>
    /// <param name="options">Conversation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of conversation events as they occur.</returns>
    IAsyncEnumerable<ConversationEvent> ConverseAsync(
        IAsyncEnumerable<byte[]> audioInput,
        ConversationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One-shot turn: processes a single audio input and returns the complete response.
    /// </summary>
    /// <param name="audioInput">Audio stream containing the user's speech.</param>
    /// <param name="options">Conversation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete conversation turn with transcribed text, response, and optional audio.</returns>
    Task<ConversationTurn> ProcessTurnAsync(
        Stream audioInput,
        ConversationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the client for an object of the specified type.</summary>
    object? GetService(Type serviceType, object? serviceKey = null);
}
