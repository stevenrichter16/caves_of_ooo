# Sill court independent source review

2026-10-28. Independent review of the bounded fresh-Sill generation change. The reviewer read production source, focused tests, the phase plan and root's native count receipt; did not run Unity, tests or Git. This report is the only reviewer-authored file.

**Disposition: no remaining actionable P1/P2 source finding at the hashes below.** The count comparison is acceptable for this bounded relocation. Actual scene, service use, travel and fresh Continue remain separate acceptance gates; this is not whole-town or population-increase acceptance.

The original P2 was an anchor-only clearance check: a passable multi-cell prop anchored outside the court could occupy a preferred cell without appearing in `cell.Objects`. Current `CaptureClearGround` uses `cell.Occupants`, requires exactly one anchored Grass owner and refuses Grass footprints. The added `footprint` obstruction case explicitly establishes an outside anchor, inside occupancy and nonblocking body before requiring ordinary fallback and preservation. The source correction resolves the reported path; root owns its native regression result.

The fixed pair, ten real apron floors and approach spine are in bounds and have non-overlapping footprints. Early house exclusion provides space without bulldozing houses. The Larder mirror copies rows and legend without mutating the shared stamp. Both complete sites and their original Grass identities are checked before and after staging. Reservations follow successful placement; failed additions remove previously staged additions. Ordinary placement remains the fallback, and successful placement skips exactly the two original shop stamps. The shared stock helper retains the existing empty-inventory rule. Exact address, biome, POI type/name/faction/profile gates and the cached-zone path remain intact.

Root's `sill-court-count-comparison.json` records detached full-pipeline enabled/disabled runs for the three planned seeds. The disabled arm removes both Court assignments and its cave-entrance filter. These are count observations, not a claim that unrelated layouts or RNG outcomes stay identical.

| Seed | Beds/chairs, disabled → enabled | Doors | Interior cells | Brain owners | MarketStalls |
| --- | --- | --- | --- | --- | --- |
| 64 | 7/7 → 7/7 | 6 → 7 | 80 → 78 | 20 → 22 | 5 → 5 |
| 65 | 9/9 → 9/9 | 8 → 8 | 102 → 102 | 23 → 22 | 5 → 7 |
| 777 | 9/9 → 9/9 | 6 → 5 | 110 → 142 | 21 → 20 | 5 → 6 |

Every arm retains one of each of the five shopkeepers, Elder, WellKeeper, Farmer and Well; two TinkersForges, two AlchemyStills and one Chest. Beds/chairs are surviving-house proxies, not a universal housing/access proof. The receipt does not enumerate every other fixed resident role, so it does not independently establish their exact counts. Door counts depend on surviving valid apertures; their changes alone do not show removed houses. Randomized resident totals must not be reported as a population increase. Existing `PopulationTable.VillageDecor` permits zero to two additional MarketStalls; the observed totals of five to seven are consistent with that unchanged decoration path, while shopkeeper/service counts stay fixed.

The focused court fixture covers real stock/services, routes from the well, scope rejection, occupied-site fallback, missing paired content and cached graphs. It now includes the footprint case. Native results and gameplay evidence must be attached by root separately. Counter rendering was initially deferred; its subsequent source integration is reviewed below.

Reviewed SHA256 pins:

- `SillCourtComposition.cs`: `9fbdc164199cf7ef166bd6777b39655e881cc230f435bef8ba680c11ec1a6320`
- `VillageBuilder.cs`: `1a64e6753fcc3448dbefad577abee9b7eb50b7cfdfb7e8676314880f757de76f`
- `LandmarkBuilder.cs`: `f89bd3134416547f16b299f74dadcf8b4abb8813cf723825117371629c60c325`
- `OverworldZoneManager.cs`: `dcf2216fc5e05921b20e9e166d132884cef474deab1ffb668ff6b5dfeb73c2b2`
- `SillCourtPopulationTests.cs`: `b9d7699077b5898ec08a68e7ffed9b887cd5a0b3d8eade551d22ecebe59e5d90`
- `sill-court-count-comparison.json`: `8761bf54e8ebf241b32db45f952932273a6d687cc023a18c4c27870ca4ea3e25`

## Exact counter integration

2026-10-28 follow-up: **no concrete actionable P1/P2 finding** in the bounded `FlatPixelSharedFurnitureCatalog` extension, Greybox constructor seam or narrowed Sill fallback case. Root reports 83/83 native court checks passed; related rendering GREEN and actual counter appearance were still pending at this source review. The reviewer did not execute Unity or tests.

The new branch recognizes exact MarketStall identity, canonical glyph/layer, unique owned visible Render/Physics/Examinable parts, actual solidity, world custody and a single-cell body. It refuses added services, container/inventory, creature/terrain, material/damage and custom visual meanings. It adds no gameplay service or collider and preserves the existing painter-owned single Render cue. The four existing world/reader role routes consume this shared lookup and retain fallback/disclosure authority. Ground ownership remains independent.

