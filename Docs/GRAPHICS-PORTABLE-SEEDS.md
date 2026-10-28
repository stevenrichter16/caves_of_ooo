# Portable gladroot and emberwheat seeds

**Status — 2026-10-28: planned only.** Root is completing the preceding package delivery. This preflight changes only this document: no runtime, tests, assets, imports, Unity state or saved gameplay. Actual seed baseline captures and native RED must precede implementation.

## Visible goal and scope

Carry the existing exact CandyCarrotSeed and EmberwheatSeed bodies beyond the opening into ordinary world drops and authorized inventory/examine views. Both are already legitimately carried; this is a small common-supplies gap, not a search for undisclosed content. Preserve their existing art, planting behavior and the rest of the plot-products atlas. Other seeds, planted crop coverage, full growing cycles and whole-biome polish are not accepted by this change. Buckler still needs original art and remains outside this scope.

| Exact identity | Current user-facing name and function | Existing approved frame |
| --- | --- | --- |
| CandyCarrotSeed2697 | **gladroot seed**; Item, takeable/non-solid, weight0, layer5, glyph`,`/`&O`. Owned Seed.CropBlueprint=`CandyCarrotCrop`, RequireCultivatedSoil=false. Commerce3 and Examinable exist; it has no special Seed tag or explicit inventory category. | plot-products frame1, rect(16,0,16,24): one pale oblique fingernail-sized seed. |
| EmberwheatSeed2698 | **emberwheat seed**; same physical item contract, glyph`,`/`&W`, owned Seed.CropBlueprint=`EmberwheatCrop`, RequireCultivatedSoil=false, Commerce5 and Examinable. No special Seed tag/category. | plot-products frame2, rect(32,0,16,24): one narrow golden kernel. |

The two cells depict seed identity, not stack count, soil, moisture, a growing plant, bag, glow or guaranteed harvest. Their small scale is intentional. The approved contact sheet was inspected; preserve the distinct pale diagonal seed and warm upright kernel rather than enlarging them into props.

## Source sweep and corrections

- `FlatPixelOpeningArtCatalog.PlotProductFrameOf` already selects frames1/2 through `PlotSeed(owner, exactCrop, false)`. The resource is `Sprites/Live16x24/plot-products`,128×24 with eight native16×24 cells. Modern Greybox presentation still gates these opening products on local admission; legacy subject routing is more permissive. Do not broaden the whole eight-product atlas or loosen regional admission.
- `SeedPart` grants Plant only for a genuinely carried seed. Underfoot planting needs real Plantable terrain, no existing crop, no Barren veto, valid current owner/custody and the real factory. These two legacy seeds **do not require cultivated soil underfoot**. The separate adjacent action requires a prepared bed even for these seeds. An inspected/dropped seed cannot plant or be consumed by rendering.
- Actual planting consumes one carried unit and creates the exact crop. CandyCarrotCrop is the two-stage gladroot crop, authored20 ticks/stage; EmberwheatCrop is authored35 ticks/stage. Those numbers do not establish an actual growth duration under water/environment rules. No growth-timing or mature-harvest claim belongs in this portability slice.
- `FlatPixelPlotProductTests` already proves exact crop/cultivation semantics, owned/missing parts, reskins, stack validity, local world and carried reader custody, optional atlas behavior and real planting. Keep those controls; add only the missing portable contexts and the new constructor seam. Existing eleven-texture Greybox callers already pass plotProductsAtlas: preserve that explicit injection just as the prior essentials slice preserved itemsAtlas. Never silently reload an omitted atlas.

## Approved source and immutable pins

Resource PNG/import remain unchanged. Build complete-frame Sprite views directly from the borrowed texture; do not use a cropped imported sub-sprite, resize pixels, regenerate art or rerun Unity import tooling for this plan. Runtime16PPU/centered pivot(8,12), Point, clamp, no mipmaps, no compression, no collider. Existing editable source and original imagegen provenance satisfy approved-art reuse; no new concept generation or fabricated source is needed.

| File | SHA256 at preflight |
| --- | --- |
| `Assets/Resources/Sprites/Live16x24/plot-products.png` | `453c1eef4fa91f7d607c037a7398f2c6b376ca653e692e4f7bd8782cce3cf7da` |
| `Assets/Resources/Sprites/Live16x24/plot-products.png.meta` | `abb9d983340720e7a4cd6a1c3db2ad9066cdf49241c62aee23d821d2f38b63be` |
| `ArtSource/Live16x24/plot-products/plot-products.blend` | `5ab3ac1f1f6e02d8f66beb7a9baf3b24577b7f1a94975a09f4b7cc0cdbba8341` |
| `ArtSource/Live16x24/plot-products/source-cells.json` | `db6f6fbab95b76da92a3620a63c09eb8c6acb4a2a05cd16e8d247e6d378ffab1` |
| `Tools/Live16x24/build_plot_products.py` | `adc85eea6f88578aca55b75ea4335d17008933ebcabd6c6edb6eb9fb7fcc82c4` |

