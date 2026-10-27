# Whole-world native sprite scope audit

Read-only source audit, 2026-10-27. Repository: `/Users/steven/caves-of-ooo`. Prepared for the user's new priority: fill missing/inconsistent native sprites throughout every biome and area before optional animation refinement. The original investigation performed no Unity execution, game mutation, Git operation or production-file edit; its later normalization adds only this document and the linked planning ledger. This report is preserved source-audit evidence, not a completed world census. The original read-only report is retained below with source inventory normalized into `ArtSource/Live16x24/world-sprite-coverage.json`.

## Main finding

The six opening graphs are a useful accepted anchor, but they are not the game's content boundary. The authored surface has **400 cells in six canon biomes**; the serialized biome enum has **10 live keys**. Towns, sinkhole stacks, ordinary depth bands, finite lairs, special sites, carried goods and dynamic state outcomes add distinct coverage dimensions.

There are two separate deficits:

1. **Existing approved art is not admitted everywhere its real owner can travel.** Nearly all native plants/items/fixtures are behind `FlatPixelGreyboxCatalog.IsAdmitted`, which admits only six opening graphs. Four portable actor identities are the global exception. LongSword, LeatherArmor, Hatchet and Cudgel already have native identities but can become glyphs outside those graphs. This is an integration gap, not a need to redesign those sprites.
2. **Many regional and common object identities have no native 16×24 mapping yet.** Legacy 16×16 environment PNGs or 3D sources are reusable references, not proof of flat runtime coverage. Exact remaining totals are unknown until identity/state and generated-owner inventories are joined.

Sources: `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:185`, `:819` onward; `FlatPixelOpeningArtCatalog.cs:246`; `FlatPixelIncidentalEquipmentCatalog.cs:14–66`; `EnvironmentSpriteRenderer.cs:1152` onward. The companion resolver audit informed the equipment-first slice recorded in `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md`; the original scratch report remains `/tmp/coo-world-sprite-code-audit.md`.

## Registry totals and what they mean

| Source universe | Grounded count | Important bound |
| --- | ---: | --- |
| `BiomeType` values | 10 | Six authored surface biomes plus four retained legacy keys; not 10 finished kits. |
| Authored surface coordinates | 400 | 20×20 table; POIs replace some ordinary wilderness pipelines. |
| Authored `WorldMapAuthoring.Places` settlements | 17 | Includes Sill and Morrowfast; does not include six sinkholes or Root/Felling. |
| Authored sinkhole mouths | 6 | Each requires mouth, descent and floor coverage; deeper generic underground is separate. |
| POI enum kinds | 7 | Village, Lair, MerchantCamp, RiverChunk, Sinkhole, FellingSite, Root. A registered kind is not proof of a fresh-world instance. |
| `Formation` enum | 27 non-None forms | Spread6, Sodden6, Beating6, Grovelands4, Stump5. Overwrit uses its own4-form enum. |
| `Objects.json` definitions | 712 unique names | Includes abstract-like bases, natural attacks, derived aliases and special content. Not 712 required distinct pictures or a completion denominator. |
| Definitions with inherited Creature tag / Brain part | 124 | Includes base/template identities; not a generated population count. |
| Definitions with inherited Terrain tag | 93 | Overlaps other categories; state-owned ground/clouds also exist outside this list. |
| Definitions with resolved Physics.Takeable=true | 299 | Includes templates and aliases; not all are ordinary loot or independently visible. |
| Tags: Item / MeleeWeapon / Armor / Grimoire | 286 / 37 / 22 / 34 | Inherited tag counts, overlapping and including base definitions. Do not use ArmorPart count for gear: actors inherit armor too. |
| Crop definitions / biome crop species | 40 / 35 |35 species are five for each of six canon biomes plus Cave; other crop families are separate. |
| Loot tables / alchemy rules / repair recipes / tinkering recipes |100 / 9 / 9 / 10 | Reachability sources for generated/held outcomes, not art inventories. |
| Liquid / gas definition files |26 / 9 | Tile-state clouds and residues add their own cases; nine gas entities do not cover all tile-state marks. |
| Literal distinct `BlueprintName` entries in `PopulationTable.cs` |95 | Source-string inventory only, including objects and equipment; excludes substitutions, dynamic builders, settlement inventories, loot and state gates. |

