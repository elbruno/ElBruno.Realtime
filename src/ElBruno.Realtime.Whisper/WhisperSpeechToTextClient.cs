using Microsoft.Extensions.AI;
using Whisper.net;

namespace ElBruno.Realtime.Whisper;

/// <summary>
/// An <see cref="ISpeechToTextClient"/> implementation backed by Whisper.net for local speech-to-text.
/// Automatically downloads the Whisper GGML model on first use.
/// </summary>
/// <remarks>
/// <para>Supports both batch (<see cref="GetTextAsync"/>) and streaming (<see cref="GetStreamingTextAsync"/>) transcription.</para>
/// <para>Audio input must be 16kHz, 16-bit PCM WAV format (mono). Use NAudio or similar to convert if needed.</para>
/// </remarks>
public class WhisperSpeechToTextClient : ISpeechToTextClient
{
    private readonly string _modelId;
    private readonly string? _cacheDir;
    private readonly string? _language;
    private WhisperFactory? _factory;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Creates a new <see cref="WhisperSpeechToTextClient"/> with the specified model.
    /// </summary>
    /// <param name="modelId">Whisper model identifier (e.g., "whisper-tiny.en", "whisper-base.en"). Default: "whisper-tiny.en".</param>
    /// <param name="cacheDir">Optional directory for caching downloaded models.</param>
    /// <param name="language">Optional language hint (e.g., "en"). Default: auto-detect.</param>
    public WhisperSpeechToTextClient(
        string modelId = "whisper-tiny.en",
        string? cacheDir = null,
        string? language = null)
    {
        _modelId = modelId;
        _cacheDir = cacheDir;
        _language = language;
    }

    /// <summary>
    /// Creates a new <see cref="WhisperSpeechToTextClient"/> from a pre-downloaded model file.
    /// </summary>
    /// <param name="modelPath">Path to the GGML model file.</param>
    /// <param name="language">Optional language hint.</param>
    /// <returns>A new client instance.</returns>
    public static WhisperSpeechToTextClient FromModelPath(string modelPath, string? language = null)
    {
        var client = new WhisperSpeechToTextClient("custom", language: language);
        client._factory = WhisperFactory.FromPath(modelPath);
        return client;
    }

    /// <inheritdoc />
    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(cancellationToken);

        var language = options?.SpeechLanguage ?? _language ?? "auto";

        using var processor = _factory!.CreateBuilder()
            .WithLanguage(language)
            .Build();

        var segments = new List<TextContent>();
        TimeSpan? firstStart = null;
        TimeSpan? lastEnd = null;

        await foreach (var segment in processor.ProcessAsync(audioSpeechStream, cancellationToken))
        {
            segments.Add(new TextContent(segment.Text));
            firstStart ??= segment.Start;
            lastEnd = segment.End;
        }

        return new SpeechToTextResponse(segments.Cast<AIContent>().ToList())
        {
            ModelId = options?.ModelId ?? _modelId,
            StartTime = firstStart,
            EndTime = lastEnd,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(cancellationToken);

        var language = options?.SpeechLanguage ?? _language ?? "auto";

        using var processor = _factory!.CreateBuilder()
            .WithLanguage(language)
            .Build();

        // Yield session open
        yield return new SpeechToTextResponseUpdate
        {
            Kind = SpeechToTextResponseUpdateKind.SessionOpen,
            ModelId = options?.ModelId ?? _modelId,
        };

        await foreach (var segment in processor.ProcessAsync(audioSpeechStream, cancellationToken))
        {
            yield return new SpeechToTextResponseUpdate(segment.Text)
            {
                Kind = SpeechToTextResponseUpdateKind.TextUpdated,
                StartTime = segment.Start,
                EndTime = segment.End,
                ModelId = options?.ModelId ?? _modelId,
            };
        }

        // Yield session close
        yield return new SpeechToTextResponseUpdate
        {
            Kind = SpeechToTextResponseUpdateKind.SessionClose,
            ModelId = options?.ModelId ?? _modelId,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(WhisperSpeechToTextClient) || serviceType == typeof(ISpeechToTextClient))
            return this;

        return null;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_factory is not null) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_factory is not null) return;

            var modelPath = await WhisperModelManager.EnsureModelAsync(
                _modelId, _cacheDir, cancellationToken);
            _factory = WhisperFactory.FromPath(modelPath);
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

        _factory?.Dispose();
        _factory = null;
        _initLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
