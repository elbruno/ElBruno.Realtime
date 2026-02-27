namespace ElBruno.Realtime;

/// <summary>Represents a detected speech segment from voice activity detection.</summary>
public class SpeechSegment
{
    /// <summary>Gets or sets the PCM audio data of the speech segment.</summary>
    public byte[] AudioData { get; set; } = [];

    /// <summary>Gets or sets the start time relative to the stream beginning.</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Gets or sets the end time relative to the stream beginning.</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Gets or sets the confidence that this segment contains speech (0.0–1.0).</summary>
    public float Confidence { get; set; }

    /// <summary>Gets the duration of this speech segment.</summary>
    public TimeSpan Duration => EndTime - StartTime;
}
