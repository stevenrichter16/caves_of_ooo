# Predator diversion: spend food to change a hunt

Status: implemented; native regression and independent review complete; ordinary-input acceptance partial, with the bounded limitation recorded below. Baseline main `49dc8768f`, 2026-10-03. This is original Caves of Ooo behavior, inspired by systemic exploration rather than a claim of Qud source parity. The [three prompts](PREDATOR-DIVERSION-PROMPTS.md) were executed under the user's standing autonomous implementation/push authorization.

## 1. Decision and scope

Add one finite opportunity to draw an admitted furrowstalker away from its existing hunt with actual meat left on the ground. The player sacrifices useful food, places or throws it into the animal's view, and remains outside the animal's threat response. The animal approaches, visibly feeds, consumes one real unit, then resumes the original hunt with its prior information and remaining pursuit allowance. This may create time to cross cover or let the reedback escape; it guarantees neither. Throwing food at an animal is still an attack. Entering its sight or threatening it can provoke ordinary combat.

Three approaches remain meaningful: avoid the hunt using real cover, confront the predator, or spend food on a diversion. Afterward, the saved hunter cannot be baited repeatedly, spent food does not return, and the surviving prey/ordinary death drops remain their actual owners. No XP, relationship reward, healing, new loot roll, tame state or permanent safety is granted for feeding.

Accept exactly **RawMeat and DriedMeat**. The latter makes the choice available from existing ordinary starting provisions; the former competes with healing/cooking/sale and is already stocked by Wayside Cook and rolled by BeastT1 loot. Do not invent a free cache to guarantee the demonstration. Bread, mushrooms, crops, arbitrary Food parts and corpses are not interchangeable bait.

### Candidate comparison

| Candidate | Decision |
|---|---|
| Finite meat diversion | Selected: reuses actual hunting, food, throwing, cover, save and animation. Adds a new player intervention rather than another actor/loot catalogue. |
| Mechanical trap jamming with timber | Strong next material-use slice. Existing visible traps lack a player disarming action. Defer so trigger authority, payment, spent states and dedicated readable models get their own complete review. |
| Curation inspection annex | Valuable authored follow-up: use two actual compartments and a finite useful cache, preserving the same living half-set. Existing3×2 cage cannot meaningfully support this by adding a certification flag. Requires its own room design and containment acceptance. |
| Carrying a sleeping Curation subject | Reject here: DragRules rejects all Creature owners. Calm/sleep is neither taming nor cargo conversion. |
| More crop outputs | Defer: current crops already supply gas, elemental brews, water vessels, light, healing and repairs. Connect those uses to situations before multiplying species. |

## 2. Verification sweep and corrected premises

| Premise | Actual source | Design correction |
|---|---|---|
| Existing predator eats any corpse or food. | `SpreadPredatorPart.Feed/BindOwnKill` accepts only its exact native own-kill Reedback corpse receipt. | Keep that path intact. Diversion has separate saved state and eligibility. |
| Bait can safely execute when a throw lands. | `ThrowItemCommand` splits a real unit, lands it, then broadcasts ItemLanded inside an inventory action. Retriever callbacks may run before outer commitment. | Predator notices only on its next ordinary idle turn, never from ItemLanded. Normal cancelled/rolled-back actions leave no eligible bait on the following ordinary turn. |
| Meat is already throwable. | RawMeat and DriedMeat lack HandlingPart; the ordinary throw picker requires it. | Add explicit one-hand carry/throw handling with their existing weight2 to exactly these foods. Preserve food, stack and economy values. |
| Food makes an attacking predator friendly. | The hunt yields to visible outside-prey hostiles and current combat; higher Brain goals already own turns. | Preserve threat priority. The player must use placement, distance and real sight cover. |
| Visible mesh means opaque cover. | Physics.Solid and the Solid LOS tag differ. Current hunt already uses actual AIHelpers.HasLineOfSight. | Use real sight, not art/terrain labels or omniscient target coordinates. |
| A stack should disappear when fed on. | Ordinary drop can leave multiple units; throw extracts exactly one. | Consume exactly one eligible unit and preserve the same remaining stack. |
| A new subsystem/registry is needed. | Current predator Part owns saved enums/scalars/Entity aliases; BoredGoal already calls it. | Extend only this admitted role. Do not create global animal feeding, scent simulation or a new goal hierarchy. |
| Old hunts should be silently reconfigured. | Retained save Parts preserve physical state; generation manifest currently12. | New enabled manifest13 admits diversion. Missing/v12/v11/previous cached actors retain existing behavior. |
| New animal art is required. | Furrowstalker already has a distinct original rig with five native clips; RawMeat/DriedMeat have portable models. | Reuse real movement and current-owner Interact on feeding progress. Test and inspect actual visibility/removal. |
| Curation cages provide sealed concealment. | Their rails block movement but not LOS/gas. Calm is temporary and live creatures cannot be hauled. | Record as future containment constraints, not features of this milestone. |

