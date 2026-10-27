# Next worldwide sprite slice: portable harvest products

2026-10-27. **Status: eight native frames and nine unchanged reuses integrated; final399/399 native tests pass. PaleSalt and Bone are observed through ordinary gameplay and Editor restart. Source push and fresh packages are next; the other15 ordinary observations remain open.** Source inspected: the actual boot-loaded `Assets/Resources/Content/Blueprints/Objects.json`, native catalogs, ordinary population/composition sources, loot registry and existing inventory/examine route. Bootstrap loads this exact Objects file (`GameBootstrap.cs:230–239`); BlueprintLoader resolves inheritance (`BlueprintLoader.cs:35–105`).

## Concrete problem and bounded deliverable

I viewed `Docs/Verification/Live16x24/beating-final-09-pale-salt-inventory.png`: the successfully harvested pale-salt item is selected in the real inventory and its portrait is still an enlarged yellow fallback glyph. The captured native vein and successful harvest do not finish the product's visual identity. Bone and SaltbriarSprig are source-confirmed missing native product mappings; this screenshot itself does not show them, so their actual product captures remain pending.

The loaded Objects file declares **17 distinct Harvestable output identities**. This is a bounded static content set, not a count of all game items, all runtime-modified yields, crops, corpses or preparation outputs. Nine already have approved local native frames; eight need new native static sprites. Proposed delivery: **8 new native16×24 product frames +9 immutable reuses, all17 identities usable in actual world/inventory/examine contexts across any biome/area where that legitimate item exists.** Do not make every regional graph globally admitted. No animation or gameplay-balance work.

The render route currently permits the four approved portable equipment identities and portable actors outside local opening graphs. Most opening ItemFrameOf/WorksProductFrameOf results remain behind `local` in the world and subject paths (`FlatPixelGreyboxCatalog.cs:838–886,909–961`). The existing product methods' name `PortableNativeProduct` describes item shape, not global route admission. More PNGs alone will not fix the nine reuse gaps.

## Exact reuse contract — preserve all pixels/source bytes

| Existing product | Resource and atlas geometry | Exact frame | Existing semantic selector |
|---|---|---:|---|
|GlowQuartz|`Sprites/Live16x24/opening-items`,12 frames,192×24|6|`FlatPixelOpeningArtCatalog.ItemFrameOf`: owned MineralPreparationPart|
|Mushroom|same opening-items|4|owned FoodPart + CookablePart.Into=`RoastedMushroom`|
|FireClay|same opening-items|9|exact item identity; strengthen common portable owner/footprint/part checks, without adding a nonexistent MaterialPart|
|SalvagedTimber|same opening-items|10|exact item identity; current source also owns PreparationRecipePart (`shape_haft`)|
|StoneburrSeed|`Sprites/Live16x24/north-works-products`,7 frames,112×24|0|owned ReagentPart with positive binding property; this is a reagent, not SeedPart crop seed|
|FrostLichen|same north-works-products|1|owned ReagentPart with positive cold property|
|MendleafSprig|same north-works-products|2|owned ReagentPart with positive vital property; source also owns preparation recipe|
|WildBerries|same north-works-products|3|owned FoodPart|
|GoldCoin|same north-works-products|4|owned CommercePart|

Source selectors: `FlatPixelOpeningArtCatalog.cs:270–283,319–353,540–563`; resource names/counts15–27,127–138. Opening-items11 is the specific Marlback corpse and must **not** be borrowed for Bone; all nonlisted opening sprites stay under their existing policy. Existing atlas/frame ownership can be reused through a narrow helper or a small companion with borrowed textures; avoid reauthoring/recoloring accepted images just to make a packet uniform.

## Eight new products — proposed frozen part map after actual concept critique

Proposed `world-harvest-products` atlas:8 horizontal cells,128×24, transparent item silhouettes, Point/no mipmaps. These are real carried products, not miniature world nodes. Exact final prompts/parts must be recorded after the screenshot reinterpretation. The frozen source now follows this exact eight-frame map; ordinary gameplay acceptance remains pending.

