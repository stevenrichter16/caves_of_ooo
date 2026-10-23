# North connector and Nella: native parts proposal

Status: **planning only; no native art or runtime integration implemented by this packet**. Read-only census/source audit, 2026-10-23. The current bounded stage is exact scope, ordinary playable greybox, actual capture and imagegen concept review. Native asset packets follow that evidence. The list below is not a claim that two admitted addresses or a resident portrait complete a region.

## Evidence and limits

- Actual detached census: `Docs/Verification/Live16x24/connected-two-seed-census.json.gz`, SHA256 `9fd153f5817892537ed16eae02a9f4c4729ff7a70b0c8b9c44d75209ce8c4279`. Seeds64 and65, exploration version17, exact recorded generation order. Counts are diagnostic placed owners, not currently visible objects. Held inventory is recorded separately and must not become decorative spawns.
- Actual source: `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs`, `SpreadExplorationWorksites.cs`, `Assets/Resources/Content/Blueprints/Objects.json`; current native selectors in `FlatPixelOpeningArtCatalog.cs` and `FlatPixelGreyboxCatalog.cs`.
- State authorities: `CropPart`, `CropSystem`, `HarvestablePart`, `LocalGatheringClaimPart`, `LiquidPoolPart`, `FieldMedicinePart`, `BrewingService`. Existing `ConnectedSpread3DLibrary.ResolveReserveBed` and `PatchbearerArtLibrary.ResolveModel` provide semantic counterexamples, not permission to copy their per-query scans or 3D models.
- Scope/previous work: `Docs/LIVE-16X24-CONNECTED-SITES.md`, `Docs/LIVE-16X24-CELLAR.md`, `Docs/LIVE-16X24-ART-DIRECTION.md`.
- No new Unity call, gameplay action, spawn, capture, source-art change or Assets edit was made for this audit. Greybox captures and selected imagegen references for this packet are still the next gate.

## What is actually on the route

| Zone | Seed64 | Seed65 |
| --- | --- | --- |
| North connector `Overworld.11.9.0` | Tree32, Hedge124, Bush52; Magpie2, Viper1; still, stoneburr, frost lichen; enriched yard adds Mendleaf1, AlchemyShelf2, Patchbearer1 and11 stone walls. BerryBush, CopperPipe4, PeatBog8, Crate, StairsDown also present. | Tree34, Hedge165, Bush34; Magpie1, Viper2; basic still/stoneburr/frost lichen and3 stone walls. DryBrush3, FlowerField6, HollowStump, BerryBush, PeatBog, Sack, Signpost. No enriched drying yard. |
| Seed plot `Overworld.11.8.0` | Tree17, Bush10, Reeds4, CropRow142/RipeCropRow1; Magpie1, Scrabbler1. HaulBarrel, Sack2, RiverShrine, SteamVent2, PeatBog2, StairsDown. Nella at(19,9), public crops and reserve packet. | Tree33, Bush14, CropRow239/RipeCropRow2; Magpie1, Scrabbler1. CopperPipe5, BrinePool, Crate, FallenBeam. Nella at(22,9), public crops and reserve packet. |

Both plots also have BerryBush1 and2 signs. Each surface graph has2000 ordinary Grass owners. Trees/hedges/bushes dominate the route; admitting only the functional destination would leave most scenery and several real actors generic. Other seeds may differ again: this proposal covers the observed identities and the listed direct outcomes, not arbitrary generated content.

## Reuse before making new pixels

Fifteen named identities have required reusable art or ground treatment:

| Owner | Reuse and necessary routing work |
| --- | --- |
| `Grass` | Existing quiet dark-green ground. Never turn2000 grass owners into2000 foreground decorations. |
| `Bush` | Baseframe5 low growth, with a new exact ordinary-owner route; current art mapping requires the glade VisualID. No invented glade ID. |
| `Reeds` | Baseframes3/4, with a new exact ordinary non-solid vegetation route. Current mapping is glade-role-specific. |
| `CropRow`, `RipeCropRow` | Existing base stubble/grain, preserving owned `FieldHarvestPart.Harvested` for the latter. These field rows are not `CropPart` plants. |
| `StoneWall` | Accepted cellarframe8 continuous masonry, preserving current owned solid/non-takeable Wall semantics. |
| `Crate` | Accepted cellarframe1, one neutral container silhouette regardless of contents. |
| `StairsDown`, `Signpost`, `FallenBeam` | Existing accepted anchor frames and their exact current contracts. |
| `HaulBarrel` | The accepted banded barrel silhouette is visually suitable, but needs its own exact solid/owned Handling route. It is not a `WoodenBarrel` container; do not give it a lid/content state. |
| `MarlbackScrabbler` | Existing cast/motion and lifetime rules. |
| `SpreadSeedKeeper` | Bind the approved keeper PNG unchanged; details below. Do not reuse the generic Villager frame or move/duplicate Nella. |
| `Emberwheat`, `FireClay` | Existing loose-item art for public grain and processed marlroot clod. |

