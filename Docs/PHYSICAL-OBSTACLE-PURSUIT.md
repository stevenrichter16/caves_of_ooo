# Physical obstacles and fair pursuit

**Status:** implementation accepted for publication after F11 `24bf7615`. Native test-first RED reproduces the defect; the repaired source passes735/735 affected native cases, including all47 new cases. The original controlled before/after pair proves the pursuit change but its first candidate run exposes a staging-only autosave rejection. The corrected, cache-preserving candidate follow-up passes12/12 live checks with12 validated paid inputs and zero errors. Evidence boundaries and failed attempts are retained below.

## Problem and scope

A player can stop ordinary single-cell melee pursuit by putting a beam into a hedge gap, even though the pursuer can physically walk around the boundary. The pathfinder's `ignoreCreatures` branch omits noncreature Physics.Solid owners; movement correctly refuses those owners, so the next turn repeats the same failed plan. This breaks believable pursuit before tactical hauling can offer fair delays.

This slice repairs that existing path filter only. It changes no blueprint, generation budget, saved schema, collider, LOS rule, terrain, actor ability, model or animation. Original source/content hashes were verified unchanged before implementation. See the retained sweep/counter map and actual baseline traces.

## Reference and divergence

This is CoO's existing navigation contract: FindPath.Search, AIHelpers.TryApproachWithPathfinding, KillGoal and MovementSystem. Existing ordinary wall navigation promises a reachable target can be approached around an obstruction. No external Qud source parity is asserted. The goal is readable exploration and fair enemies using the actual world geometry.

| Planned / assumed | Actual implementation and deliberate limit |
|---|---|
| Physical props should affect routes | The repaired intermediate-cell filter respects their existing Physics.Solid state. |
| Creature omission | Only actual Creature-tagged actors are skipped when requested; existing Solid-tag and door gates remain separate. |
| Larger actor / target contact | Existing footprint-aware route and ToContact are preserved unchanged. |
| Endpoint exception | Legacy intentional goal-cell handling stays as pinned by FindPathTests. |
| Tactical hauling defense | Deferred. A stalled pursuer is not accepted as successful separation; real paid-input witness must prove a detour before new content. |
| New model / sight shield | None. A beam remains the same transparent-to-LOS physical owner. |

## Implementation

The existing single-cell search now always evaluates its physical occupancy predicate. Inside that indexed loop:

```csharp
var owner = cell.Occupants[i];
if (ignoreCreatures && owner.HasTag("Creature")) continue;
var physics = owner.GetPart<PhysicsPart>();
if (physics != null && physics.Solid) return true;
```

The preceding Solid/door checks, goal exception, body/contact branch, traversal order, costs, heuristics and movement execution remain unchanged. The private helper is renamed to describe physical occupancy rather than falsely calling all physical owners creatures. The direct approach may still make an unsuccessful optimistic step before using A*; no broad AIHelpers refactor is included.

## Performance and observability

This is one existing bounded indexed scan with a corrected filter; it adds no object allocation, route cache, field, scheduler action or update loop. Existing A* node budgets remain. The surrounding FindPath allocation behavior is unchanged; no whole-search zero-GC or native frame-time improvement is claimed.

The action continues through ordinary KillGoal thoughts, movement and turn handling. The test/native observer records actual route choices, paid turns and outcomes. No per-node diagnostics are added to the hot search loop, and no new verb or independently routed action contract is introduced. Existing action observability is not represented as a newly instrumented pathfinding service.

## Test evidence and corrections

The eleven maintained methods contain43 paired cases. On unchanged source the corrected audit passes31 controls and fails12 cases exposing one defect. Three scheduler traces remain at `(6,10)` for31 NPC actions /30 player waits /310 ticks despite a fully executable ten-step bypass. Wall and genuine two-cell controls reach contact in nine actions /90 ticks. Thin props, creature omission and all door counters already work.

Initial evidence was27 pass /16 fail: four wide-body preflight errors incorrectly searched to the occupied player anchor. Actual KillGoal uses ToContact. Only that fixture preflight was corrected; both raw receipts/source copies remain, and the four initial failures are not game bugs.

