# Game Improvement Ideas — Scenario 03 Blazor Aspire Side-Scroller

**Context:** Voice-controlled side-scroller game at `src/samples/scenario-03-blazor-aspire/scenario-04.Game/`. Currently features keyboard + voice commands ("jump", "shoot"), obstacles, enemies, projectiles, stomp mechanic, score system, particle effects, parallax, screen shake, TTS feedback.

**Evaluation Criteria:**
1. **Fun factor** — Does it increase engagement & replayability?
2. **Implementation complexity** — Easy/Medium/Hard to add without breaking current architecture
3. **Voice+Game integration** — How well does it showcase voice as a *game mechanic* (not just an input method)?

---

## TOP IDEAS

### 1. **Combo System — Chain Voice Commands for Multiplier**
Brief: Earn multipliers for executing sequences. Say "jump" → "shoot" → "jump" within 3 seconds = 3x score for those actions. Resets on miss.

- Voice words: (no new words; uses existing "jump"/"shoot")
- Complexity: **Medium**
- Why it's good: Transforms voice from a 1:1 input → strategic rhythm gameplay. Perfect demo of voice-centric mechanics & difficulty scaling.
- **Implementation notes:** Track last command timestamp + action queue. Display combo counter on HUD. Add visual feedback (combo flash, sound). Showcase the ElBruno.Realtime framework's ability to handle rapid-fire voice parsing.

---

### 2. **Shield/Block Voice Command — "Shield", "Guard", "Protect"**
Brief: Say "shield" to activate 2-second temporary invincibility. Cooldown: 5 seconds. Blocks one enemy hit, then expires. Use case: avoid instant game-overs in tight spots.

- Voice words: `shield`, `guard`, `protect`, `block`, `barrier`
- Complexity: **Easy**
- Why it's good: Gives players a safety net & control over danger. Tests framework's ability to differentiate command types (action vs. defensive). Visually cool (character glows, HUD shows shield bar).
- **Implementation notes:** Add `invincible` state tracking (already have `invincibleUntil`). New event: "shield-activated". Add shield glow particle effect. Reuse existing voice phrase framework.

---

### 3. **Difficulty Waves + Adaptive Voice Pitch Hint System**
Brief: Game gets harder every 30 seconds (faster scroll, more enemies). At hard difficulties, give *voice pitch cues*: higher pitch = "jump now!", lower pitch = "shoot now!" (AI-generated tones via TTS). Player must obey cues to stay alive.

- Voice words: (no new words; uses hints as feedback, not input)
- Complexity: **Hard** (requires pitch synthesis, but TTS already available)
- Why it's good: **This is the crown jewel for showcasing voice+AI.** Combines STT (player commands) + TTS (pitch guidance) as a *dynamic difficulty feedback loop*. Uniquely demonstrates ElBruno.Realtime's real-time pipeline in a game context.
- **Implementation notes:** Use QwenTTS (already in pipeline) to generate tonal feedback. Sample at different frequencies. Or: use `SpeechSynthesis.speak()` with different pitches. Tie difficulty scaling to `state.time` milestones. Log which cues player followed to measure "voice IQ."

---

### 4. **Speed Boost — "Fast", "Turbo", "Speed"**
Brief: Say "fast" to temporarily increase player movement speed + scroll speed by 1.5x for 3 seconds. Creates high-score window. Cooldown: 8 seconds.

- Voice words: `fast`, `turbo`, `speed`, `boost`, `rapid`, `quick`
- Complexity: **Easy**
- Why it's good: Adds burst strategy. Players compete to trigger "fast runs" to score more before obstacles catch up. Simple to implement, high engagement.
- **Implementation notes:** Modify `CONFIG.baseSpeed` temporarily. Particle trail effect while boosted. New event: "speed-boost". Audio cue (whoosh sound).

---

### 5. **High Score Persistence + Leaderboard Ranking**
Brief: Save high scores locally (localStorage) or to API (SignalR). Show top 3 on Game Over screen. Rank player's current run globally (if API-backed).

- Voice words: (no new words)
- Complexity: **Medium** (depends on backend availability)
- Why it's good: Encourages replayability & competition. Showcases API integration (SignalR `GameHub` can broadcast high scores). Simple but highly effective for engagement.
- **Implementation notes:** Store `{ score, timestamp, name }` in `localStorage` as fallback. If API available: send high score via `hubConnection.invoke('RecordScore', score)`. Display top 3 on Game Over modal. Add a "/leaderboard" page if feasible.

