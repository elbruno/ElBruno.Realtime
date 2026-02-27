# Decision: Game Visual & Aesthetic Architecture

**Date:** 2025-02-28  
**Author:** Lambert (Frontend Developer)  
**Status:** Implemented  
**Scope:** scenario-04.Game

## Context

Bruno requested a comprehensive overhaul of the side-scroller game with 7 improvements spanning visual rendering, gameplay mechanics, and aesthetic design. The game needed to transform from a prototype with colored rectangles to a polished indie game experience while maintaining 60 FPS performance.

## Decision

### Visual Rendering Architecture

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

### Particle System Design

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

### Color Palette Selection

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

### Screen Shake Implementation

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

### Parallax Cloud System

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

### CSS Animation Strategy

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

### Text Display Architecture

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

## Impact

### Performance
- Maintained 60 FPS with all effects enabled
- Particle count capped naturally by short lifetimes
- Drawing functions called once per entity per frame

### Maintainability
- Modular draw functions easy to update
- Color palette in constants object
- Particle/text systems use same pattern

### User Experience
- Game looks polished and complete
- Visual feedback for all actions
- Clear communication of voice commands
- No confusion about entity types

## Team Relevance

**For Future Game Features:**
- Particle system can handle power-ups, item pickups, score multipliers
- Draw functions can be extended with more detail or animation frames
- Color palette can shift dynamically (day/night cycle)
- Text system can show level names, combo counters, achievements

**For Other Blazor Components:**
- Canvas rendering pattern reusable for charts, visualizations
- CSS animation patterns apply to any Blazor UI
- Voice command display pattern useful for other voice-enabled components

## Files Modified

- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/wwwroot/js/game-engine.js`
- `src/samples/scenario-03-blazor-aspire/scenario-04.Game/Components/Pages/Game.razor`

## References

- [HTML5 Canvas Tutorial](https://developer.mozilla.org/en-US/docs/Web/API/Canvas_API/Tutorial)
- [Juice it or lose it](https://www.youtube.com/watch?v=Fy0aCDmgnxg) — Game feel reference
- Web Speech API integration maintained from previous implementation
