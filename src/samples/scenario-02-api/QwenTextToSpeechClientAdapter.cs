using ElBruno.QwenTTS.Pipeline;
using RealtimeTextToSpeechClient = ElBruno.Realtime.ITextToSpeechClient;
using RealtimeTextToSpeechOptions = ElBruno.Realtime.TextToSpeechOptions;
using RealtimeTextToSpeechResponse = ElBruno.Realtime.TextToSpeechResponse;
using RealtimeTextToSpeechResponseUpdate = ElBruno.Realtime.TextToSpeechResponseUpdate;
using RealtimeTextToSpeechUpdateKind = ElBruno.Realtime.TextToSpeechUpdateKind;

namespace Scenario07RealtimeApi;

/// <summary>
/// Adapts <see cref="ITtsPipeline"/> (registered by <c>AddQwenTts()</c>) to
/// the <see cref="ITextToSpeechClient"/> interface used by the Realtime pipeline.
/// </summary>
internal sealed class QwenTextToSpeechClientAdapter : RealtimeTextToSpeechClient
{
    private readonly ITtsPipeline _pipeline;
    private readonly string _defaultVoice;
    private readonly string _defaultLanguage;

    public QwenTextToSpeechClientAdapter(
        ITtsPipeline pipeline,
        string defaultVoice = "ryan",
        string defaultLanguage = "auto")
    {
        _pipeline = pipeline;
        _defaultVoice = defaultVoice;
        _defaultLanguage = defaultLanguage;
    }

    public async Task<RealtimeTextToSpeechResponse> GetSpeechAsync(
        string text,
        RealtimeTextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var voice = options?.VoiceId ?? _defaultVoice;
        var language = options?.Language ?? _defaultLanguage;

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

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(QwenTextToSpeechClientAdapter) || serviceType == typeof(RealtimeTextToSpeechClient))
            return this;
        return null;
    }

    public void Dispose()
    {
        // ITtsPipeline lifetime is managed by DI; do not dispose here.
    }
}
