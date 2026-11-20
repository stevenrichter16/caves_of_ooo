# Machine installations: marker, retention, registry, placement

Living record for slices S1, S2, S6 and S7 of the machine architecture spec (`MACHINE-ARCHITECTURE-SPEC.md`, copied from the
session design `ARCHITECTURE-SPEC.md` on acceptance). Standing direction: compose machines from Parts and shared contracts, put
usable machines in every town and village, keep ordinary convenience, keep saves additive. The user's twelve-machine pitch is in
[MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md). The home/base system will build on the same marker and retention contract.

Date: 2026-11-18. Owner of Unity/Git: the Claude session (see the execution plan). Decisions below were taken under the user's
instruction to proceed without intervention; each is recorded so it can be reversed by deleting a data row or one conjunct.

## What shipped (native-accepted; actual play and Mac package pending)

| Slice | Delivers | Where |
|---|---|---|
| S1 | Removing, renaming, moving or retyping a saved Part or field is a failing test | `Assets/Tests/EditMode/Gameplay/Save/SavedPartContract*.cs`, `SavedPartContract.txt` (385 Parts) |
| S2 | A cached village that holds a retaining marker or a pinned legacy finite fixture is not unloaded for regeneration after a settlement repair relapse | `InstallationMarkerPart`, one conjunct in `OverworldZoneManager.CanUnloadZone` |
| S6 | Validated installation specs, 14 error codes, and an every-town census with a ratchet | `InstallationRegistry`, `Resources/Content/Data/Installations.json` |
| S7 | A generic, optional, deterministic, fresh-only post-builder (priority 4650) and the first three placements: a working forge in Gantry, Tine and Slip (a fourth row, the Last Counter, was pulled to Pending, see Decision 1) | `InstallationPostBuilder`, one line in `CreateVillagePipeline`, three data rows |

## S1: the saved-Part contract

Saves persist each Part's assembly-qualified type name and reflect public fields by name. An unresolvable type name returns null
without consuming the Part's body, desynchronising the rest of the entity's stream, so a removed, renamed or moved Part (or a
retyped field) corrupts later saves. `SavedPartContract.txt` records every Part and its serialised fields (reflected or custom) at the
time it was generated. Tests fail when a golden Part or field disappears, changes type or flips between reflected and custom
serialisation; additions are never violations; every Part added after the golden must pass a hygiene lint (namespace
`CavesOfOoo.Core`, public parameterless constructor, serialisable public fields, no field named `ZoneID`, `StationX`, `StationY` or ending
in `ZoneID`/`ReceiverID`, because positional authority is what makes the kitchen family non-relocatable).
Regenerate for an **addition** with `COO_REGENERATE_SAVED_PART_CONTRACT=1` and the `GenerateSavedPartContract` test (Unity's batch
filter selects `[Explicit]` tests when their class is named, so the environment variable is the real guard). Regenerating to hide a
removal needs the owner's approval. Custom-serialised Parts are covered by name only; their wire format is covered by the existing save tests.

## S2: marker and retention

Problem (verified by `RelapseRegeneratesUnmarkedVillage_Control`): a village whose settlement repair relapses is unloaded and
regenerated from the seed on the next visit, which resets any finite machine state in it. `InstallationMarkerPart { InstallationId,
Origin, Revision, RetainZone }` plus `InstallationMarkerPart.Retain(manager, zoneID)` close that for everything that carries a retaining
marker with `RetainZone` (every player set-down sets it) or `Origin="Relocated"`, and three shipped finite fixtures that cannot take a Part because
their validators pin the Part list (`PeatOilStrainer`, `AbandonedFieldWorkRig` wherever it is hauled, `CinderholdOilRecoveryTray`).
Policy: retention is per installation kind. A stateless forge regenerates identically (so a retained village is not required and the
town keeps refreshing its build-time stock rolls); a state-bearing kind sets `RetainZone`. Consequence disclosed: a retained village no
longer re-rolls build-time stock after a relapse. Diag `worldgen/ZoneRetained { zone, reason: marker | legacy-blueprint, count }`.
RED proof: with `OverworldZoneManager.cs` restored to HEAD, 11 of the 22 retention tests fail (every marker case) while the controls pass.
Second unload source (home/base PB0a): `AdvanceSettlement` also reports a change when a well-keeper who knows the purification rite renews the well on arrival, and the caller unloads the inactive village for that too. Only the
starting village and Wellmeet track a main well, so only they can reach it. `WellKeeperRenewalRegeneratesUnmarkedVillage_Control` proves the hazard (the renewal ran and the unmarked starting village was regenerated) and
`WellKeeperRenewalDoesNotUnloadAMarkedVillage` proves the one `CanUnloadZone` conjunct keeps a marked one through it. No production change was needed.

