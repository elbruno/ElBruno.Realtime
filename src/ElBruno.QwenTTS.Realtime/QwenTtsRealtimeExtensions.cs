using ElBruno.QwenTTS.Pipeline;
using ElBruno.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.QwenTTS.Realtime;

/// <summary>
/// Extension methods for registering QwenTTS as the TTS provider
/// with the real-time conversation pipeline.
/// </summary>
public static class QwenTtsRealtimeExtensions
{
    /// <summary>
    /// Adds QwenTTS as the text-to-speech provider for the real-time pipeline.
    /// Registers both the <c>ITtsPipeline</c> (via <c>AddQwenTts()</c>) and the
    /// <see cref="ITextToSpeechClient"/> adapter in a single call.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseQwenTts(this RealtimeBuilder builder)
    {
        builder.Services.AddQwenTts();
        builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
        return builder;
    }

    /// <summary>
    /// Adds QwenTTS as the text-to-speech provider for the real-time pipeline.
    /// Registers both the <c>ITtsPipeline</c> (via <c>AddQwenTts()</c>) and the
    /// <see cref="ITextToSpeechClient"/> adapter in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQwenTtsRealtime(this IServiceCollection services)
    {
        services.AddQwenTts();
        services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
        return services;
    }
}
