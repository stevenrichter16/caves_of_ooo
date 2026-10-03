# Sodden expedition — Sumphold's southern circuit

Status: complete. 899/899 selected native Unity EditMode tests pass; the corrected ordinary PlayMode journey passes 11/11 with zero unexpected errors.

## Intent and readiness

Give the Sodden a connected ordinary exploration outing with preparation, a route choice, recoverable supplies and a changed place to return to. This is original Caves of Ooo content inspired by the user's desire for consequential exploration, not a claim of source parity with Qud.

| Area | Readiness | Decision |
|---|---|---|
| Terrain, coatings, fire and gas | Green | Use real MirePool, Duckboard, PeatBank and native tile state. |
| Repair and harvest | Green | Reuse native material recipes and finite harvest owners. |
| Crops | Green | Existing SumpsieveCrop supplies a real treatment ingredient. |
| Preparation service | Green | Add one repaired, staffed recipe and one carried treatment. |
| Placement and revisits | Green | Three exact layouts; new-world version gate and bounded retention. |
| Presentation | Green | Six original small models, including both bench states. |
| Full bog simulation, boats | Out of scope | No water-flow, boats or concealed-path mechanic is promised. |

## Verification sweep and corrections (before production)

| Premise | Source inspected | Correction and consequence |
|---|---|---|
| Sumphold is a Sodden chunk | SUMPHOLD-COMPOSITION.md; WorldMapAuthoring | Sumphold is Spread tier 1 at 15,6. Preserve it. Put the circuit in actual Sodden tier 2 at 15,7; 16,7; 17,7. |
| Any nearby address is free | WorldGenerator; existing recovery placement | Preserve the oil recovery shore at 16,6 and Drowned Ledger at 17,5. The selected three addresses lie inside authored-POI exclusion ranges. Still check the actual map and POI before admission. |
| Boots make mire safe | SODDEN-COMPOSITION.md; native liquid/gear blueprints | Leather boots provide existing armor, not wetland immunity. Describe them honestly. |
| Marsh gas is explosive; reeds conceal | Sodden terrain parts and composition | Gas poisons; reeds alone do not grant concealment. Do not advertise either invented mechanic. |
| SwampLeech, IronBoots, Sickle exist | Parsed Objects.json | They do not. Use Reedfrog/MawToad and actual LeatherBoots/Buckler. |
| A sumpsieve pad cures every poison | SumpsievePad; CureTonic | It removes ordinary PoisonedEffect only. The new dressing combines this with bleeding treatment, not gas/fungus immunity. |
| Every save should get the sites | SpreadExplorationPlan.Restore and manager | Admit fresh version 14 only. Preserve accepted version 13 explicitly when raising CurrentVersion. Cached/legacy graphs remain literal. |
| A menu action automatically costs a turn | InputHandler resource branch | Add the preparation command to the committed inventory-action path: success costs one action; failure costs none. |
| New art requires external modeling | Sodden/Sumphold voxel builders | Existing original cuboid pipeline supports the small kit. Use real current owners and real state swaps. |

## Places and journey

The addresses below are surface chunks (`Overworld.x.y.0`). From Sumphold go one chunk south to the shelter, east to the crossing, then east again to the works. Notices explain this in local terms and state real costs. No quest acceptance or map-marker chase is required.

