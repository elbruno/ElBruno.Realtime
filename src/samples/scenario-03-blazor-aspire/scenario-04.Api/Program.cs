using Microsoft.Extensions.AI;
using Scenario04.Api.Hubs;
using Scenario04.Api.Services;
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────
// Aspire service defaults (OpenTelemetry, health checks, etc.)
// ──────────────────────────────────────────────────────────────
builder.AddServiceDefaults();

// ──────────────────────────────────────────────────────────────
// Ollama via Microsoft Agent Framework
// ──────────────────────────────────────────────────────────────
// Uses Microsoft.Extensions.AI.Ollama (OllamaChatClient) as the
// underlying IChatClient, following the pattern from:
// https://learn.microsoft.com/agent-framework/agents/providers/ollama
//
// Aspire injects the Ollama endpoint via connection string.
// The OllamaChatClient is registered as IChatClient for DI.
// ──────────────────────────────────────────────────────────────
var ollamaEndpoint = builder.Configuration.GetConnectionString("ollama")
    ?? builder.Configuration["Ollama:Endpoint"]
    ?? "http://localhost:11434";

var ollamaModel = builder.Configuration["Ollama:Model"] ?? "phi4-mini";

// Register OllamaChatClient as IChatClient (Agent Framework pattern)
builder.Services.AddChatClient(new OllamaChatClient(
        new Uri(ollamaEndpoint), ollamaModel))
    .UseFunctionInvocation()
    .UseOpenTelemetry()
    .UseLogging();

// ──────────────────────────────────────────────────────────────
// PersonaPlex Realtime: Whisper STT for server-side audio processing
// Auto-downloads whisper-tiny.en model on first use (~75MB)
// ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISpeechToTextClient>(
    _ => new WhisperSpeechToTextClient("whisper-tiny.en"));

// ──────────────────────────────────────────────────────────────
// Application services
// ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ConversationService>();
builder.Services.AddSingleton<GameFeedbackService>();

// ──────────────────────────────────────────────────────────────
// SignalR with MessagePack for binary audio streaming
// ──────────────────────────────────────────────────────────────
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// CORS — allow the Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();
app.MapHub<ConversationHub>("/hubs/conversation");
app.MapHub<GameHub>("/hubs/game");

// Simple health/info endpoint
app.MapGet("/", () => new
{
    Service = "PersonaPlex Conversation API",
    OllamaEndpoint = ollamaEndpoint,
    OllamaModel = ollamaModel,
    AgentFramework = "Microsoft.Agents.AI + Microsoft.Extensions.AI.Ollama",
    Status = "Running"
});

app.Run();
