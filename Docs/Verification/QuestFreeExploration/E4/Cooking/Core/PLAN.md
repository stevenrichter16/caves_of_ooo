# E4 / F10 finite cooking source — private verified plan

Status: private core candidate implemented after executed RED on 2026-09-28. The parent authorized the bounded grain recipe and exact cooking-source repair; no shared files or Unity state changed. Current result and scope are in `GRAIN-MILESTONE.md` and `CORE-REVIEW.md`. The original merchant-only availability proposal is superseded by deliberate ToastedEmberwheat from existing finite field yield. Source placement, saved family registration, workspot name/art/readout and real approach timing remain later gates. Explicit `FiniteCooking=false` preserves all established authored stations and old saves.

## 1. Outcome and scope

One small, saved, cooling wilderness cooking source can prepare food the player actually carries while its real heat/fuel state permits. Its cooled/exhausted state must be readable. The source gives no food, vessel, salvage, merchant stock or enemy allowance. Ordinary Campfire/Morrowfast stations, free resting, existing fire propagation and protected landmarks remain unchanged.

The initial finite resource is a **window of stored heat**, not a claim that merely cooking burns Fuel. Use ordinary Thermal cooling and the existing material tick. Do not add Burning automatically, spend fabricated fuel per menu action, or start an offscreen simulation. If a later variant begins actually burning, its native damage/heat propagation/fuel use must be tested separately before rollout.

## 2. Verified source corrections

Paths and line numbers refer to the current working source captured in `source-manifest.json`, not necessarily committed HEAD alone.

| Claim | Verified source | Consequence |
|---|---|---|
| Inventory cooking has no heat gate today | `Assets/Scripts/Gameplay/Items/CookingService.cs:23`, `:71–76`: first adjacent CampfirePart wins. | Cold/exhausted rejection is a **new opt-in contract**, not a correction to a previously specified temperature rule. |
| Existing whole-stack transaction is meaningful | `CookingService.cs:19–21`, `:31–66`; output creation precedes inventory receipt/application, and success uses AfterCommit. | Keep quantity, merge, capacity, rollback and success-record behavior. Do not replace the transfer implementation. |
| Current factory recheck is incomplete | `CookingService.cs:41–44` checks recipe/count/current inventory and distance to the captured Entity, but not the exact current Campfire/Fuel/Thermal parts or source membership/backlinks/anchor. | Capture a narrow exact station witness and revalidate it after output factories, before any food change. A removed/replaced part must not retain permission. |
| Fresh Campfire is hot but is not necessarily burning | `Assets/Resources/Content/Blueprints/Objects.json:18368–18479`: CampfirePart, Thermal Temperature500/FlameTemperature300/HeatCapacity0.8; Fuel200/Max200/BurnRate0.5/HeatOutput1.5. No Burning initialization. `VillagePopulationBuilder.cs:754–787` adds the part only if missing and ground markers; no ignition. | Requiring Burning would reject a fresh raw station. Fuel200 is not evidence of ongoing consumption. |
| Cooling and fuel consumption are distinct | `Materials/MaterialSimSystem.cs:37–59,63–103` gives hot non-burning scenery EndTurn only. `ThermalPart.cs:231–251` cools toward25 at default0.02. `FuelPart.cs:21–43` consumes only on ConsumeFuel. `BurningEffect.cs:91–123` sends that event and marks exhaustion. | No universal fuel-ticking change. Under unchanged defaults and no outside heat, T(n)=25+475×0.98^n: tick66≈150.203, tick67≈147.699. This is source-derived arithmetic, not an executed native timing claim. |
| Initial hot temperature does not automatically ignite | `ThermalPart.cs:43–71`: ignition requires an ApplyHeat crossing from below the effective flame threshold. EndTurn does not ignite. | A cooling initial source can be low-risk without pretending its mesh means active Burning. Ordinary later real ignition remains possible. |
|150°C has a real but separate source precedent | `Assets/Resources/Content/Data/MaterialReactions/fire_plus_raw_meat.json:8` and `fire_plus_raw_starapple.json:8` use MinTemperature150 on the **food itself**. `CookablePart.cs:5` explicitly separates inventory recipes from thermal ground recipes. | Parent chooses150 for the new station rule; it is a deliberate shared design value, not pre-existing inventory semantics or a proof that source/food temperatures are physically identical. Do not use100°C or FlameTemperature as an accidental cooking threshold. |
| Authored stations must remain compatible | `World/MorrowfastSceneRuntime.cs:238–255` adds CampfirePart to hearth/oven/outdoor stove without Fuel/Thermal. Tally, Cinderhold and LastCounter compositions create ordinary Campfires with Fuel200. | Default `FiniteCooking=false` preserves both thermalless authored stations and ordinary authored Campfires, even after their thermal values cool. No retrofit on load. |
| Bare scenery names do not authorize cooking | Raw Oven `Objects.json:21805–21844` has no CampfirePart. UntendedFire `:29889–29922` has Thermal/Light but no CampfirePart/Fuel; its protected contract is `LandmarkBuilder.cs:1019–1033`. HearthPatch is a separate settlement system. | No name/glyph fallback, no protected fire conversion, no furnace/oven framework. |
| Rest is independent | `Settlements/CampfirePart.cs:23–53` offers rest; `RestSystem.cs:7–17,69–105` uses nearby-hostile gate, healing and pure clock advance. | Finite cooking refusal never removes resting or adds heat/fuel cost to rest. Keep exact existing tests. |
| Material cooling is local, not elapsed world time | `Presentation/Input/InputHandler.cs:983–1007` calls MaterialSim after the local player turn. RestSystem AdvanceClock is pure clock (`:94`); existing `DensityEverydayAdversarialTests.cs:88–89` pins no simulated actor turns. | Do not claim67 map ticks, offline expiry or that resting performs60 material ticks. Native action flow determines any one follow-up local material tick. |

