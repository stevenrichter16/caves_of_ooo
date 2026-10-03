# Living fieldwork: water, timber and usable ground

Status: implemented; native regression, ordinary-input journey and independent review complete. Baseline: main `407111a69`, 2026-10-02. Original Caves of Ooo design, not Qud source parity. The user authorized the prompts in [EXPLORATION-DEPTH-NEXT-PROMPTS.md](EXPLORATION-DEPTH-NEXT-PROMPTS.md) and their autonomous execution.

## 1. Brainstorm result and scope

The connected region already supports a repaired well, cultivated crops, a paid meal, ink and local gathering consequences. Its next missing connection is mundane material use: carried fresh water cannot grow a crop, and a working wooden passage does not currently compete with the physical material/obstacle it requires. This milestone connects those existing systems and introduces a visible working garden at spawn. It is not a new catalogue of enemies, another delivery quest or a claim of whole-campaign replayability.

Selected additions:

1. **Mundane irrigation across prepared cultivation.** Spend one unit from a selected carried water vessel to tend one planted crop. This gives repaired wells, finite water sources, Drawgourd/Sunbladder/Raingourd shells and clean-water flasks a shared useful consequence in every biome where existing prepared cultivation works.
2. **A persistent working-yard passage.** The glade's existing north shelter wall gains one new buckled wicket in fresh worlds. Restore it with two salvaged timber to open/close a genuine local shortcut to a Drawgourd bed. Walking around remains possible. Breaking the gate through ordinary destruction remains a distinct, permanently open alternative. Some existing wilderness field passages receive the same repairable variant under new-world admission, so learning transfers beyond the showcase location.
3. **Finite physical timber reclamation.** A new heavy timber pallet in the existing glade cellar can remain whole and be hauled as a solid load, or be dismantled into two actual salvaged timber. Dismantling removes that same blocking owner; it does not leave a reusable material dispenser. Existing timber sources and Sealbark preparation remain alternative supplies.

The local loop is: discover the garden/wicket, harvest its ripe Drawgourd for a real empty vessel and seed, retrieve cellar materials, choose well versus kitchen clay expenditure, choose timber versus retaining a movable obstacle, restore or break the wicket, fill/tend/replant, leave on another ordinary expedition and return to the actual progressed crop and changed passage. No rewards are paid for following a prescribed order. The two clay uses remain optional; the public finite basin and already-supported other water sources remain valid.

### Alternatives considered

| Candidate | Decision |
|---|---|
| More crops/enemies | Reject for this milestone: 35 cultivated species already exist. Deepen their utility first. |
| Grain bait for the Reedback | Defer: it already flees close approach and consumes only one bounded row; feeding it adds little practical choice. |
| Predator diversion with meat | Good future encounter addition; requires a new authoritative bait source and AI priority contract. It is not necessary for the fieldwork loop. |
| More Curation deliveries or a loan rack | Defer: a courier, filing, cabinet and rental systems already exist. Another hand-in/rental service would duplicate current decisions. |
| Curation nonlethal quarantine transfer | Keep as a future focused encounter. It deserves containment/AI/saved-owner acceptance rather than being a side feature here. |
| Flooding, bridge simulation or weather irrigation | Defer: those are new environment systems. The proven wicket/door and carried-water contracts supply the current change. |

## 2. Verification sweep and corrected premises