1. **The dressing shelter, 15,7.** An inhabited stone-sided work shelter on the townward edge of the bog. Sella, a PeatCutter, works beside two genuine sumpsieve crops; she does not automatically water them. A second shelter contains a usable bed and chair. The broken dressing bench needs two SalvagedTimber. Examine text names the works farther east and the crossing between them. Repair permanently changes the bench's model and unlocks its staffed service. The service becomes unavailable if the actual bound worker is absent, dead or hostile.
2. **The cutbank crossing, 16,7.** A broken peat-work route with a direct wet crossing and a connected dry detour. Between (20,12) and (60,12), the wet route is 40 cardinal steps; the dry route is 56 (54 allowing diagonal movement), versus 40 wet steps. Boundaries remain accessible, and every edge has a reachable dry arrival route. First world-map descent selects the western shore by the actual notice; remembered local return positions keep precedence. Real mire coating supplies one acid damage per turn and minus two Agility. Real peat and boards retain native fire/destruction behavior. Heating mire can release poisonous marsh gas, so burning a route is a consequential action rather than a free bypass. A native scarlet Bandfrog occupies an island beside the shortcut. Its caustic skin can poison attackers. The placed frog uses native stay behavior on a dry island; threats can still provoke ordinary combat. The dry bow begins outside its initial sight radius. This is not a staffed checkpoint.
3. **The abandoned works, 17,7.** Broken shelters, peat cuts, wet pockets and surviving stock distinguish it from both other chunks. A finite locker contains LeatherBoots, Buckler and two KnotflaxCord. One salvage owner yields four SalvagedTimber once. Two lengths restore the shelter bench; the remaining timber still has other repair/trap uses. A MawToad inhabits an optional part of the works; required access does not demand killing it. No living work crew is added here.

Seeded local variation changes wilderness shapes around stable meaningful routes and owners. Different purposes and routes, rather than multiplying encounter packets, carry the depth. Other Sodden chunks retain their existing quiet formations.

## Preparation contract

- `SoddenDressingBench`: visible, stationary, destructible wooden repair owner with `Composition=Wood`, recipe `timber-dressing-bench`, and `SoddenPreparationPart` bound once to the actual local PeatCutter.
- Repair: consume two carried SalvagedTimber through the existing repair transaction. No repeated repair reward.
- Prepare: one SumpsievePad + one KnotflaxCord + two drams → one SoddenFieldDressing. One committed player action. Real inventory/currency capacity and worker checks apply; any refusal leaves inputs, money and time unchanged.
- Apply: a carried field dressing removes ordinary PoisonedEffect and/or BleedingEffect together. Consume one only if a supported condition is present. No HP restoration, gas-poison cure, fungal cure or advance immunity.
- Ingredients have independent existing uses. Nearby crops supply pads; initial works cord is finite. Continued preparation uses supplies grown, gathered or traded through existing systems rather than an automatic stock refill.
- Saved public owner references/scalars bind the service to the actual station, worker, position and zone. Aliases, replacement workers, teleported stations, stale zones, equipped payment items and failed transfers cannot mint outputs.

## Generation, persistence and presentation

Admission requires enabled exploration version 14+, one exact canonical surface address, actual Sodden map biome, no conflicting POI and complete content dependencies. A dedicated cold pipeline creates the layouts; final generation validation occurs after ordinary hooks. Cached graphs are never repaired, restocked or reconfigured from a plan. Retain these three bounded graphs so repair, harvest, looting, corpses and destroyed owners survive leaving and returning; save/load must preserve them literally.

New art: broken bench, restored bench, works salvage, works locker, route notice and carried dressing. Reuse original native Sodden terrain and PeatCutter models. Recipes must resolve current visible native owners and ignore removed, hidden, reskinned or foreign owners. No decorative model may imply usable geometry or a service unsupported by the object beneath it.

## Milestones and acceptance

1. Observed RED → layout, content, service and version admission. Pair every positive with a negative scope/condition case.
2. New art and native dispatch; generate assets and inspect the functional three-place composition.
3. Dedicated adversarial tests: wrong world/version/site, missing dependencies, generation callbacks, stale/duplicate/changed owners, payments, save round trip, literal removed stock, service failure and success action costs.
4. Focused native EditMode suite plus relevant neighboring regressions. A reviewed native PlayMode journey/scenario must exercise discovery, route availability, finite stock, repair, paid preparation and changed-state presentation. Keep script-observable evidence separate from visual/feel judgement.
5. Cold review Q1 (symmetry), Q2 (cross-feature contracts), Q3 (counterchecks), Q4 (documentation and intent); fix significant findings before commit. Keep docs and code together, fetch/rebase and push main.

