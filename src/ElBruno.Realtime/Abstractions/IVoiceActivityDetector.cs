namespace ElBruno.Realtime;

/// <summary>
/// Detects voice activity (speech vs. silence) in a continuous audio stream.
/// Produces discrete speech segments suitable for transcription.
/// </summary>
public interface IVoiceActivityDetector : IDisposable
{
    /// <summary>Detects speech segments in a continuous stream of audio chunks.</summary>
    /// <param name="audioChunks">Incoming PCM audio chunks from a microphone or stream.</param>
    /// <param name="options">Options to configure voice activity detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of detected speech segments.</returns>
    IAsyncEnumerable<SpeechSegment> DetectSpeechAsync(
        IAsyncEnumerable<byte[]> audioChunks,
        VadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the detector for an object of the specified type.</summary>
    object? GetService(Type serviceType, object? serviceKey = null);
}