The existing player presentation also remains unchanged. Four already-supported items that can be carried/dropped or produced on this route are `HealingTonic`, `Mushroom`, `DriedMeat`, `MarlbackCorpse`; reuse their existing exact routes. Thus the explicit reuse ledger is15 required named identities plus4 supported ordinary outcomes/held items, not19 newly drawn frames. Other previously supported arbitrary drops retain their existing behavior.

The four newly covered crops may reuse the accepted cellar planted-seed mark for stage0. A buried seed does not need to advertise a fully grown species. Keep exact per-blueprint semantic admission despite the shared frame. This avoids four near-identical new seed-ground drawings while preserving the distinct loose seeds below.

## Proposed new art, split into reviewable groups

The minimum observed-route proposal is **45 new cells**:39 named blueprint identities plus one reserve-ground overlay role, with state variants accounting for the additional cells. Reused art above is excluded. This count deliberately includes seed65 surroundings and finite liquid states, rather than pretending the seed64 hero destination is the whole route. It can be delivered as small independent atlases after actual capture; it is not authorization to bypass the production pipeline.

### Ordinary surroundings —12 identities,14 cells

| Owner | Cells and silhouette contract |
| --- | --- |
| `Tree` | 1: visible rooted trunk beneath a compact broad crown. Solid obstacle; should not resemble passable low grass. |
| `Hedge` | 1: dense woven hawthorn/bramble mass, nearly cell-wide to read as a boundary. Preserve a quiet silhouette when repeated. Solid but not ordinary masonry. |
| `BerryBush` | 1: low leaves carrying a few distinct red berries. Owned unspent harvest yields `WildBerries`1–3; gathering removes the source, so no invented permanent spent bush. |
| `DryBrush` | 1: sparse dry forked stems, clearly unlike leafy hedge. No baked flame; actual thermal/state rendering owns fire. |
| `FlowerField` | 1: sparse petals/short flowers, not solid shrub. This real owner has `FlowerCharmPart`, `LifespanPart` and petal residue; no eternal decorative patch or readiness glow. |
| `HollowStump` | 1: squat solid broken trunk with a dark opening. Finite `GoldCoin` harvest removes it; do not draw exposed coins before the action. |
| `CopperPipe` | 1: short blunt copper segment, not a fence or wall. Real conductor material; electrical state remains an overlay. |
| `PeatBog` | 2: water-bearing peat and drained peat, same low footprint. Current owned `LiquidPoolPart` must identify water and actual positive/zero volume. Preserve heat/fire/marsh-gas behavior; no water painted after depletion. |
| `BrinePool` | 2: saline surface and empty mineral depression. Exact brine identity, positive/zero volume. Never borrow a fresh-drinking-water cue just because both sources also paint water coating. |
| `SteamVent` | 1: low warm vent opening. Do not bake permanent steam, wetness, charge or flame into the identity frame; actual source/tile/thermal state remains authoritative. |
| `RiverShrine` | 1: low worn stone with one carved token, non-solid; owned `SanctuaryPart`. No invented offering inventory or permanent aura. |
| `Sack` | 1: compact tied cloth silhouette, unlike a crate or seed kernel. Real container; no empty/full artwork absent an exact contents contract. |

Pools need source identity and existing ground/overlay priority reviewed together: today the generic flat classifier recognizes only water and would treat brine as unknown. Do not suppress charge/heat/gas just because custom source art now exists.

### Works and gathering sources —6 identities plus ground cue,7 cells

`AlchemyStill`1, `AlchemyShelf`1, `StoneburrPatch`1, `FrostLichenPatch`1, `MendleafPlant`1, `ConnectedReserveTray`1, reserve-soil cord overlay1.