| Premise | Actual source correction | Consequence |
|---|---|---|
| Water poured onto a crop irrigates it. | `LiquidVesselService` creates a `LiquidPoolPart`; `CultivatedSoilPart.IsCultivated` refuses flooded beds. Only crop watering currently supplies moisture. | Add a distinct tend-crop action. Never create a pool/coating or alter generic pouring. |
| All vessels use one quantity model. | `WaterskinPart` stores clean-water `Charges` (Drawgourd uses it, capacity 3); `LiquidVesselPart` stores named liquid/`Volume`. | Revalidate the exact singleton carried owner and debit one unit from the correct representation; arbitrary vessels require exact pure `water`. |
| Soil stores water independently. | CultivatedSoil is a saved marker, not a moisture reservoir. `CropPart` owns moisture and `CropTime` owns elapsed growth. | Tend planted crops only. Empty-soil irrigation is outside scope. |
| Watering can blindly snapshot then reconcile. | `CropPart.Water` first reconciles elapsed time, and legacy maturity may remove the crop. | Resolve previously elapsed growth before committing fresh supply; revalidate current owner before debit. Rollback of new irrigation must not erase independently elapsed world time. |
| The current repair gate blocks a shortcut. | Existing timber-gate-frame describes a damaged gate hanging open. | New buckled-wicket blueprint and recipe. Never change the old gate's promised behavior. |
| A generated passage is guaranteed nearby. | Ordinary passage installation needs an exact hedge source and measured safe detour; optional sources may refuse. | Guarantee the authored glade garden/wicket in the new glade variant. Wilderness variants remain source-bound, with no guaranteed frequency claim. |
| The glade wicket shortens regional travel. | Existing wall x43..50/y6 has local north/south approaches; the ordinary well-to-north-border route can bypass it elsewhere. | Advertise a local working-yard shortcut only. Place the useful garden north of that wall, and measure its actual bypass. |
| Garden46,4 is already empty. | It intersects seeded gravel dressing. | Reserve it in the new plan before decoration, not by deleting already-generated rubble. |
| Bumping manifest version preserves v11 automatically. | Restore enumerates historical versions then CurrentVersion. | Explicitly admit v11 while introducing v12; preserve literal old content and missing-manifest behavior. No format columns added. |
| A solid pallet supplies sight cover. | Physics.Solid blocks movement; line of sight instead uses the Solid tag/closed doors. The low pallet has no Solid tag. | Describe a movable obstacle, not sight cover; preserve the existing collision/LOS distinction. |
| Dismantling a dragged owner needs a second cleanup system. | Zone.RemoveEntity releases actual drag links. | Reuse that lifecycle; test complete rollback including grip/speed if the enclosing inventory action rejects. |

Inspected sources: `CropPart`, `CropTime`, `CropSystem`, `CultivatedSoilPart`, `SeedPart`, `WaterskinPart`, `WaterVesselService`, `LiquidVesselService`, `HarvestablePart`, `InventoryTransferSnapshot`, `RepairablePart`, `DoorPart`, `DragSystem`, `ReferenceGladePlan/Builder`, `GleanersCellarBuilder`, `SpreadExplorationPlan/Passage`, native input and existing 3D libraries. Earlier connected design and biome crop docs remain the record of their own shipped behavior.

## 3. Irrigation contract

A world action on an exact current planted crop names each eligible carried water owner separately: `water with <vessel>` and its finite cost. The command identifies that actual vessel ID; confirmation cannot silently select another vessel after inventory changes. `CropWateringService` exposes `MoisturePerUnit=40`, pure action enumeration/recognition and transaction-joining execution. `CropPart` supplies the action and readout; the existing world-action transaction dispatcher charges one ordinary action only after success.

Requirements: living, actionable player physically in the current zone; same-cell/adjacent crop with a real prepared, plantable, unflooded bed; unripe valid crop; singleton carried vessel with exact inventory/backlink ownership; positive valid quantity, and exact `water` for arbitrary-liquid containers. Reject aliases, replaced owners, equipped sources, two incompatible vessel parts, empty/corrupt/mixed liquids, wrong zone/range, malformed/stale commands and a no-benefit crop already at 40+ moisture. Old crops on bare ground retain their existing rain-only behavior. Prepared legacy crops may be tended, subject to normal elapsed reconciliation.

One successful action tops that crop to 40 authored moisture units, matching existing per-crop rain allowance. It consumes exactly one drink/volume unit, does not stack extra moisture, does not advance growth immediately, preserves the wet-time remainder and does not create any pool, coating or free harvest. Existing growth while away continues in retained graphs. A ripe crop does not need watering. Querying menus never reconciles time or mutates supply.

All new payment/state changes join the outer inventory transaction; refusal or outer failure restores vessel quantity, crop wet state and presentation. Already elapsed growth is not rewound by a failed new action. When reconciliation retires an old automatic-yield owner, no new water is charged to that missing crop. Selection and success/refusal diagnostics must identify the actual crop and vessel.

**Tradeoffs:** water spent tending cannot also be drunk; drawing from the finite basin spends its real stock; repairing the well establishes a renewable source but consumes the same initial clay that could repair the kitchen pan. Rain remains the area option and keeps its current rules. No thirst pressure, crop death, upkeep timer, XP farming or new resource registry is added.

## 4. World content and lasting geometry

