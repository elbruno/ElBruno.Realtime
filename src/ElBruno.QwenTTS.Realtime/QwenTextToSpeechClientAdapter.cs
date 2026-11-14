using ElBruno.QwenTTS.Pipeline;
using RealtimeTextToSpeechClient = ElBruno.Realtime.ITextToSpeechClient;
using RealtimeTextToSpeechOptions = ElBruno.Realtime.TextToSpeechOptions;
using RealtimeTextToSpeechResponse = ElBruno.Realtime.TextToSpeechResponse;
using RealtimeTextToSpeechResponseUpdate = ElBruno.Realtime.TextToSpeechResponseUpdate;
using RealtimeTextToSpeechUpdateKind = ElBruno.Realtime.TextToSpeechUpdateKind;

namespace ElBruno.QwenTTS.Realtime;

/// <summary>
/// Adapts <see cref="ITtsPipeline"/> (registered by <c>AddQwenTts()</c>) to
/// the <see cref="ITextToSpeechClient"/> interface used by the Realtime pipeline.
/// </summary>
public sealed class QwenTextToSpeechClientAdapter : RealtimeTextToSpeechClient
{
    private readonly ITtsPipeline _pipeline;
    private readonly string _defaultVoice;
    private readonly string _defaultLanguage;

    /// <summary>
    /// Initializes a new instance of the <see cref="QwenTextToSpeechClientAdapter"/> class.
    /// </summary>
    /// <param name="pipeline">The QwenTTS pipeline registered by <c>AddQwenTts()</c>.</param>
    /// <param name="defaultVoice">Default voice identifier. Defaults to "ryan".</param>
    /// <param name="defaultLanguage">Default language. Defaults to "auto".</param>
    public QwenTextToSpeechClientAdapter(
        ITtsPipeline pipeline,
        string defaultVoice = "ryan",
        string defaultLanguage = "auto")
    {
        _pipeline = pipeline;
        _defaultVoice = defaultVoice;
        _defaultLanguage = defaultLanguage;
    }

    /// <inheritdoc />
    public async Task<RealtimeTextToSpeechResponse> GetSpeechAsync(
        string text,
        RealtimeTextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var voice = options?.VoiceId ?? _defaultVoice;
        // QwenTTS doesn't recognize culture codes like "en-US" — normalize to "auto"
        var rawLang = options?.Language ?? _defaultLanguage;
        var language = rawLang.Contains('-') ? "auto" : rawLang;

        var tempPath = Path.Combine(Path.GetTempPath(), $"qwentts_{Guid.NewGuid():N}.wav");
        try
        {
            await _pipeline.SynthesizeAsync(text, voice, tempPath, language);

            var audioData = await File.ReadAllBytesAsync(tempPath, cancellationToken);

            return new RealtimeTextToSpeechResponse
            {
                AudioData = audioData,
                AudioStream = new MemoryStream(audioData),
                MediaType = "audio/wav",
                SampleRate = 24000,
                ModelId = options?.ModelId ?? "qwen3-tts",
            };
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* cleanup best-effort */ }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RealtimeTextToSpeechResponseUpdate> GetStreamingSpeechAsync(
        string text,
        RealtimeTextToSpeechOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new RealtimeTextToSpeechResponseUpdate
        {
            Kind = RealtimeTextToSpeechUpdateKind.SessionOpen,
        };

        var response = await GetSpeechAsync(text, options, cancellationToken);

        if (response.AudioData is { Length: > 0 })
        {
            yield return new RealtimeTextToSpeechResponseUpdate
            {
                Kind = RealtimeTextToSpeechUpdateKind.AudioChunk,
                AudioData = response.AudioData,
                SampleRate = response.SampleRate,
            };
        }

        yield return new RealtimeTextToSpeechResponseUpdate
        {
            Kind = RealtimeTextToSpeechUpdateKind.SessionClose,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(QwenTextToSpeechClientAdapter) || serviceType == typeof(RealtimeTextToSpeechClient))
            return this;
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // ITtsPipeline lifetime is managed by DI; do not dispose here.
    }
}
