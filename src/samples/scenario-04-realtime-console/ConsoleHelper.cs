using NAudio.Wave;

namespace Scenario04RealtimeConsole;

/// <summary>
/// Shared console utilities for logging, microphone selection, and mode selection.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Logs a message with a timestamp prefix in [HH:mm:ss] format.
    /// </summary>
    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("[HH:mm:ss]");
        Console.WriteLine($"{timestamp} {message}");
    }

    /// <summary>
    /// Displays available microphones and lets the user pick one.
    /// Press ENTER to use the default (device 0).
    /// Returns the selected device index, or -1 if no microphones are available.
    /// </summary>
    public static int SelectMicrophone()
    {
        var deviceCount = WaveInEvent.DeviceCount;
        if (deviceCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log("❌ No microphone found. Please connect a microphone and try again.");
            Console.ResetColor();
            return -1;
        }

        Log("🎙️  Available microphones:");
        for (var i = 0; i < deviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            Console.WriteLine($"   [{i}] {caps.ProductName}");
        }

        Console.Write("   Select microphone [0]: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return 0;

        if (int.TryParse(input, out var selected) && selected >= 0 && selected < deviceCount)
            return selected;

        Console.WriteLine($"   Invalid selection, using device [0]");
        return 0;
    }

    /// <summary>
    /// Displays TTS engine options and lets the user pick one.
    /// Press ENTER to use the default (QwenTTS).
    /// </summary>
    public static TtsEngine SelectTtsEngine()
    {
        Console.WriteLine();
        Log("🔊 TTS engines:");
        Console.WriteLine("   [0] QwenTTS     — Qwen3-TTS, ~500MB model (default)");
        Console.WriteLine("   [1] VibeVoice   — VibeVoice-Realtime-0.5B, ~1.5GB model, higher quality");
        Console.WriteLine("   [2] None        — No TTS (text-only responses)");

        Console.Write("   Select TTS engine [0]: ");
        var input = Console.ReadLine()?.Trim();

        switch (input)
        {
            case "1":
                Console.WriteLine("   → VibeVoice TTS");
                return TtsEngine.VibeVoice;
            case "2":
                Console.WriteLine("   → No TTS");
                return TtsEngine.None;
            default:
                Console.WriteLine("   → QwenTTS");
                return TtsEngine.QwenTts;
        }
    }

    /// <summary>
    /// Displays conversation mode options and lets the user pick one.
    /// Press ENTER to use the default (streaming mode).
    /// </summary>
    public static ConversationMode SelectMode()
    {
        Console.WriteLine();
        Log("🔄 Conversation modes:");
        Console.WriteLine("   [0] Streaming  — see STT and LLM tokens in real-time (default)");
        Console.WriteLine("   [1] Batch      — wait for complete response before displaying");

        Console.Write("   Select mode [0]: ");
        var input = Console.ReadLine()?.Trim();

        if (input == "1")
        {
            Console.WriteLine("   → Batch mode (ProcessTurnAsync)");
            return ConversationMode.Batch;
        }

        Console.WriteLine("   → Streaming mode (ConverseAsync)");
        return ConversationMode.Streaming;
    }
}

/// <summary>
/// Defines which TTS engine to use for speech synthesis.
/// </summary>
public enum TtsEngine
{
    /// <summary>QwenTTS — smaller model, faster startup.</summary>
    QwenTts,

    /// <summary>VibeVoice — larger model, higher quality voice.</summary>
    VibeVoice,

    /// <summary>No TTS — text-only responses.</summary>
    None,
}

/// <summary>
/// Defines how the conversation loop processes audio and displays results.
/// </summary>
public enum ConversationMode
{
    /// <summary>
    /// Uses ConverseAsync — streams STT, LLM tokens, and TTS chunks in real-time.
    /// </summary>
    Streaming,

    /// <summary>
    /// Uses ProcessTurnAsync — waits for the full pipeline before displaying results.
    /// </summary>
    Batch,
}