Counts were computed from current repository text/JSON without instantiating entities. Blueprint counts merge parent Parts/Tags as `BlueprintLoader.Bake` does (`Assets/Scripts/Data/Blueprints/BlueprintLoader.cs:135`). All712 inherit a Render part, which is exactly why a simple “has Render” count is not a useful work denominator. No runtime-generated variant or all-seed completeness claim follows.

## A. Surface biomes and their actual content families

Source: `Assets/Scripts/Gameplay/World/Map/WorldMap.cs:14`, `WorldMapAuthoring.cs:53`; dispatch `OverworldZoneManager.cs:207–282`, surface builders `:660–962`; formations `Generation/Formation.cs:17`; population `Assets/Scripts/Data/Tables/PopulationTable.cs`.

| Biome | Table cells | Forms / distinct scene obligations | Grounded common identities to inventory |
| --- | ---: | --- | --- |
| Spread |142|Hedgerow, FieldStrips, OldRoad, FlowerMeadow, Fallow, RiverMeadow; random exploration assignments and work sites|Grass, Tree, Bush, Hedge, Reeds, CharmFlowers, CropRow/RipeCropRow, RoadStone; Magpie, Viper, PetDog, MarlbackScrabbler/Gleaner/Tunnelguard, GiantSpider; BerryBush, Beehive, HollowStump, Signpost; common melee/armor drops.|
| Sodden |63|OpenMire, PeatCuts, ReedMaze, DrownedCopse, Causeway, BogFace; three-zone inhabited district|MirePool, PeatBank, Duckboard, DeadTree, Reeds, BogTakenBody; Bandfrog, GinFrog, Reedfrog, Greatdew, MawToad, Viper; working bog tools, body recovery and district service owners.|
| Beating |107|SaltPan, RuinField, DuneBelt, CaravanRoad, WindBarrens, BrineLens; two abandoned counters and Tenth Fire|SaltCrust, DuneCrest, Sand/SandstoneWall/SandstoneFloor, BrinePool, DryBrush, Cactus, Rock, Bones, PaleSaltVein; Scorpion, SunStriker, BrittleHound, GlassScorpion, DuneLurker; salt/travel structures and thirst goods.|
| Grovelands |43|Grove, TendrilFen, FruitingWall, CompostingField; Bloom-front and authored doll grove|MycelialColumn, GroveSeep, GroveSign, GroveRedGrowth, FruitingBody, VineWall, CompostRow/CompostCache, ChoirIronVein; GroveLanternMoth, HelmwoodFrog, WineLeafSundew, ChoirTendril, Rotling, Shambler, Mosshulk; Choir/fungal structures.|
| Overwrit |23|Blank plus GreenRim, DryRim, PilgrimageRim; special Unsaying coordinate separately|OverwritGround, OverwritNewGrowth, OverwritPilgrimBench, OverwritWaymarker, Rubble. Preserve deliberate sparse/blank identity rather than arbitrary clutter; actual whole-scene quality/fullness review still applies. Authored ordinary composition currently enumerates22 addresses and omits2,11. Do not turn the omitted coordinate into an invented shipped special site without checking its real routing.|
| Stump |22|Foothills, Slopes, Summit bands; Grainfield, CascadeGorge, ButtressRidge, SummitScrub, RimForest|TepuiStone/TepuiWall, GrainRidge, DescentLedge, SprayPool, StoneDome, TankBrocchinia, TepuiboneVein; CascadeFather, GlasspaneFrog, YellowfootWayfarer, Wardline, MawToad, SkySari, SariSnake, BrocchiniaSentinel, SummitSinger. Ecology/world flags change which fauna exist.|

