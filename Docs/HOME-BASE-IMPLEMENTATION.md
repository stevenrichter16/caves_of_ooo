# Home/base implementation record

Living record for the slices of [HOME-BASE-DESIGN.md](HOME-BASE-DESIGN.md) that are built. Direction (2026-11-17): design and architecture first; implementation follows the machine contracts
(`MACHINE-INSTALLATIONS.md` marker and retention, `MACHINE-STATIONS.md`, `MACHINE-ACTION-ROWS.md`). Nothing here is player-visible until a kit exists (PB2b); a player who declines has no new save footprint.

## Built

| Slice | What | Evidence |
|---|---|---|
| PB0a | A well-keeper's renewal also unloads an inactive village; the marker conjunct already keeps a marked one | `InstallationRetentionTests.WellKeeperRenewal*` (control + marked) |
| PB1 | `WaysitePlan.IsEligibleIgnoringFamily` and `IsFamilyOwned` extracted (`IsEligible` is their conjunction); `HeldPlaceZones.Classify` (7 classes, refusal codes) and `PlacementCheck.Evaluate` (10 ordered rules, caps by scan) | `HeldPlaceZoneClassTests`, `PlacementCheckTests` (63) with waysite and density suites (223) |

### PB1 decisions

- **Saved data only.** The classifier reads the world map, plan tables and the zone's saved entities; the check never reads `GenReservedCells` (not serialised) and mutates nothing (pinned). A decision after a save round trip equals the one before it.
- **Zone classes** (first match wins): `poi-zone`, `protected-ground` (the opening, authored wilderness scenes, the pilot, regional-situation zones and the rest of `WaysitePlan`'s exclusions), `biome` (allow-list Spread, Sodden, Beating, Stump; Grovelands by Choir law and
  Overwrit excluded), `family-owned` (Spread exploration families), `wild-surface` (admitted). Every underground zone is `column-pending` until the column audit (PB7); non-overworld ids are `unsupported-zone`. A three-seed sweep reaches every class and checks that
  no admitted zone is Grovelands or Overwrit.
- **Extraction is a pure refactor** of accepted generation code, pinned by a seed sweep (3 seeds x every map cell) against a copy of the old function, plus the conjunction identity.
- **Ground** is a property predicate: walkable, at least one terrain object, none of them `RoadStone`, no liquid or gas pool, step trigger, flame or aflame occupant, and no heat, cold, charge, cloud or coating on the tile. Grass, Floor and StoneFloor (the rig's verified set) pass.
- **Other rules** in order: occupancy (terrain only; the fixture's own entity never counts), open neighbours (8 for a solid fixture, 4 for a bench), a 3-cell edge margin, doors/stairs/wells within 2, caps (8 per zone, 32 per world, counted by scan; only
  Player or Relocated markers count, so authored installations never use the player's allowance), fixture state (destroyed, equipped, dragged), and the class-specific ground class (`BareGround` refuses vegetation; an unknown class fails closed).
- **Dormant by design.** `protected-near` is not run (no protection capability exists at HEAD) and the result says so (`ProtectedNearChecked == false`) rather than claiming it checked.
- **Cost.** Scanning a 300-zone cache for held fixtures is an action-time cost, pinned under 250 ms in a synthetic test; it is never per frame.

## Limits and next

- Family-owned zones stay refused until the exploration-family consumer audit (PB1a); `column-pending` until PB7. No kit, verbs or readers exist yet.
- Next: S19 installable state and PB2a (the same-entity set-down / take-up / hand-over transaction), then the camp box (PB2b), readers (PB3) and the live retention bench (PB4).
