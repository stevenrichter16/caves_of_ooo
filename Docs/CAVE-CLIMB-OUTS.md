# Cave climb-outs — more ways up from ordinary caves

**Status (2026-10-29):** implemented and regression-tested in the verification copy; not yet seen in live play. CoO-original gameplay change (no Qud parity claim). User direction: "there needs to be more stairs up in the caves below the surface level."

## Problem (measured before the change, seed 64, row 6)

| Floor | Zones sampled | With no up-stair | Most up-stairs in any zone |
|---|---|---|---|
| 1 below surface, reached sideways | 8 | 7 | 1 |
| 2 below surface | 8 | 0 | 1 |

A cave floor's only up-stair came from a down-stair registered by the zone above (`StairsUpBuilder`). Underground zones also connect sideways at the same depth (`WorldMap.GetAdjacentZoneID` keeps Z), so a player who walked out of the cave they entered usually found no way up except walking back.

## Behaviour

- **Placement.** `CaveClimbOutStairsBuilder` (priority 3550, after the paired stairs and before `StairConnectorBuilder`) gives each horizontal third of an ordinary cave floor an up-stair if that third has none. Stairs go on open floor that holds no other stair. Each carries the integer property `CaveClimbOut = 1`.
- **Scope.** Only columns `UndergroundLayoutPlan` calls ordinary get climb-outs: no point of interest and outside authored areas, in any biome. Authored sinkhole floors, the Root chamber, lairs, catacomb villages and Morrowfast's column keep their own stairs.
- **Determinism.** The builder uses its own random sequence, seeded from the world seed and zone ID, never the pipeline's. Every later builder on the floor (population, hazards, crops, villages) generates exactly what it did before.
- **Climbing.** A climb-out leads to the zone above.
  - If that zone has any down-stair, the climber arrives at the nearest one, as with every stair.
  - If it has none, the climber arrives at the same spot, or the nearest open cell, and a down-stair opens there. It is registered as a `StairsDown` connection back to the climb-out, so the route works both ways. The message log says the way back stays open.
  - The `event/CaveClimbOutOpened` diag record has a `CaveClimbOutRejected` twin if the stair cannot be placed.
- **Unchanged.** Paired stairs behave exactly as before. An ordinary up-stair whose return is missing still refuses ("The return stairs are missing"; W6.7 rule).

## Tests

`CaveClimbOutStairsTests`:
- three or more up-stairs, at least one per third, on floors 1–3 of three ordinary columns;
- none on the surface, on authored sinkhole floors or under Morrowfast;
- identical positions for the same world;
- climbing opens exactly one way down and returns to the same climb-out, and a second climb reuses it;
- an existing way down is used and nothing new opens;
- an ordinary up-stair without a return still refuses.

The diag record is pinned. RED (stub phase): 7 failures, every floor showing 0–1 up-stairs.

Two existing tests were revised deliberately:
- `DensityUndergroundLayoutTests` now requires exactly one paired up/down stair and checks that every stair, climb-outs included, is reachable and safe.
- `DensityUndergroundRoutesAdversarialTests` selects the paired up-stair explicitly.

## Regression record (verification copy, command-line Unity)

- **Full run.** All 186 classes touching stairs, underground zones or vertical travel ran: 5,205 of 5,219 passed and 3 were inconclusive (existing allocation probes).
- **Environment failures.** Two failures came from fixture files absent in the copy.
- **Pre-existing failures.** The other nine fail identically on the committed sources without this change, in a baseline run of the same classes. They are three Felling sleep-menu cases, the FoundingVillage two-week bloom case, three Overwrit crop cases and the terrain glyph audit.
- **Order-dependent case.** One FoundingVillage case passes in isolation.
- **First run.** It used the pipeline RNG and had no column scope. It broke 25 tests across crops, a catacomb village and the Root chamber, which led to the dedicated seed and the ordinary-column scope.

## Limits

- Saves made before this change keep their cached cave floors; climb-outs appear on floors generated afterwards.
- A climb-out under a settlement with no way down opens its down-stair at the arrival cell. It is not composed into authored scenery.
- Not yet verified in live play.