Fresh enabled manifest v12 opts into an authored glade variant. Existing default `ReferenceGladePlan.Create`/builder callers remain legacy unless explicitly opted in. At (46,6), replace the planned wall specification before owner creation with **GleanersBuckledWicket**. Preserve flanking walls, north/south approaches and all unrelated props, the existing well, notice, fire, grain, stairs and public roads. Reserve (46,4) for one real prepared Drawgourd bed and initially ripe existing DrawgourdCrop; its existing harvest produces the actual empty shell and seed, not an extra vessel grant. Verify ordinary access to the bed before repair, not just connectivity in a theoretical graph. Examine/notice prose explains water use and the cellar's finite timber.

The wicket uses ordinary DoorPart, Wood composition, a new `timber-wicket-hinge` recipe costing two SalvagedTimber, and destructibility with no timber reward. While buckled, normal opening is refused. Repair permits ordinary open/close; opening changes the actual path. Destruction removes the barrier through the existing combat/destruction path and grants no repair state or reward. Later closure after repair must obey native occupied-cell/door rules. The old hanging-open gate and its recipe remain unchanged.

Use the same blueprint for a deterministic subset of admitted FieldPassage situations in fresh v12 worlds, reusing their exact hedge source, original owners, route-preservation proof and safe bypass. Preserve the ordinary v11 field gate and old manifest behavior. Refused placement cannot create materials, consume unrelated loot, overwrite authored sites or reroll the same graph. No additional family or population allocation is needed.

Place one **GleanersTimberPallet** on a verified reachable dry cellar cell, out of the stairs/mandatory return. This new owner is a declared finite material budget, not stolen from old supplies. Weight 90 / Strength 12 is a haulable solid load. It blocks movement, but its low boards do not block sight; it is not advertised as cover. The actual generic Harvestable transaction dismantles it for exactly two SalvagedTimber; the UI says `dismantle for timber (2)`, while existing harvest labels keep their defaults. A full pack places recoverable output at the source via the existing overflow contract. The pallet has no separate Destructible/yield path; dismantling is its explicit finite removal action. Do not block the only exit or imply cross-zone hauling: the actual existing drag system releases on zone transition.

Versioning gates require both Enabled and v12. v11/v10/missing-manifest saves keep prior plans and literal retained graphs; no retrofits. Newly accepted glade/cellar/situation owners use their established retained save graphs. New actions work on eligible saved crops/vessels; adding an action is distinct from modifying saved world content.

## 5. Presentation and content budget

Budget: two new gameplay blueprints (wicket, pallet), one repair recipe, one existing Drawgourd crop/soil placement, four original static model states (buckled wicket, repaired closed, repaired open, intact pallet), concise factual notice/examination text, and native action feedback. Existing wet/dry/growth crop models, gourd vessels, timber portables, humanoids and animations are reused. No decorative geometry grants gameplay authority. Orientation follows the real DoorPart; removed pallet/gate owners disappear normally. Use the established source cuboid/import pipeline and palette, with imported-model tests and actual normal-camera inspection.

Planning readiness: 🟢 material repair, harvest transactions, doors, exact inventory ownership, retained crop clocks, save graphs and voxel import; 🟡 shared watering and new deterministic plan gates; 🟡 inherited source geometry for wider wilderness variants; 🧪 unaided discovery, economic balance and campaign-scale depth remain playtest questions.

## 6. Delivery batches and acceptance gates

1. **Shared irrigation:** observed RED for offered/actual action and real debit/growth; implement helper/dispatch/readout; paired empty/wrong-liquid/ripe/no-benefit/range/cancel/rollback/legacy counters and native save. No full framework rewrite.
2. **Material and generation:** observed RED for fresh-world content, actual recipe/blueprint, old-v11 compatibility and real bypass; add finite pallet label/state tests, candidate source/refusal/once-only counters, hauling and inventory overflow. Root integrates Objects.json surgically and records parsed changes. Update version pins only when they intentionally name the current version.
3. **Presentation and ordinary route:** original models and actual owner-based routing, import and inspect. Extend the isolated native input launcher. Start with an ordinary chosen build, harvest the real vessel/seed, get actual cellar materials, repair/fill the actual well, tend the planted crop, use the wicket/bypass and leave/return; demonstrate regrowth and F5/F6. Gameplay uses normal keys; editor execute_code remains read-only. Include a native repair/destruction alternative or controlled native fixture with honest setup distinction.
4. **Review and delivery:** affected native Unity regressions, independent adversarial/Q1-Q4 review, significant fixes, actual screenshot inspection, living evidence and discovery directions. Preserve saves/editor settings/unrelated files, commit verified batches, fetch/rebase and push main.

