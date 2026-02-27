using ElBruno.Realtime;
using ElBruno.VibeVoiceTTS;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.VibeVoiceTTS.Realtime;

/// <summary>
/// Extension methods for registering VibeVoiceTTS as the TTS provider
/// with the real-time conversation pipeline.
/// </summary>
public static class VibeVoiceTtsRealtimeExtensions
{
    /// <summary>
    /// Adds VibeVoiceTTS as the text-to-speech provider for the real-time pipeline.
    /// Registers both the <see cref="VibeVoiceSynthesizer"/> and the
    /// <see cref="ITextToSpeechClient"/> adapter in a single call.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="defaultVoice">Default voice preset (e.g. "Carter", "Emma"). Defaults to "Carter".</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseVibeVoiceTts(this RealtimeBuilder builder, string defaultVoice = "Carter")
    {
        builder.Services.AddSingleton<VibeVoiceSynthesizer>();
        builder.Services.AddSingleton<ITextToSpeechClient>(sp =>
            new VibeVoiceTextToSpeechClientAdapter(
                sp.GetRequiredService<VibeVoiceSynthesizer>(), defaultVoice));
        return builder;
    }

    /// <summary>
    /// Adds VibeVoiceTTS as the text-to-speech provider for the real-time pipeline.
    /// Registers both the <see cref="VibeVoiceSynthesizer"/> and the
    /// <see cref="ITextToSpeechClient"/> adapter in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultVoice">Default voice preset (e.g. "Carter", "Emma"). Defaults to "Carter".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVibeVoiceTtsRealtime(this IServiceCollection services, string defaultVoice = "Carter")
    {
        services.AddSingleton<VibeVoiceSynthesizer>();
        services.AddSingleton<ITextToSpeechClient>(sp =>
            new VibeVoiceTextToSpeechClientAdapter(
                sp.GetRequiredService<VibeVoiceSynthesizer>(), defaultVoice));
        return services;
    }
}
