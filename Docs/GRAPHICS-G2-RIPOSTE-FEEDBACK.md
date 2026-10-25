# Riposte feedback — issued counterattack cue

2026-10-24. Status: native implementation and selected regression verification
complete:206 distinct passes,0failures,0skips. Ordinary captures now record
Player Hurt and Guarded feedback, but no full parry or issued Riposte was
observed. The forge/tempering attempt was interrupted by real combat and remains
incomplete. Root owns Unity and subsequent ordinary acceptance.

## Player-visible invariant and scope

An actually issued Riposte should face its opponent and request the existing
Attack pose before the counter resolves, including when that counter misses.
Rejected counter attempts must not manufacture an attack. Preserve accepted
Player art, combat outcomes/RNG, selected weapon, one-strike allowance, audio
and the existing one-event ordinary/multihand action contract.

This is a narrow presentation defect, not new defense mechanics or art. The
user's Qud-inspired native style remains unchanged; no new Qud parity claim is
made. Guard/parry already emit truthful words and audio. Other selected-weapon
skills, Rejoinder and Backswing remain separately named follow-up gaps.

## Pre-implementation verification sweep

Read the saved [source audit](Verification/Live16x24/riposte-action-source-audit.md)
against current source, including its direct-call inventory and existing native
fixtures. Corrections/decisions:

| Source | Verified contract / correction |
| --- | --- |
| `CombatSystem.cs:94–133,151–221` | Ordinary/committed action emits facing+Attack once before hand gathering. Its internal single-strike calls must keep default behavior; unconditional per-strike emission would duplicate normal/multihand cues. |
| `CombatSystem.cs:228–258` | Single strike rejects null actor/target/zone/RNG, then establishes supplied/generated provenance and diagnostic cause before audio and accuracy. The optional cue belongs after those scopes and before resolution. Natural miss/full parry/armor stop are issued strikes, not cancellation. |
| `LongBlades_Riposte.cs:16–49` | Real full-parry path checks parent ancestry, actor/contact and selected held blade, reserves before proc, revalidates after proc, spends before issue and releases reservation in `finally`. It calls single-strike directly and currently has no facing/Attack event. |
| `EntityVisualHooks.cs:94–97`, `AnimatedEntityRenderer.cs:451–461` | Attack callback is currently unprotected; flat renderer consumes it and reads current VisualFacing. Reuse this existing hook/art without a new renderer/state. |
| `WorldAudioCombat.cs:78–79` | Existing optional audio callback containment is a local try/catch. New optional visual failure must likewise not abort committed simulation. It does not guarantee later multicast subscribers run after a throw. |
| `MeleeStrikeProvenance.cs:9–39`, `RiposteAllowancePart.cs:8–11` | Scope/provenance and Spent are public; Reserved/ComparisonUncapped are internal. Tests use reflection for reservation observations instead of inaccessible member references. |
| Saved audit test1 | **Correction:** never assert inside the callback that production will catch. Capture facts, then assert after combat returns; NUnit assertion exceptions would otherwise be swallowed and create false GREEN. |
| `FiftySecondCombatFixture.cs:29–40`, `MultiCellAbilityConsumerTests.cs:76–88` | The reusable native fixture has an actual equipped selected blade and no generated natural offhand. A separate factory-Player multihand counter will prove two actual strike rolls still share one outer cue. |

Independent read-only source audit by
`/root/projection_cold_review/look_focus_draft_review` agreed with the seam and
identified the callback-assertion/internal-access cautions before tests.

## Minimal implementation after RED

Implemented signature extension: final optional `bool emitAttackVisual = false`
on `CombatSystem.PerformSingleAttack`. Only Riposte passes `true`, by name.
After null cancellation and counter scope/cause setup, call existing
`FaceToward` and `EntityVisualHooks.EmitAttack` inside local exception containment.
Do not add random draws, global animation-deduplication state, audio or turns.
Do not reroute through ordinary melee: that would gather hands and alter veto,
companion/oath/provenance/selected-weapon semantics.

The existing reserved/spent gates are already set when the cue is emitted;
normal reentry through `TryCounter` must remain rejected before RNG. Arbitrary
presentation callbacks are not sandboxed gameplay authorities; any proven
post-callback invalidation problem must be assessed narrowly, not used to
rewrite every legacy single-strike caller.

Owned runtime files after RED: `CombatSystem.cs`, `LongBlades_Riposte.cs` only.
Initial tests: new `Assets/Tests/EditMode/Gameplay/Combat/RiposteVisualFeedbackTests.cs`
and fresh two-line `.meta`. This plan is the only owned living feature document.

## Verification and acceptance

Initial behavioral tests drive ordinary melee → full parry → real Riposte.
Capture exact actor/target/zone, facing, order, counter provenance, spent/reserved
state, pre-resolution HP and RNG counts. Counter hit and miss both need one
extra cue; failed proc needs none. Assertions run after callbacks return.