Core completion means each selected addition is reachable, functional, saved, visibly readable and tested with counterchecks. Merely adding a definition or passing a helper test is incomplete. Comparative performance, unaided discovery and long-session balance are not claimed by these gates.

## 7. Implementation and evidence ledger

Implemented and verified:

- Irrigation uses exact carried Waterskin or pure-water LiquidVessel owners, one unit per paid action, 40-unit top-up, old-time reconciliation and transaction rollback. Menus show the selected vessel's remaining supply. Source part, native InputHandler, save and malformed/stale-owner cases are covered.
- New enabled worlds use manifest12. Glade plan reserves the bed before dressing, replaces the one planned wall cell with a closed buckled wicket, and retains all other owner specifications. Explicit11 restore is preserved. A stable subset of admitted source-bound wilderness passages gets the repair variant.
- The finite90-weight pallet is reachable in both cellar orientations; generic Harvestable gains a saved display label without changing its command. Observed outer rollback lost the actual hauling grip; the narrow source-rollback fix now restores the same reciprocal parts and only their captured speed penalty.
- Four new original models extend the14-form Connected Spread source to18. Wicket/pallet owners use mutable views; real repair/door/removal states and QuarterTurns select them. All14 previous source models and the shared24-color palette remain unchanged.

### Observed RED and first GREEN

Initial native job `3b189c5553b54c8ca6a39163e5462de2` completed73 cases, recording missing content/models, unchanged action label and two lost-grip rollback failures before production edits. Its failure list caps at25; it is not a complete failing-count receipt. A separate39-case watering run (`9c82b6eeff7d44ff90483770d9c161cc`) observed missing offered actions, failed actual water use and unreconciled growth before the service was published. Two initial fixture compile errors used internal presentation/entity APIs; these were corrected to public routing/cell assertions before those meaningful RED runs.

The first integrated131-case native run completed with128 passing and3 failures: two diagnostic assertions accidentally included old buffer events with reused entity IDs, plus an existing hoist missing from the exhaustive hauling table. The diagnostic fixture now uses fresh trace IDs. All new pallet lifecycle, model state/orientation, ordinary input payment, crop-save and generation tests in that run passed. The broader affected regression selection and later cold-eye cases are recorded separately below.

Python source geometry tests observed missing forms RED, then passed6/6 after bounded geometry corrections. Blender oblique/top contact sheets were rendered and viewed; Unity imported the reviewed hash using the unchanged shared material. These are static art checks, not the in-game camera acceptance.

### In-phase self-review

- 🟡 Fixed — synthetic failed dismantling restored source/stock but lost its reciprocal hauling grip. Narrow Harvestable rollback restores the exact valid grip and captured penalty. Successful removal still uses Zone.RemoveEntity's ordinary release.
- 🟡 Corrected scope/text — solid movement is not sight blocking. The low pallet is a movable obstacle; no new sight-cover system or Solid tag is introduced. This corrects the initial cover wording.
- 🟡 Corrected text — the same wicket can appear in wilderness passages. Its shared examination must not invent a cellar or crop there; local garden/cellar directions belong only to the authored glade instance. Repair diagnosis remains state-dependent.
- 🔵 Test harness corrections — internal API usage and stale diagnostic-buffer records were fixed without weakening gameplay assertions. The older hoist receives its missing exhaustive Strength boundary.
- 🧪 Deferred — unaided discovery, water economy, repeated-expedition appeal and campaign-scale balance require human playtesting. Native scripts verify reachable choices and consequences, not these qualities.

### Discovery directions

Start a new world for the authored fieldwork layout. From the starting glade, go north toward the ruined working shelter and well. The Drawgourd bed is just north of its short wall; the buckled wicket occupies that wall. Walk around the west end to harvest before repairing. The supply cellar stairs are in the western ruin. Its far store contains the existing clay crate and the new timber pallet; the longer service passage still avoids the direct approach. Return with timber and clay, restore the wicket/well, fill the harvested shell, and use the crop's world-action menu to water its replanted seed. Crops need continued moisture and real elapsed travel; watering does not instantly make them ripe.

Existing saves keep their original physical plans. Hand watering still becomes available on their eligible prepared crops with carried clean water. It does not retrofit a new garden or replenish spent supplies.


### Native regression and visual review follow-up

