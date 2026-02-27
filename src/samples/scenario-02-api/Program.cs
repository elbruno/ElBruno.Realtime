using Microsoft.Extensions.AI;
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;
using ElBruno.Realtime.SileroVad;
using ElBruno.QwenTTS.Pipeline;
using Scenario07RealtimeApi;

// ──────────────────────────────────────────────────────────────────
// Scenario 07: Real-Time Conversation API with SignalR
//
// ASP.NET Core API that exposes a SignalR hub for streaming
// real-time audio conversations using the PersonaPlex Realtime
// pipeline: VAD → STT → LLM → TTS
//
// Prerequisites:
//   - Ollama running locally: ollama serve
//   - phi4-mini model: ollama pull phi4-mini
// ──────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── Register the realtime pipeline ──────────────────────────────
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful, friendly voice assistant. Keep responses concise (1-2 sentences).";
    opts.DefaultLanguage = "en-US";
})
.UseWhisperStt("whisper-tiny.en")   // Local STT, auto-downloads model
.UseSileroVad();                    // Local VAD, auto-downloads model

// Register QwenTTS pipeline and adapter for ITextToSpeechClient
builder.Services.AddQwenTts();
builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();

// ── Register the LLM (Ollama) ───────────────────────────────────
builder.Services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

builder.Services.AddSignalR();
// WARNING: AllowAnyOrigin is for local development only.
// In production, restrict to trusted origins: policy.WithOrigins("https://your-app.com")
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

// ── REST endpoint: one-shot turn (10MB max upload) ──────────────
app.MapPost("/api/conversation/turn", async (
    HttpRequest request,
    IRealtimeConversationClient conversation) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data with an 'audio' file.");

    var form = await request.ReadFormAsync();
    var audioFile = form.Files["audio"];

    if (audioFile is null)
        return Results.BadRequest("Missing 'audio' field in form data.");

    if (audioFile.Length > 10 * 1024 * 1024)
        return Results.BadRequest("Audio file exceeds 10MB limit.");

    using var audioStream = audioFile.OpenReadStream();
    var turn = await conversation.ProcessTurnAsync(audioStream, new ConversationOptions
    {
        EnableAudioResponse = true,
    });

    // Return JSON with text; audio response as base64
    byte[]? audioBytes = null;
    if (turn.ResponseAudio is not null)
    {
        using var ms = new MemoryStream();
        await turn.ResponseAudio.CopyToAsync(ms);
        audioBytes = ms.ToArray();
    }

    return Results.Ok(new
    {
        userText = turn.UserText,
        responseText = turn.ResponseText,
        processingTimeMs = turn.ProcessingTime.TotalMilliseconds,
        audioBase64 = audioBytes is not null ? Convert.ToBase64String(audioBytes) : null,
        audioMediaType = turn.AudioMediaType,
    });
});

// ── SignalR hub for streaming conversation ───────────────────────
app.MapHub<ConversationHub>("/hubs/conversation");

// ── Health check ────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new
{
    service = "PersonaPlex Realtime API",
    version = "0.1.0-preview",
    endpoints = new[]
    {
        "POST /api/conversation/turn - One-shot audio turn",
        "WS /hubs/conversation - SignalR streaming conversation",
    }
}));

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  PersonaPlex Realtime API - Scenario 07         ║");
Console.WriteLine("║  POST /api/conversation/turn                    ║");
Console.WriteLine("║  WS   /hubs/conversation                       ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");

app.Run();