- Still: compact receiving vessel and condenser coil. Owned `AlchemyStillPart`; no permanent brewing flame, output bottle or fake working/ready states. Brewing acts immediately and its results become real carried items.
- Shelf: solid broad wooden frame with visible compartments and circular wear marks. It blocks passage/sight in the authored yard. `ContainerPart` does not imply unlimited or visible stock; the observed shelves contain no held goods.
- Stoneburr: ochre hooked seedheads over leathery leaves; unspent `HarvestablePart`→`StoneburrSeed`.
- Frost lichen: pale cool crust close to the ground; unspent harvest→`FrostLichen`. Avoid making it a water source or quartz vein.
- Mendleaf: low silver-veined leaves; unspent harvest→`MendleafSprig`. No new weather-close animation without an actual state contract.
- Tray: shallow low tray with two cord loops. One neutral silhouette; the initial tray is empty and stays real when contents change. Do not paint harvested products into it automatically.
- Cord: persistent physical cue on the two exact reserve soil owners, readable beneath or beside the current crop and after removal. It is not a new entity and must not occupy the foreground crop slot. Public beds have no cord.

Reserve cue authority: ground property `SpreadExplorationResidents.ReserveBedKey` (`ConnectedSpread.ReserveBed`) links the saved keeper; then the actual owned `LocalGatheringClaimPart.Bound(zone)` must validate the world key/seed, current managed graph, exact keeper/soil/tray references and positions. `FirstSoil`/`SecondSoil` identify the two beds. Draw the physical cord independently of a player's permission status; do not color it red/green from hidden rights. Reject copied IDs/foreign or missing claims. Resolve the keeper with a bounded zone/mode cache or existing generation-aware index and use the existing cell pass; do not call the old3D resolver's whole-zone `SingleOrDefault` for every cell or frame.

### Planted crops —4 identities,6 new cells plus shared stage0

| Crop | Required state frames | Actual outcome |
| --- | --- | --- |
| `CandyCarrotCrop` (gladroot) | Shared planted mark0; new low leafy sprout1. **No standing ripe frame.** | `HarvestAtMaturity=false`; completion auto-releases2 `CandyCarrot` and removes crop. No seed yield. |
| `EmberwheatCrop` | Shared mark0; new narrow upright sprout1. **No standing ripe frame.** | Auto-releases2 `Emberwheat` and removes crop. Distinct from `RipeCropRow` field harvest. |
| `MarlrootCrop` | Shared mark0; new knuckled-leaf sprout1 and ripe root/clay shoulders2. | Mature hand harvest removes owner and releases2 `MarlrootClod` +1 `MarlrootSeed`. Clod prepares into real FireClay. |
| `PitchpodCrop` | Shared mark0; new paired-leaf sprout1 and papery pod/amber bead crown2. | Mature harvest removes owner and releases2 `PitchpodResin` +1 `PitchpodSeed`. Resin is a brewing reagent, not already-burning ammunition. |

Require exact owned `CropPart`, expected output identities and positive quantities, matching maturity lifecycle and valid stage. Do not pin mutable moisture, compost or stage duration. Seed64 begins marlroot0/pitchpod2;65 begins marlroot2/pitchpod0. The public four crops begin dry at0 in both seeds.

Important source correction: the builder adds `CultivatedSoilPart` only to the two reserve beds here. The four public plots sit on existing plantable ground; they are not automatically private or specially prepared. Crop moisture presentation already uses actual crop/ground state. Old prose naming a fixed number of moist rounds predates elapsed-world conversion; do not make a growth-timer promise in art.

### Actors —3 new identities,4 static cells

- `Magpie`1: compact dark/cream bird with long tail and two feet, clearly unlike Tatterjay or a humanoid. The real passive hoarder carries finite random goods; do not draw particular stolen equipment permanently.
- `Viper`1: low angular S-curve, raised head, no legs or baked poison cloud. Its actual combat/effects own the threat, not a perpetually active visual effect.
- `MarlbackPatchbearer`2: accepted Gleaner-family shale anatomy with pale repaired straps and an accessible cradle; healing bottle present versus absent. Preserve equipment-neutral hands. Use the existing public read-only `FieldMedicinePart.FindCarriedMedicine()` for the **healing-tonic slot**, as the accepted3D selector already does. The absent healing bottle does not mean the separate Antidote is gone. Do not label the entire harness “empty” or infer all medicine from this one slot. Actual consumption/transfer invalidation must update the frame without a new zone scan.