---

### 6. **Duck/Crouch Voice Command — "Duck", "Down", "Crouch"**
Brief: Say "duck" to crouch for 1 second, avoiding flying enemies. Can't jump while ducking. Adds dodge strategy to complement stomp + shoot mechanics.

- Voice words: `duck`, `down`, `crouch`, `low`, `avoid`
- Complexity: **Medium** (requires new animation + collision detection tweak)
- Why it's good: Introduces a *third distinct action* (jump/shoot/duck) without overwhelming players. Teaches voice recognition robustness (short, punchy words work best).
- **Implementation notes:** Add `player.isDucking` state. Reduce player.height by 30% while ducking. Disallow jump. Update collision detection to skip flying enemies. Draw player in crouch pose (shorter sprite). Event: "duck-activated".

---

### 7. **Combo-Dependent Difficulty Scaling + Voice Clarity Scoring**
Brief: Award bonus points (10x multiplier) if player *says the voice command clearly* (high confidence score from Web Speech API). Combine with combo system: perfect combo + clear voice = massive score spike.

- Voice words: (no new words; uses existing)
- Complexity: **Medium** (requires parsing Web Speech API confidence, updating voice handler)
- Why it's good: **Incentivizes clean voice input & diction.** Showcases STT confidence metadata (part of Web Speech API). Players learn that *clarity matters* — excellent demo of voice tech capabilities.
- **Implementation notes:** Check `SpeechRecognitionResult.isFinal` + `confidence` (if available in browser). If confidence > 0.9, apply 2x multiplier. Update combo counter to show "PERFECT" when all actions hit high confidence. Tie to leaderboard: track "clarity score" as separate metric.

---

## RUNNER-UP (Lower Priority)

### **Difficulty Selection Menu**
Brief: Choose Easy/Medium/Hard before starting. Changes obstacle density, enemy speed, starting scroll speed, shield cooldown.

- Complexity: **Easy**
- Why it's good: Accessibility. New players benefit from easy mode.
- **Implementation notes:** Add radio buttons to Game.razor before start. Pass difficulty to `startGame()`. Adjust CONFIG multipliers based on selection.

---

## RECOMMENDATION FOR BRUNO

**Phase 1 (Next Sprint):** Add ideas **#1 (Combo)** and **#2 (Shield)**. Both are high-impact, medium-easy, and immediately showcase voice-driven gameplay.

**Phase 2 (Following Sprint):** Add idea **#5 (High Scores)**. Traction multiplier from Phase 1 combo system. Integrates with existing SignalR infrastructure.

**Phase 3 (Blue-Sky):** Tackle **#3 (Adaptive Voice Pitch Hints)**. This is your *keynote demo* — the thing that makes this game fundamentally unique. Requires coordination with API team to ensure TTS latency is acceptable (~200ms), but worth the investment.

---

## ARCHITECTURE NOTES

- **Voice framework:** Current Web Speech API pattern (continuous listening, keyword matching in JS) is solid. New commands just add entries to the `voiceCommands` map.
- **State management:** Use `state` object for all timers/cooldowns. Add new properties as needed (e.g., `state.shieldUntil`, `state.comboCount`, `state.lastCommandTime`).
- **Event system:** Reuse `emitEvent()` pattern to notify C# layer of new mechanics. Example: `emitEvent('shield-activated', 'shield')`.
- **TTS feedback:** Already working well. Can tie new events to new phrase arrays (e.g., `ShieldPhrases = ["Protected!", "Shield up!", "Guard mode!"]`).
- **SignalR integration:** Optional for most ideas. Only needed for leaderboard (#5) or pitch hints (#3). Keep it optional so game works offline.

---

## DEMO NARRATIVE

**Why voice?**

This game answers the question: *"What if your voice wasn't just input, but a *mechanic*?"*

1. Keyboard = reflexive (jump/shoot when you see danger)
2. Voice = intentional (you *speak* your strategy; it's pre-planned)

Combo system forces voice strategy. Pitch hints create a dialogue between game & player. High score clarity scoring teaches diction. Together, they showcase ElBruno.Realtime as more than "speech-to-text in a game" — it's a new category: **conversational game mechanics**.

---

**Created by:** Ripley (Lead/Architect)  
**For:** Bruno Capuano (ElBruno.Realtime framework owner)  
**Date:** 2026-02-27
