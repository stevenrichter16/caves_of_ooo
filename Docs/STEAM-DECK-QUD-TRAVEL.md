# Safe local controller travel

Status: original 64 planner cases passed native Unity in the parent's 177-case focused suite. The menu reachability snapshot follow-up is implemented after native compile RED, and all 79 planner cases pass in the isolated runner; all 79 also passed native Unity in final job `ffbf73765b284474b9bfcd8ea01a80f4` (407 total). The native Play audit `e5dde5d513c84863a527c54330459975` passed all 39 checks, including a real paid edge-travel step and interruption. Independent review found no actionable issue in the planner or detached reachability addition.
CoO adaptation of the controller travel intent documented in
`STEAM-DECK-QUD-CONTROLS.md`, not complete Qud autoexplore parity. No autoloot,
automatic attacks, door operation, world-map travel or zone crossing.

## Contract and ownership

The new `CavesOfOoo.Core.ControllerTravel` is a pure local planner. Its step
queries return one adjacent step through out parameters, or false with both
outputs zero. It never moves an actor, fires an event, opens a door, alters
visibility, advances a turn, generates a zone or retains a travel session.

```csharp
bool TryStepTowardEdge(Zone zone, Entity actor, int dx, int dy, out int stepX, out int stepY);
bool TryStepTowardFrontier(Zone zone, Entity actor, out int stepX, out int stepY);
bool TryStepTowardPoint(Zone zone, Entity actor, int x, int y, out int stepX, out int stepY);
bool HasDanger(Zone zone, Entity actor);
ReachabilityMap BuildReachable(Zone zone, Entity actor);
```

The parent owns execution: actor/zone identity, HP changes, user interruption,
repeat cadence, cancellation, movement vetoes and repeated no-progress. It must
re-query before every step and stop rather than attack or operate a barrier.
The planner cannot predict arbitrary movement-event vetoes without executing
gameplay callbacks. Cancelling means ceasing to request steps; no retained path
or cancellation API is necessary.

## Verification sweep

| Premise | Verified correction |
| --- | --- |
| Existing FindPath is directly suitable | It allows hidden cells, finite-cost hazards, operable doors and a special goal-cell admission path. Local safe travel needs its own bounded admission predicate. |
| Cell.IsSolid is the movement rule | Physics-only props also block. Use whole-body collision checks; do not ignore creatures or allow doors. |
| Checking the anchor checks a wide actor | SpatialFootprintPart can project far from its anchor and omit the anchor. Every candidate body cell must be in bounds, known, unblocked and safe. Stale placed footprints fail closed. |
| Diagonals require both cardinal sides clear | Existing FindPath/Zone wide-body rules reject a diagonal only when both cardinal placements are blocked. The planner uses that rule with safe, known whole-body placements. |
| Navigation weight rejects hazards | TerrainNavigationWeight.ForStep is a finite preference. Safe travel rejects any positive result and separately checks actual fire and step triggers. |
| Existing hazard queries read only the target | SteamContact examines adjacent cells. Parent approved a conservative exception: an unseen adjacent hot steam source may stop travel, without identifying or revealing it. No existing terrain API is changed. |
| Nearby tile energy is always damage | Tile heat/cloud state is reaction data. Use the existing actual-contact query; do not invent damage from unused energy values. |
| Any hostile anywhere should stop travel | This leaks hidden enemies and makes convenience travel unusable. Only currently visible living hostile creatures stop planning; passive creatures without personal hostility are not proactive threats. |
| WorldMap is an ordinary zone | WorldMap.IsWorldMapZoneID has explicit identity semantics. Refuse this planner there. |

Read: CLAUDE.md; PERF-FOUNDATION.md; controller plan; FindPath,
TerrainNavigationWeight, GasNavigationWeight, SteamContact, MovementSystem,
PhysicsPart, Zone/Cell, OccupiedCells, SpatialFootprintPart, StairTravel,
FactionManager/BrainPart, BurningEffect/ThermalPart, TriggerOnStepPart and
TrapJammingPart. No Unity tools are used by this agent.

## Search semantics

Use a bounded eight-neighbor BFS with no more than `Zone.Width * Zone.Height`
anchors. Read a candidate's visible/explored flags before inspecting collision
or hazard contents. Arrays and query-local safety results may be reused inside
one call; do not cache across calls because public fields and occupants mutate.
Stable neighbor ordering prefers the requested heading (or point direction).

Point travel requires a known safe destination and returns false when already
there. Frontier travel chooses the nearest reachable known safe cell next to an
unexplored, nonvisible cell; that neighbor's terrain is never inspected. Standing
at a frontier is a stop, not permission to step blindly into it. Edge travel
seeks the requested boundary; a diagonal stops at either requested boundary.
If the edge remains unknown, approach the furthest safely reachable known cell
in that heading, then stop until new knowledge exists. The actor's complete
body determines whether a boundary has been reached.

