# Sodden visual coverage and integration audit

Status: source audit plus first native owner census and controlled render review, 2026-10-08. Read `CLAUDE.md` before auditing. Production work is coordinated separately. The results below distinguish selected models, actual submissions, exact style approval and content not exercised by natural generation.

## Corrections found before implementation

| Initial assumption | Verified correction | Consequence |
| --- | --- | --- |
| Sodden needs its first 3D kit | It already has 24 static variants, plus 24 Sumphold variants, 28 Ledger variants and a stateful district kit. | Make an original, coherent regional treatment; preserve native state selection and existing 3D ownership. |
| Sumphold is a Sodden map tile | `WorldMapAuthoring.BiomeRows[6][15]` is `S`; Sumphold is at `Overworld.15.6.0`, profile `Boatyard`. The authored map has 63 `D` surface tiles. | Include Sumphold only as an explicit current-POI exception if the visual circuit includes it. Do not alter its gameplay biome. |
| Zero missing voxel meshes means complete 3D coverage | The voxel counter sees source meshes already selected by recipes. `RefreshCurrent` skips owners whose recipe has no model. | Census every current visible native owner and record selected model, failure, authored/rendered flags, and style approval separately. |
| Original frogs and snakes already cover their native bog | Original Reedfrog/GinFrog/Bandfrog are in `SpreadVisitorCreatureSource`, but refinement is Spread-only except the district Bandfrog. Original Viper is similarly Spread-scoped. | Core Sodden fauna need explicit admission; Greatdew separately needs a static plant-hazard model. They cannot be inferred from library availability. |
| Rebuild the shared Sodden assets in place | Sodden/Sumphold/Ledger forms are also borrowed by other regional kits and the 868-entry Spread native-style graph. | Prefer regional copies/new assets selected per receiving zone. Avoid silently changing other biomes. |

Sources: [map and authored addresses](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:53), [Sumphold](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:223), [skipped recipe owners](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:227), [visitor admission](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpreadVisitorCreatureLibrary.cs:85), [Viper body](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpreadBiomeActorLibrary.cs:54), [shared static graph](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpreadNativeStyleSource.cs:10).

## Actual regional scope

- Wilderness: six composed surface formations—OpenMire, PeatCuts, ReedMaze, DrownedCopse, Causeway, BogFace. The existing static index excludes authored places and sinkhole mouths. It is an address/generation predicate, not sufficient proof of the current world's identity. [SoddenCompositionPlan](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/SoddenCompositionPlan.cs:26)
- Sumphold: `Overworld.15.6.0`, current village `Boatyard`; already qualifies for Spread presentation because its biome is Spread. The boatyard has hull trestles, loading fingers, peat work, cutters and toll rolls. [composition](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/SumpholdCompositionPlan.cs:32)
- Drowned Ledger: `Overworld.17.5.0`, current village `ExcavationCamp`; preserved bodies, stakes, reading table, RecensionScribe and CurationSorter are actual native owners. [composition](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/DrownedLedgerCompositionPlan.cs:38)
- Southern circuit: `Overworld.15.7.0` dressing shelter, `16.7.0` cutbank crossing, `17.7.0` abandoned peat works. All three are authored Sodden and use exact address/version-gated fresh generation. [district plan](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/SoddenDistrictPlan.cs:11)
- Other live Sodden surfaces include rolled lairs/merchant camps and named sites; a wilderness-only scope misses them. Belowground needs an explicit policy: a bounded first pass can include committed Sodden lair floors using the existing ledger authority while leaving unrelated sinkhole interiors and ordinary caves unchanged. [lair ownership](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:150)

Recommended `SoddenPresentationScope.IsActive(zone)`: require the exact current cached graph from `WorldLocationContext`, canonical in-bounds address, valid current map, actual Sodden biome on surfaces, plus the explicit Sumphold address **and** current `Boatyard` village profile. Preserve `AreaCompositionScope.Allows`. If underground scope is approved, require an actual committed Sodden lair floor; never infer it from the address alone. Model this after [SpreadPresentationScope](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/SpreadPresentationScope.cs:19), without changing that predicate globally.

