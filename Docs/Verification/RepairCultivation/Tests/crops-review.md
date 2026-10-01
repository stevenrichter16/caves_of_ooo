# Cultivated crop runtime implementation review

Status: implemented; reference runner passes, root owns native verification and content/art integration. No commits made by this agent. Exact files/hashes in owned.json.

## Contract

- SeedPart.RequireCultivatedSoil defaults false. New content opts in. CultivatedSoilPart is a saved marker on the existing exact Terrain+Plantable owner. It changes no geometry and needs no save-version change. It refuses carried/solid/foreign/barren/flooded terrain. No player till tool in this slice.
- CropPart.HarvestAtMaturity defaults false; new crops opt in and use GrowthStage 2 as standing ripe. StageGlyphsRaw/StageColorsRaw index2 supplies maturity. SeedYieldBlueprint/SeedYieldCount defines a returned genuine seed targeting this crop. Legacy auto-drop behavior remains.
- HarvestCultivatedCrop joins PerformInventoryActionCommand's InventoryTransaction. Complete fresh output batch is validated before world mutation; exact source/parts/recipe/actor context is rechecked after factories; all physical output is placed at the source cell and the exact parent removed together. Rollback removes the products and restores original crop identity, cell and ordering. All outputs remain on ground for ordinary pickup even when inventory has space; full inventory is therefore harmless. Per-harvest unit cap32 prevents malformed huge counts.
- New seedlings still use ordinary Conjure Rain and active-area player-turn cadence. Ripe crops stop growth but remaining moisture dries normally. Rain is not supplied automatically by wells. No offscreen growth or perishability is promised.
- PlantSeed now claims actual seed/actor, rechecks planting position/soil after factory callbacks, validates a fresh actual crop, and joins the outer command receipt. Post-action failure restores consumed seed and removes new plant.

## RED/GREEN evidence

- initial-red.xml:13cases,3controls pass/10fail. Eight feature failures and two confirmed audit bugs: malformed noncrop target consumed seed; failed/partial yield placement lost parent.
- first-green.xml:13/13.
- adversarial-red.xml:51cases,47pass/4fail. Four confirmed bugs: ripe moisture froze; later initializer could corrupt earlier output ownership; plant callback moved actor but planted abandoned cell; after-action throw restored seed but left crop.
- adversarial-green.xml:51/51.
- batch-identity-red.xml:7callback cases,6pass/1fail. Later initializer duplicated earlier output ID.
- farming-final.xml:150/150 across9fixtures, including53new cases and97prior farming cases. Final batch check now revalidates all IDs, ownership, blueprints and returned-seed targets.
- Actual TurnManager.EndTurn test verifies NPC End does not tick growth and player End does. Save tests cover stage0/1/2 and soilmarker. Native scheduler/UI/pixels remain root acceptance responsibilities.

Reference runner needs a scratch-only UnityEngine.TestTools.LogAssert shim for two older fixtures. It exercises their gameplay assertions but does not enforce native expected editor logs. Legacy no_factory and unknown_blueprint diagnostics plus original unknown-blueprint Unity error call are preserved. The shim is not a repository change. Native Unity remains authoritative.

## Counter-checks / adversarial surface

Standing and legacy maturity, dry/wet, NPC/player/away cadence, plantable/cultivated, ripe/unripe, duplicate harvesting, full capacity, wrong/current parts, distant/dead/detached/foreign actor, source move/ID replacement, output mutation after initial creation, malformed/bounded counts, missing/wrong returned seeds, barren/wet/nonplantable/foreign terrain, repeated in-flight action, explicit rollback and real AfterInventoryAction throw. Mutation probes run through real factory hooks and the public command executor, not fabricated success flags.

## Q1–Q4

