namespace ElBruno.Realtime;

/// <summary>Options for a real-time conversation session.</summary>
public class ConversationOptions
{
    /// <summary>Gets or sets the system prompt for the LLM.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Gets or sets the voice ID for TTS output.</summary>
    public string? VoiceId { get; set; }

    /// <summary>Gets or sets the language for STT and TTS (e.g., "en-US").</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets whether barge-in is enabled (user can interrupt AI). Default: true.</summary>
    public bool EnableBargeIn { get; set; } = true;

    /// <summary>Gets or sets the maximum number of conversation history turns to maintain. Default: 20.</summary>
    public int MaxConversationHistory { get; set; } = 20;

    /// <summary>Gets or sets whether to generate spoken audio responses. Default: true.</summary>
    public bool EnableAudioResponse { get; set; } = true;

    /// <summary>Gets or sets any additional properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }
}
