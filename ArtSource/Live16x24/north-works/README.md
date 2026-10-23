# North works, forage and creature art

Status: original source art rendered and independently reviewed, 2026-10-23. This packet supplies nine static frames for existing north-route owners. Native runtime selection, owner/state invalidation and bounded ordinary Play acceptance are now complete in [the integration record](../../../Docs/LIVE-16X24-NORTH-WORKS.md): 755 selected Unity cases pass; actual harvest, batch brewing and matching examine portraits are captured. The packet does not complete the route or introduce new mechanics.

## Rebuild and inspect

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_north_works.py
python3 ArtSource/Live16x24/north-works/verify.py
```

- `north-works.blend`: named editable integer-coordinate mesh collections, flat emission materials and a separate contact scene. One world unit in this authoring scene renders to one native pixel.
- `north-works.png`: 144×24 atlas, nine 16×24 cells in the fixed order below.
- `north-works-contact.png`: 960×1184 direct8× geometry render. It is not a resized atlas.
- `source-cells.json`: exact named-part/color grid, top-left origin, generated from geometry instructions rather than sampled from a concept.
- `manifest.json`: exact frame rectangles, bounds, colors, uses, limits, references and current PNG hash.
- `verify.py` / `verification.json`: read-only raster/source/contact and prior-art preservation checks. Only the JSON verification receipt is written by the validator.

Integrated resource is `Sprites/Live16x24/north-works`. Import at16PPU with centered tile pivot, Point filtering, Clamp, no mipmaps and no compression. All cells fit a single16×24 frame, touch row22, leave row23 transparent and remain inside side margins. No character animation or directional sheet is included.

The frozen PNG SHA256 is `fb318bdbf5e49c4a0efd36639415e92fc27a548559fc5574de5deee58bdaeab4`.
Decoded RGBA SHA256 is `d58e3964a4d269bf017f75358ac7c49768c0acb786a8ff579f0b625ddaad9366`.
Cycles embeds variable render-timing text metadata in PNG files, so a rebuild may change the file checksum while reproducing every decoded pixel exactly. The manifest records the current rendered artifact; no raster metadata stripping or rewriting is performed.

| Index | Exact owner/state | Native distinction |
| --- | --- | --- |
| 0 | `AlchemyStill` | Warm receiving body, curved condenser pipe and small attached receiver. No flame, liquid level, bubbles or output item. |
| 1 | `AlchemyShelf` | Broad wooden posts, two genuinely empty compartments, visible worn board edges and feet. No painted bottles or infinite stock. |
| 2 | `StoneburrPatch` | Ochre hooked heads on unequal stems above dark leathery leaves, distinct from grain kernels. |
| 3 | `FrostLichenPatch` | Low wide cool crust, six pixels high. No tall flowers, pool, emitted frost or luminous haze. |
| 4 | `MendleafPlant` | Low broad pointed foliage with simple pale veins inside olive contours, twelve pixels high. |
| 5 | `Magpie` | Dark bird with cream breast, long low tail, short beak and separated feet. No permanent stolen item. |
| 6 | `Viper` | Low angular S-curve with open lower coil and raised head. No legs or baked poison effect. |
| 7 | `MarlbackPatchbearer`, healing slot present | Accepted Gleaner shale anatomy and stance with pale patched straps and a small healing bottle in its cradle. |
| 8 | Same actor, healing slot absent | Identical body, hands, straps and stance; only the eight bottle pixels differ. The cradle remains. |

## Source contracts and corrections

The measured owner inventory and concept critique are in `ArtSource/Live16x24/concepts/north-parts-plan.md`. This pass rechecked the eight exact blueprints in `Assets/Resources/Content/Blueprints/Objects.json`, `AlchemyStillPart`, `HarvestablePart` and `FieldMedicinePart` before authoring.

- The still is existing non-solid furniture with `AlchemyStillPart`. Brewing executes through real commands immediately; there is no persistent running/ready animation state. The small attached vessel is part of the apparatus, not an available tonic.
- The shelf is an existing solid wooden `ContainerPart` owner. Its current observed contents are empty. Neither the art nor its open compartments add sight-opacity, stock, a loot count or a new lid state.
- Stoneburr, frost lichen and mendleaf are non-solid, unspent gathering sources. Their respective real yields are `StoneburrSeed`, `FrostLichen` and `MendleafSprig`; successful harvest removes the source. No spent standing variant or fixed product count is painted. The lichen is not a prepared coating, and the mendleaf art does not invent a weather-close animation.
- Magpie is a passive hoarder with actual inventory. The silhouette has no permanently attached shiny object. Viper's bite, poisoning, corpse and venom-gland outcome remain simulation-owned.
- Patchbearer inherits Gleaner anatomy and carries independent `HealingTonic` and `Antidote` supplies. `FieldMedicinePart.FindCarriedMedicine()` is the existing pure local-inventory query for an actual positive carried healing tonic. Health/control eligibility is separate: a healthy actor still carries its bottle. An absent healing slot does **not** mean the antidote, all inventory or cutting equipment is gone.
- The generated concept's humanoid body and large held flask were rejected. This source imports the accepted cast generator read-only, obtains the original Gleaner geometry cells, and remaps its palette to restrained shale/skin/linen clusters. Pale patched straps and the small cradle are then authored on that body. No approved PNG or earlier generator is modified. Hands remain equipment-neutral.

Native routing must still validate exact blueprint, owned parts, current position, visibility and actual state. These source files do not grant visibility or permission and do not infer carried inventory from a world pile. The runtime must invalidate the real actor cell when the healing slot changes; the art introduces no extra count or scan.

## Greybox → concept → native review

The parent first captured ordinary traversal and interaction at the real connector and Nella route. Relevant evidence includes `Docs/Verification/Live16x24/north-greybox-works.png`, `north-vegetation-before-works.png` and `north-vegetation-native-entry.png`. The route's danger, masonry and real owner positions are preserved. This art pass changes no layout, spawn or combat value.

The saved scene direction is `ArtSource/Live16x24/concepts/north-works-concept.png`; extracted reference parts are `north-parts-reference.png`, with their exact prompts beside them. Both were viewed alongside actual captures and accepted base/cast art. Their silhouettes informed the work, but gradients, alpha halos, irregular illustrative pixels, fake shelf stock, tall lichen and humanoid patchbearer were rejected. No concept is sliced, sampled, downscaled or imported as production art.

Pass1 authored native integer quads with3–6 opaque colors each. The still's pipe and receiver, empty shelf, hooked burr heads, low lichen, long-tailed bird, open snake coil and state-specific healing slot passed source/contact checks and independent silhouette review. Patchbearer's two states differ only in an eight-pixel bottle area; all lower-body/hand cells are identical.

Pass2 addressed a concrete review issue: Mendleaf's narrow tall limbs looked like a forked stalk or cactus. A stricter low-height assertion first failed against the16-pixel first pass. The crown was lowered to12pixels and side leaves broadened around inset pale veins, with the same four colors and one-cell footprint. The final native/contact review accepted that correction. No additional detail was added to meet an iteration count.

## Verification and remaining gates

The read-only verifier was authored and run before production, failing because the source cells did not yet exist. After the first render it passed. The tightened Mendleaf shape check then supplied the second expected failure, followed by the final passing render.

The final receipt verifies:

- Exact144×24 dimensions, fixed nine-frame order, binary alpha,3–6 colors per frame and Point/Clamp/16PPU manifest contract.
- Zero source/native and contact/native mismatches, checking every8×8 contact block against the native cell.
- Nine distinct connected silhouettes, baseline22, clear row23 and side margins.
- Empty shelf interiors, still apparatus without flame/liquid names, hooked burr parts, low wide lichen, low veined mendleaf, long tail and legless low snake.
- Healing bottle present versus absent differs at exactly eight cells inside a4×5 region. Lower body, hands and repaired straps remain identical.
- Approved user sprites and base/cast bytes match HEAD; anchors, states, motion, items, cellar and north vegetation match accepted manifest hashes.

**Can verify here:** editable geometry, exact native pixels, alpha/palette/shape constraints, state-pair source consistency and preservation of prior assets. **Cannot verify here:** real Unity owner routing, stationary inventory invalidation, fog/remembered behavior, interaction/harvest outcomes, whole-route visual density, motion, input or performance. Those runtime/Play gates are separately documented in the integration record linked above; source verification alone does not establish them.

Static Magpie, Viper and Patchbearer identities are deliberate scope: directional walking, attack, hurt and self-use poses are still gaps. Nella's approved keeper, crop/reserve states, cords, products and remaining incidental surroundings are separate pending groups. This packet is not a full north-route or release-ready graphics claim.