## Coverage inventory

| Live content | Current route | Required regional treatment |
| --- | --- | --- |
| Grass/Floor, MirePool/PeatBog, PeatBank, DeadTree, Duckboard, BogTakenBody | `SoddenVoxelLibrary`: ground/mire/peat/snag/boards/body, four variants each | First visual priority: wet/dry separation, cropped reed silhouettes, blocky peat strata, low readable boards and prone bodies. |
| Reeds | Borrowed `SpreadVoxelLibrary` | Regional copies or explicit art override; do not recolor the shared Spread source. |
| Sumphold floor/wall/hull/rolls/cutter/water | `SumpholdVoxelKitLibrary`, six families × four | Preserve work-yard identity and actual NPC/tool silhouettes. |
| Ledger preserved/stake/table/scribe/sorter/parcel/boards | `DrownedLedgerVoxelKitLibrary`, seven × four | Keep unsealed witnesses distinct from the transport parcel; preserve exact quest states. |
| Dressing bench, salvage, locker, notices and field dressing | `SoddenDistrictRecipes` chooses actual current repair/harvest/container state | Both broken/working bench; no unharvested salvage after depletion; portable dressing remains portable. |
| Reedfrog, GinFrog, Bandfrog, Viper, MawToad; Greatdew plant hazard | Real population tiers1–3. Base ring catalog binds MawToad; existing original frog/Viper libraries are mostly Spread-gated; no pre-change `Greatdew` presentation match found | P0 fauna closure plus separate Greatdew static art. Greatdew is a non-solid vegetation owner whose real snare roots and corrodes a passing creature, not an animal or a harmless decoration. Preserve Bandfrog's scarlet warning, frog species distinctions and the snake's legless anatomy. |
| Player, town residents, wardens/traders, visitors and lair guards | Base actor bindings plus scoped humanoid/visitor refinements; equipment is a separate route | Reuse real rigs and sockets through regional adapters. Test imported visitors and the player in the same region. |
| Sumpsieve, Drowsebell, Chillcress, Slipsedge, Peatlantern | `BiomeCropRecipes` / closed crop catalog already provide plant states, seed and harvest forms | Preserve actual crop stage and useful harvest identity; include immature/mature/stubble and naturally dropped yields in census. |
| BrinePool, SteamVent, DryBrush, AcidPool, oil/poured liquids, gas, traps, ruins, containers, heavy props | Shared pool/terrain recipes, tile state, regional landmarks and existing fallbacks | Semantic warning colors must survive regional palette treatment. Never replace active traps with decorative scenery or create nonexistent safe ground. |
| Supplies, equipment, quest items, bodies and placed combat utilities | Shared catalog + portable/scenery refinements, several gated to Spread | Audit actual receiving-zone owners after drop/use/destruction. Inventory contents are not world-visible owners until dropped. |

Sources: [Sodden family aliases](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SoddenVoxelLibrary.cs:115), [Sumphold aliases](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SumpholdVoxelKitLibrary.cs:117), [Ledger aliases](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/DrownedLedgerVoxelKitLibrary.cs:117), [district current-owner guards](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SoddenDistrictRecipes.cs:28), [population](/Users/steven/caves-of-ooo/Assets/Scripts/Data/Tables/PopulationTable.cs:560), [native catalog](/Users/steven/caves-of-ooo/Assets/Art3D/SpawnRing/Definitions/catalog.json), [crop data](/Users/steven/caves-of-ooo/Assets/Resources/Content/Data/Farming/BiomeCrops.json:79), [hazards](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs:104), [surface generation closure](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:918).

## Bounded implementation architecture

