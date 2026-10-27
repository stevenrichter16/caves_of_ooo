# Next worldwide static sprite kits — independent source audit

2026-10-27. **Recommendation: ordinary Overwrit first, ordinary Stump second,
then the already observed Grovelands actor family.** This order gives the two
remaining authored surface biomes a coherent material identity before another
animation or small item polish pass. It does not declare any region complete.

This audit reads the coverage ledger, generators, blueprint inheritance and an
earned save. It adds no assets, production code, tests, entities or generation
rules; it runs no Unity session, native test or Git operation. Source-backed
candidates below still require root's actual entry, current-authority check,
before capture, concept/native extraction, integration and matched whole-view
playtest. No new visual acceptance is claimed here.

## Evidence and candidate authority

`ArtSource/Live16x24/world-sprite-coverage.json` records 23 Overwrit and 22 Stump
surface cells with no admitted regional native kit, alongside continuing shared
actor, settlement, underground and special-site gaps. The ordinary authoring
tables admit **22 Overwrit addresses and 18 Stump addresses**; these are scope
counts, not rendered/accepted scene counts. The Grovelands materials and bounded
Fen water work do not fill either biome's distinct identities.

Read-only decoding of the serialized header/world map in
`Fixtures/grovelands-water-crossed65/Quick.sav.gz` checked the save magic,
format 8, session checkpoints and all three world-seed fields (65). It found
active `Overworld.0.8.0`. The 3,554-byte prefix through the complete 20×20 map
and visited bitmap was consumed according to `SaveSystem.cs`; no game loading
or rewriting was performed. Compressed-file SHA-256:
`986ec412b56018ce2c5c1210d9d1fe7518efa67bcfb0006233c5a30ca2a03772`.

All ordinary candidates listed below have the expected serialized biome, null
POI and `Visited=false`. Thus they are genuine addresses in the earned world,
**not yet observed/generated acceptance scenes**. The map alone cannot prove
absence of a saved lair: the live adapter must also require the current cached
graph, current map/POI and read-only `LairStacks.HasSavedColumn` exclusion.
Do not use `LairStacks.Inspect` as a read-only classification substitute; it
initializes/adopts a ledger. Do not expand admission just because an address
shares a biome label.

## 1. Overwrit ordinary ground, clustered shoots and rim furniture

**Smallest cohesive first kit: four exact owners:** `OverwritGround`,
`OverwritNewGrowth`, `OverwritPilgrimBench`, `OverwritWaymarker`. Start immediately
south of the earned Fen, then inspect all four authored composition roles.

| Candidate | Source formation | Purpose of actual view |
| --- | --- | --- |
| `Overworld.0.9.0` | GreenRim, outward north | Immediate neighbor of earned Fen; show the vegetation edge and readable inward route. |
| `Overworld.0.10.0` | Blank | Judge the quiet ground as a whole scene; distinguish intentional open terrain from visually unfinished tiles. |
| `Overworld.3.10.0` | PilgrimageRim, outward east | Inspect inward-facing furniture if naturally generated; alternative candidates are 4,11; 4,12; 3,13; 3,14. |
| `Overworld.0.13.0` | DryRim, outward south | Show the dry regional boundary; alternatives 1,13 and 2,14. |

`OverwritCompositionPlan` chooses these roles from the authored neighboring
biomes, independently of seed; Spread neighbors take precedence, then
Grovelands, then Beating. Its source aims for 5–10 shoots in Blank and 18–32
in Rim, clustered in groups, with reserved three-cell approaches. These are
generator targets, not measured seed-65 counts. Pilgrimage furniture has a
35% attempt gate and placement constraints: neither a bench nor a marker is
guaranteed at the first candidate. Obtain the actual owner naturally before
claiming the complete furniture view. Do not inject one for a supposedly
ordinary comparison.

Visual objective: a restrained pale, smooth ground plane; low shoots with a
consistent cut height; a recognizably wooden inward-facing bench and a short
blank stone marker. Keep the edges/clusters legible and paths quiet. Repeated
high-contrast floor noise or arbitrary ruins would contradict this source.
The user's fullness requirement still applies: review normal gameplay views
and record whether the material transitions, clusters and focal objects make
the chunk feel intentional. If actual views remain too bare, name that defect
and propose a separately tested composition change rather than treating a
sprite sheet or the word “Blank” as acceptance.

Semantic cautions: the bench is solid/destructible Wood with Thermal, **not a
Restable service**. The marker is solid/destructible Stone with Rubble wreckage;
it is not a RegionalSignpost service or legible sign. Growth is non-solid
Plant with Destructible/Thermal, not a harvest crop. Ground has no Material
part; requiring one would reject every ordinary floor. Do not alias accepted
Grass, generic Signpost or a resting bench by appearance. Preserve burn,
damage, removal, ownership and reader meaning through native states or honest
fallback. Existing player/equipment/product art can continue unchanged.

Ordinary Overwrit uses its own composition/connectivity pipeline, not the
generic ambient-prop/population spine; only Rim receives the existing cave
entry. **Exclude `Overworld.2.11.0` (the Unsaying), underground ruins, bleeds and
other special content.** Its current serialized POI is null, demonstrating why
null POI alone is insufficient. The finite ordinary table still excludes it.

## 2. Stump petrified terrain, spray basins and summit plants

**One larger coherent family:** `TepuiStone`, `TepuiWall`, `GrainRidge`,
`DescentLedge`, `SprayPool`, `StoneDome`, `TankBrocchinia`, `TepuiboneVein`.
This replaces the shared material gaps across five distinctly composed
formations rather than making disconnected props. Root may stage the work
internally, but the final comparison must include foothill, slope and summit.

