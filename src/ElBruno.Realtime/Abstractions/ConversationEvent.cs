namespace ElBruno.Realtime;

/// <summary>Represents a single event during a real-time conversation.</summary>
public class ConversationEvent
{
    /// <summary>Gets or sets the kind of this conversation event.</summary>
    public ConversationEventKind Kind { get; set; }

    /// <summary>Gets or sets the transcribed text from STT (for transcription events).</summary>
    public string? TranscribedText { get; set; }

    /// <summary>Gets or sets the LLM response text (for response events).</summary>
    public string? ResponseText { get; set; }

    /// <summary>Gets or sets the TTS audio chunk (for audio response events).</summary>
    public byte[]? ResponseAudio { get; set; }

    /// <summary>Gets or sets the detected speech segment (for speech detection events).</summary>
    public SpeechSegment? SpeechSegment { get; set; }

    /// <summary>Gets or sets error information (for error events).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the timestamp of this event.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Describes the kind of a conversation event in the real-time pipeline.</summary>
public enum ConversationEventKind
{
    /// <summary>VAD detected the start of speech.</summary>
    SpeechDetected,

    /// <summary>STT produced a partial (in-progress) transcription.</summary>
    TranscriptionPartial,

    /// <summary>STT completed transcription of a speech segment.</summary>
    TranscriptionComplete,

    /// <summary>LLM started generating a response.</summary>
    ResponseStarted,

    /// <summary>LLM produced a text token/chunk.</summary>
    ResponseTextChunk,

    /// <summary>TTS produced an audio chunk ready for playback.</summary>
    ResponseAudioChunk,

    /// <summary>A complete conversation turn has finished.</summary>
    ResponseComplete,

    /// <summary>Barge-in: user interrupted the AI response.</summary>
    Interrupted,

    /// <summary>An error occurred in the pipeline.</summary>
    Error,
}
