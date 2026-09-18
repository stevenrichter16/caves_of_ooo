# Morrowfast first-ring native survey — source preparation, 2026-09-09

**Status:** read-only source survey; no Unity execution, generated-zone observation, repository edit, new art, or gameplay change. The ring is authorized in `Docs/BLENDER-VILLAGE-3D-PLAN.md:208`, but implementation follows the village gate. Root owns adoption and native verification. Paths below are repository-relative to `/Users/steven/caves-of-ooo`.

## Exact scope and correction table

The centre is **Morrowfast `Overworld.3.6.0`**, not `WorldMap.StartingZoneID` (that symbol still identifies Sill for shared settlement/population contracts). X increases east; Y increases south; each zone is 80×25 cells. The eight-neighbour table below is a source calculation, not a rendered observation.

| Direction | Zone ID | Native biome / tier | Elevation band | Actual generation route / selected formation | Named identity |
|---|---|---:|---|---|---|
| NW | Overworld.2.5.0 | Stump / 4 | Foothills | Stump wilderness / **CascadeGorge** | No fixed named POI |
| N | Overworld.3.5.0 | Stump / 5 | Slopes | **FellingSiteBuilder only**, bespoke authored scene | the Felling-Site |
| NE | Overworld.4.5.0 | Stump / 4 | Slopes | Stump wilderness / **ButtressRidge** | No fixed named POI |
| W | Overworld.2.6.0 | Grovelands / 3 | None | Grovelands wilderness / **CompostingField** | No fixed named POI |
| E | Overworld.4.6.0 | Grovelands / 4 | None | Grovelands **Grove** base, then **SinkholeMouthBuilder** | **Olderdeep**, FoundingVillage profile |
| SW | Overworld.2.7.0 | Grovelands / 3 | None | Grovelands **Grove** base, then **SinkholeMouthBuilder** | **Ginmere**, DrownedSima floor archetype |
| S | Overworld.3.7.0 | Grovelands / 3 | None | Grovelands wilderness / **TendrilFen** | No fixed named POI |
| SE | Overworld.4.7.0 | Grovelands / 3 | None | Grovelands wilderness / **Grove** | No fixed named POI |

All eight have **world-road=false, world-river=false**. This does not mean no local water or path. Native formations, hazards, grove seeps and Felling authored water supply those. Do not invent a continuation of Morrowfast's decorative lanes across neighbouring chunks before inspecting their actual edges.

Evidence: `Gameplay/World/Map/WorldMapAuthoring.cs:53–109,253–263` (under `Assets/Scripts/`), `StumpBands.cs:37–65`, `Gameplay/World/Generation/Formation.cs:138,178–217,254`, `Map/OverworldZoneManager.cs:33–140,232–309,554–644`. Formation values were independently calculated from the source FNV-1a selector; actual runtime collector must assert them again.

| Tempting premise | Correction / implication |
|---|---|
| Eight ordinary woodland chunks | Three Stump chunks, including bespoke Felling; five Grovelands chunks including two sinkhole mouths. |
| N uses ordinary slope formation/fauna | Felling POI routing wins before biome routing; its authored population is separate. |
| Olderdeep is a surface village | Its founding settlement is the sinkhole **floor at z=2**. Surface has the mouth plus native Grove recipe. Current surface tier4 becomes floor tier5. Older Lore/Geography's tier3 and design tier5-approach wording drift; do not move NPCs aboveground to satisfy it. |
| Sinkhole ellipse is an impassable void | The builder clears the interior and reserves it during generation; it creates no Void tag/part or fall/collision rule. Never give the art an impassable collider or fabricate lethal/pit-floor semantics. |
| Every river-looking native cell has a WaterPuddle | TendrilFen writes permanent water directly to `Zone.TileState`; SprayPool has no LiquidPoolPart; pool entities and tile coatings are distinct. |
| Same seed completely fixes all generated results | Zone RNG uses `WorldSeed ^ zoneID.GetHashCode()`; runtime hash implementation matters. Loadout/Trader also use independent default RNGs. Archive the actual dump and runtime/version, not only the seed. |
| Border openings already match across chunks | ConnectivityBuilder opens each zone edge independently. Transitions may relocate arrival by up to10 cells. Record both sides; do not turn inferred straight roads into claimed native routes. |
| Terrain elevation is a numeric cell heightmap | Current elevation is the Stump **band**, formation and ledge/ridge entities. No general per-cell elevation field was found in the inspected map/builder APIs. Visual heights remain presentation choices constrained by native walkability/occlusion. |
| Tier follows the new player start | Current tiers remain3–5 around the village. No threat rebalance is included in this art scope. |

