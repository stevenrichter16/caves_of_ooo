# Home/base implementation record

Living record for the slices of [HOME-BASE-DESIGN.md](HOME-BASE-DESIGN.md) that are built. Direction (2026-11-17): design and architecture first; implementation follows the machine contracts
(`MACHINE-INSTALLATIONS.md` marker and retention, `MACHINE-STATIONS.md`, `MACHINE-ACTION-ROWS.md`). Nothing here is player-visible until a kit exists (PB2b); a player who declines has no new save footprint.

## Built

| Slice | What | Evidence |
|---|---|---|
| PB0a | A well-keeper's renewal also unloads an inactive village; the marker conjunct already keeps a marked one | `InstallationRetentionTests.WellKeeperRenewal*` (control + marked) |
| PB1 | `WaysitePlan.IsEligibleIgnoringFamily` and `IsFamilyOwned` extracted (`IsEligible` is their conjunction); `HeldPlaceZones.Classify` (7 classes, refusal codes) and `PlacementCheck.Evaluate` (10 ordered rules, caps by scan) | `HeldPlaceZoneClassTests`, `PlacementCheckTests` (63) with waysite and density suites (223) |
| PB2a | Same-entity set down, take up and hand over on the install state (spec S19), with the fixture definitions and the install events; no kit ships | `HeldFixtureActionTests`, `HeldFixtureAdversarialTests`, `PackableFixtureRegistryTests` |

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

### PB2a (with spec S19): set down, take up, hand over

`InstallablePart` (Mode Fixture/Portable, State Installed/Loose; a `Loose` state vetoes station readiness through the existing guard), `PackableFixturePart` (Kind, SetTick: the only Part home/base owns), `IRebindable` (a seam, no implementer yet), the
install events (`BeforeInstall`/`Installed`/`BeforeUninstall`/`Uninstalled`), `PackableFixtures.json` (definitions, validated per row, shipped empty: no kit yet) and `HeldFixtureActions`. Evidence: `HeldFixtureActionTests`, `HeldFixtureAdversarialTests`,
`PackableFixtureRegistryTests`, `PlacementCheckTests`, `HeldPlaceZoneClassTests` (123 together; with the saved-part contract and retention suites 175).

- **One transaction per verb, same entity.** A carried kit offers "set down" per legal adjacent cell in the pack menu; a standing, player-held fixture offers "take up" and "hand over" to an adjacent actor in the world menu. Rows declare `ActionTime.OneAction`
  (S3), so no edit to either legacy chain was needed: one action on commit, none on refusal, pinned through the real menus. Set down removes the kit from the pack, flips `Takeable`/`Solid`, sets the `Owned` tag, places the *same entity*, and attaches the install state and a
  marker (`Origin=Player`, `RetainZone=true`, `InstallationId=player:<id>`); take up and hand over reverse or release exactly that. Forced failure at any point (a veto, an overweight load, an outer failure after the command) restores the pack, cell, flags, tag and Parts.
- **A refusal says why in a plain sentence** (`HeldFixtureActions.Reason`); when no direction is legal, one disabled row carries the reason instead of eight rows or none. Strings are mechanical placeholders pending voice review.
- **Hand over** releases the marker and the tag and leaves the object standing with its contents; it returns the cap slot and needs no definition. **Take up of an unknown kind** refuses (`unknown-kind`), and the fixture stays retained and standing.
- **Hardening found while writing the sweep:** a creature beside the spot (the person setting it down) must not count as a plugged opening (neighbour checks are structure-only); ground is terrain-only so a wall on the cell reads as "something is already there"; a stack of kits is
  not one fixture; command coordinates are range-checked in 64-bit; the eight-direction menu scan computes the zone class and caps once (`PlacementCheck.PlacementBatch`); the validator flags a container kit without `ContainerLoad` because its contents would weigh nothing.
- **Weight honesty.** A box whose blueprint lacks `ContainerLoadPart` launders its contents' weight (pinned as a documented gap and flagged by validator code P13). The camp box (PB2b) must carry the part.

## Limits and next

- Family-owned zones stay refused until the exploration-family consumer audit (PB1a); `column-pending` until PB7. No kit, verbs or readers exist yet.
- Not done in this slice: the rig's `Origin="Relocated"` stamp on a committed crossing (the rig is retained by the legacy list already), the hand-over confirmation line (PB3), the readers and the derived list (PB3), a kit blueprint and its stock line (PB2b), and the live retention bench (PB4).
- Next: the camp box (PB2b), readers and derived list (PB3), then the live retention bench and actual play (PB4).
