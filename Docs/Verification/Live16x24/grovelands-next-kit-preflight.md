# Grovelands: next cohesive native area kit

2026-10-27. Read-only source preflight after the portable harvest-product pass. **Recommendation: render the existing forest envelope and the four formation-defining material/interaction families together, beginning ordinary capture at a Grove and its adjacent compost/fruiting districts.** Do not add decorative population or new systems to make this region seem fuller. The existing owners already provide a different, coherent place; most lack admitted native identities.

No art, runtime, tests, Unity state, Git state or build was changed for this preflight. Source-only calculations below are not generated-world receipts or actual traversal. The existing voxel composition previews are historical, not current flat16×24 gameplay acceptance.

## Verified scope and corrections

The coverage ledger reports43 authored Grovelands surface cells and no admitted region-specific native kit at its audit (`ArtSource/Live16x24/world-sprite-coverage.json`, biome `Grovelands`). Its candidate list is a starting inventory, not all visible owners. `GrovelandsCompositionPlan.IsWildernessZone` indexes authored surface addresses excluding named places, sinkhole mouths and the doll grove (`Assets/Scripts/Gameplay/World/Generation/GrovelandsCompositionPlan.cs:35–47`).

Reading the authored rows and reproducing the small FNV selector yields **37 static ordinary candidates:13 Grove,7 TendrilFen,11 FruitingWall and6 CompostingField**. Excluded from those37 are Morrowfast3,6; Cinderhold6,6; Deepest Cathedral5,4; Olderdeep4,6; Ginmere2,7; and the authored doll grove1,6. This is address metadata only. A receiving saved world can add a lair, camp or other POI; its live map and saved-lair authority must still veto regional admission.

A material correction for content expectations: **all37 ordinary authored candidates have tier3**, so the ordinary population table is `GrovelandsTier3`, not the gentler tier1 table. Tier3 supplies one weighted encounter family among Shambler/Mosshulk/Rotling, WineLeafSundew1–3, optional ChoirTendril and HelmwoodFrog. It does **not** contain GroveLanternMoth. Do not promise a moth-led first journey from the general faction description or tier1 tables. Exact spawn outcomes still require the receiving world (`PopulationTable.cs:613–677`; authored biome/tier rows in `WorldMapAuthoring.cs:52–107`).

Other important distinctions:

- The base floor is **Grass**, not a missing `GroveFloor` blueprint. The composed forest uses existing Grass, VineWall, Tree and Bush. Adding art does not require new terrain definitions.
- A GroveSeep is a finite water pool with a Well service, not an inexhaustible decorative well. Drawing water spends one volume and empty stock is unavailable. Its original volume40 is a default, not an identity guard. There is no authored TileStateSource on this owner; do not reuse the Sodden “depleted but renewing hazardous film” rule blindly (`Objects.json:35167`; `WellDrinkingService.cs:7–38`; `LiquidPoolPart.cs:35–173`).
- LiquidPool initialization can apply the liquid registry's glyph/color. Runtime guards must follow actual known water semantics rather than pinning only the raw JSON glyph/color.
- FruitingBody is a **solid** destructible plant shelf, not a harvest source. GroveRedGrowth is a separate **non-solid** Harvestable source yielding raw toxic GroveRed. Do not paint every violet shelf as ripe edible fruit.
- CompostRow is non-solid and non-takeable. CompostCache is a separate non-solid Harvestable on a row cell:60% chance of1–3 GoldCoin. Harvest consumes the cache even when the roll yields nothing; the underlying row remains. A “pile of visible guaranteed coins” would be false.
- MycelialColumn owns an actual radius3/intensity0.5 light. Its source description has pale growth/violet veins while its light field is green. A quiet static veined body can coexist with the actual light; no new pulse, halo or full-cell luminous carpet is required.
- WineLeafSundew is a non-solid plant with GreatdewSnare, not a hostile Creature or food. Its45% grab chance/three-turn hold/corrosion are existing mechanics. `GreatdewSnarePart` triggers on entering the plant cell, not adjacency, and is not consumed on trigger (`TriggerOnStepPart.cs:432–483`). A distinctive low dark wine rosette is an important danger identity, not optional decoration.

## What the generators really make

`OverworldZoneManager.GetPipelineForZone` routes POIs before ordinary biome generation (`OverworldZoneManager.cs:118–199`). The ordinary Grovelands branch creates a composed surface pipeline and adds BloomFront (`249–268`); the special doll grove is deliberately separate. `CreateGrovelandsPipeline` at817–830 uses `GrovelandsCompositionBuilder(WorldSeed)` plus `GrovelandsFormationBuilder { Composition = composition }`.