Opportunistic lairs/camps cannot claim this ring in current source: all eight are Manhattan distance≤2 from authored Morrowfast; `WorldGenerator.PlacePOIs` registers authored sites first and rejects opportunistic sites closer than3 (`Generation/WorldGenerator.cs:63–157`). Bloom-front is absent for the three Grovelands wilderness IDs: source hashes modulo100 are W75, S47, SE98, while membership requires<8 (`Builders/BloomFrontBuilder.cs:67`). Sinkhole and Felling pipelines exclude it structurally. This is fixed source behaviour, not a promise about arbitrary future world-map edits.

## Per-chunk content constraints

### NW — CascadeGorge

Stump base is DesertBuilder configured with TepuiStone/TepuiWall and no cactus (`OverworldZoneManager.cs:572–617`), then CascadeGorge after connectivity. The formation places walkable DescentLedge terraces and up to10 SprayPool placements in the lower half, with seeded gorge position (`StumpFormationBuilder.cs:143–169`). Use petrified pink-grey stone, water/spray pockets, terraces and native gaps. No authored river crossing flag exists. Native entities decide which pools/ledges survived placement and connectivity repair.

### N — the Felling-Site

The only builder installs `FellingSceneDefinition` and its durable state/owners (`Builders/FellingSiteBuilder.cs`, `World/FellingSceneRuntime.cs:53`). Definition resource: `Assets/Resources/SceneArt/FellingSite/definition.json`, validated as a full2000-cell map. Native IDs include `felling-terrain:x:y`, `felling-owner:<componentId>`, six bare positions and seventh position `(40,8)`. Export the complete definition and actual owner availability, not just its raster art. It is a scene with mutable owners, harvested dressing and save-preserved removals.

`FellingScenePopulation.cs:32–74` attempts two GlasspaneFrog near `(22,2)` and `(23,8)`, and one YellowfootWayfarer near `(57,20)`, with bounded habitat fallback/skip. Dressing is two MushroomRing clusters near `(24,15)` and `(54,14)` and Tepuibone near `(55,23)`. Durable revision prevents replenishing collected/killed content. These passive animals are **not** the general hostile Stump pool. Do not fill the ritual circle with invented villagers/guards. Canon: six bare positions and an empty seventh, no plants in the scar; source supplements are peripheral habitat/dressing, not permission to grass the scar (`Docs/FELLING-WORLD-DESIGN.md:621–676`).

### NE — ButtressRidge

Same native Stump base;4–6 radiating GrainRidge spokes with periodic gaps, then slope TepuiboneVein seams (`StumpFormationBuilder.cs:97–116,174–199`). Band-level grain direction is east-west; the buttress formation is specifically radiating roots, not an interchangeable parallel Grainfield. Repair can remove blocking placements; mesh binding follows the resulting entities. No metre-height cliffs or new traversal rules are encoded.

### W — CompostingField

JungleBuilder base: VineWall, Grass, Tree and Bush; configured SeedChance50/TreeChance0.16 (`JungleBuilder.cs:16–66`, `OverworldZoneManager.cs:554`). CompostingField lays seeded spaced CompostRow runs and2–4 attempted CompostCache placements (`GrovelandsFormationBuilder.cs:304+`). These caches have real contents/interactions. They should read as ordered half-consumed remains in dark loam, not generic farm beds or fabricated loot.

### E — Olderdeep mouth

