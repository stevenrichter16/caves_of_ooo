# Editing and exporting the Morrowfast 3D village

The village is an editable Blender kit connected to the existing Morrowfast game state. The Blender scene supplies appearance; native game entities still own movement, doors, room discovery, inventory, equipment, interaction and saves.

## Open and edit the art

Open `ArtSource/Village3D/village_master.blend` in Blender 5.2.1 LTS. It contains the assembled overhead village, studio camera and lights, plus reusable asset collections.

`ASSET_LIBRARY_EDITABLE` holds the original pieces: stones, planks, foliage, cloth, furnishings and character parts. This collection is excluded from the render view layer so prototypes do not appear together at the world origin. Enable it while editing a prototype, then exclude it again for the assembled scene. The village uses collection instances, so editing an original updates its instances.

House shells, roofs and doors are independent assets. Furnishings remain separate owner models; the roof cutaway can expose them. Repeated prop and vegetation families have several variants. The character kit has four hood colors, a simple nine-bone rig, five animation takes and four equipment sockets.

The reproducible source is `ArtSource/Village3D/build_scene.py`. It reads the adjacent `native-definition.json` and `art-definition.json` snapshots and uses art seed `9042026`. A source rebuild regenerates the scene: manual edits to the `.blend` are not automatically read back into that script. Keep a separate edited scene or incorporate durable changes into the builder before replacing a generated master.

## Rebuild the source and FBX files

From the repository root, run:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 2 \
  --python ArtSource/Village3D/build_scene.py -- \
  --output /tmp/codex-village3d-export --render
