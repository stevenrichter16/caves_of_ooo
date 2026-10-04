# Enemy field medicine: a finite supply worth interrupting

Status: implemented; native regression, live checks and review complete (2026-10-03). Baseline main `2646bd5c6`. Prompts: [POST-EQUIPMENT-DEPTH-PROMPTS.md](POST-EQUIPMENT-DEPTH-PROMPTS.md). Original Caves of Ooo encounter content using existing combat and consumption; no Qud source-parity claim.

## Brainstorm result

The previous milestone made equipment families meaningful to learned skills. This milestone gives a concrete enemy behavior worth controlling: an uncommon marlback carries one real healing tonic and spends it when badly hurt. Killing or disabling it before use leaves that exact item available to recover; permitting recovery changes both the fight and the remaining loot. Medicine replaces its attack or movement for that action. The bottle is visible while physically carried and its empty cradle remains after use.

Compared candidates:

| Candidate | Decision |
| --- | --- |
| Finite enemy medicine | Selected: connects real inventory, native tonic effects, AI turns, control, equipment and death loot. Adds an observable combat decision without a new combat framework. |
| Curation nonlethal inspection | Retain next: promising distinct social play, but current cage is 3×2, creatures cannot be hauled, and doors cannot close over occupied cells. Needs a deliberate two-compartment layout and real containment proof; a certification flag would not deliver it. |
| Wellmeet third-day hospitality | Retain: an actual dialogue promise currently ends at effect expiry. A remembered host relationship deserves its own lore and saved-state milestone. |
| Gas-mask equipment | Valid content gap but follows immediately from another equipment batch; defer in favor of a new enemy decision. |

## Readiness and corrected premises

- 🟢 Native tonic use consumes actual carried units; normal death drops remaining equipment/inventory. Existing AI, skill/control, turn and saved inventory contracts can carry this feature.
- 🟡 NPC combat kits never use carried consumables. Add one explicit opt-in decision, not universal item-use AI.
- 🟡 A model permanently decorated with a bottle would lie after consumption. Two actual-inventory-selected visual forms are required.
- ⚪ No changes to tonic potency, all enemy loadouts, starting encounters, combat rewards, party automation or global crafting.

| Premise | Verified source correction | Consequence |
| --- | --- | --- |
| Tier-two Spread surface encounters are ordinary sources | Every authored Spread surface cell is tier one. `SpreadTier2` is not a reachable source on the current authored map. | Use real ordinary Spread cave floors at depth3–5, which are tier two. Preserve the starting surface/cellar and authored POIs/lairs. |
| Adding a weighted enemy row preserves density | Surface ambient rows use independent minima; an extra row adds enemies. Underground has a real pick-one DepthEncounter. | Replace the first actually rolled gleaner only in the opted-in underground branch. Preserve group counts, other outcomes and random stream. |
| Hooking KillGoal alone covers wounded enemies | `BoredGoal` can directly start FleeGoal when below its threshold. | Medicine decision in both KillGoal before retreat and FleeGoal before moving. No extra BeginTakeAction dispatch. |
| Healing can be a simulated HP increment plus later loot adjustment | `TonicPart.ApplyTo(target,user,...consumeItem:true)` consumes before payload using actual inventory ownership. | Call the canonical path with the actual tonic and injected combat RNG. No synthetic supply counter. |
| Small enemies need a newly weakened tonic | Existing HealingTonic rolls4d6+4 and caps at maximum HP. | Keep known player-item behavior; make one uncommon20HP role with one initial unit and disclose a substantial recovery. No repeated free healing. |
| Control is a new interrupt mechanic | Native status effects already block actor actions. | Respect scheduler and direct-call action checks. Prove a stunned actor cannot spend medicine and healthy/unstocked controls use normal AI. |
| New NPC names can reuse a source game's creatures | Marlbacks are original low, broad, shale-plated burrowers with salvaged tools. | New patchbearer is an original variation, preserves species anatomy/faction, and receives original harness art. |