The terrain names above are literal known blueprint references in the corresponding `*CompositionPlan`, `*CompositionBuilder` and `*FormationBuilder` sources. Some references are guards or legacy fallback content, not a promise that every listed object spawns in every zone. The population names are actual table entries; probability, tier, habitat and world flags still apply.

### Retained legacy keys

`Cave`, `Desert`, `Jungle`, `Ruins` remain serialized and routed (`WorldMap.cs:4–12`, `OverworldZoneManager.cs:340–355,620–656`). None occupies a fresh authored surface cell, but underground, legacy saves, tools and older content still use these recipes. They must remain readable and be explicitly tracked as compatibility coverage rather than erased from scope.

Representative identities from their actual population tables: CaveBat, CaveSlime, CaveBear, Glowmaw, Stalagmite; DesertBandit, SandWurm, Rock; JungleApe, CanopyStrangler, SporeShambler; RuinScavenger, SkeletalSentry, StoneGolem, BrassHusk, CharredHusk, PalimpsestEcho. Higher-tier items include Battleaxe, Mace, Spear, Claymore, Greatsword, Warhammer, Buckler/IronBuckler, ChainMail, Cloak/WardedCloak and IronshodBoots. These are source-backed candidates, not a claim all have been visually audited.

## B. Named areas, interiors and special routes

### All17 authored settlements

Source: `WorldMapAuthoring.cs:209–229`; pipeline/profile dispatch `OverworldZoneManager.cs:963` onward. Interiors are actual geometry/owners in these zone graphs, not assumed additional zone IDs.

| Settlement | Address | Faction | Profile |
| --- | --- | --- | --- |
|Morrowfast|Overworld.3.6.0|Stillcord|Morrowfast|
|Sill|Overworld.10.10.0|Villagers|plain village|
|Gantry|Overworld.7.8.0|Villagers|CrossroadsExchange|
|Tine|Overworld.13.7.0|Villagers|LakesideVillage|
|Cinderhold|Overworld.6.6.0|SaccharineConcord|PruningPost|
|Posy|Overworld.5.9.0|BowerFolk|plain village|
|Marrowstye|Overworld.12.12.0|PaleCuration|Intake|
|Quillhold|Overworld.14.9.0|Palimpsest|PrimaryArchive|
|Tally|Overworld.10.14.0|SaccharineConcord|CentralExchange|
|Wellmeet|Overworld.8.16.0|TentRight|TentCamp|
|the First Tent|Overworld.5.17.0|TentRight|TentCampFirst|
|the Salt-Vault|Overworld.15.15.0|PaleCuration|plain village|
|Sumphold|Overworld.15.6.0|Villagers|Boatyard|
|the Drowned Ledger|Overworld.17.5.0|Palimpsest|ExcavationCamp|
|Slip|Overworld.16.11.0|Villagers|plain village|
|the Last Counter|Overworld.18.18.0|SaccharineConcord|ConcordPost|
|the Quiet's Door|Overworld.16.1.0|CatacombFolk|plain village|

`WorldMap.StartingZoneID` still means Sill10,10, but ordinary fresh gameplay now prioritizes the reference glade11,10. Do not confuse the world constant with the current boot route.

Town kits need more than walls: Villager, Merchant, Farmer, Innkeeper, Warden, Weaponsmith, Quartermaster, Scribe, TentRightHost, SaltMaster, faction residents, their held/traded goods, shop counters/market stalls, beds/chairs/wells/ovens/forges, doors/gates, notices/readable objects and local service states. The exact list must come from each profile's actual owner/stock generation; a generic town portrait cannot certify all factions or named NPCs.

### All six sinkhole stacks

