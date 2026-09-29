# Physical obstacles and fair pursuit

**Status:** private implementation ready after a test-first source/behavior audit. F11 publication and a native test-only RED remain prerequisites for shared production adoption. The scoped candidate passes43 focused cases and695 affected private cases; baseline and candidate separately compile against actual Unity runtime/full test references. Those compile checks are not native execution.

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

The one-file private candidate passes43/43. The scoped affected private run passes695/695, including the focused43. One selected native fixture, MultiCellSpatialAdversarialTests, requires Unity test-only decode isolation and was excluded from the private runner without fabricating a replacement scope. It remains in the32-fixture native request. Both baseline and candidate compile as separate actual CavesOfOoo and full EditModeTests assemblies. Native RED/GREEN and the player-input witness are still root-owned and pending at this draft.

## Self-review

- 🟡 Confirmed: ignoreCreatures bypasses real noncreature physical owners, stranding ordinary pursuit. The private minimal correction passes all focused and supported neighbor checks; do not mark shipped before native acceptance.
- ⚪ Corrected test premise: a wide body cannot occupy the player's target anchor. Four initial preflight failures are retained as rejected evidence and the proper contact-route control now passes.
- ⚪ Preserved limits: this does not make props block sight, grant enemy obstacle destruction, change diagonal rules or promise an escape outcome. Thin-prop fallback and existing door permissions remain pinned.
- 🧪 Pending release evidence: root native baseline/candidate test receipts and bounded actual player/pursuer/prop input witness. Reference compilation and private scheduler evidence cannot replace them.

Independent source peer review found no additional significant issue: only the physical filter changes; preceding structural/door gates and footprint/contact/hazard branches remain intact.

## Implementation log / proposed files

1. Freeze ten player-flow hypotheses before detailed read, then run eleven paired methods including the actual movement bypass counter. Retain initial and corrected receipts.
2. Verify all ten source/content hashes match the audited baseline; compile maintained tests against actual separate Unity references.
3. Implement the exact one-file private predicate delta, run43 and supported695, and retain source peer review.
4. Root publishes F11, adopts test-only baseline, executes native RED, then checks exact source preimage before candidate adoption.
5. Root runs selected32 native fixtures, a bounded real-input witness, and finalizes this living doc/evidence in the same commit using CLAUDE §2.3.

Proposed source/test files:

- `Assets/Scripts/Gameplay/AI/FindPath.cs`: respect noncreature physical props under the existing creature-omission policy.
- `Assets/Tests/EditMode/Gameplay/AI/MovableObstaclePursuitAuditTests.cs` and meta:43 paired planner/movement/scheduler/body/door/prop checks.
- `Docs/PHYSICAL-OBSTACLE-PURSUIT.md` plus retained verification evidence: this finalized living record, scope corrections and native/private results.
