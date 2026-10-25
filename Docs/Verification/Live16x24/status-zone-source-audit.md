# Poison turn-context zone audit — 2026-10-24

Read-only source/evidence audit by `/root/projection_cold_review`. No Unity,
test, runtime, art or repository edits. Root owns the active ordinary Play run.

## Finding and severity

**P2, real player-visible defect:** Player turn-start status damage can receive
the original launch zone after a legitimate zone transition. HP changes, but
current-zone Hurt, numeric feedback and target-cell invalidation correctly reject
that stale context. This is not a Riposte regression or a reason to weaken the
renderer’s exact-zone authority. The issue also has a source-visible lethal
cleanup risk; no lethal poison failure is claimed observed.

Actual evidence: `Docs/Verification/Live16x24/G2Action-20261024-234223-659/`.
The event observer records supplied hook arguments, not a guessed zone. Player
2695 and Viper8952 fight in `Overworld.11.9.0`. Correct-zone melee damage is
followed by old-zone `Overworld.11.10.0` periodic damage in the same owner cycle.
At Unity frame574766, there is only a Viper attack and old-zone damage2; captured
frame10/tick1710 is actual11.9, HP31, PlayerIdle, no numeric readout. Its visible
log says the bite failed to penetrate, then poison dealt2. This isolates the
periodic feedback failure from ordinary melee damage.

## Source chain (precise current locations)

1. `GameBootstrap.cs:1469–1481` initially wires every creature Brain.CurrentZone,
   including the Player, to the starting zone.
2. `ZoneTransitionSystem.cs:155` (horizontal) and `:471` (stairs) transfer the
   Player’s physical ownership with `TryTransferEntityTo`; neither rewires the
   Player brain. Its `:237` CurrentZone assignment is for transferred companions.
3. `InputHandler.cs:1126–1163` updates CurrentZone/manager and registers/rebinds
   destination NPCs, but Brain.CurrentZone is assigned only inside
   `creature != PlayerEntity`. The Player brain therefore retains launch/load
   zone. Do not infer a saved poison-origin field: PoisonedEffect does not have one.
4. `TurnManager.cs:343–346,565–574` constructs BeginTakeAction with
   ResolveActorZone, which currently returns Brain.CurrentZone only.
5. `StatusEffectsPart.cs:525–547` forwards that event to OnTurnStart unchanged.
   `PoisonedEffect.cs:36–43` takes Zone from the event and calls ApplyDamage.
6. `CombatSystem.cs:1138–1143` forwards the stale Zone to Damage visuals;
   `:1207–1228` uses it for grouped numbers, death and cell invalidation.
   `AnimatedEntityRenderer.FlatPixel.cs:130–137` correctly refuses a zone other
   than its current exact instance. Missing Hurt/readout is an upstream context bug.
7. `CombatSystem.cs:1380–1499` also uses supplied Zone for death/drop/witness/
   removal. A lethal periodic hit could therefore remove from the wrong world
   graph or lose ground equipment. This is a source risk, not live evidence here.

Bleeding/Burning/Acidic/Electrified also read the same BeginTakeAction Zone;
repair the producer rather than patching each effect or special-casing poison.

## Smallest safe source repair

Prefer a **Player-only** canonical physical-zone branch in
`TurnManager.ResolveActorZone`. Obtain `actor.SpatialZone`, then prove the exact
current indexed anchor (`GetEntityCell(actor)`, ParentZone, canonical GetCell
reference and reference membership in Objects). If WorldLocationContext supplies
an OverworldZoneManager, require its CachedZones entry for this ZoneID to be the
same Zone reference. Return that owner zone; do not substitute global ActiveZone,
search all zones, generate zones, change AI context or rebind mutable effects.
For a detached/invalid Player, return null rather than the known-stale brain.
Leave the existing non-Player Brain.CurrentZone branch unchanged: this avoids
changing dormant NPC catchup/off-zone simulation semantics.