| Frame | Product / registered source | Distinct silhouette and required semantics |
|---:|---|---|
|0|PaleSalt (`Objects.json:29120`)|A small pale crystalline cluster or broken salt crystals with restrained dust; distinguish from huge live vein and GlowQuartz. Item/Mineral, owned MineralPreparationPart and CommercePart. **No MaterialPart is authored on this item.**|
|1|Bone (`2292`)|A pale worn length with a porous broken end; neither a skull pile nor Tepuibone. Item, owned HandlingPart and CommercePart; original non-solid takeable body. World source is Bones, a different blueprint.|
|2|SaltbriarSprig (`33852`)|A small gray/tan salt-rimed leaf sprig, readable separately from green Mendleaf and red berries. FoodItem with owned FoodPart, Cooking=`Meal`; current Healing1d2 is textual mechanic, not a reason to add healing sparkle.|
|3|ChoirIron (`29193`)|A compact dark metallic ore chunk with reddish fractured crumbs; distinguish from PaleSalt and ordinary stone. Item/Mineral, owned MineralPreparationPart and CommercePart. No invented generic IronOre identity.|
|4|Honeycomb (`29337`)|A broken amber comb with visible wax cells, not a honey bottle/beehive. FoodItem with owned FoodPart; actual honey coating interaction remains in MaterialFieldActions.|
|5|GroveRed (`36253`)|A dense unsettling red fruit/growth piece, not wholesome berries or purified pulp. ReagentItem with vital:3 **and toxic:1**, PreparationRecipe=`detox_grove_red`. Native art must not remove or conceal toxicity information. Its original RenderLayer is3, unlike most product layer5; don't impose a generic layer5 guard.|
|6|Tepuibone (`36718`)|A dense pale sandstone fragment retaining closed wood-like grain. **It is stone from the Root, not animal Bone.** Item/Mineral, PreparationRecipe=`shape_head`, weight12. It has no MineralPreparationPart; do not copy the infusion-mineral guard blindly.|
|7|KnotflaxCord (`43535`)|A short coiled/braided off-white cord with a clear knot; distinct from a loose fiber plant. Item, bounded live stack, owned PreparationRecipePart authorizing braid_binding/bandage. Trap, repair and consumption state belong to their real services.|

Exact field values which change prices/quantity/condition should continue to be shown in UI text rather than baked into a sprite. Verify original part/tag shape with the runtime factory before implementation; source-only inherited summary is not native execution evidence.

## Where these owners are actually supplied

This table grounds broad reuse without claiming all areas have been seen. The public sprite policy should follow the legitimate item, including transport/trade, rather than its harvest birthplace.