Source: `World/Map/SinkholeSites.cs:40`, `SinkholeArchetypes.cs:40–65`; actual routing `OverworldZoneManager.cs:144–165,365–565`.

| Mouth coordinate | Site | Specific lower-floor identity |
| --- | --- | --- |
|4,6|Olderdeep|Founding/stranded settlement; dedicated composition at depths0–2|
|5,4|the Deepest Cathedral|Choir cathedral; dedicated composition at depths0–2|
|12,3|Lampwell|Bioluminescent stranded/catacomb settlement|
|16,4|Spivenor|Spore-Wedded stranded settlement|
|2,7|Ginmere|Drowned Sima, nest, PrickleBrowGecko; dedicated composition at depths0–2|
|2,4|Stillleaf|Sealed library/archive; dedicated composition at depths0–2|

Mouth, descent, actual floor occupants, doors/seals and loot each need coverage. Generic deeper levels continue separately. Sinkhole archetypes number four, not the eight contemplated in old design prose. Do not claim all depths of a sinkhole share one approved picture.

### Other mandatory area classes

- **World-map zone**: the actual singleton `WorldMap` with a20×20 logical map in an80×25zone. Terrain/POI markers and fog must remain legible; cartographic symbols are a separate intentional presentation role, not automatically missing actor sprites. `WorldMap.cs:42–103`, `WorldMapZoneBuilder.cs`.
- **Root**: `Overworld.3.3.0` mouth plus `Overworld.3.3.1` chamber, TheRoot face and ending-route state. `Generation/Builders/RootSite.cs:19–25`; deeper zones use ordinary underground. **Felling-Site**:3,5,0 and its authored circle/participants/ending state, `FellingSiteBuilder.cs:12–14`.
- **Morrowfast**:3,6,0, already among17towns but its dedicated scene runtime and named resident roles need a distinct acceptance row. **Multi-cell pilot**:3,7,0, separate authored footprint/multi-cell source; preserve true dimensions/owner selection instead of compressing every body into one tile. Sources `World/MorrowfastSceneRuntime.cs`, `World/MultiCellPilotRuntime.cs`.
- **Six opening graphs**:11,10,0;11,10,1;11,9,0;11,8,0;12,10,0;12,11,0. Retain evidence as bounded accepted anchor rather than reset it.
- **Sodden district**:15,7,0 dressing shelter;16,7,0 cutbank crossing;17,7,0 abandoned peat works. `Generation/SoddenDistrictPlan.cs:11–34`.
- **Authored wilderness**: Tenth Fire2,19,0; doll grove1,6,0; reference glade11,10,0. Abandoned counters19,18,0 and19,19,0; repair/cultivation site2,6,0. Respect intended emptiness at fire/doll sites. `OverworldZoneManager.cs:714–834`, `RepairCultivationSite.cs:13`.
- **Seed-selected exploration**:19non-None `SpreadExplorationFamily` values plus None, including roads, collectors, field passages, heavy salvage, hunting cover, still/forge/trapper sites, seed plot/kitchen, wet crossing/heavy frame. Rare encounter pairs, wayhouse and older situations are additional overlays. `SpreadExplorationPlan.cs:13`, `SpreadRareEncounterPlan.cs`, `SpreadWayhousePlan.cs`, `SpreadWildernessSituationPlan.cs`.
- **Regional service/cargo overlays**: five explicit request routes: Morrowfast↔1,5; Cinderhold↔5,6; Gantry↔7,7; Sumphold↔16,6; Wellmeet↔8,17 (`World/RegionalSituations.cs:38–55`). `SecondExplorationSites.cs:17–20` names11destinations covering archives, smith/rental/guest services, Olderdeep descent, Ginmere rope landings, bog works/crossing, East pen and abandoned counter store. These are overlays, not11new independent biomes.
- **Lairs**: current fresh-world selection requests3–5lairs and2–3merchant camps, with bounded attempts, so counts are targets not guarantees. Both exclude Overwrit/Stump. Finite new lair columns run to depth1or2 for Spread/Sodden/Beating/Grovelands, while loaded legacy lairs retain literal worlds. Lair boss candidates include MarlbackWallkeeper, DesertProwler, JungleStalker; legacy Ruins uses AncientGuardian. `WorldGenerator.cs:110–157,183–201`, `Generation/LairStacks.cs:91–94,178`.
- **RiverChunk** is a supported POI pipeline, but the current fresh `PlacePOIs` implementation does not itself place that kind. Track registered/legacy/scenario support separately from ordinary fresh-world reachability; do not inflate ordinary area count.