The affected131-fixture native sweep completed2392 cases:2387 passed; five failures were two overlooked current-version12 assertions, the older hoist's missing haul-table row, and two pre-existing additive scene inspections combining both scenes' global2D lights. The three assertion/table cases were corrected. Scene inspection now temporarily disables only other loaded active global lights, borrows an already-loaded scene rather than closing it, and restores exact enabled flags/context in finally. It neither edits saved scenes nor suppresses error logs. An intermediate safe-scene attempt skipped Unity's temporary runner scenes and was replaced; those skips are not counted as passes.

Final targeted content/art/manifest/hauling/review run:106/106 native cases passed. Final scene run:4/4 passed with original SampleScene restored clean. All664 existing parsed blueprints remain identical; only two new owners were added (666 total). All14 previous source models remain identical; four new forms were added.

First ordinary native trip `c6f535d56d3041239e5244521ff6bec3` passed fresh start, original yard, real bypass, shell/seed harvest and replanting, then honestly stopped at an observer route refusal: the moving cellar guard occupied the cautious waypoint's clearance. Player remained40HP. The fieldwork driver subsequently gained bounded ordinary defense with its real original dagger/finite tonics against an actual blocking hostile; no source, AI, player or clock grants were used. That failed attempt is not a completed gameplay acceptance.

Visual inspection of the ordinary camera confirmed the new wicket and planted bed appear among the native voxel scenery. It also exposed a real cue error: the HUD and door action list still offered open/close on a repair-blocked door. Ten paired native cases confirmed4 failures and6 correct controls before the fix. Both read-only projections now use the existing repair-function guard; repair and examination remain available, while repaired/ordinary doors keep their usual actions.

The final affected door, repair, hint, watering and harvest regression selection passed158/158 native cases. Independent post-GREEN review rechecked exact-vessel selection, elapsed-time/rollback ordering, both repair guards, success-only payment, and literal world/model ownership with no remaining significant source issue.

Native attempt `fd48e9f558734341ad9ea4a49902a753` passed8 checkpoints through actual pallet hauling and dismantling. It exposed a driver premise error: filling a vessel is already a **paid** action (`WaterVesselService` publishes the exact `event/WaterskinFilled` receipt), not free. Corrected both affected driver assertions to require paid fill and exact acquired vessel/source/quantity; production timing was unchanged.

Native attempt `b584d31481f244b59a368d6a0db5bcb4` passed11 checkpoints through the actual well, earned shell, first irrigation, and paid wicket repair/open/cross/close. Its unrelated Sill informant route was refused by the cautious observer. Screenshots of its factual watering menu and repaired/closed wicket were inspected. This interrupted attempt is not a completed regrowth/save acceptance.

The shared observer already supports operable doors; that refusal does not prove a production door defect. Per the user's instruction to assess importance before pursuing a side issue, the fieldwork acceptance excursion uses the established northern cold bank's real finite lichen source and still instead. The requirement is a useful ordinary expedition, actual crop unload/elapsed time/return and saved consequences. An additional Sill report is outside this milestone and is no longer claimed by its completed route. No production routing, NPC, source or timing behavior is changed to make the scenario pass.

Native attempt `ce5047fd7d764237b77f3aabf8e8d441` passed9 checkpoints through well repair and filling. An inherited audit defense then pursued a wounded retreating Marlback away from the worksite and lost its cautious path. This was a driver limitation, not evidence that the fieldwork action failed. Fieldwork now attacks actual adjacent threats without pursuing a separated target, rechecks other adjacent hostiles, and retains ordinary route/terrain checks and finite starting tonic/attack budgets. Other native modes and production combat are unchanged.

## 8. Implementation map