Authorities inspected: `PopulationTable.Roll/UndergroundTier`, authored world tiers and `OverworldZoneManager` pipeline admission, `TonicPart.ApplyTo`, `LoadoutPart`, `KillGoal`, `FleeGoal`, `CombatTacticsPart`, native inventory/death flow and existing marlback model identity; `RELEASE-VISION.md`, `DENSITY-ORIGINAL-ENEMY-ART.md`, `ORIGINAL-ENEMY-REPLACEMENT-AUDIT.md`, equipment/fieldwork/predator/trap/receiving-yard living designs. Implementation logs must record any later corrections before production relies on them.

Later pre-implementation corrections: native Examine descriptions are assembled by `ExaminablePart.BuildDescription`, so integrate the live-stock `Describe()` there. `EmitInteraction` correctly refuses self-targets; use a small actor-only SelfUse event and the existing Interact clip, with pose continuity across full/empty refresh. The approved marlback palette has no coral swatch; use its amber/green medicine bottle and pale straps without changing the original body palette.

## Exact encounter and action contract

**MarlbackPatchbearer** inherits MarlbackGleaner: 20HP; no new combat statistics; inherited cutting loadout, allied assistance, learned tactics, natural MarlbackRake, faction and40% flee threshold. Override display to `marlback patchbearer`, glyph`s`, color`&w`; carry exactly one HealingTonic through native Loadout. Attach **FieldMedicinePart**, with `TonicBlueprint=HealingTonic` and `UseAtOrBelowPercent=40`. No weapon-family or natural-weapon damage change. The inherited values are authoritative; tests pin resolved content.

`TryUseMedicine(threat,zone,rng)` succeeds only for the current, living, nonplayer opted-in creature in actual combat against a current live hostile creature, at or below40% health and below full health. Require its own Brain/current zone, actual spatial owners, actionable status, no conversation, no Calm/NoFight goal or party relationship, valid RNG and an actual carried usable matching tonic. Refuse ground/equipped/foreign items, nonpositive quantities and missing healing payloads. This does not introduce a general dice-expression validator. Inspect without changing inventory or invoking effects.

On success, consume exactly one actual carried unit and apply its native payload through TonicPart. Return immediately from the AI action: no move, attack or second tactic. On refusal, preserve ordinary AI fallback. No clock edits, extra action dispatch, hidden cooldown, replenishment, permanent use flag or new effect. If an actor later legitimately acquires more matching medicine, the same finite-inventory rule applies; this is not a lifetime one-use claim. Recruited actors do not automatically spend it.

Expose bounded diagnostics with actual actor/threat/item IDs, pre/post HP and remaining owned units. Player-facing use/healing messages use native tonic behavior. Examine text describes serious-wound use and whether a matching bottle remains; queries are read-only. A successful use does not create an empty bottle item; the empty model is the existing harness cradle.

## Source and persistence

Opt only ordinary managed Spread cave generation at depths3–5/tier2, after authored POI/lair/special routes have selected their own recipes. When the existing underground encounter roll selects MarlbackGleaner, replace its first result with MarlbackPatchbearer; retain any other gleaners and every other result. Max one patchbearer in that roll; no extra actor, second encounter or additional random draw. Other biomes/depths, direct legacy table callers, surface sites and the guaranteed glade cellar keep old behavior. Expected rarity follows the existing gleaner branch (initial sweep:1/12 eligible floors); verify rather than promise a particular walk contains one.

Content only appears in newly generated eligible graphs. Cached and loaded creatures keep literal parts/inventory; no grants or retrofit. Save while carrying or after use preserves exact tonic availability, HP, remaining effects and AI state under existing serialization. Ordinary death drops the actual unused tonic; a spent tonic cannot reappear in inventory loot. Generic debug regeneration retains existing semantics and is not a forever-unique spawn guarantee.

