# Shared camp cloth — original native source

Three original 16×24 cells, rendered directly from editable integer unit-quads in Blender. This packet is a candidate for actual gameplay integration; the contact is a source study, not a screenshot or whole-area acceptance.

| Frame | Real owner | Source treatment |
|---|---|---|
| 0–1 | `TentWall` | Fully opaque, straight cell boundaries. Two broad slack charcoal cloth folds with warm ochre supports and small ties. The two variants move the folds and unequal facets. |
| 2 | `GuestClothPole` | Slender passable post, short arm and small dark hanging cloth; transparent padding, baseline row 22, empty row 23. |

The atlas is 48×24. Runtime slicing uses each complete 16×24 cell, centered pivot and 16PPU. Point filtering, no mipmaps, no compression and no NPOT scaling are recorded in the new `.meta`. No collider is authored by the art. TentWall retains its actual Cloth material, 8HP, thermal state and solid cell; the pole retains its passable body and has no new timer or service. Burning, removal and actor coverage remain runtime responsibilities. No roof, overhang, movement, animation or extra owner is depicted.

## Reference critique and extraction

`scene-reference-01.png`, its exact root-authored prompt and `reference-manifest.json` are preserved. The generated image provides a dark woven-cloth/ochre-post direction only. It changes the original 1280×800 game capture to 1586×992, alters UI and lighting, enlarges/merges parts and changes wall footprints. Those changes are rejected. No generated pixel, inferred building outline or raster tracing enters these cells. The actual wall and pole reader captures are input witnesses, not a claim that the new art has been played.

The new drawing uses eight opaque flat colors and binary alpha. Broad folds are kept quiet instead of adding woven noise. The wall is a full blocked-cell presentation, not physically raised geometry; every editable pixel quad lies on z=0. The pole is 89 opaque pixels inside bounds `[6,2,13,23]`. In the connected contact, repeated timber spacing may still be strong; actual normal zoom and Beating grading must judge this and whether the small hanging cloth stays readable. Accepted original sources are unchanged.

## Editable and verification artifacts

- `source-cells.json` stores every top-left-origin pixel as a named part/palette pair or transparent null.
- `palette.json` contains the eight RGB swatches.
- `editable.blend` contains the three reusable part-group collections, a native atlas scene and direct-geometry contact scenes. Each occupied pixel is one axis-aligned unit quad with an emission material, no texture tracing.
- `shared-camp-cloth.png` is the direct 48×24 render; `contact-native.png` and `contact.png` are direct geometry at 1× and8×. They are not resampled raster edits.
- `source-red.json` records the expected missing-source failure before the atlas existed.
- `source-verification.json` checks all 1,152 native pixels, 324,480 placed contact pixels, both wall opacity and pole transparency, and rejects six intentional corruption cases.
- `blend-geometry-verification.json` checks the reopened saved blend's 857 unit-quads/37 named groups, exact parts/colors/placement and rerender image chunks. The first strict whole-file comparison failed because Blender's `File` text stamp changed from `<untitled>` to the saved path. Every pixel and image chunk was identical. The corrected verifier allows only that one provenance text difference; both PNGs are retained.
- `protected-inputs.json` pins 21 exact reference/helper/editable/approved source files plus six metadata files. Metadata checks permit trailing-whitespace differences only and never rewrite them. This is a finite input audit, not a claim about all project files.
- `publication.json` records the new runtime-copy hashes. It does not claim runtime tests or gameplay acceptance.

Reproduce from the project root (Blender 5.2.1 used):

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shared_camp_cloth.py -- --build
python3 Tools/Live16x24/build_shared_camp_cloth.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shared-camp-cloth/editable.blend --threads 4 --python Tools/Live16x24/build_shared_camp_cloth.py -- --inspect-blend
```

The one-time `--snapshot` refuses repinning an existing protection record. `--publish` refuses overwriting an existing runtime atlas or meta. Pillow is used only to decode and verify outputs; it never creates, edits, scales or saves these images. Native tests, actual travel/readers/state cues and scene-fullness review are separate root-owned gates.