| Files | Responsibility |
|---|---|
| `Gameplay/Farming/CropWateringService.cs`, `CropPart.cs`; `Presentation/Input/InputHandler.cs` | Exact carried-water action, pure menu/readout, elapsed reconciliation, transaction, payment and visual feedback. |
| `Gameplay/Items/HarvestablePart.cs` | Saved finite-source action label and original hauling-grip rollback. |
| `Gameplay/World/DoorPart.cs`; `Gameplay/Interaction/WorldAffordanceQuery.cs` | Menu and HUD agree with the existing repair requirement. |
| `Resources/Content/Blueprints/Objects.json`; `Data/Repairs/Tier1Repairs.json` | Two new world owners and one timber recipe; existing parsed objects unchanged. |
| `Gameplay/World/Generation/ReferenceGladePlan.cs`; builders `ReferenceGladeBuilder.cs`, `GleanersCellarBuilder.cs` | Reserved real garden, local repairable passage, finite cellar source, factual directions and late staging validation. |
| `Generation/SpreadExplorationPlan.cs`, `SpreadExplorationPassage.cs`, `Builders/SpreadExplorationBuilder.cs`; `Map/OverworldZoneManager.cs` | Manifest12 admission, historic11 compatibility and a source-bound subset of wilderness wickets. |
| `ArtSource/ConnectedSpread3D/*`; `Presentation/Rendering/ConnectedSpreadSource.cs`, `ConnectedSpread3DLibrary.cs`; `Editor/Art/ConnectedSpread3DBuilder.cs`; four new resource mesh/prefab pairs and library | Original static forms, actual state/orientation routing, mutable owner views, retained source and review renders. |
| New `MundaneCropWatering*`, `ConnectedHarvest*`, `ConnectedTimberPallet*`, `GleanersFieldwork*`, `RepairDoorAffordanceTests` plus metas | Positive/counter/adversarial behavior, native inputs, overflow, exact source ownership, current/legacy save graphs and imported art. |
| Existing Spread manifest/pipeline/haul tests and `ReferenceGladeSceneTests.cs` | Intentional current-version pins, original haul coverage and non-destructive editor-scene isolation. |
| `Scenarios/Custom/SpreadDiscoveryNativePlayer.Fieldwork.cs` plus meta, shared driver/Connected/District partials and `Editor/Scenarios/SpreadDiscoveryNativeBatch.cs` | Isolated ordinary-key journey, bounded real defense, paid source actions, source readers, screenshots and actual F5/F6. |
| This design, prompt ledger and `Docs/Verification/LivingFieldwork` | Executed decisions, corrections, independent review, raw evidence and explicit limits. |

Script/resource/editor paths in the table are under `Assets/`; tests are under `Assets/Tests/EditMode/`. The selected additions are one connected milestone: water utility, physical materials and persistent geometry share its ordinary-play acceptance. No new biome-wide framework, enemy species or service registry is introduced.

## 9. Completed native acceptance

Final ordinary-input run **`4045caba70154a09a61ccafcd4b35ed5` passed19/19**, complete=true, failures=0, unexpectedErrors=0. [Raw report and screenshot inventory](Verification/SpreadDiscoveryExpeditions/Native/4045caba70154a09a61ccafcd4b35ed5/report.json); [verification index](Verification/LivingFieldwork/README.md). It used226 paid local inputs and two actual world-map steps. No resource/HP/position/clock grants, artificial rest loop or rain cast was used. The Duelist returned alive at15/40HP with the actual gathered frost lichen; this is a functioning route, not a claim of permanent safety or balanced difficulty.

Observed outcomes:

- The original ripe Drawgourd produced the exact shell and saved seed. Replanting consumed that seed on the actual prepared bed.
- The original finite cellar clay repaired the well; the earned shell filled through a paid native inventory action. Its first crop action spent exactly one of three water units.
- The actual90-weight pallet followed hauling movement, released normally, and disappeared when dismantled for exactly two timber. That timber was consumed by the repaired wicket.
- The direct approach to the garden shortened from8 to3 steps after opening; the player crossed the actual wicket and closed it. The separate cell-to-cell test's2-step crossing measures only the immediate north/south approaches.
- An ordinary northern expedition harvested one actual FrostLichen and read its real still. On return, the same planted crop had advanced from13 to40 moist growth units and exhausted its first watering.
- A second watering and useful well/basin circuit brought the crop to actual maturity. The second harvest produced another shell and seed, followed by a third planted owner and third watering. The finite basin remained3 units because this route examined it rather than drawing from it.
- Native F5, one real unsaved movement and F6 replaced the player/world graph and restored exact crop timing/moisture, vessel quantities, repaired door state, empty original crate, consumed pallet/forage, material counts, equipment and position.

Inspected ordinary-camera captures show the offered1-of-N watering action, intact/consumed pallet, buckled/repaired-closed/open wicket states, second ripe crop and loaded working yard. The new models remain in the native voxel scene rather than a detached art preview. The editor returned to the original clean SampleScene with no console errors.

**Can verify:** this one actual build/seed route, its measured payments and physical consequences, exact saved replacement graphs, selected native regression gates, source geometry/import state and the inspected camera output.

**Cannot verify:** unaided discovery, all seeds/builds/biomes in Play mode, human assessment of readable art/animation, comparative performance, long-session economy or campaign replayability. Native Play chose repair; controlled EditMode cases cover the destructive alternative and rollback/overflow boundaries. Interrupted driver attempts remain preserved and are not counted as complete journeys.
