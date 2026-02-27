using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;
using ElBruno.QwenTTS.Realtime;
using Scenario04RealtimeConsole;
using static Scenario04RealtimeConsole.ConsoleHelper;

// ──────────────────────────────────────────────────────────────────
// Scenario 04: Real-Time Microphone Conversation
//
// Captures audio from the default microphone, transcribes with
// Whisper, sends to Ollama LLM, and speaks the response.
//
// Pipeline:  Microphone → Whisper STT → Ollama LLM → QwenTTS → Speakers
//
// Prerequisites:
//   - Ollama running locally with phi4-mini:
//     ollama pull phi4-mini && ollama serve
//   - A working microphone
//
// Code structure:
//   Program.cs                     ← Entry point (this file)
//   ConsoleHelper.cs               ← Log(), mic/mode selection, enums
//   AudioHelper.cs                 ← Recording, WAV encoding, playback
//   StreamingConversationMode.cs   ← ConverseAsync conversation loop
//   BatchConversationMode.cs       ← ProcessTurnAsync conversation loop
// ──────────────────────────────────────────────────────────────────

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  ElBruno.Realtime Console - Scenario 04         ║");
Console.WriteLine("║  Real-time Microphone Conversation              ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();

// ── 1. Select microphone ────────────────────────────────────────
var deviceNumber = SelectMicrophone();
if (deviceNumber < 0) return;
Console.WriteLine();

// ── 2. Select conversation mode ─────────────────────────────────
var mode = SelectMode();
Console.WriteLine();

// ── 3. Show model status ────────────────────────────────────────
var whisperModelId = "whisper-tiny.en";
var whisperFileName = $"ggml-{whisperModelId.Replace("whisper-", "")}.bin";
var whisperModelDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ElBruno", "Realtime", "whisper-models");
var whisperModelPath = Path.Combine(whisperModelDir, whisperFileName);

Log("📂 Model locations:");
if (File.Exists(whisperModelPath))
{
    var fileSize = new FileInfo(whisperModelPath).Length;
    Console.WriteLine($"   Whisper: ✅ Found at {whisperModelPath} ({fileSize / (1024 * 1024)} MB)");
}
else
{
    Console.WriteLine($"   Whisper: ⬇️ Will be downloaded on first use to {whisperModelPath} (~75 MB)");
}
Console.WriteLine("   LLM:     Ollama phi4-mini (ensure 'ollama serve' is running)");
Console.WriteLine("   TTS:     Auto-downloaded by QwenTTS on first use");

var runtimeInfo = WhisperSpeechToTextClient.GetRuntimeInfo();
if (!string.IsNullOrEmpty(runtimeInfo))
    Console.WriteLine($"   Runtime: {runtimeInfo}");
Console.WriteLine();

// ── 4. Configure services ───────────────────────────────────────
var services = new ServiceCollection();

services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful assistant. Keep responses brief (1-2 sentences).";
    opts.DefaultLanguage = "en-US";
})
.UseWhisperStt("whisper-tiny.en")
.UseQwenTts();

services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

var provider = services.BuildServiceProvider();
var conversation = provider.GetRequiredService<IRealtimeConversationClient>();

Log("✅ Pipeline initialized");
Console.WriteLine("   STT:  Whisper tiny.en (GPU enabled, auto-download on first use)");
Console.WriteLine("   LLM:  Ollama phi4-mini (localhost:11434)");
Console.WriteLine("   TTS:  QwenTTS");
Console.WriteLine();
Log("Press Ctrl+C to exit.");
Console.WriteLine();

// ── 5. Run conversation loop ────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine();
    Log("👋 Exiting...");
};

try
{
    if (mode == ConversationMode.Streaming)
        await StreamingConversationMode.RunAsync(conversation, deviceNumber, cts.Token);
    else
        await BatchConversationMode.RunAsync(conversation, deviceNumber, cts.Token);
}
catch (OperationCanceledException)
{
    // Normal exit via Ctrl+C
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Log($"❌ Error: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Log("Done.");