These are static identities, not a direction or motion claim. Native new-actor movement/reaction poses remain a later explicit motion packet; existing Scrabbler/player animation continues normally.

### Direct products and ordinary local outcomes —14 identities,14 cells

New compact loose art: `CandyCarrot`, `CandyCarrotSeed`, `EmberwheatSeed`, `MarlrootClod`, `MarlrootSeed`, `PitchpodResin`, `PitchpodSeed`, `StoneburrSeed`, `FrostLichen`, `MendleafSprig`, `WildBerries`, `GoldCoin`, `InertSludge`, `BrewedTonic`.

Use small actual items, not full plants or bundles implying counts. The first11 cover the four planted crops and three gathering sources plus berries; GoldCoin covers the real hollow-stump output, InertSludge the stocked compost/brewing failure result, and BrewedTonic the actual generic brewing output. `BrewingService.BrewBlueprintName` is `BrewedTonic`; there is no separate `BrewedCoating` blueprint. A neutral opaque flask must not promise healing or safe drinking: the real configured status/coating payload and description determine its use. Existing `Emberwheat`, `FireClay` and `HealingTonic` art is reused.

Keep existing owned Render/Physics/positive-stack guards. Semantic identity may serve authorized carried examine, but world placement still requires real current-cell ownership. No phantom duplicate of held stock, no harvest product drawn before a successful action, and no new independent quantity counter.

## Approved keeper and palette contract

Use `Assets/Art/SeedKeeper3/seed-keeper-3.png` byte-for-byte:16×24, SHA256 `ec3e10a85de884ec9611fa6f4d1cb9856e02d66c8ed8b540e9bd8ca20c8e2601`. Actual alpha bounds are(1,0)–(15,24), so it occupies row23 and the full24-pixel height. **Do not normalize it to the newer generated actors' row22 baseline, trim its margins, recolor it or recreate it from the earlier concept.** Its six opaque colors are `7AA153`, `53683E`, `F9C706`, `638144`, `97C768`, `FFFFFF`. Point/16PPU/centered tile use the original cell envelope.

New scenery should use accepted palette families from `ArtSource/Live16x24/{base,cast,anchors,states,items,cellar}/source-cells.json`: forest `53683E`, leaf `75834B`, dark leaf `435A35`; wood `94713D`/`5F4830`, linen `B5A47E`; stone `3C5148`/`526657`, mortar `233C32`; ochre `C99538`, rust `9B442B`; quiet water `196463` and cold mineral `70A68A` only where appropriate. Reserve the keeper's saturated hat yellow/white for its approved actor rather than flooding every plant with it. New objects use3–6 deliberate flat clusters, binary alpha, quiet soil and no painted light haze. Solid boundaries may fill cell edges; small walkable props/products must not imply blockage through oversized silhouettes.

## Explicit remaining gaps and acceptance

- Nella stocks `KnotflaxSeed`, `HearthbulbSeed`, `SeamleafSeed` in both seeds. They can be bought and planted, but their plants are **not initial census owners**. A complete merchant-to-harvest loop needs a further minimum12 cells: three distinct loose seeds, six sprout/ripe frames (shared stage0), and three harvested products (`KnotflaxCord`, `Hearthbulb`, `SeamleafSprig`). Cooking/processing secondary outputs then need their own source audit. Do not claim all of Nella's offerings are native from the45-cell route proposal.
- `WateringGrimoire` and arbitrary magpie/loot/equipment drops remain explicit fallback unless separately covered. The census contains real random held weapons, armor, tonics, reagents and manuals; they must remain usable/visible rather than silently suppressed. Viper corpse/gland and other death outcomes also need separate measured coverage before an “all drops” claim.
- No arbitrary Spread admission: validate the exact current managed north connector and plot, their world generation/version/POI expectations, with glade/cellar preserved and east/kitchen refused. Missing actors/services must not be recreated merely to meet this list.
- Before asset production: ordinary glade→north connector→plot capture at actual viewport; inspect the real vegetation density, actor scale, worksite and claim bed positions. Reinterpret those screenshots with imagegen, extract functional parts, reject unsupported detail, then author editable native geometry.
- For integration: tests first; independent optional atlases, no resource ownership leaks, exact sprite/frame resource binding, state/foreign-owner/visibility/carry counters. Check stationary crop growth, pool drain, finite medicine use, claim after harvest, expiry/removal and fullscreen return without a new frame-time scan.
- Actual revisit must demonstrate public versus cord-marked beds, watering/growth, product pickup, tray persistence and unchanged rights semantics. Native tests and two-seed census do not prove all-seed composition, controller feel, Steam Deck performance or release activation.

