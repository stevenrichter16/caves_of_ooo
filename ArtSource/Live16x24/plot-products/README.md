# Plot products: native loose crop goods

Status: original native source rendered, source-verified and independently reviewed, 2026-10-23. This directory is source art only. Runtime tests, resource import and ordinary gameplay verification belong to the parent integration task; this source record does not claim those gates have passed.

## Fixed contract and verified meanings

| Index | Exact blueprint | Real source/use | Native art requirement |
| --- | --- | --- | --- |
| 0 | `CandyCarrot` (display gladroot) | The two-stage public crop auto-releases two; consume one for1d4 healing. | One detached orange tapered root, with only a small cut leaf stub. No planted crown, prepared meal or cooking-effect promise. |
| 1 | `CandyCarrotSeed` | Starter/keeper stock; carried seed plants `CandyCarrotCrop` on eligible ground. | One pale fingernail-sized seed, distinct from the planted mark and accepted drawgourd seed. |
| 2 | `EmberwheatSeed` | Starter/keeper stock; carried seed plants `EmberwheatCrop`. | One narrow golden kernel. Not the accepted harvested sheaf, glowing ember or a seed bag. |
| 3 | `MarlrootClod` | Mature reserve harvest yields two; Prepare consumes one and produces one `FireClay`. | Compact red clay caught by knuckled root strands. Must differ from the leafy growing crop and smooth prepared clay. |
| 4 | `MarlrootSeed` | One saved seed accompanies actual mature harvest; plants `MarlrootCrop` in cultivated soil. | One compact angular red-brown seed. No loose-clay pile or claimed bed marker. |
| 5 | `PitchpodResin` | Mature reserve harvest yields two; actual Reagent properties are `heat:1,combustible:1`. | One loose amber resin piece, without bottle, fire, glow, growing pods or ready coating. |
| 6 | `PitchpodSeed` | One saved seed accompanies actual mature harvest; plants `PitchpodCrop` in cultivated soil. | One distinct compact dry seed. No pod stem, flower, seed packet or invented fruit. |
| 7 | `InertSludge` | Keeper/provisioner stock and a finite brewing outcome; one carried unit composts a fresh crop once, reducing growth-stage time25% without supplying water. | Low neutral matte compost lump, not a bucket, flask, liquid pool or beneficial-effect icon. |

The source sweep checked these exact definitions and inheritance in `Assets/Resources/Content/Blueprints/Objects.json`, plus `FoodPart`, `SeedPart`, `BotanicalProcessingPart`, `CultivationPreparationService`, `BrewingService` and keeper stock. All eight inherit takeable `Item` and `Stacker`. Stack quantity remains in the simulation/UI; an illustration is not a count promise.

Important corrections before production:

- Gladroot's `Food.Cooking = Meal` is a flavor tag. Its blueprint has no `CookablePart`; this packet promises only its actual eat/heal use.
- The first two seeds allow ordinary Plantable ground underfoot; reserve seeds require `CultivatedSoilPart`. Adjacent planting uses prepared beds. A dropped seed must be picked up before planting, and one is consumed by the real action.
- `MarlrootClod` is unprepared material with `BotanicalProcessingPart`, not already usable FireClay. The already accepted FireClay and Emberwheat images are reused, not recreated here.
- `PitchpodResin` is raw reagent. Brewing and throwing a resulting coating are separate operations; the raw resin image must not suggest it is already a bottle or a burning projectile.
- `InertSludge` has no specialized compost/reagent part. The existing cultivation service accepts that exact carried item; brewing can also produce it as a finite fallback outcome. The same neutral material image serves either source. It supplies no water.

Loose items can cover one another at the same render layer until picked up. Do not combine a real seed and produce into a fictional pile sprite or leave an extra world copy when a transfer packs the item. Exact current owner, carried/ground authority, positive stack and semantic selection remain runtime responsibilities.

## Source workflow

