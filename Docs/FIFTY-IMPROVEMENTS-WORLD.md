# Fifty improvements — world, fieldwork and finite supplies

Status: items **31–45 are implemented and verified** from the plans approved on 2026-10-05 against `bfa625f44`. Final source passed **831/831** affected checks, including all **112 world checks**, and the refreshed controlled native world benchmark passed **15/15**. The broad first run retained **15,848 passed / 41 failed** out of 15,889; its failures and bounded corrections are documented rather than recast as a complete broad rerun. Combat owns 1–15, clarity owns 16–30, meals own 46–50; root owns Unity, shared Objects.json changes and commits. These are original CoO improvements, not a claim of Qud parity.

## Source sweep and scope corrections

Current code is primary. The table records the pre-implementation source sweep against `bfa625f44`; its gaps explain the approved work, not missing behavior in the completed implementation. Historical planning prose is not proof that a feature remains absent.

| Evidence | Already works / actual seam | Consequence |
|---|---|---|
| `Farming/CropWateringService.cs:15–125`, `CropTime.cs:36–95` | Exact carried-vessel hand watering, elapsed world-time growth and rollback-aware tending already ship. | Do not count irrigation or absence growth as new. New work supplies adjacent planting, optional gathering and additional physical interactions. |
| `World/BiomeCropPlan.cs`, `Farming/BiomeCropCatalog.cs`, `Docs/BIOME-CROPS.md` |35 biome species and finite saved patches already exist. | No new crop roster, restock service or duplicate crop framework. |
| `Items/WaterVesselService.cs:35–46` | Waterskins already deplete finite pool volume. | Do not describe unlimited pool filling as a current defect. New transfers conserve existing units. |
| `Items/LiquidVesselService.cs:20–48,139–212` | Source-specific fill and directional pour already exist; pour moves all current volume. | Add a measured one-unit choice, retaining pour-all. |
| `Settlements/WellPart.cs:23–64` | Fault gating exists, but execution only checks actor nonnull and IsUsable, without current owner/reach/death checks. |31 closes this observed direct-service gap; it does not invent water repair. |
| `Settlements/CampfirePart.cs:37–79`, `RestSystem.cs:53–117` | Next-band rest checks reach; ordinary rest ignores TryRest's result, and direct ordinary rest can succeed without a physical actor/clock. |32 aligns actual campfire outcomes and safe context. Rest duration, healing and public/paid authorization stay unchanged. |
| `Farming/FieldHarvestPart.cs:25–66` vs `CropYieldService.cs:15–103` | Field grain stages directly in the world, sets stubble and packs without joining the outer receipt. It also measures anchor distance. Cultivated harvest already has a transaction. |33 repairs the older field-row path; do not rewrite working cultivated harvest. |
| `Farming/SeedPart.cs:43–62,85–136` | Carried seed offers one underfoot Plant command. Cultivated-soil/current-owner/source checks exist. |34 extends target choice through that contract rather than inventing free tilling. |
| `Farming/CropPart.cs:94–124`, `CropYieldService.cs:70–99` | Standing ripe crops harvest into ground produce plus returned seed. No deliberate young-crop clearing or optional pack destination. |35–36 are new choices; existing ground harvest remains. |
| `Items/TorchLightPart.cs:185–200`, `Materials/FuelPart.cs:13–42` | Torch ignition searches zone owners only; existing fuel consumption and controlled torch flame work. |42 adds real equipped light sources, not a new lighting engine. |
| `Items/CookingService.cs:12,102–136`, `World/Generation/SpreadExplorationCooking.cs:56–69` | Finite coals use actual remaining fuel and temperature. No feed/relight interaction exists. |41 uses timber and an actual ignition source; no infinite cooking station, free heat or new rest service. |
| `Farming/CultivatedSoilPart.cs:12–25` | Any physical pool makes a bed flooded/unusable for planting/tending. |44 captures eligible bed identity before its own pour, irrigates with one real unit and retires an exhausted poured owner. It never globally ignores flooding. |
| `Objects.json:WaterPuddle`, `Materials/ThermalPart.cs:75–89`, `Effects/Concrete/FrozenEffect.cs`, `LiquidVesselService.cs:228–235` | Natural WaterPuddle has Thermal; real cold creates Frozen and tile ice. Filling currently checks liquid/purity/reach, not frozen state. |45 uses existing cold/ice state. Poured pools without Thermal are not retrofitted with a new thermal simulation. |

Paths in this table are relative to `Assets/Scripts/Gameplay/` except named docs and Objects.json. Also read `CLAUDE.md`, `REPAIR-TIER-1-AND-CROPS.md`, `MATERIAL-REPAIR-ROADMAP.md`, `DENSITY-EVERYDAY-ENVIRONMENT.md`, `SPREAD-CONNECTED-EXPLORATION-DESIGN.md`, and `EXPLORATION-DEPTH-NEXT-DESIGN.md`. Their existing repair, crop, stove, public-service and persistence contracts remain boundaries. No new lore facts, NPCs, moral flags, world generation allocations or content migrations are needed.