After confirmed RED, add bounded meaningful counters: invalidation before issue,
throwing/reentering observer, lethal ordering, ordinary multihand/committed
nonduplication and default direct-call compatibility. Existing flat motion
tests cover owner/FOV/replacement lifecycle and accepted Attack/Hurt ordering;
rerun them rather than expanding this slice into new art or renderer mechanics.

Relevant native suites: `ComposableRiposteTests`,
`ComposableRiposteAdversarialTests`, `ComposableBladeTests`,
`ComposableBladeAdversarialTests`, `FloatingCombatTextTests`,
`WorldAudioDefenseTests`, `EntityVisualStateTests`, `FlatPixelActorMotionTests`.

Root ordinary acceptance must observe an actual keyboard-driven full-parry and
issued counter with bounded multi-frame capture and real action receipts. A
test-only hook emission or posed sprite is not that evidence. Until then,
Player Hurt and Guarded now have the bounded observations recorded below;
full-parry/Riposte and continuous action-feel acceptance remain pending.

## Initial self-review and log

- 🔵 Source sweep completed before tests; approved art remains unchanged.
- Native RED at 23:19:57–59 UTC: three cases, two intended missing-counter-cue
  failures and one failed-proc control passing; zero skips. Evidence:
  `Verification/Live16x24/riposte-feedback-red.xml` and its job/summary receipts.
- Implemented the optional single-strike cue and Riposte-only opt-in after that
  RED. Existing reservation, selected blade, RNG, audio and outcome paths remain.
- Expanded to twenty focused cases: issued hit/miss/parried/armor-stopped/lethal,
  failed proc and five post-proc invalidations, throwing/reentering observer,
  ordinary multihand/committed and default direct-call counters, null cancellation,
  plus three real parry→counter Player-renderer cases (east/west and hidden/reveal).
  The seventeen added cases are post-RED counterchecks, not independently RED-proven.
- Independent source review caught a fixture API error before GREEN: motion
  advancement is public. The new harness now invokes `AdvancePresentation`
  directly rather than searching only nonpublic methods. This was source review,
  not a native behavioral failure or production correction.
- First expanded native run: twenty cases, sixteen passing and four fixture
  expectation failures. Existing landed Cutting damage calls
  `OnHitClassEffects.Apply` after damage (including lethal), which rolls its
  Bleeding chance. Successful counters therefore finish with three percentage
  draws, while the cue correctly observes two before resolution. Corrected only
  those four final totals; miss/zero-damage and cue-time counts remain distinct.
  Production is unchanged. The failed `riposte-feedback-green.xml` is retained.
- Final independent source review by
  `/root/projection_cold_review/look_focus_draft_review` found no remaining
  actionable P1/P2 after the public method lookup correction. It reviewed hook
  timing/provenance, exception containment, nonduplication and fixture cleanup;
  it did not compile or run Unity.
- Final native selected sweep at23:28:24–23:28:40UTC passed206/206,0fail,0skip.
  XML contains206 unique full test names:20 focused cases and186 regressions
  across eight neighboring fixtures. Evidence:
  [final summary](Verification/Live16x24/riposte-feedback-final-summary.json),
  [XML](Verification/Live16x24/riposte-feedback-final.xml), and matching final
  job receipt. This is a selected native sweep, not the whole project suite.
- 🧪 Tests establish emitted events, simulation invariants and submitted frames;
  the ordinary sessions below did not produce a full-parry counterstrike and
  therefore do not close that gameplay acceptance gate.

## Ordinary continuation — source-checked capture bounds

The seed64 launch receipt records normal SampleScene, absent saved art-mode
preference and no forced presentation. Paired cellar receipts at tick760 record
Player2695, level2,42HP: the earned point changes1→0 and Riposte rank changes
null→1 without changing the listed inventory. These are
`riposte-feedback-earned-point.json` and `riposte-feedback-learned.json`;
learning the skill does not establish that it later triggered.

### North encounter: Player Hurt and Guarded, no observed Riposte

[G2Action-20261024-234223-659/index.json](Verification/Live16x24/G2Action-20261024-234223-659/index.json)
contains19 untouched1280×800 captures, ticks1680–1730, Player at63,17 in
Overworld.11.9.0. Its event file records15observations:6attacks and9damage
events. The local opponent is Viper8952. Metadata proves these bounded
submitted states:

- Frame01 at tick1690 has the exact `Live16x24 Player hurt` sprite, HP39,
  and Player-owned `En garde`/`Guarded` readouts. Frame02 already displays the
  idle artwork while its enum remains Hurt; frame03 separately reaches Idle.