## Implementation evidence and self-review

- RED: Unity compiler reported CS0246 for absent SoddenDistrictPlan at SoddenDistrictGenerationTests.cs:113 before production edits.
- First native run: 105 cases, 90 passed, 15 failed (work-source contract, new native mode absence, literal old-version fixture). Service core passed 49/49.
- Art RED before import: missing six real assets confirmed. Imported through reviewed SoddenDistrictKitBuilder. Whole-graph checks then identified native StoneFloor/StoneWall/Bandfrog recipe omissions; fixes reuse existing original art.
- Route review: cardinal-only testing hid an eight-direction shortcut that reduced the dry penalty to four steps. Widening the actual mire preserves all exits and raises the dry distance to 54 versus 40 wet.
- Native Apply fixture initially lacked bootstrap status effects; corrected before evaluating the intended action-cost assertion.
- Final native GREEN: 899/899 selected Unity EditMode tests pass, including 199 new cases; zero failures or skips. The corrected ordinary native journey passes 11/11. Independent source review found no unresolved significant defect.
- Scope additions after native review: first world-map descent now chooses the dry western shore, and the shelter includes usable resting furniture. Both close observed arrival/readability gaps within the same three destinations. Native gas/coating/fire behavior is reused; the dressing deliberately treats ordinary wounds rather than inventing gas protection.

## Files

The complete source/data/art/test/document inventory is [changed-files.txt](Verification/SoddenExpedition/changed-files.txt). It contains 100 files, including Unity metadata. Verification artifacts are listed separately below.

- New world owners: `SoddenDistrictPlan`, `SoddenDistrictBuilder`, `SoddenDistrict`, `SoddenPreparationPart`, `SoddenDressingPart`.
- Shared gameplay: `OverworldZoneManager`, `WorldMapTraversal`, `SpreadExplorationPlan`, `ExaminablePart`, `InputHandler`, `InventoryUI`.
- Content: five new `Objects.json` records, one repair recipe, and Sella's four-node conversation.
- Art: `SoddenDistrictKitBuilder`, `SoddenDistrictArtLibrary`, `SoddenDistrictRecipes`, six generated mesh/prefab forms with one catalogue; shared ring/presenter/voxel/visitor registration.
- Native scenario: Sodden journey partial, ordinary-player dispatcher and isolated editor launcher.
- Tests: nine new fixture files; two neighboring generation fixture updates; fifteen current-version exploration fixture updates preserving explicit legacy versions.
- Living documents: this design and the three executed working prompts.


## In-phase review ledger

- 🟡 Wet-route danger: a passive reedfrog did not justify the advertised risk. Replaced that placed actor with the existing Bandfrog beside the wet shortcut; preserved an initially unseen dry approach. No new enemy blueprint.
- 🟡 Eight-direction geometry: diagonal movement reduced the original dry cost to 44 against 40. A failing eight-direction path test led to widening the real mire: 54 dry versus 40 wet; cardinal dry distance remains 56.
- 🟡 Keeper availability: ordinary PeatCutter wandering would take Sella away from the bench. The generated keeper now has a native stay position, with normal threat/self-preservation behavior retained. Other peat-cutters are unchanged.
- 🟡 Native inventory time: actual keyboard Apply initially left the inventory open and spent no action. A native failing assertion preceded adding committed dressing Apply to the existing one-action inventory path. Before/after failures retain supplies, conditions, money and time.
- 🟡 Full-scene art: native whole-graph tests identified missing masonry and Bandfrog models. Reused existing original Sumphold/Wellmeet stone and the scarlet-black Bandfrog body; no seventh model is necessary.
- 🟡 Rollback presentation: restoring poison after a rejected treatment restored its mechanics but lost its aura. Paired successful/rollback tests preceded restoring the original aura during undo.
- 🔵 Added Sella's bounded four-node conversation: supplies, exact route and Sumphold's unresolved Concord/Bog-Taken dispute. No quest, reward or hidden progress flag. The ordinary conversation reference saves with the worker.
- 🔵 Current manifest pins across earlier exploration tests now refer to CurrentVersion; explicit legacy versions stay literal. Raising fresh admission must not silently turn a version-13 compatibility test into a version-14 test.
- 🧪 Native journey is one predeclared seed/build using known directions. Route comparisons are graph measurements, not a guarantee that wandering creatures cannot approach a previously dry lane.