Source README/manifest/verification and `plot-products-contact.png` are beside the blend. Original concept/prompt are `ArtSource/Live16x24/concepts/plot-products-v1.png` and `plot-products-prompt.txt`. `Tools/Live16x24/import_plot_products.cs` and `profile_plot_products.cs` are historical helpers, not required actions for immutable reuse. Qud is the established restrained grid reference only; no new external art or parity claim.

## Earned route and baseline gate

The existing read-only format8 loader independently confirms fixture `expedition-essentials-used163`: Player2695 at OpenMire18.8.0(40,10), HP36/40, turn2460, carries CandyCarrotSeed2697×4 and EmberwheatSeed2698×2 in the actual InventoryPart list with matching Physics inventory ownership. Quick.sav.gz SHA256 is `976078e435b9ad126fb0c53abf65555b05a64212fe65f3abd1f6e62e48a66d8d`. Earlier fixture157 carries the same IDs/counts, already audited in the expedition-essentials plan. This proves carried custody, not exposed world-owner or seed portrait acceptance.

After delivery, root chooses a legitimate earned branch and captures normal inventory readers, deliberate drops, stepping aside and exposed bodies for both IDs. Current163 contains a real nearby Bandfrog threat; do not call it a safe route or disable it. Ordinary planting needs a visibly verified eligible cell—no claim is made that the current underfoot mire cell is plantable. Normal movement to eligible ground is allowed; no fabricated soil, injected seed, teleport or hidden-owner search. Actual ground readers are a separate capture gate; do not infer them from inventory portraits.

## Minimal tests-first implementation

Prefer extending the existing `FlatPixelExpeditionEssentialsCatalog` with these **two exact roles** and one independently optional plot-products texture. Preserve its existing two-input constructor by forwarding null for the new group; default loading and explicit missing input remain distinct. Reuse its already-authoritative four Greybox routes, frame ownership/disposal, current graph/fog/custody rules and render cues. Store/reuse the existing explicit plotProductsAtlas from Greybox's eleven-input path so old injected local fixtures continue to work. No new general-purpose art framework is justified.

Seed predicates require sole current owned Seed with its exact CropBlueprint and RequireCultivatedSoil=false, alongside the adapter's genuine Item/Render/Physics/Examinable, stack/footprint and foreign-role guards. Do not require a nonexistent Seed tag, category, material, repair or thermal part. Exact known-seed refusal must remain authoritative before the older local opening mapping. Other seeds, crops, gladroot food and reserve products retain their existing routes.

Smallest useful RED additions, using existing fixture helpers:

1. Two parameterized real-factory cases: outside-opening selected world tile and current authorized carried/ground reader use the exact approved rect; inside-opening pixels stay equal. Initial native-body assertion fails on current glyph fallback before checking assets.
2. One constructor/control case: existing two-input essentials construction does not load seeds; explicit valid plot atlas admits only those frames, null/malformed atlas falls back without disabling the three existing essentials; legacy eleven-input local injection still works.
3. One refusal group: wrong crop, flipped cultivation rule, foreign/missing Seed and explicit reskin cannot regain native art through the local fallback. Keep a positive restoration counter and an unrelated reserve seed/crop control.
4. One lifecycle/control case: actual pickup/drop and Plant spends exactly one carried seed, stale loose-item reader/paint retires, remaining stack retains its identity, refused planting spends nothing. Inspection does not plant. The resulting crop keeps its existing presentation; this does not promise new outside-opening crop art.

Use the complete existing `FlatPixelPlotProductTests` plus the preceding Essentials/GlobalPresentation fixtures for borrowed lifetime, selection, fog and unrelated-item regressions rather than duplicating dozens of generic counters. Run/confirm native RED before runtime; add meaningful later counters only if review finds a concrete uncovered seam, and label their timing honestly.

## Acceptance and limits

Root verifies both actual inventory readers and exposed ground bodies, normal pickup, at least one eligible planting if safely attainable through ordinary play, F5/fresh Continue with exact remaining counts and no ghost seeds. If ordinary planting is not observed, leave it native-only explicitly. Root then obtains independent source/actual-image review, records native results, updates bounded coverage and publishes exact owned work. Do not include this planned slice in a package already building from the preceding source.

Cached two-frame reuse introduces no per-frame resource load, timer, animation, simulation event, RNG or growth change. No FPS/zero-allocation/Deck claim. Approved art, normal saves, current builds, unrelated work and all55+5 polish scopes remain preserved/open. Current stage has no implementation or acceptance; root alone owns Unity, tests, Git and delivery. Next action is actual baseline capture after package restoration, then the narrow RED cases above.
