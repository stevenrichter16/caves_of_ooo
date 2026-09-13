# Sodden voxel art kit

Status: SD02-art-red confirmed the missing library in isolated Unity before production authoring. Initial library/build passed SD03. Native SD06 renders revealed short landmark silhouettes and tiled-looking duckboards; a readability refinement is in progress. This is CoO-original visual art, not a Qud source port.

The kit supplies six coarse native-cell families, each with four generated variants. Every mesh uses one renderer, the existing ring palette material, constant UV swatches, and no colliders or gameplay scripts. Native entities remain the authority for occupancy, interactions, destruction and liquids.

| Family | Native mapping | Silhouette and palette intent |
|---|---|---|
| ground | Grass, Floor | A continuous low olive-peat floor, one swatch. Variation confined to the buried base so adjacent cells remain quiet. |
| mire | MirePool, PeatBog | A contiguous tea-teal surface, one swatch, slightly above ground. No raised ornamental rim. Removing the native pool removes this surface. |
| peat | PeatBank | Broad stepped layers with a dark cut face and muted earth top. |
| snag | DeadTree | Tall bare trunk with two staggered, broken forks, charcoal wood and exposed pale ends. |
| boards | Duckboard | Three broad plank strips over two sleepers, weathered wood in two tones. |
| body | BogTakenBody | Small prone cloaked figure with head, folded arms and boots. Ordinary bog-taken remains only; pre-Felling witnesses remain exclusive to their authored place. |

Reeds use the established Spread kit through the parent renderer integration. No duplicate reed assets or biome ownership rules are introduced here.

## Verification sweep

| Source | Verified shape / correction |
|---|---|
| SpreadVoxelLibrary.cs | Additive ScriptableObject entries expose Id, Prefab, Mesh and SpawnRing3DCatalog.Model metadata. Existing library accepts all entries as entity; Sodden must explicitly distinguish its ground family. |
| SpreadVoxelKitBuilder.cs | Offline combined cube meshes use the ring WorldMaterial, 16 by 8 palette UVs, preserve existing asset GUIDs, and destroy temporary authoring objects. |
| SpawnRing3DCatalog.cs | Metadata contains kind, boundsCenter, boundsSize, triangles, materialFamily, rigFamily, clips and sockets. No native entities belong in the catalog. |
| SpawnRing3DRecipes.cs | Recipes place art at native cell centers and preserve the owning entity. The kit therefore must fit the centered [-0.5, 0.5] X/Z cell. |
| SoddenFormationBuilder.cs | Shipped exact blueprint names are MirePool, PeatBank, DeadTree, Duckboard and BogTakenBody. |
| Lore/History/02_Geography.md | Ordinary peat bodies are known around Sumphold; intact pre-Felling witnesses belong to the Drowned Ledger. Art mapping must not claim arbitrary corpse/witness blueprints. |
| SpawnRingPalette.png | Actual sampled palette indices: olive peat 68=(97,94,56), dark tea teal 24=(24,55,60), dark wood 15=(63,53,29), gray wood 12=(66,73,68), pale wood 13=(116,124,111). All blocks sample one UV coordinate rather than the source's textured surface. |

## Tests and adversarial checks

The asset tests cover exact family coverage and four distinct meshes each; cell bounds; bounded block count; one renderer; no collider or MonoBehaviour; at most two swatches per object; contiguous same-height/same-color ground and mire; explicit blueprint mapping and nonmatching controls; strict family and variant rejection; and mutation fixtures for missing, duplicate, null and corrupted material/mesh/metadata references. The fixtures validate an independent library clone and revalidate the unmodified source afterward.

## Performance

All geometry is authored offline into a shared mesh per variant. Ground and pool tiles each use one box; the largest family budget is ten boxes (240 vertices). Runtime lookup uses a validated fixed dictionary and precomputed model identifiers; valid recipe lookup introduces no new allocation. There is no per-frame procedural mesh creation or per-voxel GameObject. The parent presentation integration retains normal native dirty-patch batching and owns its profiling and renderer tests.

## Reproduction and review

Build entry point: `CavesOfOoo.Editor.SoddenVoxelKitBuilder.Run` in a disposable/isolated Unity project. It writes only `Assets/Resources/SoddenVoxel3D`, preserves existing generated asset GUIDs, and does not save or rearrange a scene.

Visual honesty: numerical tests prove bounds, metadata and batching structure. Final composition, readability and palette are reviewed from actual native camera renders by the parent feature before close-out.

## Implementation log

- SD02-art-red: missing SoddenVoxelLibrary compile errors confirmed in the isolated validation project before production authoring.
- Implemented the additive library and offline mesh/prefab builder; no live scene mutation. Strict lookup rejects invalid families and variants instead of silently borrowing another family.
- Quiet ground and mire use one constant color and level tops across variants. Peat uses four boxes, boards five, snags and bodies eight; maximum authored geometry is 192 vertices per mesh (below the 240-vertex test budget).
- Source-side self-review: native aliases remain explicit, exposed geometry stays within one centered cell, generated resources are borrowed instead of revoxelized, and invalid library validation clears the stale index before rejecting.
- Strengthened corruption counterchecks: wrong mesh changes only a cloned prefab's MeshFilter while retaining valid entry metadata; wrong material uses a different valid ring material. This prevents null checks or metadata checks from accidentally satisfying the intended identity tests.
- Three new script metadata GUIDs audited against all Assets metadata: each occurs once. Generated resource GUIDs remain part of the parent installation audit.
- Pending parent gates: Unity compile/build, numerical asset suite, native-camera visual review and full regression comparison. No final visual or performance claim is made before those results.

## Native-camera readability refinement

Viewed SD06 DrownedCopse-64, BogFace-64 and Causeway-64. The 1.5–1.8-cell snags read as stumps at the unchanged overview camera; bank caps matched the ground too closely; plank orientation made the causeway read as pale tile courses. Fixes target the generator's families rather than repositioning the preview scene.

Four new numerical readability cases are written before modifying the builder: snags 2.7–3.25 cells high with broad upper forks; banks 1.1–1.5 cells high and two earth swatches independent of ground; three duckboard top planks spanning north/south across west/east travel, using dark supports and weathered warm wood. SD08 isolated RED confirmed all four intended failures with zero compile errors before modifying the builder. Ground/mire remain unchanged in this art refinement; pool material decisions belong to the parent integration.

The revised builder now generates snags 2.78–3.20 cells high with wider upper forks, peat faces 1.18–1.39 cells high in dark and warm brown (palette 10/8), and crosswise duckboards in weathered wood/dark supports (11/15). Cell footprints, mesh counts, quiet ground, mire, and body art are unchanged. SD10 rebuilt the assets and rendered eighteen complete native-pipeline examples with zero missing meshes. Parent visual review confirms taller dead-tree silhouettes, legible brown faces, and crosswise wooden boards. SD11 passes all 29 art-kit tests and all 82 new Sodden cases; the full suite has exactly the existing 32 baseline failures.
