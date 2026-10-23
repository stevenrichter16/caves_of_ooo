# North connector and seed plot — measured owner contracts

Source audit/design only, 2026-10-23. No new scope, art, claim cue or runtime is implemented by this document. Parent plan: [LIVE-16X24-NORTH-ROUTE](LIVE-16X24-NORTH-ROUTE.md). Counts come from the committed [actual detached census](Verification/Live16x24/connected-two-seed-census.json.gz), not a prediction from population tables. Greybox travel, capture, imagegen and native authoring remain separate gates.

## Complete placed-owner census

Columns N64/N65 are `Overworld.11.9.0`; P64/P65 are `Overworld.11.8.0`. Counts are physical owners, including substrate and lower occupants; they are **not the number of foreground tiles or visible objects**. All 38 identities below have empty `RenderPart.VisualID` in this census. Role-bearing instances are described after the table.

Native means an existing exact semantic selector already supplies accepted pixels **once the new zone is admitted**. New means no current accepted selector; initial greybox must remain an explicit placeholder. Reusable candidates require a new deliberate selector and tests, not a fabricated glade VisualID.

| Blueprint | N64 | N65 | P64 | P65 | Current art / bounded need |
| --- | ---: | ---: | ---: | ---: | --- |
| AlchemyShelf | 2 | 0 | 0 | 0 | New solid compartment shelf; real container, no full/ready promise. |
| AlchemyStill | 1 | 1 | 0 | 0 | New still with owned `AlchemyStillPart`; no invented flame/active state. |
| BerryBush | 1 | 1 | 1 | 1 | New berry-bearing source; actual harvest/removal. |
| BrinePool | 0 | 0 | 0 | 1 | New brine-specific liquid cue; not fresh water. |
| Bush | 52 | 34 | 10 | 14 | Accepted base low-bush pixels are a candidate, but current selector requires glade VisualID. |
| CandyCarrotCrop | 0 | 0 | 2 | 2 | New gladroot seed/sprout; no standing ripe frame. |
| ConnectedReserveTray | 0 | 0 | 1 | 1 | New corded shallow tray; one physical frame. |
| CopperPipe | 4 | 0 | 0 | 5 | New low conduit; real conductive terrain, not loose salvage. |
| Crate | 1 | 0 | 0 | 1 | Native cellar frame1; owned container. |
| CropRow | 0 | 0 | 142 | 239 | Native base frame8; already-cut stubble, not growing crop. |
| DryBrush | 0 | 3 | 0 | 0 | New dry, readily combustible brush; not harvestable grain. |
| EmberwheatCrop | 0 | 0 | 2 | 2 | New seed/sprout; do not substitute ripe field-row art. |
| FallenBeam | 0 | 0 | 0 | 1 | Native anchor frame5; owned physical/handling parts. |
| FlowerField | 0 | 6 | 0 | 0 | New low conjured flowers; no produce/action implied. |
| FrostLichenPatch | 1 | 1 | 0 | 0 | New pale cold crust; actual `FrostLichen` harvest source. |
| Grass | 2000 | 2000 | 2000 | 2000 | Existing plain ground; two plot owners additionally bind reserve beds. |
| HaulBarrel | 0 | 0 | 1 | 0 | New handling barrel or deliberate reuse; **no ContainerPart**, unlike WoodenBarrel. |
| Hedge | 124 | 165 | 0 | 0 | New continuous solid hedgerow; highest-volume new foreground need. |
| HollowStump | 0 | 1 | 0 | 0 | New solid stump, real GoldCoin harvest; not a container. |
| Magpie | 2 | 1 | 1 | 1 | New bird; approved Tatterjay is a different blueprint/species, not a drop-in alias. |
| MarlbackPatchbearer | 1 | 0 | 0 | 0 | New distinct pale-strapped marlback; finite carried treatment supply. |
| MarlbackScrabbler | 0 | 0 | 1 | 1 | Native cast frame2 plus accepted compact motion. |
| MarlrootCrop | 0 | 0 | 1 | 1 | New seed/sprout/ripe rooted clay plant. |
| MendleafPlant | 1 | 0 | 0 | 0 | New silver-veined forage; actual sprig harvest. |
| PeatBog | 8 | 1 | 2 | 0 | New peat/water surface; wet and combustible, finite pool volume. |
| PitchpodCrop | 0 | 0 | 1 | 1 | New seed/sprout/ripe resin-pod plant. |
| Reeds | 0 | 0 | 4 | 0 | Accepted straw-reed pixels are a candidate; ordinary green reeds currently do not match the glade selector. |
| RipeCropRow | 0 | 0 | 1 | 2 | Native base7→8 using owned FieldHarvest.Harvested; no CropPart lifecycle. |
| RiverShrine | 0 | 0 | 1 | 0 | New low worn offering stone; not a stocked service. |
| Sack | 0 | 1 | 2 | 0 | New soft openable container, not a barrel or loose food. |
| Signpost | 0 | 1 | 2 | 2 | Native anchor frame8. |
| SpreadSeedKeeper | 0 | 0 | 1 | 1 | Explicit byte-identical approved Seed Keeper3 import/binding required. |
| StairsDown | 1 | 0 | 1 | 0 | Native anchor frame4; preserve direction and destination. |
| SteamVent | 0 | 0 | 2 | 0 | New hot vent; do not imply a drinkable water source. |
| StoneWall | 11 | 3 | 0 | 0 | Native solid cellar masonry frame8; exact Wall + solid/non-takeable physics. |
| StoneburrPatch | 1 | 1 | 0 | 0 | New hooked ochre seedheads; real StoneburrSeed source. |
| Tree | 32 | 34 | 17 | 33 | New solid trunk/canopy silhouette; ordinary destructible tree. |
| Viper | 1 | 2 | 0 | 0 | New low snake silhouette; not generic humanoid or Tatterjay. |
| **Total** | **2244** | **2256** | **2196** | **2309** | Includes 2000 ground owners per zone. |