| Candidate | Band / exact source formation | Composition to preserve |
| --- | --- | --- |
| `Overworld.2.5.0` | Foothills / CascadeGorge | Unequal spray basins, outcrops and low ledges with a dry route around water. |
| `Overworld.2.2.0` | Slopes / Grainfield | Broad east–west petrified grain bands with meaningful gaps and vegetation pockets. |
| `Overworld.4.5.0` | Slopes / ButtressRidge | Tapered root fingers and sheltered pockets, distinct from the grainfield. |
| `Overworld.3.4.0` | Summit / SummitScrub | Irregular smooth domes, trees and sheltered tank-brocchinia. |
| `Overworld.2.3.0` | Summit / RimForest | Unequal tree/bush islands and an open crack; not a uniform forest carpet. |

These formation choices were reproduced directly from the authored bands and
the source FNV-1a selector; no random scene census is implied. `StumpCompositionPlan`
uses 2,000 TepuiStone base owners, then its formations and reserved approaches;
the current ordinary pipeline also retains real landmarks, hazards and habitat
population. Materials alone therefore cannot complete the chunk's actor/prop
coverage. Match these generators, not the older fallback scatter builder.

Use pink-grey **petrified wood grain**, smooth stone shoulders and low readable
ledges. Do not recolor Beating dunes or sandstone and call them TepuiStone.
GrainRidge and StoneDome are solid but have no Material/Destructible part;
DescentLedge is passable. TepuiStone's declared glyph variants are ordinary
identity, not necessarily custom mutation. TepuiWall inherits real wall,
destruction and thermal semantics. An over-general “all stone parts required”
predicate would silently leave major formations unskinned.

SprayPool is an actual `LiquidPool(water,40)`, not ownerless Fen water. Its
projection, finite/depleted liquid body and energy/status behavior need their
own bounded check before any approved water art is borrowed; the existing
strict ownerless-water adapter intentionally excludes it. TankBrocchinia has
Well but **no LiquidPool/finite stock**, so do not give it the depleted
GroveSeep contract. TepuiboneVein is solid and harvests 1–2 Tepuibone, with no
mining-service part. The already accepted portable **Tepuibone product** is
reusable unchanged after harvest; it is not the vein sprite.

Already approved exact identity reuse: `Tree` from vegetation frame 0 and
`Bush` from opening frame 5, with the same current-owner, selected/disclosed
state and optional-resource gates used by Grovelands. This is a new regional
routing gate, not permission to repaint modified trees or borrow every plant.
Retain player, equipment and shared product art. Do not expand existing
Grovelands region admission to obtain that reuse.

**Separate special-site work:** 2,4 Stillleaf/sinkhole, 3,3 Root, 3,5 Felling-Site
and 3,7 multi-cell pilot are not ordinary Stump composition. The save confirms
the first three POIs; 3,7 has null POI but is explicitly excluded by source.
Their machinery, architecture, multi-cell bodies and underground areas remain
separate coverage obligations.

## 3. Observed Grovelands actor bodies

After the two new biome envelopes, return to **Shambler, Mosshulk and
ChoirTendril**. The retained baseline census records four Shamblers across
4,7 Grove and 4,8 Compost, plus one Mosshulk and two ChoirTendrils in 0,8 Fen:
seven observed unskinned actors. These known earned areas provide a stronger
immediate target than an unobserved full creature registry. Rotling remains a
registered follow-up, not an observed claim. See `grovelands-next-gap-plan.md`.

Create exact static silhouettes; preserve essential reach/windup/status cues
and talk/trade behavior. `Shambler` and `SporeShambler` share a display phrase
but differ in blueprint, material, faction and mechanics. Neither has approved
exact native actor art to borrow. ChoirTendril is not a MycelialColumn or a
generic hostile plant. This is body coverage, not a new nuanced animation
system.

## Bounded next-session goals and limits

1. Enter earned-world 0,9 and 0,10 normally; record actual owner census,
   current graph/map/POI/lair authority and full visible before views. Continue
   to one actual furniture-bearing PilgrimageRim and one DryRim.
2. Produce and integrate the four-owner Overwrit family through the established
   RED → minimal adapter → GREEN → matched normal-input/restart pipeline.
   Preserve hidden-owner, floor/overlay, optional-resource and reader truth.
3. Capture all five Stump candidates before authoring that family; reuse only
   exact approved identities and keep the special-site exclusions explicit.

No new native execution, live candidate census, before/after visual judgement,
packaged run or physical Deck test was performed in this audit. No newly
visited/accepted address or global completion percentage is added. Root owns
current package delivery and the next actual gameplay session. The independent
review finds no source blocker to the proposed order; natural furniture
availability and current saved-lair ownership remain explicit live checks.

Source trail: `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md`;
`ArtSource/Live16x24/world-sprite-coverage.json`;
`Assets/Scripts/Gameplay/World/Generation/{OverwritCompositionPlan,StumpCompositionPlan,Formation,LairStacks}.cs`;
`Assets/Scripts/Gameplay/World/Generation/Builders/{OverwritCompositionBuilder,StumpCompositionBuilder}.cs`;
`Assets/Scripts/Gameplay/World/Map/{WorldMapAuthoring,StumpBands,OverworldZoneManager}.cs`;
`Assets/Resources/Content/Blueprints/Objects.json` and its inherited base parts;
`Assets/Scripts/Gameplay/Settlements/WellPart.cs`;
`Assets/Scripts/Gameplay/Save/SaveSystem.cs`;
the current `FlatPixelGrovelandsMaterialsCatalog` and `FlatPixelHarvestProductsCatalog`;
`grovelands-baseline-census.json` and `grovelands-next-gap-plan.md`.
