# Thirty-five additional item utility passes

Status: **implemented, iterated and verified** in native Unity, following the shipped cord/clay/water tranche (`a3afce8c5`). Requested explicitly by the user on 8 October 2026. All 35 item paths are implemented; the final focused regression passed 2,005/2,005 and the representative native Play walkthrough passed 14/14 with zero errors.

## Working prompt

For each item, identify an ordinary source, its existing uses and a believable physical use in a concrete fight or preparation situation. Improve the connection between those properties and player action. Prefer existing damage, coatings, temperature, effects, food, light and movement systems. Implement the whole interaction: selection, payment/time, target consequence, readable feedback, persistence and countercases. Do not invent powers from names, add cosmetic aliases to reach a count, or imply that NPCs autonomously choose the new actions.

This is **35 item passes**, grouped into shared systems; it is not a claim of 35 independent mechanics. Shared medical treatment and food sharing deliberately preserve each item's existing payload and limitations. The qualitative Qud reference is improvisation from understandable properties, not implementation parity.

## Design decisions

- Adjacent treatment is reliable close-range aid for a willing companion, avoiding a physical throw and its collision/attack path. It costs one supply and one action, requires an actual useful effect, and never treats an enemy against its will. Existing self-use and throwing remain.
- Shared meals put existing preparation choices onto the companion taking the risk. One meal replaces that recipient's previous meal; cold resistance is not freeze immunity. No remote healing or resurrection.
- Raw materials act locally. Thin liquid films never mint a recoverable pool. Thermal/conductive outcomes use actual simulation and retain self/allied risk. Costs compete with brewing, repair, food and equipment infusion.
- Equipment actions require the exact item actually worn/held, not merely stowed. Bracing counters one explicitly physical shove/pull, not teleportation, spell damage or arbitrary scripted placement; moving cancels it. Gas fanning is bounded and cannot erase a whole persistent hazard. Light is illumination, not an invented enemy distraction.
- UI help must distinguish what an action changes from what stays dangerous. No permanent fire immunity, blanket gas immunity, magical acid neutralization or fabricated slow from Hobbled.

## Item ledger and intended situational use

| Pass | Item | Design and meaningful limit |
|---|---|---|
| 01 | HealingTonic | Directly treat an injured adjacent companion with its existing healing roll; spend your own reserve. |
| 02 | Antidote | Treat an actually poisoned companion; ongoing exposure can poison again. |
| 03 | BurnSalve | Treat a burning companion through its existing cure/cooling payload; no lasting fireproofing. |
| 04 | Panacea | Treat current ailments while preserving positive boons; do not waste a dose on an unaffected ally. |
| 05 | KnotflaxBandage | Bind a companion's actual bleeding without throwing the dressing. |
| 06 | SoddenFieldDressing | Treat both supported bleeding and ordinary poison with the manufactured dressing; do not broaden its cure set. |
| 07 | SumpsievePad | Treat ordinary and gas poisoning, retaining the pad's actual distinct cure set. |
| 08 | ClaspbeanPulp | Apply its existing bleeding cure to a companion; compete with field-meal production. |
| 09 | MargincressRibbon | Bind a companion's bleeding using the discovered Echo crop output. |
| 10 | AbsentmintLeaf | Clear an adjacent companion's confusion, preserving other conditions. |
| 11 | KnitmossPad | Spend the Grovelands healing pad on a hurt companion. |
| 12 | SootrootPulp | Extinguish/cool a companion using the existing pulp payload; compete with botanical ink. |
| 13 | CookedMeat | Share the +2 Toughness prepared meal with a willing companion. |
| 14 | ToastedEmberwheat | Share heat resistance before a fire expedition; replaces their prior meal. |
| 15 | RoastedMushroom | Share acid resistance before exposed combat; does not clean acid off gear. |
| 16 | RoastedHearthbulb | Share cold resistance; freezing still requires a separate response. |
| 17 | RoastedStarapple | Share the existing dodge preparation with the companion holding a passage. |
| 18 | FieldMeal | Share its healing and one-bleed treatment; preserve the commissioned meal's exact payload. |
| 19 | FireMoss | Kindle nearby existing combustible terrain or scenery; no fuel conjured on empty stone. |
| 20 | FrostLichen | Freeze actual nearby wet ground into a slippery route; dry-ground counter, allied risk. |
| 21 | GlacierSalt | Apply a strong cold pack to a nearby hot target; excessive cooling may freeze it. |
| 22 | EmberFruit | Use warm pulp to help thaw an adjacent frozen target; reuse thermal thaw rather than dispel unrelated control. |
| 23 | GlimmerBrine | Lay a finite conductive film to prepare a real electrical route; no free electrical attack. |
| 24 | SparkRoot | Discharge into an adjacent conductor; dry-ground refusal and real network danger. |
| 25 | PrismreedPith | Wick a finite thin coating from nearby ground; cannot drain a pool or permanent source. |
| 26 | LampOil | Spread a finite flammable slick as its physical oil properties imply; preserve fuel/brewing uses. |
| 27 | SlipsedgeGel | Lay a slippery gel film with its actual nonflammable/conductive tradeoffs. |
| 28 | PitchpodResin | Smear an adjacent target with the real sticky, flammable pitch coat; close-range risk and clear hostility. |
| 29 | Honeycomb | Smear honey as a sticky coat using actual coating penalties; gives up food and can burn. |
| 30 | Torch | Touch a held, lit torch to nearby fuel for a real fuel cost; no remote fire spell. |
| 31 | LampveinFan | Fan a bounded amount of transient nearby gas; action/hand cost, no permanent-cloud erasure. |
| 32 | GroundwireScreen | Bleed an existing electrical charge from the holder into the ground; cannot act while stunned, wet neighbors may be endangered. |
| 33 | GripfrondWrap | Brace against one physical shove/pull using a nearby solid handhold; exact worn handwear and stationary stance. |
| 34 | IronshodBoots | Plant feet against one physical shove/pull on stable nonslippery ground; heavy-footwear tradeoff remains. |
| 35 | GlowQuartz | Spend a mineral on temporary ground illumination, freeing a hand; finite saved lifetime, no hearing/stealth claim. |

