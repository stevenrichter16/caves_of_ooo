# Release baseline triage — exact RS28 failures

Read-only audit, 2026-09-17 local / RS28 ended 2026-09-18 01:50 UTC. No code, test, scene, asset, or Unity changes were made for this triage. Primary evidence: `/Users/steven/caves-of-ooo/Docs/Verification/VoxelWorld/RS28-final-full/receipt.json`.

RS28: **14,901 total; 14,869 passed; 32 failed; zero compiler errors**. These are the exact inherited baseline failures, not new regional-situation regressions. “Baseline” does not make them acceptable release results. They represent **three work groups, not 32 independent engine defects**.

| Group | Failed cases | Classification | Player consequence / confidence |
|---|---:|---|---|
| Authored profile and Sill start-table pins | 2 | Confirmed stale test expectations, plus stale nearby comments | Current profiles and configured spawn are intentional newer behavior; do not revert production to satisfy the old assertions. |
| Seven ground-equipment body families | 29 | Confirmed unfinished GA03j feature; legacy presentation-specific requirements but a real surviving fallback identity problem | Mace resolves to a vial, footwear/headwear to torso armor, and selected weapons to one generic blade. Missing art/registration blocks many later assertions; this is not proof of 29 visibility/tint/lifecycle bugs. |
| Building-block variant fingerprint | 1 | Confirmed installed-art contract failure; source/import provenance unresolved, likely unfinished rebuild/import rather than a new simulation bug | Six families have fewer than four imported mesh+UV fingerprints. The older kit is documented as art/prefabs, not shipped player construction. |

## 1. Correct the two stale design pins without changing the world

### PlaceProfileTests.TheProfileTable_IsData_AndComplete

`Assets/Tests/EditMode/Gameplay/World/PlaceProfileTests.cs:205–228` recognizes only seven old profiles and demands that every other named village be plain. It fails first on Morrowfast and therefore masks four further outdated expectations in the same loop.

The actual table at `Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:209–228` contains **12 profiled and five plain villages**. Retain the old seven and add the five deliberately authored entries:

- Morrowfast → `Morrowfast`.
- Gantry → `CrossroadsExchange`.
- Tine → `LakesideVillage`.
- Quillhold → `PrimaryArchive`.
- Tally → `CentralExchange`.

The five plain controls remain Sill, Posy, the Salt-Vault, Slip, and the Quiet’s Door. Repair the expected table and retain the exact profile/null counter-check. Do not remove the “all table entries checked” invariant or make arbitrary nonempty profiles acceptable. Existing profile-save and per-composition runtime-authority tests should accompany the focused run. Nearby `WorldMapAuthoring`/`WorldGenerator` comments also need truthful historical wording.

### WorldMapAuthoringTests.Sill_IsAtTheCentre_OnSpread_AtTierOne

`Assets/Tests/EditMode/Gameplay/World/Map/WorldMapAuthoringTests.cs:84–95` asserts Sill is `Places[0]` because “the start reads Places[0].” That premise is false today. Sill still occupies (10,10), Spread, tier 1, but Morrowfast is first in the table.

Crucially, **the current serialized gameplay scene starts at `Overworld.2.6.0`**, as requested in the earlier spawn change: `Assets/Scenes/Main/SampleScene.unity:516`. `GameBootstrap.FreshGameZoneID` has a code default of Morrowfast (`.../Presentation/Bootstrap/GameBootstrap.cs:25`), but `GenerateStartingZone` actually reads the scene-configured field (`:1318–1322`). It never selects `Places[0]`. `WorldMap.StartingZoneID` remains Sill (`.../World/Map/WorldMap.cs:82`) and still gates older Sill starter stock in `VillagePopulationBuilder`; it is not the current scene spawn selector.

Repair the Sill test to use `WorldMapAuthoring.PlaceAt(10,10)`, retaining its name/biome/tier assertions. Add or retain a separate configured-bootstrap spawn invariant. **Do not move the player’s spawn, reorder the authored table, or globally rewrite `StartingZoneID` as part of this correction.** Update the old comments claiming Sill is first/start (`WorldMapAuthoring.cs:199–200`, `WorldGenerator.cs:67`).

## 2. Finish or explicitly replace the unfinished ground-item presentation contract

Source proof is unusually clear: `Docs/EQUIPMENT-GROUND-SPRITES-PLAN.md:125` records the original **32 cases / three passing controls / 29 RED** and says production assets/mappings were not yet implemented. That is the same failure group still present in RS28.

All seven promised PNGs are absent under `Assets/Resources/Sprites/Environment/`:
`item_dagger`, `item_sword`, `item_spear`, `item_boots`, `item_gloves`, `item_helmet`, `item_mace`.

The renderer confirms missing integration:

