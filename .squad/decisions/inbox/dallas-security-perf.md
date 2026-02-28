---
date: 2026-02-28
author: Dallas (C# Developer)
status: completed
related_issue: 1
---

# Security Hardening & Performance Optimization (Phase 1)

## Summary

Implemented security hardening and performance optimizations per Ripley's triage of Issue #1:
- **Input validation:** Added SessionId validation with length limits and character whitelisting
- **File integrity checks:** Added size bounds validation for Whisper and Silero VAD model downloads
- **Performance optimization:** Replaced manual audio normalization loop with SIMD-optimized `TensorPrimitives`

## Implementations

### 1. SessionId Input Validation (Task 1.1)

**File:** `src/ElBruno.Realtime/Abstractions/ConversationOptions.cs`

**What was added:**
- Private backing field `_sessionId` with property setter validation
- `MaxSessionIdLength = 256` constant
- Compiled regex pattern `^[a-zA-Z0-9_-]+$` for character whitelist
- Property setter throws `ArgumentException` with clear messages for:
  - Length exceeding 256 characters
  - Invalid characters (rejects path separators `/`, `\`, null bytes, etc.)
- Null/empty values explicitly allowed (backward compatibility)

**Why:**
- Prevents path traversal attacks via session identifiers
- Enforces reasonable session ID format (alphanumeric + dash/underscore)
- Maintains backward compatibility (null allowed, defaults to `__default__` downstream)
- Clear error messages for validation failures

**Known limitations:**
- Validation only applies to `ConversationOptions.SessionId` property
- Consumers using session store directly can bypass validation (by design — stores are extensible)
- Regex pattern is permissive (allows any length of valid chars up to 256)

---

### 2. File Integrity Checks (Task 1.2)

**Files:**
- `src/ElBruno.Realtime.Whisper/WhisperModelManager.cs`
- `src/ElBruno.Realtime.SileroVad/SileroModelManager.cs`

**What was added:**
- **Whisper models:** Size bounds validation after download
  - Min: 10KB (catches empty/corrupted downloads)
  - Max: 2GB (conservative bound — largest known model ~1.5GB)
  - Throws `InvalidOperationException` with formatted message showing actual vs expected bounds
  - Deletes corrupted file before throwing
- **Silero VAD:** Size bounds validation after download
  - Min: 100KB (catches empty/corrupted downloads)
  - Max: 50MB (Silero VAD v5 is ~1.8MB, leaves headroom for future versions)
  - Same error handling pattern as Whisper

**Why:**
- Detects corrupted downloads (network interruptions, disk full, malicious proxy)
- Prevents loading invalid ONNX/GGML files into inference sessions (would cause runtime crashes)
- Conservative bounds chosen to allow future model versions without code changes
- Explicit file cleanup on failure prevents re-use of corrupted cache

**Bounds rationale:**
- Whisper `tiny.en` is ~75MB, `base` ~150MB, `large-v3` ~1.5GB → 2GB max is safe
- Silero VAD v5 is ~1.8MB → 50MB max allows 27x growth (unlikely)
- Min bounds (10KB/100KB) are conservative — actual models are 100x+ larger

**Known limitations:**
- Does **not** validate ONNX/GGML format correctness (parser validation happens at model load time)
- Does **not** verify checksums/hashes (relies on HTTPS transport security)
- Relies on external downloaders (`ElBruno.HuggingFace.Downloader`, `Whisper.net` downloader) for HTTPS enforcement
- Bounds are hardcoded (not configurable) — acceptable for known model sources

---

### 3. TensorPrimitives Optimization (Task 2.2)

**File:** `src/ElBruno.Realtime.SileroVad/SileroVadDetector.cs` (line ~206)

**What was changed:**
- Added `System.Numerics.Tensors` v9.0.0 NuGet package to `ElBruno.Realtime.SileroVad.csproj`
- Added `using System.Numerics.Tensors;` import
- **Before (manual loop):**
  ```csharp
  for (int i = 0; i < floats.Length; i++)
      floats[i] = sample / 32768f;
  ```
- **After (SIMD-optimized):**
  ```csharp
  for (int i = 0; i < floats.Length; i++)
      floats[i] = sample; // No division in loop
  TensorPrimitives.Divide(floats, 32768f, floats); // SIMD batch operation
  ```

**Why:**
- `TensorPrimitives.Divide` uses SIMD instructions (AVX2/AVX-512 on x86, NEON on ARM)
- Expected 2-10x speedup for audio normalization (512-sample windows, called frequently)
- Reduces CPU time in VAD hot path (called every 32ms for continuous audio)
- Zero allocation overhead (operates in-place on existing array)

**Integration with ONNX Runtime:**
- No compatibility issues — `TensorPrimitives` operates on plain `float[]` arrays
- ONNX Runtime consumes the normalized floats via `DenseTensor<float>` (unchanged)
- All 80 existing tests pass (66 original + 14 session store tests)

**Known gotchas:**
- `System.Numerics.Tensors` is .NET 8+ only (codebase already targets net8.0 + net10.0)
- SIMD benefits require hardware support (degrades gracefully to scalar on older CPUs)
- Benchmark needed to quantify actual speedup (Task 2.1 not yet implemented)

---

## Build & Test Results

**Build:** ✅ 0 errors, 0 warnings (Release configuration)
- All 9 projects compile cleanly (net8.0 + net10.0 targets)
- New NuGet dependency resolves without conflicts

**Tests:** ✅ 80/80 pass (net8.0 + net10.0)
- 66 original tests (SileroVadDetector, WhisperSpeechToTextClient, InMemoryConversationSessionStore, DI registration)
- 14 session store tests added in prior work
- No regressions from changes

**Verification:**
- SessionId validation tested manually (would require new test suite per Task 1.3)
- File integrity checks only trigger on corrupted downloads (cannot test without mocking downloader)
- TensorPrimitives correctness verified by existing `SileroVadTests` passing (byte-to-float conversion unchanged)

---

## Findings & Learnings

### Current Validation Patterns

**Existing guards (pre-change):**
- Path traversal prevention: Both `WhisperModelManager` and `SileroModelManager` use `Path.GetFullPath()` + `StartsWith()` validation (lines 48-51 and 30-33)
- Null checks: `RealtimeConversationPipeline` constructor uses `ArgumentNullException.ThrowIfNull()` for required dependencies
- Enum validation: Whisper model IDs validated via `Dictionary.TryGetValue()` with clear error messages

**Gaps (now addressed):**
- ✅ SessionId had no validation (length/format) → **Fixed** with regex + length limit
- ✅ Model downloads had no size validation → **Fixed** with min/max bounds
- Audio format validation still missing (16kHz assumption not enforced) → **Out of scope**

### TensorPrimitives Integration

**Compatibility:**
- Works seamlessly with ONNX Runtime (operates on plain arrays)
- No changes needed to `DenseTensor<float>` construction
- `Microsoft.ML.OnnxRuntime` v1.24.2 has no conflicts with `System.Numerics.Tensors` v9.0.0

**Performance considerations:**
- SIMD benefits are CPU-dependent (AVX2/AVX-512 on Intel/AMD, NEON on ARM)
- 512-sample normalization (VAD window size) is ideal for SIMD (32-64 floats/cycle with AVX-512)
- Should be benchmarked (Task 2.1) to quantify actual speedup

**Future opportunities:**
- `ConvertBytesToFloat` could also use `TensorPrimitives` for the byte-to-short conversion
- Other audio processing loops in WhisperSpeechToTextClient may benefit (requires audit)

### File Size Bounds Reasoning

**Conservative vs observed:**
- Whisper: Used 2GB max (observed largest is 1.5GB) for future-proofing
- Silero VAD: Used 50MB max (observed is 1.8MB) — very conservative, 27x headroom
- Min bounds: 10KB/100KB chosen to catch empty files without false positives

**Why not tighter bounds?**
- Model sources (HuggingFace, Whisper.net) may add new variants
- Tight bounds would require code changes for each new model
- Risk of false positives (legitimate new models rejected) > risk of false negatives (slightly-corrupted files passing)

**Limitation:**
- Bounds are hardcoded (not configurable)
- Acceptable because model sources are known and trusted (not user-provided)

---

## Next Steps (Not Implemented)

**Out of scope for Phase 1:**
- Task 1.3: Path traversal test coverage (Kane's work)
- Task 1.4: README security posture section (Ripley's work)
- Task 2.1: BenchmarkDotNet project (Kane + Dallas, required before Task 2.3)
- Task 2.3: Hot-path allocation audit with `ArrayPool` (data-driven, requires benchmarks)

**Recommendations:**
- Add `ConversationOptionsTests.cs` with edge cases:
  - SessionId too long (257+ chars)
  - SessionId with path separators (`../`, `..\\`, `/tmp/`)
  - SessionId with null bytes or control characters
  - Null/empty SessionId (should succeed)
- Add integration test for model download failure scenarios (requires mock downloader or test server)
- Add benchmark to quantify TensorPrimitives speedup (BenchmarkDotNet project in Task 2.1)

---

## Decision Summary

| Component | Decision | Rationale |
|-----------|----------|-----------|
| SessionId validation | Regex `^[a-zA-Z0-9_-]+$`, max 256 chars | Blocks path traversal, maintains backward compat (null allowed) |
| Whisper size bounds | 10KB - 2GB | Conservative (largest known ~1.5GB), catches corruption |
| Silero VAD size bounds | 100KB - 50MB | Very conservative (actual ~1.8MB), future-proof |
| TensorPrimitives | In-place `Divide` after loop | SIMD speedup, zero allocations, ONNX-compatible |
| Error handling | Delete corrupted file + throw | Prevents cached corruption, clear error messages |
| NuGet version | `System.Numerics.Tensors` 9.0.0 | Latest stable, compatible with net8.0/net10.0 |

**Status:** ✅ All Phase 1 tasks complete. No blockers. Ready for Kane's test coverage (Phase 2).
