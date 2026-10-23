# Live 16×24 — connected sites

**Status: PARTIALLY IMPLEMENTED, 2026-10-23.** The actual two-seed census is complete and the glade’s connected cellar is integrated in the opt-in presentation; its precise evidence and remaining acceptance gates are in [the cellar record](LIVE-16X24-CELLAR.md). The four other proposed surface sites remain unadmitted and unimplemented. A release build still does not enable flat mode. [Live art direction](LIVE-16X24-ART-DIRECTION.md) owns overall implementation status and the required production pipeline.

**Current cellar acceptance:** nine native frames are imported, including dedicated solid masonry; native [714/714 cellar/opening tests](Verification/Live16x24/opening-cellar-green.xml) passed. The portrait color correction passed [9/9 GPU tests](Verification/Live16x24/opening-portrait-color-green.xml), the overlapping [348/348 regression](Verification/Live16x24/opening-portrait-color-regression.xml), and an [exact actual-popup palette comparison](Verification/Live16x24/cellar-native-portrait-palette-review.json). These are overlapping receipts, not a sum of unique tests.

The second ordinary keyboard journey completed finite looting, dismantling and harvesting, return upstairs, cellar revisit and F5/F6 save/load. Crate contents stayed zero; the pallet and ripe crop stayed removed; inventory retained two clay, two timber, two pulp, one seed, Ditchkeepers' Footwork and Shattered Rime. After the guard reduced health from 40 to 28, the player used Calm through the actual hotbar/target controls and returned to glade `(28,19)`, then descended to cellar `(40,12)`. Saving there, moving to `(40,11)` and loading restored `(40,12)`, 28 HP, flat presentation, inventory and depletion. Receipts: [supplies](Verification/Live16x24/cellar-native-supplies-final.json), [return](Verification/Live16x24/cellar-native-return-up.json), [revisit](Verification/Live16x24/cellar-native-revisit.json), [moved after save](Verification/Live16x24/cellar-native-saved-away.json), [reload](Verification/Live16x24/cellar-native-reloaded.json). The [cellar record](LIVE-16X24-CELLAR.md) preserves the failed first return and full acceptance details. Watered growth, the four surface art packets, full six-site journey, mouse/controller, physical Deck performance and normal release activation remain separate work.

## Goal and boundary

Carry the accepted flat 16×24 presentation from the real glade through ordinary travel to the existing seed plot, wayside kitchen and gleaners’ cellar. Keep their actual owners, mechanics, supplies, collision, generation and saved state. Do not move Nella or Orven to spawn, create replacement services, inject demonstration supplies, or complete jobs from rendering.

The reference is the user’s requested restrained Qud-like flat grid, implemented with original assets and the approved Seed Keeper/Cook artwork. No decompiled Qud implementation or new external reference was examined for this audit.

The smallest continuous envelope uses these **six exact addresses**:

| Address | Purpose | Admission/coverage observation |
| --- | --- | --- |
| `Overworld.11.10.0` | Existing glade | Preserve its existing separate `ReferenceGladePlan.IsActive` authority. |
| `Overworld.11.10.1` | Gleaners’ cellar | Existing shallow store below the actual glade stairs. |
| `Overworld.11.9.0` | North connector | Existing field alembic; can include the enriched drying yard. |
| `Overworld.11.8.0` | Nella’s seed plot | Existing connected `SeedKeepersPlot` destination. |
| `Overworld.12.10.0` | East connector | Existing tempering shelter. |
| `Overworld.12.11.0` | Orven’s kitchen | Existing connected `WaysideKitchen` destination. |

`Overworld.11.11.0`, the trapper yard, is deliberately outside this first envelope. The selected route to Orven goes east and then south. Other Spread chunks, towns, lairs and depths retain the prior presentation. This is not whole-biome coverage.

## Source sweep and corrections

