# Live opening anchors — native source

Status: twelve native static owner/state frames authored and visually reviewed;
Unity import, state-routing tests and actual gameplay acceptance are separate.

## Production record

- Greybox/layout authority: the actual opening at `ReferenceGladePlan.ZoneID`,
  captured in `Docs/Verification/Live16x24/native-base-start.png` after the
  rectangular projection and fullscreen lifecycle checks. The root agent owns
  that Play receipt; this asset task did not run or alter Unity.
- Concept: `../concepts/anchor-parts-v1.png`; exact prompt in
  `../concepts/anchor-parts-prompt.md`. It was generated from the actual game
  capture and accepted `../base/opening-base-contact.png`. This concept is a
  visual guide, not the imported atlas. Its bevels and noisy shading were
  deliberately reduced to flat three-to-seven-color clusters.
- Native authoring: `Tools/Live16x24/build_opening_anchors.py` creates original
  integer XY mesh geometry in Blender. No raster reference is read, sampled,
  traced, cropped or downscaled by the authoring script. One authored grid unit
  becomes one native square pixel. Its implementation follows the existing
  base group's emission-material and direct-render setup.
- Editable outputs: `opening-anchors.blend` (named frame collections and named
  shape parts), `source-cells.json` (palette and exact cells), and the generator.
- Production raster: `opening-anchors.png`, **192×24 RGBA**, twelve 16×24 frames
  left-to-right, 16 PPU, centered tile pivot, Point filter, no mipmaps or lossy
  compression on import. This file has no baked dark-green ground.
- Review image: `opening-anchors-contact.png`, direct Blender render of the same
  geometry at 8×, with labels and a separate dark-green background. It is not
  gameplay and is not an image-resized version of the atlas.
- All subjects touch image row 22, matching the base/cast foot baseline. The
  lit-wall strip alone also fills row 23 and row 0 so stacked wall cells have no
  vertical alpha break. One-cell footprint throughout.
- Static frames only. The two repair/supply pairs are state variants, not
  animation. All previously approved PNGs, base/cast groups, scenes and source
  generators remain unchanged.

## Exact frame order and routing contract

| Index | Frame | Real owner / authoritative state |
| --- | --- | --- |
| 0 | cracked-well | `RepairLinedWell`, owned nonportable `RepairablePart.Repaired == false`, recipe `clay-well-lining`, owned `WellPart` |
| 1 | sealed-well | Same owner with `Repaired == true`; sound lining, not an unconditional availability icon |
| 2 | filled-basin | `SpreadDrawPoint`, owned `LiquidPoolPart`, `LiquidId == "water"`, `Volume > 0` |
| 3 | empty-basin | Same water source, volume depleted; ground coating stays independently authoritative |
| 4 | stairs | `StairsDown` with owned `StairsDownPart`; actual cellar travel uses its registered connection |
| 5 | fallen-beam | `FallenBeam`, native handling/physics; solid and haulable, not carryable or throwable |
| 6 | chest | Exact `Chest`; `ContainerPart` does not have a persistent open-lid state |
| 7 | barrel | Exact `WoodenBarrel`; no depicted contents or open/broken status |
| 8 | sign | Exact `Signpost`; a neutral board, no arrow or invented writing |
| 9 | mushrooms | `MushroomRing`, owned unspent `HarvestablePart` yielding `Mushroom` |
| 10 | quartz-vein | `GlowQuartzVein` with `reference-glade-lit-wall`, owned unspent harvest yielding `GlowQuartz` |
| 11 | lit-wall | Exact `Wall` with `reference-glade-lit-wall`; not a mineral harvest source |

**Blueprint identity precedes VisualID.** Native generation deliberately gives
both the ordinary Wall and GlowQuartzVein the same lit-wall VisualID, a real
green `LightSourcePart` (radius 2, intensity .35) and quarter-turn metadata. This
batch uses neutral flat contours; it does not rotate 16×24 rasters by 90° or bake
light haze. Future connection variants can honor actual adjacent owners.

Countercases: missing/foreign state parts fall back rather than inventing
repair or water availability; ordinary and rope-fault wells do not become
clay-lined wells; non-water pools do not advertise drinking; an empty basin
does not erase wet ground. Harvest removes mushroom/quartz sources, with
products getting their own art routes. Chest empty/locked state is not a lid
animation. Burnt or destroyed owners yield their actual replacements, and
moving the beam moves all of its art. No art should expose hidden live state.

Source audit covered `ReferenceGladeBuilder`, `GleanersDistrict`, Objects.json,
`RepairablePart`, `WellPart`, `WellDrinkingService`, `LiquidPoolPart`,
`HarvestablePart`, `ContainerPart`, `StairsDownPart`, `DragSystem` and
`LightSourcePart`. Wicket, campfire and Drawgourd remain a separate art group.

## Visual passes and verification

1. First native/contact inspection: accepted well/basin state silhouettes,
   restrained palette, and quartz versus continuous-wall distinction. Root
   independently inspected the contact and accepted this direction.
2. Revised the stairs from an 11-pixel-high rectangle to a 9-pixel-high ground
   opening with widening tread cuts; revised barrel to a 14-pixel-high tapered
   outline with a wider middle, distinct from the rectangular closed chest.
   Re-rendered native/contact and inspected both scales. State pairs retained
   identical alpha silhouettes; the repaired seam and water interior are the
   only state changes. No further static-art blocker was found.

Run `python3 ArtSource/Live16x24/anchors/verify.py` to read both PNGs and compare
every native pixel to authored cells and every contact pixel to its exact 8×
geometry block. It also checks binary alpha, palette, dimensions, state-pair
masks/differences, baseline, continuous wall edges and manifest hash. The
script never edits an image; it writes only `verification.json` after passing.

**Can verify here:** original editable source, exact pixels/palette/alpha,
native-size silhouette review and deterministic state-pair differences.
**Cannot verify here:** Unity import settings, correct runtime owner routing,
fog behavior, actual repair/drawing/harvest/removal presentation or hardware
performance. Root/integration agent owns native tests and Play capture next.
