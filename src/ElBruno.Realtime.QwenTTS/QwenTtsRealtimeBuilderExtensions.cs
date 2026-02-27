using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.QwenTTS;

/// <summary>
/// Extension methods for registering QwenTTS with the real-time conversation pipeline.
/// </summary>
public static class QwenTtsRealtimeBuilderExtensions
{
    /// <summary>
    /// Adds QwenTTS as the text-to-speech provider.
    /// </summary>
    /// <param name="builder">The real-time builder.</param>
    /// <param name="defaultVoice">Default voice/speaker (e.g., "ryan", "serena"). Default: "ryan".</param>
    /// <param name="defaultLanguage">Default language. Default: "auto".</param>
    /// <param name="modelDir">Optional model cache directory.</param>
    /// <returns>The builder for chaining.</returns>
    public static RealtimeBuilder UseQwenTts(
        this RealtimeBuilder builder,
        string defaultVoice = "ryan",
        string defaultLanguage = "auto",
        string? modelDir = null)
    {
        builder.Options.TextToSpeech.VoiceId = defaultVoice;

        builder.Services.AddSingleton<ITextToSpeechClient>(
            _ => new QwenTextToSpeechClient(defaultVoice, defaultLanguage, modelDir));

        return builder;
    }
}
