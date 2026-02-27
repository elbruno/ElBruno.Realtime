namespace ElBruno.Realtime;

/// <summary>Options for voice activity detection.</summary>
public class VadOptions
{
    /// <summary>Gets or sets the speech probability threshold (0.0–1.0). Default: 0.5.</summary>
    public float SpeechThreshold { get; set; } = 0.5f;

    /// <summary>Gets or sets the minimum speech duration in milliseconds to emit a segment. Default: 250ms.</summary>
    public int MinSpeechDurationMs { get; set; } = 250;

    /// <summary>Gets or sets the minimum silence duration in milliseconds to end a segment. Default: 300ms.</summary>
    public int MinSilenceDurationMs { get; set; } = 300;

    /// <summary>Gets or sets the input audio sample rate in Hz. Default: 16000.</summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>Gets or sets the number of audio channels. Default: 1 (mono).</summary>
    public int Channels { get; set; } = 1;

    /// <summary>Gets or sets any additional provider-specific properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }

    /// <summary>Creates a shallow copy of this options instance.</summary>
    public virtual VadOptions Clone() => new()
    {
        SpeechThreshold = SpeechThreshold,
        MinSpeechDurationMs = MinSpeechDurationMs,
        MinSilenceDurationMs = MinSilenceDurationMs,
        SampleRate = SampleRate,
        Channels = Channels,
        AdditionalProperties = AdditionalProperties is not null
            ? new Dictionary<string, object?>(AdditionalProperties)
            : null,
    };
}
