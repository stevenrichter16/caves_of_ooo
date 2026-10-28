# Beating environment — original twelve-frame material revision

Source-only first candidate for an actual game trial. The 544×24 atlas keeps the existing 34-frame contract. Only Sand0–3, SandstoneFloor12–15, SandstoneWall16–17 and RoadStone32–33 are revised. All 22 other frames, including dunes, salt, rocks, brine, depleted wet film and source bodies, remain exact in their cells, original palette values and 8,448 rendered RGBA pixels. The approved `beating-materials` source folder, PNG, editable blend and builder remain unchanged in place; no duplicate approved atlas packet was made.

## Reference and original parts

The author viewed current Dune/Ruin before captures, the generated reference and root's parts critique. Adopted: warm arid material identity, subdued mortar recesses and layered shading inside real solid wall cells. Rejected: the generated concept's raised walkable floor platforms, strong paving grid, noisy sand waves, changed sprites and geometry. No generated pixels were traced, sampled or downscaled.

Sand0 and floor12 are quiet bases. Minority sand/paving variants use broad low-contrast surface wear, with sparse shallow broken paving joins. Both alternating road frames use subtle packed-dust fields instead of bright grains or pits. The floor base remains exact `#806A4C`: the existing shallow-cave ambient backing uses this value, so its true-floor/ownerless transitions need no runtime change. The shared floor frames also change ordinary shallow SandstoneFloor presentation at depths1/2; a real cave counterview is part of root's acceptance scope.

The two wall frames use original wide masonry faces, lower planes, recessed mortar and short worn highlights. They remain fully opaque with the same straight one-cell footprint. No exposed-edge mask exists in this contract: shading represents internal masonry relief only, not an invented exterior rim, projected shadow, opening or protrusion. Actors, items, crops, movement, destruction and selection are untouched.

The source study uses the actual 75% quiet ground hash and alternating wall/road parity, arranged as a simple demonstration rather than a reconstructed game map. Self-review watch: wall course/highlight repetition and overly faint ground wear must be judged at actual graded normal zoom. The sheet does not establish fullness, gameplay quality or whole-area polish. Root/another reviewer owns independent art and actual judgments.

## Source and checks

`Tools/Live16x24/build_beating_environment.py` is a narrow adaptation of the established Stump source workflow. It reads approved editable cells, changes the twelve authorized frames, and exports original named integer unit-quads through the existing Blender model/camera/instance helpers. Pillow only reads rendered pixels for verification. No raster authoring or post-render editing is performed.

`source-red.json` retains the genuine missing-source verification failure before the first export; this is not a Unity/native-test claim. `source-verification.json` checks all 13,056 atlas pixels, the 22 unchanged frames, opacity of the twelve revised cells, quiet sand/floor bases, common flat-ground boundary colors and 1,033,450 direct opaque contact/study pixels. Corruptions of an untouched salt pixel, wall opacity, quiet sand and the shallow-floor base are rejected. Eighteen relevant approved source/helper/runtime-contract/reference/before inputs are pinned. No foreign `.meta` files are guarded. Root's original baseline receipt is retained; the replaceable runtime PNG/importer are deliberately not active guards.

Two fresh exports are compared for exact atlas, source-cell, palette, contact and joined-study bytes. Both saved blends are reopened and checked against their own hashes for 11,185 exact unit quads / 109 named mesh groups, palette/materials, integer coordinates and no animation. Binary `.blend` identity is not claimed. The first export and proof are kept once under `verification/rebuild-first`; the identity receipt and final frozen handoff bind the result.

## Reproduce

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_beating_environment.py -- --build
python3 Tools/Live16x24/build_beating_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/beating-environment/editable.blend --threads 4 --python Tools/Live16x24/build_beating_environment.py -- --inspect-blend
```

Do not repeat `--snapshot`: original pins cannot be replaced. Intended existing import remains centered16PPU, Point/Clamp, no mipmaps or compression. Root handles Assets, Unity, actual Dune/Ruin/road/shallow comparisons and publication. This art task has made no Assets/runtime/test, Unity or Git changes.
