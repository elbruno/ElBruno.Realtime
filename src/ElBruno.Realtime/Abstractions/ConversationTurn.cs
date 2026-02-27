namespace ElBruno.Realtime;

/// <summary>Represents a complete conversation turn (user speech → AI response).</summary>
public class ConversationTurn : IDisposable
{
    /// <summary>Gets or sets the transcribed text from the user's speech.</summary>
    public string UserText { get; set; } = string.Empty;

    /// <summary>Gets or sets the AI's text response.</summary>
    public string ResponseText { get; set; } = string.Empty;

    /// <summary>Gets or sets the AI's spoken response audio stream.</summary>
    public Stream? ResponseAudio { get; set; }

    /// <summary>Gets or sets the media type of the response audio (e.g., "audio/wav").</summary>
    public string? AudioMediaType { get; set; }

    /// <summary>Gets or sets the total processing time for this turn.</summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>Gets or sets the model ID used for LLM completion.</summary>
    public string? ModelId { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        ResponseAudio?.Dispose();
        GC.SuppressFinalize(this);
    }
}
