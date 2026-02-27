namespace ElBruno.Realtime;

/// <summary>Configuration options for the real-time conversation pipeline.</summary>
public class RealtimeOptions
{
    /// <summary>Gets the speech-to-text configuration.</summary>
    public SttProviderOptions SpeechToText { get; } = new();

    /// <summary>Gets the text-to-speech configuration.</summary>
    public TtsProviderOptions TextToSpeech { get; } = new();

    /// <summary>Gets the voice activity detection configuration.</summary>
    public VadProviderOptions VoiceActivityDetection { get; } = new();

    /// <summary>Gets or sets the default system prompt for conversations.</summary>
    public string? DefaultSystemPrompt { get; set; }

    /// <summary>Gets or sets the default conversation language.</summary>
    public string? DefaultLanguage { get; set; } = "en-US";
}

/// <summary>Configuration for the STT provider.</summary>
public class SttProviderOptions
{
    /// <summary>Gets or sets the model ID (e.g., "whisper-tiny.en", "whisper-base.en").</summary>
    public string ModelId { get; set; } = "whisper-tiny.en";

    /// <summary>Gets or sets the speech language.</summary>
    public string? Language { get; set; }
}

/// <summary>Configuration for the TTS provider.</summary>
public class TtsProviderOptions
{
    /// <summary>Gets or sets the model ID.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the default voice ID.</summary>
    public string? VoiceId { get; set; }
}

/// <summary>Configuration for the VAD provider.</summary>
public class VadProviderOptions
{
    /// <summary>Gets or sets the speech detection threshold (0.0–1.0).</summary>
    public float SpeechThreshold { get; set; } = 0.5f;

    /// <summary>Gets or sets the minimum silence duration to end a segment (ms).</summary>
    public int MinSilenceDurationMs { get; set; } = 300;
}
