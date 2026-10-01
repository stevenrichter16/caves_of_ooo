# Tier 1 material repairs and cultivated crops

Status: Tier1 and crop expansion complete and reviewed. Native regression915/915; final live route21/21. Base `7f6fcfa04`. User authorizes implementation, review/fixes, original crop models and full growing/harvesting integration with minimal intervention. Governing roadmap: [MATERIAL-REPAIR-ROADMAP.md](MATERIAL-REPAIR-ROADMAP.md).

## Directive and scope

Complete Tier 1 before adding later-tier repair complexity. Audit existing materials, damage, settlement repairs, inventory transactions and saved world graphs. Reuse viable foundations; replace a primitive mechanism only when the audit demonstrates a reason and preserves its callers/saved behavior. Provide three useful repair situations with ordinary sources of fire clay, timber and rope. Make fault, requirements and restored function understandable in Examine and environment art.

Then expand cultivated crops beyond emberwheat with distinctive useful plants in actual tilled soil, obtainable seeds, real harvest outputs and original models. Audit existing gladroot and berry behavior rather than assuming they are absent or equivalent to cultivated crops. Verify planting, watering, growth, harvesting, repeat prevention and save/load through the ordinary player path. Keep supported active-area growth and any offscreen limitations explicit.

## Audit workstreams

1. Repair/material/damage/inventory/world-state source sweep: existing contracts, calls, state ownership, availability and atomicity.
2. Crop/lore/use sweep: actual species, planting terrain, schedule, moisture, harvest outputs, utility, seed acquisition and persistence.
3. Art/placement sweep: established source/import libraries, visible stages and owner-based rendering, candidate world locations and preservation.
4. Root integrates the findings, records concrete Tier 1 and crop plans below, writes failing tests before implementation, and owns Unity refresh/import/test coordination.

The audit and narrowed plan below were recorded before production work. Subsequent checkpoints retain the RED evidence and distinguish confirmed defects from test-setup mistakes.

## Readiness and corrections

Original content inspired by the user's exploration goals; no copied Qud content or claimed exact Qud parity.

| Surface | Verified current contract | Implementation decision |
|---|---|---|
| Materials | `MaterialPart.cs:15–28` owns reaction identity/tags and legacy fire/acid/conduction values. These are not repair composition. | Separate `CompositionPart`; do not reinterpret reaction fields or the deferred fire scale. |
| Legacy repairs | `SettlementManager.cs:111–299` has functioning dialogue-specific well/oven/lantern repair stages, guides, material payment and saved state. TinkerItem repair cost is unused; BrokenEffect is a combat marker. | Preserve settlement quests and avoid tinkering. Add opt-in structural faults, not a blanket HP-healing or resurrection system. |
| Water | `WellPart` draws unconditionally; `WaterVesselService:98–114` separately accepts wells as fresh water. | One shared availability predicate gates both. Ordinary wells remain usable. |
| Doors | `DoorPart.CanOperate` is shared by direct use and AI. Destruction removes exhausted owners. | Jam new gates open until repaired; block their operation through the common predicate. Never block the path or revive destroyed owners. |
| Transactions | PerformInventoryActionCommand passes a transaction; default world-menu dispatch does not. Native snapshots support scoped rollback and ownership claims. | Route the exact repair action through a command, charge one turn only on committed success, consume positive genuinely carried units across stacks, and roll state/payment back together. |
| Saves | Public Part fields persist; saved Parts are loaded instead of rebuilt from new blueprints. Explicit unloading can regenerate zones. | Saved repaired state; bounded retention for the new authored sites. Old cached content remains literal. |
| Sources | FireClay exists in trade; no ordinary portable timber/rope item exists. | Add salvage timber and knotflax cord, finite physical world sources and trade availability. Keep FireClay's previous uses in its guidance. |
| Crops | Both gladroot (CandyCarrot IDs) and emberwheat have real CropPart growth. Seed/sprout crops currently turn into loose produce; RipeCropRow is separate finite scenery. | Preserve legacy maturity behavior. Add opt-in standing maturity and explicit harvest for the new cultivated species. |
| Soil/start | Seeds use Plantable terrain; no explicit tilled-soil definition. Current fresh spawn is Morrowfast, whose six exact starter-garden terrain owners are marked Plantable by MorrowfastStartingGarden. Historical glade directions are not current spawn directions. | Preserve authored terrain/geometry and add explicit cultivation state/visuals where suitable. New seeds require cultivated beds; provide real world beds and seeds. Verify current starting route. |
| Crop schedule | CropSystemPart accepts active-zone player TickEnd; dry crops pause. Existing rain tops moisture up, not additively. | Keep one growth tick per player turn and active-area scope. Verify full dry/wet/grow/harvest/save loop. |
| Potential growth defects | TryMature counts factory products without checking placement, and SeedPart does not verify a valid crop owner. | Test these hypotheses RED, then fix confirmed loss/invalid-content cases while adding standing harvest. |

