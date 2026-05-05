# Kane — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, xUnit, ONNX Runtime
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2026-05-05: Issue #3 GPU Configuration Tests — Complete

**Test Results:** 100/100 passing (50 tests × 2 TFMs: net8.0 + net10.0). Zero failures, zero skipped.

**New Test Class Added:** `QwenTtsRealtimeExtensionsTests` — 10 tests validating GPU device ID configuration

**Test Coverage:**
1. `UseQwenTts_NoCallback_RegistersServices` — Backward compatibility (no callback parameter)
2. `UseQwenTts_WithDeviceIdCallback_RegistersServicesWithConfiguration` — GPU device ID callback (e.g., `opts.GpuDeviceId = 1`)
3. `UseQwenTts_MultipleConfigurationOptions_AcceptsAllOptions` — Multiple configuration options chained
4. `AddQwenTtsRealtime_NoCallback_RegistersServices` — IServiceCollection overload backward compatibility
5. `AddQwenTtsRealtime_WithConfiguration_RegistersServicesAndInvokesCallback` — IServiceCollection with configuration
6. `UseQwenTts_NullCallback_RegistersServicesWithDefaults` — Edge case: explicit null callback
7. `AddQwenTtsRealtime_NullCallback_RegistersServicesWithDefaults` — Edge case: null callback on IServiceCollection
8. `UseQwenTts_ChainedWithOtherBuilderCalls_WorksCorrectly` — Fluent chaining with other builder methods
9. `UseQwenTts_ReturnsBuilderForChaining` — Fluent API pattern verification
10. `AddQwenTtsRealtime_ReturnsServiceCollectionForChaining` — IServiceCollection chaining verification

