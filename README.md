# ElBruno.Realtime

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElBruno.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime)
[![Build Status](https://github.com/elbruno/ElBruno.Realtime/actions/workflows/publish.yml/badge.svg)](https://github.com/elbruno/ElBruno.Realtime/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/elbruno/ElBruno.Realtime?style=social)](https://github.com/elbruno/ElBruno.Realtime)
[![Twitter Follow](https://img.shields.io/twitter/follow/elbruno?style=social)](https://twitter.com/elbruno)

A pluggable **real-time audio conversation framework** for .NET, following [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) patterns. Build voice-powered apps with local STT, TTS, VAD, and any LLM — all running on your machine, no cloud required.

## Powered By

This project is built on two core Microsoft frameworks for AI and conversation management:

- **[Microsoft.Extensions.AI (MEAI)](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)** — Provides unified abstractions for chat clients (`IChatClient`) and speech-to-text (`ISpeechToTextClient`), enabling pluggable LLM and STT providers throughout the pipeline.
- **[Microsoft Agent Framework](https://learn.microsoft.com/en-us/ai/agents/)** — Manages conversation sessions, per-user state, and dialogue continuity, ensuring each user gets a consistent, stateful conversation experience.

Together with industry-standard models (Whisper STT, Silero VAD, ONNX Runtime), these frameworks provide a production-ready foundation for real-time voice applications.

## Architecture

```
    Microphone (Audio Input)
        │ raw PCM audio
        ▼
    🔇 Silero VAD ─── Voice Activity Detection (~2 MB ONNX)
        │ speech segments
        ▼
    🎙️ Whisper STT ─── Speech-to-Text (~75 MB GGML)
        │ transcribed text
        ▼
    🤖 Any IChatClient ─── LLM Chat (Ollama / OpenAI / Azure)
        │ response text
        ▼
    🗣️ Any TTS ─── Text-to-Speech (pluggable)
        │ WAV audio
        ▼
    Speaker (Audio Output)
```

All models download automatically on first use. The LLM is pluggable via `IChatClient` — use Ollama, OpenAI, Azure, or any provider.

## Features

- **Local-First** — All audio processing runs locally. No data leaves your machine.
- **Microsoft.Extensions.AI** — Implements `ISpeechToTextClient` and follows M.E.AI patterns throughout
- **Pluggable Providers** — Swap STT, TTS, VAD, or LLM independently
- **Auto Model Download** — Models download from HuggingFace/Whisper.net on first use
- **DI-Ready** — One-line setup with `AddPersonaPlexRealtime()` + fluent builder
- **Streaming** — Full async streaming via `IAsyncEnumerable` for real-time processing
- **Multi-Target** — Supports .NET 8.0 and .NET 10.0

---

## Quick Start

### Install

```bash
dotnet add package ElBruno.Realtime          # Core abstractions + pipeline
dotnet add package ElBruno.Realtime.Whisper   # Local STT (Whisper.net)
dotnet add package ElBruno.Realtime.SileroVad # Local VAD (Silero)
```

### Basic Usage

```csharp
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;
using ElBruno.Realtime.SileroVad;
using Microsoft.Extensions.AI;

// 1. Configure the pipeline (models auto-download on first use)
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful voice assistant.";
})
.UseWhisperStt("whisper-tiny.en")   // 75MB, or "whisper-base.en" for accuracy
.UseSileroVad();                    // Voice activity detection
// .UseYourTts()  — plug in any ITextToSpeechClient

// 2. Register your LLM (any IChatClient provider)
builder.Services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

// 3. Use the pipeline
var conversation = app.Services.GetRequiredService<IRealtimeConversationClient>();

// One-shot turn
using var audio = File.OpenRead("question.wav");
var turn = await conversation.ProcessTurnAsync(audio);
Console.WriteLine($"User: {turn.UserText}");
Console.WriteLine($"AI: {turn.ResponseText}");

// Streaming full-duplex
await foreach (var evt in conversation.ConverseAsync(microphoneStream))
{
    if (evt.Kind == ConversationEventKind.ResponseTextChunk)
        Console.Write(evt.ResponseText);
}
```

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| **Core** | | |
| [`ElBruno.Realtime`](https://www.nuget.org/packages/ElBruno.Realtime) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime) | Core: `ITextToSpeechClient`, `IVoiceActivityDetector`, `IRealtimeConversationClient`, pipeline orchestration, DI |
| **Speech Processing** | | |
| [`ElBruno.Realtime.Whisper`](https://www.nuget.org/packages/ElBruno.Realtime.Whisper) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.Realtime.Whisper.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime.Whisper) | `ISpeechToTextClient` (M.E.AI) via Whisper.net — auto-downloads GGML models |
| [`ElBruno.Realtime.SileroVad`](https://www.nuget.org/packages/ElBruno.Realtime.SileroVad) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.Realtime.SileroVad.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime.SileroVad) | `IVoiceActivityDetector` via Silero VAD v5 ONNX — configurable thresholds |
| **Text-to-Speech Bridges** | | |
| [`ElBruno.QwenTTS.Realtime`](https://www.nuget.org/packages/ElBruno.QwenTTS.Realtime) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.QwenTTS.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.QwenTTS.Realtime) | `ITextToSpeechClient` adapter for QwenTTS with GPU device configuration |
| [`ElBruno.VibeVoiceTTS.Realtime`](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS.Realtime) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.VibeVoiceTTS.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS.Realtime) | `ITextToSpeechClient` adapter for VibeVoiceTTS |
| [`ElBruno.KokoroTTS.Realtime`](https://www.nuget.org/packages/ElBruno.KokoroTTS.Realtime) | [![NuGet](https://img.shields.io/nuget/v/ElBruno.KokoroTTS.Realtime.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.KokoroTTS.Realtime) | `ITextToSpeechClient` adapter for Kokoro TTS |

## Samples

| Sample | Description |
|--------|-------------|
| [scenario-01-console](src/samples/scenario-01-console/) | Realtime console app |
| [scenario-02-api](src/samples/scenario-02-api/) | ASP.NET Core API with SignalR |
| [scenario-03-blazor-aspire](src/samples/scenario-03-blazor-aspire/) | Blazor + .NET Aspire with voice chat + voice-controlled side-scroller game |
| [scenario-04-realtime-console](src/samples/scenario-04-realtime-console/) | Real-time microphone conversation with Whisper STT + Ollama LLM |

### Run a Sample

```bash
# Prerequisites: Ollama running with phi4-mini
ollama pull phi4-mini
ollama serve

# Run the console sample
cd src/samples/scenario-01-console
dotnet run
```

## Auto-Downloaded Models

All models are cached in `%LOCALAPPDATA%/ElBruno/Realtime/` and shared across apps:

| Model | Size | Purpose | Auto-Download |
|-------|------|---------|---------------|
| Silero VAD v5 | ~2 MB | Voice activity detection | ✅ Yes |
| Whisper tiny.en | ~75 MB | Speech-to-text (fast) | ✅ Yes |
| Whisper base.en | ~142 MB | Speech-to-text (accurate) | ✅ Yes |
| Phi4-Mini (Ollama) | ~2.7 GB | LLM chat | ❌ Manual: `ollama pull phi4-mini` |

## Documentation

| Document | Description |
|----------|-------------|
| [Models Overview](docs/models-overview.md) | How each model is used in the pipeline |
| [Architecture](docs/realtime-architecture.md) | Three-layer architecture, data flow, M.E.AI integration |
| [Publishing](docs/publishing.md) | NuGet publishing guide |

---

## Building from Source

```bash
git clone https://github.com/elbruno/ElBruno.Realtime.git
cd ElBruno.Realtime
dotnet build
dotnet test
```

## Requirements

- .NET 8.0 or .NET 10.0 SDK
- ONNX Runtime compatible platform (Windows, Linux, macOS)
- Ollama (or any `IChatClient` provider) for the LLM
- Sufficient disk space for model files

---

## Contributing

Contributions are welcome! Here's how to get started:

1. **Fork** the repository
2. **Create a branch** for your feature or fix: `git checkout -b feature/my-feature`
3. **Make your changes** and ensure the solution builds: `dotnet build`
4. **Run tests**: `dotnet test`
5. **Submit a pull request** with a clear description of the changes

Please open an issue first for major changes or new features to discuss the approach.

---

## 👋 About the Author

Hi! I'm **ElBruno** 🧡, a passionate developer and content creator exploring AI, .NET, and modern development practices.

**Made with ❤️ by [ElBruno](https://github.com/elbruno)**

If you like this project, consider following my work across platforms:

- 📻 **Podcast**: [No Tienen Nombre](https://notienenombre.com) — Spanish-language episodes on AI, development, and tech culture
- 💻 **Blog**: [ElBruno.com](https://elbruno.com) — Deep dives on embeddings, RAG, .NET, and local AI
- 📺 **YouTube**: [youtube.com/elbruno](https://www.youtube.com/elbruno) — Demos, tutorials, and live coding
- 🔗 **LinkedIn**: [@elbruno](https://www.linkedin.com/in/elbruno/) — Professional updates and insights
- 𝕏 **Twitter**: [@elbruno](https://www.x.com/elbruno/) — Quick tips, releases, and tech news

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Related Projects

- [ElBruno.PersonaPlex](https://github.com/elbruno/ElBruno.PersonaPlex) — NVIDIA PersonaPlex-7B-v1 ONNX inference
- [ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS) — QwenTTS text-to-speech
- [ElBruno.VibeVoiceTTS](https://github.com/elbruno/ElBruno.VibeVoiceTTS) — VibeVoiceTTS
- [ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image) — Text-to-image generation
- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader) — HuggingFace model downloader