## Concrete Tier 1 plan

1. Data-defined recipes bind an exact fault ID, required composition category, supply blueprint/quantity and plain-language diagnosis/action. Initial recipes: cracked lining → two FireClay; jammed wooden frame → two SalvagedTimber; snapped well line → one KnotflaxCord. A separate saved composition Part is descriptive; explicit repair rules remain authoritative.
2. RepairablePart owns one fault and a repaired flag. Live local player, current ground owner/parts, intact target, valid recipe/composition and exact carried supplies are required. Success consumes the required units and restores function in the same transaction. No skill/tool/quality system yet.
3. Share well availability across drink/fill. Repair enables a jammed-open ordinary gate to close/open. Add a narrow transactional menu branch and informative Examine/material guidance, diagnostics and visible before/after selection.
4. Place useful fault examples and reachable finite sources in an ordinary near-start exploration site without overwriting previous owners or forcing a detour. Retain its saved graph. Add trade fallback. Pin actual generation and access, not only isolated object creation.
5. RED/GREEN tests, adversarial review, native input/save/pixel checks, repair fixes and documentation before proceeding to final crop acceptance.

## Cultivated crop plan

Create three CoO-original cultivated species: knotflax yields cord used by the rope repair; hearthbulb supplies food and a cooked meal; seamleaf supplies a useful brewing reagent. Author distinct growth silhouettes and produce/seed appearances. Species, growth durations and yields are content definitions; mature plants stay until explicitly harvested. Stage all harvest outputs before committing, preserve supplies/owner on failure, provide seed return for replanting, and leave a reusable cultivated bed. Existing crops keep their old behavior.

Add seed acquisition and actual planted world beds, respect the current Morrowfast garden terrain contract and Spread seed-keeper stock checks, and expose clear soil/water/harvest descriptions. Visuals must follow the actual owner and saved stage, with no decorative crop pretending to grow. Verify legacy crops plus new planted/mature/harvested state, dry pause, player-turn schedule, two species sharing a zone, save/load and removal. No offscreen growth claim.

## Verification and review

Capture RED before production, GREEN after, paired counter-checks and a dedicated adversarial sweep. Check current-owner identity, distant/dead/foreign actor refusal, quantities/stacks, repeated repairs/harvest, transaction rollback, native saves, source preservation, real input and visual state. Run Q1–Q4 before publication. Use native Unity as authoritative; reference-runner evidence alone cannot prove presentation, input, exact native seeds or scheduler timing. Preserve prior unrelated dirt. Exact owned files only; fetch/rebase before authorized push to main.

## Implementation log, results and accepted limits

Tier 1 and the cultivated crop loop are implemented. Later repair tiers, generalized decay, universal component simulation and offscreen farming remain outside this iteration. The audit preserved working settlement repairs and fixed confirmed inventory/growth defects rather than replacing their foundations.


## Implementation checkpoints