|Area family|Existing generated composition and functional destination|Visual priority|
|---|---|---|
|Shared forest envelope|Grass in every cell. Clustered VineWall at high canopy, Tree at intermediate density and Bush at margins; reserved openings and three-cell approaches remain clear. Boundary rows are left open. (`GrovelandsCompositionBuilder.cs:17–42`)|Quiet ground and connected obstructing vegetation must be visible before decorating a showcase ring. Borrow accepted actual-identity art where appropriate.|
|Grove|Open elliptical floor, gapped MycelialColumn ring and a guaranteed central GroveSeep;2–4 GroveRedGrowth attempts around the edge;3–6 outer columns; grown GroveSign placed after reachability repair. (`GrovelandsFormationBuilder.cs:78–202`)|Pale fungal columns and a readable small water destination; a physically accessible grown sign. Keep the central emptiness, entry gaps and visible approach lanes.|
|TendrilFen|Two permanent **tile-state water** veins braid through the forest, with solid FruitingBody banks and occasional red growth;30% attempt for a ChoirIronVein;1–3 ChoirTendril residents targeted after repair. (`GrovelandsFormationBuilder.cs:212–282`)|Connected wet lanes and distinct solid banks; actual NPC silhouettes remain a separate required follow-up. Water here is tile state, not hundreds of fabricated pool owners.|
|FruitingWall|Three reserved beds produce nine short vertical broken shelves of solid FruitingBody. Individual placements pass90% rolls and avoid approaches; repair removes only this builder's placements. (`GrovelandsFormationBuilder.cs:289–316`)|Compact shelf formations with broken rhythm, visibly solid but not masonry or a screen-wide barcode.|
|CompostingField|Three reserved beds, spaced straight row segments with density depending on bed stage;2–4 finite CompostCache attempts on genuine row cells. (`GrovelandsFormationBuilder.cs:322–370`)|Low continuous organic rows with quiet cloth/chitin/boot remnants and distinguishable pocketed cache spots. Walkability must remain legible.|

The shared surface pipeline also adds connectivity, cave entrances, ambient stamps, hazards, containers, population, trade stock and sparse haulable props (`OverworldZoneManager.cs:918–938`). These make whole-view coverage broader than the four formation lists:

- Grovelands ambient stamp catalog: GroveShrine, MendleafGarden and HuntersBlind. Shrine can supply the five named Choir keepers Mogu/Grib/Nam/Sien/Sopp plus Campfire; it is not a named world POI (`LandmarkBuilder.cs:705–738`). Do not scatter manufactured ruins merely because other biomes have them.
- Hazard table: PeatBog, BrinePool, SteamVent, DryBrush and MirrorMucilagePool; conductive patches may add CopperPipe (`HazardTerrainBuilder.cs:113–120,154–176,183–247`). They need honest visual/state handling when encountered. A generic green floor must never erase those cues.
- One sparse FallenBeam or HaulBarrel is possible, not a clutter population (`HaulablePropBuilder.cs:24–62`). Ordinary containers and loose loot can appear; reused material art is not acceptance of all these owners.
- BloomFront membership is a fixed8% address roll, vetoed by actual shrine keepers. It adds6–10 **BloomingFruitingBody**, up to2 actual Bloomed hosts, and a SmithAnvil/Warhammer workshop attempt (`BloomFrontBuilder.cs:42–154`). BloomingFruitingBody is not ordinary FruitingBody. Preserve distinct source/status semantics; do not let a generic shelf mapping hide the difference.
- The five Grovelands crops in the coverage ledger remain separate families: Choirwick, Knitmoss, Sourmantle, Murmurpod and Sealbark. `BiomeCropPlan.SurfaceSites(manager)` ranks eligible receiving-world candidates and allocates up to10 Grovelands patches, rotating five species; `ForZone` is appended through `BiomeCropPlacement` during ordinary pipeline assembly (`BiomeCropPlan.cs:27–46,62–69`; `OverworldZoneManager.cs:86–87`). Placement stages two actual crop owners, including one at growth stage2, after source checks. Their old3D recipes are not accepted native sprite coverage. Capture any real patch encountered; full growth/product/utility art needs a subsequent dedicated slice, not invented “ripe plants” inside this kit.

## Ordinary capture candidates and routes

Select from the **actual saved map first**, then use normal world-map movement and descent. Candidate selection below is a source calculation from `FormationSelector.For/StableIndex` (`Formation.cs:134–189,254–278`), not a generator override or a guarantee that a saved cell has no POI. Keep the current earned fixture before travel.

