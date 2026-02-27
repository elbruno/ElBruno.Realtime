using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ElBruno.Realtime.Pipeline;

/// <summary>
/// Default implementation of <see cref="IRealtimeConversationClient"/> that chains
/// VAD → STT → LLM → TTS into a seamless real-time conversation pipeline.
/// </summary>
/// <remarks>
/// All component providers are injected via DI. The pipeline:
/// 1. Detects speech using <see cref="IVoiceActivityDetector"/>
/// 2. Transcribes speech using <see cref="ISpeechToTextClient"/>
/// 3. Generates response using <see cref="IChatClient"/>
/// 4. Synthesizes audio using <see cref="ITextToSpeechClient"/>
/// </remarks>
public class RealtimeConversationPipeline : IRealtimeConversationClient
{
    private readonly IVoiceActivityDetector? _vad;
    private readonly ISpeechToTextClient _stt;
    private readonly IChatClient _chatClient;
    private readonly ITextToSpeechClient? _tts;
    private readonly RealtimeOptions _options;
    private readonly List<ChatMessage> _conversationHistory = [];
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="RealtimeConversationPipeline"/>.
    /// </summary>
    public RealtimeConversationPipeline(
        ISpeechToTextClient stt,
        IChatClient chatClient,
        RealtimeOptions options,
        IVoiceActivityDetector? vad = null,
        ITextToSpeechClient? tts = null)
    {
        _stt = stt ?? throw new ArgumentNullException(nameof(stt));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _vad = vad;
        _tts = tts;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ConversationEvent> ConverseAsync(
        IAsyncEnumerable<byte[]> audioInput,
        ConversationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var systemPrompt = options?.SystemPrompt ?? _options.DefaultSystemPrompt;
        var enableAudio = options?.EnableAudioResponse ?? true;
        var maxHistory = options?.MaxConversationHistory ?? 20;

        // Initialize conversation history with system prompt
        if (systemPrompt is not null && _conversationHistory.Count == 0)
        {
            _conversationHistory.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }

        if (_vad is null)
        {
            // No VAD: treat entire stream as one speech segment
            var allAudio = new List<byte>();
            await foreach (var chunk in audioInput.WithCancellation(cancellationToken))
                allAudio.AddRange(chunk);

            await foreach (var evt in ProcessSpeechSegmentAsync(
                allAudio.ToArray(), options, cancellationToken))
            {
                yield return evt;
            }
            yield break;
        }

        // With VAD: detect speech segments and process each
        await foreach (var segment in _vad.DetectSpeechAsync(audioInput, cancellationToken: cancellationToken))
        {
            yield return new ConversationEvent
            {
                Kind = ConversationEventKind.SpeechDetected,
                SpeechSegment = segment,
            };

            await foreach (var evt in ProcessSpeechSegmentAsync(
                segment.AudioData, options, cancellationToken))
            {
                yield return evt;
            }

            // Trim history
            TrimHistory(maxHistory);
        }
    }

    /// <inheritdoc />
    public async Task<ConversationTurn> ProcessTurnAsync(
        Stream audioInput,
        ConversationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var startTime = DateTimeOffset.UtcNow;
        var systemPrompt = options?.SystemPrompt ?? _options.DefaultSystemPrompt;

        if (systemPrompt is not null && _conversationHistory.Count == 0)
        {
            _conversationHistory.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }

        // Step 1: STT
        var sttResponse = await _stt.GetTextAsync(audioInput, cancellationToken: cancellationToken);
        var userText = sttResponse.Text;

        // Step 2: LLM
        _conversationHistory.Add(new ChatMessage(ChatRole.User, userText));

        var chatResponse = await _chatClient.GetResponseAsync(
            _conversationHistory, cancellationToken: cancellationToken);
        var responseText = chatResponse.Text;

        _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, responseText));

        // Step 3: TTS (optional)
        Stream? responseAudio = null;
        string? audioMediaType = null;

        if (_tts is not null && (options?.EnableAudioResponse ?? true))
        {
            var ttsOptions = new TextToSpeechOptions
            {
                VoiceId = options?.VoiceId ?? _options.TextToSpeech.VoiceId,
                Language = options?.Language ?? _options.DefaultLanguage,
            };

            var ttsResponse = await _tts.GetSpeechAsync(responseText, ttsOptions, cancellationToken);
            responseAudio = ttsResponse.AudioStream;
            audioMediaType = ttsResponse.MediaType;
        }

        var processingTime = DateTimeOffset.UtcNow - startTime;

        return new ConversationTurn
        {
            UserText = userText,
            ResponseText = responseText,
            ResponseAudio = responseAudio,
            AudioMediaType = audioMediaType,
            ProcessingTime = processingTime,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(RealtimeConversationPipeline)
            || serviceType == typeof(IRealtimeConversationClient))
            return this;

        return null;
    }

    private async IAsyncEnumerable<ConversationEvent> ProcessSpeechSegmentAsync(
        byte[] audioData,
        ConversationOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // STT
        using var audioStream = new MemoryStream(audioData);
        var sttResponse = await _stt.GetTextAsync(audioStream, cancellationToken: cancellationToken);
        var userText = sttResponse.Text;

        yield return new ConversationEvent
        {
            Kind = ConversationEventKind.TranscriptionComplete,
            TranscribedText = userText,
        };

        if (string.IsNullOrWhiteSpace(userText))
            yield break;

        // LLM
        _conversationHistory.Add(new ChatMessage(ChatRole.User, userText));

        yield return new ConversationEvent
        {
            Kind = ConversationEventKind.ResponseStarted,
        };

        var responseBuilder = new System.Text.StringBuilder();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            _conversationHistory, cancellationToken: cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent textContent && textContent.Text is not null)
                {
                    responseBuilder.Append(textContent.Text);
                    yield return new ConversationEvent
                    {
                        Kind = ConversationEventKind.ResponseTextChunk,
                        ResponseText = textContent.Text,
                    };
                }
            }
        }

        var responseText = responseBuilder.ToString();
        _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, responseText));

        // TTS
        if (_tts is not null && (options?.EnableAudioResponse ?? true) && !string.IsNullOrWhiteSpace(responseText))
        {
            var ttsOptions = new TextToSpeechOptions
            {
                VoiceId = options?.VoiceId ?? _options.TextToSpeech.VoiceId,
                Language = options?.Language ?? _options.DefaultLanguage,
            };

            await foreach (var ttsUpdate in _tts.GetStreamingSpeechAsync(
                responseText, ttsOptions, cancellationToken))
            {
                if (ttsUpdate.Kind == TextToSpeechUpdateKind.AudioChunk && ttsUpdate.AudioData is not null)
                {
                    yield return new ConversationEvent
                    {
                        Kind = ConversationEventKind.ResponseAudioChunk,
                        ResponseAudio = ttsUpdate.AudioData,
                    };
                }
            }
        }

        yield return new ConversationEvent
        {
            Kind = ConversationEventKind.ResponseComplete,
            ResponseText = responseText,
        };
    }

    private void TrimHistory(int maxTurns)
    {
        // Keep system prompt + last N messages
        if (_conversationHistory.Count <= maxTurns + 1) return;

        var systemMessage = _conversationHistory.FirstOrDefault(m => m.Role == ChatRole.System);
        var recent = _conversationHistory
            .Where(m => m.Role != ChatRole.System)
            .TakeLast(maxTurns)
            .ToList();

        _conversationHistory.Clear();
        if (systemMessage is not null)
            _conversationHistory.Add(systemMessage);
        _conversationHistory.AddRange(recent);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