1. **Add one regional scope and imported art library.** Keep procedural/offline mesh creation in an editor builder, following `SoddenVoxelKitBuilder`. Persistent combined meshes/prefabs use original cuboid silhouettes, region-owned palette/material and real 3D depth; no gameplay colliders/scripts on art assets. Every entry has exact model/state provenance and deterministic variants. Start with dominant environmental families, then close the census; 28 attractive environmental models alone are not complete biome coverage.
2. **Preserve recipe authority, then select the regional presentation.** `SpawnRing3DRecipes.Resolve` already handles district repair, crop stages, doors, parcels, equipment discoveries and refusal reasons. Override successful model identity only when its exact source/state is supported. For genuine unmodeled owners, bounded regional adapters may call existing pure portable/scenery/actor resolvers after the same current-owner checks. Never rescue hidden/foreign/disguised/invalid-state refusals. [recipe dispatch](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs:25), [district dispatch](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs:77), [portable guards](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpreadPortableWorldRecipes.cs:13)
3. **Wire both rendering paths.** Nonbatched actors/items use `SpawnRing3DPresenter.PrefabFor`; batched terrain/scenery use `SpawnRing3DGroundPatches.GetModel`. Add new material admission to ground patches and per-bind materials; add generated mesh registration to `VoxelWorldPresentation` so already-blocky models are not re-voxelized. Include the new regional flag in bind/rebind readiness, cleanup and evidence. [presenter](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:87), [PrefabFor](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:275), [ground model path](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DGroundPatches.cs:278), [generated registry](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/VoxelWorldPresentation.cs:23)
4. **Keep lighting and evidence regional.** Select deliberate wetland lighting only for this scope, with owned material clones. Existing `TryGetApprovedStyle` is a Spread-specific contract; a false result currently means no Spread approval, not automatically invisible/invalid 3D. Extend/report regional evidence explicitly. Do not change its meaning to return true for any visible mesh. [lighting and bind](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:137), [style evidence](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:465)
5. **Close visible owner coverage before declaring completion.** Capture six formations × three seeds, all three district sites, Sumphold and Drowned Ledger with real generated graphs. Record each owner rather than only blueprint counts. Add carried-in/drop/state-transition fixtures afterward for gaps not naturally sampled. Gate visual approval separately from simulation invariance.

Do not widen Spread scope as a shortcut: it selects glade lighting, actor sizing, static palette copies, equipment and transient volumes together. Do not recreate scenery from composition plans inside rendering: removed/harvested/repaired native objects remain authoritative. Do not global-edit `VoxelWorldMeshCatalog`, shared materials or existing prefab assets to make this one region look different.

## Native acceptance and counterchecks

- Positive scope: current Sodden wilderness, native district, Ledger and explicit Sumphold exception. Negative: other biomes, spoofed profile, stale cached zone, malformed address, unrelated depths. Confirm ordinary startup stays in the current starting biome.
- Each known render-visible owner gets a model and an honest authored/rendered/style receipt; report unknown owners by exact blueprint and recipe failure. Test transferred items/actors after the initial bind.
- Both overview and close camera captures: wet vs dry movement, low boards, creature silhouettes, readable warning colors, terrain height/picking, selected owner and usable menu. No external image editing to make the render look better.
- Stateful changes: harvested crops/salvage, repaired bench, opened/destroyed door/container, dead actors, spent snare, expired quartz light, dropped/equipped item. No ghost art and no resurfaced deleted terrain.
- Compare source gameplay graph before/after bind/refresh/release: IDs, membership, parts, inventory, HP, terrain, crop state, RNG and world flags unchanged. Rendering must remain read-only.
- Native importer validation plus original animation checks (Idle/Walk/Interact/Attack/Hit, bone weights, sockets). Real Play use verifies movement, attack, pickup and interaction. Screenshots still require human/model visual inspection; numerical coverage alone cannot certify the requested appearance.

## Audit limits and self-review

Initial 🟡 creature admission finding: the first native census now confirms modeled/rendered and approved common fauna, including 23 Greatdew owners, after scoped integration. This does not imply coverage of every rare visitor or imported item.

🔵 Existing `Before/receipt.json` has 18 native formation rows with zero missing voxel replacements. It does not contain per-owner coverage, town/district close views, or style approval, and cannot support a claim that every object is already 3D.

