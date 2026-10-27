# Isolated shale parity — original native candidate v4

Status: v4 source frozen and exact runtime copies published after root reported
the genuine72-frame/Rubble contract RED (44discovered,41expected failures and
3fallback passes). This is approval for actual comparison, not visual parity,
scene acceptance or gameplay verification. Root owns Unity/runtime/tests/captures.
The user paused broad coverage to concentrate on this isolated cave scene.

## Current source contract

`shale-parity.png` is1152x24, with72opaque16x24cells: eight ShaleFloor variants
0..7 and64ShaleWall cells at `8 + exposureMask*4 + variant` (four variants).
Exposure bits N1/E2/S4/W8 mean actual disclosed valid floor neighbors; unknown
space never supplies an exposed face. Mask0 is joined/unknown continuation.
Every pixel stays inside the actual owner cell; there is no extruded geometry,
new collider, simulation owner, shifted boundary or changed disclosure.

`shale-parity-rubble.png` is a separate16x16 binary-alpha source for an actual
selected Rubble owner in this isolated study. Three unequal gray fractured plates
have68opaque pixels in bounds[1,5,15,15], with shallow contact shade and five tones.
It is not floor decoration, a loot item, a pile-shaped obstacle or a replacement
for the original Environment/rubble.png. Normal presentation and optional-resource
failure retain that untouched original. Runtime selection/guards are root-owned.

`source-cells.json` and `rubble-source-cells.json` hold editable named part/color
cells. `shale-parity.blend` contains their exact unit-quad meshes, direct native
renders and contact scenes. The16x16 source uses the established24-coordinate
helper with a documented negative8 native instance offset; all resulting pixels
are inside its16x16 canvas. No image is traced, recolored, cropped or downsampled.

`contact-native.png`, `contact.png` (direct4x), `material-study.png` (direct4x)
and `rubble-contact.png` (direct8x) are geometry-rendered source studies, not game
screenshots. The synthetic room is deliberately not the actual map.

## Current shape and palette choices

Terrain palette is exactly v3. Floor base63,68,74 remains calibrated to root's
neutral-grade/ambient1 experiment; there is no baked amber compensation. Chips
use72,77,83 middle,82,87,93 edge and55,60,66 shallow shade. The eight literal
asymmetric contours vary orientation, notch/corner geometry and one/two clusters,
with15,18,35,38,40,42or46changed pixels per cell (mean34.5/384,8.98%). Two sparse
variants create quiet intervals; highlights follow off-center edges rather than
an apex. Floor source means range63.20–63.75/68.20–68.75/74.20–74.75.

Four wall fields use6/6/7/6 broad unequal ledges, mostly3–4pixels high, with different
vertical schedules, clipped corners, interrupted highlights and coherent recesses.
Applied wall tones are17/18/20,29/32/37,39/43/51,49/55/63,61/67/76and74/81/89.
The inherited unused84/91/99 palette entry remains reserved, not applied.
Joined-wall source means are42.15–43.38/46.63–47.93/53.89–55.35. These are source
statistics, not the actual graded next capture or acceptance thresholds.

Rubble colors are contact44/49/55, lower69/75/82, face91/97/104, facet112/118/124
and one restrained fresh126/132/138 pixel. Its stronger grouped fragments and
contact shade distinguish the real body from low-contrast floor chips. Inspect
angularity versus a polished-pebble reading at actual size.

## Verification and preserved history

`iteration4-source-red.txt` preserves the old v3 output failing the new source
contract before v4 geometry output. Earlier source RED and candidate receipts
remain historical evidence, not additional Unity tests. Root's native contract
RED is separately retained in the gameplay verification folder.

The current verifier checks all27,648terrain pixels,256Rubble pixels and
1,319,424placed contact pixels. All72terrain cells are distinct; exact parts,
palette, alpha, exposure bits, edge bases and variable floor density are checked.
Twelve adversarial controls reject incorrect canvas, alpha, altered source,
wrong mask/variant, invented parts and filtering. Actual runtime copies must
match source byte-for-byte; staged verification explicitly allows the old copy
and records that boundary without claiming publication.

Two fresh Blender builds match all eight deterministic outputs byte-for-byte.
Both reopened saved blends pass exact geometry/emission checks. The final blend
has27,716unit faces and1,122named mesh clusters; binary blend identity across
sessions is not claimed. Only volatile Cycles timing text chunks are removed
from PNG output; pixels and other PNG chunks are not modified.

Protected-inputs.json retains150original inputs, including49preexisting dirty
files. Earlier candidate-v1/v2 pins remain unchanged. Candidate-v3/ freezes its
complete42-file source/builder/runtime packet; iteration4-protected-inputs.json
adds the actual10 capture and original Rubble PNG/meta for45additional exact
pins. Nothing is automatically repinned. No older approved source or resource is
changed. The new terrain meta/GUID was preserved byte-for-byte on v4 publication;
the Rubble derivative has a separate new GUID with Point/Clamp/no-mipmap/no-NPOT/
uncompressed16PPU import intent. Root creates cached centered FullRect views.

Current terrain SHA256:
`0179f1943ec61982fdd533f4482a277a6b091d5ed426bfa6d737bf6070e4cfb8`

Current Rubble derivative SHA256:
`fd31b664e0ddc78fb816621c3ef00b82b8e1e608996462b0e1ef4ef290164162`

Original unchanged Rubble SHA256:
`dfb2097e05743e87e5c21aaa022e3bed9f5c4647383819a0c27027e7fff63172`

See publication.json, source-verification.json, blend-geometry-verification.json,
reproducibility.json and frozen-handoff.json for exact hashes and source bounds.
PARTS-AND-REFERENCE.md retains the historical decisions and actual named defects.

## Commands and remaining visual questions

Run from repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --build
python3 Tools/Live16x24/build_shale_parity.py --verify-source
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shale-parity/shale-parity.blend --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --inspect-blend
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --publish
python3 Tools/Live16x24/build_shale_parity.py --verify
```

Publication requires root's authorized import window. Do not rebuild frozen
pixels just to update receipts. Parent and independent source reviewer observed
improved floor topology but warned the broad flat wall faces might read as smooth
staggered blocks rather than fractured slate. Floor contrast could still be too
low, and Rubble might read as rounded beads at native size. These are named
actual-comparison risks, not reasons for another contact-only revision. Freeze
v4 for root's controlled gameplay capture. Source proof is not gameplay, parity,
whole-region polish, performance or device acceptance.
