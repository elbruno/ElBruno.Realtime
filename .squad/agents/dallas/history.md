# Dallas — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, ONNX Runtime, Whisper.net, Blazor, ASP.NET Core, SignalR
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2025-07-17: Initial Build Analysis

**Build Status:** ✅ Solution builds cleanly — zero errors, zero warnings across all 7 projects (5 libraries + 2 samples) on both net8.0 and net10.0 targets.

**Test Status:** ✅ All 66 unit tests pass on both net8.0 and net10.0.

**SDK:** .NET 10.0.103 is installed and working.

**Key Dependencies:**
- `Microsoft.Extensions.AI.Abstractions` 10.0.0 — core AI interfaces (ISpeechToTextClient, IChatClient)
- `Microsoft.Extensions.AI` 9.5.0 / `Microsoft.Extensions.AI.Ollama` 9.7.0-preview — used by samples for Ollama LLM
- `Whisper.net` + `Whisper.net.AllRuntimes` 1.9.0 — local STT via GGML models
- `ElBruno.QwenTTS` 0.1.7-preview — local TTS (ONNX-based Qwen3-TTS)
- `ElBruno.HuggingFace.Downloader` 0.5.0 — downloads Silero VAD model from HuggingFace
- `Microsoft.ML.OnnxRuntime` 1.24.2 — ONNX inference for Silero VAD

**Architecture Patterns Discovered:**
- Multi-target: net8.0 + net10.0 via Directory.Build.props
- Lazy model init: SemaphoreSlim double-check pattern in all providers (Whisper, QwenTTS, SileroVAD)
- Thread safety: `_inferenceLock` SemaphoreSlim declared in SileroVadDetector (though not used in RunInference — potential issue)
- DI builder pattern: `AddPersonaPlexRealtime()` returns `RealtimeBuilder` for fluent `.UseWhisperStt().UseQwenTts().UseSileroVad()` chaining
- All models auto-download on first use (Whisper GGML, Silero ONNX from HuggingFace, QwenTTS ONNX)
- Pipeline: VAD → STT → LLM (IChatClient) → TTS with IAsyncEnumerable streaming

**Runtime Prerequisites:**
- Ollama must be running locally (`ollama serve`) with `phi4-mini` model pulled
- Models download automatically on first use but require internet access
- Audio format: 16kHz, 16-bit mono PCM for STT/VAD; QwenTTS outputs 24kHz WAV

**Observations:**
- `SileroVadDetector._inferenceLock` is declared but never used in `RunInference()` — inference is not thread-safe
- scenario-03-blazor-aspire has its own .slnx and is NOT part of the main solution
- Samples target net10.0 only (not multi-target)
- Core libraries produce XML docs (GenerateDocumentationFile=true)
