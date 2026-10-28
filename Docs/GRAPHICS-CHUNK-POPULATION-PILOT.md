# Chunk population pilot: Sill's well-and-shop court

**Planned only, 2026-10-28.** The user has authorized recurring density work alongside environmental polish. This preflight selects one connected inhabited place. No generation, population, art, tests or Unity state changed. Root owns implementation sequencing, actual capture and publication.

## Choose a populated place, not more scattered objects

The normal views `Verification/Live16x24/shared-village-roles-after-01-world.png` and `sill-envelope-after-08-interior.png` show residents and real buildings separated by large lawns. The newer Sill material pass improves surfaces, not their arrangement. Compare `east-shelter-after64-use-approach.png`: the Spread approach also remains broad, but its forge/apron already received a deliberate grouping pass. Its quiet wilderness should not automatically become a second town.

**Build Sill's existing smithy and provisioner's shop into a small facing pair on the north approach to the existing well.** Players should see two actual occupants, counters and a working forge together, enter either shop, trade, then reach the well and the rest of town. The same five shops remain; the smith and provisioner are relocated during generation, not duplicated. This creates a legible inhabited center with useful services rather than another cosmetic scatter pass. Wider Sill yards remain a later scope.

## Source corrections and hooks

| Verified current source | Consequence for the pilot |
| --- | --- |
| `WorldMap.StartingZoneID` is **10.10**, although current playable glade starts at11.10. `OverworldZoneManager.CreateVillagePipeline`, around971/1060, gives Sill a large ordinary village and five town stamps. | Gate exact Sill POI/address/biome; do not mistakenly modify the glade or every village. |
| `VillageBuilder.BuildZone` creates6–9 random houses for largeTown, avoiding a12×6 central square; paths are Grass. `GetVillageSquareCenter` is40,12. | Make room for the court before house placement, not by bulldozing generated houses afterward. |
| `StampCatalog.Town` in `LandmarkBuilder.cs:97–150` already supplies TheSmithy (`Weaponsmith`, counter, `TinkersForge`) and TheLarder (`Provisioner`, counter), plus three other shops. `LandmarkBuilder.TryPlace` around1284 uses80 random anchors. | Offer a preferred Sill anchor/orientation for these two existing stamps; retain ordinary placement fallback and all other stamps. Reuse the existing shop creation/stock/restock path once. |
| River priority3850 precedes shops3860, ordinary doors3900 and population4000. `FootprintClear` rejects reserved cells, stairs and liquid. | Reserve the planned area from ordinary house placement separately; do **not** put future shop footprints into `GenReservedCells` before their own validation. Revalidate after river/entrance work. |
| `VillagePopulationBuilder.PlaceMainWell` anchors one actual well at40,12, with existing repair-site wiring/cardinal markers. Starting chest/crafting placements have their own offsets. | Preserve those positions and mechanics. Do not add another well, forge service, trade stock or shopkeeper. |
| `PreferredServiceCell` requires real interior/StoneFloor. It is not a generic outdoor resident-position hook. | Do not relax that shared validator or drag the Elder/Farmer/WellKeeper outside for this pilot. The two existing shopkeepers provide the focused population. |
| Shop `+` is an open gap, not a VillageDoor; `.` preserves ground and does not automatically set interior/weather shelter. | Keep truthful open shop thresholds; do not claim roof or new door behavior. |
| `ZoneManager.GetZone` returns cached graphs before generation (`Map/ZoneManager.cs:44–55`). | Freshly generated Sill only. No forced regeneration, migration or injecting the new court into an earned cached town. |

## Concrete first greybox

Use an authored, north-of-well arrangement so it stays clear of the southern river. Initial preferred coordinates, subject to source fit/counterchecks:

- **Smithy:** original6×4 stamp at31,4; its existing east opening is36,6. Actual smith32,5, counter32,6, forge35,5.
- **Provisioner:** horizontally mirrored original5×4 Larder at43,4; west opening43,6. Same role/stock marker, actual keeper46,5 and counter44,5. Mirror cell geometry, not semantic roles or contents.
- Keep the six-cell space37..42 between the fronts open. Two tiny actual StoneFloor aprons at37..38/41..42 by rows6..7, linked only at39..40,row7, give ten worn floor owners over bare Grass. Their existing flat Sill art is sufficient; do not imply a long new road or change `IsInterior`.
- Preserve a two-cell circulation spine around39..40 from row7 toward the well's existing north approach. Do not claim the well, its markers, the established chest or crafting offsets. Do not add arbitrary crates, barrels, loot, crops or extra services.
- Exclude these footprints plus a small approach margin from random-house admission before carving. This is a compact plan exclusion, not a new general settlement framework. Other houses and the remaining three shops keep their variation. Existing generic shop placement is the fallback if a preferred footprint is invalid; never clear stairs, water, reserved work, residents or existing objects to force it.

The court must remain conspicuously populated in the actual view: both shopkeepers inherit non-wandering/staying Villager brains, but ordinary reactions still run. Do not freeze AI or promise a schedule. Native Weaponsmith/Provisioner art already exists; MarketStall is still a glyph in the existing Sill fallback fixture. If actual baseline confirms that counter inconsistency, include **only that exact already-functional counter** in this scene's screenshot→imagegen→native-art work. Its text correctly says trading belongs to the keeper; do not make the prop a merchant or stock container.

## Test and ordinary acceptance plan

Before production, write a focused failing generation fixture: a fresh full Sill pipeline produces the facing pair, exactly one of each existing five shopkeepers, real themed inventory and one shop forge; both entrances, their counter/keeper/forge interaction positions and the well are reachable after all builders. Preserve household/service counts, main-well/chest/crafting locations and reserved door/stair routes. Compare at least seeds64,65 and existing town-test seed777. Pair with other-village/profile and invalid-POI gates, obstructed preferred-footprint fallback, missing blueprint no-partial-placement, and cached-zone/save-return controls. Do not pin random entity IDs or stock outcomes that are expected to shift when generation consumes RNG differently.

Reuse `StartingTownTests`, `BiomeLandmarkTests`, `VillageBuilderInteriorTests` and `DensityGeneratedDoorContentTests` / `VillageDoorInvalidationTests`; add relevant existing settlement/stock tests only where their path changes. Do not duplicate every atlas test: accepted Sill materials remain unchanged. Any new native counter needs its own exact-owner/fallback tests before integration.

**Actual baseline:** use earned `Fixtures/sill-environment-return178` (39,12,HP38,tick1280); the older comparison is `Fixtures/sill-envelope-after65` (67,18,HP38,tick1070). These contain cached old Sill and are before/unchanged-save evidence, not after candidates. For the after view, start a normal fresh seed64/65 Warden campaign in root's owned isolation, use ordinary world-map travel one cell west from glade11.10 into Sill10.10, and approach the real center. Confirm current healthy state and input before traversal; no teleport, reveal or map injection. Capture both fronts in one normal-zoom view, enter both, open real trade, inspect/use the forge affordance, reach the well, leave/re-enter and fresh Continue. A purchase is optional and only if affordable; stock UI itself must be real.

Acceptance is a noticeable populated court with coherent fronts and open routes, not a higher prop count. Preserve quiet space outside it. Record missing/failed placements and real AI/combat honestly. This pilot does not certify all Sill, all settlements, better FPS or Deck delivery. Root's parallel Overwrit material pass remains separate.