## Shared execution rules

Each new successful physical action costs one ordinary action; refusal costs none. Browsing actions is pure. Use current actor/source/part ownership, occupied-cell reach and exact selected identities at execution. Join existing `InventoryTransaction`; rollback restores only owned changes, including actual water/fuel, crop/ground state and outputs. Reentrant or stale selections cannot pay twice. Existing models and saved public fields are sufficient. No new Objects.json entry is presently required; any later necessity is root-owned.

New verbs use ordinary `GetInventoryActions` and `InventorySystem` dispatch. The clarity owner integrated the small paid-command classifier into InputHandler and InventoryUI. Do not add a modal targeting framework. Tests first establish real command behavior; add paid-input, replacement-save and native demonstrations before completion. Tests may use explicit controlled fixtures, labeled separately from ordinary discovery.

## 31 — Trustworthy drinking from nearby wells

**Player outcome:** a working well cures thirst only while the living player is actually beside that same physical well; a stale menu cannot drink from a removed or remote source.

**Design/plan:** add one pure local availability proof shared by WellPart action projection and execution: live actor, actual actor/well membership, intact current WellPart/Physics, occupied-cell distance≤1, usable repair state. Reject carried/equipped wells and stale foreign-zone aliases. Keep full Parched cure and limitless ordinary well supply. Apply the cure through the caller's receipt so a failed outer action cannot leave thirst cured.

**Acceptance/counters:** adjacent live usable well cures all Parched; distant, removed, dead, faulted and foreign-owner cases preserve effects/action clock. An outer throw restores the same Parched payload. A repaired well still works. Save/revisit does not undo repair or manufacture a supply owner.

**Files/evidence:** `WellPart`, existing WaterVessel/repair predicates, new `FiftyWorldServiceTests`. The initial source sweep identified this seam; the execution ledger below records its native tests.

## 32 — Campfire rest reports and pays only a real safe rest

**Player outcome:** attempting to rest beside danger or through a stale fire menu does not spend an action; real rest still heals and advances the established clock.

**Design/plan:** use the same current source/reach/dead checks for ordinary and next-band Campfire actions; propagate the boolean result from RestSystem. Require a physical actor and valid active clock before ordinary rest as next-band already does. Preserve current hostile radius and no-offscreen-combat policy. Include occupied-body distance so a large nearby hostile is not overlooked; do not change faction hostility. No new blanket environmental hazard system.

**Acceptance/counters:** adjacent authorized safe rest returns handled with full HP and+60 ticks; blocked/remote/stale/no-clock rest returns unhandled with HP/effects/time unchanged. Next-band still lands on its boundary. Cooking-only coals never gain rest. Paid inn/bed callers retain existing prices/permission gates.

**Files:** `CampfirePart`, `RestSystem`, `FiftyWorldServiceTests`. Confirmed outcome/reach seam; no rest rebalance.

## 33 — Glean field grain without losing or duplicating the row

**Player outcome:** harvesting a field row either gives its finite grain and leaves stubble, or leaves everything untouched. Large rows can be reached at their actual edge.

**Design/plan:** bring FieldHarvestPart onto the established inventory receipt pattern. Stage and validate unique portable products before placement; capture row Harvested/render/examine state; pack within real capacity and leave overflow on the actual source contact/anchor. Claim actor and source before factories, and use occupied-cell reach. Preserve grazer ConsumeByGrazer's no-yield path and local saved stubble.

**Acceptance/counters:** successful gather produces exactly authored units once; outer failure, invalid product, moved source and reentry restore original row and inventory. Full pack leaves physical grain, never deletes it. Multi-cell near edge succeeds while a real gap refuses. Revisit/save retains cut state.

**Files:** `FieldHarvestPart`, existing inventory receipt helpers; `FiftyWorldHarvestTests`. One improvement, not separate counts for each guard.

## 34 — Plant a carried seed into an adjacent prepared bed

**Player outcome:** plant the bed beside you without stepping onto it or moving off a carefully chosen path.

**Design/plan:** retain PlantSeed-underfoot; add deterministic actual bed choices within one occupied-cell step to the seed's action list. Encoded command contains zone and cell. SeedPart reuses existing transactional source/factory/soil/one-seed proof against the selected cell. Do not offer occupied, flooded, barren or unprepared cultivated beds. Do not move the player or manufacture terrain.

**Acceptance/counters:** choosing east bed consumes exactly one carried seed and plants east while the actor stays; changing that bed before execution refuses with no payment. Remote cell, wrong zone, crop overlap and noncultivated counter refuse. Old PlantSeed behavior passes. Replacement save retains crop/seed counts.

**Files:** `SeedPart`; `FiftyWorldFieldworkTests`. New verb, no map/UI framework.