- Frames07 and11 show subsequent Player hurt sprites. Frame15 at tick1730
  shows the Player's normal attack pose; its event is the8-damage lethal hit
  against Viper8952. Frame17 reaches Idle. PlayerHP is26 by the final frames.
- No recorded attack has counter depth greater than0 or nonzero depth. No
  full-parry or issued-Riposte evidence was observed. `Guarded` and active
  `En garde` must not be relabeled as a full parry or successful counter.

This closes only the prior lack of sampled ordinary Player Hurt/Guarded
submission evidence. The readout/pose metadata does not measure guard's exact
mitigated damage, continuous motion feel, sound synchronization or controller
ergonomics. This documentation audit inspected events and frame metadata, not
the PNGs as a new independent visual review. The root task owns image review.

### East forge approach: interrupted by a dangerous ordinary encounter

The real preparation route harvested FrostLichen8993 in north11.9.0:
`forge-temper-lichen-before/after.json` record tick1620→1630 and the new single
carried lichen. The later `forge-temper-brew-preview.json` is explicitly still
pre-commit: lichen8993 remains carried at tick1730. It is not an output receipt.
Root's subsequent ordinary Play observation records brewing actual output8995
and traveling to east12.10.0; no successful temper was performed.

[G2Action-20261024-235149-310/index.json](Verification/Live16x24/G2Action-20261024-235149-310/index.json)
contains24 untouched1280×800 captures, ticks2140–2200. Player2695 begins11,18
at22HP; Scrabbler11285 begins7,18 at15HP. Its longer event stream contains
16observations:6attacks and10damage events. Exact bounds:

- Frames01/02 and04/05 record Player walkA/B. Frame08 at tick2170 records
  Player Hurt with `En garde`/`Guarded` and18HP; frames12/13 at tick2180 again
  record Hurt and6HP. Frames18/19 at tick2200 show the hurt sprite at1HP;
  frame21 reaches Idle. These are real injury/recovery samples, not defence
  success or a successful forge interaction.
- The event stream extends beyond the24-frame capture window. It ultimately
  records Player5HP; do not describe that retreat state as the final captured
  frame, which remains the earlier1HP state. All recorded attack provenance
  stays at depth0/counterDepth0. Again no observed full-parry/Riposte occurred.
- Root observed the Scrabbler sever the blade-bearing hand, both real healing
  tonics being consumed during the encounter, and retreat through the world map
  at5HP with bleeding stopped. These later route facts are the root's ordinary
  session account, not facts established by the pre-brew preview or the limited
  frame metadata. The lost hand/weapon invalidates any assumption that the
  earlier selected blade is still equipped or that tempering can resume unchanged.

The encounter failure is retained. Neither frost tempering, forged output,
on-hit freezing, full-parry nor an actual issued Riposte is accepted from it.
Do not grant replacement supplies/limbs, force a counter, teleport to the forge,
or erase the real failed approach to obtain a success screenshot.

### Separate diagnostic and cleanup

Both event files expose a zone-attribution inconsistency: some periodic damage
against current Player2695 is labeled Overworld.11.10.0 while current frames and
ordinary attacks are in11.9.0 or12.10.0. The first stream has five such records;
the second has three. This is the separately owned poison-zone investigation,
not a proven Riposte defect or a correction made by this feature. Keep the raw
evidence and fix/test it separately before treating those labels as truthful
current-zone presentation evidence.

`riposte-feedback-cleanup.json` at23:56:08Z records stopped Play, isolation false,
artPending false, viewBorrowed false, null save-root override and SampleScene.
It proves that recorded cleanup, not the Editor state during any later work.
No new build, physical Deck run or full six-zone acceptance is claimed.

## Bounded next action

Finish the separately scoped periodic-damage zone correction and its review.
Then re-read the actual saved character's location, HP, effects, limbs, equipped
and carried weapon custody, remaining medicine and brewed8995 custody before
issuing movement. The immediate useful proof is a safe current-item readout and
save/reload continuity, not another prolonged counter-proc hunt at5HP. Resume
ordinary recovery/preparation through existing reachable services only when the
current game state supports it. Do not assume the world map is a healing pause
or that the severed blade is available in the pack.

If no safe attainable recovery/weapon path exists, retain that run as failed
route evidence and defer its forge continuation. A separately labeled ordinary
new game may supply a future deliberate route; it must not be represented as the
same character's recovery. Keep Riposte ordinary full-parry/counter acceptance,
complete forge/crop loops, full six-zone journey and packaged/device gates open.


Continuation correction: NativeSaveIsolation.Finish deletes its disposable audit root after restoring user settings. The injured east-run F5 checkpoint was not separately exported before cleanup; its screenshots/events remain, but that disposable5HP save cannot be resumed. The separately exported full-health earned checkpoint is preserved and the sword pass explicitly replays that branch. No user save was removed or overwritten.
