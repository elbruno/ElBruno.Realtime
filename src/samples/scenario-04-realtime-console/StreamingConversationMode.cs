using ElBruno.Realtime;
using static Scenario04RealtimeConsole.ConsoleHelper;

namespace Scenario04RealtimeConsole;

/// <summary>
/// Streaming conversation mode using <see cref="IRealtimeConversationClient.ConverseAsync"/>.
/// 
/// This mode shows results as they arrive:
///   1. Transcribed text appears immediately after STT completes
///   2. LLM response tokens stream to the console character-by-character
///   3. TTS audio is collected and played after the response finishes
/// 
/// Best for interactive use — the user sees activity as it happens.
/// </summary>
public static class StreamingConversationMode
{
    public static async Task RunAsync(
        IRealtimeConversationClient conversation,
        int deviceNumber,
        CancellationToken cancellationToken)
    {
        var options = new ConversationOptions
        {
            SystemPrompt = "You are a helpful, friendly assistant. Keep responses concise.",
            EnableAudioResponse = true,
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            Log("🎤 Listening... (speak, then pause for 1.5s to process)");

            byte[] audioData;
            try
            {
                audioData = await AudioHelper.RecordUntilSilenceAsync(deviceNumber, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (audioData.Length < AudioHelper.MinimumAudioBytes)
            {
                Log("(no speech detected, listening again...)");
                Console.WriteLine();
                continue;
            }

            try
            {
                var wavData = AudioHelper.CreateWavData(audioData);
                await ProcessStreamingTurnAsync(conversation, wavData, options, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log("❌ Cannot connect to Ollama. Make sure it's running:");
                Console.WriteLine("   ollama serve");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Processes a single conversation turn using the streaming ConverseAsync API.
    /// Feeds the recorded audio as a single chunk and handles each event type.
    /// </summary>
    private static async Task ProcessStreamingTurnAsync(
        IRealtimeConversationClient conversation,
        byte[] wavData,
        ConversationOptions options,
        CancellationToken cancellationToken)
    {
        Log("🔄 Processing...");
        var startTime = DateTime.UtcNow;
        var audioChunks = new List<byte[]>();

        // Feed audio as a single-chunk async enumerable
        async IAsyncEnumerable<byte[]> AudioSource()
        {
            yield return wavData;
            await Task.CompletedTask;
        }

        // Process each streaming event as it arrives
        await foreach (var evt in conversation.ConverseAsync(AudioSource(), options, cancellationToken))
        {
            switch (evt.Kind)
            {
                case ConversationEventKind.TranscriptionComplete:
                    if (!string.IsNullOrWhiteSpace(evt.TranscribedText))
                        Log($"📝 You: {evt.TranscribedText}");
                    else
                        Log("(no speech recognized)");
                    break;

                case ConversationEventKind.ResponseStarted:
                    var timestamp = DateTime.Now.ToString("[HH:mm:ss]");
                    Console.Write($"{timestamp} 🤖 AI: ");
                    break;

                case ConversationEventKind.ResponseTextChunk:
                    if (evt.ResponseText is not null)
                        Console.Write(evt.ResponseText);
                    break;

                case ConversationEventKind.ResponseAudioChunk:
                    if (evt.ResponseAudio is not null)
                        audioChunks.Add(evt.ResponseAudio);
                    break;

                case ConversationEventKind.ResponseComplete:
                    Console.WriteLine();
                    break;

                case ConversationEventKind.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log($"❌ {evt.ErrorMessage}");
                    Console.ResetColor();
                    break;
            }
        }

        // Play collected audio chunks after the full response
        if (audioChunks.Count > 0)
        {
            Log("🔊 Playing response...");
            var combinedAudio = AudioHelper.CombineAudioChunks(audioChunks);
            await AudioHelper.PlayAudioAsync(combinedAudio, cancellationToken);
        }

        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        Log($"⏱️  Total: {elapsed:F1}s");
        Console.WriteLine();
    }
}