The new role is discoverable through ordinary cave population, its distinct visible harness, native Examine and the use log. No quest acceptance, HUD marker or mandatory victory reward.

## Presentation

Use the established original marlback body/rig/clips and a pale strap harness with an open upper-shoulder cradle. Both stock states use identical framing so spending medicine cannot change the original body’s size or planted feet. Full form seats a stoppered amber/green bottle; empty form leaves the cradle clear. Recipe checks an actual current carried HealingTonic owner with valid inventory back-links, not a fixed blueprint costume or copied item name. Ground item remains the existing HealingTonic asset. Both role states must resolve in the actual managed Spread cave scope, including state refresh after consumption and saved graph replacement. Reuse native animation; do not claim a bespoke drinking motion unless one is actually authored and verified.

## Milestones and acceptance

1. **RED and core:** actual inventory consume/heal, Kill/Flee action replacement, ordinary fallback, threshold/cap boundaries, no player/follower/nonhostile/out-of-zone use, status/Calm controls; preserve raw failures before production.
2. **Source/content/art:** inherited resolved role and exactly one carried tonic; genuine ordinary pipeline/source proof, old/other depth/biome/POI controls, same-roll count/RNG counterchecks; original full/empty native models, exact-owner guards and state refresh. JSON semantic proof changes only intended blueprints.
3. **Save, loot and adversarial review:** actual death before/after use, partial stacks, wrong-owner/stale graph, saved full/spent state, scheduler one-action/stun assertions and neighboring AI/tonic/population/render regression. Fix significant issues; record bounded deferred questions.
4. **Native play:** use an isolated normal-input encounter route if ordinary source/navigation is tractable; otherwise clearly separate generated-source evidence from a controlled native scene. Demonstrate actual actor medicine expenditure, no attack on that turn, real rendered bottle state and surviving saved depletion. Review screenshots independently; no assertion alone proves readability or fun. Do not change production to make the harness pass.
5. Q1 symmetry/Q2 consistency/Q3 counterchecks/Q4 doc drift, living docs updated with exact counts and limitations, repository commit template, fetch/rebase and authorized push to main.

## Review and execution ledger

### Implementation and observed corrections

- Added the single inherited `MarlbackPatchbearer` blueprint and opt-in `FieldMedicinePart`; all previous blueprints are semantically unchanged. The exact armed-creature pin grows51→52 because the child inherits MarlbackRake.
- Kill/Flee call the same decision before movement/tactics. Real tonic use replaces the complete action; canonical inventory and effects own payment. No item grant, hidden recharge, save migration or medicine-potency change.
- Actual world Examine describes the threshold, stock and competing loot outcome. The native event log names tonic use/healing. `FieldMedicineUsed` identifies actor/item at top level and threat/HP/remaining owned units in its payload.
- Original two-state harness art, real actor-only self-use hook, native Interact clip, exact corpse provenance and narrowly scoped cave style approval. No dedicated drinking clip is claimed.
- Changed only ordinary Spread cave population admission; selected-row substitution preserves group count, order and RNG. `PopulationVariantApplied` retains both old and replacement blueprint provenance.

### In-phase review

🟡 **Committed death and ownership (fixed).** A positive-HP stale actor or threat could pass the first guard, and a spatially aliased carried threat could qualify. Added explicit death-handled and physical owner checks to decision/self-use. Native review RED captured each branch before fixes.

🟡 **Renderer invalidation (fixed).** Native tonic consumption did not dirty the owning actor cell. Added one success-only cell redraw after payment; refusals leave presentation untouched. Separate self-use hook preserves the real Interact action across the full→empty mesh replacement.

🟡 **Incomplete diagnostic context (fixed).** Initial used record omitted the actual threat and remaining inventory quantity. Added both, counted from the same validated local stock predicate, including stack and multiple-reference controls.