## 35 — Clear an unwanted young crop deliberately

**Player outcome:** sacrifice a seed or sprout to free its prepared bed for a different planting, without attacking one's own garden.

**Design/plan:** add ClearCrop only to real unripe cultivated crops at reach 1. One action removes that exact crop, returns no seed/produce and keeps original terrain/cultivation. This is an explicit sunk-cost choice, never a harvest shortcut. Existing local reserve claims/warnings apply to clearing another person's marked crop; use existing claim recording instead of a new morality flag.

**Acceptance/counters:** stage 0/1 clears and allows subsequent planting; ripe crops keep Harvest and do not expose destructive clear. Foreign/distant/nonprepared/currently changed owner refuses. Outer rollback restores exact crop/cell ordering; save/revisit preserves cleared bed and no refund.

**Files:** `CropPart`, bounded crop action service, `LocalGatheringClaims` integration; `FiftyWorldFieldworkTests`. New behavior; no general bulldozer.

## 36 — Gather ripe crop produce into the pack, keeping overflow real

**Player outcome:** choose Gather to pack ripe produce and its returned seed, or keep the existing Harvest-to-ground choice.

**Design/plan:** add an explicit alternate command to CropPart; extend CropYieldService only at output destination. Keep its exact source, yield validation, local claim/progress and transaction. Add each output within actual capacity; those that do not fit remain on the original crop cell. Report packed versus dropped counts. No autofill from other cells or mass harvest.

**Acceptance/counters:** enough capacity packs exactly yield+seed and removes one crop; full/partial capacity conserves all units as ground overflow. Ground Harvest stays unchanged. Failed outer action restores crop, carry weight and all outputs. Replay cannot duplicate; save preserves split destinations.

**Files:** `CropPart`, `CropYieldService`; `FiftyWorldFieldworkTests`.

## 37 — Transfer clean water between carried vessels

**Player outcome:** consolidate a partly full waterskin/gourd/flask without pouring water onto the ground or discarding a useful container.

**Design/plan:** source-vessel actions name an exact other carried vessel; transfer min(source units, destination capacity remaining), water only. Support existing Waterskin and LiquidVessel units without mixing or arbitrary drinking. Reject self-transfer, duplicate IDs, equipment aliases, nonwater liquid and malformed capacity. Clear an emptied flask's LiquidId, preserve empty vessel owners, and join one receipt.

**Acceptance/counters:** 3→empty capacity 2 leaves 1+2; partial destination caps exactly; total water never changes. Foreign/stale/acid/full/self choices refuse. An outer throw restores both vessels; save retains units and IDs.

**Files:** `WaterskinPart`, `LiquidVesselPart`, small shared water-transfer service; `FiftyWorldLiquidTests`.

## 38 — Pour one measured unit instead of the whole flask

**Player outcome:** spend a little oil/water/other supported liquid on one nearby cell and keep the rest for later.

**Design/plan:** keep existing pour-all action and command semantics. Add distinct pour-one actions for valid neighboring destinations with an encoded expected liquid/current volume/zone/cell. Parameterize only the amount in the existing receipt, source/purity and contact pipeline. Existing same-liquid pools merge normally; no split-item trick or extra recovered volume.

**Acceptance/counters:** 3 units pour-one→2 carried+1 ground; pour-all still→0+3. A stale volume/liquid/destination refuses; mixed source/destination rules remain. One-unit source behaves exactly once. Rollback and save conserve units and coating lease.

**Files:** `LiquidVesselService`; `FiftyWorldLiquidTests`.

## 39 — Douse a burning world object with carried clean water

**Player outcome:** spend one water unit to extinguish a reachable burning prop before it consumes the remaining fuel or blocks a useful worksite.

**Design/plan:** water-bearing vessels offer exact nearby noncreature burning-owner targets. One unit applies the existing wet/cooling/extinguish semantics to that owner, preserving structural damage already taken. No creature/combat cleanse and no global flame cancellation. Validate before payment, capture affected water/thermal/burning/wet state, commit together; no positive outcome if the object is dry/cold/already destroyed.

**Acceptance/counters:** one unit stops the current real burn and wets/cools the same prop without restoring HP/fuel; acid/empty/remote/dead/changed target refuses. Other burning owners remain lit. Rollback and save retain authoritative state.

**Files:** water vessel action service, existing WetEffect/Thermal events; `FiftyWorldEnvironmentTests`. New physical action, distinct from spell damage.

## 40 — Dry wet gear beside genuine heat

**Player outcome:** spend time at a real fire to dry a carried wet torch or tool, then use it again.

**Design/plan:** the nearby Campfire action list names exact wet carried equipment. Each paid DryGear reduces WetEffect.Moisture by 0.25, floored at 0; remove Wet only at 0 through its normal lifecycle. Require actual heat≥150 and usable finite fuel when finite; reject Frozen items until thawed. Do not damage gear, turn frozen into dry, replenish fuel, dry the entire pack or mutate a merely listed item. A warm public stove remains valid on its existing terms.