## Actual greybox and concept review —2026-10-23

This continuation supersedes the earlier pending-capture note. Independently viewed all six actual captures under `Docs/Verification/Live16x24/north-greybox-{entry,works,plot,reserve-terms,trade,trade-return}.png`, their saved metadata, and both `north-{plot,works}-concept.png` images plus exact prompt text in this folder. The actual seed64 walk reached the north connector and Nella, read reserve terms, bought two gladroot seeds, and returned from the square trade screen to the flat world. Entry/works show40HP; plot/terms/trade-return show25HP. The log records three stone-wall strikes, two successful patchbearer hits and a Calm escape. This is useful traversal evidence, not a clean or harmless hero scene. The parent reports212 native scope/consumer cases green; this art review did not rerun tests or operate Unity.

**Decision:** both concepts are useful direction references, not final assets or coordinates. The45-cell proposal remains a measured coverage ledger; no whole-route native-art completion is claimed. The extraction sheet must be reviewed separately before authoring.

| Observation | Keep | Correct before native production |
| --- | --- | --- |
| Actual connector paints tree, hedge and low vegetation with very similar small green symbols; concept gives them separate forms. | Dense hedge boundary, visible tree trunk/crown, lower bush. These materially improve route reading. | Concept foliage has too many shaded leaf fragments and some silhouettes extend across apparent cell boundaries. Reconstruct compact16×24 clusters,3–6 flat colors, without touching collision or moving owners. Solid hedge mass must not imply gaps; low bushes must remain visibly passable. |
| The actual worksite already contains11 solid masonry cells and real openings; the route struck stone walls while escaping. | Existing continuous solid-cell masonry and clear passage gaps are higher priority than decorative plants. | The concept replaces walls with beveled, mossy pillar chunks and edits their apparent shapes. Reuse accepted native StoneWall art and exact real cells. Do not import concept gaps, isometric caps, greenery on wall faces or a new layout. |
| Worksite still/shelves are distinct in concept. | Recognizable condenser/receiving vessel and two broad compartment shelves. | Simplify copper gradients to flat clusters. Shelves are real solid containers and currently empty: no bottle row, hanging herb stock, perpetual flame or bubbling output. Do not enlarge the still/shelves into adjacent walking cells. |
| Concept distinguishes three forage sources. | Hooked ochre stoneburr heads, silver-veined mendleaf, restrained cold color for frost lichen. | Frost lichen currently looks like a tall flowering bush; flatten it into a low crust. Reduce many stoneburr loops to a few readable native hooks. Do not interpret decorative leaves as additional harvest owners or retain a source after its real removal. |
| Plot concept preserves the broad two-reserve/four-public arrangement, quiet floor and an empty tray. | Thin reserve-cord corners and the shallow empty tray with matching loops; exact signs can reuse accepted art. | Read placement from the real metadata, not concept pixel distances. Reserve beds are(17,6)/(18,6), tray(21,7), Nella(19,9); public beds(17/18,8/10). Do not turn cord corners into solid posts/fences, make every public bed permanently cultivated, or keep a seed mark after the crop is removed. |
| Seed64's right reserve crop is ripe Pitchpod; concept makes two prominent gold flower-like heads. | Paired muted leaves, paper-pod shape and a small amber bead. | At16×24 it must read as pods, not an ornamental flower or grain head. No new blooms, ripe gladroot/emberwheat standing states, seed quantities or ready-action icons. The surrounding field stubble is existing `CropRow`, not newly planted public crop. |
| Concept replaces the approved keeper and player with larger illustrated derivatives. | General gold/green contrast helps identify the resident and player. | Reject both derivative actor drawings. Bind approved keeper PNG byte-for-byte with its original24-pixel envelope, and retain accepted player art. Do not recolor, rescale individual pixels, normalize baseline or create another keeper owner. |
| Concept magpie and viper have useful long-tail/low-S silhouettes; patchbearer has pale straps and a large held bottle. | Separate bird, snake and Marlback body classes. | Patchbearer is too humanoid and its hand-held oversized flask suggests permanent equipment. Derive from accepted Gleaner shale anatomy, retain equipment-neutral hands, and put a small actual healing bottle in its cradle only while `FindCarriedMedicine()` finds one. A missing healing bottle does not erase the separate Antidote. Concept animal positions are not live entity positions. |
| Both concepts have soft ground variation, many color values and irregularly sized illustrative “pixels.” | Quiet negative space and restrained material families. | No sampling/downscaling, baked shadow, gradient, grain, haze or noise. Native sprites must come from original exact integer-cell source geometry and be judged at actual1× and direct8× scale. |