There are no initial free loose-item owners in these four census rows. Inventory/container contents are not decoration to scatter around their holder. Unsupported later equipment and harvest products must remain explicit fallback until their separate native selectors exist.

### Real authored placements and stock

North64: still `(62,17)`; stoneburr `(60,18)`; lichen `(64,18)`; mendleaf `(55,19)`; shelves `(66,16)` and `(66,18)`; patchbearer `(68,17)`. Eleven walls carry `SpreadWorksite.Role=broken-wall`; source role does not itself establish a repair action or broken-state meter. North65 has still `(63,19)`, stoneburr `(62,17)`, lichen `(62,21)` and three walls. **Its enriched shelves, mendleaf and patchbearer do not exist.** Do not force enriched placement for screenshots.

Plot64: Nella `(19,9)`, public gladroot `(17,8)/(18,8)`, public emberwheat `(17,10)/(18,10)`, public sign `(21,9)`. Reserve marlroot `(17,6)` begins seed; pitchpod `(18,6)` begins ripe; tray `(21,7)`; reserve sign `(20,6)`. Plot65: Nella `(22,9)`, public gladroot `(20,8)/(21,8)`, public emberwheat `(20,10)/(21,10)`, public sign `(24,9)`. Reserve marlroot `(20,6)` begins ripe; pitchpod `(21,6)` begins seed; tray `(24,7)`; reserve sign `(23,6)`. Coordinates are evidence for those seeds, never runtime art-routing constants. Public crop/reserve/keeper roles are attached by `SpreadExplorationResidents`.

Nella's actual stock in each seed is InertSludge×2, CandyCarrotSeed×2, EmberwheatSeed×2, WateringGrimoire, KnotflaxSeed, HearthbulbSeed and SeamleafSeed. Do not gate her portrait on remaining stock, initial currency, passive brain state or hitpoints. Use exact Creature identity and current owned visible RenderPart, preserving explicit foreign-reskin fallback. Approved [Seed Keeper3](../Assets/Art/SeedKeeper3/seed-keeper-3.png) bytes, palette, margins and 16×24 dimensions are authoritative.

Other custody, aggregated per zone, separately from placed owners:

- North64: two Magpies carry PoisonTonic, HealingTonic, Mushroom, StrengthTonic, Dagger, CandyCarrotSeed, IronSpikeComponent, EmberFruit and Mace. Crate holds GoldCoin×4. Patchbearer carries HealingTonic and Antidote and equips Dagger.
- North65: Magpie carries LeatherGloves, CandyHeartRoot and SteelBladeComponent. Sack contains FireMoss.
- Plot64: Magpie carries StoneburrSeed; Scrabbler equips Cudgel. Two sacks together contain SparkRoot, FireMoss and two separate DriedMeat owners. Nella's stock is listed above; reserve tray starts empty.
- Plot65: Magpie carries KindleGrimoire, LampOil, Cudgel, IronSpikeComponent and Buckler. Scrabbler equips LeatherCap, LeatherGloves and Dagger. Crate holds GoldCoin×5, FireMoss and Hatchet. Nella's stock is listed above; reserve tray starts empty.

## Exact crop and tray contracts

Owned CropPart and owned visible RenderPart, exact blueprint, non-carried/non-equipped physical world owner, and empty VisualID are the intended semantic route. World selection and authorized subject portraits continue to enforce their respective contexts; no new global placement permission is inferred from a valid crop blueprint.

