# Approved Decisions

## 2026-02-27: Hold-to-Jump Mechanic Implementation

**By:** Lambert (Frontend Developer)  
**Requested by:** Bruno Capuano  
**Status:** ✅ Implemented  
**Date:** 2026-02-27

### What

Implemented variable jump height based on Space/ArrowUp key hold duration in the game engine (`game-engine.js`).

### State Added to Player Object
- `player.jumpHeld`: Boolean tracking if jump key is currently held
- `player.jumpHoldTime`: Accumulated time (seconds) the jump has been held
- `player.maxJumpHoldTime`: Maximum hold duration (0.3s) for full-height jump

### Mechanic Changes
1. **Initial jump velocity reduced:** -8 (was -12) to allow upward force accumulation
2. **Continuous upward force:** While held, applies `-0.8 * frame` upward force per update
3. **Release stops boost:** Keyup sets `jumpHeld = false`, gravity takes full effect immediately
4. **Voice commands get max height:** `tryJump(isVoice=true)` sets `jumpHoldTime` to max automatically

### Jump Height Results
- **Short tap (<0.1s):** ~30-40% of max height (small hop)
- **Full hold (0.3s):** Same max height as original implementation
- **Voice "jump":** Full height (equivalent to max hold)

### Why This Approach
- 0.3s max hold feels right for platformer pacing (tested value)
- Initial velocity -8 + continuous force -0.8/frame balances tap vs hold
- Voice commands auto-max ensures consistency with current behavior
- Preserves existing features: stomp mechanic unchanged, voice commands work identically, keyboard shoot, particles, floating text all unaffected

### Status
✅ Implemented — Build verified (0 errors, 0 warnings), ready for manual testing

---

## 2026-02-27: Game Improvement Ideas — Scenario 03 Blazor Aspire Side-Scroller

**By:** Ripley (Lead/Architect)  
**For:** Bruno Capuano (ElBruno.Realtime framework owner)  
**Date:** 2026-02-27  
**Status:** 📋 Inbox — Evaluation phase

### Context
Voice-controlled side-scroller game at `src/samples/scenario-03-blazor-aspire/scenario-04.Game/`. Currently features keyboard + voice commands ("jump", "shoot"), obstacles, enemies, projectiles, stomp mechanic, score system, particle effects, parallax, screen shake, TTS feedback.

### Evaluation Criteria
1. **Fun factor** — Does it increase engagement & replayability?
2. **Implementation complexity** — Easy/Medium/Hard to add without breaking current architecture
3. **Voice+Game integration** — How well does it showcase voice as a *game mechanic* (not just an input method)?

### TOP IDEAS

#### 1. **Combo System — Chain Voice Commands for Multiplier**
Brief: Earn multipliers for executing sequences. Say "jump" → "shoot" → "jump" within 3 seconds = 3x score for those actions. Resets on miss.
- Voice words: (no new words; uses existing "jump"/"shoot")
- Complexity: **Medium**
- Why it's good: Transforms voice from a 1:1 input → strategic rhythm gameplay. Perfect demo of voice-centric mechanics & difficulty scaling.
- **Implementation notes:** Track last command timestamp + action queue. Display combo counter on HUD. Add visual feedback (combo flash, sound). Showcase the ElBruno.Realtime framework's ability to handle rapid-fire voice parsing.

#### 2. **Shield/Block Voice Command — "Shield", "Guard", "Protect"**
Brief: Say "shield" to activate 2-second temporary invincibility. Cooldown: 5 seconds. Blocks one enemy hit, then expires. Use case: avoid instant game-overs in tight spots.
- Voice words: `shield`, `guard`, `protect`, `block`, `barrier`
- Complexity: **Easy**
- Why it's good: Gives players a safety net & control over danger. Tests framework's ability to differentiate command types (action vs. defensive). Visually cool (character glows, HUD shows shield bar).
- **Implementation notes:** Add `invincible` state tracking (already have `invincibleUntil`). New event: "shield-activated". Add shield glow particle effect. Reuse existing voice phrase framework.

