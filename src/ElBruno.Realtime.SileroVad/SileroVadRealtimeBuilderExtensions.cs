using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.SileroVad;

/// <summary>
/// Extension methods for registering Silero VAD with the real-time conversation pipeline.
/// </summary>
public static class SileroVadRealtimeBuilderExtensions
{
    /// <summary>
    /// Adds Silero VAD as the voice activity detection provider.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="cacheDir">Optional model cache directory.</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseSileroVad(
        this RealtimeBuilder builder,
        string? cacheDir = null)
    {
        builder.Services.AddSingleton<IVoiceActivityDetector>(
            _ => new SileroVadDetector(cacheDir));

        return builder;
    }

    /// <summary>
    /// Adds Silero VAD from a pre-downloaded model file.
    /// </summary>
    public static RealtimeBuilder UseSileroVadFromPath(
        this RealtimeBuilder builder,
        string modelPath)
    {
        builder.Services.AddSingleton<IVoiceActivityDetector>(
            _ => SileroVadDetector.FromModelPath(modelPath));

        return builder;
    }
}
