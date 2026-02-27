using Microsoft.AspNetCore.SignalR;
using ElBruno.Realtime;

namespace Scenario07RealtimeApi;

/// <summary>
/// SignalR hub for streaming real-time conversations.
/// Receives audio chunks from clients and streams back conversation events.
/// </summary>
public class ConversationHub : Hub
{
    private readonly IRealtimeConversationClient _conversation;

    // Max 10MB of base64 (~7.5MB decoded audio, ~4 minutes of 16kHz PCM)
    private const int MaxBase64Length = 10 * 1024 * 1024;

    public ConversationHub(IRealtimeConversationClient conversation)
    {
        _conversation = conversation;
    }

    /// <summary>
    /// Process a single audio file and return the text response.
    /// Client sends a base64 WAV, gets back transcription and AI response.
    /// </summary>
    public async Task<object> ProcessTurn(string audioBase64)
    {
        if (string.IsNullOrEmpty(audioBase64) || audioBase64.Length > MaxBase64Length)
            throw new HubException($"Audio data must be non-empty and less than {MaxBase64Length / 1024 / 1024}MB.");

        var audioBytes = Convert.FromBase64String(audioBase64);
        using var audioStream = new MemoryStream(audioBytes);

        var turn = await _conversation.ProcessTurnAsync(audioStream, new ConversationOptions
        {
            EnableAudioResponse = false, // Text only for simplicity
        });

        return new
        {
            userText = turn.UserText,
            responseText = turn.ResponseText,
            processingTimeMs = turn.ProcessingTime.TotalMilliseconds,
        };
    }

    /// <summary>
    /// Streaming conversation: client sends audio chunks, server streams events back.
    /// </summary>
    public async IAsyncEnumerable<ConversationEventDto> StreamConversation(
        IAsyncEnumerable<byte[]> audioChunks,
        string? systemPrompt = null)
    {
        var options = new ConversationOptions
        {
            SystemPrompt = systemPrompt,
            EnableAudioResponse = false, // Streaming TTS over SignalR is complex; text for now
        };

        await foreach (var evt in _conversation.ConverseAsync(audioChunks, options))
        {
            yield return new ConversationEventDto
            {
                Kind = evt.Kind.ToString(),
                TranscribedText = evt.TranscribedText,
                ResponseText = evt.ResponseText,
                Timestamp = evt.Timestamp,
            };
        }
    }
}

/// <summary>DTO for serializing conversation events over SignalR.</summary>
public class ConversationEventDto
{
    public string Kind { get; set; } = string.Empty;
    public string? TranscribedText { get; set; }
    public string? ResponseText { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