**Acceptance/counters:**0.5→0.25→dry across two paid actions beside valid heat; cold/exhausted finite source, dry/frozen/foreign gear and stale target refuse. Existing natural evaporation continues unchanged. Outer rollback restores exact moisture/effect; save retains partial drying.

**Files:** `CampfirePart`, bounded equipment-drying service; `FiftyWorldEnvironmentTests`. Fixed rate is a new balance choice, not a proven fun claim.

## 41 — Feed and relight finite cooking coals at an actual material cost

**Player outcome:** bring timber and a real flame to make a known cooled workspot useful again on a return trip.

**Design/plan:** only opt-in FiniteCooking sources offer Feed with one carried SalvagedTimber, adding 10 fuel up to the authored MaxFuel. Refuse full fuel before payment. Feed does not create heat. Relight requires positive fuel and actual nearby/equipped controlled flame or hot burning source; set coals to 450 through bounded existing thermal behavior. One action each. Exhausted/cold coals never become free rest, and timber remains a competing repair/trap-jam resource.

**Acceptance/counters:** cold 0 coals+timber become 10 fuel but still cannot cook; ignition then enables existing Cook. No timber/no ignition/fullfuel/malformed owner refuses without costs. Fuel cap, fixed identity, saved heat/fuel and revisits hold. Existing nonfinite public stoves and meal recipes remain unchanged.

**Files:** `CampfirePart`, finite-fire action service, `FuelPart` only if a narrow helper is necessary; `FiftyWorldEnvironmentTests`. Uses existing SalvagedTimber/SpreadCookingCoals; no Objects.json addition.

## 42 — Pass fire from an equipped torch to another torch

**Player outcome:** light a spare from the torch being held rather than having to drop the light on the ground first.

**Design/plan:** extend TorchLightPart's source search to exact equipped physical owners, including both valid hand slots, deduplicated by owner. Require controlled light enabled, positive valid fuel, sufficient heat and not wet/frozen. Exclude target itself, stowed items, stale equipped backlinks and arbitrary glowing items. Target still follows existing loose/equipped reach rules; no free portable ignition item.

**Acceptance/counters:** an extinguished loose adjacent target lights from an actual held lit torch; stowing/extinguishing/exhausting/malforming the source makes the identical attempt refuse. Flame transfer spends an action, does not manufacture fuel or duplicate light, and persists normally.

**Files:** `TorchLightPart`, shared read-only ignition lookup if 41 needs it; `FiftyWorldEnvironmentTests`.

## 43 — Reclaim already-cut field stubble as a prepared growing bed

**Player outcome:** turn a finite gleaned field location into a place worth revisiting with seeds and water.

**Design/plan:** only a harvested FieldHarvest owner offers PrepareBed. Its physical terrain owner remains; mark it Plantable and add CultivatedSoilPart after verifying nonbarren, unflooded, nonsolid ground and no existing crop. Preserve harvested stubble identity/state, changing only its planting suitability. No new produce, seeds, instant growth or regeneration. Never allow the action on unharvested grain or arbitrary wilderness terrain.

**Acceptance/counters:** glean→PrepareBed→plant existing cultivated seed works on that exact cell; uncut row, liquid/barren/occupied soil and replay refuse without supplies/time. Save/revisit retains exhausted grain and cultivation; no grain respawn or terrain replacement.

**Files:** `FieldHarvestPart`, `CultivatedSoilPart` existing predicate; `FiftyWorldFieldworkTests`. New narrow cultivation permission, not free-form tilling.

## 44 — Poured finite clean water can irrigate a real planted bed

**Player outcome:** pouring one unit onto a young prepared crop does the expected useful work instead of leaving only an inert puddle.

**Design/plan:** during a committed water pour, capture the current crop and prepared-terrain proof before placing this action's pool. Reconcile old crop time, then if still unripe and below 40 moisture, transfer exactly one of the actual poured pool units into the existing moisture top-up. Retire an exhausted poured owner using existing lifecycle. Excess water remains a real pool and can still flood the bed. No automatic continuing drain, thin-coating irrigation or water creation. Shared helper must respect outer rollback; world/contact publication occurs only after state is committed.

**Acceptance/counters:** measured 1 water onto valid dry bed→40 moisture, no recoverable unit/pool remains; 3 water→40 moisture+2 ground units. Ripe/already-watered/unprepared/mixed/nonwater cases receive ordinary ground pour with no crop benefit or extra consumption. Failed transfer preserves pre-pour crop and vessel; existing 40 top-up rather than additive water, elapsed-time and save rules hold.

**Files:** `LiquidVesselService`, crop watering helper; `FiftyWorldLiquidTests`. Needs 31–38 current owner/conservation patterns, not a new irrigation simulation.

## 45 — Frozen water must thaw before it can be drawn

