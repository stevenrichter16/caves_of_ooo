# Spawn-ring 3D asset guide

This is the adopted refined218-model bundle for the playable eight-zone spawn ring. All218 strict Unity imports and final native78-check acceptance pass. Separate80.023s paced native profiling and raw recomputation pass; full results, environment/performance limits and final visual review are in Docs/BLENDER-SPAWN-RING-3D-PLAN.md. Blender previews alone do not prove native performance, fog, interaction, animation or saves. The game remains authoritative: the eight captured scenes are editable reference layouts, not replacement maps.

## Contents and editable source

- `ring_kit.blend`: `ASSET_LIBRARY_EDITABLE` holds one reusable collection per catalog model. Individual leaves, stones, rig parts and component meshes remain editable. The library is excluded from the initial view to avoid displaying every asset at the origin; enable its collection to edit it.
- `models/<model-id>.fbx`: runtime models; static source pieces consolidate into one palette mesh plus a separate water mesh where applicable. Roof/owner/actor-style identity is preserved through individual model assets, not a single flattened landscape.
- `catalog.json`: exact blueprint/model mapping,55 authored Felling owner identities, actual mesh triangle and bind-space bounds metadata, rig families, clips and equipment sockets.
- `textures/SpawnRingPalette.png`: opaque2048×1024 atlas with128 stable color/paint swatches. Model UVs carry their material colors. The palette uses `SpawnRingPalette`; water slots use `SpawnRingWater`. The village palette is not overwritten.
- `scenes/<zone-id>/scene.blend`: full80×25 captured native cells, owner/entity instances and an embedded hidden ImageGen guide. `manifest.json` records source hashes and every on-ground entity token represented. Optional whole-scene FBX files are omitted here; runtime uses the individual model FBXs.
- `scenes/<zone-id>/overhead.png`: strict overhead whole-zone review render. Lighting is Blender art direction, not native lighting/FOV evidence.
- `build_ring.py`,`mesh_kit.py`,`native/`,`references/`: self-contained procedural source,compressed native capture JSON,exact catalog contract,art brief,and eight separate references. Native data may be re-exported for new art inspection, but these snapshots must never seed-reconstruct live or saved simulation state.

## Rebuild and import

Run the bundled builder with Blender5.2.1LTS or a verified compatible version:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 2 \
  --python build_ring.py -- --output /tmp/spawn-ring-rebuild --render
```

`--skip-export` produces review sources/renders without FBX and must not be mistaken for an import bundle. `--kit-only` stops after the reusable kit. `--zones` accepts a comma-separated subset for review. Source art seed9092026 is deterministic; FBX/blend byte equality is not promised because exporter timestamps and container metadata can vary. Use normalized actual geometry/UV/material and clip/socket checks, not binary-FBX hash equality.

The native Unity builder is separate from the village builder:

```sh
Unity -batchmode -projectPath /Users/steven/caves-of-ooo \
  -executeMethod CavesOfOoo.Editor.SpawnRing3DAssetBuilder.BuildFromCommandLine \
  -spawnRing3dSource /absolute/path/to/this/bundle \
  -spawnRing3dReport /tmp/spawn-ring-import-report.json