Inspected authority includes `SpreadPredatorPart`, `SpreadGrazerPart`, `BoredGoal`, `BrainPart`, `SpreadActorContext`, `AIHelpers`, `FoodPart`, `ThrowItemCommand`, `DropPartialCommand`, `AIRetrieverPart`, `SaveSystem`, `SpreadExplorationHunt`, `SpreadExplorationBuilder/Plan`, Wayside Cook stock, BeastT1 loot, `SpreadPortableRecipes`, `FurrowstalkerLibrary`, the hunt implementation doc and the current receiving-yard canon audit. No new lore revelation or faction behavior is introduced.

## 3. Exact behavior contract

### Admission and priority

Extend `Configure(zone, prey, bool enableMeatDiversion=false)` without changing the meaning of current two-argument calls. Save `MeatDiversionEnabled`, one attempted allowance, an explicit phase, exact target reference/ID/anchor, remaining approach budget and feeding progress. Configure false or legacy missing fields means no new behavior. An enabled role is eligible only while its original hunt is valid, active and not in its own-kill corpse feeding or a terminal phase.

Notice happens only on an ordinary scheduled AI turn after the synchronous drop/throw commits or rolls back. A synthetic callback manually firing AI inside an open inventory transaction is outside this scheduling contract; no global transaction-observation framework is introduced. Notice a real current public ground RawMeat or DriedMeat owner on a paid idle action: within radius6 or the animal's shorter sight radius, within its existing12-cell home leash, visible by actual LOS, physically approachable and free of conflicting ownership. Reject carried/equipped/container-held, foreign-zone, removed, moved/replaced, burned/burning, malformed/depleted, creature, service-owned or claimed sources. Do not accept arbitrary entities merely because they have Food. Choose nearest eligible source with a stable tie-break. Bound the scan to local cells; no region/world scan or persistent global registry.

Existing visible hostile/personal retaliation, current non-quarry target, low-health retreat, party/recruitment restrictions, Calm/conversation/work/follow/active combat goals retain priority. A new threat interrupts active diversion without consuming bait or resetting the one-attempt allowance. Higher goals may suspend the role; they must never run a feeding action in parallel. Recruitment/context invalidation clears only this role's commitment, not goals, faction hostility or other work.

### Finite approach and feeding

Admission spends the hunter's single attempt. Bind exact current item reference/ID/cell; never substitute a nearby similar item. Allow at most6 paid approach actions. On each action revalidate physical identity, unchanged anchor, visibility, hazard/context and leash. Use ordinary movement; do not teleport or take a movement plus feeding action in one turn. A moved, hidden, picked-up, consumed or replaced target cancels this attempt, clears the reference and leaves all actual food alone. The hunter never follows an inventory or the food's hidden new coordinates.

At the actual adjacent source, use2 paid feeding-progress actions with existing Interact emitted while both owners are current; consume one unit on the next paid action. A multi-unit source retains its original owner and count minus1. A singleton is removed through the ordinary zone lifecycle. Do not invoke the player's Food healing or produce replacement loot. Mark the attempt consumed before publishing removal/visual callbacks so callbacks cannot consume twice. Another hunter must revalidate its claim after the first consumes; there is no duplicate consumption of the same unit.