The one-file private candidate passes43/43. The scoped affected private run passes695/695, including the focused43. One selected native fixture, MultiCellSpatialAdversarialTests, requires Unity test-only decode isolation and was excluded from the private runner without fabricating a replacement scope. It remains in the32-fixture native request. Both baseline and candidate compile as separate actual CavesOfOoo and full EditModeTests assemblies. Native RED/GREEN and the completed player-input witness are recorded below; private execution and reference compilation remain separate evidence.

## Self-review

- 🟡 Confirmed: ignoreCreatures bypasses real noncreature physical owners, stranding ordinary pursuit. The minimal correction now passes focused, affected native and controlled actual-input acceptance.
- ⚪ Corrected test premise: a wide body cannot occupy the player's target anchor. Four initial preflight failures are retained as rejected evidence and the proper contact-route control now passes.
- ⚪ Preserved limits: this does not make props block sight, grant enemy obstacle destruction, change diagonal rules or promise an escape outcome. Thin-prop fallback and existing door permissions remain pinned.
- 🧪 Open experience evidence: ordinary generated-site tactical utility, an escape outcome, and human readability remain unmeasured. Native tests and the controlled witness do not replace those.

Independent source peer review found no additional significant issue: only the physical filter changes; preceding structural/door gates and footprint/contact/hazard branches remain intact.

## Implementation log / files

1. Freeze ten player-flow hypotheses before detailed read, then run eleven paired methods including the actual movement bypass counter. Retain initial and corrected receipts.
2. Verify all ten source/content hashes match the audited baseline; compile maintained tests against actual separate Unity references.
3. Implement the exact one-file private predicate delta, run43 and supported695, and retain source peer review.
4. Root publishes F11, adopts test-only baseline, executes native RED, then checks exact source preimage before candidate adoption.
5. Root runs selected32 native fixtures, a bounded real-input witness, and finalizes this living doc/evidence in the same commit using CLAUDE §2.3.

Source/test files:

- `Assets/Scripts/Gameplay/AI/FindPath.cs`: respect noncreature physical props under the existing creature-omission policy.
- `Assets/Tests/EditMode/Gameplay/AI/MovableObstaclePursuitAuditTests.cs` and meta:43 paired planner/movement/scheduler/body/door/prop checks.
- `Docs/PHYSICAL-OBSTACLE-PURSUIT.md` plus retained verification evidence: this finalized living record, scope corrections and native/private results.


## Working prompts

Planning: verify why an ordinary pursuer fails to use an actually traversable detour around existing movable props. Compare creature omission, solid tags, physical occupancy, doors and body/contact routes. Preserve deliberate contracts and choose the smallest repair, with real movement and scheduler counters.

Implementation: execute native test-only RED on the published source, adopt only the verified predicate correction, then run affected native regressions and a disclosed controlled scene with actual paid hauling and unsuppressed pursuer turns. Preserve all failures, distinguish unit/private/native/live evidence, resolve notable findings and update this document in the same authorized main commit.


## Native integration log

F11 is published at `24bf76155ce0e4ba16dd20a8264fb5457a305e71`; root and origin matched before these tests were adopted. Native job `31ab043054de46d7968ad0697687c9ec` executed45 cases in2.7974468s:31 controls Passed,12 expected pathfinding failures and2 missing observer-entry failures, zero skips. The twelve failures match the one private-confirmed predicate defect; no production correction was adopted before this native RED.

The independent controlled-observer package was then adopted while FindPath remained unchanged. Native job `0f7ecbd853fc41b295e7be509554b0b9` passes8/8 in1.1988467s, covering new mode/isolation guards plus existing hunting, hauling and scene-restoration controls. This proves guard behavior, not the live arena.

The first native attempt `2a3308b4cce04593a4638c3a69061280` failed its recorder before any paid input: anonymous lighting data retained a raw Unity Color, whose `linear` property recursively serializes. The partial report and precise native exception are retained; this is not a pursuit baseline result. Repeated report failure also prevented the usual observer finish callback. Root used the authorized native stop, then a read-only check confirmed Play stopped, both isolation/observer flags cleared, original root/preferences restored and Main scene clean. A narrow scalar-lighting snapshot with serialization counters follows; no gameplay, geometry, RNG or input choice changes. The corrected baseline must run before the production path filter is adopted.