The original one-texture constructor explicitly omits counter art. The new 16x16 input is independent of the 288x24 Bed/Chair input; malformed dimensions do not reload resources or break the other family. Default travel after warming East art still loads the counter, whereas explicit resource-disabled East injection does not. Full Rect(0,0,16,16), centered pivot, 16 PPU and full portrait UV preserve the approved square image rather than stretching it to a 16x24 body. Views are cached and disposed; textures remain borrowed. The removed Sill pending-family case is precisely MarketStall, with the other pending families retained.

The 17-case fixture covers default/legacy world and reader submission, backing floor, both constructor paths including warmed East, independent missing/malformed inputs, ownership/custody/shape/state refusals, disclosure/cover/replaced graph, Render cues and borrowed lifetime. Actual square-counter size, awning contrast, keeper separation and readability in the generated court remain visual gates; source checks cannot settle them.

Independent hashes confirm the approved counter PNG and importer are unchanged. Counter-review SHA256 pins:

- `FlatPixelSharedFurnitureCatalog.cs`: `49b1412970e3fad2dc077f8d2ae909107bf544ea51aadc930bc57444d82a6eea`
- `FlatPixelGreyboxCatalog.cs`: `efd7bcf40cb2212923e54a47d5df5b83223d8c075f0d4c39e97d32f24a7f124c`
- `FlatPixelMarketStallTests.cs`: `b20e850d4da5b9afb6807c5c4b8861114385f2cd9bc35b2ac7c07b23a28e8ed0`
- `FlatPixelSillEnvelopeTests.cs`: `cd809deb01a26ac805ba1ed1b929bccc09fb2b34d9cfb2c6747b5306ba416358`
- `market_stall.png`: `31d04cd09bf4056d24ffa8aa9691a6ec1a86b09569d8398695ae680e38701a8c`
- `market_stall.png.meta`: `e681ed01707dc460af55603fc9b0a65fa2303325caca3f490a8a63458337c4e5`

## Actual seed64 court and counter review

2026-10-28: **zero actionable P1/P2 visual findings for the bounded court arrangement and exact counter integration.** Independently viewed `sill-court-before64-north.png` / `sill-court-after64-north.png`, the four `sill-court-counter-before/after-world/reader.png` captures, `sill-court-provisioner-interior.png`, `sill-court-well-access.png` and `sill-court-after64-return.png` at normal zoom. The north comparison uses the same player coordinate39,7 but different turns/lighting and naturally changed NPC positions; it establishes composition differences, not pixel identity or a controlled lighting comparison.

The facing shop fronts and small joined aprons give the north-of-well area a clearer arrangement than the former scattered frontage. Both openings remain recognizable; the actual player is shown inside each shop across the counter-world/provisioner views. Straight masonry boundaries, flat exterior aprons, player silhouette and the two compact counters remain distinct. The approved square counter is materially clearer than the former `=` glyph in both the world and reader: striped awning, dark recess and brown counter read as one small prop without covering the player or extending the apparent wall footprint. Its reader still assigns trading to the keeper and introduces no false container/trader service.

The well interaction capture shows the existing draw-water affordance and open approach, and the return view retains the court arrangement. Root separately reports both threshold routes, the forge menu, well access and ordinary glade travel completed, plus83 generation and152 rendering native passes. Those executions are root's evidence; this reviewer inspected the PNGs and source only.

Remaining debt is explicit. The wider lawns and separated houses still leave broad open composition; this pilot does not finish Sill. The after views do not establish two keepers continuously tending their counters: natural combat/reactions moved residents away, so neither a frozen AI schedule nor a sustained increase in visible population is accepted. The shops' grass interiors and open thresholds do not imply new roof/shelter mechanics. Seed65 actual comparison and fresh Continue182 were pending when this section was written. No full-area, performance or packaged-delivery claim follows from this bounded visual disposition.

## Seed65 and fresh Continue follow-up

2026-10-28: **zero additional actionable P1/P2 findings; bounded court arrangement and reuse of the approved counter art are accepted.** Independently viewed `sill-court-after65-north.png`, `sill-court-after65-smith-trade.png`, `sill-court-after65-provisioner-dialogue.png`, `sill-court-after65-provisioner-trade.png`, and `sill-court-fresh-continue182.png` against `sill-court-after64-return.png`. The second seed retains the facing fronts, recognizable openings and small joined aprons. Both trade screens identify the intended keeper and show appropriate existing stock; the provisioner's dialogue exposes the real trade option. These captures establish available interfaces, not completed purchases.

The Continue view preserves the recognizable court, well, player position and surrounding arrangement. Root reports ordinary seed65 bootstrap, both shop thresholds in both seeds, fixture182 at turn580/HP40 after the seed64 forge/well/travel checks, and fixture183 at turn390/HP40 for seed65. Root's world comparison reports a nonempty difference bounding box `(34,270,58,300)`; this review makes no exact-pixel restart claim. Those state and execution facts are root's evidence, separate from the reviewer's inspection of the PNGs.

The actual provisioner remains a smiley glyph, visible in the world/dialogue view, and the forge remains a glyph. This corrects the earlier pilot premise that those families already had native sprite coverage: both are remaining art-coverage debt, not newly delivered art or a required runtime expansion for this bounded pass. Broad empty yards, incomplete resident/prop coverage and naturally moving residents remain open. No full-area, continuously staffed court, increased NPC population, performance or packaged-delivery acceptance is implied.