| Product family | Source-grounded supply and area reach |
|---|---|
|Bone|Bones in Beating CaravanRoad/population (`BeatingCompositionPlan.cs:221`; PopulationTable473–516); also Stillleaf, Olderdeep, Marrowstye, Cathedral and Ginmere composed underground cache areas (`*CompositionPlan.cs:94,67,76,87,65` respectively), plus sinkhole descents (`SinkholeDescentBuilder.cs:85`). Registered BoneCacheT1/T2/T3, TombVaultT2 and UndertakerStock loot also supply the carried Bone.|
|PaleSalt / GlowQuartz / ChoirIron|Beating SaltPan vein (`BeatingCompositionPlan.cs:214`), opening quartz (`ReferenceGladePlan.cs:51`), and shared depth population mineral bands: quartz always, salt tier≥2, choir iron tier≥3 (`PopulationTable.cs:1159–1172`). Grovelands also places actual ChoirIronVein (`GrovelandsFormationBuilder.cs:257`). OreCache/ArcanistStock/construct-death tables supply products beyond direct vein harvest; these are delivery routes, not proof of every seed's contents.|
|SaltbriarSprig|Beating's three tier populations include real Saltbriar (`PopulationTable.cs:473,495,515`); Harvestable outputs this product. It may legitimately travel into every other region without changing identity.|
|GroveRed|Real GroveRedGrowth near Grovelands grove edges (`GrovelandsFormationBuilder.cs:138–150,241`); source literal product is toxic reagent, not FoodItem.|
|Tepuibone|Real Stump formation veins (`StumpCompositionPlan.cs:262`; StumpCompositionBuilder65); RootSite places three actual loose Tepuibone (`RootSite.cs:26,85–105`); MorrowfastMenderStock, Felling/Morrowfast current sources also supply it. Important preparation/trade/ending identity, although those later scene kits remain unfinished.|
|WildBerries / Honeycomb / GoldCoin|Spread's real BerryBush/Beehive/HollowStump population (`PopulationTable.cs:392–394,417–418`) and registered Jungle population712–714. GoldCoin also comes from CompostCache in Grovelands (`GrovelandsFormationBuilder.cs:368`) and broad registered container/death/merchant loot. Honeycomb has Basket/Innkeeper/Envoy routes; berries have Basket/Villager/Farmer/Gnome routes.|
|Mushroom|Real MushroomRing in the opening and CaveTier1 (`ReferenceGladePlan.cs:54`; PopulationTable538), plus MorrowfastProvisionerStock, Provisioner/Villager/Gnome/WaysideCook and sack/basket loot.|
|MendleafSprig / StoneburrSeed / FrostLichen|Actual Spread exploration forage (`SpreadExplorationWorksites.cs:48–60`), with Apothecary/CommonReagent/RareReagent/Farmer/Hermit registered stock supplying the same carried items. Reagent and future regional flora coverage must remain distinct.|
|FireClay / SalvagedTimber / KnotflaxCord|RepairClayBank, RepairTimberPile/GleanersTimberPallet and RepairCordBundle finite harvest owners in Objects; MorrowfastMenderStock (`LootTables.json:1–46`); actual Sodden works stocks cord (`SoddenDistrictBuilder.cs:28–30`). Cord also comes from its crop. Preparation, repair, trap-jamming and snare use are real current mechanics (`MaterialUseDescription.cs:52–59`; PreparationRecipePart37–43; CordSnareActions).|

Registered scope is all10 biomes/area families in the world ledger, but this slice has **not** audited or captured a resource source in each biome. Overwrit, special sites, settlements, lairs and unknown local variants remain explicit scene/coverage work. A resource sprite can be portable into those areas without implying their terrain/actors are finished. Legacy `SpreadPortableRecipes`/voxel recipes and EnvironmentSpriteRenderer generic item icons are not accepted native16×24 coverage; the observed flat inventory still uses fallback.

## Minimal integration and guards

Use one small exact-product semantic adapter or a narrow portable helper plus8-frame companion; no universal item taxonomy, global regional admission, new render hierarchy or new UI flow. World and subject routes must consult the same exact mapping. A private known-role set with17 names is acceptable. It should preserve all unrelated opening artwork and four portable equipment paths.