Original hunt phase, last-seen coordinates, pursuit and hidden-search budgets do not refresh during diversion. On cancellation/completion, resume through the existing role on a later turn with that same information. Hidden prey positions must not influence diversion movement or saved last-seen fields. Preserve own-kill corpse feeding, its70% corpse roll and all independent Beast loot unchanged.

### Readability and persistence

Examine meat explains leaving food in view and staying out of sight without promising safety. Predator Look describes actual investigation/feeding/finished diversion only while its current role is authoritative; it never reveals hidden food/prey coordinates. Existing hunter clips and ground meat models show actual movement, feeding progress and disappearance/remaining stack. No floating quest arrows or fictitious menu action.

Save/load must replace the owner graph while preserving exact aliases, current target cell, progress, one-attempt allowance and original hunt budgets. Clear target references after cancellation/consumption. Zone return cannot restart the hunt or replenish food. Native actor/item turns, action cost and actual threat behavior remain authoritative.

## 4. Content and generation budget

No new creature, corpse, item, loot table, extra population roll or free food grant. Two existing foods gain explicit throw handling and factual Examine guidance. Original animal/item art is reused if native inspection supports it. Current enabled manifest13 passes a new admission flag into existing source-bound hunt placement; other families, generation salts, original owners, placement geometry and caller RNG are unchanged. Explicit12 restore remains admitted. Existing lower-version generation uses the existing default false; retained graphs stay literal.

Planning readiness: 🟢 physical food ownership/drop/throw, original bounded hunt, saved graph and art; 🟡 diversion priority, exact finite feeding, old-version gate and ordinary-input opportunity; 🧪 unaided discovery, long-session economy and all-seed usefulness require playtesting.

## 5. Implementation and verification gates

1. Observe native RED for absent enabled diversion, exact paid movement/feeding/one-unit consequence, legacy counter and fresh-world opt-in. Reflection-based fixtures may name not-yet-existing saved fields without manufacturing a fake passing feature.
2. Implement scoped role state/priority and finite source mutation; pair visible/hidden, exposed/carried, stationary/moved, Raw/Dried/other food, singleton/stack, proper/corrupt owner, safe/hazard, idle/hostile/Calm/flee/party, first/repeated allowance and complete/refused transaction cases.
3. Verify replacement saves before notice, mid-approach, mid-feed, after consumption/cancellation; same-history/different-hidden-prey comparison, competing hunters and callback boundaries. Original hunt/corpse semantics keep their own regression suite.
4. Verify current/lower/missing/cached manifest behavior, identical generation assignments/source owners apart from enabled capability, ordinary supply reach and actual art routing.
5. Use native Unity affected regressions and a bounded isolated ordinary-input scenario: actual starting provision or legitimately acquired meat, generated hunt, native drop/throw onto clear ground, ordinary NPC diversion, real food debit, return to original hunt, F5/unsaved action/F6. No source/actor/stat/time grants. If a controlled local fixture is needed, disclose its setup and do not call it ordinary discovery. Inspect screenshots/interaction evidence separately.
6. Post-GREEN independent Q1-Q4/adversarial review, fix significant findings, record minor unrelated limits, update this doc and prompt ledger, commit using CLAUDE§2.3, fetch/rebase and push main.

## 6. Implementation/evidence ledger

### Initial evidence

- Native RED job `3e76270c95a24382b04b13ed63371e67`: all59 selected cases completed, status failed. The returned failure list is capped25 and the final result object is null; no exact failure total is inferred. Raw receipt: [native-red.json](Verification/PredatorDiversion/Tests/native-red.json). Observed failures cover actual food handling/guidance, absent version13/saved admission, native launch guards, ignored food and absent feeding presentation/state. A preceding zero-test import attempt is not RED evidence; a test's inaccessible internal property was corrected before this run.
- First native integration job `5ad1dd001fd74755a2b097f9483c5e47`:72/72 completed, one reported failure in diagnostic-test isolation, with no capped failures. Actor IDs were reused between restored fixture scopes, so the negative case saw the prior case's receipts. The test must bound its observation to new receipts. Raw receipt: [native-first-integration.json](Verification/PredatorDiversion/Tests/native-first-integration.json).
- Surgical data proof:666 blueprints before/after; exactly RawMeat and DriedMeat changed. Existing part values remain unchanged when parsed; only Handling and Examinable added. [Parsed diff](Verification/PredatorDiversion/Tests/blueprint-diff.json).