⚪ Unknown modded/future/reskinned objects should retain their native fallback and produce an actionable coverage row; silently assigning a generic creature would obscure mechanics. Scope of completion is the current live content closure, with safe fallback for unknowns.

No production files, world generation, saved worlds, or scenes were edited by this audit.

## Follow-up capture harness

Added editor-only [SoddenNativeArtPreviewBatch](/Users/steven/caves-of-ooo/Assets/Editor/Scenarios/SoddenNativeArtPreviewBatch.cs:20) and its fresh `.meta` at the coordinator's request. `Capture(output)` produces 18 real wilderness cases (six formations × seeds 64, 1729, 729490642), then the three district destinations, Sumphold and Drowned Ledger at seed64. Each receipt links an owner census and an overview plus up to five selected close camera renders: primary native feature, wet/dry edge, creature, crop and loose item when present. No owner is inserted to fill a missing category.

The census records the actual `TryGetApprovedStyle` boolean/failure independently from recipe model selection, `IsAuthoredEntity` and `IsRenderedEntity`. Zero mesh gaps cannot hide an unmodeled owner. Each zone writes its receipt before the next begins. Capture uses detached fresh world generation, private stock RNGs, restored content services/settings, an isolated preview scene and disposable camera targets. It never writes scene assets, world saves or player preferences.

Harness status: native compilation and execution succeeded under the coordinator. The first run captured all 23 cases and their owner receipts in `Iteration1/`. Its numerical and visual results are recorded below; complete regional approval is not yet established.

## Dedicated adversarial hypotheses

Added `SoddenNativeArtAdversarialTests.cs` and a fresh `.meta`: 28 cases cover actual Greatdew snare/damage ownership, destroyed or carried plant refusal, appearance overrides, shared item and fauna ownership, spent snare/expired light/harvested bush state, canonical current-map authority, removed visitors, unknown future owners, another biome, and real original frog/snake skin and five animation clips. Each mutable refusal starts from a modeled/rendered positive owner; numeric mesh gates remain in the separate asset suite. Tests only: no production fixes in this step. Native first result: all 28 cases passed. The complete first batch was 84/100, with the remaining failures in other suites under coordinated review. A failing case remains a hypothesis until its fixture and actual XML are checked.


## First native census and visual review

[Iteration1 receipt](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration1/receipt.json) covers 23 generated zones and **58,332 current render-visible owner observations**, with 69 distinct blueprints. Every observation was authored and rendered. There were **zero truly unmodeled owners** and **zero missing voxel meshes**. Separately, **3,697 owner observations across 38 blueprints and 77 model IDs lacked exact style approval**; every failure was `unmapped-style-source`. These owners already had real 3D geometry. A missing proof entry must not be reported as a missing model.

Highest-priority regional closure targets from the actual census:

| Exact native selection | Observations / location | Meaning |
| --- | --- | --- |
| `spread-environment-paving-{0..3}` | Floor1,841 + StoneFloor159, Sumphold | The bright checkerboard dominates the settlement; a coherent regional ground treatment is needed, not merely approval. |
| `wellmeet-floor-{0..3}` | StoneFloor755, district / Ledger | Old gray interiors remain visually disconnected from the new mire. |
| `sumphold-wall-{0..3}` | StoneWall171 + SandstoneWall62 | Large gray slabs need readable blockwork and a regional material treatment. |
| `sumphold-water-{0..3}` | WaterPuddle295 | Preserve water identity while matching the new bog treatment. |
| `drownedledger-boards-{0..3}` | Duckboard48, Ledger | Preserve the visibly walkable dry route against wet ground. |
| `wellmeet-tent-{0..3}`, `wellmeet-corner-{0..3}` | TentWall45, shelter | Connect inhabited shelter architecture to the same regional palette. |
| `sodden-district-bench-broken` / working counterpart, `sodden-district-works-locker`, `sodden-district-works-salvage`, `sodden-district-route-notice` | Native district destinations | Useful objects must read as repair bench, equipment locker, salvage and route information; the initial locker is two plain brown blocks. Both bench states belong to closure. |
| `drownedledger-preserved-*`, `drownedledger-stake-*`, `drownedledger-table-*` | Bodies3, stakes3, table1 | Keep the location's unusual preservation / survey purpose legible. |
| Shared `ring-vine-wall-*`, `ring-copper-pipe`, `ring-steam-vent`, `ring-brine-pool-*`, `ring-woven-basket`, `ring-crate`, `ring-dry-brush-*`, `ring-hollow-log-*`, `ring-stairs-down`, `density-pool-acid` | Counts91/51/26/25/18/17/14/12/10/8 | Already modeled. Decide explicitly between exact reviewed source approval and scoped palette/geometry copies; do not mute acid, steam or brine warnings. |

