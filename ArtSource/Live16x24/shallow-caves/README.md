# Shallow cave wall source — frozen for the first actual trial

2026-10-27. Two original opaque 16×24 `SandstoneWall` cells, side by side in
`shallow-caves.png` (32×24). The root reviewer accepted the direct native and
8× contacts for an in-game trial. This packet does not certify actual cave
readability, a new build, Steam Deck performance or completed area polish.

## References and extraction

The root retained the actual ordinary Editor route at
`Docs/Verification/Live16x24/shallow-caves-before-02-depth1-entry.png`, with its
broader route/census in `shallow-caves-before-play.json`. These are Editor
captures loaded from an earned fixture, not a new standalone cave run.
`scene-reference-01.png` and `parts-reference-01.png` are imagegen concepts;
their exact prompts and the root's `REFERENCE-CRITIQUE.md` remain unchanged.
The scene image's raised rubble, cast shadows and altered stair are rejected.
The parts image supplies only the planar material direction: its noisy
surface, outlined tiles, irregular pixel scale and incorrect row labels are
also rejected. The critique records the NPS Rock Strata reference and its
limited factual use; no external photograph is incorporated.

The new source consists of original named integer-cell geometry. Neither
reference image is decoded or sampled by the builder. This is not a reduced,
traced or cropped generated image. Existing approved pixels are unchanged.

## Owner, material and frame contract

| Frame | Rectangle | Real owner/state |
| --- | --- | --- |
| 0 | `(0,0,16,24)` | Surviving `SandstoneWall`, first sparse internal fracture pattern |
| 1 | `(16,0,16,24)` | Same owner/state, second sparse internal fracture pattern |

Both cells are fully opaque. Every outermost pixel is the same base color,
so neighboring walls form a continuous planar mass without edge masks or
tile outlines. There are no masonry joints, rock objects, bevels, extrusion,
cast shadows or transparent holes. Frame0 has367 base pixels and17 detail
pixels (95.57% quiet base); frame1 has368/16 (95.83%).

The four original colors are mass `#5A4B37`, fracture `#4C3F2F`, seam
`#544634` and worn interior flecks `#64543E`. The mass is darker than the
unchanged Beating floor base `#806A4C` (RGB128,106,76). The contact's lighter
notch uses only that floor RGB on a flat geometry plane; it is not a copy or
modification of a floor sprite and it is not gameplay. The native renderer
paints native foreground/local ground in their authored colors; depth ambient
0.28/0.22 is not assumed to multiply these pixels.

The blueprint uses structural `DestructiblePart` HP30/max30/hardness2, not a
creature health stat. Surviving partial damage keeps the intact wall mass.
Actual destruction removes the wall and creates its real `Rubble` owner;
this source does not invent a broken-wall state or change collision/repair.

The root's separate runtime slice reuses five existing families: Beating
floor12–15, Environment rubble/stalagmite, opening-anchors down stair4 and
opening-cellar up stair0. This builder neither copies nor changes them.
Root owns the new runtime metadata and Assets import. Intended new wall
import: Point, Clamp, no mipmaps, uncompressed, centered pivot,16PPU,FullRect.

## Editable production and reproduction

`shallow-caves.blend` has two source collections with768 original unit quad
faces in11 named mesh clusters. Cells lie on a flat integer grid at z0;
the exact native scene instances them at x0 and16. `source-cells.json` records
every named part/color. `manifest.json` records source ownership, frame/state
mapping, contacts and nonpixel PNG metadata normalization.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shallow_caves.py -- --build
python3 Tools/Live16x24/build_shallow_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shallow-caves/shallow-caves.blend --threads 4 --python Tools/Live16x24/build_shallow_caves.py -- --inspect-blend
```

The native atlas and both1×/8× contacts render directly from those same
collections at final resolution. There is no raster resize or paint step.
Only volatile `cycles.*_time` PNG text chunks are removed after render;
IHDR/IDAT, alpha and color chunks retain their exact rendered bytes. Pillow
is used only for independent decoding/checking, never image creation/editing.

`protected-inputs.json` pins20 finite reference/helper/approved PNG/editable
files byte-for-byte and five borrowed import metadata files after explicitly
removing trailing horizontal whitespace only. This accounts for Unity's
pre-existing empty-field spaces without hiding changes to GUIDs, structure,
settings or values. Initial raw hashes remain provenance. Relevant import
fields are checked separately; a filter-change counter is rejected. Neither
raw metadata nor existing art is rewritten. Evolving coverage records and
other agents' unrelated JSON files are not dependencies.

## Retained source checks and limits

- `source-red.txt`: before production, verification failed because the native
  atlas did not exist. This is source RED, separate from root's native tests.
- `source-green.txt` / `source-verification.json`:768 native pixels exactly
  match source cells;324,480 contact pixels match direct integer placement;
  opacity, quiet base, shared edges and input protection pass. Nine deliberate
  corruptions are rejected, with one positive whitespace-normalization control.
- `blend-check-first.txt` and `blend-check-final.txt`: reopened saved geometry
  matches every cell/part/color; final check also validates actual emission
  material colors, unit faces, untransformed instances and exact frame order.
- `rebuild-before.json`, `rebuild.log`, `reproducibility.json`: two fresh Blender
  builds produce byte-identical atlas, both contacts and source cells. Blender
  file bytes differ because of session metadata; both saved sources passed
  geometry inspection. The final blend hash is recorded in the frozen handoff.

Source verification and an approved contact do not establish actual scene
fullness, correct owners/disclosure/readers, destruction integration, depth2
travel/return, packaged behavior or hardware performance. Actual cave views
must judge contrast and repetition. Root's gameplay/runtime records are the
authority for those separate gates. No further source pixel changes are planned
before that trial provides a named defect.
