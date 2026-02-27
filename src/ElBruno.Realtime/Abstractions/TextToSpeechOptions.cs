namespace ElBruno.Realtime;

/// <summary>Options for a text-to-speech request.</summary>
public class TextToSpeechOptions
{
    /// <summary>Gets or sets the model ID for text-to-speech.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the voice identifier to use for synthesis.</summary>
    public string? VoiceId { get; set; }

    /// <summary>Gets or sets the language for synthesis (e.g., "en-US").</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the desired output sample rate in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets the speech speed multiplier (1.0 = normal).</summary>
    public float? Speed { get; set; }

    /// <summary>Gets or sets any additional provider-specific properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }

    /// <summary>Creates a shallow copy of this options instance.</summary>
    public virtual TextToSpeechOptions Clone() => new()
    {
        ModelId = ModelId,
        VoiceId = VoiceId,
        Language = Language,
        SampleRate = SampleRate,
        Speed = Speed,
        AdditionalProperties = AdditionalProperties is not null
            ? new Dictionary<string, object?>(AdditionalProperties)
            : null,
    };
}