| Assumption to avoid | Verified source and resulting constraint |
| --- | --- |
| The connecting chunks are empty travel lanes. | [SpreadExplorationPlan](../Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs) assigns `11.9` to `FieldAlembic` and `12.10` to `TemperingShelter`; [SpreadExplorationWorksites](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs) places services, forage, cover and conditional enemies. Include their real objects in the census. |
| Nella and Orven have fixed cell coordinates. | [SpreadExplorationResidents](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs) safely fits each site around seed-dependent candidate anchors. Addresses are fixed; owner cell positions are not. Inspect the generated graph instead of assuming `(22,12)` or `(58,12)`. |
| All crop types have the same ripe state. | `CandyCarrotCrop` (display name gladroot) and `EmberwheatCrop` use the older seed/sprout→produce lifecycle. Marlroot, pitchpod, claspbean and sootroot use `HarvestAtMaturity` and explicit stage 2. Follow each owned [CropPart](../Assets/Scripts/Gameplay/Farming/CropPart.cs); do not manufacture a stage-2 gladroot plant. |
| Orven’s oven is the glade’s finite fire. | The resident builder adds `CampfirePart { AllowRest=false, FiniteCooking=false }` to this exact `Oven`; the cot supplies rest. Do not apply the finite campfire’s cold/coals/burning contract to the oven. |
| Repairing the pan and repairing the well are freely compatible. | [Tier1Repairs](../Assets/Resources/Content/Data/Repairs/Tier1Repairs.json) charges two `FireClay` for either. The cellar supplies two total. Preserve this choice; do not restock for the visual demonstration. |
| A broad existing presentation scope is the new mode switch. | [SpreadPresentationScope](../Assets/Scripts/Gameplay/World/Generation/SpreadPresentationScope.cs) covers much more than these six zones. Reuse its current cached-graph/map checks only behind an exact allowlist; do not replace flat admission with unrestricted Spread admission. |
| Destroyed owners should revoke visual admission. | The world retains literal graphs after services or stairs are lost. Scope must not depend on a living keeper, remaining harvest, stocked crate or intact staircase. Rendering must not regenerate those owners. |
| Existing approved PNGs are already bound to the flat runtime catalog. | Keeper/Cook assets live under `Assets/Art`, while [FlatPixelOpeningArtCatalog](../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs) currently loads its accepted atlas groups from `Resources/Sprites/Live16x24`. An explicit serialized binding or byte-identical resource import is still required. |

The new site owners listed below have empty inherited `RenderPart.VisualID` values. The resident builder attaches `SpreadResident.Role` and `ConnectedSpread.Role`; the cellar uses `GleanersCellar.Role`. These properties are useful identity evidence, not permission to discover or mutate another owner. Use exact blueprints and owned parts; retain unfamiliar/reskinned-owner fallback.

## Owner and state inventory

### Nella — `Overworld.11.8.0`

- `SpreadSeedKeeper`: Nella Tern, conversation and `TraderPart` with `SeedKeeperStock`; the connected builder adds the actual `LocalGatheringClaimPart`.
- Public planting: two `CandyCarrotCrop`, two `EmberwheatCrop`, initially dry seeds; existing plantable ground and a `Signpost`.
- Reserve: `MarlrootCrop`, `PitchpodCrop`, `ConnectedReserveTray`, a second `Signpost`, and two actual ground owners with `CultivatedSoilPart`. One reserve crop starts ripe and the other dry according to the seed.
- [LocalGatheringClaimPart](../Assets/Scripts/Gameplay/World/LocalGatheringClaimPart.cs) binds the two soils and tray. Rights cost eight drams or a valid Orven introduction. The physical cord belongs to the validated soil owner and survives crop removal. Do not paint every crop as private or reveal permission/state through fog.
- Stock includes seeds and `WateringGrimoire`; carried goods are inventory content, not extra objects to place around the plot.

Approved actor pixels: [Seed Keeper 3](../Assets/Art/SeedKeeper3/seed-keeper-3.png), with existing [prefab](../Assets/Art/SeedKeeper3/SeedKeeper16x24.prefab). Preserve PNG bytes, original margins and colors; do not substitute the earlier keeper concept.