- Repair runtime: composition + immutable JSON recipes + saved opt-in fault, shared drink/fill and door gates. Native initial run `d8a26638d0644905b3a97a6d2b153632` passed all68 new repair and53 new crop cases;12 integration content checks failed before new blueprints/site were added (133total,121pass/12fail). Reference repair regressions207/207; reference farming150/150; native remains authoritative.
- Initial content/art RED `a93f009bd89547a9851fa5dc238ae39b` completed38 with failures, but concurrent compilation prevented retention of its full result; only capped raw failures are retained. It is not used as a full count claim. The later isolated12-content RED above is complete.
- Seventeen original blueprints appended surgically; all old parsed blueprints unchanged. Three fault props, three finite material sources, three species with seed/crop, four useful output items and one timber supply. Source receipts retain exact parsed comparison.
- Fresh western Morrowfast field now contains the additive allotment; native content/world checks pass12/12 including seeds64/1729 and explicit unload retention. The original supply basket remains. The generation routine preserves every original owner/terrain and refuses unsafe placement; route proof is rooted at the east-center crossing.
- Current Morrowfast starter garden keeps its six authored terrain owners and gains saved cultivation markers. Morrowfast shops and Spread seed keepers offer new supplies/seeds; older cached shelves retain their saved state until ordinary restocking. The settlement repair quest system and existing fire-clay uses remain intact.
- Input RED `c87c896cc3de4066817778cb12be7c4a`: all8 actual-menu transaction cases failed against the original dispatcher; restoration of the two exact command routes follows this evidence. Its4 village art cases failed a setup omission (starter garden was not initialized by the fixture), so those are not claimed as feature RED. Fixture corrected to call the real garden preparation before the next run.
- Art is a separate37-form procedural source pack, borrowing established native material/rig conventions without rewriting old meshes. Native import completed; subsequent30/30 art cases and live frames below verify the actual renderers.

### Boundaries and review notes

No equipment durability, combat resurrection, tools/skills/repair quality, free-form tilling or offscreen growth is introduced. New crops require existing prepared beds; legacy crops retain their previous plantable-ground/automatic-drop behavior. New mature harvest places produce and returned seed on the ground for ordinary pickup. Each successful repair/harvest costs one action, and failed transactions cost none.

The gate is a standalone allotment gate fixture; restoring it enables real closure and opening, not a new region unlock or a full defensive enclosure. Wells supply the first repair loop's practical water reward. Saved marked beds and the finite allotment are retained against explicit unload; there is no global offscreen crop simulation.


## Delivered content and player route

Start a fresh ordinary game in Morrowfast (`Overworld.3.6.0`). The six existing garden cells at `(41/42,21–23)` retain their authored terrain and now show prepared furrows. Buy new seeds from Sella Kettle, the provisioner; Orrit Coilstitch, the mender, stocks repair supplies. Spread seed keepers also stock the new seeds and retain their watering book.

Leave Morrowfast through its western edge into `Overworld.2.6.0`. The allotment lies near the eastern side of that field, around `(62,11)`; actual flora can shift individual owners into safe nearby cells. An allotment notice explains the objects and cultivation. Placement is additive, checked against the real east-center arrival route, and refuses rather than clearing original owners or severing movement. Native generation tests cover seeds64 and1729, not every possible world seed.

| Fault and composition | Carried material | Restored function |
|---|---|---|
| Split clay catch-well lining; Masonry | 2 FireClay | Draw drinking water and fill compatible vessels |
| Jammed allotment gate frame; Wood | 2 SalvagedTimber | Close/open the existing gate |
| Snapped hoist-well line; Fiber | 1 KnotflaxCord | Draw drinking water and fill compatible vessels |

Finite exposed clay, timber and cord sources stand nearby. Examine identifies the fault and requirements; the ordinary interaction menu offers Repair. Failed/refused attempts spend no material or turn. Repaired state, supplies, finite-source depletion and prepared beds survive native save/load. Explicit unloading retains marked sites; re-entry does not refill them.

| Cultivated crop | Wet player turns per stage | Ripe harvest | Actual use |
|---|---:|---|---|
| Knotflax | 16 | 2 knotflax cord + 1 seed | Repair the hoist-well line |
| Hearthbulb | 20 | 2 hearthbulbs + 1 seed | Edible raw; cook at ready heat into roasted hearthbulb (`2d4` healing) |
| Seamleaf | 24 | 2 seamleaf sprigs + 1 seed | `vital:2` reagent for the existing mending brew rules |

Each species grows seed → sprout → standing ripe plant. Harvest explicitly, pick up the real loose outputs, then plant the returned seed on the reusable empty bed. Conjure Rain supplies moisture; dry plants pause. Existing gladroot/emberwheat keep their automatic-drop maturity behavior. Wells provide drinking/filling water; they do not automatically irrigate crops. Crops grow only in the active area as the player spends turns. Prepared soil is authored; a player tilling tool belongs to a later iteration.

## Art and live acceptance