**Player outcome:** a frozen pool has a practical consequence; warming/thawing it restores access to its same conserved supply.

**Design/plan:** one pure source-phase predicate used by waterskin and liquid-flask fill: a water owner with positive Frozen.Cold, at/below its existing Thermal freezing threshold, or current ice coating on its occupied source cell cannot be drawn. Use actual source state, not a temperature label or global biome rule. Existing thaw/heat and tile melt restore eligibility; no refill occurs. Ordinary wells retain repair-based supply policy unless they actually carry an authored thermal/frozen state. Do not invent thermal Parts on poured pools.

**Acceptance/counters:** actual WaterPuddle cold application/frozen coating blocks both fill paths without spending volume/action; thaw same owner then fills and depletes normally. Warm pool/no Thermal clean source continues working; acid/brine purity rules and empty/deleted sources remain refused. Save preserves cold and volume; queries never advance thaw.

**Files:** shared `LiquidSourceSafety` phase helper, `WaterVesselService`, `LiquidVesselService`; `FiftyWorldLiquidTests`. New phase rule grounded in shipped Thermal/Frozen state; its usability merits playtesting.

## Execution order and verification

Implement service corrections 31–33 first; then fieldwork 34–36/43; liquid conservation 37–38/45; environmental actions 39–42; integrated poured irrigation 44. These are dependency groups, not extra counted improvements. Freeze all Assets during root Unity imports/runs. Each item needs its positive and counter RED, smallest implementation, affected native regressions, reentry/rollback/save review and a bounded live action receipt where feasible. Do not hide a failed observer or claim human delight from a scripted success.

Initial tests will compile against current public action APIs and literal proposed command contracts, so absent features fail behaviorally. A separate minimal paid-input extension is coordinated with clarity after command names settle. Final evidence must distinguish script-observable conservation/reach/time/save from unassisted discovery, visual polish and long-term economy. Plan rates (dry 0.25 / feed 10 / relight 450) are explicit new design choices, not measured optimal values.

## Overlap and risk ledger

- No FoodPart or cooked-food blueprint edits; meal benefits 46–50 belong to root.
- No combat AI/damage/status rebalance, no new HUD, map, menu framework or speculative tooltips. Existing wet/frozen/thermal mechanics are reused for objects only.
- Crop reserve clearing/gathering must pass existing LocalGatheringClaims hooks, not silently grant permission. Keep actual recorded-owner provenance.
- `CultivatedSoilPart` rejects pools intentionally:44 consumes its own finite pour, preserving excess flooding. Do not loosen the shared soil predicate to make the test pass.
- Exact world owner, saved depletion and outer-transaction rollback are required. Factories may invoke callbacks; validate the whole staged batch and retained source after callbacks.
- Historical initial gate: production waited for root-observed native RED. Implementation now exists; final GREEN and native bounds are recorded below.

## RED handoff — 2026-10-05

The complete initial behavior fixtures are authored in `Assets/Tests/EditMode/Gameplay/World/`: `FiftyWorldServiceTests`, `FiftyWorldHarvestTests`, `FiftyWorldFieldworkTests`, `FiftyWorldLiquidTests`, and `FiftyWorldEnvironmentTests`, using `FiftyWorldFixture`. They exercise the existing action dispatcher and actual content factory; no future production type is required to compile. Each new file has its own 32-character Unity GUID. The shared fixture restores the active clock/world/settlement, content factory hooks, liquid registry and log tick provider. At this historical handoff, production was unchanged pending root-observed RED. Root alone imported and ran Unity; subsequent receipts are recorded below.

Frozen command contract: `PlantSeedAt|<zone>|<x>|<y>`, `ClearCrop`, `GatherCrop`, `TransferWater|<destination ID>`, `PourOneLiquidVessel|<liquid>|<expected volume>|<zone>|<x>|<y>`, `DouseWorld|<target ID>`, `DryGear|<item ID>`, `FeedCookingFire`, `RelightCookingFire`, `PrepareFieldBed`. Identifiers use the existing escaped-field convention. Ordinary existing well/rest/harvest/light commands retain their names. At the historical RED handoff, the paid-command classifier was planned; it is now integrated into the existing menu dispatcher and inventory success path.

The initial suite proves command outcomes and bounded token-graph persistence for fields and carried water. It does not claim complete replacement-session persistence or an ordinary native journey. Additional focused saved-graph and paid-input integration checks follow implementation, without duplicating all existing liquid/crop tests.

Source correction approved after initial handoff: `GroveSeep` actually combines WellPart with finite LiquidPool water. Its current direct well command bypasses both finite volume and phase. Item 31 now spends exactly one real unit when the same well owner has a pool, and refuses empty, frozen or mixed supply; ordinary pool-less wells remain unlimited. Added available/empty/frozen command countercases before the exported RED rerun. This does not introduce a second water authority.

## First implementation handoff