### Orven — `Overworld.12.11.0`

- `SpreadWaysideCook`: Orven Kett, conversation and `WaysideCookStock`; the connected builder adds `CookIntroductionPart`.
- `Oven`, `Bed`, `Chair`, two shelter `StoneWall` owners.
- `ConnectedBatchPan`, `ConnectedKitchenEscrow`, `ConnectedKitchenPickup`, and one initially ripe `ClaspbeanCrop` on prepared ground.
- [KitchenBatchPart](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs) owns the real work: two emberwheat, one claspbean pulp, two drams and 120 ticks. Read actual repair/batch/output ownership for cracked, repaired empty, working and ready pan states. Give escrow and pickup different silhouettes and empty/full states; never equate an arbitrary item with ready food.
- [CookIntroductionPart](../Assets/Scripts/Gameplay/World/CookIntroductionPart.cs) recognizes this player’s actual paid pan repair. The player must ask Orven, then explicitly relay his introduction to Nella. The renderer does not grant it.

Approved actor pixels: [Cook](../Assets/Art/Cook16x24/cook-16x24.png), with existing [prefab](../Assets/Art/Cook16x24/Cook16x24.prefab).

The existing [ConnectedSpread3DLibrary](../Assets/Scripts/Presentation/Rendering/ConnectedSpread3DLibrary.cs) contains useful read-only owner/state counterexamples, including validated ready output and persistent reserve soil. Reuse those semantic contracts, not its 3D art or overly broad presentation scope.

### Gleaners’ cellar — `Overworld.11.10.1`

[GleanersCellarBuilder](../Assets/Scripts/Gameplay/World/Generation/Builders/GleanersCellarBuilder.cs) builds actual `Floor`/`StoneWall`, `StairsUp`, `Signpost`, `FallenBeam`, `MarlbackScrabbler`, `Crate`; connected/fieldwork versions add two `SootrootCrop` beds and `GleanersTimberPallet`.

For **seed 64 only**, initial source-derived coordinates are: landing `(40,12)`, notice `(39,11)`, north service passage Y5, beam `(48,5)`, guard `(55,12)`, supply crate `(64,7)`, timber pallet `(65,8)`, ripe/dry sootroot `(65,9)`/`(65,10)`. The later detached census and ordinary journey checked these static placements; the guard can move during play. Other seeds can mirror the passage and move the store.

Accepted beam/sign/Scrabbler art is reused where its exact contract fits. The completed cellar packet adds distinct upward stairs, crate, pallet, sootroot stages and harvested products, plus solid masonry and ground-only floor treatment. It does not reuse the down-stair direction or imply a chest-open state absent from gameplay.

The crate contains two `FireClay`, `DitchkeepersFootwork`, and the seed-dependent `ShatteredRimeGrimoire`/`Buckler`. Dismantling the pallet yields two `SalvagedTimber` and removes that owner. Harvest removes the ripe crop and leaves its actual bed. Native art now covers the clay, timber, sootroot pulp/seed and supported Marlback corpse; unfamiliar manuals/equipment retain explicit fallback. Room art does not imply universal item coverage.

### Two connector chunks

- **North alembic:** `AlchemyStill`, `StoneburrPatch`, `FrostLichenPatch`, `StoneWall`; enriched admission can add `MendleafPlant`, two `AlchemyShelf` owners and `MarlbackPatchbearer`. Preserve the patchbearer’s finite supply rather than painting an inexhaustible medicine source.
- **East tempering shelter:** `TinkersForge`, `StoneburrPatch`, `StoneWall`; a source-admitted pair uses `MarlbackCindercaller` and `MarlbackScrabbler`. Existing ordinary sources remain when a bounded placement refuses.
- **Surrounding terrain/population:** [SpreadCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs) can use `Tree`, `Hedge`, `FlowerField`, `CharmFlowers`, grass, roads, bushes, reeds and crop rows. Normal population, containers, loose items and encounters remain seed-dependent. The authored additions above are not a complete live census.

