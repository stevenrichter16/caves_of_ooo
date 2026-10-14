# Floating event text

## Goal, scope and readiness

Add brief readable feedback over the player and visible NPCs after a real combat outcome or first status application. Preserve combat timing, RNG, action costs and status behavior. This is an original Caves of Ooo presentation feature, not a claim of copied Qud behavior.

🟢 Existing request bus, CP437 font, tilemap overlay, zone lifecycle and status display names can be reused. 🟡 Text needs its own semantic request: decorative letter particles become native sparks, while FX-Off currently preserves only numeric particles. 🟡 Visibility must be checked at the affected creature's origin, not just wherever letters happen to draw. ⚪ New art, save fields, input bindings and legacy save migration are unnecessary.

## Verified source sweep / corrections

| Source | Verified contract / correction |
| --- | --- |
| `AsciiFxBus`, `AsciiFxRenderer`, `WorldFxCoordinator` | Existing numbers are individual particles. Do not turn words into ordinary particles: native particle conversion would erase letters and FX-Off would discard them. Add EventText request/group, preserve pooled request reset and normal coordinator ownership. |
| `StatusEffectsPart.ApplyEffectInternal` and `SendApplied` | New accepted effects reach SendApplied after Apply/Applied; refresh/stack return early. Forced effects use the same path. Gate notifications on still-active ownership after listeners; load reconstruction must remain silent. |
| `Effect.DisplayName`, `Entity.SpatialZone`, `RenderPart.Visible` | Reuse real UI effect names, fall back to physical zone when callers omit it, and reject absent/hidden entities. Avoid inventing status names from class names. |
| `BladeDefense.TryParry`, `CombatSystem.ApplyDamage` | Parried means the full eligible strike was parried. Guarded is the failed-parry fallback, not a full parry. Fully resisted damage has explicit veto/zero branches; do not claim resistance for partial damage. |
| Existing damage digits | Retain numeric feedback. Words add explanatory outcomes, not damage simulation or another turn blocker. |

## Design and implementation plan

1. Shared `AsciiFxBus.EmitEventText(Zone zone, Entity anchor, string text, string colorString)` emits one pooled EventText request. Request captures origin, anchor and bounded readable text (max 24 characters), has no gameplay lifetime/serialization and never blocks turns. Deduplicate identical pending messages for the same actor; cap pending event groups at 64. Reject hidden/unexplored origins, absent/hidden actors and wrong zones before admission. Generic enough for future world events.
2. Renderer keeps at most 32 active groups and 3 per actor, gives simultaneous different labels separate vertical lanes, with two-row spacing to allow independently timed rises, ignores duplicate active labels without renewing their lifetime, and renders words last above other effects. 1.1 wall-clock seconds independent of animation-speed settings, one-cell rise, clamp at map edges; readable full CP437 glyphs, always bypass native decorative conversion. Check captured origin and current actor visibility each frame; discard on hide/removal/zone/lifecycle change. Readouts remain in Reduced and Off modes; no new meshes/materials per event.
3. Wire actual Parried/Guarded outcomes and full damage prevention, with positive and negative controls. Preserve probabilities, guard spending and costs. Avoid an intrusive new message on every normal hit/miss.
4. Central first-status cue uses `Effect.DisplayName` with initial capital and negative/beneficial colors, after the effect really takes hold on a living creature. Same-type live stacking/refresh, expired/removed-on-apply effects, rejection, ticks and save restoration stay silent. Distinct new statuses get distinct labels. Forced accepted applications also count.
5. Native EditMode RED before implementation; GREEN plus adjacent FX/status/combat regressions. Add a deterministic visual smoke scenario that exercises true status application and parry plus rejection, native and text modes, FX-Off, lifetime, fog and reset.

## Review passes (after implementation)

1. Event truth: success versus rejection, refresh versus first onset, full/partial combat outcomes, force application, current zone and load silence.
2. Presentation and lifecycle: fog leaks, native letter preservation, edge placement, simultaneous messages, bounded groups, pooling/reset and no input blocking.
3. Independent final integration: read all diffs, rerun relevant native tests, inspect live frames and fix material issues. Record what native/script tests prove and what physical Steam Deck/visual feel remains unverified.

## Results

Complete: three review passes, 373/373 selected native Unity EditMode tests, and an isolated live scene inspection. Evidence is in `Docs/Verification/FloatingEventText/`.