| Blueprint | Legal standing frames | Owned behavior to verify | Actual output |
| --- | --- | --- | --- |
| CandyCarrotCrop (gladroot) | 0 seed, 1 sprout | HarvestAtMaturity=false; YieldBlueprint=CandyCarrot; positive yield | Legacy maturity removes crop and drops produce. |
| EmberwheatCrop | 0 seed, 1 sprout | HarvestAtMaturity=false; YieldBlueprint=Emberwheat; positive yield | Legacy maturity removes crop and drops produce. |
| MarlrootCrop | 0 seed, 1 sprout, 2 ripe | HarvestAtMaturity=true; YieldBlueprint=MarlrootClod; SeedYieldBlueprint=MarlrootSeed; positive yields | Hand harvest removes crop, produces two clods and one seed in the authored definition. |
| PitchpodCrop | 0 seed, 1 sprout, 2 ripe | HarvestAtMaturity=true; YieldBlueprint=PitchpodResin; SeedYieldBlueprint=PitchpodSeed; positive yields | Hand harvest removes crop, produces two resin and one seed in the authored definition. |

Do not pin art to authored tick lengths or exact quantities; these can change without changing the silhouette contract. The actual CropTime model reconciles passing world time, not merely one growth tick per keypress. Stage2 remains ripe when dry. `MoistureTicks > 0` governs wet soil; it must not manufacture ripe/healthy states. Public crops initially have ordinary plantable Grass beneath them. Only the reserve's two actual ground owners gain CultivatedSoilPart and `ConnectedSpread.ReserveBed=<keeper ID>`.

`ConnectedReserveTray` has owned non-solid/non-takeable Physics and a ContainerPart with authored capacity8. It has no saved open-lid state, work-in-progress or ready-product field. One shallow corded frame is truthful whether empty or holding ordinary items. A visible cord never means the current player has rights.

## Persistent reserve cue — proposed indexed route

Source authority is [LocalGatheringClaimPart.Bound](../Assets/Scripts/Gameplay/World/LocalGatheringClaimPart.cs), not a crop name, display name or proximity. It validates exact keeper/soil/tray entity references and IDs, frozen soil/tray cells, current managed zone/world seed/key, owned parts and physical placement. Its `FirstSoil` and `SecondSoil` references survive crop removal. `Bound` does not require a friendly/live keeper or player permission; action availability adds those separately. Art must not call terms/pay/introduction/reconciliation or mutate permission records.

Use the existing maintained [Zone.GetReadOnlyEntitiesWithPart](../Assets/Scripts/Gameplay/World/Map/Zone.cs) index for LocalGatheringClaimPart. The first query seeds that type once; entry/exit and Entity.AddPart/RemovePart maintain membership. Seed it during zone binding/full setup, outside the cell loop. Current measured plot has exactly one claim owner; connector has none. **Do not call LocalGatheringClaims.ClaimFor or ConnectedSpread3DLibrary.ResolveReserveBed per cell:** both currently search the whole zone.

Smallest safe presentation shape:

1. A presentation-owned pass-level cache stores current valid bed owner/cell pairs, with its source claim references. It borrows owners; it never adds parts, changes tags, binds a claim or discovers another zone.
2. Reconcile against the maintained claim-owner list once before a render decision, including cached prior entries for retirement. Match owned claim, `claim.Bound(zone)`, exact soil reference and actual current cell; require that soil's reserve-marker value matches the current keeper ID. Do not derive ownership from the top foreground crop. Avoid per-cell Bound calls and per-frame full-zone searches.
3. Publish a read-only cue for those exact visible soil cells. It is the same physical cord for Unknown, Granted and Suspended. Keep it after harvest, clearing, pickup or replanting while the binding remains valid. It does not assert that the bed is currently plantable, watered or authorized; CultivatedSoilPart itself stores no moisture. No fake remembered claim lookup: unknown/remembered cells receive no current cue.
4. Add a separate under-foreground cue layer, or a bounded cached composite of the existing ground plus cord. Preserve the actual crop/loose item/player foreground and original GroundKind/color. Do not replace the crop with soil, overwrite an actual water coating, or tint the cord by permission. This layer must participate in release, pause, mode exit and fog cleanup; transparent pixels must retain the already-owned ground.

The exact layer implementation is still a design decision after greybox capture. A dedicated single cached tile on a small overlay is easier to keep palette-neutral than tinting one multicolor tile by the wet/dry background. Any composite alternative must have a finite ground-state cache and dispose its generated assets. No runtime texture readback/copy or per-cell sprite creation.

### Off-cell invalidation matters

