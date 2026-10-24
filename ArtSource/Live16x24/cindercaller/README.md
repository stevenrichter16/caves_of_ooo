# Marlback Cindercaller — one native living body

Status: source art verified. Runtime ownership, reader, fog, combat, persistence and actual Game acceptance remain the integrating task's responsibility. This is a static body, not an animation sheet or a claim of whole-scene completion.

## Pipeline and exact mapping

The root task captured the actual seed65 east-route Cindercaller as a red `g` in `Docs/Verification/Live16x24/cindercaller-baseline-world.png`, then supplied `concept.png` and its exact `imagegen-prompt.txt`. The concept informed the narrower cap, scorched mantle and limb gaps. Its faux shading, inconsistent displayed grid, oversized scene placement and duplicate-looking legs were rejected. No concept or screenshot pixels were sampled, traced, cropped or downscaled.

The original body is authored as named integer-cell geometry in Blender. Its narrow smooth cap is shorter than the approved Gleaner's stepped ridge and narrower than the approved Scrabbler's broad dome. Both approved family bodies appear unchanged in the contact comparison. A short blunt face, tawny hind leg and two asymmetric foreclaws preserve the family anatomy. Rust and charcoal mantle edges provide a secondary cue without permanent flame, glow or smoke. The loadout's Cudgel and FireMoss are mutable gameplay objects and are not baked into this body.

| Field | Value |
| --- | --- |
| Exact blueprint | `MarlbackCindercaller` |
| Resource | `Sprites/Live16x24/cindercaller` |
| Native PNG | 16×24, one full frame, transparent background |
| Occupied bounds | `(2,5)-(15,23)`, exclusive right/bottom |
| Palette/coverage | 7 colors, 119 opaque cells |
| Baseline | Feet/claw finish at row22; row23 empty |
| Import | Point, 16PPU, centered tile pivot, no mipmaps or lossy compression |
| Motion | None; do not alias inherited Scrabbler motion |

Palette: ink `#342E25`, stone dark `#3C5148`, stone `#526657`, stone light `#7D8E71`, tawny `#A97542`, claw `#BCA576`, rust `#9B442B`. These are approved family colors. A red canonical glyph is not a requirement to recolor the whole creature red; scorched mantle and silhouette convey this exact identity.

## Editable sources and reproduction

- `source-cells.json`: 24 rows ×16 cells, top-left origin, downward Y, null transparency, named part/color pairs.
- `cindercaller.blend`: flat named-part mesh quads, orthographic native scene and same-scale comparison scene.
- `cindercaller.png`: direct native Blender render; byte-identical to the owned Resources PNG.
- `cindercaller-contact.png`: direct 8× geometry render, 768×320, with existing Scrabbler and Gleaner source geometry at the same scale.
- `manifest.json`: exact identity, dimensions, bounds, palette, resource mapping and SHA256.
- `verify.py`, `source-red.txt`, `source-green.txt`, `verification.json`: fail-before-authoring and final source checks.
- `preserved-art-hashes.json`: 65 existing approved/source/runtime art hashes captured before generation.
- Generator: `Tools/Live16x24/build_cindercaller.py`.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_cindercaller.py
python3 ArtSource/Live16x24/cindercaller/verify.py
```

The generator writes only this source directory and its new runtime PNG/meta. It imports existing render helpers without executing their generators. Existing Unity-normalized metadata is preserved on rebuild. Native/editable geometry uses no raster input. The validation uses Pillow read-only to compare rendered pixels; it does not author or edit images.

## Review and honesty bounds

Source validation was RED before the sprite/source files existed, then GREEN after generation. It checks all384 native cells against source values, exact8× contact blocks for all three family bodies, binary alpha, seven-color palette, narrow crown, separated limbs, baseline, explicit named anatomy, no held-equipment/effect parts, exact Resources bytes, importer settings, distinct silhouette from both family comparators, and preservation of all65 prior art hashes.

Native and contact were visually inspected: the new body remains within one cell, its cap reads between the squat and tall approved relatives, and the rust rim is restrained. No material source-art blocker was found. At ordinary1×scale the eye and charred seam are intentionally minor details; live readability must rely on overall cap/body outline. This art does not claim four-direction facing, equipment overlays, casting, damage, movement, state-specific animation, Steam Deck performance or release quality. Those require separate authored states and actual gameplay evidence.

Final native SHA256: `ab7fb7010cd20d3b9edab00cd48747aade51c124f8916eff6df0a5534ef72394`.

## Integration follow-through

Root completed bounded ordinary world/Examine, real NPC movement and F5/F6
acceptance after native334 distinct passes. See `Docs/GRAPHICS-G2-CINDERCALLER.md`
and actual PNG/JSON evidence. This source sheet remains a static body and does
not claim animation, full release or a current packaged build.
