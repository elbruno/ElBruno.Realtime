# Decision: ElBruno.QwenTTS.Realtime Bridge Package

**Date:** 2026-02-27
**By:** Dallas (C# Developer)
**Status:** ✅ Implemented

## What

Created `ElBruno.QwenTTS.Realtime` bridge NuGet package that provides:
1. Public `QwenTextToSpeechClientAdapter` — adapts `ITtsPipeline` to `ITextToSpeechClient`
2. `UseQwenTts()` builder extension on `RealtimeBuilder` for fluent pipeline configuration
3. `AddQwenTtsRealtime()` `IServiceCollection` extension for standalone DI registration

## Why

The `QwenTextToSpeechClientAdapter` was duplicated identically in scenario-01-console and scenario-04-realtime-console. Both samples had to manually call `AddQwenTts()` + `AddSingleton<ITextToSpeechClient, Adapter>()`. This bridge eliminates duplication and gives users a single `.UseQwenTts()` call matching the existing `.UseWhisperStt()` pattern.

## Design Decisions

- **Follows Whisper pattern exactly:** csproj structure, NuGet metadata, builder extension method signature all mirror `ElBruno.Realtime.Whisper`
- **Dual extension methods:** `UseQwenTts()` for builder-chain use, `AddQwenTtsRealtime()` for direct IServiceCollection use
- **Adapter made public:** Was `internal sealed` in samples, now `public sealed` for library consumption
- **Transitive dependency:** Samples no longer need direct `ElBruno.QwenTTS` PackageReference — it flows through the bridge

## Impact

- scenario-01: -1 file, -3 lines in Program.cs
- scenario-04: -1 file, -3 lines in Program.cs
- Solution: +1 project (ElBruno.QwenTTS.Realtime)
- Build: 0 errors, 0 warnings. Tests: 80/80 pass.