## S6: registry, validator, census

`Installations.json` holds `Installations` rows (specs), `Exemptions` (a place that never receives a machine, with a source) and `Pending`
(a place with an owner). Per-row dropping: one bad optional row never cancels the others (unlike the process catalogue). Error codes
E01 to E15 each have a test and a valid-neighbour control (E15: a row's `Site` is not a repair site of its place). A row with a `Site` places the town's own working object, the body of one repair site (the Cinderhold mine pump); it is stateful so it needs Retention Zone, it is not a row of the every-town census, and the placement sets the part's `Place` and `SiteId`. The census classifies the 17 authored places and the three catacomb villages
as exactly one of Installed, Present (Sill and Cinderhold already hold a forge), Exempt (with a source) or Pending (with an owner), and a ratchet
test lets Pending only decrease. Current census: Installed 3 (Gantry, Tine, Slip), Present 2, Exempt 5 (the First Tent, the Quiet's Door,
Olderdeep, Lampwell, Spivenor), Pending 10 (the Last Counter joined Pending, below). "A machine in every town" is complete when Pending is 0.

## S7: generic placement

Algorithm per row, in data order, deterministic: authority check (fresh zone only; a cached, active, foreign-factory or wrong-place zone is a
silent no-op, not an omission) -> replay guard -> blueprint admission (no creature, trader, stack, footprint or takeable; a state-bearing blueprint
needs `Retention: Zone`) -> anchor (first resolvable `Near` owner, lowest y,x; `Well` is always the last fallback, and every shared-pipeline village
has one) -> candidate cells ranked by distance, then an FNV-1a hash of `zoneId:install:id:x,y`, then y, x, probing at most `MaxProbe` -> a cell must be
exterior, dry, unreserved bare ground holding exactly one object from the row's `Ground` list (never `RoadStone`, the public route), with no door, stair or well
within one cell; a solid blueprint must also preserve every public route and leave a reachable neighbour to stand on -> the entity is created exactly once ->
the marker and `SettlementId` are added and the whole placement is committed as a receipt (every original unchanged, tile state and reservations unchanged,
entity count +1) or rolled back. The builder never reads or advances the pipeline rng and never returns false (a false would clear and retry the town).
Diag `worldgen/InstallationPlaced`, `InstallationOmitted { reason }`, `InstallationRejected`.

Old worlds: no exploration-version gate and no `SpreadExplorationPlan.CurrentVersion` change (bumping it without adding 17 to the restore list would make every
existing world throw on load). A town in an old world that has not been visited yet receives its forge on first visit, as the Cinderhold packets do; a town
already generated and cached never does. Entity IDs shift by one after a placement, so tests compare blueprint and position, never IDs.

Reservation of the standing cell (spec step 8) was dropped: priority 4650 is last in every village pipeline (pinned by `VillagePipelinePriorityTests`), so nothing
generated later can use the cell, and `GenReservedCells` is not saved.

### Decisions

An independent critique of the placement plan (session reports `critique-canon-player.md`, `critique-feasibility.md`) and the plan itself prefer
purpose-qualified rows only, because a placed machine is permanent in every save that visited its town. Against that, the owner's standing instruction in this
session is to put machines in every town and village. The recorded resolution:

1. **Shipped rows: Gantry, Tine and Slip (communal convenience forges, owner instruction).** The rows say so in their `Basis` ("convenience, not a gap") and are plain data: delete a row
   to revert it before release. They are also the three-pipeline test matrix (a composed crossroads pipeline with preferred service cells, a composed lakeside pipeline with wet
   neighbours and the fastest relapse, and the plain `VillageBuilder` pipeline). This is a Claude decision under the instruction to proceed without intervention, not an owner
   answer, and is flagged for the owner to confirm or reverse.
   **The Last Counter row was shipped, then pulled (2026-11-18).** It was the one row with a local problem (its supply post issues a finite weapon-component kit and the zone has no
   forge), and Editor play showed it working. The full EditMode comparison then found `ThresholdCampsRenderingTests.EveryVisibleNativeOwnerAcrossTwoChunksHasAReadOnlyModel`
   failing in zone `Overworld.18.18.0` on seeds 64, 1729 and 729490642: the placed `TinkersForge` is an unmodeled native blueprint in the 3D spawn-ring voxel-kit library
   (`LastCounterVoxelKitLibrary` has no `TinkersForge` family). That ratchet is a correct guard, so the row now sits in `Pending` with the library gap as its owner, and the row is
   recoverable from git history (last present at commit `4595107e3`, id `last-counter-supply-forecourt-forge`). Restoring it needs a forge family in that library, which is a 3D-art task outside this program; the placement code, retention and
   save paths are unaffected.
2. **Tally and Wellmeet are Pending, not placed.** Tally's verified window is a `RoadStone` apron the generic builder never uses (needs a window-membership probe over 24 seeds
   and S24 plan-reserved frontage, plus a characterisation of rented loaners in the forge picker). Wellmeet is a Tent-Right camp: "the well and the salt-work are the fixed
   points; everything else moves", so a fixed smithy conflicts with canon; its owner is a portable lent rig after the installable-state slice (S19). Water is never sold.