All 15 items now have production paths. Small scoped services hold world-owner validation, drinking, water transfer, dousing, campfire work, field preparation/clearing, finite grain harvest and poured irrigation; existing Parts supply their ordinary action menus. Existing SeedPart/CropYieldService/LiquidVesselService retain their transaction and source proofs while adding the selected target/destination/amount. No blueprints or new framework were introduced. Root observed 83 world RED cases (40 failed/43 passed), including finite-seep counters. Six further integration cases now cover reentrant/malformed grain creation, ambiguous vessel identity, query purity, actual heat thaw, and a full replacement-session save of spent grain, prepared soil, a watered crop, conserved carried water, and timber-fed cold coals. These extra cases are post-RED validation, not retroactively counted in RED.

A standalone Roslyn source check reported no compiler errors; it is not a Unity test or gameplay result. At this historical handoff, Unity execution and subsequent evidence were pending; the later receipts below supersede that status.

Dedicated adversarial sweep authored before review fixes: `FiftyWorldAdversarialTests` has 21 scenarios across mixed/finite/aliased wells, physical body reach, wide-hostile rest, outer rest rollback, late output/seed factory mutations, post-merge carry capacity on both harvest paths, crop clear rollback with independent ground work, vessel equipment aliases, douse cooling/phase/removed-effect callbacks, drying effect replacement, fuel capacity, forged equipment ignition, stale relight and replacement-session aftermath. Source review predicts failures for absolute post-merge weight, forged equipment cache, cold-object dousing, same-class wet callback rollback, destroyed douse target, and outer rest time/bleeding leakage. At that historical stage these were review hypotheses, and fixes were held for root’s native result; the next receipts record which were confirmed. The root also identified a misleading early rest diagnostic to correct alongside those fixes.

## First native implementation receipt and setup correction

Root recovered the complete current Unity receipt in `Docs/Verification/FiftyImprovements/first-implementation.xml`: 298 cases, 246 passed and 52 failed overall; 46 failures belonged to the world fixtures. This is not a GREEN result. The common new ownership predicate incorrectly required `Zone.IsFootprintCurrent` for every actor/owner, although that index contains only entities with an explicit `SpatialFootprintPart`. The corrected predicate preserves current zone/cell/physics ownership and demands registered footprint agreement only for owners that declare a footprint. Most proposed actions had therefore refused before reaching their intended behavior.

Two test premises were also corrected: a bare PhysicalObject with heat/fuel does not admit Burning without the existing flammable-material authority, so the burning-prop fixtures now supply Wood/Flammable and assert successful effect application; the authored CandyCarrotSeed weighs zero and legitimately fits a pack with zero remaining weight. Its overflow assertion now distinguishes the one packed seed from two real ground carrots. Neither correction changes status admission or seed weight. Only these ownership/fixture corrections were released for the immediate rerun. The post-merge capacity failure did reach its intended assertion (20 carried against 11 capacity); masked callback/rest/ignition hypotheses therefore required the corrected native rerun before behavior fixes.


## Corrected review RED and bounded fixes

Root exported `Docs/Verification/FiftyImprovements/review-red.xml`: 299 cases, 288 passed and 11 failed. The corrected world setup exposed eight intended failures: both harvest paths exceeded actual merged-stack capacity; a forged equipment cache supplied flame; dousing heated an already colder object; a destroyed structural owner accepted dousing; douse rollback retained its own dampening of callback-added Wet; drying rollback duplicated Wet; and a refused outer campfire action still advanced 60 ticks. Those failures were observed before their fixes. Other ordinary world behavior cases passed.

Two remaining world failures were frozen-state setup errors, not evidence that the phase rule failed: the authored GroveSeep has no freezable material or Thermal part, and the bare gear fixture also lacked a freezable material tag. The seep case now uses an actual ice coating (an existing supported phase authority); the gear case declares Metal and asserts successful Frozen application. No blueprint or universal effect gate changed.

The fixes reuse the existing post-merge capacity helper, require actual Body bindings for equipped flame donors, reject gone/zero-structure douse owners, clamp cooling against the old temperature, capture the actual Wet effect after Burning-removal callbacks, and restore only the quarter of moisture removed by drying. Campfire now passes its outer receipt into rest so failed actions publish no heal, bleeding cure or clock advance. Command services emit bounded rejection reasons; pure menu/source queries remain read-only. Cold review confirmed the earlier three environmental findings fixed, and found a legacy bed rest caller conflict in the initial transaction adaptation; that neighbor correction was subsequently observed in the existing bed fixtures, as recorded below.

A new `FiftyWorldBench` contains 15 controlled native-runtime checks through real inventory commands, owner scheduler payment and a replacement session save. It covers finite seep drinking, carried water transfer, crop clear/adjacent planting/one-unit irrigation/gathering, douse, cold-coal feeding, actual held-torch ignition and partial drying. Its elapsed crop growth explicitly advances a detached clock, then uses existing CropTime; it does not claim ordinary exploration or real-time waiting. It restores the caller's clock, world, settlement, content hooks, log and visual queues. Source compilation passed; the final Play receipt and its limits are recorded separately from source review.


