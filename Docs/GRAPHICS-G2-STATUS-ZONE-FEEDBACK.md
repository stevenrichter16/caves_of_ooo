# Status feedback after travel

2026-10-24 local / 2026-10-25 UTC. Status: native RED confirmed (7 failures / 4 controls); focused GREEN11/11 and affected regression274/274, zero failures or skips. Ordinary post-fix visual acceptance remains pending.

## Problem and bounded plan

Actual northern-worksite play in G2Action-20261024-234223-659 showed poison reducing the Player HP while passing the old glade zone to DamageCallback. Current-zone Hurt/readout ownership checks correctly refused the stale context. Fix the producer, not the renderer. Preserve all accepted art, poison balance, RNG and non-Player catchup policy. No new Qud parity claim is made.

1. Drive real transfer, InputHandler handoff, scheduler and PoisonedEffect in a failing native test.
2. Resolve Player owner-start effects through its exact current SpatialZone, canonical indexed cell, membership and managed graph; detached or superseded owners provide no invented zone.
3. Preserve existing NPC Brain.CurrentZone context. Add visibility, detached/no-brain, replacement, death/drop, unrelated ActiveZone and healthy controls.
4. Run focused and affected native regression; independently review. Ordinary post-fix gameplay remains a separate acceptance gate.

## Pre-implementation sweep

| Source | Confirmed contract |
| --- | --- |
| GameBootstrap, InputHandler.HandleZoneTransition | Player brain is wired on launch, but destination rewiring excludes Player. |
| TurnManager.ResolveActorZone | Before this fix, used only Brain.CurrentZone for BeginTakeAction. The new branch changes Player resolution only. |
| PoisonedEffect / StatusEffectsPart | Uses the event zone, has no stored poison-origin zone to update. Bleeding/burning also consume this context. |
| CombatSystem.ApplyDamage | Zone goes to graphics and lethal drops/removal, so this also has a source-visible death-cleanup risk; no live lethal failure is claimed. |
| Entity.SpatialZone / Zone.GetEntityCell / WorldLocationContext.For | Exact ownership can be checked without scans, generating zones, or global ActiveZone substitution. |
| AsciiFxBus.QueueFloatingNumber | Tests must inspect FloatingNumber, not EventText; corrected during scratch source review before import. |

Independent scratch source review by `/root/projection_cold_review/look_focus_draft_review` found the queued-number-type mistake above, corrected before import, and no remaining P1/P2 in the proposed one-method correction and eleven-case fixture. It did not compile or run Unity. Full source audit is preserved with verification evidence.

## Verification / self-review

| Native checkpoint | Result | Evidence |
| --- | --- | --- |
| RED, 2026-10-24 23:59:06–07 UTC |11 total:4 passed,7 expected failures,0 skipped|`Verification/Live16x24/status-zone-red.xml` and `status-zone-red-summary.json`|
| Focused GREEN, 2026-10-25 00:02:46 UTC |11 passed,0 failed,0 skipped|`Verification/Live16x24/status-zone-green.xml` and `status-zone-green-summary.json`|
| Affected regression, 2026-10-25 00:04:08–18 UTC |274 passed,0 failed,0 skipped|`Verification/Live16x24/status-zone-regression.xml` and `status-zone-regression-summary.json`|

XML full-name comparison confirms274 distinct cases in the status regression,
including the focused11. The earlier Riposte sweep contains206 distinct cases;
the two sweeps overlap in exactly37 `FlatPixelActorMotionTests` cases. Their
combined coverage is443 distinct cases, not480 or491. These are selected native
suites, not the complete project test suite.

Applied-source cold review by `/root/projection_cold_review` confirmed the test
and meta are byte-identical to the reviewed drafts and found no remaining
P1/P2. The branch checks the exact indexed anchor, canonical cell reference,
membership and current managed graph. It neither mutates Brain nor substitutes
ActiveZone, introduces no Physics/FOV requirement, and leaves the NPC branch
unchanged. It adds no scans, zone generation or new allocations by source
inspection; no measured allocation/performance claim is made.

The eleven cases use actual transfer/InputHandler handoff and owner-turn poison,
with source/amount/cadence assertions. Hidden, detached/no-brain, replaced
cell/graph, unrelated ActiveZone and healthy controls are non-vacuous. A real
lethal effect verifies destination removal, carried-item drop and the captured
numeric origin; two NPC controls preserve the existing brain-context policy.

- 🔵 Focused native behavior, affected regression and applied-source review are complete.
- 🧪 Ordinary post-fix poison/Hurt/readout capture is pending. Earlier actual
  Player Hurt/guard evidence precedes this correction and cannot close that gate.
- Tests establish context, damage/source, ownership, queued numbers and death
  placement; they do not prove ordinary visual timing, current packaged build
  or physical Steam Deck performance. This fix does not repair every other
  consumer of stale `Brain.CurrentZone`.

## Owned files

- Assets/Tests/EditMode/Gameplay/Turns/PlayerTurnZoneContextTests.cs and fresh meta.
- Assets/Scripts/Gameplay/Turns/TurnManager.cs, one Player producer branch applied only after confirmed RED.

## Implementation log

- Initial test request raced the import and discovered zero tests; it is not RED or GREEN. After import completed, native `status-zone-red.xml` ran all11 cases:7 intended failures and4controls passing, no skips (23:59:06–07 UTC). Exact destination/no-brain/invalid-owner/lethal assertions failed as expected.
- Only after that behavioral RED, added the Player-only physical ownership branch to ResolveActorZone. Existing NPC fallback is unchanged. No art, damage numbers, poison duration or random draws changed.
- Focused native GREEN ran the same eleven cases at00:02:46 UTC on2026-10-25: all passed, none skipped. No fixture assertions were weakened after RED.
- Affected native regression at00:04:08–18 UTC passed274/274 across ten fixtures,
  including the focused11; zero failures/skips. Exact XML overlap with the
  earlier206-case Riposte sweep was checked as described above. Root owns the
  separate ordinary post-fix replay, which remains pending.
