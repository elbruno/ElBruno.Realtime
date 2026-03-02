using System.Text.RegularExpressions;

namespace ElBruno.Realtime;

/// <summary>Options for a real-time conversation session.</summary>
public class ConversationOptions
{
    private static readonly Regex SessionIdPattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private const int MaxSessionIdLength = 256;
    
    private string? _sessionId;

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

    /// <summary>
    /// Gets or sets the session identifier for per-user conversation history.
    /// When null, a default shared session is used (backward-compatible single-user mode).
    /// Must be alphanumeric with dashes/underscores only, max 256 characters.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when SessionId exceeds 256 characters or contains invalid characters.</exception>
    public string? SessionId 
    { 
        get => _sessionId;
        set
        {
            if (value != null)
            {
                if (value.Length > MaxSessionIdLength)
                    throw new ArgumentException($"SessionId must not exceed {MaxSessionIdLength} characters.", nameof(SessionId));
                
                if (!SessionIdPattern.IsMatch(value))
                    throw new ArgumentException("SessionId must contain only alphanumeric characters, dashes, and underscores.", nameof(SessionId));
            }
            _sessionId = value;
        }
    }

    /// <summary>Gets or sets any additional properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }
}
