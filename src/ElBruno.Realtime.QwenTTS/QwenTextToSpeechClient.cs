using ElBruno.QwenTTS.Pipeline;

namespace ElBruno.Realtime.QwenTTS;

/// <summary>
/// An <see cref="ITextToSpeechClient"/> implementation backed by QwenTTS for local text-to-speech.
/// Automatically downloads Qwen3-TTS ONNX models on first use.
/// </summary>
/// <remarks>
/// QwenTTS produces 24kHz WAV audio. The pipeline is initialized lazily and shared across calls.
/// </remarks>
public class QwenTextToSpeechClient : ITextToSpeechClient
{
    private readonly string _defaultVoice;
    private readonly string _defaultLanguage;
    private readonly string? _modelDir;
    private TtsPipeline? _pipeline;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Creates a new <see cref="QwenTextToSpeechClient"/>.
    /// </summary>
    /// <param name="defaultVoice">Default voice/speaker name (e.g., "ryan", "serena"). Default: "ryan".</param>
    /// <param name="defaultLanguage">Default language (e.g., "english", "auto"). Default: "auto".</param>
    /// <param name="modelDir">Optional model directory. Uses QwenTTS default if null.</param>
    public QwenTextToSpeechClient(
        string defaultVoice = "ryan",
        string defaultLanguage = "auto",
        string? modelDir = null)
    {
        _defaultVoice = defaultVoice;
        _defaultLanguage = defaultLanguage;
        _modelDir = modelDir;
    }

    /// <inheritdoc />
    public async Task<TextToSpeechResponse> GetSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        await EnsureInitializedAsync(cancellationToken);

        var voice = options?.VoiceId ?? _defaultVoice;
        var language = options?.Language ?? _defaultLanguage;

        // QwenTTS writes to disk; use temp file then read into memory
        var tempPath = Path.Combine(Path.GetTempPath(), $"qwentts_{Guid.NewGuid():N}.wav");
        try
        {
            await _pipeline!.SynthesizeAsync(text, voice, tempPath, language);

            var audioData = await File.ReadAllBytesAsync(tempPath, cancellationToken);

            return new TextToSpeechResponse
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
    public async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingSpeechAsync(
        string text,
        TextToSpeechOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // QwenTTS doesn't support native streaming, so we synthesize fully then yield a single chunk
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
        if (serviceType == typeof(QwenTextToSpeechClient) || serviceType == typeof(ITextToSpeechClient))
            return this;

        return null;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_pipeline is not null) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_pipeline is not null) return;
            _pipeline = await TtsPipeline.CreateAsync(_modelDir);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pipeline?.Dispose();
        _pipeline = null;
        _initLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