|Candidate|Expected authored ordinary formation|Useful reason to visit|
|---|---|---|
|`Overworld.4.7.0`|Grove|Core sign→seep→red source journey; close to other formation candidates.|
|`Overworld.4.8.0`|CompostingField|One world step south of the Grove; compare soft walkable rows and cache harvest/removal.|
|`Overworld.5.7.0`|FruitingWall|One world step east of the Grove; compare solid fruiting shelves with the non-solid forage source.|
|`Overworld.0.8.0`|TendrilFen|Four world steps west from4,8 through other Grovelands candidates; provides water-bank/NPC scene and the western region edge.|
|`Overworld.7.3.0`|TendrilFen, alternate|Closer to the northeastern grove chain if the western candidate has a saved POI or unsafe encounter.|
|`Overworld.5.6.0`|FruitingWall plus positive BloomFront address roll|Later essential-state counter scene, subject to actual shrine/POI veto. Do not choose it as the clean first baseline.|

From the archived packaged Beating location at world17,13, source-only map displacement to5,7 is12 west and6 north. From the opening world11,10 it is6 west and3 north. Those are navigation coordinates, not an assertion of safe travel, current save position, generation order or local arrival cell. Root must observe the actual map and command result, descend normally, capture the current owners/positions/HP and preserve failures honestly. No teleport, grant or forced formation is needed.

Nearby authored sites remain independent scope: Cinderhold6,6 is `PruningPost`; Morrowfast3,6 has its own village pipeline; Olderdeep4,6, Deepest Cathedral5,4 and Ginmere2,7 have separate surface/underground stacks. Doll grove1,6 is deliberately bare, forces Grove formation, uses the doll stamp and excludes ambient containers/BloomFront. Reusing these as “ordinary Grovelands” would erase authored meaning (`OverworldZoneManager.cs:801–813`; `SinkholeSites.cs:43–55`; `WorldMapAuthoring.cs:212–234`).

## Smallest cohesive first implementation

**A. Existing forest envelope plus14 proposed new static frames for the actual formation owners.** Final frame count remains provisional until a real full-view baseline and imagegen critique; do not generate an isolated asset plate before that baseline.

|Owner/family|Proposed new frames|Purpose and essential-state boundary|
|---|---:|---|
|MycelialColumn|2 quiet static shape variants|Pale uneven living columns, sparse violet veins; no invented motion. Destruction removes/reveals real underlying content.|
|GroveSeep|2|Available water and exhausted exposed-filament source. Stock state follows owned water pool, while freezing/mixing/coating cues retain real fallback. Empty does not falsely advertise refill or remaining drink.|
|GroveSign|1|Grown asymmetric wooden board; recognizably readable without copying generic road-sign geometry or baking text into pixels.|
|GroveRedGrowth|1|Low attached red growth with supporting living mass; related to, but visibly larger/different from, carried GroveRed. Harvest removes it.|
|FruitingBody|2 quiet static variants|Connected squat fungal shelves/fists, violet over ochre, distinct from trees/vine walls; solid footprint stays exact. No harvest-readiness claim.|
|CompostRow|3 low static variants|Sparse remnants in organic rows, no repetitive skulls or alternating bright tile carpet. Non-solid.|
|CompostCache|1|A small pocket/cloth remnant among the row, no guaranteed coins. Consuming it reveals the literal existing row.|
|ChoirIronVein|1|A dark ore deposit with a muted ringing-metal identity, separate from the newly carried ChoirIron sprite. Harvest removal only.|
|WineLeafSundew|1|Low dark wine-leaf sticky rosette, distinct from edible red growth. Static hazard identity, not a false hunger/charge animation.|

This scope makes all four ordinary formation geometries legible and includes the recurrent tier3 plant hazard. It is preferable to a single attractive ring surrounded by default glyphs. It does **not** finish actor bodies or every optional owner. Next immediately after the material pass: static ChoirTendril plus the actually observed Shambler/Mosshulk/Rotling family, then encountered shrine keepers/props/hazards. Do not call the biome complete between these steps.

### Immutable reuse candidates

- **Grass:** Sill-envelope frames20–23 provide quiet static grass ground (`FlatPixelSillEnvelopeCatalog.cs:109–114`). Borrow their pixels through a new exact Grovelands material adapter; do not widen the Sill catalog or relabel canonical Grass as a new blueprint. Confirm the actual whole-view palette before deciding any new grass is needed.
- **VineWall:** Sill-envelope frames0–15 already encode N1/E2/S4/W8 connectivity (`FlatPixelSillEnvelopeCatalog.cs:63–73`). The exact same owner semantics are the starting reuse candidate. Neighbor reads must be currently visible/current-owner only; a hidden wall must not leak into join shape.
- **Tree/Bush/DryBrush:** north-vegetation0 Tree and3 DryBrush; opening-base5 Bush (`FlatPixelOpeningArtCatalog.cs:479–492,668–669`). They are known exact identities, currently behind local routes, not globally admitted proof. Use narrow shared resource frames; do not redraw approved source to force new packet uniformity.
- **Water lane:** preserve actual tile-state water presentation and hazard/temperature overlays. Do not replace this with an invented `GroveRiver` owner or assume a water PNG is region admission. Confirm the current world painter's real outcome during the baseline.
- **Encountered PeatBog/BrinePool:** the approved Sodden28 and Beating34 material pixels may be reused only after their original volume/coating semantics and current owner/state guards are matched. This is optional observed hazard coverage, not permission to alias the entire Sodden/Beating catalogs globally.
- **Portable goods/equipment and player:** retain the current exact global policy and accepted art, including the portable harvest17 work after it lands. Mineral deposit/source versus portable item remains a different owner.

