# ElBruno.Realtime — Project Instructions for SQUAD

## Goal

Build and evolve a **pluggable real-time audio conversation framework for .NET** that makes it trivially easy for developers to add voice conversations to their apps. Everything runs locally — no cloud dependencies.

A developer should be able to write:

```csharp
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful voice assistant.";
})
.UseWhisperStt()    // local speech-to-text
.UseQwenTts()       // local text-to-speech
.UseSileroVad();    // voice activity detection

builder.Services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));
```

...and get a complete voice conversation pipeline that handles VAD → STT → LLM → TTS transparently.

---

## Architecture

```
    Microphone (Audio Input)
        │ raw 16kHz 16-bit mono PCM
        ▼
    ┌──────────────────────────────────────────────────┐
    │  Layer 3: ORCHESTRATION                           │
    │  IRealtimeConversationClient                      │
    │  RealtimeConversationPipeline                     │
    │  Chains all components automatically              │
    │  DI: builder.Services.AddPersonaPlexRealtime()    │
    └───────────────────┬──────────────────────────────┘
                        │ uses
    ┌───────────────────┴──────────────────────────────┐
    │  Layer 2: COMPONENT ABSTRACTIONS                  │
    │                                                    │
    │  ISpeechToTextClient (M.E.AI)  │ ITextToSpeechClient (ours)  │
    │  ├─ WhisperSpeechToTextClient  │ ├─ QwenTextToSpeechClient   │
    │  └─ (pluggable)               │ └─ (pluggable)              │
    │                                │                              │
    │  IChatClient (M.E.AI)          │ IVoiceActivityDetector (ours)│
    │  ├─ OllamaChatClient           │ ├─ SileroVadDetector         │
    │  └─ OpenAIChatClient           │ └─ (pluggable)              │
    └──────────────────────────────────────────────────┘
                        │ uses
    ┌───────────────────┴──────────────────────────────┐
    │  Layer 1: MODEL ENGINES                           │
    │  Whisper.net (GGML) │ QwenTTS (ONNX) │ Silero VAD (ONNX)    │
    │  ONNX Runtime       │ Ollama          │ Microsoft.Extensions.AI │
    └──────────────────────────────────────────────────┘
```

### Data Flow

**One-shot turn** (`ProcessTurnAsync`):
```
Audio Stream → ISpeechToTextClient.GetTextAsync() → text
    → IChatClient.GetResponseAsync() → response text
        → ITextToSpeechClient.GetSpeechAsync() → ConversationTurn
```

**Streaming conversation** (`ConverseAsync`):
```
Audio Chunks → IVoiceActivityDetector.DetectSpeechAsync()
    → SpeechSegment → ISpeechToTextClient → IChatClient → ITextToSpeechClient
    → ConversationEvent stream (transcription, text chunks, audio chunks)
```

---

## Models Used

| Model | Package | Size | Role | Format | Auto-Download |
|-------|---------|------|------|--------|---------------|
| **Silero VAD v5** | `ElBruno.Realtime.SileroVad` | ~2 MB | Detects speech vs. silence | ONNX | ✅ from HuggingFace |
| **Whisper tiny.en** | `ElBruno.Realtime.Whisper` | ~75 MB | Speech-to-text | GGML | ✅ via Whisper.net |
| **Whisper base.en** | `ElBruno.Realtime.Whisper` | ~142 MB | Speech-to-text (accurate) | GGML | ✅ via Whisper.net |
| **QwenTTS (Qwen3-TTS)** | `ElBruno.Realtime.QwenTTS` | ~5.5 GB | Text-to-speech | ONNX | ✅ via ElBruno.QwenTTS |
| **Phi4-Mini** | User provides | ~2.7 GB | LLM chat | Ollama | ❌ Manual: `ollama pull phi4-mini` |

All auto-downloaded models cached in `%LOCALAPPDATA%/ElBruno/PersonaPlex/`.

