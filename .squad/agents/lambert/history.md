# Lambert — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, Blazor, ASP.NET Core, SignalR, .NET Aspire
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2025-07-18: Full Sample & Frontend Codebase Analysis

**All three samples compile successfully (0 errors, 0 warnings).**

#### Scenario 01 — Console (`src/samples/scenario-01-console/`)
- **What it does:** One-shot Audio file → Whisper STT → Ollama LLM → QwenTTS → Audio file
- **Status:** Complete and functional. Uses the `IRealtimeConversationClient.ProcessTurnAsync()` API.
- **Key files:** `Program.cs`, `scenario-01-console.csproj`
- **Prereqs:** Ollama running locally with phi4-mini, a 16kHz mono WAV input file
- **Naming inconsistency:** Folder is `scenario-01-console` but csproj says `Scenario06`, README says "Scenario 06", and paths in README reference `scenario-06-realtime-console`

#### Scenario 02 — API + SignalR (`src/samples/scenario-02-api/`)
- **What it does:** ASP.NET Core API with REST endpoint (`POST /api/conversation/turn`) and SignalR hub (`/hubs/conversation`) for streaming conversations
- **Status:** Complete backend. No frontend/client included — you'd need to build or curl against it.
- **Key files:** `Program.cs`, `ConversationHub.cs`, `scenario-02-api.csproj`
- **Hub methods:** `ProcessTurn(base64)` for one-shot, `StreamConversation(IAsyncEnumerable<byte[]>)` for streaming
- **Naming inconsistency:** Folder is `scenario-02-api` but csproj says `Scenario07`, README says "Scenario 07", paths reference `scenario-07-realtime-api`

#### Scenario 03 — Blazor + Aspire (`src/samples/scenario-03-blazor-aspire/`)
- **What it does:** Full-stack app: Aspire orchestrator + API backend (SignalR + Ollama) + Blazor Server frontend with chat UI, push-to-talk, and Speak Mode
- **Status:** Most complete sample. Builds clean. Has its own `.slnx` (not in main solution). Needs Ollama + .NET Aspire workload.
- **Key files:**
  - `scenario-04.AppHost/Program.cs` — Aspire orchestrator
  - `scenario-04.Api/Program.cs` — Backend with SignalR, M.E.AI, Whisper
  - `scenario-04.Api/Hubs/ConversationHub.cs` — Hub with text chat, audio processing, agent query
  - `scenario-04.Api/Services/ConversationService.cs` — Multi-turn streaming chat with history
  - `scenario-04.Web/Components/Pages/Conversation.razor` — Full chat UI component
  - `scenario-04.Web/wwwroot/js/audio-recorder.js` — Browser voice (Web Speech API)
  - `scenario-04.Web/wwwroot/css/app.css` — Dark-themed conversation UI
  - `scenario-04.Shared/Models/` — DTOs (AudioChunkDto, ChatMessageDto, ConversationStateDto)
- **Naming inconsistency:** Folder is `scenario-03-blazor-aspire` but all inner projects are `scenario-04.*`

#### Frontend Patterns Found
- **Browser STT:** Uses Web Speech API (`SpeechRecognition`), NOT WebRTC/MediaStream for audio capture. Client-side STT only.
- **Browser TTS:** Uses `SpeechSynthesis` API for auto-speak responses
- **SignalR:** MessagePack protocol, auto-reconnect, streaming via `StreamAsync<string>`
- **Voice modes:** Push-to-talk (single utterance) and Speak Mode (always-on, hands-free)
- **JS interop:** `window.voiceChat` object in `audio-recorder.js` with `start`, `startSpeakMode`, `speak`, etc.
- **No raw audio streaming to server yet:** Audio is transcribed client-side via Web Speech API, text sent over SignalR. Server-side `ProcessAudio()` hub method exists but is secondary path.

#### Core Library (src/ElBruno.Realtime/)
- `IRealtimeConversationClient` — Main interface with `ConverseAsync()` (streaming) and `ProcessTurnAsync()` (one-shot)
- `RealtimeConversationPipeline` — Default impl chaining VAD → STT → LLM → TTS
- `RealtimeServiceCollectionExtensions` — `AddPersonaPlexRealtime()` fluent builder
- Multi-target: net8.0 + net10.0