🟡 **Bottle hidden behind body (polished).** Initial gallery exposed a low rear cradle partially obscured by the carapace. Moved it to the upper shoulder, enlarged the pale casing and retained amber label/green neck. The raised bottle exposed an automatic-framing defect: full scale0.62 became empty0.68. Native RED reproduced it; both states now use the full form’s framing and keep the original foot rig planted. The first framing fix was briefly drafted beside its test, then removed before native RED; final verification uses the reproduced failing configuration.

🔵 **Census asserted an unjustified per-seed guarantee (corrected).** Seed64 had no Patchbearer in its nearest32 eligible columns, while seeds1/1729 had real sources. The design specifies uncommon encounters, not a guaranteed nearby one. Retained the failed raw run, changed the fixed three-seed test to report all bounded outcomes and require aggregate physical-source reachability, and ran a separately named full-map64 census. Production rarity was not retuned to pass a fixture.

### Source evidence and limits

The existing table’s theoretical Gleaner branch is1/12. At most one member is substituted. This probability is not an empirical per-world guarantee.

| Native deterministic sample | Ordinary columns | Physical entrances reaching depths1–5 | Patchbearer floors among depths3–5 |
| --- | ---: | ---: | ---: |
| Seed64, nearest32 | 32 | 18 | 0/54 |
| Seed1, nearest32 | 32 | 11 | 1/33 |
| Seed1729, nearest32 | 32 | 19 | 11/57 |
| Seed64, full eligible Spread | 128 | 67 | 10/201 |

Examples with actual registered down/up stairs: seed1 `Overworld.14.11.4`; seed1729 `Overworld.12.10.4`; full seed64 `Overworld.9.12.5` and `Overworld.13.12.4`. These are generated graph observations, not a claim to have walked those routes. Existing cached/saved floors do not gain new actors.

### Verification ledger

Raw evidence: `Docs/Verification/EnemyFieldMedicine/`.

- `native-red-initial.xml`:86cases,80expected missing-feature failures,6controls passed.
- `native-red-hooks-and-source.xml`:42cases,37expected missing-feature failures,5controls passed.
- `native-core-source-first-green.xml`:108/109passed; only the overstrong seed64 nearby-source assertion failed.
- `native-review-red-art-green.xml`:81/90passed;9targeted review defects reproduced. Source census and initial art/gallery passed.
- `native-old-art-stature-control.xml`: old low-bottle stature control passed.
- `native-raised-bottle-stature-red.xml`: raised-bottle size change reproduced before final framing fix.
- `art-build.json`:2native models; borrowed source bytes unchanged.

Final regression, native input, visual review and publication evidence follow below.


### Regression and cold review

`native-regression-after.xml`:1123selected cases;1119passed,3failed,1explicit full-map census unselected. All107 regular new cases passed; the additional explicit full-map case had already passed, giving108 distinct new native tests exercised successfully across the runs.

The three failures are pre-existing Signpost render-recipe gaps in `Overworld.2.6.0` (seeds64/1729/729490642). Temporarily restored **all changed tracked Assets to baseline2646bd5c6**, removed this milestone’s new C# and model assets, and reran the complete43-case `SpawnRing3DRecipeTests` fixture:40passed and the same3failed. Restored every feature file with byte checks. `regression-comparison.json` records **no newly failing tests**. This is a focused baseline comparison of every failing case, not a claim that the entire historical test suite is green.

⚪ **Deferred existing Signpost coverage gap:** the generic signpost in that prior settlement lacks an approved native model recipe. Unchanged on baseline; separate from medicine behavior and not a reason to broaden this combat milestone.

Q1 **Symmetry:** Kill and Flee both return after successful use; healthy/depleted actors retain their ordinary movement/attack paths. Hook reset/subscription/disposal are paired. Inventory stock, death loot, save reconstruction and art use one owner graph.

Q2 **Consistency:** native TonicPart and scheduler retain their existing contracts; no second action gate or synthetic healing. Diagnostic actor/item IDs remain top-level, threat/HP/remaining stock are explicit payload values. The action event is self-use, not a fictional spell.