---

## Repository Structure

```
ElBruno.Realtime/
├── ElBruno.Realtime.slnx              # Solution file
├── Directory.Build.props              # net8.0;net10.0, nullable, latest
├── README.md                          # Full README with badges, quick start
├── LICENSE                            # MIT
├── .github/workflows/publish.yml      # NuGet publish (OIDC)
│
├── src/
│   ├── ElBruno.Realtime/              # Core abstractions + pipeline
│   │   ├── Abstractions/              # Interfaces: ITextToSpeechClient, IVoiceActivityDetector,
│   │   │                              #   IRealtimeConversationClient, ConversationEvent, etc.
│   │   ├── DependencyInjection/       # AddPersonaPlexRealtime() + RealtimeBuilder
│   │   ├── Options/                   # RealtimeOptions (STT, TTS, VAD, conversation config)
│   │   └── Pipeline/                  # RealtimeConversationPipeline (default orchestration)
│   │
│   ├── ElBruno.Realtime.Whisper/      # Whisper STT provider
│   │   ├── WhisperSpeechToTextClient  # ISpeechToTextClient implementation
│   │   ├── WhisperModelManager        # GGML model download/cache
│   │   └── WhisperRealtimeBuilderExtensions  # .UseWhisperStt()
│   │
│   ├── ElBruno.Realtime.QwenTTS/      # QwenTTS TTS provider
│   │   ├── QwenTextToSpeechClient     # ITextToSpeechClient implementation
│   │   └── QwenTtsRealtimeBuilderExtensions  # .UseQwenTts()
│   │
│   ├── ElBruno.Realtime.SileroVad/    # Silero VAD provider
│   │   ├── SileroVadDetector          # IVoiceActivityDetector implementation
│   │   ├── SileroModelManager         # ONNX model download/cache
│   │   └── SileroVadRealtimeBuilderExtensions  # .UseSileroVad()
│   │
│   ├── ElBruno.Realtime.Tests/        # 33 unit tests (xUnit)
│   │
│   └── samples/
│       ├── scenario-01-console/       # Minimal console demo
│       ├── scenario-02-api/           # ASP.NET Core API + SignalR
│       └── scenario-03-blazor-aspire/ # Full Blazor + .NET Aspire app
│
└── docs/
    ├── models-overview.md             # Detailed model documentation
    ├── realtime-architecture.md       # Architecture + M.E.AI integration
    └── publishing.md                  # NuGet publishing guide
```

---

## NuGet Packages (4)

| Package | NuGet ID | Version | Dependencies |
|---------|----------|---------|--------------|
| Core | `ElBruno.Realtime` | 0.1.0-preview | M.E.AI.Abstractions 10.0.0, M.E.DI.Abstractions 9.0.* |
| Whisper STT | `ElBruno.Realtime.Whisper` | 0.1.0-preview | Core + Whisper.net 1.9.0 |
| QwenTTS | `ElBruno.Realtime.QwenTTS` | 0.1.0-preview | Core + ElBruno.QwenTTS 0.1.7-preview |
| Silero VAD | `ElBruno.Realtime.SileroVad` | 0.1.0-preview | Core + OnnxRuntime 1.24.2 + HF Downloader 0.5.0 |

Publishing: GitHub Actions → OIDC → NuGet.org (workflow in `.github/workflows/publish.yml`)

---

## Microsoft.Extensions.AI Integration

### Interfaces we IMPLEMENT (from M.E.AI):
- `ISpeechToTextClient` — Our `WhisperSpeechToTextClient` implements this experimental interface
- `IChatClient` — We consume any registered `IChatClient` (Ollama, OpenAI, Azure, etc.)

### Interfaces we DEFINE (following M.E.AI patterns):
- `ITextToSpeechClient` — No official TTS interface exists in M.E.AI yet. Ours follows the same patterns
- `IVoiceActivityDetector` — Audio stream → speech segments
- `IRealtimeConversationClient` — High-level pipeline orchestration

