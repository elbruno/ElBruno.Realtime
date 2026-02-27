using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.Whisper;

/// <summary>
/// Extension methods for registering Whisper STT with the real-time conversation pipeline.
/// </summary>
public static class WhisperRealtimeBuilderExtensions
{
    /// <summary>
    /// Adds Whisper.net as the speech-to-text provider.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="modelId">
    /// Whisper model identifier. Default: "whisper-tiny.en" (75MB, fast).
    /// Use "whisper-base.en" (142MB) for better accuracy.
    /// </param>
    /// <param name="cacheDir">Optional model cache directory.</param>
    /// <param name="language">Optional language hint (e.g., "en").</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseWhisperStt(
        this RealtimeBuilder builder,
        string modelId = "whisper-tiny.en",
        string? cacheDir = null,
        string? language = null)
    {
        builder.Options.SpeechToText.ModelId = modelId;

        var capturedModelId = modelId;
        var capturedCacheDir = cacheDir;
        var capturedLanguage = language;
        builder.Services.AddSingleton<ISpeechToTextClient>(sp =>
            new WhisperSpeechToTextClient(capturedModelId, capturedCacheDir, capturedLanguage));

        return builder;
    }

    /// <summary>
    /// Adds Whisper.net as the speech-to-text provider using a pre-downloaded model file.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="modelPath">Path to the GGML model file.</param>
    /// <param name="language">Optional language hint.</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseWhisperSttFromPath(
        this RealtimeBuilder builder,
        string modelPath,
        string? language = null)
    {
        builder.Services.AddSingleton<ISpeechToTextClient>(
            _ => WhisperSpeechToTextClient.FromModelPath(modelPath, language));

        return builder;
    }
}