Additional observed unapproved selections were campfires6, MendleafPlant5 (borrowed bush forms), sacks4, chairs4, campfire ground markers4, chests3, haul barrels2, beds2, and one each passage guard/post, watch lantern, shrine, well and oven. They are a smaller closure task, not grounds for a generic fallback.

Reviewed native overview and focused camera renders for OpenMire, DrownedCopse, Sumphold, DrownedLedger, PeatWorks and DressingShelter. The new dark teal floor/mire, pale reeds, branched snags and scarlet Bandfrog visibly establish the desired direction. Sumphold's ground and the district's old walls/interiors are the strongest remaining inconsistency. Image inspection is separate from the zero-missing-mesh statistic.

## Unsampled content fixture closure

Natural samples included Sumpsieve stages0/2 dry, Slipsedge stages0/2 dry and Drowsebell stages0/1/2 dry. **No Chillcress or Peatlantern, no loose Item-category owner and no player was present in that capture harness.** Their absence from this sample is not evidence that their models are missing or that they never spawn. Merchant inventory is not a world-visible owner until dropped. Other named Sodden destinations and all underground interiors remain outside the 23 captured cases.

Added [SoddenNativeContentCoverageTests.cs](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/SoddenNativeContentCoverageTests.cs) and its fresh `.meta`, **22 cases**, native first run21 passed and1 failed on a fixture assumption; the corrected fixture passed all22 in `native-final-regional-green.xml`:

- All five Sodden crops × all three stages × wet/dry state, exact approved submitted source identity, removal and native graph invariance.
- Both seed and useful harvest for every species, through actual `InventorySystem.Pickup` and `Drop`.
- Actual elapsed `CropTime.Reconcile` drives Chillcress through both stage boundaries and drying; repeated renderer refresh must not consume moisture or advance time.
- Actual player and Dagger, Torch, GroundwireScreen, IronshodBoots and FilterHood through equipped, carried, dropped and re-equipped states. World body and fitted attachment proofs are checked separately.
- Actual cord-snare entry removes the armed mesh while preserving the rooted player's body; native quartz `EndTurn` expiry removes the ground light's geometry at its real lifetime boundary.
- Foreign crop-part/yield authority, replaced current world graph and a stale equipment pointer cannot retain approved views.

The crop-state matrix is a controlled fixture; it does not claim organic cultivation of every species. One real elapsed-growth lifecycle complements it. Equipment is created through the real factory and native equip/drop operations; the tests do not claim ordinary loot acquisition or a manual expedition. No production code or world saves are changed by this fixture work.


Native first content result: [native-regional-red-content.xml](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/native-regional-red-content.xml),21/22. PeatlanternCup legitimately follows automatic free-hand equip on pickup, so asserting it must appear in `Inventory.Objects` was wrong. The corrected fixture accepts carried ownership or requires actual equipped Physics, inventory cache and current body-slot pointers before the ordinary Drop operation. No production behavior changed. The other21 cases passed, including all30 crop state forms, five equipment transitions, actual growth/dryout and both utility lifecycles. The later [native-final-regional-green.xml](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/native-final-regional-green.xml) confirms all22 pass, as does `native-final-art-red.xml`; the latter filename describes other suites’ new RED cases, not a content-coverage failure.