## Runtime seam and minimum tests

Follow the existing Beating adapter shape, not a new renderer/framework: injected optional textures plus the existing `FlatPixelFallbackCatalog`; precomputed candidate-address table from `GrovelandsCompositionPlan.IsWildernessZone`; current `WorldLocationContext.For(zone)` manager and exact `CachedZones` instance; current map dimensions/biome/POI and `LairStacks.HasSavedColumn` veto (`FlatPixelBeatingMaterialsCatalog.cs:24–67`). Do not call mutating world-generation/ledger APIs from a render query.

Resolve ground separately from the current selected visible foreground. Preserve owner registration, footprint, sole Render/Physics, resource-group fallback, disposed lifetime, and public examine authority. Reuse exact lower substrate only when compatible; the recent Sodden dry-overlay correction is especially relevant because MycelialColumn and FruitingBody inherit Terrain while Bush also has Terrain. A broad “any other Terrain blocks ground” check would create dark holes beneath legitimate foreground plants. Conversely, permanent water, crops, gas/liquids and unknown terrain must retain their meaningful ground/state.

Root-owned RED→GREEN minimum:

1. Current canonical Grove/field/fen/wall positives; paired foreign biome, dynamic POI, saved lair, special site, stale graph, malformed owner, unseen neighbor and missing injected resource counters. Do not count a37-address source matrix as37 actual visits.
2. Available/empty/contaminated/frozen GroveSeep must follow the real finite water state and preserve reader truth. No production gameplay change to make the art pass.
3. GroveRedGrowth→carried GroveRed and ChoirIronVein→ChoirIron harvest pairs; CompostCache removal→surviving CompostRow with failed yield allowed. Use existing real Harvestable/Inventory transaction path, not a visual toggle.
4. Current selected source, covered source, removed/destroyed source and status/reskin refusals; world full/dirty paint and exact public examine portrait. Protect native9 borrowed-source bytes and the new17 portable policy from fallback-route leaks.
5. One ordinary Grove sign/read/draw/harvest journey; matching full-view before/after in all four formations; real border or world-map travel, save/reload and bounded package smoke. Native tests establish semantics; those captures judge composition, contrast and travel legibility.

No per-frame texture extraction, LINQ, source rebuild, animation controller or new allocation-heavy catalog scan. Cache immutable address metadata once and tile/sprite objects once per catalog; inspect current receiving-world authority each query. Use deterministic coordinate variation with a quiet majority and no simulation RNG, then judge repetition in full views.

## Fullness and acceptance risks

- **Quiet openings are intentional:** the grove's central floor and broad approaches make the sign/seep meaningful. Fill the surrounding mass coherently, not the paths. Doll grove emptiness and Overwrit absence are authored meaning, not vacant content budgets.
- **Texture can defeat composition:** connected VineWall masses and compost bands need varied silhouettes, but high-frequency highlights or identical bright columns in every tile will reproduce the Sodden carpet/dune-stripe defects. Inspect a complete80×25 view at ordinary zoom before making density changes.
- **Layering is functional:** non-solid red/compost/cache and solid shelf/column/tree must remain distinguishable; an item or NPC can cover them. Cache removal must not erase its underlying row. A forest canopy may not conceal targeting or route openings beyond its cell.
- **Actual ordinary danger matters:** tier3 sundews and encounter groups can interrupt an earned journey. Plan short observed movement segments and normal supplies; no debug invulnerability or fake stocked inventory. If the first attempt dies, preserve the failure and use an earlier legitimate checkpoint.
- **Status and exceptional scenes cannot be repainted away:** Bloomed hosts, BloomingFruitingBody, mirror mucilage, other hazards, and crop states remain explicit missing/conditional families. Unknown/fogged content needs conservative fallback, not attractive generic greenery.
- **No current flat Grovelands baseline was inspected in this preflight.** Actual owner census, view-scale contrast, actor prominence, item route safety and chunk fullness remain unverified. The next concrete action is the earned ordinary four-area baseline, then imagegen/reference extraction and original native production. This document intentionally stops before those actions.