The real plot geometry and routes were already accepted in `Docs/LIVE-16X24-NORTH-PLOT.md`. Reuse actual `north-plot-harvest.png`, `north-plot-bare-bed.png` and `north-plot-trade.png` rather than fabricate a demonstration pile. This packet keeps the approved native palette and compact item scale from `ArtSource/Live16x24/items/opening-items-contact.png`.

The read-only source verifier was written before production and failed because `source-cells.json` did not yet exist. `source-red.txt` preserves that expected failure. The actual saved reference is `ArtSource/Live16x24/concepts/plot-products-v1.png`, with its exact prompt in `plot-products-prompt.txt`. Both were read/viewed alongside the actual harvest capture and accepted native loose-item contact sheet.

Kept the reference's short detached gladroot, different seed shapes, root-bound clod, irregular resin and low compost. Rejected generated bevel shading, faint alpha artifacts, jewel-like resin highlights and scattered sludge chips. The original native shapes use integer mesh quads with flat emission, directly rendered at128×24 and independently at8× for the contact sheet. No raster is sampled, sliced, downscaled or edited by Python. Accepted source helpers are imported read-only; no earlier art or generator is changed.

The first native/contact render passed the source checker. Independent review accepted all eight shapes without requesting further geometry changes: pale diagonal gladroot seed, narrow golden wheat grain, squat red marlroot seed and tapered olive pitchpod seed differ in outline as well as color. The clod's attached pale root strands differ from already accepted FireClay; resin reads as solid amber without flame/glow/vessel hardware; sludge remains a coherent low matte mass. No extra detail or redundant render pass was added solely to increase an iteration count.

## Rebuild, outputs and frozen artifact

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_plot_products.py
python3 ArtSource/Live16x24/plot-products/verify.py
```

Outputs are `plot-products.png` (128×24), `plot-products-contact.png` (1280×800 direct8× geometry render), editable `plot-products.blend`, `source-cells.json`, `manifest.json` and `verification.json`. The `.blend` retains original named part meshes, flat palette materials and separate native/contact scenes. One authoring unit is one native pixel. Planned resource: `Sprites/Live16x24/plot-products`.

Native cells use16PPU, centered pivot, Point/Clamp, binary alpha, no compression/mipmaps, clear row23 and occupied baseline row22. All goods fit within one cell; no frames imply extra inventory quantity.

**Frozen native PNG SHA256:** `453c1eef4fa91f7d607c037a7398f2c6b376ca653e692e4f7bd8782cce3cf7da`.

**Decoded RGBA SHA256:** `537f9e1336700ad314921bd436c2f49c22d6f53e524a2139c1da1051600d4723`.

Cycles embeds variable render-time text in PNG files. Rebuilding can therefore change the whole-file checksum while reproducing identical decoded pixels; no raster metadata stripping is performed. Preserve the accepted artifact rather than repeatedly rebuilding solely to match its file checksum.

## Verification and limits

Final `verification.json` passes fixed eight-frame order,128×24 dimensions, binary alpha,3–5 opaque colors per frame, compact connected item silhouettes, four distinct translation-normalized seed outlines, root strands on unprepared clod, no fake container/effect on resin or compost and zero source/native/contact mismatches. Every8×8 contact block matches the separately rendered source geometry.

The four seeds occupy13/12/17/15 pixels, compared with49 for gladroot,53 for clod,37 for resin and38 for compost. These counts describe compact silhouette scale, not inventory quantities. The prior keeper, cook, tatterjay, grazer, beetle jar, base and cast PNGs match HEAD; anchors, states, motion, items, cellar, vegetation, works and plot PNGs match their accepted manifests. The verifier only reads these raster assets and writes its own JSON receipt.

This source verification cannot establish runtime owner routing, visibility, drop/pickup, planting, harvest, preparation, compost behavior, input or performance. Those require separate native code tests and ordinary Play acceptance.
