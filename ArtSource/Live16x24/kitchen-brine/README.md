# Kitchen brine: native source

Current pass: native source authored and verified. Runtime mapping and ordinary
gameplay acceptance belong to root and remain pending here.

The named visual defect is the pair of blue wave glyphs on the actual kitchen
BrinePool owners. The saved Mac baseline is
`Docs/Verification/Live16x24/kitchen-brine-02-before-world-2x.jpg`.
Root recorded real brine owners, liquid/coating and material semantics in
`Docs/GRAPHICS-SIX-SCENE-POLISH.md`. This source does not alter any mechanics.

## Reference and extraction

`concept.png` and `imagegen-prompt.txt` preserve root's built-in imagegen pass.
`references.md` preserves the external salt-crust direction and approved kitchen
palette reference. The source generator never loads, samples or crops any raster.

| Extracted part | Native decision | Owner/state boundary |
| --- | --- | --- |
| Dark teal water interior | One quiet uneven flat mass, using established water colors | Stable BrinePool material identity; no depth, volume or potable-water claim |
| Broken grey-green salt deposit | A few restrained connected patches around an asymmetric edge | Suggests salt water, not an inventory resource or collectible salt |
| Dark mud boundary | Irregular low outline with interruptions in the pale crust | Natural ground pool, not a constructed clay bowl, raised rim or solid obstacle |
| Quiet horizontal water mark | One short low-contrast line | Static material detail, not motion, current, shine or active reaction |

Reject the concept's near-uniform raised ring, fine noisy highlights, rounded
antialiasing and lighting. Also reject glow, electric sparks, ice, bubbles,
steam, empty-state variants and any inferred irrigation link. One 16×24 cell,
one neutral frame, binary alpha and six flat colors are the complete art scope.
Existing approved kitchen objects and ground are unchanged.

## Production and verification

Editable named parts are planar integer-cell meshes in `native.blend`, paired
with `source-cells.json`. Blender renders `native.png` directly at 16×24.
`contact-native.png` and `contact.png` render the same geometry on approved
ground at 1× and 8×; they are source inspection views, never gameplay captures.

Run:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_kitchen_brine.py
python3 -B ArtSource/Live16x24/kitchen-brine/verify.py
```

The verifier is saved and run before native source authoring. It must fail on
the missing packet first. GREEN requires exact decoded native/source and direct
contact agreement, binary alpha, restrained palette and silhouette, resource
byte equality, valid 32-character metadata GUID, Point/Clamp/no-mips/PPU16 and
preserved references. Native source checks do not prove runtime owner admission,
reaction overlay behavior, import success or actual scene quality.

## Source review and handoff

- RED: `source-red.txt` records the expected missing native packet assertion,
  run before generator/source production existed.
- GREEN: `source-green.txt` and `verification.json` record one 16×24 frame,
  123 opaque cells, six colors, binary alpha, 10 salt cells and a single
  three-cell water mark; zero decoded native/contact mismatches and matching
  final resource bytes/import metadata.
- Author's actual 1× and direct 8× inspection: the asymmetrical dark surface
  and interrupted low salt patches read as a quiet ground pool. The concept's
  continuous raised vessel-like ring is deliberately absent. No extra texture,
  motion frame, empty state or stronger lighting is justified by this pass.
- Two consecutive builds passed the same decoded-pixel checks against
  byte-identical editable cells. `rebuild-verification.json` records an honest
  limitation: Cycles writes varying render-time text metadata in the PNG, so
  file hashes can differ despite identical pixels. No raster rewrite was used
  to force byte determinism. The manifest pins the final direct native PNG.

Resource: `Assets/Resources/Sprites/Live16x24/kitchen-brine.png` and its metadata.
The final resource contains exactly one static frame at index 0 and borrows no
other owner identity. Root must confirm current graph/owner and state routing,
correct overlays and actual cell readability before accepting gameplay.

Next: root's independent source review, runtime RED/GREEN and ordinary gameplay
comparison. Leave accepted source art stable unless that comparison finds a
named defect. No Unity session, runtime code or gameplay changes were made by
the source author.

## Root integration acceptance

2026-10-27:192/192 native tests passed; original1280×800 gameplay captures03–08
show actual1×/2× world, shared reader, player cover/uncover and real briny status.
Source and actual-image independent reviews found no further named defect.
Runtime scope, profile limits and pending packaged delivery are documented in
`Docs/GRAPHICS-KITCHEN-BRINE.md`. No whole-opening or physicalDeck claim.