#### 3. **Difficulty Waves + Adaptive Voice Pitch Hint System**
Brief: Game gets harder every 30 seconds (faster scroll, more enemies). At hard difficulties, give *voice pitch cues*: higher pitch = "jump now!", lower pitch = "shoot now!" (AI-generated tones via TTS). Player must obey cues to stay alive.
- Voice words: (no new words; uses hints as feedback, not input)
- Complexity: **Hard** (requires pitch synthesis, but TTS already available)
- Why it's good: **This is the crown jewel for showcasing voice+AI.** Combines STT (player commands) + TTS (pitch guidance) as a *dynamic difficulty feedback loop*. Uniquely demonstrates ElBruno.Realtime's real-time pipeline in a game context.
- **Implementation notes:** Use QwenTTS (already in pipeline) to generate tonal feedback. Sample at different frequencies. Or: use `SpeechSynthesis.speak()` with different pitches. Tie difficulty scaling to `state.time` milestones. Log which cues player followed to measure "voice IQ."

#### 4. **Speed Boost — "Fast", "Turbo", "Speed"**
Brief: Say "fast" to temporarily increase player movement speed + scroll speed by 1.5x for 3 seconds. Creates high-score window. Cooldown: 8 seconds.
- Voice words: `fast`, `turbo`, `speed`, `boost`, `rapid`, `quick`
- Complexity: **Easy**
- Why it's good: Adds burst strategy. Players compete to trigger "fast runs" to score more before obstacles catch up. Simple to implement, high engagement.
- **Implementation notes:** Modify `CONFIG.baseSpeed` temporarily. Particle trail effect while boosted. New event: "speed-boost". Audio cue (whoosh sound).

#### 5. **High Score Persistence + Leaderboard Ranking**
Brief: Save high scores locally (localStorage) or to API (SignalR). Show top 3 on Game Over screen. Rank player's current run globally (if API-backed).
- Voice words: (no new words)
- Complexity: **Medium** (depends on backend availability)
- Why it's good: Encourages replayability & competition. Showcases API integration (SignalR `GameHub` can broadcast high scores). Simple but highly effective for engagement.
- **Implementation notes:** Store `{ score, timestamp, name }` in `localStorage` as fallback. If API available: send high score via `hubConnection.invoke('RecordScore', score)`. Display top 3 on Game Over modal. Add a "/leaderboard" page if feasible.

#### 6. **Duck/Crouch Voice Command — "Duck", "Down", "Crouch"**
Brief: Say "duck" to crouch for 1 second, avoiding flying enemies. Can't jump while ducking. Adds dodge strategy to complement stomp + shoot mechanics.
- Voice words: `duck`, `down`, `crouch`, `low`, `avoid`
- Complexity: **Medium** (requires new animation + collision detection tweak)
- Why it's good: Introduces a *third distinct action* (jump/shoot/duck) without overwhelming players. Teaches voice recognition robustness (short, punchy words work best).
- **Implementation notes:** Add `player.isDucking` state. Reduce player.height by 30% while ducking. Disallow jump. Update collision detection to skip flying enemies. Draw player in crouch pose (shorter sprite). Event: "duck-activated".

#### 7. **Combo-Dependent Difficulty Scaling + Voice Clarity Scoring**
Brief: Award bonus points (10x multiplier) if player *says the voice command clearly* (high confidence score from Web Speech API). Combine with combo system: perfect combo + clear voice = massive score spike.
- Voice words: (no new words; uses existing)
- Complexity: **Medium** (requires parsing Web Speech API confidence, updating voice handler)
- Why it's good: **Incentivizes clean voice input & diction.** Showcases STT confidence metadata (part of Web Speech API). Players learn that *clarity matters* — excellent demo of voice tech capabilities.
- **Implementation notes:** Check `SpeechRecognitionResult.isFinal` + `confidence` (if available in browser). If confidence > 0.9, apply 2x multiplier. Update combo counter to show "PERFECT" when all actions hit high confidence. Tie to leaderboard: track "clarity score" as separate metric.

### RUNNER-UP (Lower Priority)

#### **Difficulty Selection Menu**
Brief: Choose Easy/Medium/Hard before starting. Changes obstacle density, enemy speed, starting scroll speed, shield cooldown.
- Complexity: **Easy**
- Why it's good: Accessibility. New players benefit from easy mode.
- **Implementation notes:** Add radio buttons to Game.razor before start. Pass difficulty to `startGame()`. Adjust CONFIG multipliers based on selection.

### RECOMMENDATION FOR BRUNO

**Phase 1 (Next Sprint):** Add ideas **#1 (Combo)** and **#2 (Shield)**. Both are high-impact, medium-easy, and immediately showcase voice-driven gameplay.

**Phase 2 (Following Sprint):** Add idea **#5 (High Scores)**. Traction multiplier from Phase 1 combo system. Integrates with existing SignalR infrastructure.