### Production priorities after extraction approval

1. **Route and actor readability:** ordinary Tree/Hedge/Bush admission, current solid-wall reuse, Magpie/Viper/Patchbearer identity and real medicine-slot state. Match native occupied cells to actual obstruction; rewalk the same escape route without making the enemy weaker or removing walls for the picture.
2. **Nella's existing service:** bind the unchanged keeper; add the persistent validated reserve-soil cord, neutral tray and the exact four-crop lifecycle frames. Check public versus tied rows before and after harvest and a full trade roundtrip. The concept does not substitute for these state tests.
3. **Works and direct outcomes:** still, empty shelves, three distinct forage sources, then actual seeds/produce/reagent and neutral-brew items. Collect/brew or harvest/drop/examine through ordinary actions so no decorative supplies or stale source sprites survive.
4. **Measured surrounding variants:** finish the observed seed65/secondary hazards and containers, with positive/empty pools and existing state overlays. Retain the explicit stocked-seed and arbitrary-drop gaps above until separately covered. No native integration or release-wide readiness claim comes from a successful concept image.

This review changed only this source planning document. No native image, Blender source, runtime file, blueprint, owner state or Unity scene was modified.

### Extracted reference sheet review

Independently viewed `north-parts-reference.png` and `north-parts-prompt.txt` after the scene critique. The4×4 sheet is accepted **only as an isolated silhouette reference**, not a directly usable atlas. It contains the requested16 slots and removes the generated keeper/player derivatives. The present/absent healing-bottle pair, low tray, tree trunk, broad hedge, long-tailed bird and coiled snake are useful comparisons.

Production corrections remain mandatory:

- Visible red/green edge artifacts, many partial-alpha pixels, gradients and irregular tiny detail prevent direct slicing/import/downsampling. Read-only inspection found256 alpha values and far more than the intended3–6 opaque colors; author new integer-cell quads with binary alpha and the accepted palette. No extra imagegen call is needed merely to clean reference alpha that will not be imported.
- The hedge is much wider than a single16×24 cell. Rebuild one dense cell-sized module with compatible horizontal boundaries; retain all actual passable gaps. The tree crown also needs far fewer clusters.
- The “dry” brush has live green foliage. Use sparse dry brown/ochre forks. The lichen remains a raised, shaded mound; flatten it into a low crust. Stoneburr looks like perforated corn cobs and pitchpod like round gold flowers: replace detail with a few hooked burr heads versus angular paper pods and a small amber bead.
- The reserve overlay is a complete boxed railing with large corner knobs. Replace it with restrained1-pixel twine corners/short runs and small knots, transparent center, no solid-looking posts. It must read beneath the crop and remain after harvest without looking like an impassable fence.
- The patchbearer remains a rounded humanoid with a large flask. Rebuild from accepted angular Gleaner anatomy; both states must share exactly the same body/stance and differ only at the small healing-bottle cradle. Do not carry over the two generated bodies' small pose differences or treat the absence frame as an empty-antidote claim.
- Keep shelf compartments empty and the tray shallow. The extracted still's highlights/receiver size can be reduced while preserving its identifying condenser shape. Use one actual16×24 cell per owner; the impressive enlarged sheet is not a native-size readability proof.

The reference supports the priority order above. The first native packet should be judged at1× beside accepted base/cast/keeper art and in a repeated hedge/wall strip before import, then in the actual captured route—not accepted from this enlarged sheet alone.
