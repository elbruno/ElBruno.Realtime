using ElBruno.Realtime;
using KokoroSharp;
using KokoroSharp.Core;

namespace ElBruno.KokoroTTS.Realtime;

/// <summary>
/// Adapts <see cref="KokoroTTS"/> to the
/// <see cref="ITextToSpeechClient"/> interface used by the Realtime pipeline.
/// </summary>
public sealed class KokoroTextToSpeechClientAdapter : ITextToSpeechClient
{
    private KokoroSharp.KokoroTTS? _tts;
    private KokoroVoice? _voice;
    private readonly string _defaultVoiceName;
    private readonly KModel _modelType;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Optional callback invoked during model download with progress percentage (0.0 to 1.0).
    /// </summary>
    public Action<float>? OnDownloadProgress { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KokoroTextToSpeechClientAdapter"/> class.
    /// </summary>
    /// <param name="defaultVoiceName">Default voice name. Defaults to "af_heart".</param>
    /// <param name="modelType">ONNX model precision. Defaults to float32.</param>
    public KokoroTextToSpeechClientAdapter(
        string defaultVoiceName = "af_heart",
        KModel modelType = KModel.float32)
    {
        _defaultVoiceName = defaultVoiceName;
        _modelType = modelType;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> GetSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        await EnsureModelReadyAsync();

        var voiceName = options?.VoiceId ?? _defaultVoiceName;
        var voice = voiceName == _defaultVoiceName
            ? _voice!
            : KokoroVoiceManager.GetVoice(voiceName);

        // Collect all audio segments via the job callback
        var allSamples = new List<float[]>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var job = _tts!.EnqueueJob(KokoroJob.Create(
            KokoroSharp.Processing.Tokenizer.Tokenize(text.Trim(), voice.GetLangCode()),
            voice,
            options?.Speed ?? 1f,
            samples =>
            {
                lock (allSamples)
                {
                    allSamples.Add(samples);
                }
            }));

        // Wait for the job to complete
        _ = Task.Run(async () =>
        {
            while (!job.isDone && !cancellationToken.IsCancellationRequested)
                await Task.Delay(10, CancellationToken.None);
            tcs.TrySetResult(true);
        }, CancellationToken.None);

        await tcs.Task;
        cancellationToken.ThrowIfCancellationRequested();

        // Combine all segments
        var totalLength = allSamples.Sum(s => s.Length);
        var combined = new float[totalLength];
        var offset = 0;
        foreach (var segment in allSamples)
        {
            Array.Copy(segment, 0, combined, offset, segment.Length);
            offset += segment.Length;
        }

        var wavData = ConvertToWav(combined, sampleRate: 24000);

        return new TextToSpeechResponse
        {
            AudioData = wavData,
            AudioStream = new MemoryStream(wavData),
            MediaType = "audio/wav",
            SampleRate = 24000,
            ModelId = options?.ModelId ?? "kokoro-82m",
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

        await EnsureModelReadyAsync();

        var voiceName = options?.VoiceId ?? _defaultVoiceName;
        var voice = voiceName == _defaultVoiceName
            ? _voice!
            : KokoroVoiceManager.GetVoice(voiceName);

        // Use a channel to stream segments as they complete
        var channel = System.Threading.Channels.Channel.CreateUnbounded<float[]>();

        var job = _tts!.EnqueueJob(KokoroJob.Create(
            KokoroSharp.Processing.Tokenizer.Tokenize(text.Trim(), voice.GetLangCode()),
            voice,
            options?.Speed ?? 1f,
            samples => channel.Writer.TryWrite(samples)));

        // Close the channel when job completes
        _ = Task.Run(async () =>
        {
            while (!job.isDone && !cancellationToken.IsCancellationRequested)
                await Task.Delay(10, CancellationToken.None);
            channel.Writer.TryComplete();
        }, CancellationToken.None);

        await foreach (var samples in channel.Reader.ReadAllAsync(cancellationToken))
        {
            var wavData = ConvertToWav(samples, sampleRate: 24000);
            yield return new TextToSpeechResponseUpdate
            {
                Kind = TextToSpeechUpdateKind.AudioChunk,
                AudioData = wavData,
                SampleRate = 24000,
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
        if (serviceType == typeof(KokoroTextToSpeechClientAdapter) || serviceType == typeof(ITextToSpeechClient))
            return this;
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tts?.Dispose();
        _initLock.Dispose();
    }

    private async Task EnsureModelReadyAsync()
    {
        if (_tts is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_tts is not null) return;

            _tts = await KokoroSharp.KokoroTTS.LoadModelAsync(
                _modelType,
                OnDownloadProgress);

            _voice = KokoroVoiceManager.GetVoice(_defaultVoiceName);
        }
        finally
        {
            _initLock.Release();
        }
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