- API missing-member compile RED captured before bus/scenario/render implementation. Initial behavioral RED: 81 tests, 42 expected failures, 39 controls passing.
- Review 1 (event truth): distinct Strength/Agility surges share a CLR type but are distinct active benefits. Added presentation-only `Effect.SharesStatusWith`; default runtime type, tonic override includes StatName. Same-stat refresh and Parched severity changes remain quiet. Review RED reproduced the missing Agility label.
- Review 2 (presentation/lifecycle): reproduced missing-RenderPart disclosure, movement separating an actor from labels, and staggered rises overwriting another label. Require an actual visible RenderPart on admission and every frame, follow current owner position while retaining captured-origin visibility checks, and separate lanes by two rows. Top-edge clamp reserves six rows. Native review RED: 91 tests, 9 expected failures, 82 passes.
- Correction: one handwritten test meta initially had a 33-character GUID, so Unity skipped that fixture. Replaced with a generated 32-hex GUID and confirmed the status fixture actually ran before production changes. Initial 52-case result is an incomplete diagnostic receipt; the subsequent 81-case RED includes status tests. The resistance fixture's stat Max also needed to allow 100% before its intended RED was meaningful.
- Combat labels: Parried (full defense), Guarded (failed-parry fallback), Riposte (an issued counter even if it misses), Resisted (complete elemental resistance), Blocked (complete veto/mutation prevention). Normal misses/hits and partial resistance retain existing feedback.
- Scenario: `Caves Of Ooo > Scenarios > Combat Stress > Floating Event Text`. Uses actual parry resolution and real Stunned/Poisoned/Burning application, repeating every three seconds in a cleared patch of the normal starting zone. Scene-only fixture, not shipped content or save migration.

### Final review and verification

- 🟡 Review 3 caught two issues, fixed before commit. World glyphs substitute objects for letters, so event words now use the existing cached UI font (including safe unknown-character fallback). The repeatable scenario now validates adjacency, held blade and live patient location before mutating anything, and pauses on invalidation. The final RED run had 373 cases: 19 expected font/scenario failures and 354 passing controls. The corresponding GREEN run passed all 373, with zero failures or skips in 27.42 seconds.
- 🔵 The coordinator retains pooled request ownership, advances readout wall time exactly once, and repaints newly admitted words at zero elapsed time. Native decoration under a word is suppressed before creating geometry. Caps, duplicate suppression, resets, real effect identity, force application and quiet save reconstruction have regression coverage.
- 🧪 Counter-checks include hidden versus visible, missing versus present RenderPart, first onset versus refresh/rejection/removal, independent tonic stats versus same-stat stacking, actual parry versus failed guard, full versus partial resistance, valid versus displaced/dead scenario actors, and UI letters versus ordinary decorative glyphs.
- Live native inspection produced five readable labels (Parried, En garde, Stunned, Poisoned, Burning), with no turn blocking. Native Full, native Off, and ASCII Off frames were inspected and saved. Words expired after 1.11 wall-clock seconds. Hiding the player's origin reduced five labels to three; restoring visibility did not resurrect them. Unity's console contained zero errors after teardown.
- Save isolation used a private temporary audit root. Play mode was stopped; personal save-root and last-game preference restoration were verified, and audit graphics/FX settings restored. No scene asset was saved from the scenario.

### Honesty bounds and remaining checks

Can verify: actual parry resolver/status-onset integration, readable world-camera frames, native and ASCII/FX-Off compatibility, bounded lifetime, fog suppression, nonblocking feedback, successful isolation cleanup, and the 373 selected native regression cases.

Cannot verify from these checks: physical Steam Deck readability/performance or a full project regression suite. The live fixture resolves defense directly rather than scheduling an NPC attack; full strike behavior is covered by EditMode tests. Camera captures omit the HUD. No distribution rebuild was requested for this change.

### Implementation map and deliberate differences

`AsciiFxBus` provides the reusable semantic event; `AsciiFxRenderer` owns bounded word groups; `WorldFxCoordinator` preserves them across FX preferences. `StatusEffectsPart` emits first-onset cues, while `Effect`/`TonicStatSurgeEffect` distinguish semantic status identities. `BladeDefense`, `CombatSystem` and `LongBlades_Riposte` report resolved outcomes. The showcase, menu entry and five test fixtures exercise these paths.

The initial plan's generic CP437 reference is narrowed to the existing **UI** CP437 glyph bank after live/render review. Status sameness uses semantic identity rather than only runtime type because different tonic stats legitimately coexist. Neither adjustment changes combat rules, status stacking rules, saving, costs or randomness. Sound auditions are a separate user-directed follow-up and are not connected to gameplay by this feature.
