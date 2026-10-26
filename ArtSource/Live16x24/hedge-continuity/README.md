# Connected hedge native source

Status: source pass2 and bounded Unity integration accepted. Native338/338,
ordinary destruction/traversal/save-return and actual1280×800 comparison are
recorded in `Docs/GRAPHICS-G3-HEDGE-CONTINUITY.md`. These contacts remain source
geometry previews, not in-game screenshots.

## Recorded input and extraction

- Actual playable input: `Docs/Verification/Live16x24/hedge-before-corner.png`
  and its JSON receipt; seed200893250, kitchen12.11.0, player7,4, tick4500.
  The L-corner is6,3, its vertical arm6,4–6 and horizontal arm7–16,3;
  another actual horizontal run is18–33,15. Positions are root-observed.
- Imagegen guidance: `ArtSource/Live16x24/concepts/hedge-continuity/scene-v1.png`,
  prompt and extraction README in the same directory. Accept continuous leaf
  mass, a definite turn and quieter internal rhythm. Reject fine noise,
  lighting/soft pixels, changed proportions and regenerated UI. No concept
  pixel is copied, traced, scaled or imported into the runtime resource.
- Mapping: exact existing `Hedge`, one neutral live identity, one cell, sixteen
  cardinal connections with N1/E2/S4/W8. No damage, growth or activity state
  is invented. Real owner removal opens a real gap; selection/disclosure and
  ownership are separate runtime gates.
- Frame0 is the exact approved editable `make_frames()[1]` from
  `Tools/Live16x24/build_north_vegetation.py`. The previous PNG, source and
  Blender file remain untouched, as do all other existing art resources.

## Source and delivery

`Tools/Live16x24/build_hedge_continuity.py` builds original flat integer-cell
emission meshes in Blender. `source-cells.json` records each native cell's
part/material ownership. `hedge-continuity.blend` contains editable collections,
native atlas and direct native/integer contact cameras. The source palette
is the unchanged north vegetation palette: leaf_dark435A35, leaf_mid53683E,
leaf75834B and wood_dark5F4830.

`hedge-continuity.png` and the byte-identical resource
`Assets/Resources/Sprites/Live16x24/hedge-continuity.png` are256×24: sixteen
16×24 frames in ascending mask order. Metadata is Point/Clamp, no mipmaps,
no compression, PPU16, centered pivot and binary alpha. The filename supplies
the native resource/texture name `hedge-continuity` expected by integration.

All connected E/W joins have exactly matching twelve-pixel cross-sections;
N/S joins have matching ten-pixel cross-sections. Sparse angular groups and
tiny outline irregularities stop short of the joining edges. No repeated
central shrub bulb, trunk, fence post, gradient, shadow or soft alpha is used.
An isolated approved hedge intentionally retains its original broader shape.

## Iteration and review

1. Source verifier ran RED with the uncreated native outputs named in
   `source-red.txt`. Runtime RED is separate and root-owned.
2. Pass1 solved connectivity but direct native/4× review showed smooth
   olive tubing and evenly spaced brown dashes. Parent independently found
   the same issue. `pass1-*` preserves its contact renders and editable cells.
3. Pass2 breaks the outer contour slightly away from fixed joins, introduces
   angular overlapping leaf groups, and replaces long dashes with sparse
   diagonal twig forks. The connected mass is still quieter than the original
   isolated hedge. Native and integer contacts show clear caps, corners,
   a complete enclosure, cross/T variants and a true one-cell gap.
4. `verify.py` compares every native pixel to editable cells; checks isolated
   pixels against the original atlas, all possible cardinal edge pairings,
   binary alpha, connected silhouettes, all-mask6× contacts, the native and4×
   layout, unchanged gap, source/resource bytes and import metadata. It also
   verifies42 previous source/art files remained byte-identical.
5. Parent independently reviewed the pass2 native and4× contacts and accepted
   them for integration. Art is now frozen pending actual Play comparison.
   A second full Blender build retained identical RGBA pixels in all four
   outputs and identical editable source cells (`deterministic-rebuild.json`).

Reproduce with:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_hedge_continuity.py
python3 ArtSource/Live16x24/hedge-continuity/verify.py
```

Deterministic rebuild checks compare RGBA pixels and editable source cells.
PNG container bytes can differ because Blender embeds render metadata; no
metadata stripping or raster rewrite is used to conceal that distinction.

## Self-review and bounds

- Fixed: pass1's tube-like outline and repeated twig dashes.
- Preserved: approved isolated source, prior42 source/art files and exact
  runtime-independent cell sizes/palette/joins.
- Intentional limit: a single frame per mask necessarily repeats internal
  motifs on long runs. Coordinate variants are outside this bounded atlas.
- Verified separately: actual scene comparison, ordinary destruction/revisit,
  renderer tests and profiling. Source contacts alone cannot establish these.

Next action: carry the accepted source into current packages and verify ordinary
Mac gameplay. Linux execution and physical Deck acceptance remain open.
