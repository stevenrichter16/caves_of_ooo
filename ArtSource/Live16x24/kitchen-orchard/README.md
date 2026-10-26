# Kitchen court and orchard — native source packet

Status: source and bounded runtime/ordinary gameplay accepted; see
`Docs/GRAPHICS-G3-KITCHEN-ORCHARD.md` for native260/260 and actual harvest/reload/return evidence.
These contact/repetition renders are Blender source previews, **not gameplay**.

## Defect, references and extraction

The ordinary `packaged-glade-east-18-kitchen-court.jpg` and
`packaged-glade-east-22-continued-kitchen.jpg` captures show glyph `6` hive owners
and bead-like court ground competing with small functional props. Root's built-in
imagegen reinterpretations are
`ArtSource/Live16x24/concepts/kitchen-orchard/scene-v1.png` and `parts-v1.png`.
The exact prompts/provenance remain with that concept directory. Both concepts
were viewed before native authoring. They are shape references, not production
pixels; no concept raster was sampled, traced, resized, transformed or inserted.

Accepted extraction:

| Frame | Actual owner | State / footprint | Native interpretation |
| --- | --- | --- | --- |
| orchard-hive 0 | `Beehive` (exact blueprint spelling) | Existing unharvested owner, 1×1; removal removes art | Uneven wax dome, three broad coil joints, dark low entrance. |
| working-court-v2 0 | `StoneFloor`, `court-floor` role | Static coordinate variant 0, 1×1 | One broad irregular worn slab. |
| working-court-v2 1 | Same | Static coordinate variant 1, 1×1 | Unequal upper/lower pair; broad grass joint. |
| working-court-v2 2 | Same | Static coordinate variant 2, 1×1 | Offset long slab and smaller lower stone. |

Rejected: concept bloom, soft alpha, cast shadows, dimensional rim lighting,
bright gold ground, decorative bee activity, new harvest/working cues, hive
animation, detached decorations and copied generated texture. The hive is a
neutral identity, not proof of bee activity or available honey. Existing game
parts remain the authority. No Cook, service props, wall, plant, prior floor or
approved source asset has been changed by this packet.

## Editable production and verification

`Tools/Live16x24/build_kitchen_orchard.py` creates original named flat integer-cell
mesh groups in Blender and renders directly at 16×24 / 48×24. `source-cells.json`
is the editable cell and part/palette map; `kitchen-orchard.blend` retains exact
meshes, native scenes and direct 8× contact / 4× repetition scenes. Their enlarged
previews are newly rendered from geometry, not raster resizing. The hive palette
reuses exact opening-base ochre/linen/ink entries; ground reuses the quieter
working-court palette. Import metadata uses Point, Clamp, no mipmaps, no
compression and 16 PPU. Ground remains opaque #032F23 between stones. Hive alpha
is binary, with no pixels outside its cell. All source/resource PNGs match bytes.

Before authoring, `verify.py` failed with missing packet outputs (`source-red.txt`).
After the direct Blender build, the read-only verifier checks every native pixel
against source and every contact/repetition display pixel against native art,
component counts, quiet grass margins, palette, importer metadata and 65 preserved
source/art files. Native review accepted the asymmetric hive silhouette and
larger, lower-detail one/two-stone forms; all preview pixels are clean integer
cells. No visible isolated bead group remains in a variant. Ground palette is
intentionally subdued, so it should support actual props rather than draw focus.

Run from repository root:

```
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_kitchen_orchard.py
python3 ArtSource/Live16x24/kitchen-orchard/verify.py
```

`manifest.json` records resource rectangles, owner roles, palette/source references,
concepts, actual greybox captures and hashes. Previous `working-court.png`, its
Blender/source files, kitchen-service art and every pre-existing native PNG remain
unchanged, verified by `preserved-art-hashes.json`.

Runtime resolver/state admission and root-owned actual kitchen, hive examination,
harvest/removal, save/reload and zone-return checks are now recorded in the G3 doc. Source verification
cannot establish runtime ownership, lighting, scene cohesion, performance or
physical Steam Deck acceptance. No such acceptance is claimed here.

Rebuild counter-check: a whole-PNG byte comparison first failed because Blender
embeds variable Cycles elapsed-render metadata. The corrected test compares the
exact decoded RGBA buffers of all four renders and byte-identical editable cells
(`deterministic-rebuild.json`); those pass. Within each build the shipped resource
and source PNG bytes remain identical. No PNG was rewritten to strip metadata.