#### AudioChunkDto Note
- `AudioChunkDto` defaults to 24kHz sample rate, but project docs say 16kHz 16-bit mono PCM. Mismatch to track.

### 2026-02-27: Canvas Game Engine MVP

Implemented the Phase 1 side-scroller as a client-side HTML5 Canvas engine with keyboard input, Web Speech API commands, and Blazor HUD callbacks in scenario-04.Web.

**Work:** Built `game-engine.js` (~250 lines), `Game.razor` page component, updated `NavMenu.razor` with `/game` link. All features delivered: physics, collisions, procedural world generation, 60 FPS target. Integrated with `GameHub` SignalR hub for milestone feedback.

**Outcome:** ✅ Build clean (0 errors, 0 warnings). Ready for Phase 2 (server-side collision refinement).

### 2026-02-27: Game Build Verification

Re-verified the full scenario-03-blazor-aspire solution build. Result: **✅ Build succeeded — 0 errors, 0 warnings** across all 5 projects (AppHost, Api, Web, Shared, ServiceDefaults) plus core libraries.

**File review summary:**
- `game-engine.js` (620 lines) — Full Canvas side-scroller with physics (gravity, collisions), procedural generation (rocks, holes, enemies), keyboard + Web Speech API voice commands, projectile system, invincibility frames. Uses ES module exports for Blazor JS interop. Well-structured.
- `Game.razor` (468 lines) — Complete Blazor Server page: canvas display, HUD overlay (score/lives/voice status), game-over card, voice control panel, sidebar instructions. `[JSInvokable]` callbacks for score/lives/events/game-over/voice. SignalR connection to `/hubs/game` with MessagePack. Proper `IAsyncDisposable`.
- `GameHub.cs` (56 lines) — SignalR hub with `GetFeedback`, `GetMilestoneFeedback`, `ClassifyVoiceCommand` (LLM-powered). Clean DI injection.
- `GameFeedbackService.cs` (65 lines) — Thread-safe phrase selection via `RandomNumberGenerator.GetInt32`. LLM milestone feedback only on 500-point multiples. Proper cost control.
- `GameStateDto/GameEventDto/GameInputDto` — Clean record DTOs in Shared project.
- `Program.cs` — Both `GameFeedbackService` (DI) and `GameHub` (`/hubs/game` mapping) properly registered.
- `NavMenu.razor` — `/game` link present.

**No issues found.** All game files are properly integrated, DI is wired, hub is mapped, and the solution compiles cleanly.

### 2026-02-27: Phase 2 + Phase 4 — Game File Move & Landing Pages

Executed Ripley's architecture plan Phases 2 and 4:

**Phase 2 — Moved Game Files:**
- Copied `Game.razor`, `game-engine.js`, `app.css` from `scenario-04.Web` → `scenario-04.Game`
- Deleted `Game.razor` and `game-engine.js` from `scenario-04.Web`
- Removed 🎮 Game link from Web's `NavMenu.razor` (now only has 💬 Conversation)
- No namespace changes needed — `Game.razor` had no `@using Scenario04.Web` references

**Phase 4 — Landing Pages:**
- Updated `scenario-04.Web/Components/Pages/Index.razor` — voice-chat-focused landing with link to `/conversation` and features list (Speak Mode, Push-to-talk, Text chat, Auto-speak)
- Created `scenario-04.Game/Components/Pages/Index.razor` — game-focused landing with link to `/game` and features list (Side-Scroller, Voice Control, Keyboard controls, Voice Feedback)

**Build:** ✅ 0 errors, 0 warnings across all 6 projects (AppHost, Api, Web, Game, Shared, ServiceDefaults)

### 2026-02-27: Aspire Restructure Complete — Phases 2 & 4 Finalized

**Lambert's Phases 2 & 4 are finalized.** Game files moved, landing pages created, Web cleaned up. All builds clean per orchestration log 2026-02-27T17:42.

**Cross-team:** Ripley designed, Dallas executed Phase 1 + Phase 3 (scaffold + AppHost), Kane running Phase 5 smoke test (background), Parker updating docs (background).

