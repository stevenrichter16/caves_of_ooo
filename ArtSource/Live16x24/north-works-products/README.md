# North works products: seven native portable goods

Status: original native source rendered, source-verified and independently
reviewed, 2026-10-23. Root and independent static review accept the first
native pass. Runtime integration and native Unity tests have since passed in the
[parent record](../../../Docs/LIVE-16X24-WORKS-PRODUCTS.md); ordinary
gameplay is still a separate pending gate. The Mac is locked during
this source packet, so no new live acceptance is claimed.

## Verified identities and limits

| Index | Exact blueprint | Existing source/use | Native treatment |
| --- | --- | --- | --- |
| 0 | `StoneburrSeed` | Finite StoneburrPatch harvest; Reagent `binding:2`. | Small dry hooked shell, no stem, planted mark or packet. It has **no SeedPart** and is not a plantable seed. |
| 1 | `FrostLichen` | Finite FrostLichenPatch harvest; Reagent `cold:2`. | Small low sage crust with blunt frond edges, clearly smaller than its living source. No ice crystal or glow. |
| 2 | `MendleafSprig` | Finite MendleafPlant harvest; Reagent `vital:1`; two sprigs can be prepared near a still. | Detached two-leaf sprig and short cut stem, distinct from the rooted three-leaf plant. |
| 3 | `WildBerries` | BerryBush harvest yields 1–3 units; raw Food heals 1d3. | Compact rusty-red fruit lobes and a short stem. The illustration does not encode a stack count. |
| 4 | `GoldCoin` | Existing currency, including real hollow-stump output. | One small worn gold disk with a single square dark inset. No cross, amount, stack or money bag. |
| 5 | `BrewedTonic` | Shared output blueprint for varied brewing results. | Opaque broad-based grey-green stoppered flask. No visible fill, label, healing band, effect icon or glow. |
| 6 | `ConcentratedMendleaf` | Prepare two MendleafSprig units near a still into one Reagent `vital:2`. | Compact pressed leaf wad with a folded seam and pale cut edge; still reagent, not a tonic or bandage. |

The source sweep read exact blueprints and inheritance in
`Assets/Resources/Content/Blueprints/Objects.json`, `Alchemy/ReagentPart`,
`Alchemy/BrewingService.ConfigureBrewItem`, `Alchemy/BrewItemPart`,
`Items/TonicPart` and `Preparation/PreparationRecipePart`.

Corrections that affect visual meaning:

- `WildBerries.Food.Cooking = Meal` is flavor, not a Cookable part. This
  packet does not invent prepared food or a cooking action.
- Concentrated mendleaf is a stronger ingredient, not the resulting drink.
  Its vital:2 can produce one 2d4 mending tonic instead of two separate 1d4
  sprig brews. No new recipe or concentration mechanic is added.
- Brewing writes healing into `TonicPart.Healing`; a healing-only result
  legitimately has no `BrewItemPart`. Status effects are represented by
  owned `BrewItemPart.EffectsRaw` and its form. Render display names vary
  with the actual result and are not asset identity.
- The neutral flask binds supported tonic/coating/throwable
  outcomes. Explicit `BrewItemPart.Form = Food`, foreign or unknown forms
  retain runtime fallback. A pre-existing healing-only Food result loses
  its form upstream because no BrewItemPart is created; this packet records
  that limitation and does not change brewing or claim to solve it.
- Tonic use spends one carried unit and does not create an empty container.
  The illustration therefore makes no reusable-vessel or refill-state
  promise. A bottle silhouette cannot establish safe drinking or healing.
- All seven are loose item identities. Stack quantity, current ownership,
  current cell, inventory custody, payload semantics, explicit reskins and
  fog remain runtime responsibilities. No decorative extra outputs are
  placed next to a plant or still.

## References and production