- `EnvironmentSpriteRenderer.cs:907–914` only preloads the older 12 item-body families.
- `:1874` onwards has no exact equipment aliases for these ten blueprints.
- `:1779–1784` maps `/` weapons to `WeaponGround`; `:1790–1807` maps remaining `[` items to `item_armor` and remaining `!` items to `item_vial`.
- The early item-family lookup at `:1679–1681` falls through if an expected body is unregistered. The planned missing-resource guard has not been reached by the seven adversarial removal tests: they currently stop at their explicit “must be registered first” precondition.

Exact original scope is ten native blueprints: Dagger; ShortSword/LongSword; Spear; LeatherBoots/IronshodBoots; LeatherGloves; LeatherCap/IronHelmet; Mace. Their inventory/combat behavior is not implicated. Worn equipment is a different subsystem (`Village3DEquipmentViews.cs:195–217`); these tests do not measure it.

**Recommended release decision:** because the sprite fallback remains shipped, complete the small bounded fallback repair (seven neutral silhouettes, ten exact aliases, startup preload, exact missing-body guard), preserving all three existing passing controls. This does not disable or replace voxel rendering. Match the current user preference for quiet colors; keep these as fallback assets. Real voxel dropped-equipment coverage is a separate art/presentation milestone, not a claim these tests establish.

If the release deliberately retires sprite-mode equipment, explicitly replace these PNG-specific expectations with tested voxel/fallback ownership requirements and document that product decision. Merely deleting/ignoring the 29 RED tests would leave the mace-as-vial behavior and the unanswered fallback contract. Do not call all 29 stale just because the main art direction changed.

Execution order inside this submilestone:

1. Preserve the current failing receipt and reproduce the 32-case fixture on the release source. Its three control cases must still pass.
2. Author/import the seven real assets and add exact mappings/preload. Retain a run before the missing-resource guard so the seven registration-removal cases reach their intended behavior rather than a setup assertion.
3. Add the exact missing-body guard, leaving honest original glyph/tint when an asset is absent. Do not let a missing mace body borrow vial art or missing boots borrow torso art.
4. Run the complete equipment-ground fixture, existing environment rendering/harness tests, exact-blueprint fallback controls, and native pickup/drop/full+dirty repaint acceptance at normal gameplay scale. Keep fog tests meaningful even though the user's current reveal preference is on. Palette, GUID/import, missing-resource, owner destruction and tint are separate checks.
5. Profile the touched per-cell renderer in the native 60–90-second workflow required by CLAUDE. Art identity/visibility must be judged in captured native views; test names alone do not prove recognizability.

## 3. Verify source/import freshness before rewriting building art

`BuildingBlockImportedArtTests.cs:50–69` requires four distinct imported vertex+UV fingerprints per family. RS28 reports:

| Family | Distinct imported fingerprints / required |
|---|---:|
| plank-floor | 3 / 4 |
| roof-ridge | 3 / 4 |
| roof-slope | 3 / 4 |
| stone-stair | 2 / 4 |
| timber-brace | 3 / 4 |
| wooden-stair | 1 / 4 |

The other 12 families pass this fingerprint contract. That leaves nine duplicate slots across six affected families, not six missing FBX files. Existing count, cell bounds, independent prefab children and GUID tests are not in the RS28 failure list.

There is concrete source/import drift to investigate before new art work:

- A read-only byte comparison found **all 72** `ArtSource/BuildingBlocks3D/build/models/*.fbx` differ from their corresponding installed `Assets/Art3D/BuildingBlocks/Models/*.fbx` files. FBX metadata can change bytes, so this is **not by itself proof of geometry differences or a fix**.
- Current generator source already adds variant-specific brace peg positions (`ArtSource/BuildingBlocks3D/build.py:68–70`), wooden stair pegs (`:76–82`), and stone chisel placements (`:83–84`). `Docs/Verification/BuildingBlocks3D/B05-final-variants.log` records a later 72-model export. The stored Unity variant receipt is still RED (`U05-variant-red`).
- Current static exporter bakes each piece’s transform into assembled vertices (`ArtSource/SpawnRing3D/mesh_kit.py:151–186`), so dismissing these failures as “the test forgot child translations” is not warranted for a current regenerated static export. The fingerprint does not include every possible material/normal property; validate any proposed fixture correction against actual imported geometry rather than weakening the uniqueness assertion.

Next concrete action: in the release validation clone, run the explicit asset importer against the current validated source, preserve GUIDs, then run all five imported-art tests. If duplicates remain, modify only affected generation rules and regenerate, rather than hand-moving demo geometry. Keep the requested coarse/two-color visual direction; do not “fix” uniqueness with noisy paint or imperceptible one-bit changes. Re-run source geometry/roundtrip contracts, imported bounds/collision controls, four distinct useful variants, and a readable contact sheet.

`Docs/BUILDING-BLOCKS-3D.md` explicitly separates this asset kit from future native block placement/destruction and player construction. A bounded production C# search found no current runtime references to this kit’s path/family IDs; the importer and editable BlockHouse assembly use it. Do not describe this failure as a broken building mechanic or claim that importing the kit implements construction.

