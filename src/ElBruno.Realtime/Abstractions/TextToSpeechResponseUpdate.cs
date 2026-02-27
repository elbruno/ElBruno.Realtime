namespace ElBruno.Realtime;

/// <summary>Represents a single streaming chunk from a text-to-speech operation.</summary>
public class TextToSpeechResponseUpdate
{
    /// <summary>Gets or sets the kind of this update.</summary>
    public TextToSpeechUpdateKind Kind { get; set; }

    /// <summary>Gets or sets the audio data chunk (PCM bytes).</summary>
    public byte[]? AudioData { get; set; }

    /// <summary>Gets or sets the sample rate of the audio chunk.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets any additional properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }
}

/// <summary>Describes the kind of a text-to-speech streaming update.</summary>
public enum TextToSpeechUpdateKind
{
    /// <summary>The synthesis session has started.</summary>
    SessionOpen,

    /// <summary>An audio data chunk is available.</summary>
    AudioChunk,

    /// <summary>The synthesis session has completed.</summary>
    SessionClose,

    /// <summary>A non-blocking error occurred.</summary>
    Error,
}