## 3. Actual source availability and deliberate addition

The audited ordinary Spread surface population does not roll Campfire. Current sources are village decor, authored landmark stamps and named regional compositions/scene props. There is no general wilderness Campfire receipt to borrow.

`Assets/Scripts/Data/Tables/PopulationTable.cs:1061–1071` defines VillageDecor Campfire Weight3, Min1, Max2; Well Weight2, MarketStall Weight2. `Roll:138–148` starts each ambient entry at its minimum and tests optional extras against weight/ambientWeight: a village roll requests one Campfire plus a second with probability3/7. Actual placement can still refuse because `VillagePopulationBuilder.cs:50–78` needs open cells and places actual owners. This is not a1/7 all-or-none fire chance. These service/POI sources are not a permission to move a town's Campfire into F10.

Parent has authorized F10's existing plan budget of **one deliberate utility source**, rather than relocation-only fiction. Root should give it a distinct blueprint (provisional `SpreadCoolingCampfire`), explicit finite flag, exact source receipt/final generation validator and one saved owner. Selection and geography remain root-owned. Source refusal does not mint a replacement elsewhere. Existing loot/hostile budgets stay unchanged; any nearby salvage must come from an already rolled owner.

Initial suggested source values reuse the ordinary Campfire's thermal/fuel defaults to avoid inventing another fuel system. This adds one non-takeable fuel-bearing heat source, explicitly counted as utility; it does **not** grant those200 fuel units as an inventory resource. If root prefers a shorter window or less fuel, author the numbers deliberately and regenerate the lifetime/census evidence. No automatic Burning is needed for the initial cooling variant.

## 4. Small proposed API and admission contract

1. Add public saved bool `CampfirePart.FiniteCooking = false`. No custom serializer or migration. `SaveSystem.cs:1464–1495,1897–1939` already writes public fields and constructs parts before reading known fields; an old missing field retainsfalse. Pin real old-wire compatibility, not only a round-trip made after the field exists.
2. Add one read-only source-state helper, either internal on CookingService or a small `CookingStationState` type. Candidate enum: LegacyUsable, Hot, Cooling, Exhausted, Invalid. The exact API can stay internal unless the scoped art/readout needs it.
3. Legacy current Campfire owners are usable as before. Opted-in owners require their exact owned Fuel and Thermal parts, finite positive FuelMass, finite Temperature≥150; malformed/nonfinite values refuse. Both parts must point back to that station. No active Burning requirement, no per-cook fuel debit, and no inferred permission from a name, glyph, light or tag alone. BurnRate need not participate in stored-heat eligibility because cooking is not invoking fuel consumption; actual source construction separately pins its authored Fuel fields.
4. Require the actor in the current zone and capture the exact station, CampfirePart, optional Fuel/Thermal, Physics and anchor references before creating products. Require actual zone membership and physical placement, no carried/equipped source, current part backlinks, same source part/finite flag and current reach. Preserve ordinary footprint distance semantics rather than restricting existing authored sources to one-cell anchors.
5. After all product factories, recheck exact current actor/inventory/raw recipe ownership and exact station witness: same physical source/anchor/parts and finite flag, still current in the same zone, still usable, actor still current/alive/in reach. Do not rerun FindStation and silently substitute another heat owner. Reject without consuming food if a callback removed/moved/replaced/transferred the station or parts, cooled/exhausted it, or changed the opt-in.
6. Do not restore the borrowed station or callback-owned graph on refusal. The service has not spent heat/fuel; ordinary external changes survive. Keep InventoryTransferSnapshot and its idempotent rollback. An independent change after the already completed action is not grounds to resurrect the prior source.
7. FindStation should skip unusable finite sources and still find another current usable station; capture the selected exact owner once. This avoids an exhausted neighbor masking a valid authored hearth.

