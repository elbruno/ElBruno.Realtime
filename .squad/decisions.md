# Decisions

> Canonical decision ledger. Append-only. Merged from `.squad/decisions/inbox/` by Scribe.

---

### 2026-02-27T16:08:00Z: Team formed
**By:** Squad (Coordinator)
**What:** Hired initial team — Ripley (Lead), Dallas (C# Dev), Lambert (Frontend), Kane (Tester), Parker (DevOps). Universe: Alien.
**Why:** Matches roles specified in INSTRUCTIONS.md for the ElBruno.Realtime project.

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
