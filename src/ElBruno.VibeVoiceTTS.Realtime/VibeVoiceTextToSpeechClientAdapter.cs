using ElBruno.Realtime;
using ElBruno.VibeVoiceTTS;

namespace ElBruno.VibeVoiceTTS.Realtime;

/// <summary>
/// Adapts <see cref="VibeVoiceSynthesizer"/> to the
/// <see cref="ITextToSpeechClient"/> interface used by the Realtime pipeline.
/// </summary>
public sealed class VibeVoiceTextToSpeechClientAdapter : ITextToSpeechClient
{
    private readonly VibeVoiceSynthesizer _synthesizer;
    private readonly string _defaultVoice;
    private bool _modelReady;

    /// <summary>
    /// Initializes a new instance of the <see cref="VibeVoiceTextToSpeechClientAdapter"/> class.
    /// </summary>
    /// <param name="synthesizer">The VibeVoice synthesizer instance.</param>
    /// <param name="defaultVoice">Default voice preset. Defaults to "Carter".</param>
    public VibeVoiceTextToSpeechClientAdapter(
        VibeVoiceSynthesizer synthesizer,
        string defaultVoice = "Carter")
    {
        _synthesizer = synthesizer;
        _defaultVoice = defaultVoice;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> GetSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        await EnsureModelReadyAsync();

        var voice = options?.VoiceId ?? _defaultVoice;

        // Generate audio as float[] samples at 24kHz
        var audioSamples = await _synthesizer.GenerateAudioAsync(text, voice);

        // Convert float[] samples to 16-bit PCM WAV bytes
        var wavData = ConvertToWav(audioSamples, sampleRate: 24000);

        return new TextToSpeechResponse
        {
            AudioData = wavData,
            AudioStream = new MemoryStream(wavData),
            MediaType = "audio/wav",
            SampleRate = 24000,
            ModelId = options?.ModelId ?? "vibevoice-realtime-0.5b",
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new TextToSpeechResponseUpdate
        {
            Kind = TextToSpeechUpdateKind.SessionOpen,
        };

        var response = await GetSpeechAsync(text, options, cancellationToken);

        if (response.AudioData is { Length: > 0 })
        {
            yield return new TextToSpeechResponseUpdate
            {
                Kind = TextToSpeechUpdateKind.AudioChunk,
                AudioData = response.AudioData,
                SampleRate = response.SampleRate,
            };
        }

        yield return new TextToSpeechResponseUpdate
        {
            Kind = TextToSpeechUpdateKind.SessionClose,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(VibeVoiceTextToSpeechClientAdapter) || serviceType == typeof(ITextToSpeechClient))
            return this;
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _synthesizer.Dispose();
    }

    private async Task EnsureModelReadyAsync()
    {
        if (_modelReady) return;
        await _synthesizer.EnsureModelAvailableAsync();
        _modelReady = true;
    }

    /// <summary>
    /// Converts float[] audio samples (range -1.0 to 1.0) to a 16-bit PCM WAV byte array.
    /// </summary>
    private static byte[] ConvertToWav(float[] samples, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        var blockAlign = channels * (bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        var dataSize = samples.Length * (bitsPerSample / 8);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt sub-chunk
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data sub-chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1.0f, 1.0f);
            var pcm = (short)(clamped * short.MaxValue);
            writer.Write(pcm);
        }

        return stream.ToArray();
    }
}