- Common semantic gate: exact original blueprint; Item tag; no Creature/Player/Terrain/Crop aliases; sole-owned visible Render and Physics; non-solid/takeable body; no explicit VisualID/VisualVariant or unrecognized glyph-variant shape; valid owned positive stack within MaxStack; one zero-offset cell and current registration if spatially placed. A carried item legitimately has no SpatialZone. Required semantic parts must be sole-owned, not merely the first GetPart result. Do not use a biome gate for portable products.
- Preserve dynamic truth: refuse neutral-white native art for active owner color overrides (Wet/Burning/Frozen/Electrified etc) unless the existing presentation explicitly represents them; do not fire Render events in catalog queries. Unknown transformations/reskins retain fallback. No item gains/removes a material, recipe, food or reagent part to satisfy its sprite guard. Bone/PaleSalt/Tepuibone have materially different original parts despite similar punctuation glyphs.
- Family gates: Food/Cookable products validate their real food/cook transition; reagents validate meaningful positive properties and owned cache; mineral infusion products require MineralPreparation; Tepuibone and cord use their authored PreparationRecipe; timber recipe/repair meaning remains intact. Do not classify all '*' as minerals or all '%' as food. GroveRed's positive toxic property remains in the contract; CleansedGrovePulp is a different product.
- The adapter resolves semantics, **not disclosure**. World calls already supply fallback's actual current selected visible owner. Public inventory/examine goes through the real authorized document/custody route (`InventoryUI.LedgerView.cs:305–320,347–357`; `ExaminePortraitRenderer.cs:73–105`; ExamineDocumentBuilder.IsCurrentSubject). Do not require world placement for a carried item, and do not make a raw semantic resolver itself proof of authorized inventory access.
- Reuse exact borrowed atlas regions; optional missing/malformed groups fail locally without reloading Resources behind injection. Own/dispose cached generated Tiles/Sprites only. No per-repaint texture extraction/readback/allocating art. Keep point sampling and single sRGB decode of existing inventory/examine drawing.

## Pipeline and bounded acceptance

1. Start from the earned actual Beating harvest checkpoint `Docs/Verification/Live16x24/Fixtures/beating-final-harvest65` and screenshot final09. Capture actual product ground/drop, pack selection and F1 details for PaleSalt; obtain Bone/SaltbriarSprig via their normal nearby owners when reachable. Record capture gaps honestly. Do not spawn a fake stocked inventory for ordinary acceptance.
2. Reinterpret that actual UI/ground capture with imagegen, using accepted item atlases as style references. Extract8 named static product silhouettes and exact owner/state mapping. No letters, loot tiers, invented bottles, larger vein scenery or generic substitute ore. Preserve the existing9 sprites.
3. Native RED before routing: exact17 owner semantics and frame identities; paired unknown-name/reskin/wrong-part/refused-status cases; optional resources and lifetime; ordinary global world route in local/nonlocal surface and an underground graph. A bounded context matrix may cover all registered biome keys for portability, but that is not proof of real travel to all10.
4. Exercise public reader authority with real carried inventory documents, stack split/merge/consumption, drop/pickup, same object moving to another owner, stale document/replaced graph, world coverage/fog, and a source/derived-product identity pair (Bones versus Bone, PaleSaltVein versus PaleSalt, GroveRed versus CleansedGrovePulp). Reuse existing global equipment/portrait fixtures, not a new framework or a full duplicate92-case regional suite.
5. One submitted world full/dirty drop→pickup test proves the actual painted item and its disappearance; one real resource action/harvest proves output remains semantically unchanged. Do not simulate a craft merely to render a preview. Tests must restore global content registries/saves/hooks and preserve unrelated state.
6. Return through real gameplay inventory/F1/world at two ordinary area contexts, save/reload, and capture after views matching final09. Verify harvested product and source look related but unmistakably different. The first8 are authored once and shared; finish any missing ordinary observations before claiming those families observed/delivered. Root owns package/main preservation and current-build verification.

## Explicit remaining queue after this slice

The17-item set closes the inspected finite Harvestable output family, not all items. Crop produce/seeds, prepared derivatives (CleansedGrovePulp, bandages, haft/head/binding components), RawMeat/CookedMeat, liquid vessels/content states, all remaining weapons/armor/books, quest stones MemoryMarble/MuteStone, artifacts and dynamic corpses stay separate. Existing accepted plot-products and other local item frames may need later portable delivery checks. Regions still need coherent terrain/flora/actors/props and whole-chunk fullness; this inventory-focused slice must not replace the continuing all-biome scene work.

**Next action:** root preserves the current Beating release, then creates the actual product baseline/concept/native8 + portable17 test slice. No further broad census is necessary before that bounded work starts.

## Test-first preflight — 2026-10-27 07:40 UTC

Status: source/API verification and native RED test authoring. Production routing
and the eight new sprites have not been accepted. This is CoO-original item
presentation; no external-game parity is claimed. Root retains Unity and actual
play ownership.

