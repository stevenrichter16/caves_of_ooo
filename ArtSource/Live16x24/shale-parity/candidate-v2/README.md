# Isolated shale parity — original native candidate

Status: candidate source approved for first actual gameplay comparison;
no visual-parity or whole-area acceptance. Root owns native runtime tests,
isolated integration, Unity, actual captures and Git. The user paused broad
coverage to concentrate on this scene. Existing shale art remains reversible
and unchanged; this packet is a new sibling.

`shale-parity.png` is576×24 with36opaque16×24cells. Frames0–3 are flat cool
ShaleFloor variants. Wall index is `4 + mask*2 + variant`, mask0..15, variant0..1.
Exposure bits N1/E2/S4/W8 represent **actual disclosed valid floor neighbors**,
not absent/hidden neighbors. Mask0 is joined/unknown continuation. Every exposed
face stays inside its wall owner cell. There are no transparent holes, perimeter
colliders, geometry extrusion, gameplay state changes or new simulation owners.

`source-cells.json` is editable named part/color data. `shale-parity.blend`
contains the corresponding unit-quad meshes and direct native/contact scenes.
All faces lie atz0, with exact emission colors and no smoothing/downsampling.
`contact-native.png`, `contact.png` (4×) and `material-study.png` (4×) are rendered
directly from the same geometry. They are **source studies**, not game captures.
The synthetic room arrangement is deliberately not the actual map.

## Palette

| Role | Exact RGB / hex |
| --- | --- |
| Floor base |63,68,74 /`3F444A`|
| Flat chip middle / light / contact shade |72,77,83 /`484D53`;82,87,93 /`52575D`;55,60,66 /`373C42`|
| Wall crevice / deep / mass |20,24,29 /`14181D`;29,36,44 /`1D242C`;39,47,58 /`272F3A`|
| Wall slab / facet / edge / cut |49,59,70 /`313B46`;61,71,83 /`3D4753`;74,85,96 /`4A5560`;84,95,107 /`545F6B`|

Each floor has three original11pixel shallow angular chip groups (33/384,
8.59% detail), with quiet base at every cell edge. Seven wall tones create
paired facets and crevices, with original irregular ledges1–3pixels high and
4–10pixels long. Source colors are calibrated for root's neutral-grade/light
experiment rather than compensating for normal amber Cave grading. Actual
normal-zoom color, visibility, joins and repetition still decide acceptance.

See [PARTS-AND-REFERENCE.md](PARTS-AND-REFERENCE.md) for the exact authoring brief,
owner mapping, concept critique and retained first-candidate rejection.

## Verification and preservation

`source-red.txt` records a failed source verification while the native atlas
was absent, before first geometry output. `revision-source-red.txt` preserves
the first candidate refusing the revised source contract. Neither is a Unity
runtime test or a gameplay claim.

The source verifier checks all13,824native pixels and1,022,208placed pixels in
direct contacts/study. Every generated cell matches the named geometry palette,
the36frames are unique, floor density/edges and all four exposure bits are
checked. Nine negative controls reject canvas, alpha, source, variant/mask,
pixel and importer corruption. Saved-blend inspection independently checks each
integer unit face, part name, actual emission color and native placement.

`protected-inputs.json` freezes150previous inputs, including all49preexisting
dirty files from root's shared-storage snapshot, the entire old shale source
packet/references, prior Live16x24PNG files, old imported shale/meta and borrowed
geometry helpers. `revision-protected-inputs.json` separately pins the rejected
first candidate. There is no automatic repinning or old-asset rewrite. Root's
broader snapshot covers other workspace files; this task's write scope is only
the new builder, this new source folder and the new runtime PNG/meta.

The runtime image is an exact source byte copy, with its own new metadata:
Point, Clamp, no mipmaps, no NPOT resizing, uncompressed, centered16PPU. Cached
FullRect Sprite/Tile views are created by root's adapter. The source verifier
also checks the runtime byte identity and importer fields after publication.
Any Unity serialization of the new meta is separate from original pixel proof.

## Commands

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --build
python3 Tools/Live16x24/build_shale_parity.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shale-parity/shale-parity.blend --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --inspect-blend
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --publish
```

Do not rebuild accepted pixels merely to refresh metadata. The second fresh
build comparison and reopened-blend receipts establish source reproducibility;
Blender session metadata may change binary blend hashes, so binary blend
reproducibility is not assumed. Only volatile `cycles.*_time` text chunks are
removed from rendered PNGs; image bytes/chunks are never edited or resampled.

Final candidate source/runtime PNG SHA256:
`eaeaa8276cdb0cbaa338dc4b98e716a5ca2d83de2506ec4d4fe54afc0381def1`.
The final reopened blend is
`25db483eeffe4b7a7e5d82680274a683ed4374ac1b9786d2ccafab24b714e1bc`:
13,824unit faces in552named editable mesh clusters. Both fresh builds have
byte-identical native PNG, both contact PNGs, material-study PNG and source-cell
JSON. All150old input pins,49included dirty files and14separately pinned rejected
candidate files pass. `source-metrics.json` records neutral source measurements:
floor mean63.453/68.453/74.453; joined-wall luminance p10/median/p90 approximately
23.51/46.09/69.74. These are source pixels, not actual graded gameplay metrics.

## Current limits

The source studies improve tonal range and relief but do not prove exact
reference parity, actual disclosure, gameplay movement/destruction/restart,
performance or device behavior. Root approved a trial, not the final result.
Named actual-view defects may justify further sibling candidate iteration;
prior versions and their receipts must remain preserved.
