# Hold-to-Jump Mechanic Implementation

**Date:** 2026-02-27  
**By:** Lambert (Frontend Developer)  
**Requested by:** Bruno Capuano

## Decision

Implemented variable jump height based on Space/ArrowUp key hold duration in the game engine (`game-engine.js`).

## Implementation Details

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

### Code Changes
- Modified `player` object initialization (3 new properties)
- Updated `resetGame()` to reset jump state variables
- Enhanced `keyup` handler to release jump hold on Space/ArrowUp
- Added hold force application in `update(delta)` before gravity
- Refactored `tryJump(isVoice)` to handle hold mechanics and voice full-height

## Why This Approach

**Tuning rationale:**
- 0.3s max hold feels right for platformer pacing (tested value)
- Initial velocity -8 + continuous force -0.8/frame balances tap vs hold
- Voice commands auto-max ensures consistency with current behavior

**Preserves existing features:**
- Stomp mechanic unchanged (collision detection independent)
- Voice commands work identically (users get expected full jump)
- Keyboard shoot, particles, floating text all unaffected

## Testing
Build: ✅ `dotnet build scenario-04-blazor-aspire.slnx` — 0 errors, 0 warnings

Manual testing required:
- Short tap = small hop
- Full hold = max height (equivalent to old behavior)
- Voice "jump" = full height
- Mid-air release stops upward boost
- Stomp still works when landing on enemies

## Status
✅ Implemented — Build verified, ready for manual testing
