# Ripley — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, ONNX Runtime, Whisper.net, Blazor, ASP.NET Core, SignalR
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2026-02-27: Game Architecture — Phase 1 Complete

**Designed:** Voice-controlled side-scroller game architecture (Canvas rendering, two-tier feedback, minimal SignalR integration).

**Build outcome:** ✅ Lambert completed `game-engine.js` + `Game.razor` + `NavMenu` integration. Dallas completed `GameHub` + `GameFeedbackService` + shared DTOs. Both builds clean (0 errors). Architecture decisions ratified in `.squad/decisions.md` (Phase 1 milestone).

**Status:** Phase 1 ✅ COMPLETE. Ready for Phase 2 (leaderboard persistence, multi-player).

### 2026-02-27: Per-Session Conversation History Design

**Problem:** `RealtimeConversationPipeline._conversationHistory` is a singleton-scoped `List<ChatMessage>` — corrupts in multi-user scenarios (SignalR, concurrent API).

**Decision:** Introduced `IConversationSessionStore` abstraction with `InMemoryConversationSessionStore` default. Session ID flows through `ConversationOptions.SessionId`. Avoids taking a dependency on the full `Microsoft.Agents.AI` package (too heavy, preview churn), but the interface is shaped for a future Agent Framework adapter. No new NuGet packages needed — uses existing `ConcurrentDictionary` + `List<ChatMessage>` from M.E.AI.Abstractions.

**Key insight:** The Microsoft Agent Framework's `AgentSession` is conceptually right (per-session state with serialize/deserialize), but pulling in `Microsoft.Agents.AI` for just session management would be over-engineering. A thin `IConversationSessionStore` with `GetOrCreateSessionAsync(sessionId)` achieves the same goal with zero new dependencies. The store is registered via `TryAddSingleton` so consumers can swap in Redis/Cosmos/AgentSession adapters without changing core library code.

**Design doc:** `.squad/decisions/inbox/ripley-agentsession-design.md`

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

### 2026-02-27: Game Architecture Plan — Voice-Controlled Side-Scroller

**Problem:** Bruno requested a Super Mario-style side-scrolling game for scenario-03-blazor-aspire with voice commands ("jump", "shoot") and spoken feedback ("good job" etc).

**Analysis:** Evaluated 3 rendering approaches:
- **Option A (ASCII):** Pure Blazor, ~15 FPS, retro charm, zero JS. Limited by SignalR round-trip for both rendering and input.
- **Option B (Canvas):** HTML5 Canvas via JS interop, 60 FPS, smooth scrolling, zero-latency keyboard. ~250 lines JS needed. ⭐ RECOMMENDED.
- **Option C (CSS Grid):** DOM elements with CSS transitions, ~30 FPS, mostly Blazor. DOM thrashing at scale, input latency concern.

**Decision:** Canvas (Option B) as primary approach. Game engine runs in JS (`game-engine.js`) for rendering + input; C# handles voice feedback orchestration via SignalR `GameHub`. Two-tier TTS: browser SpeechSynthesis for instant phrases, LLM-generated feedback for milestones.

**Key architectural insight:** The ElBruno.Realtime pipeline (`IRealtimeConversationClient.ConverseAsync()`) is NOT suitable for game voice commands — it's designed for conversational turns with VAD→STT→LLM→TTS latency (~1-2s). Game commands need ~200ms latency, which browser Web Speech API provides. Pipeline is useful for the Tier 2 dynamic feedback (LLM-generated encouragement via `IChatClient`).

**Voice integration pattern:** Client-side keyword spotting via Web Speech API `SpeechRecognition` (continuous mode). Small command vocabulary matched in JS. Echo prevention by pausing recognition during TTS (reuses pattern from Conversation page's Speak Mode).

**Aspire integration:** No new projects needed. Game adds a `/game` Blazor page + `GameHub` SignalR hub to existing projects. Aspire AppHost unchanged.

**Plan written to:** session-state/plan.md — 4 implementation phases, 13 files to create/modify, assigned to Lambert (frontend) and Dallas (backend).