3. **`MinRadius`** (default 2, data-only) keeps the ring around a square's centre open: ConcernWorld lands documents and guests on the first open cell in rings from the centre.
4. The forge's shipped description ("Fitting a blade, haft and binding here makes a working weapon") overstates what a forge is for, since a single weapon can be assembled anywhere
   from the pack; correcting that shipped text, and a truthful "works here" reader line, are separate small owner decisions (spec slices S5 and S13a), not done here.
5. **Honest census.** With the dossiers' purpose audits, most places have no honest machine opportunity yet (they need a finite find, a resident supplier or a local problem ahead of
   the apparatus). Installed 3, Present 2, Exempt 5, Pending 10 is the real state; "every town" is the ratchet, not a claim.

### Limits (stated, not hidden)

- Exterior placement only; plan-reserved in-room placement (a forge in a designed workshop room) is a later slice (S24).
- The forge draws as a glyph in non-admitted zones, as the Cinderhold forge does today; terms are unenforced, as at Sill and Cinderhold.
- Old, already-generated towns never gain a machine. Linux and Steam Deck are unexecuted. No packaged performance claim.
- Morrowfast (a fixed authored scene) and the three catacomb villages need their own hooks; they are Pending or Exempt in the census.

## Evidence

Editor actual play (2026-11-18): Gantry, Tine, Slip and (before its row was pulled) the Last Counter each showed the forge beside the interact menu; see `Verification/MachineSystems/InstallationsActual/observations.json`.

Native: S1 18 (17 + 1 environment-guarded generator), S2 24 retention cases (11 fail on HEAD production), S6 56 (registry, census, pipeline priorities), S7 placement cases, MinRadius and shipped-row sweeps; installation suites pass together. The full EditMode comparison and the actual-play checks are recorded in the execution plan continuation.