## Proposed first release phase

1. **R1a: truthful baseline pins.** Correct the two obsolete table/start assumptions and associated comments, with no world/profile/spawn changes. Focused old and new native-scope tests, then a recorded count reduction.
2. **R1b: finish dropped-item fallback identity.** Highest direct player-facing defect in this failure set. Own the bounded seven-family content/renderer surface; capture real missing-resource RED, independent review, normal-scale native evidence and profiling. Can run in parallel with R1c if file ownership is separate.
3. **R1c: reconcile building-kit source/import state.** Import-first diagnostic, then generator correction only where the actual imported test still fails; preserve native scene and ownership scope.
4. **R1d: clean release baseline.** One fresh full compile/test run, zero ignored/excluded failures, exact changed-case accounting, then independent cold-eye and a short real input smoke journey covering save/load, transition, pickup/drop and the newly integrated regional C-actions. Do not use the old unchanged-32 comparison as the success criterion for this release phase.

Scope honesty: this audit classifies these exact 32 failures only. It did not launch Unity, rerun the suite, render the building kit, compare actual gameplay screenshots, prove every unexecuted branch in the 29 failing tests, or certify the whole game release-ready. It found no RS28 compile failure and no failed simulation/inventory regional cases in this list. Broader release content/performance/packaging defects require their own gates.

## Exact RS28 failed-case inventory

1. `BuildingBlockImportedArtTests.Variants_DifferInGeometryOrPaint_NotJustFilename` — R1c — installed variant contract.
2. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AllTenExactBlueprintsReachTheirRealResourcesSpriteAtUnflippedRow` — R1b — unfinished equipment fallback.
3. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_dagger")` — R1b — unfinished equipment fallback.
4. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_sword")` — R1b — unfinished equipment fallback.
5. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_spear")` — R1b — unfinished equipment fallback.
6. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_boots")` — R1b — unfinished equipment fallback.
7. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_gloves")` — R1b — unfinished equipment fallback.
8. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_helmet")` — R1b — unfinished equipment fallback.
9. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport("item_mace")` — R1b — unfinished equipment fallback.
10. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_BrightAndDimForegroundCarryHueAndAlphaWithoutAuthoredColorOverride` — R1b — unfinished equipment fallback.
11. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_DirtyNeighborhoodPreservesDistantEquipmentClaimAndTint` — R1b — unfinished equipment fallback.
12. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_DisablingSpriteModeRestoresEquipmentGlyphThenReclaimsOnEnable` — R1b — unfinished equipment fallback.
13. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_HigherLayerActorHidesGroundEquipmentUntilItLeaves` — R1b — unfinished equipment fallback.
14. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_IncrementalReplacementUsesNewGlyphColorAndBody` — R1b — unfinished equipment fallback.
15. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_InvisibleHigherItemDoesNotHideVisibleLowerItem` — R1b — unfinished equipment fallback.
16. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_ItemRemovalExposesFreshUnmappedGlyphWithoutRestoringOldOne` — R1b — unfinished equipment fallback.
17. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("Dagger","item_dagger")` — R1b — unfinished equipment fallback.
18. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("ShortSword","item_sword")` — R1b — unfinished equipment fallback.
19. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("Spear","item_spear")` — R1b — unfinished equipment fallback.
20. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("LeatherBoots","item_boots")` — R1b — unfinished equipment fallback.
21. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("LeatherGloves","item_gloves")` — R1b — unfinished equipment fallback.
22. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("LeatherCap","item_helmet")` — R1b — unfinished equipment fallback.
23. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily("Mace","item_mace")` — R1b — unfinished equipment fallback.
24. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_NullNewTileRetainsMaceGlyphAndDoesNotBorrowVial` — R1b — unfinished equipment fallback.
25. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_ReleaseClaimsRestoresOriginalGlyphAndTint` — R1b — unfinished equipment fallback.
26. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_RememberedFogDoesNotRevealGroundEquipment` — R1b — unfinished equipment fallback.
27. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_SevenFamiliesHaveDifferentSilhouettesAndDistinctGuids` — R1b — unfinished equipment fallback.
28. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_StationaryFullRepaintReplacesOldFamilyWithoutStaleClaim` — R1b — unfinished equipment fallback.
29. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_TwoSameBlueprintInstancesShareBodyWithoutSharingCellTint` — R1b — unfinished equipment fallback.
30. `GameAuditEquipmentGroundSpriteAdversarialTests.Adversarial_UnexploredCellCannotClaimNewItemArt` — R1b — unfinished equipment fallback.
31. `PlaceProfileTests.TheProfileTable_IsData_AndComplete` — R1a — stale design pin.
32. `WorldMapAuthoringTests.Sill_IsAtTheCentre_OnSpread_AtTierOne` — R1a — stale design pin.
