# Incidental actors: Farmer and approved Reedback Grazer

2026-10-24. One new **static living Farmer frame** and the exact approved Reedback Grazer image prepared for the live native16×24 renderer. This is source-art evidence; runtime identity, occupancy, fog, examination and ordinary-game acceptance belong to the integrating task. Neither actor gains animation from this packet.

## Actual need and source constraints

The actual seed65 court showed Farmer7139 as a pale glyph over a cot; a current solid roaming NPC may legitimately occupy that non-solid bed. The new body must belong to that real Farmer, without moving the actor, concealing it or inventing sleep. The exact `Farmer` blueprint inherits Villager and supplies civilian conversation/trade and boots/gloves loadout. It does not guarantee a hoe, crop bundle, held item, sleep posture or service readiness. The new pose contains no such cue. The exposed profile and short work clothes are body presentation, not an equipment overlay or a promise about current inventory.

The actual seed64 east record separately showed a Reedback Grazer using `g`. The approved `Assets/Art/ReedbackGrazer16x24/03-ridge.png` already supplies the intended low body. Its image bytes are copied unchanged; its old importer, which includes a cropped rectangle, is not copied.

## Concept selection and critique

Root generated [farmer-concept.png](farmer-concept.png) from the actual court capture and accepted cast reference, with [the exact prompt](imagegen-prompt.txt). The first front/right concept supplies the useful design: receding exposed hair, short broad moss waistcoat around an ochre shirt, rolled cuffs, a broad belt and separated dark trouser legs/boots.

The generated contextual inset incorrectly replaces the approved Cook while leaving the original Farmer glyph. **Reject that inset as placement or implementation evidence.** No Cook, Keeper, Villager, Player, Warden or other owner is replaced. The extra side/back concept views are references, not requested or delivered animation. The concept also contains more pixel density and shading than the native frame can support. Those details were deliberately reduced to seven flat colors and a 10×20 occupied silhouette inside16×24. No concept raster was sampled, traced, cropped, resized or composited into the output.

Compared with the approved cast, this Farmer has a short broad shirt/waistcoat and exposed receding head. It avoids the Villager's long cream front cloth, Cook's pale cap/apron and rust body, Keeper's broad yellow hat, Player's gold hood and Warden's green cap. Lowered arms and a clear leg gap establish a neutral standing civilian; there is no weapon or harvest prop.

## Native parts and palette

Top-left native coordinates, with Y increasing downward. Occupied bounds are `(3,3)–(13,23)` exclusive on the right/bottom; feet finish on row22 and row23 is empty. Each occupied pixel is an editable integer-cell Blender emission quad grouped into the parts below.

| Part group | Native cue | Palette |
| --- | --- | --- |
| Receding/side hair, exposed crown, face, ear, eye, neck | Uncovered right-facing head, no headgear | Leather `#5F4830`, skin `#DFA06A`, cream `#E4D0A0`, ink `#342E25` |
| Shoulders, left/right waistcoat | Broad short moss body, no cape or apron stripe | Moss `#75834B`, forest `#53683E` |
| Ochre shirt and sleeves | Warm central work shirt, quieter than Player gold | Ochre `#B48B54` |
| Collar and rolled cuffs | Small pale breaks; no long cream vertical strip | Cream `#E4D0A0` |
| Lowered forearms | Relaxed empty arms | Skin `#DFA06A` |
| Belt, separate trouser legs, short boots/toes | Waist break and persistent open leg gap | Leather `#5F4830`, ink `#342E25` |

The Farmer uses exactly142 opaque pixels and7 opaque colors; alpha is only0/255. Native and direct8× contact renders were visually inspected. The contact confirms the collar/cuffs remain small, the moss sides surround an ochre shirt, and the dark lower body retains separated feet. Actual readability over the occupied cot still requires the ordinary-game capture.

## Files and recreation

- `incidental-actors.blend`: editable Farmer pixel-part collections, native and direct8× contact scenes.
- `source-cells.json`: original part/material assignment for every Farmer cell; null cells are transparent.
- `farmer.png`:16×24 direct geometry render.
- `farmer-contact.png`:512×320 direct8× geometry render; labels are outside the sprite frame. It is not a game screenshot.
- `manifest.json`: independent exact resource identities, full-frame rectangles, dimensions, hashes and import contract.
- Generator: `Tools/Live16x24/build_incidental_actors.py`.
- Resources: `Assets/Resources/Sprites/Live16x24/farmer.png` and `Assets/Resources/Sprites/Live16x24/reedback-grazer.png`, with fresh independently generated `.meta` files.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_incidental_actors.py
python3 ArtSource/Live16x24/incidental-actors/verify.py
```

The generator writes only this source directory and those two owned resource PNGs/metas. It imports render helpers without calling their generators and preserves Unity-normalized metadata on rebuild. Blender5.2.1LTS renders flat emission geometry directly at native resolution; no raster editing library creates the Farmer. Verification uses Pillow only for read-only image checks. The Grazer is an immutable approved PNG copy, not newly generated Blender art, and is intentionally absent from the Farmer's editable geometry.

## Runtime binding contract

| Exact blueprint | Resource path | Source rectangle | Pixel treatment |
| --- | --- | --- | --- |
| `Farmer` | `Sprites/Live16x24/farmer` | `(0,0,16,24)` | New static body; do not alias another civilian or derived identity |
| `ReedbackGrazer` | `Sprites/Live16x24/reedback-grazer` | `(0,0,16,24)` | Exact approved bytes; preserve its low `(0,9)–(16,21)` occupied bounds |

Both resources use16PPU, centered tile pivot `(0.5,0.5)`, Point filtering, Clamp wrapping, no mipmaps, no compression and no NPOT resize. Runtime must create the full16×24 tile sprite; do not reuse the old Grazer importer rectangle/pivot, stretch the low animal upright, pad/crop the image or condition neutral body art on mutable grazer feeding or farmer stock. World/reader authorization and visible current-owner selection remain runtime-owned.

## Verification and bounds

`source-red.txt` records the expected missing-source failure before authoring. `source-green.txt` and `verification.json` record exact native dimensions, hard alpha, seven-color Farmer palette, source/native/direct8× pixel agreement, separated feet/baseline, independent resource metadata, exact approved Grazer hash and preservation of23 pre-existing art PNGs from `preserved-art-hashes.json`.

- Farmer SHA256: `4981b3cb861d30a3c8db4b426237f2a751dd48eca584034dac43d78acb63bfc0`.
- Grazer SHA256: `29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad`.
- Zero source/native and source/direct8× pixel mismatches.

This receipt does not prove Unity import, correct actor-over-bed selection, native full/incremental redraw, movement, inventory/custody changes, reader lifetime, fog memory, save/load, packaged performance or controller behavior. Root's focused tests and actual captures close those gates. No complete incidental bestiary or release-quality claim follows from these two bindings.
