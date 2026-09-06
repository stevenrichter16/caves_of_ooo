# Hauling lifecycle and stale-link repair — A09 / GA03e

Status: COMPLETE from48489b57,9263→9322GREEN (+59tests). CoO-original lifecycle integration repair.
No new haulable content, sprite, save field, cost or tactical hauling rule.

## Player contract

Removing either endpoint releases the grip and refunds only its stored hauling
penalty. Removed/destroyed loads cannot be pulled back into a zone by walking or
forced movement. Healthy movement and vetoed removal/destruction preserve hauling.
Saved stale links are repaired after all cached-zone membership indexes are rebuilt.

## Source verification / corrections before implementation

| Source | Verified behavior / correction |
|---|---|
| DragSystem DragPart.AfterMove45–78 | Null Dragged returns early and leaves penalty; validate before early return, then re-read link. |
| DragSystem.ValidateLink286 | No production callers; current removal tests manually call it. Existing D4/D5 docs overstate integration. |
| DragSystem.FollowInto323 | ForceMoveTo can place an absent entity. Validate membership, reciprocal/lifecycle state and matching load before invoking it. |
| MovementSystem.ForceMoveTo199 / Zone.MoveEntity | First placement is supported. Do not globally forbid absent entities; repair the drag caller. |
| Zone.RemoveEntity219 | Cleanup only inside successful indexed removal; wrong-zone/repeated removal must not detach a healthy link. |
| DragSystem.Release380 / LiftSpeedPenalty263 | Refund AppliedPenalty exactly once; preserve unrelated Speed penalties and another hauler's valid reciprocal grip. Inverse cleanup needs matching protection. |
| DestructionSystem.Destroy145–169 | Gone becomes true before Destroyed callbacks; structural HP reaches zero before BeforeDestroy can veto. Use Gone for committed destruction. |
| CombatSystem.ApplyDamage882 | Creature death uses Hitpoints.BaseValue<=0; missing HP is valid for props/lightweight fixtures. Died already releases a hauler. |
| SaveGraphSerializer.RebuildLoadedWorld | Repair after every zone index is rebuilt, observing both placed haulers and placed loads. Full-session behavior only; token-graph defaults remain. |
| Objects.json / HaulableContentTests43 | Real HaulBarrel weighs75, structural HP8, noncarryable; Strength16/Speed100 grab at adjacent cells yieldsSpeed70. Synthetic names alone do not exercise shipped Handling. |
| DragWeightAndEdgesTests182–216 | Old removal tests call ValidateLink explicitly; new lifecycle REDs must omit that manual repair. |
| Acrobatics_Tumble125 | Target is temporarily removed, so generic removal releases that target hauler. Acting-hauler movement integration is separate A35/A37. |
| Docs/DRAG-AND-HAUL.md §14 | Existing costs, minimumSpeed20 and stored-refund contract remain; append integration correction with this wave. |

Post-move distance is not grab reach: a normal step away from an adjacent load
briefly puts it two cells away. Validate membership/reciprocity/lifecycle, not reach.
Inspect final load placement inside a Destroyed callback that force-moves the hauler;
terminal absence alone can miss a forbidden transient pull. Pair BeforeDestroy movement
that vetoes: HP0/Gonefalse load still follows normally.

## Plan and verification

1. RED direct removal of both ends, voluntary/forced follow after removal, wrong-zone
   and repeated removal, stored/clamped refund, null and mismatched reciprocal links.
2. Add minimum caller/removal cleanup; validate Gone/dead endpoints. Never remove
   another actor's healthy grip when cleaning an unrelated stale inverse Part.
3. Dedicated20–40-case adversarial matrix: healthy/blocked/diagonal/zero movement,
   actual horizontal/vertical transitions and refusals, real barrel lethal/nonlethal/
   vetoed damage and callback-time movement, actual hauler death, full-session valid/
   cross-zone/unplaced/null/Gone/dead links and repeated rebuild, standalone aliases.
4. Independent cold-eye and player-flow hypotheses; fix every notable finding.
5. Live keyboard hauling/removal/save recovery plus a75s movement/cadence observation
   before/after the change; full suite, daily log and attributable commit.

## Performance

Read PERF-FOUNDATION.md before authoring. Validation uses existing part lookups and
zone membership checks, without collection allocation in movement paths. Avoid an
all-zone scan per move; full-zone iteration belongs only to load finalization, using
GetReadOnlyEntities when safe because link cleanup changes Parts, not membership.
Preserve existing per-cell movement dirty hooks. Native measurements use actual
movement and report maxima/coverage; no speedup or subjective-feel claim without
supporting evidence. Profiling/debug observers remain scenario-only.

## Boundaries

Respect protected Movement/Destruction/Save work; prefer the smallest owned call sites.
No generic ForceMove change, reach retuning, new hauling animation or grab cost.
Follower war-laundering, broad movement hooks, Tumble targeting, full apply atomicity
and body-ID validation remain separate queued repairs.

## Additional verified fixture/callback boundaries

Entity.FireEvent already re-anchors after a Part removes itself; test a trailing
AfterMove observer, without changing global dispatch. Detach captures exact Part
identities before MessageLog callbacks, preserving any replacement relationship.
Native HaulBarrel currently displays its authored yellow0 glyph; it does not qualify
for WoodenBarrel’s existing sprite. Record this as visual debt, use the actual barrel
without renaming it, and do not claim sprite/pixel coverage. No new content is added.

