namespace ElBruno.Realtime.BlazorComponents;

/// <summary>Specifies which audio source to monitor in the <see cref="AudioLevelMeter"/> component.</summary>
public enum AudioSource
{
    /// <summary>Monitor the microphone (user input) audio level.</summary>
    Microphone,

    /// <summary>Monitor the speaker (TTS output) audio level.</summary>
    Speaker,
}