## Iteration2 comparison: 75-entry regional kit

[Iteration2 receipt](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration2/receipt.json) repeats the same23 cases and58,332 native owner observations. All58,332 remain authored and rendered, with **zero unmodeled owners and zero missing meshes**. Exact style failures fell **3,697→307**, closing3,390 observations (91.70% of the initial failures); affected blueprints fell38→23 and selected model IDs77→40. All307 residual failures are still `unmapped-style-source`, with no stale-owner or submitted-geometry failure in the census.

Site comparison: Sumphold2,000→0; PeatWorks443→0; DressingShelter299→2 (one bed, one chair); CutbankCrossing3→1 (passage guard); DrownedLedger668→20. Wilderness failure counts did not change. The same sampled owner population supports a direct comparison; the decrease is not from dropping objects from the census.

| Residual group | Exact selected model IDs | Native observations |
| --- | --- | ---: |
| Vegetation / log | `ring-vine-wall-{0..3}`91; `ring-dry-brush-{0..3}`14; `ring-hollow-log-{0..3}`12 |117|
| Hazards | `ring-steam-vent`26; `ring-brine-pool-{0..3}`25; `density-pool-acid`8 |59|
| Industrial prop | `ring-copper-pipe` |51|
| Containers | `ring-woven-basket`18; `ring-crate`17; `ring-sack`4; `ring-chest`3; `ring-haul-barrel`2 |44|
| Navigation | `ring-stairs-down` |10|
| Fire / forage clues | `ring-campfire`6; `ring-bush-{0,2,3}`5 (MendleafPlant); `wellmeet-marker-{1,2}`4 |15|
| Furnishings | `wellmeet-chair-{0,3}`4; `wellmeet-bed-{2,3}`2; `wellmeet-lantern-2`, `wellmeet-shrine-3`, `wellmeet-well-2`, `wellmeet-oven-2` one each |10|
| Native passage NPC | `sumphold-cutter-0` for SoddenPassageGuard |1|
| **Total** | **40 exact model IDs across23 native blueprints** |**307**|

Reviewed seven new native camera captures: Sumphold overview; PeatWorks primary; DrownedLedger primary and overview; OpenMire overview; ReedMaze overview; DressingShelter primary. The new paving removes the former bright checkerboard, walls now show dark brick courses, tent edges read as light framed screens, and the works locker has a legible cabinet frame/lock. These are meaningful visual improvements in actual generated destinations. The former dominant named-site inconsistency is resolved in this set.

Residual interpretation and priority:

1. **Visible regional outliers, but existing3D:** the chartreuse `ring-vine-wall` enclosure at ReedMaze64 (x61–65,y14–17) stands out against the darker vegetation. Brine appears as flat mint polygons (x65–66,y21–22); acid remains a bright flat green patch in OpenMire64. The33 brine/acid observations plus91 vine-wall fragments are the strongest candidates for a later small regional treatment. Keep acid/brine distinct from safe dark water; visual uniformity must not erase their warning identity.
2. **Generally compatible existing3D, exact approval absent:** containers, log/brush, pipe, stairs, campfires and settlement furniture retain blocky volumes and readable silhouettes in the reviewed captures. They do not all require replacement merely because the regional catalog lacks their exact source entry. Exact source/palette approval, if added, must validate the current imported mesh and submitted material; a blanket approval toggle would falsify the evidence. The20 remaining Ledger owners are this shared prop/furniture set, not missing site art.
3. **One actor proof gap:** SoddenPassageGuard is rendered as its existing rigged `sumphold-cutter-0` body. Treat this as a precise source/role approval question, not a reason to create a generic humanoid. The current census does not prove its animation or identify a broken action.

