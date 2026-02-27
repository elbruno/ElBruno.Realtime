# Orchestration Log: 2026-02-27T17:42 — Aspire Restructure & Game Split

**Timestamp:** 2026-02-27T17:42:00Z  
**Coordinator:** Scribe (📋 Session Logger)  
**Scope:** Aspire AppHost + Blazor Frontend Split (Web → Web + Game)  

---

## Spawn Manifest

### Agent: Ripley (Lead)
- **Role:** Architected Aspire restructure plan
- **Outcome:** Split scenario-04.Web into Web (voice chat) + Game (side-scroller), both backed by same API
- **Deliverable:** Plan written to `.squad/decisions/inbox/ripley-aspire-restructure.md`
- **Mode:** Sync

### Agent: Dallas (Backend)
- **Role:** C# / Backend Developer
- **Scope:**
  1. Created `scenario-04.Game` project scaffold (csproj, Program.cs, Blazor boilerplate)
  2. Updated AppHost to register 3 services (api, web, game)
  3. Updated solution file (`scenario-04-blazor-aspire.slnx`)
- **Outcome:** All 6 projects build clean (0 errors, 0 warnings)
- **Mode:** Sync

### Agent: Lambert (Frontend)
- **Role:** Frontend / JavaScript Developer
- **Scope:**
  1. Moved Game.razor + game-engine.js + app.css from scenario-04.Web to scenario-04.Game
  2. Deleted game-related files from scenario-04.Web
  3. Updated NavMenu (removed game link from Web)
  4. Created focused landing pages for both frontends
- **Outcome:** Build clean, 0 errors, 0 warnings
- **Mode:** Sync

### Agent: Kane (Tester)
- **Role:** QA / Verification
- **Scope:** Verifying full build + file presence + test suite
- **Status:** 🔄 In Progress
- **Mode:** Background

### Agent: Parker (DevOps)
- **Role:** Documentation & DevOps
- **Scope:** Updating README docs
- **Status:** 🔄 In Progress
- **Mode:** Background

---

## Phase Completion Status

| Phase | Agent | Description | Status |
|-------|-------|-------------|--------|
| 1 | Dallas | Create `scenario-04.Game` project scaffold | ✅ Done |
| 2 | Lambert | Move Game.razor + game-engine.js to new project, update Web | ✅ Done |
| 3 | Dallas | Update AppHost + solution file | ✅ Done |
| 4 | Lambert | Create landing pages for each frontend | ✅ Done |
| 5 | Kane | Verify both projects build; manual smoke test checklist | 🔄 In Progress |
| 6 | Parker | Update README.md and docs | 🔄 In Progress |

---

## File Changes Summary

### New Files Created: 11
- `scenario-04.Game/scenario-04.Game.csproj`
- `scenario-04.Game/Program.cs`
- `scenario-04.Game/appsettings.json`
- `scenario-04.Game/Properties/launchSettings.json`
- `scenario-04.Game/Components/App.razor`
- `scenario-04.Game/Components/Routes.razor`
- `scenario-04.Game/Components/_Imports.razor`
- `scenario-04.Game/Components/Layout/MainLayout.razor`
- `scenario-04.Game/Components/Layout/NavMenu.razor`
- `scenario-04.Game/Components/Pages/Index.razor`
- `scenario-04.Game/wwwroot/css/app.css` (copied)

### Files Moved: 2
- `scenario-04.Web/Components/Pages/Game.razor` → `scenario-04.Game/Components/Pages/Game.razor`
- `scenario-04.Web/wwwroot/js/game-engine.js` → `scenario-04.Game/wwwroot/js/game-engine.js`

### Files Modified: 4
- `scenario-04.Web/Components/Pages/Index.razor` — Removed game references, voice-chat focused
- `scenario-04.Web/Components/Layout/NavMenu.razor` — Removed Game link
- `scenario-04.AppHost/scenario-04.AppHost.csproj` — Added Game ProjectReference
- `scenario-04.AppHost/Program.cs` — Registered game frontend service
- `scenario-04-blazor-aspire.slnx` — Added Game project

### Build Verification: ✅
- All 6 projects compile cleanly
- 0 errors, 0 warnings (net8.0 + net10.0)
- Solution: `scenario-04-blazor-aspire.slnx`

---

## Architecture After Restructure

```
┌─────────────────────────────────────────────────────────────┐
│                  .NET Aspire AppHost                         │
│         (Orchestration · Dashboard · Telemetry)              │
└──┬──────────────────┬──────────────────┬───────────────────┘
   │                  │                  │
   ▼                  ▼                  ▼
┌──────────────┐  ┌───────────────┐  ┌──────────────┐
│  Blazor Web  │  │   API Backend │  │ Blazor Game  │
│  (Voice Chat)│  │  (ASP.NET     │  │ (Side-       │
│              │  │   Core)       │  │  Scroller)   │
│  🗣️ Speak   │  │  SignalR Hubs │  │  🎮 Canvas   │
│  🎤 Push-to- │  │  M.E.AI       │  │  🎤 Voice    │
│  talk        │  │  Ollama       │  │  Commands    │
└──────┬───────┘  └──────┬────────┘  └──────┬───────┘
       │     /hubs/      │     /hubs/        │
       │   conversation  │      game         │
       └────────────────►│◄─────────────────┘
```

---

## Key Decisions Ratified

1. **Canvas rendering for game** (60 FPS, responsive)
2. **Client-side voice command spotting** (Web Speech API, <200ms latency)
3. **Two-tier voice feedback** (instant phrases + LLM milestones)
4. **Aspire service discovery** (both frontends connect via `WithReference(api)`)
5. **Separate landing pages** (Web at `/` for chat, Game at `/` for game)
6. **Per-project CSS** (each frontend has independent `app.css`)

---

## Next Steps

- **Kane:** Smoke test — verify Aspire dashboard shows 3 services, both frontends load, connectivity works
- **Parker:** Update main README with new architecture diagram
- **Scribe:** Merge inbox decisions → `decisions.md`, commit `.squad/` state

---

*Logged by Scribe · 2026-02-27T17:42:00Z*
