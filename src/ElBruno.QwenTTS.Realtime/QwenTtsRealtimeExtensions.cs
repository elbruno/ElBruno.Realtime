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
    /// <param name="configureOptions">Optional callback to configure QwenTTS options (e.g., GPU device selection).</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // Default (CPU execution)
    /// builder.UseQwenTts();
    /// 
    /// // GPU execution on device 1
    /// builder.UseQwenTts(opts => opts.DeviceId = 1);
    /// </code>
    /// </example>
    public static RealtimeBuilder UseQwenTts(this RealtimeBuilder builder, Action<QwenTtsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            builder.Services.AddQwenTts(configureOptions);
        }
        else
        {
            builder.Services.AddQwenTts();
        }
        builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
        return builder;
    }

    /// <summary>
    /// Adds QwenTTS as the text-to-speech provider for the real-time pipeline.
    /// Registers both the <c>ITtsPipeline</c> (via <c>AddQwenTts()</c>) and the
    /// <see cref="ITextToSpeechClient"/> adapter in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional callback to configure QwenTTS options (e.g., GPU device selection).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Default (CPU execution)
    /// services.AddQwenTtsRealtime();
    /// 
    /// // GPU execution on device 1
    /// services.AddQwenTtsRealtime(opts => opts.DeviceId = 1);
    /// </code>
    /// </example>
    public static IServiceCollection AddQwenTtsRealtime(this IServiceCollection services, Action<QwenTtsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.AddQwenTts(configureOptions);
        }
        else
        {
            services.AddQwenTts();
        }
        services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
        return services;
    }
}