## C. Ordinary underground scope

Six material bands are explicit in `Generation/Builders/SolidEarthBuilder.cs:42–50`: sandstone depth1–2; limestone3–4; shale5–6; slate7–8; quartzite9–10; obsidian11+. Need wall/floor/edge/stair/room identity for each, plus actual cave surfaces and hazards. Depth is not a finite list of400new levels: general stairs can continue downward; completion should cover the reachable generation archetypes and state families rather than promise visiting every possible depth.

`UndergroundLayoutPlan.cs:12,25–53` adds natural-vs-roomed layout: at depth3+ in ordinary Spread/Sodden/Beating columns, a25%stable selector may use a roomed layout with Rubble/Pillar/BrokenColumn. Authored columns, missing-content cases and other biomes retain their existing paths. `OverworldZoneManager.cs:582–613` adds landmarks, hazards, containers, encounter families, and one field-medicine replacement in ordinary Spread caves. Dungeon style is therefore material band × actual layout/site × occupants, not just “Cave complete.”

Common underground native work candidates include stairs, stalagmites, rubble/pillars, ore/vein/product pairs, containers/traps, cave fauna/marlback variants, corpses and dropped loot; deeper spellcaster, ice/stone/obsidian and skeleton identities need separate state truth. Existing Sootroot cellar acceptance does not certify any other underground material band or random cave.

## D. Plants, harvests and products: full biome-crop registry

Source: `Assets/Resources/Content/Data/Farming/BiomeCrops.json`. Each row below has a CropBlueprint, SeedBlueprint and YieldBlueprint in that file; the owner lifecycle and utility determine required pictures, not a fixed animation quota.

| Ecology | Five registered species |
| --- | --- |
|Spread|Claspbean, Pitchpod, Drawgourd, Wickrush, Marlroot|
|Sodden|Sumpsieve, Drowsebell, Chillcress, Slipsedge, Peatlantern|
|Beating|Sunbladder, Shalebean, Shadefan, Cinderpea, Spurgrass|
|Grovelands|Choirwick, Knitmoss, Sourmantle, Murmurpod, Sealbark|
|Overwrit|Absentmint, Margincress, Binderroot, Greybladder, Hollowchime|
|Stump|Raingourd, Prismreed, ScarletSundew, Cloudwick, Gripfrond|
|Cave|Lampvein, Knucklecap, Sootroot, Veilpuff, Brinebutton|

Only five of these35species have exact references in the current native FlatPixel catalogs: Drawgourd, Claspbean, Marlroot, Pitchpod and Sootroot; those are still subject to local admission. This is a source-mapping observation, not five complete all-world species. The30remaining species have no exact crop-family FlatPixel mapping; their sprites/legacy resources may exist elsewhere and must be audited before authoring. CandyCarrot/Emberwheat and purchased Knotflax/Hearthbulb/Seamleaf are additional content families outside this35-row biome registry.

`World/BiomeCropPlan.cs:24–68` selects up to10surface sites per Spread/Sodden/Beating/Grovelands and up to5per Overwrit/Stump; selects up to16ordinary cave columns and maps species across depths1–5. These are candidate capacities, not guaranteed successful placements. Old saved graphs stay literal. Do not transplant/reseed live crops to manufacture a coverage screenshot.

