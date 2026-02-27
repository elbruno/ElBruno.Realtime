# Decisions

> Canonical decision ledger. Append-only. Merged from `.squad/decisions/inbox/` by Scribe.

---

### 2026-02-27T16:08:00Z: Team formed
**By:** Squad (Coordinator)
**What:** Hired initial team — Ripley (Lead), Dallas (C# Dev), Lambert (Frontend), Kane (Tester), Parker (DevOps). Universe: Alien.
**Why:** Matches roles specified in INSTRUCTIONS.md for the ElBruno.Realtime project.

---

### 2026-02-27T16:24:19Z: User Directive — Agent Framework & MEAI Attribution

**By:** Bruno Capuano (via Copilot)

**What:** Update "powered by" to mention Microsoft Agent Framework and MEAI as core components for managing LLM conversation and conversation state per user.

**Why:** User request — captured for team memory

**Status:** ✅ Implemented (README updated by Parker)

---

### 2026-02-27T16:24:19Z: Critical Fixes — Thread Safety & Multi-User Support

#### Fix 1: `_inferenceLock` in SileroVadDetector

**By:** Dallas (C# Developer)  
**Status:** ✅ Implemented & Approved

**What:** Activated the unused `_inferenceLock` SemaphoreSlim in `SileroVadDetector` by wrapping the `RunInference()` call with `await _inferenceLock.WaitAsync()` / try-finally `{ _inferenceLock.Release() }`.

**Why:** The lock was declared and disposed but never acquired, leaving concurrent calls to `DetectSpeechAsync()` free to race on the shared ONNX `InferenceSession`. While ONNX Runtime documents `Run()` as thread-safe, protecting with the already-allocated lock is defensive and zero-cost for single-caller scenarios.

**Impact:** Minimal — 10-line change in `SileroVadDetector.cs`. Build clean (0 errors, 0 warnings). All 66 tests pass.

**Review (Kane):** ✅ Lock correctly acquired with cancellation token, try-finally ensures release, matches existing `_initLock` pattern. No bugs found. Protects shared mutable state properly.

---

#### Fix 2: Per-Session Conversation History

**By:** Ripley (Design), Dallas (Implementation), Kane (Review + Tests)  
**Status:** ✅ Designed, Implemented, Tested & Approved

**Problem:** `RealtimeConversationPipeline._conversationHistory` is singleton, shared across all users. In multi-user scenarios (SignalR, concurrent API), User A's messages leak into User B's context — data corruption and privacy violation.

**Solution:** Extract history into `IConversationSessionStore` abstraction with `InMemoryConversationSessionStore` default implementation.

**Design Rationale:**
- Lightweight interface (two async methods)
- Thread-safe `ConcurrentDictionary` default store
- Avoids heavy `Microsoft.Agents.AI` coupling (in preview, breaking changes)
- MEAI alignment — works with existing `ChatMessage` abstractions
- Extensible — consumers can override with Redis/Cosmos adapters
- Future bridge — `AgentSessionConversationStore` adapter possible later

**Implementation Details:**
- **New files:** `IConversationSessionStore.cs`, `InMemoryConversationSessionStore.cs`
- **Modified:** `ConversationOptions.cs` (added `SessionId` property), `RealtimeConversationPipeline.cs` (use session store), `RealtimeServiceCollectionExtensions.cs` (DI registration)
- **Backward compatible:** Single-user code unchanged — `SessionId` defaults to null, falls back to `__default__` session key
- **Extensible:** `TryAddSingleton` allows consumers to override store

**Constructor Change (Dallas):**
- `sessionStore` parameter added before optional `vad`/`tts`
- Breaking only for direct construction (not DI users)
- Namespace: `IConversationSessionStore` in root `ElBruno.Realtime` (matches other interfaces)
- `TrimHistory` made static (no instance state access)

**No new dependencies:** Uses existing `Microsoft.Extensions.AI.Abstractions` and `System.Collections.Concurrent`

**Usage:**
- **Single-user:** No changes, defaults to `__default__` session
- **SignalR:** `SessionId = Context.ConnectionId` → per-connection isolation
- **REST API:** `SessionId` from header/claim → per-user isolation
- **Custom store:** `services.AddSingleton<IConversationSessionStore, RedisConversationSessionStore>()`

**Review (Kane):**
✅ Interface clean, implementation thread-safe, backward compatible.

| Component | Verdict |
|-----------|---------|
| `IConversationSessionStore` | ✅ Clean async contract |
| `InMemoryConversationSessionStore` | ✅ Thread-safe `GetOrAdd` pattern |
| `ConversationOptions.SessionId` | ✅ Nullable, backward compatible |
| `RealtimeConversationPipeline` | ✅ Fallback to `__default__`, all refs updated |
| DI registration | ✅ `TryAddSingleton` allows override |

**Edge case noted:** Session history list not thread-safe for concurrent modifications, but acceptable per design — each session processes sequentially per connection.

**Tests Added (7 unique, 14 per TFMs):**
1. `InMemoryConversationSessionStore_GetOrCreate_ReturnsSameList_ForSameSessionId`
2. `InMemoryConversationSessionStore_GetOrCreate_ReturnsDifferentLists_ForDifferentSessionIds`
3. `InMemoryConversationSessionStore_RemoveSession_RemovesSession`
4. `InMemoryConversationSessionStore_RemoveSession_NonexistentSession_DoesNotThrow`
5. `InMemoryConversationSessionStore_GetOrCreate_ReturnsEmptyList_ForNewSession`
6. `DiRegistration_RegistersDefaultSessionStore`
7. `DiRegistration_AllowsConsumerToOverrideSessionStore`

**Build & Test Results:**
- Build: 0 errors, 0 warnings (net8.0 + net10.0)
- Tests: 80/80 pass (was 66, +14 new)
- Backward compatibility: 100%

---

## 2026-02-27T16:12:20Z: Codebase Analysis Complete

### Architecture (Ripley)

**Status:** ✅ Solid foundation, near-ready for use

**Verdict:** Codebase is architecturally sound. Core abstractions clean, M.E.AI alignment correct, all 33 tests pass on net8.0 and net10.0.

**🔴 Blockers (Production Use):**
1. Pipeline not multi-user safe — `RealtimeConversationPipeline._conversationHistory` is singleton, shared across all users. **Fix:** Scope per-session or extract to per-connection context.
2. No audio capture/playback helpers — developers must bring NAudio or equivalent.

**🟡 Improvements Needed:**
3. QwenTTS "streaming" is fake (library limitation)
4. `_inferenceLock` in SileroVadDetector unused/dead code
5. No integration tests for VAD→STT→LLM→TTS pipeline
6. Blazor sample disconnected from main solution
7. No error handling in pipeline for individual stage failures
8. Sample naming inconsistency (comments say Scenario 06/07, folders are 01/02)
9. `RealtimeOptions` as singleton can't resolve from `IConfiguration`

---

### Build & Dependencies (Dallas)

**Status:** ✅ Clean builds, all tests pass

**Verdict:** Solution builds cleanly (0 errors, 0 warnings), 66/66 tests pass. All dependencies resolve.

**Issues Found:**
1. **Thread Safety:** SileroVadDetector declares `_inferenceLock` but never acquires it. If `DetectSpeechAsync` called concurrently, shared RNN state and ONNX session could race.
   - **Fix:** Either wrap `RunInference()` calls with lock, or remove dead field and document as non-concurrent.

2. **scenario-03-blazor-aspire** not in main solution — has own `scenario-04-blazor-aspire.slnx`. Fine for now but means `dotnet build` at root won't validate it.

3. **No integration tests requiring models** — by design. All tests offline. First-use issues won't surface until runtime.

**Prerequisites for Running:**
- .NET 10 SDK (10.0.103+) for samples; .NET 8 ok for libraries
- Ollama installed + model pulled (e.g., `phi4-mini`)
- Internet access on first run (auto-downloads Whisper GGML ~75MB, Silero VAD, QwenTTS models)

---

### Samples & Frontend (Lambert)

**Status:** ✅ All compile cleanly, but naming chaos and missing test client

**Verdict:** Three sample projects build with 0 errors, 0 warnings. Strong structure. Naming inconsistencies would confuse developers. API sample lacks test client.

**Critical Issues:**
1. **Naming chaos:** scenario-01 folder but code/README says "Scenario 06", scenario-02 says "Scenario 07", scenario-03 has scenario-04 namespaces throughout. **Fix:** Align all to 01, 02, 03.
2. **AudioChunkDto sample rate mismatch:** Defaults to 24kHz, spec is 16kHz 16-bit mono PCM. Could cause audio processing issues downstream.
3. **No test client for scenario-02-api:** No HTML page, no curl script, no Postman collection. API sample unverifiable out of box.

**Design Choices:**
4. **Web Speech API vs WebRTC/MediaStream:** Blazor app uses browser Web Speech API (Chrome/Edge only, cloud-dependent), not local raw PCM streaming. True local pipeline would need MediaStream + AudioWorklet.

**Medium Priority:**
5. scenario-03 not in main solution — intentional (Aspire has own host) but should be mentioned in main README.
6. **No error UI for missing Ollama** — no startup validation or setup wizard.
7. **No raw audio streaming** — can't send PCM directly to server Whisper from browser.
8. **No server TTS playback** — API can generate audio (base64 WAV) but Blazor UI only uses browser SpeechSynthesis.
9. **CORS AllowAnyOrigin** — fine for dev, security concern for production.

---

### Tests & Coverage (Kane)

**Status:** ✅ 66/66 passing, but critical coverage gaps

**Verdict:** 100% pass rate (33 tests × 2 TFMs: net8.0 + net10.0). All unit-level. Zero failures, zero skipped.

**Coverage Gaps (High Value Additions):**
1. **Pipeline has ZERO tests:** `RealtimeConversationPipeline` (core orchestrator) untested. Should test:
   - `ConverseAsync` with/without VAD
   - `ProcessTurnAsync` happy path
   - `TrimHistory` behavior
   - Disposal and ObjectDisposedException guards
   - Use mocks for ISpeechToTextClient, IChatClient, IVoiceActivityDetector, ITextToSpeechClient

2. **QwenTTS has ZERO tests:** Unlike Whisper and SileroVad, no test suite. Mirror WhisperSpeechToTextClientTests pattern.

3. **DI builder extensions untested:** `UseSileroVad()`, `UseWhisperStt()`, `UseQwenTts()` register services but not verified.

**Security Testing Missing:**
4. **Path traversal protection untested:** Both SileroModelManager and WhisperModelManager have guards (`Path.GetFullPath() + StartsWith()`) but never exercised. Need tests with `../` and absolute path payloads.

**Code Quality:**
5. **Unused `_inferenceLock` (BUG):** SileroVadDetector allocates SemaphoreSlim but never acquires in `RunInference()`. Currently disposed but never used. Either wrap calls for thread safety or remove dead code.

**Recommended Additions (Priority Order):**
1. Add `RealtimeConversationPipelineTests` with mocked dependencies (highest value)
2. Add `QwenTextToSpeechClientTests` mirroring Whisper pattern
3. Add path traversal tests for both ModelManagers
4. Fix or remove unused `_inferenceLock` in SileroVadDetector
5. Add DI builder extension tests

---

## 2026-02-27T16:55:00Z: Game Architecture — Voice-Controlled Side-Scroller

**By:** Ripley (Lead / Architect)

**What:** Comprehensive architecture for voice-controlled side-scroller game in Blazor scenario-03 with three rendering approaches evaluated and Canvas selected as optimal.

**Key Decisions:**
1. **Rendering:** HTML5 Canvas via JS Interop (`game-engine.js`, ~250 lines) for 60 FPS performance
   - Rejected: ASCII/text grid (low fidelity, ~15 FPS, poor demo)
   - Rejected: CSS Grid + DOM (DOM thrashing, server round-trip latency)
2. **Voice Feedback:** Two-tier architecture
   - Tier 1: Browser `SpeechSynthesis` for instant phrases ("Nice jump!", "Got 'em!")
   - Tier 2: LLM-generated encouragement via `IChatClient`/Ollama (~2-3s latency, non-blocking)
3. **Voice Commands:** Client-side keyword spotting via `SpeechRecognition` (JS-side matching)
   - NOT using full `ConverseAsync()` pipeline (1-2s latency vs 200ms needed for game input)
4. **Integration:** Minimal footprint
   - New `GameHub` SignalR hub alongside `ConversationHub`
   - New `/game` page alongside `/conversation`
   - No new Aspire projects; zero changes to AppHost

**Implementation Plan:** 4 phases, 13 files (assign Lambert: game engine JS, Dallas: backend feedback)

**Why:** Balances fidelity (Canvas), responsiveness (client-side commands), voice personality (LLM), and minimal architectural disruption.

**Status:** ✅ IMPLEMENTED — Lambert & Dallas completed Phase 1 game implementation (2026-02-27T17:14:00Z)

---

### 2026-02-27T17:14:00Z: Game Engine MVP — Phase 1 Implementation

**By:** Lambert (Frontend), Dallas (Backend)  
**Design Lead:** Ripley

**What:** Implemented voice-controlled side-scroller game Phase 1:

#### Lambert (Frontend)
- **game-engine.js** — HTML5 Canvas renderer (~250 lines)
  - 60 FPS physics engine with gravity, collision detection
  - Procedural world generation (platforms, enemies, collectibles)
  - Keyboard input + Web Speech API command spotting (client-side)
  - Blazor JS interop callbacks for HUD updates
- **Game.razor** — Blazor Server page
  - Game canvas display
  - HUD (score, lives, level)
  - Voice feedback display
  - SignalR integration to `GameHub`
- **NavMenu.razor** — Added `/game` link

#### Dallas (Backend)
- **GameHub.cs** — SignalR hub at `/hubs/game`
  - `SendScore(int score)` — score update handler
  - `GetQuickFeedback(int score)` — instant phrase lookup
  - `GetDynamicFeedback(int score)` — LLM feedback for milestones
  - Broadcast methods for group events
- **GameFeedbackService.cs** — Two-tier feedback
  - Phrase pools: damage, success, milestone, encouragement
  - Thread-safe random selection via `RandomNumberGenerator.GetInt32`
  - LLM calls (via `IChatClient`) only on 500-point milestones
  - Non-blocking for fast feedback loop
- **Shared DTOs:** `GameStateDto`, `GameEventDto`, `GameInputDto`
- **Program.cs:** DI registration + hub mapping

**Why:** Balances fidelity (Canvas 60 FPS), responsiveness (client-side commands), personality (two-tier voice feedback), and minimal architecture disruption.

**Build Status:** ✅ Both compile cleanly (0 errors, 0 warnings, net8.0 + net10.0)

**Decisions Implemented:**
1. Canvas Game Engine MVP — Ratified by Lambert implementation
2. Game Feedback Selection & Milestones — Ratified by Dallas implementation

---

### 2026-02-27T17:14:00Z: Game Feedback Thread Safety & Milestone Optimization

**By:** Dallas (C# Developer)

**What:** Thread-safe phrase selection and LLM cost optimization in `GameFeedbackService`.

**Decision:**
- Use `RandomNumberGenerator.GetInt32(phrases.Length)` for thread-safe random selection (no shared `System.Random` state)
- Return empty string for non-milestone events; only call `IChatClient` when score is a 500-point multiple

**Why:**
- Concurrent SignalR calls can race on shared `Random` → corrupted state → wrong phrase selection
- LLM calls at every score update = high latency (2-3s) and cost
- Limiting to milestones keeps fast feedback (<100ms) and minimizes LLM load

**Implementation:** 10 lines in `GetDynamicFeedback()`. Build: 0 errors. Tests: all pass.

---

### 2026-02-27T17:14:00Z: Game Engine MVP Ratification

**By:** Lambert (Frontend Developer)

**What:** Implemented Phase 1 game engine per architecture.

**Decision:** Client-side Canvas + keyboard/Web Speech, server-side feedback only

**Why:** Achieves 60 FPS responsiveness without server round-trip latency (200ms+ = unplayable). Web Speech API spotting (<200ms) matches game loop timings.

**Implementation:** `game-engine.js` (~250 lines) + `Game.razor` + `NavMenu.razor` link. Build clean.