37 original model forms cover six wet/dry growth appearances per species, three portable seed forms, useful outputs/materials, finite sources, damaged/restored wells, broken/open/closed gate and sparse furrows. Source: `ArtSource/RepairCultivation3D`; native importer: Tools → Caves of Ooo → Art → Import Repair and Cultivation. Both the ordinary wilderness presenter and Morrowfast's separate authored presenter consume current saved owners/stages. No old meshes, global biome lighting or underlying terrain models are replaced. Growth changes visible stages; this is not a smooth growth-animation system.

Native art/site/use run `b520e7f8d4584cee93792a610240819a`:62/62 pass (26 wilderness art,4 village art,8 site/persistence,6 utility,18 existing merchant restock). The village tests initialize the real fresh-start garden and cover current-owner stages, soil preservation and removal. The underlying terrain's legitimate GlyphVariants initially hid furrows; admission now preserves those fields while keeping strict new-prop guards.

First full native keyboard route `8dbf58bdfa5e4cbda018a6f97c825cd0`:21/21 checks,0 unexpected errors,14 screenshots,29.1seconds. This gathered all three supplies, repaired all faults, drank from the repaired well, closed/opened the gate, harvested/picked up/replanted knotflax, learned the ordinary watering book, cast real rain, waited31 turns through sprout and ripe stages, harvested again, and used F5/movement/F6. Saved bytes and replacement graph matched, including repaired flags, crop clocks/moisture, terrain IDs, carried quantities and missing spent sources. One disclosed player travel shortcut enters the actual generated western field from the isolated glade launcher; local movement/actions and NPC scheduling remain real. No grants, forced growth, healing or source replacement. Existing user saves/settings/scenes are isolated and restored by the launcher.

Visual inspection of the native frames confirms actual 3D wells, differentiated crop rows and prepared furrows, readable repair requirements, and persistent props after loading. The first growth frames partly hide the planted crop behind the player. Follow-up `0f51c948340e4fccbf379d5617fd69fd` tried a real step aside for clearer views, but stopped under live enemy pressure during the wait; its safety failure and screenshots are retained. That presentation-only route change was removed. The audit now explicitly forbids the shared walking helper from using starting healing tonics, matching its stated limits. These finite frames do not establish every camera angle, all-seed placement, discovery quality or long-term balance.

## In-phase review and corrections

- 🟡 Repair authority: four confirmed malformed-state holes (stale facade, duplicate fault/composition, equipped material alias) fixed before native acceptance. Eight hypothesis cases distinguish four confirmed defects from four already-correct controls.
- 🟡 Crop transactions: seven confirmed defects fixed—invalid noncrop seed target, legacy output-placement loss, ripe moisture freezing, later output initializer corrupting a prior output, moved planter, post-action seed rollback leaving a crop, and duplicate batch IDs. New/legacy routes share complete output validation and rollback.
- 🟡 Menu timing: actual menu initially bypassed the receipt; both exact actions now use the native command executor. Eight paired cases prove one turn on success, zero on refusal/veto/rollback, with no lost materials or duplicate outputs.
- 🟡 Rendering: native terrain variants suppressed soil overlays; fixed without loosening new crop/prop identity checks. Morrowfast needed a separate current-owner adapter; its four meaningful RED cases now pass. Initial four setup failures are not counted as feature RED.
- 🟡 Source integrity: route proof now starts at the real east-center crossing. Failed placement removes the matching Plantable index entry as well as the tag. World tests preserve original IDs/positions and reject incomplete placement/refill.
- 🟡 Examine consistency: three repaired props retained contradictory permanent damage prose, and the gate's name remained “jammed.” Three RED cases establish this; neutral construction text/name leaves live fault/repaired wording under RepairablePart control.
- 🔵 Merchant pins:12 existing exact-stock assertions correctly rejected the new authorized items. Updated only quantities/lists; connected factories and factory-less fallback agree. All18 native restock tests pass.
- ⚪ Gate scope: real closure/opening of a standalone gate fixture, not a complete enclosure or new region unlock. No universal broken-object/HP restoration is claimed.
- ⚪ Existing saves: cached old owners/areas remain literal. New blueprints do not retrofit old fields or gardens. Use a fresh game for the complete first-tier site; no migration was requested.

### Q1–Q4 cold-eye pass

Q1: water drawing and vessel filling share the same repair gate; door operation uses the existing shared authority. Planting and harvesting use the native transaction order, with output validation before mutation and diagnostics after commit. Removal/rollback are paired; world tag indexes are restored with the tags.