Q3 **Counterchecks:** threshold boundaries, full health, missing medicine/opt-in, stun/sleep/custom veto, Calm/conversation/party, invalid owner/death/zone/payload, stacks, death and save variants, unchanged RNG/ambient rows/other scopes, exact art provenance and plain-gleaner controls are covered. Full→empty root scale and original foot position are pinned after raised-bottle RED.

Q4 **Doc drift:** reconciled the true source depth band, actual inherited20HP/40%threshold, real tonic potency, palette/cradle location, native Interact reuse, pure availability query and actual diagnostic shape. Corrected the overstrong nearby-per-seed test; no undisclosed rarity tuning. A separate reviewer found no additional significant ordinary-flow defect.


### Native acceptance and visual review

Successful run: `Native/5044f22f91bc406594c7bb3024fcf5a9/report.json`, **8/8 checks,0errors**, actual generated `Overworld.12.10.4` at seed1729. Player approach and target injury8HP are explicit controlled fixtures. Two paid native waits were needed before the newly registered NPC received its first action: energy0→1000 after wait1, then one Begin/End after wait2. On that action the target healed8→20, consumed its original bottle, did not move or damage the player, and published remainingUnits0. Actual full/empty rendered mesh approval and native F5/F6 saved depletion passed.

The initial live run `Native/c99f8b020d4243269af1990667be4daf` prematurely expected one player wait to guarantee an enemy action. It recorded zero NPC actions and failed honestly; no game rule was changed. The driver now observes up to three paid waits and verifies exactly one enemy action. The successful run used two. This follows native scheduler insertion/energy order rather than granting NPC energy.

**Can verify (script-observable):** cold-generated current actor and original owned tonic; actual native input/scheduler expenditure; exact one-action healing with no accompanying move/attack; log; current approved full/empty mesh; replacement player/enemy save graph with unchanged spent stock.

**Can verify (visual inspection):** inspected both normal-game before/after captures and ten final gallery front/side/rear/action captures. The raised pale flask and open cradle distinguish the states in close view. Existing anatomy/rig/contact remain intact; the actual game log names the drink and healing.

**Cannot verify (visual/feel):** normal cave-scale cues are small and shaded, and the adjacent player can overlap the raised bottle in projection. Do not claim effortless distant readability; native Examine and the explicit use log supply the precise rule. Gallery framing crops a little of the stopper in some close action views, not in the normal game capture. Static clip samples do not prove fluid animation, fun, balance, or an ordinary walking/injury playthrough. No bespoke drinking clip, all-biome AI medicine or retrofitted saved enemies is claimed.

### File ledger

- `Objects.json`: one original inherited role; existing content unchanged.
- `FieldMedicinePart`, `KillGoal`, `FleeGoal`, `ExaminablePart`: real item self-treatment, AI action replacement and live description.
- `PopulationTable`, `OverworldZoneManager`: narrow selected-row substitution and source diagnostics.
- `EntityVisualHooks`, `SpawnRing3D*`, `SpreadBiomeStyleEvidence`, `SpreadPortableRecipes`, `VoxelWorldPresentation`: self-use pose, exact native harness states, cave palette/mesh provenance, saved/rebuilt view behavior and corpse identity.
- `PatchbearerArtLibrary`, editor builder, `Resources/Patchbearer3D`, `ArtSource/Patchbearer3D/design.json`: two generated native forms on unchanged source rig/body/clips.
- `FieldMedicine*Tests`, `PatchbearerArt*Tests`, armed-creature pin:108new native cases including one separately selected full-map diagnostic.
- `FieldMedicineNativeBatch/Player`: isolated, self-reporting native-input acceptance scenario; preserves save/settings/scene state.
- This design, the three working prompts, and `Verification/EnemyFieldMedicine`: design decisions, corrections, raw RED/GREEN/baseline/live/visual evidence and reproducibility.
