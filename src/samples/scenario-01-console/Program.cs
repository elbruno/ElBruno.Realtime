using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;
using ElBruno.QwenTTS.Realtime;

// ──────────────────────────────────────────────────────────────────
// Scenario 01: Real-Time Conversation Console App
//
// Demonstrates one-shot turn-based conversation using the
// ElBruno.Realtime pipeline:
//   Audio file → Whisper STT → Ollama LLM → TTS → Audio file
//
// Prerequisites:
//   - Ollama running locally with phi4-mini model:
//     ollama pull phi4-mini
//     ollama serve
//   - A 16kHz mono WAV file to use as input
// ──────────────────────────────────────────────────────────────────

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  PersonaPlex Realtime Console - Scenario 01     ║");
Console.WriteLine("║  Audio → STT → LLM → TTS pipeline              ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();

// ── 1. Check model status ───────────────────────────────────────
var whisperModelId = "whisper-tiny.en";
var whisperFileName = $"ggml-{whisperModelId.Replace("whisper-", "")}.bin";
var whisperModelDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ElBruno", "PersonaPlex", "whisper-models");
var whisperModelPath = Path.Combine(whisperModelDir, whisperFileName);

Console.WriteLine("📂 Model locations:");
if (File.Exists(whisperModelPath))
{
    var fileSize = new FileInfo(whisperModelPath).Length;
    Console.WriteLine($"   Whisper: ✅ Found at {whisperModelPath} ({fileSize / (1024 * 1024)} MB)");
}
else
{
    Console.WriteLine($"   Whisper: ⬇️ Will be downloaded on first use to {whisperModelPath} (~75 MB)");
}
Console.WriteLine($"   TTS:     Auto-downloaded by QwenTTS on first use");
Console.WriteLine();

// ── 2. Configure services ───────────────────────────────────────
var services = new ServiceCollection();

services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful assistant. Keep responses brief (1-2 sentences).";
    opts.DefaultLanguage = "en-US";
})
.UseWhisperStt(whisperModelId)   // 75MB model, auto-downloads on first use
.UseQwenTts();

// Register Ollama as the LLM (assumes Ollama is running locally)
services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

var provider = services.BuildServiceProvider();

// ── 3. Get the conversation client ──────────────────────────────
var conversation = provider.GetRequiredService<IRealtimeConversationClient>();

Console.WriteLine("✅ Pipeline initialized");
Console.WriteLine("   STT:  Whisper tiny.en");
Console.WriteLine("   LLM:  Ollama phi4-mini (localhost:11434)");
Console.WriteLine("   TTS:  QwenTTS");
Console.WriteLine();

// ── 4. Process a conversation turn ──────────────────────────────
// Check for input file
var inputFile = args.Length > 0 ? args[0] : null;

if (inputFile is null || !File.Exists(inputFile))
{
    Console.WriteLine("Usage: dotnet run -- <path-to-16khz-mono-wav>");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run -- question.wav");
    Console.WriteLine();

    // Demo mode: show how the API works without actual audio
    Console.WriteLine("── Demo Mode (no audio file provided) ──────────");
    Console.WriteLine();
    Console.WriteLine("Here's how the code works:");
    Console.WriteLine();
    Console.WriteLine("  var conversation = services.GetRequiredService<IRealtimeConversationClient>();");
    Console.WriteLine("  using var audio = File.OpenRead(\"question.wav\");");
    Console.WriteLine("  var turn = await conversation.ProcessTurnAsync(audio);");
    Console.WriteLine("  Console.WriteLine($\"User said: {turn.UserText}\");");
    Console.WriteLine("  Console.WriteLine($\"AI replied: {turn.ResponseText}\");");
    Console.WriteLine();
    return;
}

Console.WriteLine($"📁 Input: {inputFile}");
Console.WriteLine("🔄 Processing...");
Console.WriteLine();

try
{
    using var audioStream = File.OpenRead(inputFile);

    var turn = await conversation.ProcessTurnAsync(audioStream, new ConversationOptions
    {
        SystemPrompt = "You are a helpful, friendly assistant. Keep responses concise.",
        EnableAudioResponse = true,
    });

    Console.WriteLine($"📝 User said: {turn.UserText}");
    Console.WriteLine($"🤖 AI replied: {turn.ResponseText}");
    Console.WriteLine($"⏱️  Processing time: {turn.ProcessingTime.TotalSeconds:F1}s");

    if (turn.ResponseAudio is not null)
    {
        var outputFile = Path.Combine(
            Path.GetDirectoryName(inputFile) ?? ".",
            $"response_{Path.GetFileNameWithoutExtension(inputFile)}.wav");

        using var outFile = File.Create(outputFile);
        await turn.ResponseAudio.CopyToAsync(outFile);
        Console.WriteLine($"🔊 Audio response: {outputFile}");
    }
}
catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ Cannot connect to Ollama. Make sure it's running:");
    Console.WriteLine("   ollama serve");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Console.WriteLine("Done.");
