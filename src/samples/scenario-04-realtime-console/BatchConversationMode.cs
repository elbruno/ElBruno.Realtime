using ElBruno.Realtime;
using static Scenario04RealtimeConsole.ConsoleHelper;

namespace Scenario04RealtimeConsole;

/// <summary>
/// Batch conversation mode using <see cref="IRealtimeConversationClient.ProcessTurnAsync"/>.
/// 
/// This mode waits for the complete pipeline to finish before displaying anything:
///   1. Sends audio to the pipeline (STT → LLM → TTS)
///   2. Waits for the full response
///   3. Displays transcription, AI response, and plays audio
/// 
/// Simpler to understand but less responsive than streaming mode.
/// Best for understanding the basic pipeline flow.
/// </summary>
public static class BatchConversationMode
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
                using var audioStream = new MemoryStream(wavData);

                Log("🔄 Transcribing...");
                var turn = await conversation.ProcessTurnAsync(audioStream, options, cancellationToken);

                Log($"📝 You said: {turn.UserText}");
                Log($"🤖 AI replied: {turn.ResponseText}");

                if (turn.ResponseAudio is not null)
                {
                    Log("🔊 Playing audio response...");
                    await AudioHelper.PlayAudioAsync(turn.ResponseAudio, cancellationToken);
                }

                Log($"⏱️  Total: {turn.ProcessingTime.TotalSeconds:F1}s");
                Console.WriteLine();
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
}