## Source sweep and pruning

The original 293-item audit and the three `Next35-proposals-*.md` reports in `Docs/Verification/CombatSupplyImprovisation/` supply ordinary-source evidence and initial API checks. Each implementation group must append corrections from its exact source read before production.

Verified constraints include: Hobbled is only a DV penalty; Wet does not itself remove Burning; Acidic only damages organic material; adding tile cold requires explicit reaction resolution; many medicines already have thrown payloads; prepared meals are mutually exclusive; gas density is saved through a public backing field; normal AI is not generally light-sensitive; pre-disarm and general blind mechanics are absent. Therefore ink blindness, generic acid lockpicking, universal bait, imaginary movement slows and a new disarm subsystem are pruned from this 35-pass selection.

## Implementation prompt and order

Finish and verify the preceding three-use tranche first. Implement three bounded groups: companion aid/meals (01–18), raw material tools (19–29), and active equipment/light (30–35). Each group writes an invariant/counter test first and waits for confirmed native RED before production. Services join existing native inventory transactions and paid-action dispatch. Keep exact ownership/quantity/origin/target selection, lifecycle-aware undo, meaningful no-op rejection and after-commit diagnostics. Preserve all old recipes, source routes and self-use.

Run dedicated adversarial tests and affected regression fixtures, then exercise representative actions from every shared system through an isolated native menu scenario. Independently review cross-feature symmetry, countercases and description accuracy. Record per-pass actual outcome, evidence and any design correction here; do not count a plan as a completed pass. Add new entity models only when a real deployed object requires them. Commit documented groups and fetch/rebase before pushing main.

## Implementation record and source corrections

The three action services are `CompanionCareActions`, `MaterialFieldActions` and `EquipmentUtilityActions`, routed through `CombatUtilityActions` into the existing native inventory transaction and one-action UI boundary. Existing saves receive these contextual actions without blueprint-Part rebuilding. Item inspection appends guidance through `ItemExamineService` without removing live armor, food or tonic details.

```csharp
if (CompanionCareActions.IsCommand(command))
    return CompanionCareActions.TryAct(actor, item, zone, command, transaction);
```