Current EnvironmentSpriteRenderer only resolves dirty cells and neighbors; the existing single foreground result has no soil-cue field. Revalidating inside ResolveCell alone is insufficient: moving/removing the keeper or tray can invalidate a remote bed whose cell is not dirty. The cue reconciliation must run **before ZoneRenderer's `_fullDirty`/`_dirtyCells.Count` render decision**, compare old versus new valid pairs, and mark exact old/new bed cells dirty only when the result changes. Nothing requires whole-zone repainting on every actor move.

Membership version alone is insufficient: moving the tray preserves its part membership, replacing a part of the same type can preserve indexed membership, and edits to visibility, IDs, WorldKey or claimed coordinates need not change EntityVersion. A bounded revalidation of the one indexed claim plus previous entries handles those cases. If optimizing this further, a version/signature shortcut must cover those actual fields and retain fail-closed retirement; do not merely cache the first successful Bound result. `Bound` currently uses LINQ over at most64 permission records, so keep it once per pass/claim and profile before copying that work into every painted cell. A later allocation cleanup must retain gameplay authority and be independently tested.

The current physical claim validator permits a tied bed even if it is not presently usable for planting. The older 3D helper additionally calls cultivation readiness. Do not mistake readiness for ownership: a cord cue should advertise the exact bound reserve, while wet/dry/liquid/blocked state remains the existing ground/gameplay presentation.

## Grounding and foreground safeguards

Flat selection currently uses the highest visible RenderLayer in actual cell.Occupants; ties choose the later occupant. Portable owners receive current-ground/stack validation. The new native routes must retain owned RenderPart checks and exact semantic state. Remembered terrain excludes actors, items, crops, harvest sources, finite water, repairables and takeable owners. No new keeper, yield or moisture lookup through fog.

Current ground precedence is visible water coating → top crop's moisture → top field/cut row or actual usable cultivated soil → road/floor/default. Thus an actor above a public crop can currently hide that lower crop's moisture ground, and a dry cultivated reserve becomes dry-soil ground after crop removal. Keep the claim cue orthogonal to this precedence. If this packet fixes lower-crop moisture continuity, do so as a separate RED: query only that visible cell's actual owned crop, never the zone, and keep actual water coating priority. Do not claim that change is already implemented.

No ordinary empty-VisualID Bush/Reeds currently match accepted glade-native selectors. StoneWall now has a proper contiguous masonry cell; reusing the narrow glade pillar would create false gaps again. HaulBarrel cannot inherit a container/open-state promise from WoodenBarrel. Magpie cannot inherit the approved Tatterjay identity. The existing unknown/prop/actor placeholders remain honest fallback until each deliberate route is native.

## Tests and acceptance proposed before runtime

- Exact two north addresses plus current managed graph, Spread biome, no POI and canonical depth; reject same-name foreign graphs, changed maps and other surface/depth sites. Removing residents/crops/stairs never revokes zone admission or regenerates them.
- Nella exact owned visible Creature; byte-identical real resource; explicit reskin/foreign-part/carrying/fog counters; no dependency on stock, rights, display name or stale source coordinates.
- Four species' legal growth stages; reject stage2 public crops, negative/out-of-range stages, wrong yield/seed behavior and foreign CropPart. Watering/drying keeps growth frame while ground changes; maturity/harvest removes old owner and exposes actual output/soil.
- Reserve cue positive on both actual soil owners with and without a crop; absent on public beds, another grass owner at the same coordinates, forged marker-only ground, wrong world/manager/IDs, moved/removed soil/tray/keeper and detached claim parts. Restored valid graph can restore the cue without duplicating it.
- Stationary off-cell keeper/tray invalidation retires the two old cues; no unrelated cell redraw. Same-type part replacement, visibility change and world-key change do not leave stale cues. Indexed discovery must not enumerate all zone owners per painted cell or repeatedly seed the index.
- Granted/Unknown/Suspended all keep the same physical cue and leave permission/currency/stock/turn energy unchanged. No interaction or witness calls from rendering. Hidden/current memory does not reveal claim or crop state; pause/fullscreen and mode exit release all cue claims.
- Seed64 and65 source coverage: every realized placed identity is either deliberately native or explicitly listed fallback; held stock does not produce world sprites. Optional missing atlas falls back independently; disposing tiles/materials never destroys borrowed PNGs.
- Native source/contact checks and actual ordinary screenshots must follow the captured greybox→imagegen→native-art sequence. Native unit/GPU tests alone do not prove the route's composition, subtle cue contrast, harvesting/trade flow, save/revisit behavior or physical Steam Deck performance.

Next step remains root's real greybox route and concept preparation. This audit introduced no Assets, scene, gameplay, save, permission or resource mutations.