```

Use the project's established Unity/MCP startup and test discipline. The importer requires the entire218-model bundle and actual metadata. It creates ring-specific resources/materials and keeps the village library intact. Its import/native tests and the live GPU/visual gate remain required.

## Coordinates and native binding

One unit equals one native square cell. Native `(x,y)` maps to Unity `(x+.5,height,24.5-y)`. Blender authoring uses X east,Y north,Z up. The validated village export convention is retained: an export-only Blender Z180° parent with FBX+Z forward,Y up; static bake enabled and rig bake disabled. Source meshes/layouts remain in the readable east/north convention. Do not add another compensating runtime180° rotation.

Only catalog ground models receive stable coordinate-hash quarter-turns. Actors, signs, grain ridges, compost, and Felling components keep their native orientation. The clean Felling scar uses undecorated ground variant0 within x31..48,y6..18; the seven actual bare positions stay unoccupied. Felling root volumes derive the native solid-cell union and have inward-rounded exterior corners. Mutable owner geometry stays independently addressable by exact component ID.

Runtime selects models from current native blueprints,properties,component ownership and equipment. Removing an owner removes its view; removing an entity removes its view. The captured scene layout is never the runtime placement source. Unrecognized content retains honest native sprite/glyph fallback rather than acquiring a guessed model.

Current native TileState controls shared water surfaces. The preview mirrors the16 cardinal-dry masks (N1/E2/S4/W8): a corner rounds inward by.1 only if both adjacent cardinal cells are dry, with8 segments per quarter arc. Shared wet edges retain full±.5 endpoints. WaterPuddle,GroveSeep andPeatBog model dry beds/rims; Felling flow owns only ripple detail. SprayPool has its native visual basin but does not imply LiquidPool or TileState simulation. Brine/tar remain their own native hazards. No water is reconstructed from seed after a deliberate native state change.

## Creatures and equipment

There are21 actual rigs:9 humanoid and12 nonhuman. Each exports `Idle`, `Walk`, `Interact`, `Attack`, and `Hit` as in-place clips. Nonhuman rigs are species-specific frog,tortoise,serpent,avian,fungal orrooted families. Rooted animation bends its existing form; it does not simulate locomotion. Snakes use continuous weighted skin. Canon silhouettes include CascadeFather's back-borne young,Helmwood's casque,Yellowfoot's shell/feet,SkySari's wings and talons,and MawToad's oversized overhead-readable mouth.

Only humanoids expose `Equipment.Head`, `Equipment.Hand.L`, `Equipment.Hand.R`, `Equipment.Back`. Actual equipped items reuse the six existing village equipment models (blade,club,staff,shield,pack,helmet). No catalog scene equips carried stock: all41 inventories in this captured ring had empty EquippedItems. Inventory and equipment ownership remain native.

## Known visual and validation limits

The refinement removes repeated raised ground flecks and centered round patches, varies the pale descent slabs, and replaces dense Felling bark cards with fewer rounded growth ridges. Remaining art limits include faint ground-paint repetition visible in wide empty areas, repeated native rock-cluster profiles, simplified moss transitions and sparse water detail. The three scoped improvements are personally reviewed in matching gameplay crops; one-to-one ImageGen fidelity is not claimed. Native chunk geometry intentionally differs from generated reference embellishments: no invented stair runs, tents, abyss, ocean, trees, loot or gameplay obstacles are added.

Grovelands full-zone triangle totals rise because real snapshots contain hundreds of trees and vine walls. Read the exact per-model/per-zone budgets in `reports`; shared prototypes and static batching are not a promise of a particular frame rate. The current game low-detail mode changes render-target scale/shadows; this bundle does not claim mesh LOD or ground-detail filtering.


## Import corrections and receipts

Actual Unity import initially caught two geometry defects: four quartz models had collapsed apex rings, and several Felling bark caps were nonplanar self-intersecting ngons. Both were corrected in geometry: true single-apex cones; uniformly fitted convex plates with exact native-mask clipping and explicit cap/side triangles. The importer triangle/bounds/weld invariants were retained. The completed218-model Unity import then passed without discarded-polygon warnings; native integration and final visual acceptance remain separate parent-owned gates.

The baseline catalog was SHA256 `bb7a1f3f60083b19ae2e346d2b22c13a525f505d723d68152d9fa287938d3efb`. Its completed Unity import and independent source rebuild are historical receipts in the previously adopted bundle. This refinement has catalog SHA256 `1b77e93c6ab78b60a1ab01c2ded6a79758de6ba0d483426674c32f1cb3a5fe59`. Current `reports/fbx-roundtrip.json` verifies all 218 actual Blender FBX imports; `reports/source-topology.json` verifies no zero-area source triangles; `reports/scene-consistency.json` checks each saved scene against the final library and every native instance against its manifest. All eight overheads and three gameplay crops are freshly rendered from the current scene files. The captured seed has no placed GlowQuartzVein.

## Refinement after the first native Unity pilot

The current source removes raised repeating ground flecks, adds distinct worn ledge outlines, and gives the Felling roots fewer rounded flowing ridges. Ground has four painted variants per family. Tepui stone uses warm gray/brown stone and shallow moss seams; groves retain olive loam. Atlas slot56, previously the unused axis_x diagnostic swatch, is now dedicated root_bark paint; all other non-ground swatches are unchanged.

The 218-model,73-blueprint,55-owner contract and native placements stay fixed. Unique kit geometry is132,578 triangles, down from150,908. The largest captured zone is1,348,066, down from1,590,922. All twelve terrain bases are12 triangles; their richness is opaque palette paint, not extra cast-shadow microgeometry. This is independent of runtime Low mode, which does not claim mesh LOD or ground-detail filtering.

`reports/refinement-handoff.json` records the exact21 changed runtime FBXs and source/catalog/atlas allowlist;197 other FBXs remain byte-identical to the imported baseline. `reports/refinement-diff.json` records each changed model and all eight native triangle budgets. `reports/source-delta.json` verifies source/UV/color scope. `REFINEMENT-PLAN.md` contains RED/probe/review history. The before images remain in the earlier adopted art receipt; current `renders/*-gameplay-crop.png` use the same offline camera and lighting for comparison. These are Blender renders, not new Unity screenshots. The final native gate now passes78/78 with29 screenshots;80.023s of paced native sampling independently recomputes correctly. Read Docs/Verification/SpawnRing3D/PERFORMANCE.md for long-frame and workload limits; exact ImageGen fidelity and stable60fps are not claimed.


Surface polish follow-up: the accepted editable master and model FBXs now include the reviewed sculpt-normal pass. See `../ModelPolish3D/README.md` for the required fresh-build → polish → validate pipeline, original archive and preservation limits. `renders/polish-before.png` / `polish-after.png` are matched Blender galleries, not native screenshots. Per-model decisions are in `reports/polish.json`.
