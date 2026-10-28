# Portable gladroot and emberwheat seeds

**Status — 2026-10-28: native GREEN142/142 and bounded ordinary Editor use/restart observed.** Four actual baseline and eleven accepted after captures cover the two exact bodies/readers, normal pickup, both planting actions and fresh Continue166. Independent source and actual-image review found no blocking defect; source publication is the remaining root gate. Approved assets and gameplay mechanics are unchanged. Current43a151ddb packages exclude this seed change.

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

Cached two-frame reuse introduces no per-frame resource load, timer, animation, simulation event, RNG or growth change. No FPS/zero-allocation/Deck claim. Approved art, normal saves, current builds, unrelated work and all55+5 polish scopes remain preserved/open. Current runtime has bounded native and ordinary Editor evidence; root alone owns Unity, native execution, Git and delivery. Independent actual-image review is complete; publication remains pending; package and broader area acceptance stay separate.

## Tests-first preparation

`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelPortableSeedTests.cs` contains five native cases: two exact-identity world/authorized-reader cases, one optional atlas and preserved constructor case, one semantic refusal/local-route counter group, and one real drop/pickup/two-unit planting lifecycle. The previous20-case Essentials fixture is unchanged. The new fixture reuses the established HotbarSaveFixture isolation and the existing Essentials reader/snapshot pattern; it does not add a general test framework, synthetic painter or alternate planting path.

The proposed third constructor is accessed through reflection, so the initial expected failures are the existing outside-opening glyph/reader fallback and absent constructor assertions, rather than a compilation dependency. The refusal case first establishes valid local art and then requires the new adapter before checking all four refusal routes. The real lifecycle begins with the currently missing carried native reader, then checks genuine commands and exact stock/crop changes. Those predictions were subsequently confirmed by the five-case native RED recorded below.

Verification corrections: no test accesses internal `Entity.SpatialZone`; the reader retains its public Zone and removes only its owned viewer. `HotbarSaveFixture(false,false)` preserves the previous SeedPart.Factory, SettlementRuntime and TurnManager state; MessageLog/audio/diagnostic collections additionally use the prior Essentials snapshot restoration. Both target seeds intentionally plant underfoot on ordinary Grass with no CultivatedSoil marker, matching the source contract. Their shared planting engine is exercised with gladroot here; existing PlotProduct parameterized cases retain real planting coverage for both identities.

### Actual baseline now captured by root

Root completed four ordinary Editor captures before runtime changes: `portable-seeds-before-01-inventory.png`, `-02-gladroot-reader.png`, `-03-emberwheat-reader.png` and `-04-ground.png` under `Docs/Verification/Live16x24/`. The ground view was independently inspected: small glyph goods remain beside the player in the real mire; the log records the gladroot drop and hostile Bandfrog attacks. This is not a safe staged scene. Root preserved `Fixtures/portable-seeds-ground165` after the real drops: emberwheat2698×2 at(40,10), gladroot2697×4 at(41,10), Player at(42,10), HP28 with bleeding, turn2480. The read-only `portable-seeds-before-state.json` corroborates zone/position/HP; exact IDs/counts and turn are root's saved-session observation. No mechanics were modified to obtain the baseline.

## RED and implementation

Root's actual native EditMode job `b8a08b1c81b8433bb00b44e0c3c10247` completed all five cases with five intended assertion failures: two outside-opening world glyphs, two absent optional-three-texture constructor assertions and one carried-reader glyph. There was no compilation failure. `Docs/Verification/Live16x24/portable-seeds-native-red.json` preserves the uncapped five identities/messages and native timestamps; its result field is null, so no XML/result-array claim is made.

Only two runtime files changed after that RED. `FlatPixelExpeditionEssentialsCatalog` now owns two additional cached16×24 Sprite/Tile views of the borrowed approved plot-products atlas. Its existing two-input constructor forwards null for that independent atlas; its default constructor explicitly loads it. Exact CandyCarrotSeed and EmberwheatSeed predicates require one current owned Seed, the authored crop and RequireCultivatedSoil=false. Existing Item/physics/render/stack/footprint/foreign-role checks remain shared. Disposal releases both new owned views, never the borrowed texture.

`FlatPixelGreyboxCatalog` stores the plotProductsAtlas already supplied through its preserved eleven-input constructor chain and passes it to the Essentials adapter. No implicit reload occurs for a null or malformed explicitly injected atlas. Its four existing authoritative known-role routes now cover these two exact seeds without broadening opening/region admission or unrelated products. No new selector framework, simulation event, RNG, timer, art generation, asset import change or mechanic was added. The new five-case fixture and existing20-case Essentials fixture remain unchanged after RED.