| Planned assumption | Verified correction / implementation contract |
| --- | --- |
| All products can share a generic mineral/food shape | The inherited Objects definitions give every one of the 17 an Item, Render, Physics, Stacker and Commerce shape, but distinct functional parts. None authors Material or Thermal. GroveRed is a layer-3 toxic reagent; the other products are layer 5. Do not add nonexistent parts or require a generic layer. |
| Reagent values must match literal authored text | `ReagentPart.GetProperties` parses and caches current raw properties; require the meaningful positive properties, including GroveRed's vital **and toxic** identities, without requiring original potency, ordering or letter case. |
| Recipe membership can use substring matching | `PreparationRecipePart.Allows` uses exact comma-separated tokens. Tepuibone requires shape_head; timber shape_haft; cord braid_binding and bandage; GroveRed detox_grove_red; Mendleaf concentrate_mendleaf. Art must not execute a recipe. |
| A missing product resolver can simply fall through | The older local `FlatPixelOpeningArtCatalog.ItemFrameOf` and `WorksProductFrameOf` use weaker guards. A known product refused for malformed parts, reskin or owner-color state must not regain native art through those paths. Test both the real opening and a non-opening graph. |
| Frozen/electric is a useful original-item effect counter | Original products have no authored Material tags, so those effects may be refused before the art query. `ObjectStatusMatrix` permits Wet on objects; assert actual Wet application and its blue render override before testing neutral-art refusal. |
| Quantities or commerce must equal defaults | Require a live owned stack within its current maximum and correct functional ownership, not exact quantity, value, price or condition. Stacker defaults to maximum 99; cord and timber author 20. |
| Footprint refresh is a public helper named RefreshEntityFootprint | The real public mutation is `Zone.TryChangeFootprint`; `RefreshFootprint` and `Entity.SpatialZone` are internal. Tests use public zone/custody methods and reflection only for the proposed companion and existing fixture seams. |

Proposed small adapter: `FlatPixelHarvestProductsCatalog`, default resource-loading
constructor plus `(Texture2D products8, Texture2D openingItems12,
Texture2D worksProducts7)`, `TryResolve(Entity, out Tile)`, and owned cached-frame
`Dispose`. Greybox owns an optional `_harvestProductsArt` instance. This resolver
answers semantic art only; existing current-graph, selected world owner and
inventory/examine document authority remain responsible for disclosure. A
missing injected texture group remains unavailable without a Resources reload.
The proposed tests use actual factory products and public world/readers, with
paired part, state, custody, current-graph, stack and resource counterchecks,
real harvest/pickup/drop, and one submitted full/dirty paint sequence. They do
not certify ordinary player acquisition, image quality or all-biome travel.

## Current production pass — 2026-10-27 07:56 UTC

Actual isolated Editor baseline resumed the earned packaged BrineLens checkpoint
(turn3210, HP38, player40,12). PaleSalt42025 quantity1 was inspected in inventory,
F1 details, dropped, viewed at40,12 from39,12, examined through C→Right→X,
then recovered with normal movement/G and F5 saved atturn3240. All four visual
contexts used the old glyph. Six unedited Game captures and exact actions are in
`Verification/Live16x24/harvest-products-baseline.json`. The normal save trees and
38 pre-existing tracked edits remain unchanged; owned baseline saves were archived.

Built-in imagegen reinterpreted the actual packaged inventory capture and the
approved item atlases, then produced an eight-part silhouette study. Prompts,
concepts and selected external reference records are in
`ArtSource/Live16x24/world-harvest-products/concepts`. Concepts are direction only.
Original native pixel geometry was authored in the editable Blender source; no
concept crops became game assets. Root reviewed the eight silhouettes on green,
sand and inventory backgrounds and approved the integration trial. Source checks
retain missing-art RED, GREEN,13 mutation counters,317 unchanged approved inputs
and exact decoded-pixel reproducibility. Native PNG encoding metadata can vary
between renders; byte-identical PNG reproduction is not claimed. The runtime
copy and authored Unity metadata pass the source import verifier.