The bounded art pass can now claim regional terrain/architecture and meaningful destination props, plus existing modeled inhabitants and imported items. It must **not** claim every observed source has new regional art or full exact style approval:307 observations remain explicit. None of these failures currently blocks the planned native gameplay journey. The numerical sample still does not cover other named sites, all rare imported owners, all underground interiors, or ordinary acquisition rates. Those limits and the controlled unsampled fixture remain as recorded above. No C# changes were made during the broad native test run.


## Final sampled census and whole-surface closure

[Iteration3 receipt](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/receipt.json) repeats all 23 native formation/destination cases, **58,332 owner observations**, with **zero unmodeled owners, zero unrendered owners and zero missing meshes**. Exact-style failures are **157 observations  / 19 blueprints  / 30 model IDs**, all `unmapped-style-source`. The reduction 307→157 is exactly VineWall 91 + SteamVent 26 + BrinePool 25 + AcidPool 8; those 150 observations now use the reviewed regional additions. Compared with Iteration1, 3,540 of 3,697 proof gaps are closed (95.75%). This is a source-provenance metric, not a rating of visual quality.

The remaining 157 are CopperPipe 51, WovenBasket 18, Crate 17, DryBrush 14, HollowLog 12, StairsDown 10, Campfire 6, MendleafPlant 5, Sack 4, Chair 4, CampfireGroundMarker 4, Chest 3, HaulBarrel 2, Bed 2, and one each SoddenPassageGuard, WatchLantern, Shrine, Well and Oven. They remain their existing genuine 3D models. Sumphold and PeatWorks have no remaining proof misses in this sample; Drowned Ledger has 20. No residual has a stale-owner or submitted-mesh mismatch reason.

[AllSurfaces receipt](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/AllSurfaces/receipt.json) expands to **all 63 current authored Sodden surface tiles plus Sumphold, seed 64**, using the actual generated current graphs. All 64 zones complete with **162,518 render-visible owner observations, zero unmodeled/unrendered owners and zero missing meshes**. The 1,148 style misses span 26 blueprints and 49 model IDs, all `unmapped-style-source`. This census adds geographic breadth but only one seed; its owner files have no camera captures. It does not cover underground floors, arbitrary future/modded owners, all possible state combinations, or every transferred object.

| Residual group | Exact selected model families | AllSurfaces observations |
| --- | --- | ---: |
| Quiet’s Door ordinary water | `ring-water-puddle-{0..3}` |350|
| Spivenor trees / bushes / sinkhole rim | `ring-tree-{0..3}`, `ring-bush-{0..3}`, `ring-sinkhole-lip-{0..3}` |187 /114 /60|
| Native industrial/scavenged props | `ring-copper-pipe`, `ring-woven-basket`, `ring-crate`, `ring-chest`, `ring-sack`, `ring-haul-barrel`, `ring-wooden-barrel` |120 /60 /33 /23 /22 /7 /1|
| Shared plant/wood props | MendleafPlant `ring-bush-*`, `ring-hollow-log-*`, `ring-dry-brush-*`, `ring-fallen-beam-*` |50 /38  / 19 /8|
| Stairs and campfires | `ring-stairs-down`, `ring-campfire` |28 /10|
| Furnishings and doors | `wellmeet-chair-*`, `wellmeet-marker-*`, `stillleaf-open-door-3`, `wellmeet-bed-*` |4 /4 /3 /2|
| Single named-site props and guard | `wellmeet-lantern-2`, `wellmeet-shrine-1`, `wellmeet-well-2`, `wellmeet-oven-2`, `sumphold-cutter-0` |1 each|

**Largest remaining art-review targets:** Quiet’s Door (`Overworld.16.1.0`) has 359 misses, 350 ordinary water; Spivenor (`Overworld.16.4.0`) has 365 misses, 361 trees/bushes/rim. Together 724/1,148 misses (63.07%) concentrate at these two named surfaces. Their names are independently identified by [WorldMapAuthoring](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:56), not guessed from model names. Existing named-site models may be appropriate; the census alone cannot establish a palette defect. Capture these two sites before deciding whether to adopt exact shared-source proofs or author narrow regional copies. Do not blanket-approve the 49 model IDs or claim that every surface asset was remodeled.