Self-review: 🧪 source publication remains pending; independent actual-image review is complete; no area, full growth-cycle or package acceptance is claimed. Native GREEN and ordinary pickup/planting/restart are recorded below. ⚪ scope remains the two exact roles within the existing portable-supplies adapter; unrelated seeds and crops keep their previous art policy. Independent source review found no concrete blocker. Existing PlotProduct/Essentials/GlobalPresentation fixtures are the regression controls for adjacent art and authority; additional tests should only follow a demonstrated gap.

### Native GREEN and regression result

Root's actual EditMode job `f8ca8df31bba441fb7de483db5975660` passed142/142 distinct cases, zero failed and zero skipped. The reported suite duration was14.1142411seconds. `Docs/Verification/Live16x24/portable-seeds-native-green.json` contains the complete142 per-case result entries: five PortableSeed,65 PlotProduct,20 preceding ExpeditionEssentials and52 GlobalPresentation. These are one combined result, not additive overlapping run counts. All five new tests stayed byte-identical to their RED version; no production mechanic or existing fixture was changed to obtain GREEN.

A post-implementation SHA256 check also confirmed all five approved art/source/import/tool pins above unchanged. These native fixtures establish frame/authority/constructor/command invariants and regressions, not ordinary visual quality, whole-area coverage or device performance. The later ordinary after-use/restart observations are recorded below. Runtime and tests remain frozen; the record does not promote source fixtures into broader visual or device acceptance.

## Ordinary Editor use and restart

The accepted after set is eleven PNGs numbered01–06 and08–12 under `Docs/Verification/Live16x24/portable-seeds-after-*`. The separate `portable-seeds-trial-inventory-still-open.png` and its state receipt are preserved but excluded from that count and from successful interaction evidence. Four before PNGs remain the baseline. Native/code acceptance does not silently substitute for these actual views.

- Continued the earned ground165 state in OpenMire18.8.0: Player42,10, HP28 with real bleeding, turn2480. Emberwheat2698×2 lay on Grass40,10; gladroot2697×4 on Grass41,10. `portable-seeds-observed-ground.json` confirms both exposed exact owners and actual Plantable Grass at40–42,10.
- Ordinary exact ground actions/readers and adjacent pickup recovered the seeds. Actual carried readers show their approved identity frames. No seed, soil or health state was injected to obtain this route.
- Real Plant spent one gladroot,4→3, and created CandyCarrotCrop25521 at42,10. Real Plant spent one emberwheat,2→1, and created EmberwheatCrop25522 at41,10. The planted-ground receipts retain the exact owners; this establishes command effects, not approval of their outside-opening crop sprites or a growth/harvest cycle.
- F5 preserved `Fixtures/portable-seeds-planted166` at turn2510, Player41,10, HP18. Fresh Continue166 restored the same seed IDs/counts3/1, both crop IDs/positions and learned Rain. Normal combat/bleeding losses are retained; this is not a safe, healed or scripted demonstration. `portable-seeds-restarted-state.json` and `portable-seeds-restarted-ground.json` independently record the carried and exposed state.
- Saved image10 versus restarted image11 differs0/564000world pixels within rectangle[0,40,940,640], as recorded in `portable-seeds-image-comparison.json`. This is a bounded world-rectangle match, not a claim that the whole HUD, every save field, another seed or a device build matches.

Source/fixture review: [independent source review](Verification/Live16x24/portable-seeds-independent-review.md) found no concrete blocker and separately parsed RED5 and GREEN142. Independent actual-image review found no blocking seed-body defect, reproduced exact restart pixels and retained the recorded scope limits. All five approved pins in `portable-seeds-art-pins.json` and the49 unrelated dirty paths are preserved per root's check; no storage deletion occurred. Source publication is pending root; no new commit hash is claimed. Delivered Mac/Linux packages remain source`43a151ddbd714d1bf1a786084e88bc2cfcd5106b` and do not include this seed extension. No Linux/physicalDeck execution or FPS claim.

Next is the existing [original Buckler body plan](GRAPHICS-BUCKLER.md), after this bounded review/publication. Sparse mire composition, other seed identities, outside-opening planted-crop art and all55+5area-polish scopes remain open. The last actual hourly report stays2026-10-28T11:40:19.440648+00:00; next due12:40:19UTC. Updating these records is not another hourly report.

### Changed files in this slice

- `Assets/Scripts/Presentation/Rendering/FlatPixelExpeditionEssentialsCatalog.cs`: two exact seed roles and optional borrowed plot-products frames.
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: preserve and forward the existing explicit plot-products input.
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelPortableSeedTests.cs` plus fresh `.meta`: five focused tests; preceding fixtures unchanged.
- This plan, the completion/whole-world coverage continuations, CLAUDE's current scope, and the two coverage JSON ledgers: bounded evidence/current-next status only. Root owns the actual captures/receipts, review and publication.
