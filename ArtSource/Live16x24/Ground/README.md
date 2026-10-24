# Opening ground — native source packet

Status, 2026-10-24 UTC: original native source authored and independently
pixel-verified. The approved PNG and full handwritten meta have been copied to
Resources, imported and routed through ordinary flat presentation. Bounded
pond/road/field Play acceptance is in Docs/GRAPHICS-G2-GROUND.md and the adjacent
visual-review.md; wet-soil ordinary acceptance remains open. This is
one ground-material pass for the existing opening, not a new gameplay system or
an all-region art claim. The plain green ground and approved foreground/wall
sprites are unchanged.

## Evidence and concept critique

The actual input is [G2 normal start](../../../Docs/Verification/Live16x24/g2-normal-start.png),
seed64, normal launch at the real opening. Root generated
[the paintover](../concepts/g2-ground-paintover-v1.png) and
[the parts reference](../concepts/g2-ground-parts-v1.png) against that capture;
their exact prompts are retained beside the concepts. Useful ideas are shallow
inset banks, sparse flat road wear and restrained turned earth. Generated bloom,
gradients, oversized/irregular pixels, raised-looking stone chips and a bright
shoreline were rejected. No generated pixels were sampled, traced or resized.

The source audit established these distinctions before authoring:

- The pond is live tile-state water over existing ground, not a LiquidPool
  owner. It is passable and reactive. Teal never promises drinkable water,
  a refill source or a particular depth. The separate basin retains finite
  supply and its existing foreground art.
- A water mask means **disclosed dry neighbors** (`N1 E2 S4 W8`), not simply
  absent water. Hidden, invalid and other-liquid neighbors cannot grant a
  dry-bank bit. The native bank is inset entirely inside its own wet cell.
- RoadStone is a current nonsolid ground owner. Sparse embedded wear implies
  neither a raised obstacle nor a repairable/harvestable stone object.
- Fixed CropRow/RipeCropRow soil is not automatically prepared soil or a
  growing CropPart. Old field earth therefore differs from ordered furrows;
  existing stubble/grain continues to carry yield identity.
- Prepared wet soil follows actual current crop moisture. Removing that crop
  does not leave a stored wetness value on the empty bed. Dry/wet art shares
  exactly the same furrow geometry; only the palette darkens. Ground never
  paints seeds, droplets, tools, stock or permissions.

Runtime must preserve foreground actors/items, hazard marks and the independent
reserve-cord overlay. The source packet supplies opaque background pixels only;
it does not make runtime disclosure or owner decisions.

## Exact atlas contract

`opening-ground.png` is **336×24**, containing21 opaque16×24 frames in one row.
Every frame fills all384 pixels. Unity rect is `[index*16, 0, 16, 24]`,16PPU,
center pivot, Point/Clamp, no mipmaps or compression. The colored background
receives white draw tint, never multiplication by the old semantic swatch color.
The adapter deliberately declines remembered cells.

| Index | Native frame | Visual meaning |
| --- | --- | --- |
|0–15|Water, cardinal dry-edge mask|Calm teal; shallow muted silt entirely inside each disclosed edge. Mask0 is deliberately a single quiet color to avoid tiled glints.|
|16–17|Road A/B|Same packed muted-green surface, sparse embedded wear; no outlined blocks or height.|
|18|Old field earth|Quiet irregular soil below fixed stubble/grain.|
|19|Prepared dry furrows|Sparse broken ordered furrows, no seed or growth icons.|
|20|Prepared wet furrows|Identical geometry to19 with every RGB channel darker.|

The palette retains the actual prior water `#0C4344`, road `#31412D`, dry earth
`#45341F`, wet earth `#252319` and plain-green backdrop `#032F23` anchors.
Each frame uses three flat colors except mask0's single color. There is no
painted light, haze, cast shadow, reflective sparkle or animation claim.

## Files and reproduction

- `build_ground.py`: original editable integer-quad authoring, emission materials
  and direct native/contact/context renders. It writes only this directory.
- `opening-ground.blend`: editable native, contact and continuity scenes; named
  ground, bank, shallow-water, rut, earth and furrow mesh parts.
- `source-cells.json`: exact top-left-origin authored cells and palette.
- `manifest.json`: indices, semantic limits, Unity rects and direct-render
  placements for independent verification.
- `opening-ground-contact.png`: all21 frames rendered from the same geometry
  at6× integer camera scale, with labels outside the frame bounds.
- `opening-ground-context.png`: repeated roads/soil and the actual source pond
  predicate, rendered at4×. Surroundings are treated as disclosed dry only for
  this study. Basin gap stays empty. **Not a game screenshot:** actors, basin,
  fog, hazards, lighting and live mutation are omitted.
- `verify_ground.py`: read-only PNG/source checks; it never modifies an image.
- `source-red.json`: genuine missing-source failure recorded before authoring.
- `source-green.json`: exact native/contact/context verification after export.
- `build.log`: short Blender export receipt.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python ArtSource/Live16x24/Ground/build_ground.py
python3 ArtSource/Live16x24/Ground/verify_ground.py --receipt ArtSource/Live16x24/Ground/source-green.json
```

The generator imports only existing geometry/material/camera helpers from
`Tools/Live16x24/build_opening_base.py`; it never reads an image. It does not
write Resources or invoke Unity. Asset copying/import is a separate controlled
step after runtime test RED.

A second Blender export reproduced the atlas byte-for-byte. Source and
`Assets/Resources/Sprites/Live16x24/opening-ground.png` are byte-identical. The
new importer metadata explicitly uses TextureShape1 (2D), Point/Clamp, no mipmaps
and uncompressed default settings. The native import/resource contract and current routing tests pass.

Current atlas SHA256:
`b35f9d6a4b7051eb6971a00195a069f58d08a7d89e3060533745ee899a66dede`.

## Verification and acceptance limits

Source checks pass: exact336×24,21 unique frames, every alpha255,384 occupied
pixels/frame, exact palette matches,16 bounded dry-edge masks, unchanged quiet
water interiors, no extra edges outside the authorized bands, identical dry/wet
furrow geometry and strictly darker wet pixels. Native and direct integer-scale
contact/context comparisons have zero pixel mismatches.

The author inspected both native and enlarged source views. The pond study
stays continuous without repeated glints; muted banks remain far quieter than
masonry. Road wear stays flat; old earth and prepared furrows are distinct,
and wet furrows darken without adding growth marks. Root independently reviewed the contact/context and accepted the quiet source
direction without a named art defect before live verification. Further
independent review and actual runtime/Play outcome are recorded when available.

**Can verify here:** authored pixel geometry, palette, full opacity, mask-local
bounds, state-pair geometry and reproducible source layout. **Cannot verify
here:** current-owner/graph/fog routing, actual wet-state changes, hazard/cord
coexistence, world tint, player readability over the texture, frame cost or
physical Steam Deck experience. Root owns native runtime tests and the matched
ordinary Play/capture gate; this packet alone does not finish G2 or the release.