- Detailed source/readiness/ordinary-acquisition ledgers: `Docs/Verification/ItemUtility35/Companions.md`, `Materials.md`, `Equipment.md`.
- A new read-only `InventoryTransaction.BeforeCommit` condition checks final targets after outer inventory observers, before wallet/payment completion; conditions do not simulate effects.
- Companion medicines preserve their existing cure sets; prepared food replaces one recipient meal. FieldMeal is a separate 3d4/one-bleed payload, not a prepared meal. Recipient rollback observes intrinsic status changes before independent lifecycle callbacks.
- Thermal and electrical reactions occur after paid commit because they can cause irreversible damage. A downstream simulation observer exception is diagnosed, not a promise that the world can be rolled backward after damage.
- General combustible films do not all burn in the tile engine: the new ground kindling uses actual oil reactions. Pitch/honey instead apply real body coatings; sticky means Agility/DV penalties, not rooted movement.
- Sumpsieve pad/crop/seed prose incorrectly excluded gas poisoning despite the actual native cure. Only those three existing descriptions are corrected. `CrackedGlowQuartz` is the only added blueprint; the parsed comparison is saved.
- Quartz uses the existing `LifespanPart` and real `LightSourcePart`; two original fractured-shard models extend the existing scenery source. All 66 previous model definitions and the palette are unchanged (68 models / 882 cuboids). No new generic timer, shader, collision or light behavior belongs to the model.

## Performance

Menus perform bounded local queries on explicit inspection/actions. There is no new rendering Update loop, AI polling service or world-generation scan. The brace receives existing movement/owner-turn events and checks its actual equipment/ground at a physical force call site; existing Lifespan handles the light. Existing tile/render dirty hooks carry visible changes. A new callback list is allocated only for a transaction that registers a final condition. No performance improvement is claimed without profiling.

## Review and evidence

Initial native RED for the three groups: `native-three-groups-red.xml`, **192 cases: 85 passed / 107 failed**. Root guidance/transaction tests had their own prior RED (41 failures); by this run those 42 already passed. Group-specific failures were missing native menu actions and missing deployed quartz/model. Actual per-fixture counts are retained in XML; already-passing invalid-condition controls are not described as confirmed bugs.

Final native regression: `native-final-regression-green.xml`, **2,005/2,005 passed**, including all **260** new item-utility cases (85 companion, 66 material, 58 equipment, 37 guidance, 6 UI payment and 8 transaction/model checks). This is a focused native Unity sweep, not a claim that every repository test ran.

First implementation verification: `native-first-green-attempt.xml` records **499 cases, 487 passed / 12 failed**. The run covered the new services, guidance, UI payment and scenery. Failures included real recipient-record/brace issues and incorrect test assumptions (innate ColdResistance25, default-expired gas poison, absent reaction registry, native player light, and an unrelated valid self-smear menu option). Each is retained in the raw result rather than concealed by an aggregate count.

The next review run, `native-review-red.xml`, records **251 cases, 241 passed / 10 failed**. Confirmed new problems were a prepared-meal or coat changed by a pre-apply observer, hidden equipment targets and a brace removal causing Hooked to tick twice. The quartz movement case initially used a StatChanged callback that direct stat-field changes do not emit; its correction now uses a real ObjectCreated callback and explicitly asserts that the callback ran. A final four-case meal replacement pair first reproduced a duplicate resistance subtraction; its narrow receipt-ownership fix now passes. `native-final-meal-review-red.xml` retains that intermediate 258/260 result.

### In-phase self-review

- 🟡 **Fixed and verified:** bind the actual recipient health/status records through late inventory callbacks; reject changed care recipients before commitment.
- 🟡 **Fixed and verified:** stage a brace inertly through normal effect acceptance, then arm it only after commitment. An already set stance survives stun but still ends on movement or loss of its physical support.
- 🟡 **Fixed and verified:** validate visible actual contacts for aggressive material/equipment targeting; avoid exposing hidden nearby objects through menus.
- 🟡 **Fixed and verified:** defer the Hooked-specific expired-brace cleanup so the reverse effect loop does not visit the hook twice.
- 🟡 **Fixed and verified:** preserve independently changed prepared meals and liquid coats when a refused item use refunds its own cost.
- 🔵 **Intentional:** adjacent aid to known, willing party members remains usable inside obscuring cover. Its physical reach and party gates still apply; strangers and enemies are excluded.
- 🔵 **Description precision:** water-coated ground is the freeze target; brine, gel and mud have no such promised reaction. Small ignition doses refuse fuel they cannot ignite. Sticky smears require an uncoated recipient.
- ⚪ **Deferred hardening:** an arbitrary custom observer that renames a held item's blueprint/ID mid-action is not a known ordinary gameplay callback. Real ownership, equipment, position, visibility and target-state changes are covered; no speculative identity framework is added.
- 🧪 **Playtest follow-up:** relative opportunity cost and encounter frequency need ordinary expedition play; controlled tests cannot establish lasting fun.

