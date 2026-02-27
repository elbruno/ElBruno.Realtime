namespace ElBruno.Realtime;

/// <summary>
/// Represents a text-to-speech client following Microsoft.Extensions.AI patterns.
/// Converts text into synthesized audio speech.
/// </summary>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="ITextToSpeechClient"/> are thread-safe for concurrent use.
/// </para>
/// <para>
/// This interface mirrors the design of <c>ISpeechToTextClient</c> from Microsoft.Extensions.AI.
/// When Microsoft ships an official <c>ITextToSpeechClient</c>, this can be migrated to use that instead.
/// </para>
/// </remarks>
public interface ITextToSpeechClient : IDisposable
{
    /// <summary>Converts text to audio speech.</summary>
    /// <param name="text">The text to synthesize into speech.</param>
    /// <param name="options">Options to configure the synthesis request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The synthesized speech response containing audio data.</returns>
    Task<TextToSpeechResponse> GetSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Converts text to audio speech, streaming audio chunks as they are synthesized.</summary>
    /// <param name="text">The text to synthesize into speech.</param>
    /// <param name="options">Options to configure the synthesis request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of audio chunks as they become available.</returns>
    IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the client for an object of the specified type.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    object? GetService(Type serviceType, object? serviceKey = null);
}
