namespace ElBruno.Realtime;

/// <summary>Represents the result of a text-to-speech request.</summary>
public class TextToSpeechResponse : IDisposable
{
    /// <summary>Gets or sets the synthesized audio data stream.</summary>
    public Stream? AudioStream { get; set; }

    /// <summary>Gets or sets the audio data as a byte array (alternative to AudioStream).</summary>
    public byte[]? AudioData { get; set; }

    /// <summary>Gets or sets the media type of the audio (e.g., "audio/wav", "audio/pcm").</summary>
    public string? MediaType { get; set; }

    /// <summary>Gets or sets the sample rate of the output audio in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets the duration of the synthesized audio.</summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>Gets or sets the model ID used for synthesis.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the raw representation from the underlying implementation.</summary>
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties.</summary>
    public IDictionary<string, object?>? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        AudioStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