Q1: compared planting and harvesting transaction order against CookingService and the outer PerformInventoryActionCommand. Claims precede callback-bearing construction, complete batch preflight precedes placement, scoped undo precedes mutation, success prose/diagnostics publish after commit. Legacy and explicit harvest share the same yield helper rather than divergent loss behavior.
Q2: stable legacy diagnostic reasons retained; cultivated state uses the existing fields and root agreed command. No inventory-capacity special branch or silently packed seed. Remaining rainfall and local cadence agree with old crops.
Q3: counters cover feature opt-in/off, stage0/1/2, positive/missing/wrong yields, normal/changed factory sources and command commit/rollback. Dedicated39-case adversarial fixture covers authority, recipe bounds and callbacks. The14-case ordinary fixture includes end-to-end lifecycle and legacy controls.
Q4: actual API matches root handoff; scene/stock/content and models remain root-owned. Old documentation asserting two stages/no harvest is corrected in CropPart; CropSystem old actor-cadence comment now states player rounds. Existing metadata unchanged; four new source metas use fresh GUIDs.

## Accepted limits / no further edits planned

No seasonal/offscreen growth, generic till tool, extra farmer AI, inventory autopacking, crop spoilage, or automatic old-save crop migration. Reusable marked soil is preserved after harvest. The root's site/save-retention design must preserve the authored beds; this runtime does not promise that arbitrary generic ZoneManager.UnloadZone retains discarded graphs. New content and native pixels are not yet verified by this agent. No outstanding significant runtime finding after the seven confirmed fixes.

## Additional root-requested native route

Added ReferenceGladeNativeRepairCultivation.cs (+meta) only; root integrates its flag/launcher/report. Twenty feature checks plus the ordinary base start; fourteen planned screenshots. One disclosed actor travel shortcut enters the real generated Overworld.2.6.0 field. All gather/repair/drink/gate/harvest/pickup/plant/book/rain/wait/save/load actions use existing keyboard helpers. Rain is captured before FX completion and again as wet soil. Dynamic actual crop durations/yields are observed, no injected advancement. F5/F6 compares exact repaired state, door state, all remaining crop stages/progress/moisture, prepared terrain identities, carried IDs/quantities, player HP/tick/energy and absent spent source IDs/blueprints. Native runtime is not exercised by this agent; root owns that acceptance. Code remains free of seed grants, source relocation, actor heals, world-state outcomes or forced renderer reveal.

## Crop utility integration follow-up (root request)

Added six CultivatedCropUseTests with native Objects.json and BrewRules.json, using the public inventory command executor: hearthbulb stack cooks at actual SpreadCookingCoals and eating one roasted bulb heals 2d4; absent fire refuses without consumption; seamleaf resolves through brew_mending, makes a real carried tonic, and drinking consumes/heals; unsupported bulb+sprig mixture refuses without spending either; two-sprig batch refuses away from still and succeeds beside actual AlchemyStill; actual town provisioner and SpreadSeedKeeper factories open with all three new seeds in exact quantities and the keeper still stocks WateringGrimoire. Borrowed brewing registry dictionaries/order/initialized state and cooking factory are restored. Stock uses existing HaulingContentScope.

The first attempted cooking fixture used bare Oven, whose blueprint lacks a cooking part (real authored kitchens add it). Replaced the fixture with shipped SpreadCookingCoals, which has actual ready heat/fuel; no production bug claimed from that fixture mistake. All six use tests pass.

Existing MorrowfastMerchantRestockTests exact shelf pins went RED in 12 cases because authorized content added materials/seeds. Updated only the expected arrays: mender adds four SalvagedTimber and three KnotflaxCord; provisioner adds two of each new seed. Both the live stock tables and the factory-less fallback declare those amounts. crop-uses-merchant-red.xml:24 cases/12pass/12 expected pin failures. crop-uses-merchant-green.xml:24/24, including all six new utility cases and 18 prior stock cases. No runtime production changes in this follow-up. Assets frozen for root native verification.

## Native regression correction after root combined sweep

Root exported final-regression-result.json: 915 tests, 912 passed, 3 failed. Two PlantedCropArtTests witnessed legacy crop render invalidation mislabeled CropHarvested; the diagnostic already retained CropMatured, but the independent render hook did not. Fixed only that hook label to branch on actor-null, preserving CropMatured for automatic legacy maturity and CropHarvested for explicit cultivated harvesting. The third RED was the existing SpreadSeedKeeper five-unit shelf pin; updated its conditional total to eight while retaining five for SpreadWaysideCook and every other assertion. The six new utility tests already pin exact identities and counts of new seed stock. No old behavior pin was weakened. Root owns native GREEN rerun. Assets frozen after the two-line correction; no additional changes.