Required state inventory per actual owner: seed/item, early growth, ripe/harvest availability, moisture/dry pause, harvested/spent/removal outcome, prepared bed if one survives, real yield identity and utility outcome. Existing geometry/stock/custody/saves must remain authoritative. Add botanical foraging plants, berry bushes, hive removal, mushroom/ore harvests and their products separately; they are not all CropPart owners.

## E. Cross-region object and essential-state families

These categories must be joined to reachability sources, not handled as an unrelated sprite pile:

- **Player/NPC/creature**:124raw Creature-tag definitions, faction/named variants, companions, enemies and bosses; alive/injured presentation only where real state requires it; death/corpse/remains and butchery results. Keep approved static identities and existing animations. Source predicates must not turn a dead or malformed owner into a living native actor.
- **Portable equipment**:37MeleeWeapon-tag and22Armor-tag definitions including templates; shields use actual part/tags rather than guessed ShieldPart. Common shared loose/carried/equipped/examine identity is the highest reuse opportunity. Broken/repair/temper/coating must not erase or falsely change base identity; semantic required states may use existing truthful labels/cues if art is intentionally neutral.
- **Knowledge/readable goods**:34Grimoire-tag definitions and13ReadableDocument-tag definitions; WateringGrimoire and DitchkeepersFootwork are demonstrated uncovered names. Spell/skill manuals, loaned copies, maps/notices, unique quest items and documents must remain distinct where their silhouette/identity matters.
- **Food/medicine/reagents/currency/materials**: food raw/cooked, finite tonics, brewed mixtures, seeds and products, ore/processed resource, repair components, weaponsmith supplies, quest cargo and containers.100loot tables plus actual carried/loadout/trade/cooking/brewing rules are the reachability inputs. A neutral approved flask is valid when its state contract deliberately conveys contents in the reader; do not redo it merely because two recipes share art.
- **Architecture/furniture/services**: closed/open/broken doors and gates, wall/floor families, storage, beds/chairs, markets, tents, benches/signs, stairs/ropes, forges/ovens/pans, stills/shelves, wells/draw points, shrines/altars, graves, lamps and field machinery. Native pose/state must follow exact owner roles rather than visual resemblance.
- **Heavy/multi-cell props**: FallenBeam, HaulBarrel, MillStone, SaltCuredBody, SmithAnvil, StoneCoffer (`HaulablePropBuilder.cs`) and the actual pilot footprints. Pushing/dragging/cover/removal cannot create decorative duplicate owners.
- **Hazards and tile-state cues**: AcidPool, BrinePool, OilSeep/OilSlick, TarSeep, PeatBog, IceSheet, FrostVent, SteamVent, AshBed, CopperPipe/RustedRailing, ConvalescencePool, MemoryBathPool, MirrorMucilagePool (`HazardTerrainBuilder.cs`). Track entity surface, liquid component/volume/mixed phase, coating, residue, heat/cold/charge and cloud separately.26liquid files and9gas files do not authorize a universal-water sprite; fresh water, brine, ice and mixed liquids remain semantically distinct.
- **UI-held counterparts**: inventory, equipment, dialogue, targeting, world Examine and authorized item reader must agree on identity. Source-native art present in one catalog does not guarantee all these contexts. Fullscreen UI raster/keyboard/mouse/controller and package delivery stay separate acceptance axes.

## F. Native, fallback and not-yet-audited status

