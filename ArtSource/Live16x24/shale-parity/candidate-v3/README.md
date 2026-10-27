# Isolated shale parity — original native candidate

Status: iteration3 source approved and copied for its actual gameplay comparison
after candidate-v2 showed tiny repeating floor triangles and navy fine wall
lines. No visual-parity or whole-area acceptance. Root owns native runtime tests,
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
| Wall crevice / deep / mass |17,18,20 /`111214`;29,32,37 /`1D2025`;39,43,51 /`272B33`|
| Wall slab / facet / edge |49,55,63 /`31373F`;61,67,76 /`3D434C`;74,81,89 /`4A5159`|
| Retained unused wall palette entry |84,91,99 /`545B63`; current wall cells do not use this entry|

Each floor has two original18–19pixel shallow angular plates (37/384,
9.64% detail),5–7pixels wide and3–4high, with quiet base at every cell edge.
Six applied wall tones create paired facets and coherent crevices, with eight
original irregular ledges per variant,2–3pixels high and6–10pixels long before
clipping at cell edges. Source colors are calibrated for root's neutral-grade/light
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
first candidate. `iteration3-protected-inputs.json` separately pins30files of
the actual-tested candidate-v2, including its builder/runtimePNG/meta. There is
no automatic repinning or old-asset rewrite. Root's
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

Iteration3 source/runtime PNG SHA256:
`4d4900fc51fb8340e5cec5a4c511f2bc79aed440bc76ba0ba698918d94fcbe9e`.
The current geometry/reproducibility/handoff receipts bind the reopened blend
and13,824unit faces. Candidate-v2's complete receipts remain inside candidate-v2/;
do not treat its former hash or pixel statistics as the current source.
All150old input pins,49included dirty files,14separately pinned first-candidate
files and30candidate-v2 files remain protected. `iteration3-source-metrics.json`
records floor mean63.53–63.66/68.53–68.66/74.53–74.66 and joined-wall means about
42.5/47.0/54.2. These are source pixels, not actual graded gameplay metrics.

`iteration3-source-red.txt` precedes its native rebuild; source-only verification
explicitly records the staged/runtime mismatch while root retains the previous
candidate. After publication, ordinary `--verify` again requires exact runtime
byte identity. The root-approved v3 copy preserved the existing new meta bytes
and GUID. No previous resource/meta/source changed.

## Current limits

The source studies improve tonal range and relief but do not prove exact
reference parity, actual disclosure, gameplay movement/destruction/restart,
performance or device behavior. Root approved a trial, not the final result.
Named actual-view defects may justify further sibling candidate iteration;
prior versions and their receipts must remain preserved.