The next native receipt, `menu-bed-review-red.xml`, contains 342 cases, 315 passed and 27 failed overall. All eight world review defects passed after their fixes. Twenty existing bed failures confirmed that a newly created rest transaction conflicted with PlayerBedService's existing actor reservation. Rest now preserves synchronous behavior when no outer transaction is supplied, including the caller's across-callback reservation and legacy exception behavior; campfire inventory actions alone pass their existing transaction and defer publication. PlayerBedService itself remains unchanged. Three other world failures were torch fixtures using `InventoryPart.Equip(item, "Hand")`, an intentional legacy cache API that does not bind a Body part. Those fixtures now call ordinary `InventorySystem.Equip` and assert the actual Body binding. The forged-cache counter still uses its intentionally invalid equipment setup and must refuse. These are compatibility/fixture corrections, not a weakening of ignition ownership.


## Focused native GREEN and in-phase review

Root exported `Docs/Verification/FiftyImprovements/focused-green.xml`: **342/342 passed**, comprising 300 new cases across the 50-improvement program plus 42 existing bed cases. This includes the 110 world cases, all 21 dedicated world adversarial cases, the corrected physical/phase/equipment fixtures, and the old bed ownership, reservation, reentry and throwing-observer contracts. It is a focused result, not a claim that every repository test or Play demonstration passed. The broader native receipt and its follow-up corrections are recorded below.

- 🟡 **Resolved:** ordinary owners were incorrectly required to have registered multicell footprints; explicit footprints still require current registration, while single-cell owners use their actual cell/physics authority.
- 🟡 **Resolved:** failed outer campfire rest leaked clock/bleeding state; only supplied outer transactions defer rest publication. Direct bed/conversation calls retain existing synchronous behavior and reservation guards.
- 🟡 **Resolved:** post-merge weight, forged equipped flame, inappropriate structural dousing, heating during cooling, and same-class Wet rollback defects all have observed RED then focused GREEN.
- 🔵 **Scope corrections:** finite GroveSeep drinking consumes real pool water; no new supply owner, thermal part, status matrix rule, blueprint, farm-growth engine or inventory framework was introduced. Freezable/flammable/equipment fixture assumptions were repaired rather than weakening runtime authority.
- 🧪 **Evidence boundary:** broader neighbors and the native command/scheduler/save benchmark are reported separately below. Scripted tests cannot establish unassisted discovery, visual clarity in every camera, enjoyment or long-term resource balance.

## Broader native receipt and compatibility corrections

Root exported `Docs/Verification/FiftyImprovements/broad-first.xml`: **15,889 total, 15,848 passed, 41 failed**. This is not a full GREEN result. The wider run exercises contracts outside the focused fixtures. Its failures exposed two further compatibility defects: null actor/zone guards must precede dictionary lookups and nullable equality in the new ownership helpers; and the frozen-water predicate must preserve the existing literal well/spring/pool sources accepted without a PhysicsPart rather than import the stricter new-work-service predicate. The corrected phase check retains actual world-cell/footprint authority and rejects any present carried/equipped Physics state, while keeping cold/ice refusal. The existing FieldHarvest null-context/explicit-active-zone pair also requires execution to retain its original event-zone-or-active-zone fallback; actor-derived zone inference belongs only in menu queries.

The public rest method's new optional parameter broke an existing reflection caller even though ordinary calls compiled. The correction preserves the unique original four-argument public methods and uses distinctly named internal transaction entry points for campfire. Existing rest fixtures lacking a PhysicsPart need a real physical actor to test the approved current-world policy; the old null-zone-heals assertion intentionally changes to refusal. A legacy zero-stage-length crop test depends on TurnManager.Active being null: `CropPart.Water` already rejected invalid elapsed-clock crops at baseline `bfa625f44`. That fixture now explicitly establishes and restores its intended clockless legacy driver. No crop growth production change is justified by that failure.

The broad run also missed a ripe crop in the cold western allotment. Source review found no changed generation path or repair-recipe dependency. The unchanged test passed in the follow-up below; no generation code was changed and no baseline defect is established.

The initial broader corrections were applied and source-compiled without errors before the follow-up native run. Three additional positive-service fixtures now establish their real prerequisites: the seep has an adjacent physical actor and explicit zone, with its finite one-unit depletion asserted; Last Counter campfire users have a PhysicsPart, and the hostile counter now explicitly requires an unhandled, unpaid refusal; the Morrowfast owner-selection test approaches the real stove after proving its remote rest action is absent. The remaining native rain fixture now tags its real crop owner and makes it non-takeable, matching existing CropTime ownership rather than altering rain or crop growth.