### Observer corrections before the gameplay comparison

Native serializer RED `33692dc861954c6d9da17fb5f9823df5` fails both new counters after reproducing raw Unity Color recursion. The minimal detached RGBA snapshot then passes the two tests, and native job `55bbfb25f9dd46e3b8f74fe350f0cd77` passes all10 serialization/mode/restoration cases. No lighting values change.

Attempt `0599104303d5454d8aa7ef48644ef2f6` stops before paid input because the controlled transfer occurs after the initial end-of-frame capture; one `yield null` resumes before the next normal FOV/render pass. The scene now waits for one actual end-of-frame capture before checking visible owners. Attempt `c5553f8956374ba7a53c35ae8b9d24fb` confirms the pre-frame owner is current, Render.Visible is true, presenter is current and represents it, but cell visibility is still false. After the normal frame all visual checks, exact native grab/pull/release, and submitted hauling cues pass (nine checks).

That third attempt then catches a separate observer input mistake: Space is not the game's wait binding. Its recorded `paidInputs=2` counts attempted inputs; only the east pull has a validated paid-clock receipt. The wait key is corrected to Period, matching InputHandler and the existing hunting observer, with all clock assertions unchanged. Neither setup failure is counted as a pathfinding baseline. All three attempts and their restoration receipts are retained. No render refresh, forced visibility, AI goal, NPC pause or invented action receipt is used to make the observer pass.


### Genuine native baseline and candidate adoption

With both observer fixes in place, run `b6c113bf59b24d58ad9b71c75e933204` produces the expected gameplay RED: nine checks pass, `ordinary_pursuer_fairly_uses_bypass` fails. The report's failure count2 is one failed check plus its fatal stop, not two gameplay bugs. All31 attempted inputs have real paid-clock receipts (one east pull and30 waits). The original player stays at `(38,12)`, the same beam remains at `(37,12)`, and the Marlback walks from `(33,12)` to `(36,12)` then stays there through the remaining28 waits. Its ordinary KillGoal remains above BoredGoal with the original player target; no competing behavior displaced pursuit. The read-only physical BFS finds a ten-step detour. Three actual NPC moves are unforced and legal. The baseline failure image shows the raised hedge/beam, pursuer on the west and player on the east; it is a controlled test area, not a new generated chunk.

After native teardown confirms original save root/preferences and clean Main scene, root adopts the exact one-file candidate. The before/after Assets snapshot differs only in FindPath.cs (`a1733476…` → `968ce054…`); the observer, input sequence, geometry, blueprint data and test files are identical for the comparison. The32-fixture request is extended with five observer/serialization/restoration fixtures for37 native fixtures total.

The independent Q1–Q4 source review finds no further significant issue. One historical evidence-label correction is retained: the frozen regression selection metadata omitted `SpreadHuntArrivalTests` from its “absent from F9” list, although the executable request already includes it. Seven selected fixtures were absent from F9, not six. This does not change the requested tests or their results.


## Final native acceptance

Native job `c9bb9366e93a4ba9a0eebe18ad7df6b9` passes735/735 in73.3413811s: zero failures, skips or inconclusive cases across37 selected fixtures. All45 original RED names are included and now pass; all47 new cases pass. This includes the30 native-only spatial cases omitted by the private runner plus10 observer/serialization/restoration controls. The gameplay source and all tests remain byte-identical after that run. Only controlled observer setup changes afterward, as documented next; its targeted10-case follow-up is recorded separately. This is a selected regression run, not a new full-suite result; prior F9/F11 full/selected evidence keeps its own limits.

### Paired movement evidence and staging correction

The first candidate `a0b3ba924cac492aa637cd3a48084105` uses the same observer, address, actors, local terrain and inputs as baseline `b6c113bf59b24d58ad9b71c75e933204`; source snapshots differ only in FindPath.cs. It passes all12 gameplay checks. After one pull and11 waits the pursuer makes12 legal, unforced moves, goes around the northern end at `(37,7)` and reaches `(38,11)` adjacent to the player. Both retain full HP (40/15), the same original beam stays at `(37,12)`, and normal grip/release cues and exact approved geometry are observed. Root inspected both actual failure/contact images.

