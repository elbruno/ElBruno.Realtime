# Approved Decisions

## 2026-02-27: QwenTTS Migration — Local Project → NuGet Package

**By:** Dallas (C# Developer), requested by Bruno Capuano  
**Status:** ✅ Implemented  
**Date:** 2026-02-27

### What

Removed the `ElBruno.Realtime.QwenTTS` local project entirely and migrated samples to use `ElBruno.QwenTTS` v0.1.8-preview NuGet package directly.

### Why

The upstream `ElBruno.QwenTTS` v0.1.8-preview now ships `AddQwenTts()` DI extension (via `QwenTtsServiceExtensions`), `QwenTtsOptions`, and `ITtsPipeline` interface — making the local wrapper project redundant for pipeline creation and model management.

### Design Decision: Adapter Placement

The NuGet's `AddQwenTts()` registers `ITtsPipeline`, but the Realtime pipeline consumes `ITextToSpeechClient`. Options considered:

1. **Main `ElBruno.Realtime` project** — Rejected: would add `ElBruno.QwenTTS` dependency to the core abstractions package
2. **New bridge NuGet** — Rejected: over-engineering for two sample projects
3. **Adapter in each sample** — ✅ Selected: minimal, explicit, no new packages

Each sample gets a `QwenTextToSpeechClientAdapter` (~95 lines) that wraps `ITtsPipeline` → `ITextToSpeechClient`. Simpler than the original `QwenTextToSpeechClient` since DI handles pipeline lifecycle.

### Changes

| File | Change |
|------|--------|
| `src/ElBruno.Realtime.QwenTTS/` | **Deleted** (entire project) |
| `ElBruno.Realtime.slnx` | Removed project entry |
| `.github/workflows/publish.yml` | Removed pack step |
| `scenario-01-console.csproj` | `ProjectRef` → `PackageRef ElBruno.QwenTTS 0.1.8-preview` |
| `scenario-02-api.csproj` | `ProjectRef` → `PackageRef ElBruno.QwenTTS 0.1.8-preview` |
| `scenario-01-console/Program.cs` | `UseQwenTts()` → `AddQwenTts()` + adapter registration |
| `scenario-02-api/Program.cs` | Same |
| `scenario-01-console/QwenTextToSpeechClientAdapter.cs` | **New** adapter |
| `scenario-02-api/QwenTextToSpeechClientAdapter.cs` | **New** adapter |

### Impact

- **Build:** 0 errors, 0 warnings
- **Tests:** 80/80 pass (net8.0 + net10.0)
- **Breaking:** `ElBruno.Realtime.QwenTTS` NuGet package will no longer be published. Consumers should use `ElBruno.QwenTTS` v0.1.8-preview directly with an adapter.
- **scenario-03-blazor-aspire:** Not in main solution; may need similar migration separately.