The heavy-stack generic harvest failure was traced to a fixture premise: RawMeat has Handling.Weight=2, which takes precedence over the Physics.Weight the test alone changed to 10. The fixture correction sets both authoritative values and asserts the actual initial carried weight before retaining its overflow and finite-output checks. That correction is now applied with an explicit starting-weight precondition; the generic harvest implementation is unchanged.

Root’s first broader follow-up contains **829 cases, 827 passed and 2 failed**: the unchanged heavier-stack fixture and the separately owned intended price-reader RED. All corrected world-service, source-phase, rest/reflection, rain and Morrowfast cases passed. The cold western allotment also passed without any change to its test or generation code. This makes the earlier missing-crop result consistent with order/global-state sensitivity, but does not prove a specific contaminating fixture or a baseline defect. The broader corrections explicitly restore clock/global prerequisites in older fixtures; no causal attribution beyond the observed rerun is claimed. The subsequent final correction rerun passed that fixture as recorded below.

## Final correction and controlled Play receipts

Root exported `Docs/Verification/FiftyImprovements/corrections-green.xml`: **829/829 passed**, covering the corrected failing neighbors and the new program fixtures. The complete 15,889-case broad batch was not rerun; its first receipt remains 15,848 passed and 41 failed. Acceptance combines that broad coverage with the fully passing affected correction batch, rather than presenting a fictitious single all-green broad run.

The controlled native report at `Docs/Verification/FiftyImprovements/NativeClarity/7cc81439e2f3478a827eaa48c8b598ba/report.json` completed with zero failures and zero errors. Its separately recorded world benchmark (`04bc656fee5340d0bf33b6c48d2daaa8`) passed **15/15** checks: finite well drinking, conserved vessel transfer, young-crop clearing, adjacent planting, one-unit soil irrigation, no recoverable duplicate pool, measured growth, ripe gathering, dousing, finite timber feeding, no-flame refusal, genuine held-torch ignition, selected-gear drying, spent-timber refusal, and full replacement-session finite aftermath. Successful commands used actual scheduler payment; refused actions stayed free.

This world benchmark uses a detached controlled fixture and direct commands. Growth explicitly advances its detached clock. It demonstrates command, conservation, payment and replacement-save behavior, not native keyboard resource selection, ordinary item discovery, unassisted exploration, natural waiting, enjoyment or long-term balance. The same run separately exercised native reader controls. Root independently inspected all six screenshots and found their reader text readable; those screenshots do not establish world-resource visual clarity across all cameras. Root restored SampleScene in EditMode with zero console errors.

## Final source-review terminal-state correction

A last bounded cold review found that deferred campfire healing could restore HP after a successful outer `AfterInventoryAction` callback made the actor terminal. No shipped killing callback was identified; this is an adversarial lifecycle gap, not an observed ordinary-play failure. Two cases were added before production changes: zero HP without a handled-death marker, and positive HP with the existing handled-death marker. Both commit the outer action while retaining bleeding and unrelated terminal state. The approved contract preserves the committed 60 ticks, skips healing/cure, and emits `RestInterrupted` with `actor_dead` instead of a misleading `Rested`. Direct synchronous bed/conversation behavior remains unchanged. Root exported `rest-lifecycle-red.xml`: both cases failed at the intended assertion (zero HP became 40; handled-death HP 3 became 40). The bounded fix now takes one commit-time living snapshot, preserves committed time, and gates both benefits and their receipt on that snapshot. The final 831-case correction run passed after the diagnostic-scope correction below; the refreshed Play result is recorded separately. The earlier 829/829 and 15/15 receipts remain correctly identified as preceding this final guard.

The first final-guard batch (`rest-review-first.xml`) contained **831 cases: 830 passed, 1 failed**. Both new cases passed their HP, bleeding and paid-time assertions; the handled-death case counted a retained diagnostic from the preceding case because factory IDs repeat. The test now captures prior trace identities immediately before the command and requires exactly one new `RestInterrupted`/`actor_dead` receipt and zero new `Rested` receipts. No production change followed this diagnostic-scope correction.

Root exported `Docs/Verification/FiftyImprovements/final-green.xml`: **831/831 passed** on final source, including all **112 world cases** and both terminal-rest adversaries. The dedicated world adversarial fixture now contains 23 cases. The failed-outer rest, live rest, direct bed reservation and callback controls also remain green in the affected batch.

The final-source native report at `Docs/Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/report.json` completed with **zero errors and zero failures**. Its separate world benchmark (`87d096aad4444701a6d8f112fbf64d80`) passed **15/15**, including the replacement-session finite aftermath. This refresh supersedes the earlier run as final-source command/scheduler/save evidence; the same controlled-fixture and explicit clock-advance limitations apply. Root inspected the three changed captures; the other three were byte-identical to the previously inspected images. The combined report passed all 60 checks, and root restored SampleScene in EditMode with zero console errors. No further gameplay, fixture or Assets changes followed it.