| Status now | What can actually be said |
| --- | --- |
| Global flat projection + honest glyph fallback | Delivered across travel; this is visibility/identity continuity, not finished regional graphics. |
| Globally native approved actors | Exactly Player, PetDog, MarlbackScrabbler and MarlbackGleaner currently bypass the six-graph native boundary. |
| Native within the six-graph anchor | Approved opening/player cast, terrain/vegetation/masonry/water/soil, services, selected crops/products, magpie/viper, Patchbearer, Farmer/grazer/Cindercaller, LongSword/LeatherArmor/Hatchet/Cudgel and bounded aftermath mappings. Each has its own owner/state/visibility rules; this list is not exhaustive runtime acceptance. |
| Known missing integration | Existing approved portable item identity outside the six graphs; regional environments and ordinary NPCs do not inherit native art just because their blueprint matches an opening object. |
| Known uncovered identity examples | WateringGrimoire, DitchkeepersFootwork, common other equipment/held loot,30exact biome crop families, many regional faunal/terrain identities. Covered source strings/PNG names must still be checked before calling a family “no art exists.” |
| Not yet audited against actual current scenes | Remaining ordinary Spread formations; five other canon regional kits; towns and interiors; generic underground bands/layouts; sinkhole stacks; lairs/bosses; endgame/special sites; world-map style; random inventories and essential outcome states. Do not label this list “all missing”—some reusable assets exist, but their current16×24 integration and quality have not been verified. |

Resolver peer counted36PNG files in `Assets/Resources/Sprites/Live16x24`,35literal resource paths across20catalogs, and479legacy Environment PNGs (474at16×16, four atlases, one16×24). These are physical source/output counts, not native owner or region coverage percentages. The old 3D/environment path is bypassed by the flat renderer; conversion/reuse requires a named semantic match and a native-scale scene review.

## G. Evidence drift to fix in the ledger, without erasing history

`ArtSource/Live16x24/release-coverage.json` explicitly says `planning_status_ledger_not_runtime_manifest`, with six journey rows and `incomplete_not_exhaustively_censused_against_current_catalog`. Keep those honesty bounds.

Several old nested “open” texts are no longer reliable next-task selectors: cellar watered-Sootroot was later accepted through real packaged play; Patchbearer motion is now delivered; later Mac six-site service/crop/temper journeys extend beyond the older “Nella died before service” delivery row. These old records should be dated/superseded and linked to the new evidence rather than counted again as missing art. Conversely the ledger's own unknown exact unresolved identity/state count remains true.

Existing census tools are enough to start; avoid a new generic framework:

- `Tools/Live16x24/census_connected_sites.cs` and `connected-two-seed-census-summary.json`/`.json.gz` provide a historical detached authoring census of seed64/65, all owners and held goods. It deliberately includes hidden owners and ignores fog for potential-top counts, constructs no renderer and predates current art. Never treat it as actual visible coverage or current runtime mapping success.
- `Docs/Verification/Live16x24/current-scene-census-observer.cs` reads existing paint receipts, actual tilemaps and animated bodies for current visible on-screen cells. It deliberately marks covered owners as unassessed, reports unmatched receipts, and does not assess inventory/readers. Reuse it for observed scene gaps; pair it with ordinary screenshots.
- Source references in the report establish a finite registry/scope inventory, not an executed world sample. No new seed/region actual acceptance was earned in this audit.

## H. Minimal practical expansion of release-coverage

Keep historical `journey`, `packets`, delivery receipts and prior accepted assets. Add a `world_scope` section (or linked stable registry if size warrants), with these distinct row types:

1. **Area rows**: six surface biome keys with cell counts; four legacy keys; all17settlements; six sinkhole stacks with0/1/2roles; six underground material bands with natural/roomed applicability; finite special/district/lair/world-map classes above. Every row starts `not_audited` unless linked current evidence exists. Store generation source/profile/formation and applicable depth, not invented coordinates.
2. **Identity/state rows** keyed by actual blueprint + relevant VisualID/role/footprint + semantic state + presentation context. Do not key solely by PNG, display name or color. Record approved-resource path, native role guard, allowed region/context, whether the state is neutral/shared intentionally, and missing mapping vs missing asset separately.
3. **Evidence axes**, not one percent: source inventoried; resource authored/imported; runtime native route tested; actual world/reader screenshot; essential state transition; travel/revisit/save; packaged platform; unresolved quality. Record source revision and receipt seed/zone so old exact screenshots remain useful without becoming current proofs.
4. **Observed fallbacks**: exact owner/role/state, cell, current visible sprite provenance, priority/occlusion, screenshot. Keep unseen/covered/held-uninspected states unknown. Don't infer a missing corpse from a higher-layer raw-meat glyph over it.
5. **One actionable queue**: each slice names its real area/family, before capture, reused/art-needed parts, minimum semantic states and acceptance route. Optional new motion remains deferred. A source/resource count is never “biome finished.”