The functional worksite and ordinary harvest/brewing were already captured
at `Docs/Verification/Live16x24/north-works-stoneburr-harvest.png` and
`north-works-batch-brew.png`. These show real finite harvest and batch output,
not a staged pile. Read alongside accepted
`items/opening-items-contact.png`, `north-works/north-works-contact.png` and
`plot-products/plot-products-contact.png` under this source tree.

Imagegen reference: `ArtSource/Live16x24/concepts/works-products-v1.png`;
exact prompt: `works-products-prompt.txt` beside it. Keep its isolated
functional shapes, but reject generated gradients, colored fringes,
gloss/bloom and excessive detail. Further source corrections flatten the
lichen to a low crust, shorten the sprig, replace the coin's cross-like
mark with a single dark square, quiet the flask and keep the concentrate
matte. No generated pixels are sampled, traced, sliced or downscaled.

`verify.py` was written and run before authoring. `source-red.txt` records
the expected failure for all five missing production outputs. Original
integer cells were then authored as named flat-emission Blender geometry.
Native and 8× contact images are separate direct renders of that geometry.
Python's image reader only verifies pixels; it does not produce or edit
images. Existing mesh, material and camera helpers are imported read-only.

The first source pass has seven distinct connected silhouettes, binary
alpha, 3–4 colors per frame and no source/native/contact mismatches. Root
and independent review accept the quiet scale and identities. The reviewer
confirmed that the hooked shell differs from planting seeds, low lichen
from pointed quartz, open sprig from compact concentrate, and neutral grey
flask from the pale rust-banded HealingTonic. Subtle lichen/concentrate
contrast still needs actual ground review. No extra detail or redundant
render is added solely to increase an iteration count. The existing native
items remain the scale reference: the sprig is 8×11 occupied pixels, loose
lichen 7×5, burr 7×7, coin 6×6, flask 7×13 and concentrate 7×6. These are
image bounds, not inventory amounts or physical simulation dimensions.

## Rebuild and frozen output

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_north_works_products.py
python3 ArtSource/Live16x24/north-works-products/verify.py
```

Outputs: `north-works-products.png` (112×24),
`north-works-products-contact.png` (1280×800 direct 8× geometry), editable
`north-works-products.blend`, `source-cells.json`, `manifest.json` and
`verification.json`. Each native cell is 16×24; manifest requests 16 PPU,
centered pivot, Point/Clamp, no mipmaps and no compression. All occupied
bottom rows are 22, row 23 remains transparent and each image stays within
its cell. Imported resource: `Sprites/Live16x24/north-works-products`.

**Native PNG SHA256:**
`9a4e06fdbb55242a66101df145e83e034125f5fd9ce8b2f02f4907fe055250a9`.

**Decoded RGBA SHA256:**
`1c32642294baa35b97b4e0b936f3d03aa11cb1de9b6a7fca0f3c35a18d89b296`.

Cycles includes render-time text metadata. A rebuild can alter the whole
PNG file hash while reproducing exact decoded pixels; this pipeline does
not strip or rewrite raster metadata. Preserve the accepted artifact.

## Evidence and remaining gates

`verification.json` confirms exact seven-frame order, 112×24 native size,
binary alpha, 3–4 colors, seven translation-normalized distinct outlines,
connected compact goods, clear margins and zero source/contact mismatches.
It also checks the accepted manifest hashes of anchors, states, motion,
items, cellar, north vegetation, north works, north plot and plot products.
No prior art or runtime file is modified by this generator.

The source checks and static review can establish pixels, geometry,
silhouette separation and reference fidelity. They cannot establish Unity
import, semantic routing, real harvest/pickup/drop/examine, brew payload
behavior, actual world contrast, controller input or frame time. Those
remain parent-owned runtime and ordinary-Play acceptance gates. Static
inert goods do not receive invented motion or readiness states.

The parent integration now records exact-byte import,58 focused plus338
disjoint native regression passes. These results do not close ordinary
Play: source/harvest/brew images used for design predate this new item art.
