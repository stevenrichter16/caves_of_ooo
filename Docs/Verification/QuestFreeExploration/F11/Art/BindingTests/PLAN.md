# F11 original hunter binding/import gate

Status: private **test-only** package, 20 cases; actual runtime and full test assembly reference compilation both completed with zero errors. No library/importer/renderer production has been written. Actual missing-art RED requires the real Furrowstalker/FurrowstalkerCorpse definitions and sealed SpreadPredatorPart to be adopted first. Missing content must be labelled a source-precondition failure, not credited as missing art.

## Verified source and scope

The root accepted the corrected original top/oblique/five-pose source pack under `ArtSource/SpreadFurrowstalker3D`: 480 live triangles/11 bones, 192 static remains triangles, existing Idle/Walk/Interact/Attack/Hit. Feeding still follows mechanics' finite real progress contract, not an art-created action. The native rig will not introduce Stalk/Chase states or change simulation speed.

Mechanics owns exact child data: Furrowstalker, glyph f / &y / layer10, Quadruped + on-owner BodyNaturalAttack/MeleeWeapon, SpreadPredator role. FurrowstalkerCorpse inherits actual CreatureCorpse, % / &y and native SourceBlueprint/SourceID provenance; no Harvestable or new meat. Body appearance does not require an active/configured hunt: cancelled, stopped or loaded valid living owners retain their original anatomy. No hidden-target hint or overlay is added here.

| Sweep premise | Actual source | Consequence |
|---|---|---|
| Existing original animal pack supplies import/rig conventions | `QuestFreeSpreadArtLibrary.cs:10` and `QuestFreeSpreadArtBuilder.cs:15` | Use a separate two-entry optional library, never rewrite the grazer pack. |
| Existing renderer already routes all five states | `SpawnRing3DPresenter.cs` and `EntityVisualHooks.cs` | No presenter timing or event architecture changes. Gameplay owns actual strikes/feed progress. |
| Dictionary lookup alone proves current ownership | `Entity.cs:43` SpatialZone and `Zone.GetEntityCell` | Require exact SpatialZone, cell.ParentZone, current cell.Objects and all current part backlinks. Stale-backlink test restores original links in finally. |
| Owned rendering material must be the exact persistent material | `SpreadBiomeStyleEvidence.cs:73` | Use approved-style proof plus persistent palette/mesh contract; submitted per-surface material may be an approved owned clone. |
| Imported head-local Y is world-up | Existing `QuestFreeSpreadArtTests` actual coordinate correction | Sample baked vertices through skin.TransformPoint, including matching Idle counter. |
| Naming a corpse proves a real kill | Native CorpsePart provenance versus staged fixture | Art test stages exact metadata and says so; later real hunt/kill/native frames remain separate. |

## Proposed minimal implementation after executed RED

New `FurrowstalkerLibrary` at `Resources/Furrowstalker3D/Library`; exact IDs `spread-furrowstalker` and `spread-furrowstalker-remains`. Expected entries expose Id, Prefab, Mesh, Materials, Spec, with Load/Find/Validate and scoped Refine like the current original pack. Refine only exact current ordinary owners in active Spread scope, accepting missing-blueprint fallback or the expected native recipe, refusing custom visuals/glyphs, carried/equipped and hidden/foreign sources. Role must be the real sealed SpreadPredatorPart with current backlink; it is not manufactured by the library.

Five narrowly gated integration hunks are expected: `SpawnRing3DCatalog.FindModel`, `SpawnRing3DLibrary.FindPrefab`, `SpawnRing3DRecipes.Resolve`, `SpreadBiomeStyleEvidence`, and `VoxelWorldPresentation`. Library presence remains optional for unrelated models. Preserve existing chain order and all foreign recipe refusal. No broad cache changes.

New `FurrowstalkerBuilder.Build(absoluteSourceExportDirectory, absoluteReportPath)` follows the existing preview-scene isolation and importer pipeline. Validate exact catalog/source hashes, palette, two IDs, source triangles/bones/clips and inert components before saving scoped output. Keep generic imported avatar, 11 owned rigid bones, fixed root and five named controller states. One static remains mesh with correct normal/reflection handling. Use SaveAssetIfDirty only for owned outputs; retain borrowed palettes and all original libraries/meshes/FBX byte-for-byte.

Expected original outputs: two FBX files and one hunter controller under `Assets/Art3D/Furrowstalker`; two persistent meshes, two prefabs and `Library.asset` under `Assets/Resources/Furrowstalker3D`, plus exact generated metas/folders. These are proposed paths, not currently generated/adopted assets. Root must record actual output inventory and unchanged borrowed hashes at import.

## Acceptance and honest limits

The 20 test cases cover real-source precondition; two exact inert assets; actual imported buffers/material; 11 owned bones/five deforming states/fixed root; world-up Interact and Attack jaw with Idle/return controls; move/visible-hidden/removal/full saved replacement; actual corpse pickup/drop and source refusal; 11 altered/foreign source controls; two direct stale-spatial-backlink counters in one paired test; unchanged real grazer and viper controls. Exact style evidence retains submitted renderer authority.

After source adoption root executes `CavesOfOoo.Tests.FurrowstalkerArtTests` and retains all actual failures, separating source premises from missing binding. Only then implement/import privately reviewed production and run matching GREEN plus existing library lookup/style, original grazer, corpse, voxel and actor integration neighbors. Actual game-camera views of live hunter, real strike, two feeding progress actions and natural corpse/escape remain separate native gates. No forced corpse, healthy damage grant, hunt success or ordinary discovery claim follows from these tests.