Do not manufacture glade VisualIDs on ordinary terrain to force matching. Route deliberate new art by verified ordinary blueprint/parts within the admitted scope; leave unknown owners visible through fallback.

## Original staged plan — historical, 2026-10-23 before cellar implementation

This table records the initial six-site plan. Implementation deliberately admitted only glade+cellar first; Stage0's two-seed census and the bounded cellar packet are complete as summarized above. The four surface sites and continuous six-site journey remain planned, not enabled by their addresses appearing here.

Before each production change: read its actual APIs, write meaningful failing tests and confirm native RED. Follow the living art direction’s playable greybox → actual capture → imagegen reinterpretation → parts → native source → reintegration loop. Preserve all accepted source art and the previous presentation.

| Stage | Bounded deliverable | Required counters and acceptance |
| --- | --- | --- |
| 0 — census and scope | A shared read-only exact-six-zone scope for mode and catalog; native seed-64 owner census for each real generated graph, plus at least one different seed. No generation from rendering. | Same-address foreign graph, changed biome/POI, unsupported depth and neighboring zone remain excluded. Destroyed/harvested site owners do not change the admitted graph’s style. Record any refused worksite; do not insert it. |
| 1 — cellar | Up-stairs, crate, pallet, sootroot stages and underground terrain treatment; existing supported actors/props carry over. | Native down/up travel, both approaches, visible guard/action/fog, finite loot, harvesting/dismantling disappearance. Unknown drops stay visible. Save/revisit retains depletion; no new stock or owner. |
| 2 — north route and seed plot | North connector’s measured coverage, approved Nella, public/reserve crops and tray/cord treatment. | Public versus reserved distinction; actual stage/maturity contracts; dry/moist transition and elapsed growth on return; cord survives harvest. Missing/foreign claim does not advertise rights. Native trading and fullscreen return. |
| 3 — east route and kitchen | East connector’s measured coverage, approved Orven, oven/cot/chair, pan and distinct trays, claspbean stages. | Paid repair, actual working/ready/depleted states, interrupted/invalid job fallback, no phantom meal. Existing cook introduction and return to Nella work through ordinary actions. |
| 4 — continuous journey | One normal-input route through all six zones, revisits and presentation restoration outside scope. | Correct rectangular projection, camera/cursor/FX, square fullscreen UI and return, fog and removal, keyboard/controller, no duplicate bodies. Compare no-op/incremental rendering with the existing baseline; no added per-frame whole-zone census. |

Capture owner/state evidence during generation/tests or explicit diagnostics, never in a new frame-time scan. Keep atlas resources independently optional, cached and disposable; suppress a previous body only after a replacement is valid. Missing assets must not erase an owner or change its function. Admission alone is not a user-visible release setting: normal boot/controller mode selection remains the separate release activation work described in the main living doc.

## Normal-input journey

1. Start normally in the glade `(40,12)`. Inspect the cellar notice and walk to the actual stairs `(28,19)`. Use `>` (Shift+Period), or the controller Descend command; the existing stair auto-walk may refuse when threats are close.
2. In the cellar, read the tally and choose the broad guarded route or longer service route. Retrieve finite clay/manual/reward through ordinary actions; optional sootroot/pallet actions must remove their real owners. Return using `<` or controller Ascend.
3. Walk north across the glade border into `11.9`, then north into `11.8`. Find the actually placed Nella and inspect the public beds, cord-marked reserve and tray. Do not assume the site’s internal cell coordinates. No permission purchase is required merely to inspect.
4. Return south through `11.9` to the glade. Walk east into `12.10`, then south into `12.11`. Inspect Orven’s oven/cot and broken batch pan.
5. For this acceptance route, keep the cellar’s two clay for the pan rather than spending them on the well. Repair it through the real material action, then ask Orven to vouch for the repair. If testing a meal too, obtain the real ingredients/currency, start it normally and return after elapsed time; do not inject them.
6. Return north, west, north, north to Nella and relay the introduction. Revisit watered/harvested beds and the kitchen to verify actual growth, permission and work outcomes. Use ordinary Look (`L`, cursor, confirm), interaction/trade and inventory/controller commands throughout.