The field is explicit scope, not a blanket new 'station framework'. Old stations need only stronger current source authority across factory callbacks; their heat/free-rest behavior remains unchanged.

## 5. Truthful current readout and presentation are required before enabling F10

Confirmed current gaps:

- `CampfirePart.cs:59–63` always flickers red/yellow and `:87–90` always reports crackling when its proximity condition fires.
- `Presentation/Rendering/ZoneRenderer.cs:627–642` registers every CampfirePart with CampfireEmberRenderer at bind. `CampfireEmberRenderer.cs:91–104,148–180` spawns from stored anchors without heat/current-source checks.
- `AnimatedEnvironmentRenderer.cs:245–252` treats the exact Campfire blueprint as fire independent of state. `EnvironmentSpriteRenderer.cs:1380–1385` picks its fire tile by blueprint.
- `ArtSource/SpawnRing3D/build_ring.py:394–397` contains three permanently ember-colored pieces with the ring/log mesh. A bare static hot model is not a cooled-state proof.
- `LightSourceFlickerPart.cs:82–108` modulates whenever LightSource.Enabled; it does not know cooking state. Cached intensity must not overwrite a finite source's off state on later Render. Avoid changing all lights.
- `Entities/ExaminablePart.cs:102–148` composes flavor/effects/exploration details but no cooking state; ItemExamineService rejects non-Item sources (`:20–23`). The source needs a scoped current readout, not an item stat label.

Minimum coherent new-source presentation:

- Shared source-state helper supplies a current description: hot enough for carried raw food; too cool; fuel spent; unavailable source. Distinguish cooking-hot from actually aflame and do not promise relighting/refueling until a real offered verb exists.
- Finite-only cold/exhausted state suppresses hot color/crackle/embers/light and uses an actual cold coal/stone form; legacy stations retain their current presentation. Reusing ring/log geometry with separate hot-coal pieces is acceptable after exact persistent ownership/art validation. Do not mutate the global Campfire prefab or material.
- Handle exact current source removal/hidden/foreign graph and hot→cold transition. Since static geometry can be batched, verify actual dirty invalidation when the finite state crosses a boundary; do not assume changing Temperature refreshes its recipe. A scoped state-boundary notification once per material change is preferable to per-frame global rescans.
- Existing carried raw-food Cook menu is an opportunity and may refuse at execution; use an accurate cold-source refusal. New world affordance cue must not advertise actionable hot cooking for the cold source. Do not alter the current four-family action query without a separate tiny paired case if cooking is added to it.

## 6. Failing-first proof, bounded implementation sequence

Historical first checkpoint, before production: thirty private core tests executed against unchanged cooking production:3 legacy/unchanged-source controls pass,24 fail on the missing explicit FiniteCooking field, and3 reproduce exact selected-source authority gaps during output creation. See TEST-NOTES.md and final-red.xml. The full actual EditMode reference compilation succeeds with0 errors. These are isolated runner results and compilation, not native test execution; root retains the native publication gate. Production was unwritten at that historical checkpoint; current private results are in GRAIN-MILESTONE.md.

First core fixture (compile-compatible reflection for the new flag):

- Finite hot150 exactly /149.99 cold; positive fuel /0 exhausted; hot raw source without Burning passes.
- Finite missing Thermal, nonfinite heat/fuel refuse; no implicit legacy fallback after malformed finite admission.
- Old normal Campfire at cold/zero fuel and thermalless authored station still cook; no flag default and old-wire load both remain legacy.
- Factory callbacks remove CampfirePart, replace same-looking Thermal/Fuel, move source to another adjacent cell, cool/exhaust it, or toggle flag; selected original is refused with exact raw/result quantities and callback state preserved. One unchanged callback positive. Keep focused; do not invent a general arbitrary part-graph taxonomy.
- Exhausted first neighbor + usable second neighbor succeeds with the actual selected station. Distant/missing source controls already exist, retain them.