However, that first candidate reports one unexpected error and `complete=false`: the controlled arena overwrote retained cache entry `Overworld.11.10.0`, so normal autosave correctly rejects the replaced exploration graph. This is an observer setup bug, not evidence that ordinary gameplay saves fail. The original paired reports are retained without changing their failure counts or presenting the candidate as a clean run.

The setup correction selects the first currently eligible, uncached, uninstalled Spread metadata entry whose family is None. It never calls GetZone or generates candidate content, changes plan dispositions, suppresses a save, pauses actors, or filters the traveller roll. In this fixed world it selects `Overworld.10.1.0`. Normal transition callbacks and autosave remain enabled; assertions prove all pre-existing cached graph references survive, exactly one staged graph is added, and the retained graph count remains1. The original player, local geometry, ordinary actors and input sequence are otherwise unchanged. This is a candidate-only follow-up, not a second matched baseline.

Clean follow-up `8e875c8e4c40495dab0b746ba3b248ce` completes in6.2377948s:12/12 checks, zero failures/unverified families,12 validated paid inputs,11 waits,12 legal unforced moves, northern-end bypass and adjacent contact, player/pursuer HP40/15. No entry actor was added. The isolated scene returns to clean Main with Play stopped, active observer/isolation cleared and original save-root/preferences restored. The transition's normal autosave does not emit an error; no save/load roundtrip is claimed.

| Attempt | Accepted evidence | Limit / correction |
|---|---|---|
| `2a3308b4…` | Recorder failure exposed recursive Unity Color | No paid input; scalar snapshot fixed and paired-tested. |
| `05991043…` | Visibility assertion ran before normal render | No paid input; end-of-frame timing corrected. |
| `c5553f89…` | Nine checks; real grab/pull/release and raised visuals | Space was wrong wait binding;2 attempted inputs but only1 paid receipt. |
| `b6c113bf…` | Genuine baseline stall,31 paid inputs | Expected bypass check fails;9/10 checks pass. |
| `a0b3ba92…` | Matched candidate reaches contact;12/12 checks | One controlled-cache autosave error; complete=false. |
| `8e875c8e…` | Clean candidate follow-up,12/12 checks,0 errors | Controlled uncached address; no ordinary journey or roundtrip. |

## Final self-review and practical limits

- 🟡 Resolved gameplay finding: actual noncreature physical owners remain blocking when a caller elects to ignore temporary creatures. Baseline actual input stalls; corrected input takes the open route.
- 🟡 Resolved observer findings: recursive report value, premature FOV check, wrong wait key and retained-cache replacement. Raw failed reports remain; corrected serialization counters, native inputs and cache guards establish the fixes.
- ⚪ Q1–Q4 complete: both creature-policy branches, fixed/movable props, doors, endpoint exception, body/contact paths, hazard costs and scheduled movement were read against their counters. The original navigation contract is preserved outside the one predicate. No external Qud-source parity is claimed.
- ⚪ No model/environment content change was needed to repair this behavior. The controlled arena uses the existing approved raised models. New environmental variants should use this corrected physical authority, not reintroduce a permanently stalled pursuer as their payoff.
- 🧪 The witness proves legal pursuit around a player-moved obstacle and rendered hauling cues. It does not prove a net escape advantage, generated-site balance, ordinary discovery, save/load persistence of this staged scene, player comprehension, general line-of-sight cover, or frame-time improvement. Existing hauling save tests remain green separately.
- 🧪 Next content gate: compare a proposed tactical hauling layout with its open-route counter, including the player's paid setup/movement and the unsuppressed pursuer. Ship it only if the manipulation creates a useful finite choice. Otherwise prioritize alternate access to an existing finite water source. Keep models and terrain available to make the action readable; avoid decorative variants without a new decision.

All implementation/test/metafile, controlled-mode, documentation and selected raw evidence changes belong to this one independently revertible milestone. Unrelated editor logs and older untracked artifacts are excluded.

Final targeted native observer job `5c2ac81c0ac4484c8192d8bdbb4ee003` passes10/10 in1.2982061s after the staging correction, zero failures/skips/inconclusive. These ten overlap the735 selected cases; they are not ten additional cases.