Danger includes invalid/dead/detached actors, unsafe current contact and visible
living hostile creatures. Candidate admission rejects closed/locked barriers,
physical occupants, positive terrain/gas cost, actual burning/aflame owners and
active step triggers. Jammed supported traps and faction-exempt triggers are
countercases. Queries must not invoke movement events or virtual effect vetoes.

## Test-first milestones

1. Authored 27 core and 37 dedicated adversarial cases (64 total): eight directions, known point/edge/frontier semantics, detours,
   walls, doors, gas, fire, traps, threats and no-op outputs. Missing production
   type supplies the initial compile RED; parent records the native result.
2. Implement only after RED release, then native GREEN.
3. Dedicated adversarial checks: invalid contexts, hidden-state counterchecks,
   footprints, diagonal squeeze, stale bodies, mutable occupancy/hazards,
   purity/repeatability and finite enclosed search. Parent owns integration and
   native controller execution verification.

## Self-review and evidence

- 🟢 Parent executed native compile RED before this planner existed; receipt `Verification/SteamDeckQudControls/travel-red-compile.txt`. Missing ControllerTravel prevented production integration and test-assembly compilation. The same receipt also contains an unrelated menu-access error owned by the parent; it is not an assertion-level test failure receipt.
- 🟢 Isolated runner passed all 64 planner cases, zero failed/skipped; receipt `Verification/SteamDeckQudControls/controller-travel-offline-green.xml`. This compiles actual gameplay source with runner stubs; it does not replace native Unity or controller integration verification.
- 🟢 Parent reports all original 64 planner cases passed in the focused native 177-case suite. Controller integration verification remains parent-owned.
- ⚪ Conservative unseen-adjacent-steam rejection is intentional and does not
  reveal, name or target the hidden source.
- ⚪ One-zone planning and explicit input execution are deliberate scope limits.
- ⚪ Physical Deck feel and complete Qud travel parity are not established by
  pure planner tests.

Files: new `ControllerTravel.cs`, `ControllerTravelTests.cs`,
`ControllerTravelAdversarialTests.cs`, their fresh script metadata and this doc.
Production followed the parent's recorded RED release. No commits or staging by this agent.

Implementation uses per-query bounded scratch arrays and one cached safety classification per anchor. There is no cross-call route/admission cache. All unknown body cells are rejected before collision/terrain inspection. Known neighboring fire is also avoided because active flames emit heat to their perimeter. Shared collision helpers may populate existing derived geometry caches; planner purity means no gameplay state, events, turns, exploration or entity movement is changed.


## POI menu reachability snapshot follow-up

Root's menu initially called point planning for up to nine neighbors of every
visible point of interest. Each query repeats danger admission and BFS. Add
`ControllerTravel.BuildReachable(Zone, Entity)` returning a public nested sealed
`ReachabilityMap` with `bool CanReach(int x, int y)`. One bounded traversal builds
private detached bits, allowing constant-work coordinate checks during one
synchronous menu construction. Unsafe/invalid contexts return a nonnull empty
map. The current actor cell is reachable by a zero-length path; it still does
not produce a movement step from `TryStepTowardPoint`.

The map has no live graph references and no cross-build mutable backing array.
It describes only the world at construction and becomes invalid for gameplay
after any actor/world/visibility mutation. It is never used to execute travel:
every actual step continues through fresh admission. It does not reveal unknown
cells, open barriers, run effect callbacks or change the simulation.

Tests precede this API: invalid/outside coordinates, unsafe context, agreement
with point admission, snapshot independence after mutation, per-build ownership,
whole-body/diagonal admission, gas/fire/trigger safety and query purity. Expected
compile RED was the missing `BuildReachable` method. The parent executed native
compile RED with CS0117 in the new test calls (including lines 327, 339 and 346),
then released implementation. Fifteen new cases bring the planner total to 79.
The API now runs the same BFS to completion once and transfers only its private
visited bits into the detached map; no live graph reference is retained. All 79
planner cases passed in the isolated runner, zero failed/skipped; receipt
`Verification/SteamDeckQudControls/controller-travel-reachability-offline-green.xml`.
This checks actual gameplay source with runner stubs, not native Unity execution
or controller integration. The parent owns native follow-up verification.

Independent HUD-agent review of the original planner and initial 64 tests found
no actionable safety issue in known-cell/whole-body admission, diagonal rules,
query purity, scratch ownership or bounded traversal. A separate review of the
map addition also found no actionable issue: the new goal exhausts the bounded
queue through identical admission rules, cannot take an early goal return or
edge fallback, and retains only its owned visited array. Bounds checks and the
empty singleton are safe; actual stepping remains a fresh query.
