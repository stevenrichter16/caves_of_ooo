# Nella's crop stages, reserve tray and ground cords

Status: original native source rendered/source-verified and runtime integration tested,2026-10-23. The [parent record](../../../Docs/LIVE-16X24-NORTH-PLOT.md) supplies860 distinct passing Unity tests and an actual arrival capture. Ordinary rights/watering/growth/harvest/save-load visual acceptance remains pending after the computer locked; the whole north route is incomplete. Source verification below retains its narrower claims.

## Rebuild and inspect

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_north_plot.py
python3 ArtSource/Live16x24/north-plot/verify.py
```

`north-plot.blend` contains editable named integer-coordinate mesh parts and flat emission materials. One authoring world unit is one native pixel. The source generator reads no raster. `north-plot.png` is a directly rendered 128×24 atlas; `north-plot-contact.png` is a separate 1280×1152 render of the geometry at8×, including bare and occupied reserve-ground examples. No image is resized, sampled, sliced from a concept or repainted by Python.

`source-cells.json` records the exact original geometry cells. `manifest.json` records frame order, bounds, palette, semantics and current PNG checksum. The read-only `verify.py` inspects these files and decoded pixels; it writes only `verification.json`.

Imported resource: `Sprites/Live16x24/north-plot`. Use16PPU, centered tile pivot, Point filtering, Clamp, no mipmaps and no compression. Every new frame has binary alpha and a clear final row23. Foreground objects touch row22. Ground cord ends at row21 deliberately: it is a sparse separate ground decoration, not an upright sprite or a new entity.

## Fixed eight-frame contract

| Index | Exact owner/state | Readable shape and limits |
| --- | --- | --- |
| 0 | `CandyCarrotCrop`, `GrowthStage == 1` | Displayed as gladroot: a low branching spray of leaves. The edible root stays below soil; no orange carrot or mature fruit is painted. |
| 1 | `EmberwheatCrop`, stage1 | Unequal narrow green stalks and leaf blades without filled grain heads. Distinct from the already accepted ripe field-row owner. |
| 2 | `MarlrootCrop`, stage1 | Two low, broad, angular leaves and a dark crown, without ripe red clay. |
| 3 | `MarlrootCrop`, stage2 | A larger leaf crown with attached knuckled red-clay shoulders and simple root ridges. This is still the plant, not a loose clod or prepared FireClay. |
| 4 | `PitchpodCrop`, stage1 | A slim stem, opposed pointed leaves and a growing tip; no pods or resin yet. |
| 5 | `PitchpodCrop`, stage2 | Two hanging papery pods with pale folds, paired leaves and one small amber resin bead. No flowers, flame, glow or brewed effect. |
| 6 | `ConnectedReserveTray` | A shallow neutral wooden tray with two cord loops and empty open center. The frame never asserts real contents or permission. |
| 7 | Existing bound reserve ground | Unequal short twine runs, small bends and three tie knots around a transparent center. A separate ground-decoration layer preserves crop and actor foregrounds. |

All stage0 crops deliberately reuse the existing accepted planted mark; no additional seed-stage frame is hidden in this atlas. The approved `seed-keeper-3.png` is separate and untouched. Products, carried seeds and additional crops sold by the keeper are not supplied by this packet.

## Source sweep and state corrections

The source sweep checked the exact blueprints in `Assets/Resources/Content/Blueprints/Objects.json`, `Gameplay/Farming/CropPart.cs`, `CropSystem.cs`, `CropYieldService.cs`, `CropTime.cs`, `Gameplay/World/LocalGatheringClaimPart.cs` and `World/Generation/Builders/SpreadExplorationResidents.cs`.

- Gladroot and Emberwheat have only standing stages0/1 and `HarvestAtMaturity == false`; completion releases their products and removes the crop. Giving either a persistent ripe frame would invent a state. Their art therefore does not promise immediate manual harvest.
- Marlroot and Pitchpod have stages0/1/2 and `HarvestAtMaturity == true`; they remain at stage2 until actual harvest. These blueprints carry the `Crop` tag but do **not** carry `Vegetation`. A runtime selector must not reject them for that absence.
- Stage changes and watering already mark the real owner cell dirty. Harvest and rollback mark the source cell, and time reconciliation uses existing simulation ticks. This art requires no new timer, per-frame crop poll or elapsed-time interpretation of lore prose.
- The reserve tray is a non-solid, non-takeable Container owner. Its initial actual contents are empty. A fixed neutral frame avoids false stock or empty/full claims when those contents later change.
- Reserve cords belong to two exact ground references bound through the keeper's real `LocalGatheringClaimPart`, its current managed world/zone graph and `ConnectedSpread.ReserveBed` marker. They do not belong to a crop. They survive a real harvest, and public beds receive none. Ownership permissions may change without changing the physical twine. Art must not introduce collision, new marker entities, a fence, a permission color or a claim on an arbitrary copied marker.
- The four public planted beds are not automatically claimed or cultivated reserve ground. Cords must not spread to them. Exact runtime validation remains separate from this source packet.

## Greybox, concept and two visual passes

The packet reuses actual `north-greybox-plot.png` and `north-vegetation-native-plot.png` captures in `Docs/Verification/Live16x24`. It preserves real owner positions, paths and quiet negative space. The saved `north-plot-concept.png`, `north-parts-reference.png` and adjacent exact prompts in `ArtSource/Live16x24/concepts` were inspected as references. The generated keeper derivative, flower-like resin heads, thick reserve enclosure, alpha halos, gradients and excess illustrative detail were rejected. No new concept call was needed to repair artifacts that native geometry should remove.

Pass1 authored the eight native frames and four composite examples. The checker caught detached one-pixel leaf tips; those were connected before the first passing verification. Review accepted the crop species/stage differences and neutral tray but found the four identical cord corners too similar to interface targeting brackets.

Pass2 changed only the ground cord. Short unequal side runs, slight bends and small tie tails replaced the four symmetric L shapes. The source-level shape assertion first failed the old layout (`cord-pass2-red.txt`), then passed the new one. `cord-pass2.json` preserves all eight prior frame hashes; the verifier proves frames0–6 are unchanged. The final cord uses32 opaque pixels with quadrant counts12/5/7/8, three existing material colors and an entirely transparent center. It remains legible on exact dry/wet soil colors and beneath both mature reserve crops without implying a raised barrier.

## Verification and remaining gates

The verifier was written and run before production. `source-red.txt` records the expected missing-source failure. Final `verification.json` verifies exact128×24 dimensions, fixed ordering, binary alpha,3–6 colors per frame, valid margins/baselines, seven connected object silhouettes, sparse asymmetric ground twine, and zero native/source or8× contact mismatches. Every pixel of the four soil/crop composite examples is checked against the separately rendered geometry and exact source palette.

The approved keeper, cook, tatterjay, grazer, beetle jar, base and cast PNGs match HEAD. Anchors, states, motion, items, cellar, north vegetation and north works still match their accepted manifest hashes. The verifier is a read-only raster check; it does not alter those assets.

**Frozen native PNG SHA256:** `e14359280ce3d97fa645c3bbce0a5a316e0fd212a7025da363f66b01a42df641`.

**Decoded RGBA SHA256:** `83d1a381715fff0d7ec8e0dfa6a612ef3fadb95b2251422bdf2e4f8708e73b75`.

Cycles embeds variable render-time text in PNG files. Rebuilding can therefore change the whole-file checksum while producing identical decoded pixels; no raster metadata stripping is performed. Do not rebuild the accepted artifact merely to chase its file checksum.

This packet proves editable geometry, exact native pixels, reference-informed shape/state distinctions and preservation of earlier source art. It does not prove Unity routing, fog/remembered safety, claim rebinding, harvest or growth outcomes, actor/crop layering, inventory portraits, normal input, performance or whole-route readiness. Those remain separate runtime tests and real Play checks. Independent static review and the parent integration record supply their own bounded evidence.
