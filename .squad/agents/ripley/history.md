# Ripley — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, ONNX Runtime, Whisper.net, Blazor, ASP.NET Core, SignalR
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2026-02-27: Full Architecture Review — Baseline Assessment

**Solution structure (7 projects):**
- `ElBruno.Realtime` — Core abstractions + pipeline orchestration. Key interfaces: `IRealtimeConversationClient`, `ITextToSpeechClient`, `IVoiceActivityDetector`. STT uses M.E.AI's `ISpeechToTextClient`, LLM uses M.E.AI's `IChatClient`.
- `ElBruno.Realtime.Whisper` — `ISpeechToTextClient` impl via Whisper.net. Auto-downloads GGML models. Solid.
- `ElBruno.Realtime.QwenTTS` — `ITextToSpeechClient` impl via ElBruno.QwenTTS. Depends on external `ElBruno.QwenTTS` NuGet (0.1.7-preview). Non-streaming (synthesize-fully-then-yield).
- `ElBruno.Realtime.SileroVad` — `IVoiceActivityDetector` impl via Silero VAD v5 ONNX. Downloads from HuggingFace. Uses `ElBruno.HuggingFace.Downloader`.
- `ElBruno.Realtime.Tests` — 33 tests, all passing (net8.0 + net10.0). Unit-level only; no integration tests.
- `scenario-01-console` — One-shot turn demo: WAV → STT → Ollama → TTS → WAV. net10.0 only.
- `scenario-02-api` — ASP.NET Core + SignalR streaming hub. net10.0 only.
- `scenario-03-blazor-aspire` — Blazor + Aspire (separate slnx, not in main solution). Text-only chat; audio integration is placeholder.

**Key file paths:**
- Core interfaces: `src/ElBruno.Realtime/Abstractions/`
- Pipeline: `src/ElBruno.Realtime/Pipeline/RealtimeConversationPipeline.cs`
- DI: `src/ElBruno.Realtime/DependencyInjection/RealtimeServiceCollectionExtensions.cs`
- Options: `src/ElBruno.Realtime/Options/RealtimeOptions.cs`
- Builder extensions: each provider has `*RealtimeBuilderExtensions.cs`

**Build status:** ✅ Clean build, 0 warnings, 0 errors. All 33 tests pass on both TFMs.

**Architecture patterns:**
- Follows M.E.AI patterns well: `ISpeechToTextClient` from M.E.AI, custom `ITextToSpeechClient` and `IVoiceActivityDetector` in same style.
- All providers use lazy initialization with `SemaphoreSlim` for thread-safe model loading.
- All providers have auto-download model managers.
- DI uses `AddPersonaPlexRealtime()` → `RealtimeBuilder` fluent API.
- Pipeline is singleton; `_conversationHistory` is instance-level (not thread-safe for concurrent users).

**Architectural concerns identified:**
1. Pipeline `_conversationHistory` is shared state — not safe for multi-user scenarios (API/SignalR).
2. Samples hard-code `OllamaChatClient` constructor directly — should use `AddChatClient` extension method pattern for consistency.
3. No audio capture/playback utilities — developer must bring their own mic/speaker (NAudio etc).
4. QwenTTS streaming is fake (synthesize-fully-then-yield-one-chunk). True streaming would improve latency.
5. Scenario-03 (Blazor+Aspire) is not in the main .slnx and is text-only; audio pipeline integration is future work.
6. No CI/CD workflow visible in the solution (though badge references `publish.yml`).
7. `_inferenceLock` in SileroVadDetector is created but never used — dead code.