Native RED at07:55:34–07:55:41 UTC ran123 cases, all123 failed on absent companion
or actual world/carried glyph delivery, with zero compile failures. Raw XML and
MCP receipt are saved as `harvest-products-native-red.*`. Production implementation
started only after this result. No GREEN, ordinary-after, package or whole-biome
completion claim follows from this current record.


### Native RED and first implementation

Root saved the genuine native RED in
`Verification/Live16x24/harvest-products-native-red.xml`: **123 cases failed**,
including the missing companion and public world/carried-reader glyph assertions.
Production followed that run. `FlatPixelHarvestProductsCatalog` now maps the
17 exact products to the eight new native cells and nine unchanged atlas reuses.
`FlatPixelGreyboxCatalog` owns the lazy adapter, preserves the injected legacy
item/works texture groups, and prevents refused known products from falling back
to weaker local native selectors. Both subject routes and old/continuous world
routes share that policy. The implementation owns only cached Tiles/Sprites,
uses current parsed reagent properties and exact nonallocating recipe-token
comparison, and changes no generation, inventory rules or material definitions.

Files in this first test/runtime change: new
`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelHarvestProductsTests.cs`
and metadata; new
`Assets/Scripts/Presentation/Rendering/FlatPixelHarvestProductsCatalog.cs` and
metadata; `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`;
and this preflight/implementation record. The source-art packet and native atlas
are handled separately by the root and art owner. Compilation, GREEN, independent
runtime review and actual after-play/package acceptance remain pending at this
recording point. The 123-case RED is not passing behavior or visual acceptance.


### First combined native trial and saved corrections

The first combined run recorded **397 passes and 2 failures out of 399**, retained
in `Verification/Live16x24/harvest-products-native-trial.xml`. One submitted-paint
test called raw one-cell inventory/Zone mutations without asking for a renderer
refresh, so it inspected the previous player's glyph. Zone publishes automatic
footprint dirty hooks only for explicit footprints; the real inventory UI owns
its closing redraw. The saved test now submits the changed cell through the
public dirty API after the real drop/pickup, still requiring the actual painted
native tile, its disappearance and no full repaint. This is a fixture correction,
not a new gameplay invalidation behavior.

**Intentional narrower art contract:** the old opening test permitted a repeated
native SalvagedTimber image on an authored two-cell body. The new portable17
policy only represents the approved single zero-offset product silhouette.
A changed multi-cell timber owner now keeps its truthful glyph while its real
physical footprint and interactions remain intact. Root approved that bounded
contract change. The old test was replaced with a paired single-cell native →
public two-cell footprint change → same-owner glyph/unchanged two-cell body →
single-cell native restoration → removal check. No footprint mechanic changed.
Corrected native GREEN remains pending; the failed trial is preserved.

### First integration trial and corrections

The combined native run at08:03:52–08:04:39 UTC executed399 cases:397 passed,
2 failed, no inconclusive/skipped cases. Raw `harvest-products-native-trial.xml`
remains unchanged. One older test deliberately rendered a modified two-cell
SalvagedTimber through the local legacy route; the new exact carried-item policy
requires a one-cell neutral silhouette and instead preserves the truthful glyph
and physical footprint. The paired regression is being updated to this documented
stricter visual contract; no movement, item, save or footprint mechanics change.

The submitted-paint fixture called raw inventory/zone methods without the UI's
normal repaint notification and retained the old player's glyph. Public one-cell
Zone movement does not itself publish that render hook. The fixture correction
requests only the changed cell via the public renderer dirty hook, then verifies
the actual submitted native tile and pickup clearing without a full refresh.
Ordinary UI drop/pickup acceptance remains a separate required gameplay gate.

### Native GREEN and bounded self-review — 2026-10-27 08:10 UTC