### Review findings

- 🟡 Fixed before delivery: admission was initially checked before exhausted pursuit. A hunt with zero pursuit remaining now ends without accepting another cost from the player. Native paired RED job `22446d05902f479ba643809b148c5ac0` failed only the zero case; moving the exhaustion gate ahead of admission passed the focused74-case run.
- 🔵 Evidence correction: native movement must also decrease the saved diversion approach allowance; a changed actor coordinate alone could be an ordinary hunt move before notice.
- 🧪 Human fun, broad campaign depth, performance and unaided awareness remain playtest questions. Script-observable payment, ownership, scheduling and replacement persistence have native automated coverage; ordinary Play observations and their limits are recorded separately below.

### Native play progress

- Focused native GREEN:74/74 passed, zero skipped, job `0ed65514c7944e8086da59f3a089a4ab` ([receipt](Verification/PredatorDiversion/Tests/native-focused-green.json)). Includes the observed exhaustion RED correction and diagnostic scope correction.
- First ordinary attempt `db987bd6f84f47b383c88232bd03d3f5` reached the generated enabled hunt with original Duelist provisions, but refused before throwing because the hunter had stepped to48,4 within sight of the ordinary arrival40,12. Earlier F11 coordinates were correctly treated only as a candidate clue. This is an incomplete attempt, not proof of diversion. The driver subsequently gained at most two ordinary withdrawal steps within its original eight-step route limit; combat and actor positions were never edited.

## 7. Player access and acceptance boundaries

Start a **new world** for manifest13. Existing saved hunts keep their prior behavior; saved items also retain their saved Parts. In the Spread, look for the moving furrowstalker/reedback pair. Raw meat is already available from ordinary beast loot and the Wayside Cook; the Duelist starts with two dried-meat rations. Examine either food for the new use. From outside the hunter's sight, use the ordinary Throw action to put one unit on clear ground near it, or leave meat and move into cover before it notices. Throwing directly at an animal remains an attack. The animal gets one diversion attempt during its original hunt, and the food remains recoverable until actual consumption.

The test expedition uses a preselected native seed64 address, `Overworld.12.6.0`; this is evidence for a targeted ordinary-input encounter, not a universal route for arbitrary world seeds. No new map icon, quest reward, guaranteed escape, free bait supply or taming command is added.

## 8. Root cold-eye review

- **Q1 — symmetry:** compared existing own-kill corpse feeding with meat feeding line by line. Both increment progress and emit Interact while exact owners are live, then spend the commitment before removal/dirty callbacks. Corpse feeding releases the original pair because the prey died; diversion preserves that living pair and its prior budgets. The differing cleanup is deliberate. Multi-unit ground food decrements its same owner; it does not use inventory consumption or Food healing.
- **Q2 — consistency:** admission/progress/outcome records share the ai channel, top-level actor/target, phase and progress naming. Stored exact entity aliases/IDs follow the existing predator/corpse save convention. Explicit optional false defaults and literal12 restore preserve old callers and worlds.
- **Q3 — branch coverage:** enabled/legacy, raw/dried/other food, singleton/stack, public/owned/held/hazardous/hidden, stationary/moved/replaced, one/zero original pursuit, active priorities, consumed/canceled saves and diagnostic enabled/disabled have paired assertions. Additional ordinary-command commit/rollback, competing hunters, stable selection and hidden-quarry comparisons are in the native affected selection. No test treats an artificial AI callback inside an uncommitted inventory transaction as normal scheduling.
- **Q4 — docs vs implementation:** exact notice6, approach6, feeding2+1, attempted-on-admission and version13 gates match current code. The initial throw premise and zero-budget admission issue are recorded. Final native regression and ordinary Play evidence are reported separately below; source review does not stand in for either.