Normal border movement already calls `ZoneTransitionSystem.TransitionPlayer` from [InputHandler](../Assets/Scripts/Presentation/Input/InputHandler.cs); controller stair commands already route to `TryUseStairs`. Do not add a special art-tour teleport or a parallel input path.

## Original source-audit review — historical, 2026-10-23 before native census

The following statements describe the initial source-only checkpoint. They are preserved as audit history; current generation, cellar integration, journey and save/load evidence is summarized at the top and in [the cellar record](LIVE-16X24-CELLAR.md).

- **Verified by this audit:** source addresses, placement algorithms, owner blueprints/roles, crop/repair/job contracts, approved actor file paths and existing input routes. No source or asset was changed during the audit.
- **Not yet verified at that checkpoint:** actual owner census for all six seed-64 chunks, site placement success across seeds, integrated art, visual readability, the complete normal journey, controller acceptance, save/revisit presentation or Deck performance. Later evidence closes the bounded census/cellar portions, not every original gate.
- **Scope choice:** cellar first reduces the first art increment and directly extends the existing opening. North and east routes follow independently. Trapper yard, other regions and generic inventory art remain outside this document.
- **Self-review at that checkpoint:** 🟡 treating connector chunks as empty was corrected; 🟡 fixed resident cell assumptions were removed; 🟡 old versus mature-harvest crop contracts were separated; 🧪 census/travel/performance were still unperformed. No new gameplay mechanics or spawns were proposed.
- **Implementation log:** 2026-10-23 — source audit persisted only. Files changed: this new document. Tests: none run for this documentation-only addition; no Unity calls.

Future agents should update stage status, concrete changes, RED/GREEN evidence and actual capture links in the same commit as implementation, and summarize the accepted result in the root-owned live art direction. Tool choice is autonomous within the user’s authorization; coordinate shared Unity/editor ownership before any refresh, test or Play session.

## Original implementation packet — historical source-only census, 2026-10-23

This was a blueprint/state inventory from the builders and flat selectors at the source-only checkpoint, **not a generated six-zone census**. It was prepared without Unity, gameplay mutations or production edits while root completed opening acceptance. The reuse column reflects the then-current motion and twelve loose-item resources. Subsequent census results appear below; the implemented cellar now has nine dedicated frames and ordinary-floor ground treatment.

| Exact zone | Already reusable through current identity/state routes | New native art or deliberate new mapping required |
| --- | --- | --- |
| `11.10.0` glade | Existing base/cast/anchor/state/item groups and four animated actors. | No new initial-owner art for this packet. Keep unknown late drops explicit; this does not make arbitrary equipment covered. |
| `11.10.1` cellar | `Signpost`, `FallenBeam`, `MarlbackScrabbler` (including motion), loose `FireClay`, `SalvagedTimber`, valid Scrabbler corpse. | `StairsUp`, `Crate`, `GleanersTimberPallet`; `SootrootCrop` stages0/1/2. Ordinary `StoneWall` needs an intentional solid-masonry mapping and `Floor` needs ground-only treatment. Harvest exposes `SootrootPulp`/`SootrootSeed`; crate/guard equipment and manuals remain named fallback unless separately authored. |
| `11.9.0` north connector | Surviving ordinary Scrabbler/Gleaner actors, sign/container/items only where current exact routes apply. | `AlchemyStill`, `StoneburrPatch`, `FrostLichenPatch`, ordinary `StoneWall`; enriched yard additionally `MendleafPlant`, `AlchemyShelf`, `MarlbackPatchbearer`. Conditional enrichment must be measured, not assumed. |
| `11.8.0` seed plot | `Signpost`; loose `Emberwheat` already native. | Byte-identical approved `SpreadSeedKeeper` art binding; `CandyCarrotCrop` and `EmberwheatCrop` each stages0/1, `MarlrootCrop` and `PitchpodCrop` each stages0/1/2; `ConnectedReserveTray` and persistent claimed-soil cord cue. Their loose seeds/produce need their own item follow-up. |
| `12.10.0` east connector | Actual `MarlbackScrabbler` if the pair is placed. | `TinkersForge`, `StoneburrPatch`, ordinary `StoneWall`, and source-admitted `MarlbackCindercaller`. Ordinary background flowers/cover must also be counted. |
| `12.11.0` kitchen | Current generic `Signpost`/supported items only if actually present; common `StoneWall` can share the new deliberate masonry mapping. | Byte-identical approved `SpreadWaysideCook` art binding; `Oven`, `Bed`, `Chair`, pan cracked/empty/working/ready, distinct escrow and pickup empty/full frames, `ClaspbeanCrop` stages0/1/2. Ready art requires the real owned meal, not merely a state string. |