Native Grove generation runs first, then an ellipse centred approximately `(40,12)` with radius13×6 is cleared/reserved and rimmed, with an east opening and StairsDown near `(52,12)` (`SinkholeMouthBuilder.cs:30–127`). This can overwrite part of the earlier Grove. Draw the **result**, not both idealized recipes intact. Stairs connect to z1 descent; z2 is the named founding settlement. Do not include its sleeping patches/Rooted residents on the surface unless present in actual native entities. An ambient GroveShrine may independently place Choir NPCs outside reserved mouth cells.

### SW — Ginmere mouth

Same surface recipe ordering and geometry rules. The native archetype is DrownedSima at z2 (`SinkholeArchetypes.cs:37–66`). A seeded, optional Helmwood water passage can be created only when the relevant surface/floor states exist (`Map/HelmwoodPassages.cs:119+`); do not generate the basement merely to force an art feature in the first-ring dump. Dump current connection records and source pools/markers. No promise of a complete surface lake follows from the underground archetype.

### S — TendrilFen

Two sinusoidal water veins are authored as direct **permanent tile coatings**, banks with FruitingBody/GroveRedGrowth, a30% attempted ChoirIronVein, and1–3 attempted ChoirTendril residents after reachability repair (`GrovelandsFormationBuilder.cs:202–276`). The native braid is the layout source. Canon visual vocabulary is near-black loam, pale ochre growth, violet accents, living light; preserve useful sightlines and treat the iron vein as real harvestable temptation, not generic rock.

### SE — Grove

Native Grove: cleared elliptical centre, MycelialColumn ring attempts, central GroveSeep, red growth, scattered columns, and GroveSign at an east approach (`GrovelandsFormationBuilder.cs:68–196`). Keep the clean seep and its interaction surface accessible. Preserve the actual LightSourcePart-bearing columns and sign. The recipe's attempt counts are not guaranteed final object counts.

## Shared native builders and model coverage

Most wilderness/mouth pipelines retain Connectivity3000, optional CaveEntrance3500 (removed for sinkhole mouths), Landmark≈3800, Hazard3900, Population4000, Container/TradeStock4100, Haulable4200 (`OverworldZoneManager.cs:624–644`; execution is priority-sorted by `ZoneGenerationPipeline.cs:35–61`). Stump formation3100 runs after connectivity; Grovelands formation2500 before. Felling bypasses these shared ambient builders.

Hazards: Grovelands uses PeatBog, BrinePool, SteamVent, DryBrush; Stump uses BrinePool, TarSeep, AshBed, SteamVent, PeatBog. Conductive runs may add CopperPipe (`HazardTerrainBuilder.cs:77–143`). Haulables may add FallenBeam/HaulBarrel (Grovelands) or MillStone/StoneCoffer (Stump). Cave entrances/stairs, doors, chests, dropped gear, harvested outputs and corpses need fallback or reusable models too. Do not silently suppress native entities because a reference image omitted them.

### Finite creature/NPC blueprint coverage for ring generation

| Producer | Candidate blueprint IDs | State gates / honesty bounds |
|---|---|---|
| NW StumpFoothills | CascadeFather, GlasspaneFrog, YellowfootWayfarer, Wardline, MawToad | CascadeFather excluded by EcologyDamaged and only placed on SprayPool habitat; population/table attempts can fail habitat. |
| NE StumpSlopes | Wardline, YellowfootWayfarer, MawToad; SariSnake, SkySari | SariSnake/SkySari require UrquActive. Default fresh world excludes them; art must support later fresh generation under active flag. |
| N authored fauna | GlasspaneFrog, YellowfootWayfarer | Exactly the three placement attempts above; both passive. No Stump population builder. |
| Five Grovelands chunks | HelmwoodFrog, Shambler, Mosshulk, WineLeafSundew, Rotling, ChoirTendril | Table is GrovelandsTier3 for both tier3 andtier4. WineLeafSundew is a stationary snaring plant without Creature tag, so creature-only enumeration misses it. TendrilFen additionally attempts ChoirTendril residents. |
| Ambient Stump Cave stamps | CaveHermit, SnapjawWarlord, Snapjaw | Hermit hut and optional WarbandCamp. The implementation still borrows Cave stamps even though Stump canon says designed sequence, not garrison; report existing truth, do not quietly remove or invent them. |
| Ambient Grovelands shrine | Mogu, Grib, Nam, Sien, Sopp | Display names are respectively **Solm, Vurn, Amai, Isk, Ketch**. May be absent in the selected seed but remain valid spawn coverage. |