### Current native evidence

- `core-native-green.xml`: 90/90 native Unity EditMode cases passed after the keeper, treatment-time and rollback fixes (27 layout/generation, 53 service/dressing/conversation, 8 actual input cases, 2 launcher guards).
- `focused-native-212.xml`: the earlier broader pass reached 211/212; its one failure observed the keeper's old wandering behavior before the subsequent compile. The final keeper assertion is included in the 90/90 run above.
- Unmodified existing blueprints: parsed before/after comparison proves exactly five new blueprint records and zero changes to existing records. New C# metadata GUIDs validated.
- Custom-map boundary: admission checks each actual destination. An externally edited map can replace a neighboring destination while retaining a local notice's authored directions. The shipped fixed map and all tested seeds keep the entire circuit. No map-editing subsystem is added here.


### First native journey and resulting corrections

The ordinary seed-64 Duelist journey (run `5676870a51ee43e2ac6f59a823051b37`) completed the crop/works/repair/preparation/save-load circuit with 10/11 checks. The crossing check failed because the Bandfrog wandered into mire and died naturally before approach. Preserve this failed run as evidence; do not call it a passing PlayMode run.

- The liquid definition supplies **1 acid damage per turn and -2 Agility** through mire coating. Earlier planning underdescribed this as wet exposure. The wet shortcut is materially hazardous without inventing gas or immunity behavior.
- Anchor the placed crossing frog to its dry resting cell with the native stay behavior, retaining normal combat/threat response. Ordinary Bandfrog blueprints retain wandering.
- First world-map entry otherwise drops into the center mire before a choice is possible. A narrow current-district arrival rule will put a first visitor by the western notice, while remembered surface positions and legacy graphs retain existing semantics.
- Visual review found correctly mapped but sparse ground, empty shelter space and overly regular reed rows. Add real, irregular shoreline colonies and native resting furniture while keeping all paths and the wet barrier intact.


## Cold review (Q1–Q4)

- **Q1 — symmetry:** preparation uses existing inventory transfer snapshots, claims and deferred currency; refusal rolls back inputs/output/payment. Dressing removal now restores effect order, mechanics and aura on rollback. Native keyboard success commits one action for both bench preparation and carried Apply; refusals commit none.
- **Q2 — consistency:** new objects are native repair, harvest, container, examine and inventory-action owners. Their graphics follow those actual owners, including broken/repaired state and removal. New Bed/Chair models use existing native furniture parts. No decorative stand-in supplies a gameplay service.
- **Q3 — counterchecks:** cover missing content, wrong address/biome/version/POI, mutated generation hooks, changed or foreign owners, unavailable/hostile worker, shortages and capacity, inventory rollback, removed/hidden/reskinned art, literal save hydration, first versus remembered map arrivals, frog idle survival versus hostile combat, and accessible dry versus direct wet paths.
- **Q4 — intent and documentation:** this is an authored three-destination outing, with seeded scenery variation; it does not claim a new region-wide procedural system. Crops use existing growth/watering and the keeper does not farm automatically. Dressings treat ordinary poison/bleeding, not mire acid, gas, fungal illness or future exposure. Independent read-only integration review found no significant remaining defect.

### Scope boundaries

- The three retained graphs are bounded; surrounding Sodden wilderness and existing authored destinations keep their ordinary behavior.
- Existing saves retain version 13 and literal old graphs. Start a new world for the new circuit.
- A placed frog rests on its dry cell; native combat can move it into danger. No enemy immunity or broad AI change was introduced.
- Native scenario evidence covers one known route/build/seed. It cannot establish blind discoverability or long-term balance.