### Completed affected regression

- Broad native job `1bb8ed75f6e940f195fa5ef9efc1d43e`:4240/4240 completed, three uncapped failures, final result object null ([raw receipt](Verification/PredatorDiversion/Tests/native-broad.json)). These were one stale crop-text assertion and two older resident crop fixtures that emitted player events without advancing elapsed world time. Source and corresponding crop/seed blueprints matched committed HEAD; supplemental baseline reproduced exactly those failures. [Baseline comparison and corrected reference receipts](Verification/PredatorDiversion/Tests/BaselineCropFixtures/review.md).
- Corrected only the affected tests: factual crop-maturity wording and an isolated world clock advanced before player reconciliation, with NPC no-time counter and borrowed-clock restoration retained. No crop production change.
- Final native job `dd44778de9324c40832d768c8c9dba15`:104/104 passed, zero skipped ([receipt](Verification/PredatorDiversion/Tests/native-final-green.json)). Includes all87 new diversion cases and17 corrected crop/resident cases. Selections overlap; this is not a whole-assembly pass or a second4240-case run.
- Second ordinary attempt `c5343c8eea1f497fbe5253f93363d0a1`: actual withdrawal39,13 succeeded while the hunter continued its original hunt. The driver then incorrectly applied the player's sight-avoidance rule to the bait landing, excluding every desired nearby visible cell. Corrected the driver to validate empty visible bare ground and actual hazards separately from player route safety. No gameplay relaxation or owner grant.


### Ordinary-input attempt ledger

Each attempt starts a fresh isolated native world with the same predeclared seed/address, uses actual keyboard input and restores the original editor scene/save settings afterward. Failed attempts are retained as failures; the route does not reroll, reset actors or supply replacement food within a run.

| Run | Observed outcome |
|---|---|
| `db987bd6f84f47b383c88232bd03d3f5` | Initial current-sight refusal before any throw. |
| `c5343c8eea1f497fbe5253f93363d0a1` | Real withdrawal; driver mistakenly excluded bait from the hunter's sight. Corrected landing eligibility separately from player route safety. |
| `a43c113e71e94080b4ab2f5134ce5f9f` | Original ration genuinely thrown to49,7; hunter admits and approaches, but movement brings it within player threat distance. |
| `29b94d5538ce4c29becdb68cf65cf2e8` | Read-only projected route did not account for the contact path tie; same threat-distance refusal. No gameplay change. |
| `4ba8688663064506bf18c4c58aef5365` | Outward landing51,4; real diversion approach48,4→49,4, then correct threat interruption with food preserved. |
| `9b9cc9fc96de4d70a42f861bf099b4c1` | Scoped AI diagnostics identify an ambient PetDog fetching the thrown ration. The hunter switches to that dog, aborts diversion, and preserves the original hunt budget and food. This is an observed system interaction, not a feeding success. |
| `07018f0b1b704c4ebd4bcb423fbc64bd` | All current legal nearby landings fall inside the ambient dog's actual notice radius10. The bounded ordinary route refuses after8 local inputs and4 map steps, before throwing. No artificial success; further route tuning stopped as a peripheral acceptance-driver limitation. |

All raw runs live under `Verification/SpreadDiscoveryExpeditions/Native/<run>/report.json`. The latest diagnostic run records both FetchAdmission and exact SpreadMeatDiversionOutcome reason `threat-priority`; the dog is an ambient actor, not a player-commanded companion. The final route check conservatively excluded landings within a live allied retriever's actual notice radius. It does not remove the dog or alter anyone's goals/factions.


## 9. Delivery boundary and scope divergences