Sources: `Data/Tables/PopulationTable.cs:68–95,125–170,555–572`; `Builders/PopulationBuilder.cs`; `StumpFaunaHabitat.cs`; `Builders/LandmarkBuilder.cs:315–364,477–551`; `Objects.json` exact blueprint rows. Generic pools are per-entry min/max rolls, not a single mutually-exclusive draw. Do not label all of the above hostile: Brain.Passive, faction feelings and contextual hostility differ. CascadeFather, GlasspaneFrog, YellowfootWayfarer, Wardline, HelmwoodFrog have authored passive behaviour. Named Choir actors can trade/talk even when relations are hostile.

This list covers inspected **surface generation producers**, not every entity that a player can later carry, summon, spawn through an effect, or lead into these cells. Dynamic runtime art needs a truthful native fallback for unknown/new blueprint IDs. Do not require a complete new bespoke model for every possible carried item before the ring can be playable.

## Saved water — confirmed boundary, no repair in this survey

1. `Zone.cs:128–148` explicitly excludes GenReservedCells and TileState from saves. Its comment about short2–8turn state is stale for the **permanent** TendrilFen water writer.
2. `SaveSystem.cs:1114–1172` writes zone ambience/version plus cell exploration/interior/entity references, with no coating/residue/energy/cloud serialization. LoadCell directly appends references.
3. `Zone.RebuildEntityCellsFromCells` (`Zone.cs:443–468`) rebuilds membership/tag indexes but does not project pools. `ProjectPool` (`Zone.cs:484–500`) is called by AddEntity, not by this load rebuild.
4. `LiquidPoolPart` public LiquidId/Volume survives the entity graph. WaterPuddle and GroveSeep therefore remain real, drawable pool entities after load, even though their projected tile coatings do not. Neither blueprint has TileStateSource.
5. BrinePool, PeatBog and SteamVent do have TileStateSource; `ZoneTileStateSystem.OnPlayerTurnEnd:31–46` reasserts them before reactions. Their tile effects return at that later step. This is not a universal load restoration path.
6. TendrilFen's braid has no persistent source entity/part at those water-only cells. On actual save/load the coating-only braid is lost and has no inspected automatic regeneration path. A 3D renderer faithfully sampling loaded native state loses that water too; rehydrating it from the seed would hide/change native state and is outside presentation work.

Required separate control: generate S, record water-only cells, raw graph save/load, compare immediate loaded water-only and persistent-pool cells, then one explicit source-seed/turn resolution (with effects recorded). Do not conflate permanent pool visibility with permanent coating persistence. No Unity result is claimed here.

## Borders and traversal: what can be promised

`ZoneTransitionSystem.cs:43–79` maps cardinal crossings; diagonal corner overflow checks X first and does not directly enter a diagonal chunk. Source position east→targetx0, west→79, south→y0, north→24. `TransitionPlayer:91+` uses actual manager.GetZone and private FindPassableCell (`:281–329`), preferring exact arrival then radius1..10 parallel/inward search. Arrival eligibility uses IsPassable plus !ExcludeZoneArrival (`:236`); actual Cell.BlocksMovement additionally honors Physics.Solid (`Cell.cs:94–139`). Export **both** masks and mark a predicted valid-but-movement-blocked arrival instead of conflating them.

ConnectivityBuilder opens each edge separately from its RNG and joins local components (`Builders/ConnectivityBuilder.cs`). There is no cross-zone border mesh/road lock. The3×3 grid has12 unique shared cardinal borders /24 directed transitions. Every border dump should include both edge cell arrays, all contiguous open intervals, endpoint native objects/water and the actual arrival resolver's result for each usable source edge cell. A resolved arrival is not proof the route back returns to the same cell. Keep this distinction in acceptance tests.

## Concrete native extraction strategy / API contract