Root's rerun of the same combined suite passed **399/399 distinct cases**:
**123 harvest-product cases and 276 related cases**, with **0 failures,
0 skipped and 0 inconclusive**. The saved native XML is
`Verification/Live16x24/harvest-products-native-green.xml`, recorded from
08:09:45 to 08:10:32 UTC. This confirms the corrected saved source and tests;
the original 123-failure RED and 397/399 first trial remain retained.
Production code is now frozen for root's actual UI/world comparison.

The native result covers exact product/frame identity in public world and
carried-reader routes; all ten registered biome keys as bounded synthetic
contexts; current-graph, visibility and custody guards; single-cell/part/stack
semantics; the actual Wet positive control; recipe/reagent meanings; real
harvest, stack, pickup/drop/recipient actions; optional-resource isolation;
legacy injected frame compatibility; disposal; and submitted dirty repaint.
The registered-biome matrix is not natural travel or an actual source capture
from all ten biomes. The test's explicit dirty submission is not evidence that
raw inventory commands independently invalidate UI. No quantities, prices,
recipes, footprint mechanics or world generation were changed for the art.

Self-review before gameplay acceptance:

- 🟡 Fixed the legacy-selector bypass: refused known products cannot regain
  native art through the older opening item groups. Both real opening and
  non-opening counterchecks pass.
- 🟡 Fixed the submitted-paint fixture's missing redraw request, retaining
  actual drop/pickup and submitted-tile assertions. No gameplay patch was needed.
- 🟡 Documented and tested the intentionally narrower multi-cell timber art
  contract; the real body and interaction geometry remain intact.
- 🔵 Eight new native product frames and nine immutable frame reuses share a
  cached semantic resolver. Injected optional groups do not reload Resources;
  disposal destroys generated sprites/tiles and preserves borrowed textures.
- 🔵 The live owner retains recipe, reagent toxicity, commerce, stack and
  custody truth; art queries do not fire Render events or perform preparation.
- 🧪 Actual after-play inventory/F1/ground captures, carry/travel/save/reload,
  packaged delivery and hardware performance remain pending at this checkpoint.
- ⚪ These 17 products do not complete all items, a biome, or whole-scene
  fullness. Grovelands/Overwrit/Stump and remaining region/underground/site kits,
  plus Beating/Sodden props, services and actors, remain in the coverage queue.

## Actual integration acceptance — 2026-10-27 08:24 UTC

The unchanged turn3210 Beating checkpoint now shows PaleSalt42025 natively in
inventory, full details, uncovered ground and adjacent ground reader. Normal
D/drop, walking and G/pickup preserve its identity/count. Ordinary world-map
travel reaches CaravanRoad19,13; normal C/Right/H harvests actual Bones38104
at59,9 into Bone42026 count1. Inventory shows the new bone and the carried salt.
F5 atturn3540 saves HP38,cell59,9. A fresh isolated Editor launch/Continue from
the exact earned save restores both items and the absent harvested Bones.
Capture after10 shows the bone after restart. The exported checkpoint is
`Verification/Live16x24/Fixtures/harvest-products-road65`; actions, hashes and
all ten actual after screenshots are in `harvest-products-play-summary.json`.

Can verify: real input actions, current owners/counts, native portraits, harvest
removal, cross-zone portability and Editor save/restart continuity. Visual
inspection confirms a small crystalline salt cluster distinct from its vein and
a narrow animal bone distinct from the Bones world pile. The29.847second Editor
profile is informational, not a controlled speed comparison or device benchmark.
Cannot verify here: ordinary observation of the other15 products, whole-biome
fullness, packaged launch of this revision or physical Deck operation.

Normal saves and38 foreign tracked modifications remain hash-unchanged; owned
isolation roots are archived. No cache or build deletion occurred. No gameplay
quantities, recipes, prices, damage, generation or optional animations changed.
Next: preserve reviewed source on main, refresh packages, then begin the actual
Grovelands baseline and native area kit. Do not wait for all15 remote product
observations before covering unrepresented regions.