| Planned | Delivered / reason |
|---|---|
| Complete ordinary-input feeding, saved checkpoint and second-ration/reload sequence in the predeclared encounter. | **Incomplete ordinary sequence.** Actual original ration throw, admission and two paid approach actions are observed; the ambient dog then causes the required threat interruption. A later safe route cannot produce a legal non-fetch landing within its bound. Full feeding/one-unit consumption/replacement-save semantics are proven by controlled native tests, not this expedition. The driver remains a truthful reproducible partial audit. No gameplay priority was relaxed to make it pass. |
| New art if required. | Existing original five-clip furrowstalker rig and portable meat models satisfy the tested current-owner rendering contract. No new models or animations were necessary. Native ground/hunter pixels were inspected, but an ordinary feeding pose was not reached. |
| Affected tests only. | A broad native selection exposed three pre-existing crop fixture failures. Source/baseline comparison proved they were stale test assumptions; only those tests were corrected. No crop gameplay change. |

**Can verify (script-observable):** all87 new feature cases pass inside final104/104 native EditMode selection; original hunt and generation regressions ran in the4240-case affected sweep with only the three separately corrected baseline failures. After the final driver correction, native job `6fb81c342fc64ed0b902120980e7155a` passed6/6 isolation/presentation cases, zero skipped ([receipt](Verification/PredatorDiversion/Tests/native-final-presentation.json)). These selections overlap. Native actual-input evidence proves the ordinary build/provisions, generated capability, real one-unit throw, real diversion movement, preserved hunt history and interruption for another creature. Pure map time in reports is ordinary travel, not a clock grant.

**Can verify (pixel inspection):** root viewed the actual glade/start and generated hunt arrival, then the current ground-ration screenshot in run `9b9cc9fc96de4d70a42f861bf099b4c1`. It shows the teal 3D field, planted cover, distinct animal bodies, player and small ground ration after the log's actual throw. Current view/mesh records agree with `spread-furrowstalker` and `spread-portable-driedmeat`. The ration is small and subtle at whole-zone scale; it is not a large signpost or guaranteed unaided affordance.

**Cannot verify:** ordinary feeding pose/consumption/F5–F6 completion at this selected source, unaided discovery, universal opportunity around any spawn, long-session balance or player enjoyment. The controlled presenter tests explicitly place/reveal owners; they are not ordinary-play evidence. The native journey's failed checks remain in every raw report. 🧪 Follow-up: select a separate clearly disclosed non-conflicting hunt for human acceptance, observe real feeding and F5/F6, and assess whether the small meat model/readout needs more legibility. This is a playtest gap, not an undisclosed passing claim or a reason to change predator priorities. 🧪 No all-assembly or all-platform pass is claimed.

### Implementation map

Fresh cold hunts opt in at their existing source:

```csharp
// Existing Hunt call passes this final admission argument:
plan.Enabled && plan.Version >= 13
```

The actual call retains its existing positional arguments; this abbreviated snippet illustrates only admission. The role owns the one-attempt saved state, calls existing movement on approach actions, emits the existing Interact hook on each of two progress actions, and removes exactly one real meat unit on the following action.

- `SpreadPredatorPart.cs`: saved finite diversion, authority/priority/source checks, honest Look posture.
- `Objects.json`: surgical RawMeat/DriedMeat Handling and Examine additions.
- `SpreadExplorationPlan.cs`, `SpreadExplorationHunt.cs`, `SpreadExplorationBuilder.cs`: manifest13 opt-in and literal legacy restore.
- `PredatorMeatDiversionTests.cs`, `PredatorMeatDiversionAdversarialTests.cs`, `PredatorDiversionContentTests.cs`, `PredatorDiversionGenerationTests.cs`, `PredatorDiversionPresentationTests.cs`, `PredatorDiversionNativeModeTests.cs`:87 new native cases, plus corrected current-version pins and three stale crop cases.
- `SpreadDiscoveryNativePlayer.PredatorDiversion.cs`, existing player partial and editor launcher: bounded isolated actual-input audit, read-only diagnostics and explicit unmet checks.
- This living design, prompt ledger and `Verification/PredatorDiversion`: corrections, source reviews and raw evidence. Failed native expeditions remain under their original run IDs.

Next content priority: practical timber intervention in visible mechanical traps, with physical payment, spent trap state and clear environmental readouts. Follow that with a genuinely usable Curation inspection annex. These are recommendations for separate complete milestones, not unfinished pieces of this implementation.
