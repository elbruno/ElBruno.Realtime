# Repository Rule: Maintain the "What's New" Section in README.md

**Location:** `README.md` → `## What's New` section  
**Rule version:** 1.0  
**Scope:** All NuGet releases for the `ElBruno.Realtime` library family

---

## Rule

When publishing a **new NuGet release** (any version bump — patch, minor, or major), the AI agent responsible for the release **MUST**:

1. **Evaluate** whether the release includes a user-facing feature, fix, or meaningful change.
2. **If yes**, prepend a new row to the `## What's New` table in `README.md`:
   - `Version` column: the new version number (e.g., `0.12.0`)
   - `Feature` column: a concise, human-readable description of the most important change (bold the feature name)
3. **Trim** the table to the **last 5 rows** — drop the oldest row when adding a new one keeps the count at 6.
4. **Commit** the README update as part of the release commit (not a separate commit).

### What counts as a "user-facing" change?

✅ Include:
- New packages / NuGet IDs
- New Blazor components, abstractions, or extension methods
- New provider support (STT, TTS, VAD, LLM)
- Breaking API changes
- Significant bug fixes that affect behavior
- New sample applications or major documentation additions

❌ Skip:
- Internal refactoring with no API surface change
- CI/CD infrastructure changes
- Documentation-only fixes
- Dependency version bumps with no behavioral change

---

## What's New Table Format

The `## What's New` section must appear in `README.md` immediately after the badge row and before the `## Architecture` section. Format:

```markdown
## What's New

| Version | Feature |
|---------|---------|
| X.Y.Z | **Feature Name** — brief description of what was added or changed |
| ... | ... |
```

Maximum rows: **5** (most recent first).

---

## Example

After releasing `0.12.0` with a new `AudioBuffer` helper:

```markdown
## What's New

| Version | Feature |
|---------|---------|
| 0.12.0 | **AudioBuffer** — new `IAudioBufferService` for low-latency audio queuing across the pipeline |
| 0.11.0 | **BlazorComponents** — `ElBruno.Realtime.BlazorComponents` RCL with 5 Razor components |
| 0.10.0 | **Per-session isolation** — `IConversationSessionStore` prevents history leakage in multi-user scenarios |
| 0.10.0 | **Thread-safe VAD** — activated `_inferenceLock` in `SileroVadDetector` |
| 0.9.x  | **Kokoro & VibeVoice TTS** — two new TTS adapter packages |
```

---

## Enforcement

This rule is checked by the AI agent during release preparation. If the rule is skipped without justification, the release commit is considered **incomplete**.