The four surface extensions retain [CreateSurfacePipeline](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs): landmarks, hazards, containers, population and haulable props still run around the bounded destination. [SpreadCompositionPlan.ObjectAt](../Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs) supplies ordinary `Tree`, `Hedge`, `FlowerField`, `CharmFlowers`, `Bush`, `Reeds` and `CropRow` according to formation/topology. Current base art deliberately requires glade VisualIDs for `Bush`/`Reeds`/`Rubble`/`Wall`; matching names do not imply coverage elsewhere. The first generated census must separate these ordinary surroundings from the destination packet. Current enabled exploration excludes these non-`None` family addresses from ordinary [BiomeCropPlan](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs) surface allocation; do not invent another five-species patch on these sites.

### Corrections to resolve before widening admission

- **Resolved original finding — Floor was a prop fallback.** At the source checkpoint, [FlatPixelGreyboxCatalog.Classify](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs) recognized `Grass`/`RoadStone`, but `Floor` fell through to `Prop` and `GroundOf` defaulted to surface green. The foundation packet below corrected this before the first cellar capture, retaining cultivated dry/wet priority and hidden-state restrictions.
- **Do not apply glade state art by analogy.** A supplied `Oven` is non-finite cooking scenery/service, not the finite `Campfire`; ordinary `StoneWall` has no glade VisualID. Reuse pixels only through an explicit correct owner contract. `StairsUp` must visibly differ from the accepted descending steps.
- **The reserve cord is a lower soil owner.** A crop occupies the same cell and normally wins the foreground. A new crop frame alone cannot communicate the persistent claim. Design the small validated soil cue in the existing cell pass, retaining it after harvest and suppressing live claim lookup in fog. The old 3D `ResolveReserveBed` performs a zone-wide keeper search; do not copy that into flat frame-time rendering.

### Original proposed greybox/capture batch — historical

This sequence is retained to explain the work's progression. Actual admission was narrowed to glade+cellar, and the six/eight-frame proposal became nine frames after visual review rejected the narrow surface ruin pillar as solid cellar masonry. Current implementation and evidence are in [the cellar record](LIVE-16X24-CELLAR.md).