This one-method producer correction repairs already-live and loaded Players
whose brain is stale. Merely updating Player Brain in HandleZoneTransition would
repair future successful transitions but leave the already-affected checkpoint
incorrect until another transition/load. Synchronizing that field can be a
separate justified follow-up; it is not required to fix authoritative tick context.

Do not change poison potency, duration, RNG, attribution, per-owner cadence,
damage amount, or effect removal. HP may still tick with null context in pure
detached fixtures; the guarantee is no fabricated world zone/visuals.

## Concrete bounded RED design (no tests written or run here)

New focused fixture can reuse `HotbarSaveFixture` isolation and real
`ZoneTransitionSystem.TransitionPlayer` plus the existing reflected native
`InputHandler.HandleZoneTransition` harness pattern in
`DensityWorldActionIntegrationTests.cs:68–89`. Use a real owned Brain whose old
zone is intentionally retained, put the Player in the new canonical zone,
apply actual PoisonedEffect with fixed1d1, then drive real TurnManager
ProcessUntilPlayerTurn (not manual EmitDamage). Capture hooks and assert afterward.

1. **Successful ordinary transition → poisoned owner start:** exact Damage zone
   is the destination, source is preserved, HP decreases1, one current-origin
   grouped number, no number in old zone; compare actual BeginTakeAction context.
   Assert Brain still points old to make the authority correction nonvacuous.
2. **Visible native Player:** use actual ZoneRenderer as prior motion fixture;
   destination poison tick submits existing Hurt frame48; hidden Player still
   loses1HP but does not show a pose/readout or replay it on reveal. No new art.
3. **Canonical no-Brain Player:** placed owner receives its real zone; a detached
   Player with stale Brain receives null, not former zone. Same-ID replaced
   managed graph/cell cannot authorize the stale object graph.
4. **Lethal1HP poison in destination:** exact death callback zone, Player removed
   from destination, target old zone untouched, expected equipment drop/current
   death source. This is a meaningful check for the wider source risk.
5. **Non-Player compatibility:** actor whose existing Brain zone differs from
   another active world zone retains original non-Player context; no global
   ActiveZone/catchup policy change. No-brain detached NPC remains null.
6. **No status / no transition controls:** no invented damage, existing owner
   cadence and normal current-zone values unchanged.

Useful existing regression groups: PoisonBalanceRegressionTests,
EffectTickOnApplyTurnTests, BearTrapBleedingBugClosureTests, StatusEffectTests,
TurnManagerAdversarialTests, MultiCellTransitionTests,
DensityWorldActionIntegrationTests, FlatPixelActorMotionTests,
GroupedDamageReadoutTests. Keep focused actual RED before producer edits.

## Actual Player action evidence (not Riposte)

Independent pixel/metadata review by
`/root/projection_cold_review/look_focus_draft_review` inspected the original PNGs.

| Frames | Actual evidence |
| --- | --- |
|01,07,11|Player2695 at63,17 actual11.9, Hurt time0, opening-motion rect48,0,16,24; HP39/33/28. Each follows correct-zone direct damage2/5/2 and old-zone poison1.|
|02,08,12|Hurt state about0.10s is already displaying idle recovery sprite; do not label these authored Hurt-pose pixels.|
|03,09,13; clean06|Recovery to Idle/base rect0,0,16,24; frame06 has no remaining readouts.|
|01|Visible Guarded and En garde words + red2; log reports measured guard and partial damage. This is guard feedback, not full parry.|
|10|Old-zone-only poison2 leaves HP31 with Idle/no readout, supporting the stale periodic-feedback finding.|
|15–18|Ordinary Player Attack rect32 then recovery/Idle; event Origin0, CounterDepth0, lethal8 against Viper8952. No reactive counter provenance.|

Bounded acceptance: actual ordinary Player Hurt/guard-word/Attack/recovery art
is observed at1280×800. No full-parry or Riposte is in this19-frame capture;
native206 tests (including20 Riposte) do not replace that missing ordinary gate.