### Native Play witness

Raw final result: `Docs/Verification/ItemUtility35/Native/3c093b3f70b24b52bc7e7d7d3a4a4678/report.json`: **14/14 checks passed; zero errors; complete=true**. All eleven actual inventory actions spent exactly 1,000 energy, with ten/eleven global ticks according to the boots’ retained Speed 95. The isolated scenario enters N/Classic normally, then uses a finite controlled pack, flat floor and stationary willing companion. I/arrow/Enter invoke the real menus and pending-turn handoff. The two final physical pushes are explicit measurements. Scene/input/camera/save settings are restored, and Unity is back out of Play.

**Can verify (script-observable):** actual menu availability, item expenditure and one-action costs; companion healing and Heat 20 meal; brine/wicking; actual water freezing and warming a frozen companion; oil ignition; bounded gas fanning; finite light with approved native 3D model; first push blocked and second push allowed. Native EditMode additionally exercises all remaining variants, natural stun recovery, save/expiry, stale targeting and refusal/rollback countercases.

**Visual inspection:** `12-crystal-model.png`, `13-plant-feet.png` and `14-finished.png` were opened and inspected. The new shard model is rendered, the active brace action is readable in the inventory, and ordinary feedback identifies the ground light and one-use brace. The deliberately plain controlled floor is not evidence of natural biome composition or encounter aesthetics.

**Cannot verify (visual/feel/balance):** natural item acquisition is established through source traces, not this controlled grant scenario. This does not establish encounter frequency, autonomous NPC use of these verbs, ordinary expedition balance or long-term fun. A model partly behind the player is occluded normally; the final displaced-player frame provides an unobstructed view.

Earlier Play reports remain as raw setup evidence: starter-stack merging required an exact finite test pack; the speed penalty invalidated a fixed ten-tick assertion; a constructed gas needed its own entity ID. These were scenario corrections, not gameplay bug claims.

### Completed pass groups

| Group | Outcome | Direct native evidence |
|---|---|---|
|01–18 care/meals|All 18 existing items gain actual adjacent-party treatment/preparation while retaining prior uses.|43 ordinary +42 adversarial checks; healing and heat preparation through native menus.|
|19–29 materials|All 11 gain the specified local fuel, cold, conductive, wicking, film or sticky-coat use.|36 ordinary +30 adversarial checks; six material menu actions in Play.|
|30–35 equipment/light|All 6 gain held/worn or consumable utility with physical prerequisites and drawbacks.|21 ordinary +37 adversarial checks; fan, quartz and boots through native menus.|

Cold-eye review completed across source symmetry, shared service shapes, countercases and description accuracy. All identified medium/high findings are fixed and covered. Deferred items above concern ordinary balance/playtest and speculative external identity renaming, not unfinished listed item paths.

### Files changed

- New action services: `CompanionCareActions`, `MaterialFieldActions`, `EquipmentUtilityActions`; dynamic `ItemTacticalUseDescription`; `EquipmentBraceEffect`.
- Existing integration: `CombatUtilityActions`, `ItemExamineService`, `InventoryTransaction`, `Entity`, `StatusEffectsPart`, `SkillCombatHelpers`, `Cudgel_Slam`, `HookedEffect`.
- Content/presentation: one `CrackedGlowQuartz` blueprint and two original models; scenery source/recipe/library registration; three Sumpsieve description corrections.
- Dedicated ordinary/adversarial/guidance/UI/model/transaction tests and `ItemUtility35NativePlayer` / `ItemUtility35NativeBatch` repeatable isolated audit.
- Full parsed blueprint and prior-model comparisons are in this verification folder. All new Assets files include metadata.

## Follow-on request after all 35 passes

The user additionally requests investigation of the local decompiled Caves of Qud project, comparing its actual engagement mechanisms with Caves of Ooo and recommending concrete directions. Do this after all 35 passes; locate the local reference, inspect primary implementation and compare actual current CoO code rather than assuming parity. The research follows this completed implementation in a separate document; it does not change the verified scope above.