The initial regression fixture uses real Objects.json HaulBarrel (HP8/Hardness0),
Strength16/Speed100 and stored penalty30. Full-session stale cases bypass normal
removal with explicit raw membership fixture setup, so new removal cleanup cannot
make load-repair tests vacuous. Includes an unplaced player and its unplaced load.
All wholly unplaced referenced graph links should be included if finalization exposes
the loaded entity list; otherwise explicitly bound that remaining case before close-out.

## Executed initial RED and native authoring corrections

- Initial test-authoring compile rejection: direct Destroy requires its fourth cause argument. Retained compile log; corrected before relying on XML.
- Actual initial26test run: 8PASS/18FAIL,0CS,09:48:58Z,.4513278s. Failures reproduce endpoint-removal leaks, forbidden callback-time pull, stale/mismatched links and six historical-save shapes. No production edits yet.
- Native launcher metadata contained a33character GUID; Unity ignored the file with0CS. Replaced with generated32hex GUID. Root retry also used an incorrect extra namespace; both failed launch logs retained.
- First measured baseline delivered135 discrete and178 held steps (89 repeats), zero step failures, over75.356s. Uncapped headless rendering exceeded30,000sample capacity during discrete phase, so held samples were absent and the validity gate correctly failed. Raised scenario-only capacity to200,000; retained failed raw capture and reran before implementation. No performance conclusion from incomplete samples.

## Minimum implementation and baseline

- Complete baseline41PASS,0CS;75.2867measured seconds,76,774frames;
 137discrete/178held steps (89held repeats), zero step failures. Both historical
 saved-link and normal-removal resurrection reproduced through real keyboard walking.
 All profiler markers valid; teardown and disposable-save-root removal verified.
- Implemented successful-removal detach, AfterMove/FollowInto validation and
 full-session graph repair after rebuilt indices. Full token list covers wholly
 unplaced nonplayer pairs; manual rebuild snapshots placed entities plus player.
- Minimum focused82/82GREEN,0CS,09:57:21–22Z,.6660317s. Initial26 plus existing
 hauling/content neighbors. Dedicated28-case sweep authored afterwards.

## Adversarial and cold-eye corrections

- Dedicated26 plus initial/hauling neighbors:106/108passed,0CS. Two travel
 refusal fixtures used Physics-only walls; current arrival code uses IsSolid
 (separate recorded A36). Added actual Solid tags for terrain-wall refusal
 controls; A36 remains queued and is not claimed fixed here.
- Root and independent reviewer identified replacement DragPart receiving the
 same old AfterMove after a cleanup message callback grabs another load.
 Hypothesis54run:53PASS/1FAIL,0CS,09:59:41–42Z,.6256526s. New load moved to5,5
 despite being grabbed after the actor arrived at4,5. No-regrab control passed.
- Added a hauling-owned event marker before validation: replacement grips skip
 that already processed event; nested genuine movement has a separate event.
 Added a nested ForceMove countercheck, preserving legitimate following.
- Dedicated sweep now29cases (26taxonomy +2callback hypothesis cases +1nested
 event control),55new hauling tests including initial26. Initial neighbor filter
 misspelled the save-decode class; final focused run uses its exact class names.

Final focused163/163GREEN,0CS,10:00:52–53Z,1.4562462s: all55new hauling
cases, existing hauling/content neighbors and52save-decode/isolation cases.
GUID audit:2443metadata files/2443valid unique GUIDs,0collisions.

## Live/full verification and late metadata review

- First post-fix native37PASS,82.7546s total/75.2699s measured;134discrete,
 178held89repeats,75,720frames,0step failures. Both removal/load cleanup pass.
 Teardown flags and disposable-root removal pass. Core full9318/9318GREEN,
 0CS,10:03:47–10:06:15Z,147.600372s retained as pre-log-review evidence.
- Independent review confirms lifecycle/identity/load coverage. Late observation:
 repair messages run before bootstrap replaces its old TickProvider closure, so
 a loaded save at17 can receive a repair entry stamped222. Add metadata RED/control,
 scoped saved-clock repair with finally-restoration, and a native deliberate wait
 before F6 so the discarded clock is demonstrably different. Full suite reruns.
- Historical docs incorrectly imply Released exclusively means player intent.
 A09 preserves existing Release/Died semantics and extends it to successful
 removal. Clarify Released=explicit release/removal/death and Slipped=validation/
 follow failure; no voluntary-intent inference from kind alone.

Late metadata RED33:31PASS/2FAIL,0CS,10:06:54–55Z,.5351171s; loaded17 and
missing-clock0 both incorrectly received222. Scoped repair-clock binding with
finally restoration fixes both. Healthy history and throwing-observer restoration
controls remain green. Final focused167/167GREEN,0CS,10:07:54–55Z,1.4057711s.
Total59new tests: initial26 plus dedicated33 (including callback and metadata cases).


## Close-out

Final full9322/9322GREEN,0CS; final native39PASS, including saved-clock metadata.
All notable review findings resolved; see GA03e-REPORT.md for exact raw gates and
performance/honesty bounds. Whole-game queue continues with A10.
