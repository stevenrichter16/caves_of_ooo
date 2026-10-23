# Opening cast — four native base poses

Status: authored and reviewed as native Blender renders; actual Resources import, state routing and live Game acceptance belong to the integrating agent. These are four distinct **static living poses**, not animation sheets or a complete character-animation release.

## Files and recreation

- `opening-cast.png`: transparent 64×24 atlas, four adjacent native 16×24 frames.
- `opening-cast-contact.png`: 1024×352 labeled view, rendered directly from the same geometry at exactly 8× native magnification.
- `opening-cast.blend`: editable front-facing mesh quads organized into named character/sub-part collections. Native and contact scenes are retained.
- `source-cells.json`: original authored pixel-cluster/part/palette data; top-left coordinates, Y downward.
- `manifest.json`: exact order, blueprint identities, bounds, palettes, pivot and source hash.
- `verification.json`: read-only dimension/pixel checks and preservation hashes.
- Generator: [build_opening_cast.py](../../../Tools/Live16x24/build_opening_cast.py).

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_opening_cast.py
```

The generator writes only this directory. It imports the accepted base generator's orthographic render/material helpers without calling its main function. Bytecode output is disabled; no earlier source or asset is changed. Blender 5.2.1 produces both PNGs directly from flat emission geometry, without raster input, tracing, sampling, cropping or downscaling.

## Exact frame contract

| Index | Blueprint | Occupied bounds, exclusive right/bottom | Colors | Identity |
| --- | --- | --- | --- | --- |
| 0 | `Villager` | `(3,3)-(13,23)`, 10×20 | 5 | Uncovered profile, sloped olive tunic, cream front cloth, lowered hands and separated boots. No keeper hat, player hood, warden cap or weapon. |
| 1 | `PetDog` | `(1,12)-(15,23)`, 14×11 | 5 | Low horizontal tawny body, short muzzle, one floppy ear, raised bent tail, cream chest and open belly/leg gaps. Small eye; no white mascot eyes or gadget collar. |
| 2 | `MarlbackScrabbler` | `(1,8)-(15,23)`, 14×15 | 6 | Squat broad shale-backed burrower, overlapping plate masses, short blunt face under the leading plate, hind foot and two asymmetric foreclaws. |
| 3 | `MarlbackGleaner` | `(2,3)-(15,23)`, 13×20 | 7 | Same plated species with a taller stepped ridge, leaning posture, restrained rust seams and one extended foreclaw. Not a scrabbler recolor. |

Every frame occupies one gameplay cell. All feet/claws finish at native row 22, with exclusive bottom baseline 23 and one empty row below, matching the accepted base group. Import at 16 PPU, centered tile pivot `(0.5,0.5)`, Point filtering, no mipmaps and no lossy compression. Rectangles are `(index*16,0,16,24)`.

Route by the exact real creature identities. A dog is never a generic humanoid; another creature must not silently receive a marlback or civilian frame. These are equipment-neutral base poses: loadout, actual attacks, status, death, removal and visibility remain gameplay-owned. Future actor animation needs deliberate derivatives, not duplicated copies pretending to be motion.

## Design and review

Viewed [cast-parts-v1.png](../concepts/cast-parts-v1.png) and its [prompt](../concepts/cast-parts-prompt.md), with the accepted [base contact sheet](../base/opening-base-contact.png) as the native style anchor. Kept four broad body ideas and rejected the concept's excessive detail and large toy-like eyes. These frames are original geometry reconstruction; no pixels were taken from the concept.

Two direct render/review passes:

1. Established the unhelmeted villager, low canine and two slate-plated creatures. Checked flat clusters, one-cell fit, equipment-neutral anatomy and common baseline.
2. Lowered and widened the scrabbler's shell and short face from the first taller version. It now reads as a squat 15-pixel body beside the gleaner's 20-pixel stepped posture. The dog retains its 14×11 silhouette and open underside; neither creature gains a large white eye or long snout.

An independent native/contact review found no material blocker for the static group. It confirmed the canine muzzle/tail/leg gaps, civilian head and shoulders, and distinct squat versus stepped marlback contours without relying solely on rust color. Preserve the face/limb gaps in future animation; motion readability remains a live-context check.

The accepted base atlas stays byte-identical at SHA256 `71ce34419f0e5b1a9a494dd4abd2b29efa8c5866dab97e2aaa57b1e2fb42df2e`. All five approved source PNGs also match repository HEAD exactly. No existing base frame, approved sprite, Asset import or Unity scene was modified while authoring this group.

## Verification and limits

- Exact 64×24 native dimensions and four nonempty 16×24 frames.
- Alpha values only 0/255, with 5/5/6/7 opaque colors.
- **0 mismatches** across all 1,536 native pixels compared to authored geometry/palette values.
- **0 mismatches** between each contact sprite's exact 8× blocks and its native pixel values over `#032F23`.
- Final PNG SHA256: `35a2de34d9bf6d6dfe66c7dd47439f7ac03ec6b7de9e09f026d6cb623e24d73a`.
- Direct native and contact renders inspected after the final pass. No smooth gradients, baked light, 3D extrusion, fabricated animation or carried equipment.

These are source-art checks. Actual visibility, NPC motion, combat spacing, removal, save/load, memory fog, shared-material batching and appearance in the live opening still require the integrating task's tests and Game capture. No Deck performance or final animation quality is claimed here.