```

The completed export directory contains:

| Output | Purpose |
|---|---|
| `village_master.blend` | Editable source collections and assembled village |
| `models/*.fbx` | Separate models, roofs, doors, rigs and attachments |
| `textures/VillagePalette.png` | Shared 1024×1024 texture atlas with 64 painted swatches |
| `manifest.json` | Model IDs, native owner bindings, offsets, rooms and scenery placements |
| `renders/village_overhead.png` | Full roof-on composition |
| `renders/village_cutaway.png` | Authored interiors with roofs hidden |
| `reports/validation.json` | Source counts, bounds and validation limits |

`--skip-export` builds the source scene and manifest for a quick art preview. `--probe-only` exports only the labelled metre/axis fixture. The normal build exports 123 runtime models; the separate `axis_probe.fbx` retains its named diagnostic markers and is not a runtime library entry.

Generate the detailed layout and geometry-budget reports after the build:

```sh
python3 ArtSource/Village3D/reconcile_layout.py /tmp/codex-village3d-export
python3 ArtSource/Village3D/budget_report.py /tmp/codex-village3d-export
```

The master packs the palette image and also retains a relative `textures/VillagePalette.png` reference. Runtime models use a single palette mesh/material group, plus a separate water group where needed. The exporter keeps the editable source pieces intact.

## Import into Unity

With Unity open and outside Play mode, choose **Tools → Caves of Ooo → Village 3D → Import completed Blender export**, then select the completed export directory.

For a command-line import, close the Editor instance using this project first, then run:

```sh
/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath /Users/steven/caves-of-ooo \
  -executeMethod CavesOfOoo.Editor.Village3DAssetBuilder.BuildFromCommandLine \
  -village3dSource /tmp/codex-village3d-export \
  -village3dReport /tmp/village3d-import.json \
  -logFile /tmp/village3d-import.log
```

The builder checks the manifest against native Morrowfast, imports the FBX files, assigns the project materials, creates model prefabs and character controllers, and updates `Assets/Resources/Village3D/Library.asset`. Owned models, textures, materials, prefabs, animations and rendering assets live under `Assets/Art3D/Village/`.

Reimport through this builder to preserve established Unity asset GUIDs and the verified imported transforms. It adds or reuses the dedicated 3D renderer and enables soft-shadow support while preserving the existing default renderer. It does not add physics bodies or colliders to the art. The import report records changed files and validation failures; obsolete assets are retained rather than silently deleted.

The builder validates asset structure. Actual in-game rendering, animation, interaction and performance still require the native play checks recorded in the implementation plan. Open `Assets/Art3D/Village/Scenes/VillageShowcase.unity` for the fully revealed art scene. During ordinary gameplay, F11 switches between 3D and the existing presentation; Shift+F11 reduces cosmetic scenery, shadows and render resolution. These display controls do not consume a turn. New games start on the southern Morrowfast road at (40,23); loading a save retains its saved location.

## Reuse a model or attach equipment

Use the prefabs under `Assets/Art3D/Village/Prefabs/` for Unity presentation. The runtime library maps stable model IDs to those prefabs. Preserve the imported child transforms inside any additional wrapper; the exporter already supplies the tested axis correction.

Character animation takes are `Idle`, `Walk`, `Interact`, `Attack` and `Hit`, at 24 frames per second. Idle lasts two seconds; the other takes last one second. The animation has no root translation. Blender may import an FBX take as several per-object actions; those actions still belong to the same five takes.

The attachment transforms are exactly `Equipment.Head`, `Equipment.Hand.L`, `Equipment.Hand.R` and `Equipment.Back`. The reusable attachment models are:

| Model ID | Attachment origin |
|---|---|
| `equipment-blade` | Hand grip |
| `equipment-club` | Hand grip |
| `equipment-staff` | Hand grip |
| `equipment-shield` | Grip at shield centre |
| `equipment-pack` | Back attachment, pack extends downward |
| `equipment-helmet` | Head attachment |

Blade, club and staff extend along source local up; tilt them toward the character's facing direction for a readable overhead silhouette. A prefab attachment displays equipment already owned and equipped by the native entity. Adding an art attachment alone does not grant an item, alter statistics or create inventory state.

## Coordinates and native ownership

One Blender metre equals one game cell. Blender uses X east, Y north and Z up; Unity uses X east, Y up and Z north. Native cell `(x,y)` maps to Unity `(x + 0.5, height, 25 - y - 0.5)`.

The FBX preset is +Z forward/Y up with one export-only 180° rotation about Blender Z. Both forward presets without that explicit correction reversed the two horizontal axes in the actual Unity probe. The corrected probe passed all four Unity axis, scale and floor checks. Leave the authoring scene in its normal east/north/up orientation and retain the exporter correction.

All 71 owner IDs and the five native room/roof/door relationships are preserved. Owner views use cell centre plus their manifest `visualOffset`; retain that offset when the owner moves. House roots use native owner anchors, so their mesh bounds can be asymmetric. Door roots are left hinges, 0.45 units west of the door cell centre.

The reference composition occupies X 21.25–58.75 across the 25-cell height. Terrain covers the full 80×25 zone so following an actor beyond the central composition does not expose a void. The shipped 67-cell western creek, native building footprints and door cells take precedence over ambiguous reference pixels. `reports/layout-reconciliation.json` records exact native footprints, reference feet, model offsets, rooms, water and decorative additions.

Static fences, vegetation, stones and decorative planters have no independent harvesting, collision, destruction or persistence mechanics. Native mutable owners remain separate. The Blender showcase player is a preview object and is excluded from the persistent owner manifest. Visibility, hidden-room state, lighting and selection must continue to follow the native game.

## Rebuild verification

A rebuild audit compares the parsed manifest and exact palette PNG hash, then compares imported FBX geometry, UVs, material assignments, weights, skeletons, sockets and animation curves. Numerical mesh and transform values are normalized to six decimal places. FBX files can contain different timestamps and source paths, so binary equality is not the reproducibility contract.

The 2026-09-09 independent rebuild passed for all 123 runtime models. The adopted builder and input snapshots matched their delivered hashes, parsed manifests matched exactly, and the palette PNG was byte-identical. All four character rigs retained their takes and sockets. None of the FBX container files was byte-identical, so the verified claim is reproducible exported content, not identical FBX bytes. The separate axis fixture also retained its named-marker geometry.

The current source estimate is 362,905 placed triangles across the whole zone and 328,705 across the conservative central reference crop. These counts include both roofs and their hidden room contents; they are asset estimates, not measured Unity frame times or draw calls. Use the implementation plan's native captures for performance acceptance.


The refined kit preserves all123 models,71 native owners and111 static placements. Its conservative crop exceeds the initial300,000-triangle design target; fuller foliage and cloth are a documented visual-budget exception subject to native profiling.

Six bare soil cells beside the arrival lane—X41/X42,Y21..23—are recorded by `starterGardenCells` and rendered inside `detail-patch-04-00`. Fresh bootstrap marks the existing native terrain plantable; seed planting uses the ordinary crop mechanic. The road at X40 stays clear.