> **Upstream proposal**: We plan to propose `ITextToSpeechClient` to [dotnet/extensions](https://github.com/dotnet/extensions) with a link to our implementation as a reference.

---

## Key Technical Decisions

1. **Namespace**: `ElBruno.Realtime` (not `ElBruno.PersonaPlex.Realtime` — this is model-agnostic)
2. **Multi-target**: net8.0 + net10.0
3. **`[Experimental(MEAI001)]`**: Suppressed via `<NoWarn>` — we depend on M.E.AI experimental `ISpeechToTextClient`
4. **Thread safety**: `SemaphoreSlim` guards lazy model initialization; `_inferenceLock` on SileroVadDetector
5. **Path traversal protection**: All model cache dirs validated with `Path.GetFullPath()` + prefix check
6. **DI lifecycle**: All providers registered as singletons via `AddSingleton<TService>(factory)`
7. **Audio format**: 16kHz, 16-bit mono PCM throughout the pipeline
8. **QwenTTS workaround**: `TtsPipeline.SynthesizeAsync()` is file-based — we use temp files + cleanup

---

## Current State (2026-02-27)

### ✅ Complete
- Core abstractions + pipeline orchestration
- Whisper STT provider (9 model sizes, auto-download)
- QwenTTS TTS provider (multiple voices, wraps ElBruno.QwenTTS)
- Silero VAD provider (ONNX Runtime, RNN state tracking)
- DI: `AddPersonaPlexRealtime()` with fluent builder
- 66 tests passing (33 × 2 TFMs)
- 3 sample scenarios (console, API, Blazor+Aspire)
- Security hardened (path traversal, input size limits, concurrency)
- NuGet packaging + GitHub Actions publish workflow
- Documentation (README, architecture, models overview)

### 🔮 Future Work
- **Server-side TTS streaming**: Stream audio chunks back to browser via SignalR as they're synthesized
- **Full-duplex barge-in**: Detect user speech during AI response, cancel TTS, restart pipeline (state machine: IDLE→LISTENING→PROCESSING→SPEAKING→INTERRUPTED)
- **Browser integration**: WebRTC or MediaStream API for browser microphone → server pipeline
- **Additional STT providers**: Azure Speech, Google Speech, faster-whisper
- **Additional TTS providers**: Piper TTS, Azure Speech, browser SpeechSynthesis
- **Propose `ITextToSpeechClient`**: File GitHub Issue on dotnet/extensions with our implementation as reference
- **Performance**: Pipeline latency profiling, model warm-up, concurrent session support
- **CI/CD**: Add build+test workflow, code coverage, automated NuGet preview releases

---

## Related Projects

- [ElBruno.PersonaPlex](https://github.com/elbruno/ElBruno.PersonaPlex) — NVIDIA PersonaPlex-7B-v1 ONNX inference (the original model this was born from)
- [ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS) — QwenTTS text-to-speech (used by our TTS provider)
- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader) — Model downloader (used by Silero VAD provider)
- [ElBruno.VibeVoiceTTS](https://github.com/elbruno/ElBruno.VibeVoiceTTS) — Alternative TTS library
- [ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image) — Text-to-image generation

---

## Team Roles Needed

| Role | Responsibility |
|------|---------------|
| **Architect** | Pipeline design, M.E.AI alignment, interface evolution |
| **C# Developer** | Provider implementations, DI extensions, streaming patterns |
| **ML/ONNX Specialist** | Model loading, tensor formats, inference optimization, new model providers |
| **Frontend Developer** | Blazor UI, browser audio integration, WebRTC/MediaStream |
| **Security Reviewer** | Audio data privacy, model download verification, input validation |
| **DevOps** | CI/CD, NuGet publishing, version management |
| **Documentation** | API docs, samples, migration guides |