### 2025-02-28: Radical Game Overhaul — 7 Visual & Gameplay Improvements

Implemented comprehensive enhancements to the side-scroller game at Bruno's request. All 7 improvements delivered:

**1. Scrolling Ground Markers:** Added visual grass tufts, pebbles, and dashes that scroll with world offset to show ground movement. Implemented `groundMarkers` array with 3 marker types (grass, pebble, dash) that regenerate as they scroll off-screen.

**2. Reasonable Hole Sizes:** Reduced hole width from `randomRange(70, 130)` to `randomRange(50, 90)` for jumpable challenge. With jump velocity -12 and gravity 0.5, all holes are now clearable with proper timing.

**3. Stomp Mechanic:** Implemented Mario-style enemy/obstacle stomping. When player falls onto top of enemy (vy > 0, collision from above), enemy is destroyed, player gets +25 points, small bounce (-8 vy), and particle explosion. For obstacles, player lands on top temporarily until it scrolls away.

**4. Speak Final Score:** Added TTS call in `loseLife()` when lives reach 0: `speakText(\`Game over! Your final score is ${state.score} points.\`)`. Integrates with existing Web Speech Synthesis API.

**5. Detailed Canvas Rendering:** Complete visual overhaul from colored rectangles to detailed pixel art:
   - **Player:** Character with body, circular head, eyes, hat, animated legs (walking cycle), arms
   - **Rocks:** Irregular polygon shapes with cracks, shading, texture lines
   - **Enemies:** Body with eyes, pupils, spiky horns, bounce animation
   - **Ground:** Grass strip on top (green), dirt layers below (brown), cross-section view
   - **Sky:** Gradient from purple `#2c1654` to `#7c3c8c`, sun/moon, parallax clouds with multi-ellipse rendering
   - **Projectiles:** Pulsing glowing orbs with shadow blur

**6. Show Speech & Actions:** Added on-screen text display for voice commands and TTS speech:
   - Speech bubble near player showing last TTS text (fades after 3s)
   - Action text at top-center ("ACTION: JUMP", "ACTION: SHOOT") on voice command (fades after 2s)
   - Voice command log in Game.razor showing last 5 commands with point values
   - `showSpeechText()` and `showActionText()` functions with timers

**7. Radical Aesthetic Redesign:** Complete theme overhaul to polished indie game:
   - **Palette:** Sunset/twilight theme — purple/pink sky, vibrant player (#FF6B9D), enemies (#9D4EDD), golden projectiles (#FFD60A)
   - **Particles:** Jump dust, shoot muzzle flash, enemy death explosions (15 particles), player hit burst
   - **Floating Score Text:** +10, +25 score popups that rise and fade
   - **Screen Shake:** On player hit (1.0 intensity, decays over time)
   - **Game.razor CSS:** Dark gradient background, glowing borders, animated buttons with pulse, gradient cards, backdrop blur effects, game-over screen with slide-up animation
   - **HUD:** Redesigned with glow effects, better borders, integrated look

**Technical Details:**
- All changes in 2 files: `game-engine.js` (620 → 820+ lines) and `Game.razor` (468 → 490+ lines)
- Performance maintained at 60 FPS target with particle systems and gradients
- All ES module exports intact for Blazor JS interop
- All `dotNetRef` callbacks preserved: OnScoreChanged, OnLivesChanged, OnGameEvent, OnGameOver, OnVoiceStatusChanged, OnVoiceCommand, OnVoiceError
- Build: ✅ 0 errors, 0 warnings

**Key Patterns:**
- **Particle System:** Array of objects with x, y, vx, vy, life, maxLife, alpha, color. Updated in game loop with gravity.
- **Floating Text:** Similar to particles but rise upward, fade based on life/maxLife ratio
- **Drawing Functions:** `drawPlayer()`, `drawRock()`, `drawEnemy()`, `drawProjectile()` for detailed rendering
- **Clouds:** Parallax scrolling at 0.2x world speed, multi-ellipse composition, wrap-around regeneration
- **Ground Markers:** Procedurally generated, 3 types, scroll with world, regenerate when < 50 on screen

**File Paths:**
- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/wwwroot/js/game-engine.js`
- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/Components/Pages/Game.razor`