A source-only exporter draft accompanies this report. It is **batch-editor-only**, creates no scene or player, invokes no SaveGameService/preferences/runtime save-root API, and uses a fresh EntityFactory + OverworldZoneManager with seed729490642. It must not run inside the user's live Play session. Dedicated-process static initialization is deliberate; it is not described as a read-only action on an existing live world.

The export should:

- Load the actual shipped Resources blueprints, factions, loot, liquid/gas/material definitions and normal factory consumers before generation. Preserve default fresh world flags; archive registry resource hashes and runtime version. Record that Loadout/Trader default RNG can vary independently of WorldSeed.
- Generate centre then NW,N,NE,W,E,SW,S,SE via actual GetZone. Apply the exact fresh-start garden helper only to the centre if root wants **post-bootstrap garden** state; otherwise label it raw manager-generated centre. Do not manufacture or move actors.
- Assert all9 IDs, expected coords/biome/tier/POI routes and80×25cells. Expose the actual priority-sorted pipeline through a derived manager (read-only protected API). Do not duplicate a hand-authored substitute builder pipeline.
- Serialize every cell's IsSolid/IsWall/BlocksMovement/IsPassable/ExcludeZoneArrival/interior, exact ground occupants, native IDs/blueprints/part types, public scalar fields, render/physics/liquid data, all tile-state layers, authored owner identity/room metadata, GenReserved flag marked generation-only, and its connected component index.
- Serialize nested inventory/equipment/body references using distinct collector tokens, retaining native IDs separately (natural entities may have null IDs). Do not assign missing IDs, serialize an Entity by recursive JsonUtility, or mutate source references for display.
- Copy the native Felling/Morrowfast definitions to the output as source artefacts. Owner footprints and pixel/depth art coordinates are metadata, not gameplay elevation. Export named owner state, current cell and mutable/removal state.
- Record existing manager.GetConnectionSnapshot only; do not generate destination basements. Include StairsUp/Down and public fields so transition markers remain real.
- Produce exact per-zone SHA256 of the actual JSON and a separate layout fingerprint excluding GUID identity. The same seed is not a portable exact-layout guarantee. Generate machine-legible border pairing records using the **actual private arrival resolver via fail-loud reflection**, not a hand-copied approximation.
- Keep raw PNG/overhead schematic as a later presentation of this exported data, preserving80×25cells and full border context. The accepted37.5×25.5 central village framing is not permission to distort80cells into a3:2 image. Native GPU screenshots then corroborate the dump.

### Pre-art acceptance gates

1. Collector completes9 zones,2000 cells each, no failed generation, expected POIs, source assets hashed, explicit native seed/runtime/provenance; owner IDs and no invented pits.
2. Twelve border pairs /24directions reported, at least one viable route for each claimed traversable adjacency. Failed or Physics-blocked arrival is reported honestly for a separate gameplay decision.
3. Exact blueprint frequency/inventory report reviewed against candidate table and dynamic fallback; no assumed universal hostile list.
4. Save/load water control above, owner deletion/harvest and equipment identity persistence; presentation follows loaded state and never redraws harvested entities from the initial dump.
5. Only then one reference image per chunk using its **actual** exported snapshot and canon; reuse scale/material/lighting and border ledger. Procedural chunks require per-cell/per-entity mesh assembly for other seeds/saves, not a single baked scene masquerading as all native layouts.
6. Runtime integration extends current `Village3DPresenter.Bind` Morrowfast-only gate (`Presentation/Rendering/Village3DPresenter.cs:71–76`) explicitly. Existing ClaimsCell claims whole viewport and IsRenderedEntity suppresses mapped entities/Morrowfast terrain (`:327–331`); extending those without a complete ring fallback can hide real content. Use exact current entity references and native FOV/light; no new collision/navigation simulation.

## Not performed / no implied completion

No native world dump exists yet from this task; no ring image, model, scene or runtime adapter is implemented. No generated placement count, border crossing success, save roundtrip result, visual quality or performance claim is made. The source-confirmed water persistence mismatch and current tier pressure are reported, not repaired or silently worked around.
