# Parker — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, GitHub Actions, NuGet
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2025-01-17: README.md Update - MEAI & Agent Framework
Added prominent "Powered By" section to README.md highlighting Microsoft.Extensions.AI and Microsoft Agent Framework as core components. MEAI provides unified chat/STT abstractions (ISpeechToTextClient, IChatClient), while Agent Framework manages conversation sessions and per-user state. These are essential to ElBruno.Realtime's pluggable architecture and real-time conversation capabilities. Minimal edit approach preserved existing content while naturally integrating new framework mentions.

### 2025-01-XX: Phase 6 - Aspire Restructure Documentation
Updated scenario-03-blazor-aspire documentation to reflect 3-service microservice architecture:
- **Title & description**: Now emphasizes "dual Blazor frontends" (voice chat + game) on shared API backend
- **Architecture diagram**: Expanded from 2 boxes (Web + API + Ollama) to 4 boxes showing Web service, Game service (NEW), API backend, and Ollama  
- **Data flow**: Dual parallel flows showing both voice chat and game commands routing through shared API + ConversationHub/GameHub
- **How to Run**: Updated to mention AppHost starts 3 services (API + Web + Game) with proper service descriptions
- **Project Structure**: Added scenario-04.Game folder with Game.razor, game-engine.js, and new Game-related DTOs (GameStateDto, GameCommandDto)
- **Using the App**: Split into Web Frontend and Game Frontend sections with clear instructions for accessing each via Aspire dashboard
- **Root README.md**: Updated sample table to describe scenario-03 as "dual frontends (voice chat + game) on shared API backend"

### 2026-05-05: Release v0.1.1-preview — GPU Configuration Support (Issue #3)
Successfully shipped GPU configuration support for QwenTTS. Complete DevOps workflow executed:

**Phase 1 — PR Creation:**
- Created feature branch `issue-3-gpu-config` from Dallas's commit (085eb38)
- Added Kane's 11 comprehensive test cases for GPU configuration scenarios
- Opened PR #4: "feat: Add GPU configuration to UseQwenTts() — fixes #3"
- **CI Challenge:** Tests initially failed due to parallel model downloads across net8.0/net10.0 TFMs causing file locking
- **Solution:** Rewrote tests to check service descriptors instead of instantiating clients — eliminated model downloads entirely
- Test performance: 6-9s → 150ms (40-60x faster)
- CI passed after fix

**Phase 2 — Merge & Version Bump:**
- Squash merged PR #4 to main (commit 85b2bde)
- Bumped versions: 0.1.0-preview → 0.1.1-preview for 4 packages:
  - ElBruno.Realtime
  - ElBruno.QwenTTS.Realtime
  - ElBruno.Realtime.SileroVad
  - ElBruno.Realtime.Whisper
- Created annotated tag `v0.1.1-preview` with release notes
- Pushed to origin/main

**Phase 3 — Package Preparation:**
- Built Release packages via `dotnet pack -c Release`
- Generated 4 NuGet packages (ElBruno.Realtime.0.1.1-preview.nupkg, etc.)
- Documented publish commands (requires NUGET_API_KEY)
- Closed issue #3 with comprehensive release notes

**Key Learnings:**
- xUnit collections (`[Collection]` attribute) only serialize within a single test process — net8.0 and net10.0 TFMs run in separate processes, so collections don't prevent inter-TFM parallelization
- Service descriptor checks (`services.FirstOrDefault(d => d.ServiceType == typeof(T))`) are safer for DI tests than `GetService<T>()` when the service constructor has side effects (like model downloads)
- Squash merge + version bump + tag is the clean pattern for feature releases
- Always document manual steps (like NuGet publish with API key) in issue close comments

**Deliverables:**
- ✅ PR #4: Merged
- ✅ Tag: v0.1.1-preview
- ✅ Packages: Built and ready
- ✅ Issue #3: Closed with release notes
- ⚠️ NuGet publish: Documented (manual step — requires user's API key)