**Implementation Details:**
- Feature implemented by Dallas (C# Dev) in commit `085eb38`: Added `Action<QwenTtsOptions>?` callback parameter to both `UseQwenTts()` and `AddQwenTtsRealtime()` methods
- Property name: `GpuDeviceId` (not `DeviceId`) — matches ElBruno.QwenTTS v0.1.8-preview API
- Backward compatible: Optional parameter with null default, existing code unaffected
- XML docs include GPU configuration examples

**Edge Cases Validated:**
- Null callback safety — services register with defaults
- Fluent API chaining — builder/collection instances returned correctly
- Multiple configuration options — callback can set multiple properties without errors
- Both extension method variants tested (RealtimeBuilder and IServiceCollection)

**Commits:**
- `085eb38` — feat: Add GPU configuration callback to UseQwenTts() — fixes issue #3
- `9547bbd` — test: Add comprehensive tests for GPU configuration in UseQwenTts() — issue #3

**Test Pass Rate:** 100% (50/50 across net8.0 and net10.0)

**Findings:**
- All tests execute successfully
- QwenTTS package provides `GpuDeviceId` property (int, default: 0) for GPU device selection
- Configuration callback pattern follows .NET DI conventions
- No breaking changes introduced — fully backward compatible
- Tests download QwenTTS models on first run (~several seconds per test run)



### 2025-07-18: Initial Test Suite Analysis

**Test Results:** 66/66 passing (33 tests × 2 TFMs: net8.0 + net10.0). Zero failures, zero skipped.

**Test Classes (4 files, 5 logical classes):**
- `AbstractionTests` — 12 tests: Options cloning, SpeechSegment duration, ConversationEvent defaults, disposal, enum coverage
- `DiRegistrationTests` — 4 tests: DI registration, builder pattern, IChatClient wiring, RealtimeOptions defaults
- `SileroVadTests` — 3 tests: GetService, Dispose idempotency
- `WhisperModelManagerTests` — 2 tests: SupportedModels list, unknown model rejection
- `WhisperSpeechToTextClientTests` — 4 tests: GetService, Dispose idempotency, ObjectDisposedException on disposed client

**Coverage Gaps Identified:**
1. **RealtimeConversationPipeline** — ZERO tests. This is the core orchestration component (VAD→STT→LLM→TTS). No tests for ConverseAsync, ProcessTurnAsync, TrimHistory, GetService, Dispose, or ObjectDisposedException guard.
2. **QwenTextToSpeechClient** — ZERO tests. No GetService, Dispose, or ObjectDisposedException tests (unlike Whisper which has these).
3. **DI builder extensions** — UseSileroVad, UseWhisperStt, UseQwenTts not tested for correct service registration.
4. **No security tests** — Path traversal protection in SileroModelManager and WhisperModelManager is not tested. Both have `Path.GetFullPath() + StartsWith()` guards that should be validated.
5. **No concurrency tests** — SemaphoreSlim patterns (_initLock, _inferenceLock) untested.
6. **No edge cases** — Empty audio, malformed PCM input, null/whitespace text to TTS, concurrent disposal.
7. **SileroVadDetector.DetectSpeechAsync** — Core VAD logic untested (would need model or mock).
8. **ConvertBytesToFloat / CreateSegment** — Internal helpers in SileroVadDetector untested.

**Security Observations:**
- Path traversal guards exist in both ModelManagers using `Path.GetFullPath() + StartsWith()` — good pattern but untested.
- No input validation on audio format (arbitrary bytes accepted as PCM16).
- QwenTTS uses `Path.GetTempPath()` with GUID for temp files — safe pattern.
- `_inferenceLock` SemaphoreSlim declared in SileroVadDetector but never actually used in RunInference — potential thread safety bug.

### 2026-02-27: Review of Critical Fixes (InferenceLock + Per-Session History)

**Reviewed two fixes, both approved:**

1. **`_inferenceLock` fix in SileroVadDetector** — Lock now properly acquired around `RunInference()` with try-finally at lines 90–99. CancellationToken passed to WaitAsync. No bugs found.

2. **Per-session conversation history** — New `IConversationSessionStore` interface + `InMemoryConversationSessionStore` (ConcurrentDictionary-backed). Pipeline uses `DefaultSessionId ("__default__")` when `ConversationOptions.SessionId` is null for backward compatibility. DI uses `TryAddSingleton` so consumers can override.

**Tests added:** 7 new tests (80 total across 2 TFMs, all passing):
- 5 tests for `InMemoryConversationSessionStore` (same-ID identity, different-ID isolation, remove, remove-nonexistent, new-session-empty)
- 2 DI tests (default store registration, consumer override via TryAddSingleton)
- 1 assertion added to existing `ConversationOptions_DefaultValues` test for `SessionId`

**Design note:** The `List<ChatMessage>` returned by the store is not itself thread-safe, but this is acceptable since the pipeline processes one turn at a time per session. If concurrent session access becomes a requirement, the store should return a synchronized collection.

### 2026-02-27: Game Implementation — Phase 1 Complete

**Cross-agent note:** Lambert & Dallas completed game Phase 1. No new tests required at this stage (canvas/physics tested manually, backend integration via game sessions). Recommend game integration tests in Phase 2 if multiplayer sessions need concurrency validation.

### 2026-02-27: Phase 5 — Aspire Restructure Verification (Game Project Split)

**All 6 checks PASSED.** Dallas scaffolded `scenario-04.Game`, Lambert moved files, structure verified end-to-end.

| # | Check | Result |
|---|-------|--------|
| 1 | Aspire solution build (`scenario-04-blazor-aspire.slnx`) | ✅ PASS — 0 errors, 0 warnings, all 6 projects built |
| 2 | Main solution build (`ElBruno.Realtime.slnx`) | ✅ PASS — 0 errors, 0 warnings, no regression |
| 3 | Test suite | ✅ PASS — 80/80 tests pass (net8.0 + net10.0) |
| 4 | File presence | ✅ PASS — see details below |
| 5 | AppHost verification | ✅ PASS — references Api, Web, Game; registers `api`, `web`, `game` |
| 6 | Solution file | ✅ PASS — all 6 projects present (AppHost, ServiceDefaults, Api, Web, Game, Shared) |

**File presence details (Check 4):**
- `scenario-04.Web/Components/Pages/`: Conversation.razor ✅, Index.razor ✅ | Game.razor ❌ (absent, correct), game-engine.js ❌ (absent, correct)
- `scenario-04.Web/wwwroot/js/`: audio-recorder.js ✅
- `scenario-04.Game/Components/Pages/`: Game.razor ✅, Index.razor ✅ | Conversation.razor ❌ (absent, correct), audio-recorder.js ❌ (absent, correct)
- `scenario-04.Game/wwwroot/js/`: game-engine.js ✅
- `scenario-04.Api/Hubs/`: ConversationHub.cs ✅, GameHub.cs ✅
- `scenario-04.Api/Services/`: ConversationService.cs ✅, GameFeedbackService.cs ✅

**AppHost details (Check 5):**
- `.csproj` has `<ProjectReference>` to Api, Web, and Game ✅
- `Program.cs` registers `api`, `web`, and `game` via `AddProject<>()` ✅
- Game project gets `WithReference(api).WaitFor(api).WithExternalHttpEndpoints()` ✅

**Verdict:** Restructure is clean. Game concern fully separated from Web. No regressions in main solution or test suite.