Independent final image checks: [BogFace vine enclosure](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/BogFace-729490642-actor.png) shows an actual raised leafy barrier and its native opening/occupant; [OpenMire overview](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/OpenMire-64-overview.png) retains distinct green/yellow acid, cyan brine and dark mire. No missing geometry is apparent in those views. Shared campfire/container forms remain visibly simpler than the new vegetation. These two images are a bounded visual review, not proof of every owner or an interactive playthrough.

## Cold-eye integration review and performance follow-up

Read-only review of the current scope, presenter, recipe admission, ground patches, shared equipment/creature admission, generated-mesh registration and style-evidence changes found **no additional concrete blocking owner-authority, lifecycle or cross-biome regression**. This is bounded source review alongside the recorded native tests, not a universal absence-of-bugs claim.

- [SoddenPresentationScope](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SoddenPresentationScope.cs:18) requires the exact graph currently held by its world manager, canonical surface address, current map dimensions/biome, native composition permission and exact Sumphold/Ledger profile exceptions. Address parsing is cached; current graph/map authority is still rechecked. Shared libraries are widened through that predicate, not by globally widening Spread.
- [Presenter refresh](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:266) resolves actual present owners, removes stale recipes/views and lets native changed geometry mark patches; no generation-plan replay was introduced. Palette choice is captured at bind and a changed regional authority forces a rebind. [Release](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:770) disposes owned surfaces, equipment/transients and clears the regional library reference.
- [Ground patch refresh](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DGroundPatches.cs:144) retains dirty/fingerprint reuse. The additional Sodden mesh stream is staged with the other streams, disposed on failure/replacement and on final release. Its source material is borrowed while the submitted material is surface-owned. No new per-frame mesh construction or mesh-buffer copy was found in these changes.

**🧪 Bounded performance follow-up, not a performance pass.** The ordinary Play report [e1f258aed37649188a5c4e51e3e90fb4](/Users/steven/caves-of-ooo/Docs/Verification/SpreadDiscoveryExpeditions/Native/e1f258aed37649188a5c4e51e3e90fb4/report.json) records 90.03 seconds / 2,635 completed-frame samples: `COO.ZoneRenderer.LateUpdate` mean 24.59 ms, p95 62.65 ms, max 317.34 ms; Main Thread mean 34.14 ms, p95 75.04 ms, max 1,547.97 ms; GC Allocated In Frame max 4,772,371,951 bytes, p95 13,399,387 bytes. These high values warrant investigation. The marker is the whole ZoneRenderer, not isolated new Sodden code; this Editor run includes zone generation, AI, screenshots and serialized per-step audit IO, with no pre-change baseline. It cannot attribute those costs to this patch, establish a leak, measure GPU time or certify standalone performance.

One concrete profiling candidate is repeated bind-time validation of the same imported assets: [VoxelWorldPresentation](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/VoxelWorldPresentation.cs:119), [presenter binding](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:168) and [regional style catalog](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpreadBiomeStyleEvidence.cs:94) validate overlapping libraries. [SoddenNativeArtLibrary.Validate](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SoddenNativeArtLibrary.cs:90) copies readable mesh vertex/UV/normal buffers while checking exact assets. This is observable bind work, **not evidence that it caused the reported 4.77 GB spike or persistent frame cost**. `Find` uses its prepared dictionary thereafter. Do not remove validation or authority checks on that assumption.

Next controlled performance check: fixed already-loaded Sodden zone, identical camera/FOV and native owner graph, screenshots/audit serialization disabled; separate idle frames, repeated movement refresh, first bind and return bind. Compare before/after the art change with identical settings, and attribute allocation stacks/markers before editing. Keep any such work separate from the present visual release. The first Play’s interruption was an obsolete observer-only starting-dagger assertion after the player earned/equipped a mallet; it is not evidence of a game weapon restriction. That run is incomplete, and the coordinator’s corrected rerun must supply final functional acceptance.
