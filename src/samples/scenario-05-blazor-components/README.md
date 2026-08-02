# scenario-05 — BlazorComponents Demo

This sample demonstrates all five components from the `ElBruno.Realtime.BlazorComponents` NuGet package inside a Blazor Server application.

## What This Sample Shows

| Component | Description |
|-----------|-------------|
| `SessionControls` | Start/Stop/Mute button cluster in Small, Medium, Large sizes |
| `RealtimeSessionPanel` | Full session card with VAD indicator and speaker dot |
| `AudioLevelMeter` | VU bar meter bound to microphone or TTS output level |
| `LatencyMonitor` | Rolling STT/LLM/TTS latency badges (green/yellow/red) |
| `ConversationTimeline` | Scrolling turn-by-turn transcript with playback indicator |

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Optional: Ollama with `phi4-mini` for a live LLM backend

## Run the Sample

```bash
cd src/samples/scenario-05-blazor-components/scenario-05.Web
dotnet run
```

Then open `https://localhost:5001` in your browser.

## Wiring a Real Pipeline

The `Program.cs` has the pipeline calls commented out. To wire real speech processing:

```csharp
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful assistant.";
})
.UseWhisperStt("whisper-tiny.en")   // auto-downloads ~75MB on first run
.UseSileroVad();                    // auto-downloads ~2MB on first run

// LLM via Ollama (or any IChatClient provider)
builder.Services.AddChatClient(
    new OllamaChatClient(new Uri("http://localhost:11434"), "phi4-mini"));

builder.Services.AddRealtimeBlazorComponents();
```

## Package Reference

```bash
dotnet add package ElBruno.Realtime.BlazorComponents
```

See [docs/blazor-components.md](../../../../docs/blazor-components.md) for the full component API reference.
