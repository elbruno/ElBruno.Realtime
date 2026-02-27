# Kane — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, xUnit, ONNX Runtime
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

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
