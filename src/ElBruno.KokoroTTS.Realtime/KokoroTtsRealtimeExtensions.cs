using ElBruno.Realtime;
using KokoroSharp;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.KokoroTTS.Realtime;

/// <summary>
/// Extension methods for registering KokoroTTS as the TTS provider
/// with the real-time conversation pipeline.
/// </summary>
public static class KokoroTtsRealtimeExtensions
{
    /// <summary>
    /// Adds KokoroTTS (Kokoro-82M) as the text-to-speech provider for the real-time pipeline.
    /// Registers the <see cref="ITextToSpeechClient"/> adapter.
    /// The ONNX model (~320MB) is auto-downloaded on first use.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="defaultVoice">Default voice name (e.g. "af_heart", "af_sky", "am_adam"). Defaults to "af_heart".</param>
    /// <param name="modelType">ONNX model precision. Defaults to float32.</param>
    /// <param name="onDownloadProgress">Optional callback invoked during model download with progress (0.0-1.0).</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseKokoroTts(
        this RealtimeBuilder builder,
        string defaultVoice = "af_heart",
        KModel modelType = KModel.float32,
        Action<float>? onDownloadProgress = null)
    {
        builder.Services.AddSingleton<ITextToSpeechClient>(_ =>
            new KokoroTextToSpeechClientAdapter(defaultVoice, modelType)
            {
                OnDownloadProgress = onDownloadProgress
            });
        return builder;
    }

    /// <summary>
    /// Adds KokoroTTS (Kokoro-82M) as the text-to-speech provider for the real-time pipeline.
    /// Registers the <see cref="ITextToSpeechClient"/> adapter.
    /// The ONNX model (~320MB) is auto-downloaded on first use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultVoice">Default voice name (e.g. "af_heart", "af_sky", "am_adam"). Defaults to "af_heart".</param>
    /// <param name="modelType">ONNX model precision. Defaults to float32.</param>
    /// <param name="onDownloadProgress">Optional callback invoked during model download with progress (0.0-1.0).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKokoroTtsRealtime(
        this IServiceCollection services,
        string defaultVoice = "af_heart",
        KModel modelType = KModel.float32,
        Action<float>? onDownloadProgress = null)
    {
        services.AddSingleton<ITextToSpeechClient>(_ =>
            new KokoroTextToSpeechClientAdapter(defaultVoice, modelType)
            {
                OnDownloadProgress = onDownloadProgress
            });
        return services;
    }
}