Q2: every fault uses the same immutable recipe schema, exact carried-unit payment and saved flag. Every new species uses the same standing maturity/ground-output/returned-seed contract; legacy automatic maturity remains opt-in false. Models read owner state and add no gameplay state, collision or independent crop clocks.

Q3: counter-checks cover missing/wrong/insufficient/forged supplies, equipped aliases, stale/distant/dead owners, repeated use, failed callbacks, partial output placement, explicit rollback, dry/wet growth, player/NPC ticks, legacy/new maturity, current/foreign terrain, saved stage0/1/2, full inventory and presentation removal. Dedicated adversarial fixtures complement actual-menu, generated-site, native-save and useful-output integration.

Q4: the roadmap's Tier1 is a foundation of three explicit faults and three supplies, not a material-substitution or generic durability engine. Content durations, counts, brewing/cooking behavior and current spawn directions above match source. New crop/maturity evidence is authoritative in Unity; earlier reference runner tests use scratch shims and do not establish input or pixels. Native final regression/publication receipts follow below.


## Final regression

Native Unity EditMode selection:48 fixtures,915 cases, including188 new feature cases and727 existing cases. First run `6b92da586ff44dce892e46306eca88b9` passed912/915: two old crop-art assertions exposed a real legacy dirty-hook name regression (`CropHarvested` instead of `CropMatured`); one old seed-keeper stock-count pin expected5 rather than8 entries. Restored the old hook only for legacy maturity and updated only the authorized seed-keeper count, preserving cook stock. Final run `c854564685dc451f96fabfb1fac4e0c2`: **915 passed,0 failed,0 skipped**,84.15seconds. No gameplay implementation changes followed; later audit edits only restore the original movement route and enforce its no-healing condition.

This is a substantial affected-system selection, not a claim that the entire repository suite ran. It includes farming/scheduler, repairs, actual menu transactions, current starting garden, native owner/save/unload behavior, shop restocking, guidance, water, doors, old settlement repair paths and both rendering presenters. Evidence: `Docs/Verification/RepairCultivation/Tests/regression-selection.json`, `final-regression-result.json`, `final-green-result.json`. Source model tests:3/3. All21 new C# sources have distinct version2 metadata with no collision against tracked metadata. Parsed Objects.json proof:17 additions,0 prior blueprint changes/removals, original order preserved.

### Files and extension points

- Roadmap/living docs: this file and `MATERIAL-REPAIR-ROADMAP.md`.
- Repair rules: `Assets/Resources/Content/Data/Repairs/Tier1Repairs.json`; runtime composition, registry and transaction gate in `Assets/Scripts/Gameplay/Repairs/`.
- Crop runtime: Farming CropPart/SeedPart/CropSystem plus CultivatedSoilPart/CropYieldService. Blueprint parameters choose species duration, yield, seed and legacy/standing maturity.
- Content/distribution: seventeen new Objects.json blueprints; three changed loot tables; Morrowfast stock fallbacks and Spread seed-keeper declaration.
- World/input: RepairCultivationSite, MorrowfastStartingGarden, OverworldZoneManager retention, Examine/material guidance and the two exact menu command routes.
- Art: new RepairCultivation3D source/import/library, independent soil overlays and current-owner selection in the wilderness and village presenters.
- Tests/evidence: ten new fixtures (188cases); two precise existing merchant/resident stock pins; native keyboard audit partial and launcher. Raw native reports retain honesty bounds and labelled setup travel.

Tier2 can add repair materials/substitutes and more faults by extending content and guarded registry rules; it should not bypass current inventory transactions. Future cultivated species can supply different growth/yield definitions and model forms. Tilling tools, irrigation from carried water, offscreen farming and components require their own scoped iteration and acceptance tests.


Final ordinary-route rerun `aca44f9a21ec45acac45e40c358e2a51`, after the wording and legacy-hook fixes and with the strict no-healing audit gate: **21/21 passed**,0 unexpected errors,14 native screenshots,31.01seconds. It observed31 ordinary waits/one real rain cast and exact F5/F6 restoration. Final images were visually inspected: neutral well construction text leaves diagnosis dynamic; the view from the gate shows all three distinct ripe crop models and three dry seed beds. The direct growth views remain partly covered by the player; model-stage differences also have native renderer tests. Unity exited Play and restored `Assets/Scenes/Main/SampleScene.unity`; no scene edits or user saves were overwritten.
