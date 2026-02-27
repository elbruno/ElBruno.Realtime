using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Scenario04.Api.Services;
using Scenario04.Shared.Models;

namespace Scenario04.Api.Hubs;

/// <summary>
/// SignalR hub for real-time conversation between the Blazor frontend and the
/// Ollama-powered backend via Microsoft Agent Framework.
///
/// Architecture:
///   Browser ──SignalR──► ConversationHub ──M.E.AI──► Ollama (phi4-mini)
///                                       ──Whisper──► Server-side STT
///
/// Uses Microsoft.Extensions.AI.Ollama (OllamaChatClient) as the IChatClient,
/// following the Agent Framework pattern from:
/// https://learn.microsoft.com/agent-framework/agents/providers/ollama
///
/// For multi-turn streaming: ConversationService manages chat history per session.
/// For one-shot agent queries: Uses IChatClient.AsAIAgent() directly.
/// For server-side audio: Uses ISpeechToTextClient (Whisper.net) for transcription.
///
/// The hub uses MessagePack protocol for efficient binary transfer (audio chunks).
/// </summary>
public sealed class ConversationHub : Hub
{
    private readonly ConversationService _conversation;
    private readonly IChatClient _chatClient;
    private readonly ISpeechToTextClient? _sttClient;
    private readonly ILogger<ConversationHub> _logger;

    public ConversationHub(
        ConversationService conversation,
        IChatClient chatClient,
        ILogger<ConversationHub> logger,
        ISpeechToTextClient? sttClient = null)
    {
        _conversation = conversation;
        _chatClient = chatClient;
        _logger = logger;
        _sttClient = sttClient;
    }

    // ──────────────────────────────────────────────────────────
    // Text conversation (streaming response)
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Send a text message and receive a streaming response from Ollama.
    /// The response is streamed token-by-token to the client.
    /// </summary>
    public async IAsyncEnumerable<string> SendMessage(string sessionId, string message, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: SendMessage from {ConnectionId}, session={Session}", Context.ConnectionId, sessionId);

        await foreach (var token in _conversation.ChatStreamAsync(sessionId, message, personaPrompt))
        {
            yield return token;
        }
    }

    /// <summary>
    /// Send a text message and receive a complete (non-streaming) response.
    /// </summary>
    public async Task<string> SendMessageComplete(string sessionId, string message, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: SendMessageComplete from {ConnectionId}, session={Session}", Context.ConnectionId, sessionId);
        return await _conversation.ChatAsync(sessionId, message, personaPrompt);
    }

    // ──────────────────────────────────────────────────────────
    // Audio conversation
    // Speech-to-text is handled client-side via the Web Speech API.
    // The transcribed text flows through SendMessage (above).
    // This method is kept for future server-side audio processing
    // when PersonaPlex ONNX models are integrated.
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Process raw audio bytes through the conversation pipeline.
    /// Uses ISpeechToTextClient (Whisper.net) for server-side transcription,
    /// then routes the transcribed text through the LLM for a response.
    /// Falls back to browser-side STT guidance if Whisper is not available.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessAudio(string sessionId, byte[] audioData, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: ProcessAudio from {ConnectionId}, {Bytes} bytes", Context.ConnectionId, audioData.Length);

        if (_sttClient is null)
        {
            // No STT client registered — guide user to browser-side STT
            await foreach (var token in _conversation.ChatStreamAsync(
                sessionId,
                "[Audio received — server-side STT not configured. Use the mic button for browser-based voice input.]",
                personaPrompt))
            {
                yield return token;
            }
            yield break;
        }

        // Server-side STT: transcribe audio using Whisper
        using var audioStream = new MemoryStream(audioData);
        var sttResponse = await _sttClient.GetTextAsync(audioStream);
        var transcribedText = sttResponse.Text;

        _logger.LogInformation("Hub: Transcribed audio to: \"{Text}\"", transcribedText);

        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            yield return "[No speech detected in audio]";
            yield break;
        }

        // Route transcribed text through the LLM
        await foreach (var token in _conversation.ChatStreamAsync(sessionId, transcribedText, personaPrompt))
        {
            yield return token;
        }
    }

    // ──────────────────────────────────────────────────────────
    // One-shot Agent query (Microsoft Agent Framework pattern)
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Execute a one-shot agent query using the Microsoft Agent Framework pattern.
    /// Creates an AIAgent from the OllamaChatClient with custom instructions.
    ///
    /// This follows the pattern from:
    /// https://learn.microsoft.com/agent-framework/agents/providers/ollama
    ///
    /// <code>
    /// var agent = chatClient.AsAIAgent(instructions: "You are a helpful assistant.");
    /// var result = await agent.RunAsync("What is the largest city in France?");
    /// </code>
    /// </summary>
    public async Task<string> AgentQuery(string question, string? instructions = null)
    {
        _logger.LogInformation("Hub: AgentQuery from {ConnectionId}", Context.ConnectionId);

        var agent = _chatClient.AsAIAgent(
            instructions: instructions ?? "You are a helpful, concise assistant. Keep responses to 1-3 sentences.");

        var result = await agent.RunAsync(question);
        return result.Text ?? string.Empty;
    }

    // ──────────────────────────────────────────────────────────
    // Session management
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Clear the conversation history for a session.
    /// </summary>
    public void ClearSession(string sessionId)
    {
        _conversation.ClearSession(sessionId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