1. Add exact-six-address read-only admission tests, including graph/map/POI/depth rejection and destroyed-site retention. Generate through `OverworldZoneManager.CreateDetached(factory, seed, true)` for seed64 and one odd seed (65 exercises the cellar's mirrored passage and Buckler reward). Request the surface glade before the cellar: `GleanersDistrict.CanBuildCellar` requires its actual stair and registered connection. Record realized/refused destination roles rather than force placement.
2. Record each generated zone's foreground owner counts, exact blueprint/VisualID/role, relevant owned state, catalog-selected group or explicit fallback, and contained/carried goods separately. This is a one-shot test/diagnostic export, never a renderer scan. The source table above is the checklist, not the result.
3. Enable the existing rough fallback only for the new admitted graphs, with the floor correction and current full-screen/projection/visibility guards. Make the **first real capture batch just glade→cellar**: arrival/up-stair, broad guard route, service route/beam, store/soil. Preserve stock and AI; navigate ordinary inputs. The other four addresses stay unaccepted greyboxes pending their own packet and capture.
4. Reinterpret those actual captures with imagegen, extract parts, then author the first cellar atlas: **six frames** (`StairsUp`, `Crate`, pallet, sootroot seed/sprout/ripe), optionally extending the same reviewed batch to **eight** with pulp/seed so harvesting has native results. Keep floor a ground treatment; review reuse of the accepted dark masonry for ordinary `StoneWall` rather than automatically making a duplicate wall asset. Crate has no saved lid-open state; pallet/ripe crop disappear through their real actions.
5. Confirm synthetic RED/GREEN, actual-resource missing RED/import/binding, stationary crop transitions, fog, removal, unknown-drop visibility and down/up return. Then capture the normal cellar journey and only afterward start the north connector/plot packet. New art counts do not close cross-seed layout, travel/save, performance or release gates.

**Source-only handoff at that time:** this supplement produced no native census, screenshot or integrated art. Stage0 plus the cellar greybox was the next task then; its completed bounded results now follow below and in the current cellar record.

## Cellar foundation packet — scope and floor, 2026-10-23

**Foundation checkpoint at 07:40 UTC:** native scope/floor GREEN, 31/31 passed with zero skips, before the first ordinary-input cellar capture. This packet admitted **only the opening glade and its current managed cellar**, as production still does. The four surface extensions keep their previous presentation until their own packets. This foundation itself added no atlas; the subsequent nine-frame integration and completed cellar journey are recorded above and in [the cellar record](LIVE-16X24-CELLAR.md).

### Verified contracts and corrections

| Source check | Result used here |
| --- | --- |
| `ReferenceGladePlan.IsActive` | Preserve its existing managed and standalone opening contracts. Do not broaden or rewrite opening authority. |
| `SpreadPresentationScope.IsActive` | Rechecks current graph identity, canonical address, valid map arrays and current Spread biome. Its broader committed-lair admission requires an additional no-surface-POI check for this exact cellar. |
| `GleanersDistrict.CanBuildCellar` | Generation eligibility depends on the surface stair/connection. Rendering must not rerun it: destroyed stairs, removed stores and harvested owners must not revoke an existing cellar's style or regenerate supplies. |
| `Floor` and existing selector | Ordinary owned non-solid, nonportable `Floor` was classified as a generic prop. It now contributes only cached ground, with a subdued cool green-gray `#182C29`; explicit reskins or malformed/foreign parts keep fallback semantics. |
| `GroundOf` and remembered terrain | Actual visible water and crop/cultivated-soil state retain priority. Memory reads static floor/wall geometry and dims it, excluding hidden live crop, liquid, actor and collision state. |
| Existing projection/fullscreen lifecycle | The shared catalog admission is already used by `ZoneRenderer.FlatPixelModeActive`. No second mode gate or camera algorithm is needed; the real renderer fixture checks rectangular cellar metrics and square fullscreen/out-of-scope restoration. |

The exact-subject floor portrait borrows the same cached solid sprite and stone tint as the world floor. Neither path adds a raster resource, terrain scan, frame allocation or gameplay mutation. The existing bounded cell-object traversal finds the floor beneath a higher foreground owner; roads retain priority over their substrate.

**Evidence:** `FlatPixelCellarScopeTests` native RED discovered 31 cases: 9 passed, 22 failed, 0 skipped in [opening-cellar-scope-floor-red.xml](Verification/Live16x24/opening-cellar-scope-floor-red.xml). An earlier stale assembly run discovered zero cases and was explicitly rejected as evidence. The observed failures cover missing cellar admission and its floor/grid consequences. Native [opening-cellar-scope-floor-green.xml](Verification/Live16x24/opening-cellar-scope-floor-green.xml) then passed all 31 cases with zero skips at 07:40:37–07:40:46 UTC. These tests stage authoritative cells and do not substitute for the real-generated census recorded below, ordinary down/up journey or visual art review.

**Foundation self-review at 07:40 UTC:** 🟡 six-zone admission was narrowed to this two-zone packet so unfinished neighboring art was not silently enabled; 🟡 generic Floor markers corrected before the first cellar capture; ⚪ old cellars retain their actual saved owners rather than receiving authored replacements; 🧪 ordinary Play, cross-seed census, visual readability and atlas integration were separate upcoming gates. Their later results do not change this checkpoint's limited scope. This is CoO-original presentation work, not a claim of Qud implementation parity.

**Files:** modified `FlatPixelGreyboxCatalog.cs` and scope comments in `ZoneRenderer.FlatPixel.cs`; added `FlatPixelCellarScopeTests.cs` and its Unity meta. Root owns native test execution, the main art-direction summary and the final commit.

## Actual two-seed census — 2026-10-23 07:41 UTC

The editor executed [census_connected_sites.cs](../Tools/Live16x24/census_connected_sites.cs) against detached managers for seeds **64 and 65**, generating the glade before its cellar and then the four surface destinations. All **12 zones generated** in **9,373 ms** with no failure or restoration errors. Both detached managers retained a null active zone; reported world, settlement, save-root and equipment-version cleanup checks were all true. Evidence: [execution receipt](Verification/Live16x24/connected-two-seed-census-execute.json), [structured summary](Verification/Live16x24/connected-two-seed-census-summary.json), [compressed raw census](Verification/Live16x24/connected-two-seed-census.json.gz). The decompressed raw SHA-256 matches the summary's `9fd153f5817892537ed16eae02a9f4c4729ff7a70b0c8b9c44d75209ce8c4279`.

| Generated zone | Placed entities, seed64 / seed65 | Actual flat admission |
| --- | --- | --- |
| Glade `11.10.0` | 2,681 / 2,679 | Yes |
| Cellar `11.10.1` | 2,008 / 2,008 | Yes |
| North connector `11.9.0` | 2,244 / 2,256 | No |
| Seed plot `11.8.0` | 2,196 / 2,309 | No |
| East connector `12.10.0` | 2,392 / 2,379 | No |
| Kitchen `12.11.0` | 2,289 / 2,239 | No |

Both cellars contain the actual upward stair, notice, beam, guard, crate, two cultivated sootroot beds and timber pallet. Seed65 mirrors the service passage: beam `(48,19)` and crate `(66,17)`, versus seed64 `(48,5)` and `(64,7)`. The stored goods are **two FireClay units**, one Ditchkeepers' Footwork, and Shattered Rime for seed64 or Buckler for seed65; the guard's equipped Cudgel/Hatchet is recorded separately, not as a loose floor item. No harvesting, dismantling, looting or replenishment occurred during this census.

Placement varies materially: seed64's north yard includes the Patchbearer, two shelves and Mendleaf; seed65 has the basic still/forage/walls without that enrichment. Conversely, the east forge's authored Cindercaller/Scrabbler pair appears in seed65 but not seed64. Both eastern chunks also contain a real pen gate and Reedback Grazer, which the earlier destination-only source list did not enumerate. Nella's reserve plot and Orven's pan/escrow/pickup packet were present in both seeds at different resident coordinates. These are observed placements, not guarantees for other seeds; nothing was inserted to fill an absent group.

**Bounds:** placed and potential-top counts include hidden entities and deliberately ignore player fog; held goods are separate custody records. This one-shot diagnostic constructs no renderer/catalog textures, player or live graph and performs no gameplay actions or saves. It verifies generation/ownership for two explicit seeded runs, **not** current player visibility, native art coverage, live-session RNG replay, visual quality, the ordinary journey or Deck performance. The four generated surface extensions remain unadmitted. The earlier source-only census limitation is closed for these two runs; capture and art acceptance remain separate gates.
