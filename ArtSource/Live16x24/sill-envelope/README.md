# Sill static surface source study

2026-10-27 UTC. **Source integrated and observed in actual Unity Play; package and whole-area completeness remain pending.**
The root visually reviewed the revised winding-strand contact and accepted it as
an integration candidate. Whole Sill composition, discovery, transitions and
state handling remain the next gates.

## Inputs and corrections

- Real playable input: `Docs/Verification/Live16x24/portable-equipment-before-01-sill.png`.
  The revealed chamber/corridor uses actual owners; the large dark shapes are fog.
- Built-in imagegen concept and exact prompt:
  `ArtSource/Live16x24/concepts/sill-envelope/`.
- The generated board incorrectly enclosed fog edges with new walls. Reject
  that geometry. This source creates individual surface frames only, no level
  layout, world entities, shadows, paths, loot or service fixtures.
- VineWall is `Plant` material in `Assets/Resources/Content/Blueprints/Objects.json`,
  not a timber fence. Thick living winding stems and sparse leaf pairs replace
  the concept's heavy posts. Its accepted sibling Hedge image is unchanged.
- `VillageDoorPlacementBuilder` sets `DoorPart.QuarterTurns` from the real
  aperture. Four essential door frames cover both axes and open/closed states;
  the initial two-frame proposal was corrected before completion.
- `DoorPart.IsClosed` also includes a locked owner. These neutral door surfaces
  do not promise an unlocked passage or add a lock symbol. Runtime must select
  actual state and preserve existing authority checks.

## Native frames and owner mapping

All frames are square-pixel16×24 on a384×24 single-row atlas. PPU16,
center pivot, Point filtering, Clamp wrap, no compression or mipmaps. Foreground
is transparent except authored pixels; the four ground frames are fully opaque.

| Frames | Owner/material | Selection | Limits |
| --- | --- | --- | --- |
|0–15|Exact VineWall / Plant|Cardinal disclosed connection mask N1/E2/S4/W8.|Living barrier only; no invented neighbors, topology or construction posts. Isolated mask0 is the conservative portrait/memory candidate.|
|16|Exact VillageDoor|QuarterTurns even, IsClosed true.|Panel crosses east-west wall; passage runs north/south.|
|17|Exact VillageDoor|QuarterTurns even, IsClosed false.|Same aperture; visible central north/south opening.|
|18|Exact VillageDoor|QuarterTurns odd, IsClosed true.|Panel crosses north-south wall; passage runs east/west.|
|19|Exact VillageDoor|QuarterTurns odd, IsClosed false.|Same aperture; visible central east/west opening.|
|20–23|Exact Grass substrate|Four restrained native surface variants.|Low grass/moss/silt only. No raised stones, harvest crops, loot, ownership, road or hazard claim.|

Do not rotate the door raster: the vertical alternative is independently
hand-authored on the same16×24 grid. Optional masks and state selection are not
a runtime implementation. Root owns scope admission, owner selection, current
graph/visibility, material validation, connection updates and real interactions.

## Editable authoring and visual iteration

`Tools/Live16x24/build_sill_envelope.py` defines named pixel parts as original
integer geometry. It does not load, sample, crop, trace or resize any raster.
Each colored cell is an editable front-facing quad in `sill-envelope.blend`;
`source-cells.json` preserves exact named parts and palette per native cell.
The atlas and enlarged contact are rendered directly from those quads.

The first VineWall pass looked too much like mottled stone or hedge. Root review
requested clearer winding strands. The final revision removes procedural
mottling, uses two explicit winding strands per axis and sparse attached leaf
pairs. It retains all original connection footprints, frame count and restrained
palette. Ground remains deliberately quiet beneath actors and objects. Filling
chunks requires actual coherent placement and material rhythm, not higher noise
in every tile; this contact cannot establish scene-level fullness.

## Verification

- `source-red.txt`: missing native output fails before authoring/export.
- `source-green-final.txt` / `verification.json`:24 distinct frames,
  exact source→PNG RGBA match, binary alpha, all four ground frames opaque,
  matching connection edge profiles and distinct open doors with clear route axes.
- Every authored contact pixel matches the source at direct6× geometry scale.
  Ten in-memory mutation counters reject bad frame count, dimensions, unknown
  palette, ground holes, missing/invented connections and misleading open doors.
-72 approved runtime PNG/meta input hashes remain unchanged.
- `rebuild-verification.json`: decoded native atlas and complete contact RGBA
  pixels are identical across independent final builds; editable source cells
  are byte-identical. PNG file bytes can differ because Blender embeds variable
  Cycles timing text metadata. No metadata stripping or raster editing was used.
- `saved-blend-verification.json`: reopens the saved Blend and checks all24 frame
  collections, palette properties, integer quad coordinates, per-frame pixel
  counts and expected scene names. No Unity editor was started by this author.

Reproduce:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_sill_envelope.py
python3 ArtSource/Live16x24/sill-envelope/verify.py
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/sill-envelope/sill-envelope.blend --python ArtSource/Live16x24/sill-envelope/inspect_saved_blend.py
```

## Remaining acceptance

Root integration, native RED/GREEN ownership/state counterchecks, actual Sill
before/after route and doorway interactions, surface continuity, save/revisit,
profiling, independent whole-scene review and packaged verification are pending.
No biome or area is complete merely because these24 source frames exist.

## Root integration receipt

Imported unchanged atlas into Assets/Resources/Sprites/Live16x24/sill-envelope.png.
See Docs/GRAPHICS-SILL-ENVELOPE.md for native RED/GREEN, actual doors/interior/river
views, save/reload and remaining fullness gaps. This source proof remains separate
from gameplay evidence; it does not certify every region or all Sill fixtures.