Then current-flow and state evidence:

- Actual MaterialSim ticks take fresh opted-in500 source through the150 boundary while fuel remains unchanged and no Burning appears. A later actually Burning source variant would require its own exhaustion check; no such source variant is activated by this core milestone.
- Same whole-stack quantity/capacity/merge and thrown AfterInventoryAction rollback from existing suites; no new mirrored transfer tests unless a seam actually changes.
- Full save before/after cooling/exhaustion: exact source identity/flag/temperature/fuel and input/output units; retained away graph return does not reconstruct hot source. Old authored source retains legacyflag.
- New source hot/cold/exhausted Examine and render/flicker/ember/light controls, invalid/hidden/stale owner pairs; same-camera native pixels viewed after actual cooling. Imported cold/hot asset state is a required gate, not source-code inference.

Relevant unchanged neighbors: `DensityEverydayTests`, `DensityEverydayAdversarialTests`, `DensityEverydayIndependentReviewTests`, `DensityCookingReceiptReviewTests`, `DensityTorchLightTests`, `DensityTorchAdversarialTests`, `CampfirePartTests`, existing `BurningEffectTests` and thermal/material tests. Use the precise changed subset initially; root runs native/full gate.

Native short witness after source/art integration: normal player with real carried raw ingredient, actual inventory Cook against the generated opted-in owner, exact consumed/output counts and one ordinary paid action; inspect the station then wait actual local material turns to cool, repeat Cook and see refusal with ingredient intact. Save/reload/return the same cooled source. A staged source/ingredient setup, if needed for finite coverage, must be labeled and cannot be called ordinary acquisition. One safe outside path, no heat/stat/HP writes, no relight on reload and no source reroll.

## 7. Q1–Q4 / readiness

- Q1: admission and post-factory revalidation use the same state and exact references. Hot/cold/exhausted readout follows the same helper. No borrowed source rollback or cooldown reset.
- Q2: inventory150 is newly selected from the ground-recipe precedent; it is not FlameTemperature or the torch ignition gate. Free rest and old stations preserve policy. Material ticks, map clock and rest clock remain distinct.
- Q3: hot/cold, fueled/spent, finite/legacy, malformed/current, callback unchanged/changed, direct/outer transaction and old/new save branches have named paired gates. The private30-case RED and its blocked-by-missing-API bounds are recorded above; native execution remains pending root publication; the private core now passes its paired checks.
- Q4: cooling window is finite stored heat, not200 turns/fuel charges, real Burning, an infinite cooking fire, or offscreen expiry. Wilderness source is a deliberate one-owner utility addition because audited ordinary Spread producers offer no relocatable Campfire. Selection/realization/native visual gates remain unrun.

Root decision now sufficient for the small core fixture: explicit opt-in and150 accepted. Remaining content/art details (source name/initial authored values/cold mesh) belong to the F10 integration slice before activation, not a reason to rewrite thermal/fuel/rest systems.

Resumed checkpoint: see `RESUME-SWEEP.md` for concrete existing healing benefit, the Emberwheat-not-cookable correction, source availability, and finite-only hot/cold cue requirements. No new production or test execution in that checkpoint.


## Current grain milestone and timing boundary

The new independent FoodItem `ToastedEmberwheat` preserves one unit per raw unit and weight1, adds one d4 to healing (2d4→3d4), and changes generated commerce value10→12. These are base authored values, not quoted player sale income. It has no Cookable part; there is no repeat-toasting cycle. Existing RipeCropRow YieldCount1 and all placement/food budgets are unchanged. No new recipe is retrofitted onto existing saved objects.

A fresh source reread found only one production call to MaterialSimSystem.TickMaterialEntities: InputHandler.EndTurnAndProcess at1002, after actual player EndTurn and ProcessUntilPlayerTurn. TurnManager's global tick and per-actor EndTurn do not each call the scenery material pass. Thus the computed67 default-decay passes and optional267 slower-decay passes are neither world TickCount nor an unconditional number of player inputs. The executed test directly calls material passes; actual source arrival/travel/menu/scheduler timing remains a native activation gate. No decay coefficient is changed here.