### Implementation anchors

Admission and placement live in `SoddenDistrict.Eligible`, `SoddenDistrictPlan` and `SoddenDistrictBuilder`; the manager seals the generation receipt after ordinary hooks. Runtime state remains in actual entities:

```csharp
// Saved native state selects the new model; the plan does not recreate it.
return repair.Repaired
    ? SoddenDistrictArtLibrary.WorkingBench
    : SoddenDistrictArtLibrary.BrokenBench;
```

`SoddenPreparationPart.PrepareCommand` travels through the ordinary world-action menu and committed input path. `SoddenDressingPart.ApplyCommand` travels through the ordinary inventory menu. `WorldMapTraversal` keeps remembered returns ahead of `SoddenDistrict.TryFirstArrival`.


### Final native EditMode evidence

`Verification/SoddenExpedition/final-native-green.xml`: **899/899 passed, zero failures/skips**, 380.04 seconds in Unity 6000.3.4f1. This selected 48 fixtures, including 199 new cases: 94 generation/integration, 53 service/dressing/conversation, 38 art, 8 native input, 4 arrival and 2 launcher guards. Neighbors include current/legacy exploration manifests, original Sodden/Sumphold composition, material repair, cultivation, crop time, gas and coatings. This is a native editor run, not the standalone runner; it is a focused regression sweep, not the full repository suite.

Additional RED evidence preserves the native free-Apply turn failure, unsafe first map arrival, missing art and malformed-content failures. The shoreline/frog/furniture pass also retains separately labeled standalone RED/GREEN results (12 initial failures, then 94/94); the final native suite includes those assertions.


### Final ordinary PlayMode evidence and honesty bounds

Run [`ece4916f4d0d451ba113b061907a8bb0/report.json`](Verification/SpreadDiscoveryExpeditions/Native/ece4916f4d0d451ba113b061907a8bb0/report.json): **complete=true, 11/11 passed, failures=0, unexpectedErrors=0**, 148.87 seconds, 439 local inputs, five map steps and 444 completed player turns. The editor reported `Native capture exit=0` and restored the original scene through the existing isolated launcher.

**Can verify (script-observable):** ordinary seed-64 Duelist new game; town work-slip read; broken bench discovered; real crop harvested; dry crossing traversed while the stationed frog remains alive; native locker emptied and finite frame harvested; ground return; exactly two timber spent repairing; exactly one pad/cord and two drams spent preparing; actual broken/working/salvage models submitted; F5 → an unsaved step → F6 restores distinct loaded graphs with the repaired bench, exact worker, dressing, consumed salvage and empty locker.

**Visual review:** inspected final broken-shelter, dry-crossing, looted-works and restored-bench captures. The stepped shoreline, irregular reed stands, real shelter bed/chair, alive bandfrog, broken works and changed bench appear in the actual game view. These retain the existing Sodden palette and presentation; no new camera or biome-wide graphics overhaul is claimed.

**Cannot verify:** blind player discovery, all-build/all-seed balance, subjective pacing, or every emergent combat interaction. The live journey follows a predeclared dry route; it does not deliberately poison/burn the player. Native EditMode tests separately cover treatment, fire/gas/coatings and route geometry. The 899-case sweep is selected, not the complete repository suite.

The first failed run's report and dry-crossing capture remain under `5676870a51ee43e2ac6f59a823051b37` as evidence of the fixed frog-placement bug. It is not counted as passing.

### Reproduce in normal play

Start a new world. At Sumphold, read the work slip on the toll rolls. Go one chunk south to Sella's shelter, then east through the cutbank crossing and east once more to the abandoned works. Harvest the collapsed drying frame, take the locker supplies, return with two timber to repair the bench, then use the staffed preparation action with a sumpsieve pad, knotflax cord and two drams. Resting furniture is usable; the second crop requires ordinary watering/growth. Neither loot nor repair state resets when revisiting.
