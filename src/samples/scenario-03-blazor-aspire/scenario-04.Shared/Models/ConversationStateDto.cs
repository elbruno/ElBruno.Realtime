namespace Scenario04.Shared.Models;

/// <summary>
/// Current state of a conversation session.
/// </summary>
public record ConversationStateDto
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string VoicePreset { get; init; } = "NATF2";
    public string? TextPrompt { get; init; }
    public ConversationMode Mode { get; init; } = ConversationMode.OllamaReasoning;
}

/// <summary>
/// Conversation pipeline mode.
/// </summary>
public enum ConversationMode
{
    /// <summary>Audio → Mimi Encoder → Ollama (text reasoning) → Mimi Decoder → Audio</summary>
    OllamaReasoning,

    /// <summary>Text chat only via Ollama (no audio pipeline)</summary>
    TextOnly,
}
