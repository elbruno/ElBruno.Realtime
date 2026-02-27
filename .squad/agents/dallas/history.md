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
- ~~`SileroVadDetector._inferenceLock` is declared but never used in `RunInference()` — inference is not thread-safe~~ **FIXED** (see below)
- scenario-03-blazor-aspire has its own .slnx and is NOT part of the main solution
- Samples target net10.0 only (not multi-target)
- Core libraries produce XML docs (GenerateDocumentationFile=true)

### 2025-07-17: Fixed _inferenceLock in SileroVadDetector

**Bug:** `_inferenceLock` (SemaphoreSlim) was declared and disposed but never acquired. Concurrent calls to `DetectSpeechAsync()` could race on the shared ONNX `_session` via `RunInference()`.

**Fix:** Wrapped the `RunInference()` call in `DetectSpeechAsync` with `await _inferenceLock.WaitAsync(cancellationToken)` / `try-finally { _inferenceLock.Release() }`. Minimal 10-line change. Build: 0 errors, 0 warnings. Tests: 66/66 pass.

### 2026-02-27: Implemented Per-Session Conversation History

**Task:** Replaced singleton `_conversationHistory` field in `RealtimeConversationPipeline` with per-session history via `IConversationSessionStore` abstraction. Design by Ripley.

**Changes:**
- **NEW** `Abstractions/IConversationSessionStore.cs` — interface with `GetOrCreateSessionAsync` and `RemoveSessionAsync`
- **NEW** `Pipeline/InMemoryConversationSessionStore.cs` — default `ConcurrentDictionary`-based implementation
- **EDIT** `Abstractions/ConversationOptions.cs` — added `SessionId` property (nullable, defaults to `"__default__"`)
- **EDIT** `Pipeline/RealtimeConversationPipeline.cs` — constructor takes `IConversationSessionStore`; removed `_conversationHistory` field; added `GetSessionHistoryAsync` helper; `TrimHistory` now static accepting `IList<ChatMessage>`; `ProcessSpeechSegmentAsync` takes `conversationHistory` parameter
- **EDIT** `DependencyInjection/RealtimeServiceCollectionExtensions.cs` — `TryAddSingleton<IConversationSessionStore, InMemoryConversationSessionStore>()` and inject into pipeline factory

**Backward compatibility:** Fully maintained. `TryAddSingleton` means consumers who don't register their own store get the in-memory default. Null `SessionId` falls back to `"__default__"` key. Build: 0 errors, 0 warnings. Tests: 66/66 pass (net8.0 + net10.0).

### 2026-02-27: Game Backend Voice Feedback

**Work:** Added `GameHub`, `GameFeedbackService`, and shared game DTOs in scenario-04 for voice feedback. Quick phrase pools now serve instant responses while milestone scores trigger `IChatClient`-generated encouragement. Hub mapped at `/hubs/game` alongside the existing conversation hub.

**Features:**
- Thread-safe random phrase selection via `RandomNumberGenerator.GetInt32`
- Two-tier feedback: instant (damage/success) + LLM milestone (500-point increments)
- `GameHub` methods: `SendScore`, `GetQuickFeedback`, `GetDynamicFeedback`, broadcast events
- `GameFeedbackService`: Non-blocking, cost-optimized (LLM calls only on milestones)
- Shared `GameStateDto`, `GameEventDto`, `GameInputDto` for client-server type safety

**Outcome:** ✅ Build clean (0 errors, 0 warnings, net8.0 + net10.0). Full integration with Lambert's Canvas game engine.

### 2026-02-27: Migrated QwenTTS from Local Project to NuGet Package

**Task:** Remove `ElBruno.Realtime.QwenTTS` local project and upgrade to `ElBruno.QwenTTS` v0.1.8-preview NuGet package.

**Problem:** The local `ElBruno.Realtime.QwenTTS` project wrapped `ElBruno.QwenTTS` to provide `QwenTextToSpeechClient` (adapter to `ITextToSpeechClient`) and `UseQwenTts()` builder extension. The upstream NuGet v0.1.8-preview now includes `AddQwenTts()` which registers `ITtsPipeline` in DI, making the local wrapper redundant for pipeline creation — but the Realtime pipeline still needs `ITextToSpeechClient`, not `ITtsPipeline`.

**Solution:**
- Deleted `src/ElBruno.Realtime.QwenTTS/` entirely (project, adapter, builder extension)
- Removed from `ElBruno.Realtime.slnx` and `.github/workflows/publish.yml` pack step
- Each sample now references `ElBruno.QwenTTS` v0.1.8-preview directly as a PackageReference
- Created `QwenTextToSpeechClientAdapter` in each sample — takes `ITtsPipeline` from DI (registered by `AddQwenTts()`), adapts to `ITextToSpeechClient`
- Adapter is simpler than original: no lazy init lock (DI handles pipeline lifetime), no disposal of pipeline (DI-owned)
- Updated `Program.cs` in both samples: `services.AddQwenTts()` + `services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>()`

**Key Pattern:** `ITtsPipeline` → `ITextToSpeechClient` adapter pattern. If a future `ElBruno.Realtime.QwenTTS` NuGet is needed, this adapter could be extracted back into a library.

**Build:** 0 errors, 0 warnings. **Tests:** 80/80 pass (net8.0 + net10.0).