Start with a source inventory and a small representative actual route per new biome/form/layout, then expand only when its current owners reveal a gap. A representative route is sampling, not all-seed proof. Track unsupported/unseen states explicitly; do not try to render400zones in one editor burst or manufacture every state in the user's save. Native tests can cover ownership/fog/state permutations; ordinary play must still establish the actual scene.

## Recommended next delivery

**First: make the four already-approved portable equipment identities stay native across region travel.** LongSword/LeatherArmor/Hatchet/Cudgel are concrete, existing shared goods and use accepted pixels. Give them a narrow globally authorized item route, retaining current-graph, exact owner/custody, fog, visual override and missing-resource guards. Do not widen `IsAdmitted` to every area and accidentally grant green ground or role-specific scenery worldwide. Verify actual equipped/carried/drop/reader on a first outside-opening route and save/revisit. This closes a real inconsistency without another asset packet.

In parallel, prepare the first **regional scene kit** from a real nonopening scene, preferably the ordinary Spread→Sodden district route because it exposes new wet-ground, reed/duckboard/deadwood, frog/body/workplace families and existing service interactions. Capture and inventory that scene before choosing its related sprites. Retain the whole-world registry so this does not become another endless opening loop. Common equipment/manuals/food can be reused throughout the route; new regional parts must retain unique material identity. Town and underground queues should proceed from the same inventory, not wait for optional caster or idle-animation polish.

This recommendation deliberately preserves all current gameplay, generated layouts, scarcity, danger, saves and accepted art. No broad world-admission change or regional art completion is claimed by this report.


## I. Chunk composition/fullness gate added from user direction

The user additionally requires chunks not to feel too bare. The worldwide ledger
therefore records scene fullness as a required acceptance axis, separate from
whether each visible owner has a correct sprite. Review whole actual gameplay
views for layered terrain/material identity, coherent vegetation/rock/structure
clusters, landmarks, secondary props and transitions between chunks. Keep routes,
sightlines, hazards and interactable owners clear. Reject both accidental blank
expanses and arbitrary uniformly scattered clutter. Any functional layout change
must use real generation/owners and their tests; art may not invent collision,
loot, available services or hidden-state information.

The Overwrit's Blank and other authored quiet places remain intentionally sparse.
That identity is not an exemption from quality review: material, edges, landmarks,
route framing and the contrast with neighboring areas still need an explicit
actual-scene verdict. No universal density percentage or entity-count threshold
is prescribed; the acceptance question is whether the chunk feels deliberate,
readable and visually complete for its actual biome and gameplay.

### Normalization checks and limits

The ledger author independently reread the actual `BiomeType`,20surface rows,
17Places,6SinkholeSites,27Formation plus4OverwritFormation values,6underground
material bands and35BiomeCrops species. Source parsing confirmed400surface cells
and the recorded142/63/107/43/23/22biome distribution. It confirmed712unique raw
Objects definitions,26liquid JSON files, and5exact native crop-name mappings in
FlatPixel catalogs; these are source observations only. Other inherited-content
and reachability counts above retain their original source-auditor provenance.

The ledger's registered-source references exist in the repository. Its area rows
start not-yet-audited except the separately referenced bounded opening anchor;
no new runtime, visual, package, all-seed or whole-region acceptance was earned.
This normalization changed only the ledger and this preserved audit document.