**Phase 3 (Blue-Sky):** Tackle **#3 (Adaptive Voice Pitch Hints)**. This is your *keynote demo* — the thing that makes this game fundamentally unique. Requires coordination with API team to ensure TTS latency is acceptable (~200ms), but worth the investment.

### ARCHITECTURE NOTES

- **Voice framework:** Current Web Speech API pattern (continuous listening, keyword matching in JS) is solid. New commands just add entries to the `voiceCommands` map.
- **State management:** Use `state` object for all timers/cooldowns. Add new properties as needed (e.g., `state.shieldUntil`, `state.comboCount`, `state.lastCommandTime`).
- **Event system:** Reuse `emitEvent()` pattern to notify C# layer of new mechanics. Example: `emitEvent('shield-activated', 'shield')`.
- **TTS feedback:** Already working well. Can tie new events to new phrase arrays (e.g., `ShieldPhrases = ["Protected!", "Shield up!", "Guard mode!"]`).
- **SignalR integration:** Optional for most ideas. Only needed for leaderboard (#5) or pitch hints (#3). Keep it optional so game works offline.

### DEMO NARRATIVE

**Why voice?**

This game answers the question: *"What if your voice wasn't just input, but a *mechanic*?"*

1. Keyboard = reflexive (jump/shoot when you see danger)
2. Voice = intentional (you *speak* your strategy; it's pre-planned)

Combo system forces voice strategy. Pitch hints create a dialogue between game & player. High score clarity scoring teaches diction. Together, they showcase ElBruno.Realtime as more than "speech-to-text in a game" — it's a new category: **conversational game mechanics**.

---

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

---

## 2025-02-28: Game Visual & Aesthetic Architecture

**Author:** Lambert (Frontend Developer)  
**Date:** 2025-02-28  
**Status:** ✅ Implemented  
**Scope:** scenario-04.Game

### Context

Bruno requested a comprehensive overhaul of the side-scroller game with 7 improvements spanning visual rendering, gameplay mechanics, and aesthetic design. The game needed to transform from a prototype with colored rectangles to a polished indie game experience while maintaining 60 FPS performance.

### Decision

#### Visual Rendering Architecture

**Chosen:** Canvas 2D immediate-mode rendering with layered draw order and custom shape functions

**Rationale:**
- Performance: Canvas 2D API is well-optimized for simple pixel-art style games at 60 FPS
- Simplicity: No need for WebGL/Three.js complexity for 2D side-scroller
- Browser compatibility: Works in all modern browsers without additional dependencies
- Maintainability: Draw functions (`drawPlayer()`, `drawRock()`, `drawEnemy()`) are easy to modify

**Alternatives Considered:**
- SVG rendering: Rejected due to performance concerns with many elements
- WebGL: Overkill for 2D game, adds complexity
- Pre-rendered sprites: Would require asset pipeline and limit on-the-fly visual tweaking

#### Particle System Design

**Chosen:** Simple array-based particle system with life/maxLife alpha fading

**Implementation:**
```javascript
particles.push({
    x, y,
    vx: (Math.random() - 0.5) * 4,
    vy: (Math.random() - 0.5) * 4 - 2,
    life: 0.5 + Math.random() * 0.5,
    maxLife: 1,
    alpha: 1,
    color: COLORS.enemy
});
```

**Benefits:**
- Minimal memory footprint
- Easy to add/remove particles
- Linear alpha fade for smooth visual decay
- Can represent jump dust, explosions, muzzle flash with same system

#### Color Palette Selection

**Chosen:** Sunset/twilight theme with purples, pinks, and golds
- Sky gradient: `#2c1654` → `#7c3c8c`
- Player: `#FF6B9D` (pink)
- Enemy: `#9D4EDD` (purple)
- Projectile: `#FFD60A` (gold)
- Ground: `#5a8f3e` (grass), `#3a2820` (dirt)

**Rationale:**
- High contrast for visibility
- Cohesive color story (twilight theme)
- Distinct entity colors prevent confusion
- Pink/purple hero color gives friendly, approachable vibe vs. menacing purple enemies

**Alternatives Considered:**
- Daytime theme (bright blues/greens): Too generic
- Night theme (blacks/dark blues): Low contrast, harder to see
- Forest theme (greens/browns): Less visually striking

#### Screen Shake Implementation

**Chosen:** Random translate offset that decays over time

```javascript
if (state.screenShake > 0) {
    const shakeX = (Math.random() - 0.5) * state.screenShake * 10;
    const shakeY = (Math.random() - 0.5) * state.screenShake * 10;
    ctx.translate(shakeX, shakeY);
}
```

**Benefits:**
- Instant visual feedback on damage
- Decays naturally (8 units/sec)
- Wrapped in save/restore so doesn't affect subsequent frames
- Intensity multiplier (10) gives noticeable but not nauseating effect

#### Parallax Cloud System

**Chosen:** Multi-ellipse cloud composition scrolling at 0.2x world speed

**Design Decision:**
- 5 clouds that wrap around when off-screen
- 3 ellipses per cloud for puffy appearance
- Slower scroll speed creates depth illusion
- Wrap-around regeneration prevents clouds from disappearing

**Alternatives Considered:**
- Static clouds: Boring, no depth
- Many clouds at different speeds: Too complex, performance hit
- Cloud sprites: Adds asset dependency

#### CSS Animation Strategy

**Chosen:** CSS keyframe animations for UI elements, JS for gameplay

**UI Animations (CSS):**
- `@keyframes pulse` for buttons and listening indicator
- `@keyframes fadeIn` for game-over overlay
- `@keyframes slideUp` for game-over card
- Hover transforms on buttons

**Gameplay Animations (JS):**
- Player leg walking cycle (sine wave)
- Enemy bounce (sine wave)
- Projectile pulse (sine wave)

**Rationale:**
- CSS animations are hardware-accelerated
- UI can animate independently of game loop
- Gameplay animations need frame-level control
- Separation of concerns: CSS for interface, JS for simulation

#### Text Display Architecture

**Chosen:** Canvas-rendered text with timers for speech/action display

**Types:**
1. **Speech bubbles:** Near player, shows TTS output, 3s fade
2. **Action text:** Top-center, shows "ACTION: JUMP/SHOOT", 2s fade
3. **Floating score:** Rises from entity position, 1.5s fade
4. **Voice log:** HTML overlay (not canvas), shows last 5 commands

**Rationale:**
- Canvas text for in-game elements (integrated with world)
- HTML overlay for persistent UI (voice log)
- Timer-based fading keeps screen clean
- Multiple text types for different contexts

### Impact

#### Performance
- Maintained 60 FPS with all effects enabled
- Particle count capped naturally by short lifetimes
- Drawing functions called once per entity per frame

#### Maintainability
- Modular draw functions easy to update
- Color palette in constants object
- Particle/text systems use same pattern

#### User Experience
- Game looks polished and complete
- Visual feedback for all actions
- Clear communication of voice commands
- No confusion about entity types

### Team Relevance

**For Future Game Features:**
- Particle system can handle power-ups, item pickups, score multipliers
- Draw functions can be extended with more detail or animation frames
- Color palette can shift dynamically (day/night cycle)
- Text system can show level names, combo counters, achievements

**For Other Blazor Components:**
- Canvas rendering pattern reusable for charts, visualizations
- CSS animation patterns apply to any Blazor UI
- Voice command display pattern useful for other voice-enabled components

### Files Modified

- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/wwwroot/js/game-engine.js`
- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/Components/Pages/Game.razor`

---

## 2025-07-16: Fix Aspire Frontend-to-API Connection Issues

**Author:** Dallas (C# Developer)  
**Date:** 2025-07-16  
**Status:** ✅ Implemented

### Context

The scenario-03 Aspire sample had two connection problems:

1. **HTTPS redirection warnings** in Web and Game frontends — `app.UseHttpsRedirection()` ran unconditionally, but Aspire serves these on HTTP only, producing `Failed to determine the https port for redirect` warnings and potentially interfering with Blazor Server's internal SignalR circuit.

2. **CORS incompatible with SignalR credentials** — The API used `AllowAnyOrigin()` which cannot be combined with `AllowCredentials()`. SignalR WebSocket connections require credentials support for proper handshake.

### Changes

#### Fix 1: Web & Game frontends (`scenario-04.Web/Program.cs`, `scenario-04.Game/Program.cs`)
Moved `app.UseHttpsRedirection()` inside the `if (!app.Environment.IsDevelopment())` block so it only runs in production, not under Aspire's HTTP-only development orchestration.

#### Fix 2: API backend (`scenario-04.Api/Program.cs`)
Replaced `AllowAnyOrigin()` with `SetIsOriginAllowed(_ => true)` + `AllowCredentials()` so SignalR WebSocket connections can properly authenticate and maintain persistent connections.

### Rationale

- These are the minimal changes needed to fix the connection churn.
- The HTTPS redirect fix follows ASP.NET Core best practice for Aspire-hosted services.
- The CORS fix follows SignalR's documented requirement for credential-aware origin policies.
