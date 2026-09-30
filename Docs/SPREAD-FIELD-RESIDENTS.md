# Spread field residents and growing plots

Status: complete and verified for publication after `158f22298`. Original CoO content, not a Qud parity claim.

## Outcome and implementation directive

Add two different reasons to stop during ordinary Spread travel: a seed keeper's open growing plot, and a wayside cook's small kitchen and cot. Use real talk, trade, planting, rain, growth, pickup, cooking and rest. Give both people distinct original 3D bodies and make existing planted crops visibly respond to growth and moisture. No quest acceptance, extra hostile group, new trade framework or tinkering access.

1. Add two Villager children, `SpreadSeedKeeper` and `SpreadWaysideCook`, with modest purses, distinct conversations and themed real stock. Seed keeper: two gladroot seeds, two emberwheat seeds, one watering grimoire; cook: one each raw meat, mushroom, emberwheat, dried meat and toasted emberwheat. Normal purchase prices and existing timed restock apply.
2. Append `SeedKeepersPlot=16` and `WaysideKitchen=17` in exploration version 10. Seed plots contain four initially dry seed-stage crops and a short physical sign. Kitchens contain a usable oven, unowned cot, chair and an open shelter. One new resident per site. Preserve every pre-existing generated owner, stock, caller RNG, protected address, arrival and exit. Sources are explicitly new content, not replacements pretending to be old population rolls.
3. Assign examples two chunks north (11.8) and east then south (12.11) of the starting glade only if the previous allocation was quiet. Broader additions occupy a previously quiet band; retain prior active families, quiet areas and cardinal family exclusion. Literal saved versions 2–9 and cached graphs retain their content. Failed placement stays refused.
4. Extend the established model sources/importers: two resident bodies and eight crop appearances (two species × seed/sprout × dry/wet). Native owner, growth and moisture determine appearance. Mature crops become loose produce; no invented standing harvest stage.
5. Run native RED before production, counter-checks and adversarial source/save tests. Inspect generated views and run the existing isolated Play launcher with disclosed setup shortcuts. Review Q1–Q4, update this document, commit exact owned paths, fetch/rebase and push main. Continue with a further useful Spread slice after this batch is verified if feasible.

## Readiness and pre-implementation corrections

| Surface | Readiness / verified contract | Decision |
|---|---|---|
| Friendly NPC trade | Existing Villager has Brain/Staying, Conversation, Trader and inherited inventory. Its IntProps purse is 100 independently of Trader.Drams. | Override both purse values. Use ordinary trade and themed stock, not a new request system. |
| Restock | TraderRestockSystem runs on zone entry after 300 ticks, with a 100-dram floor and shelf threshold 3. | Stamp new opening stock at current tick; finite opening stock is not lifetime finite stock. No restock tuning in this slice. |
| Farming access | Starter kit and village provisioner stock already supply seeds; old FarmingAccessGrant comment claiming no trade source is stale. | New plot is a world opportunity and replenishment source, not a claim that farming was unreachable. |
| Growth | CropPart has seed stage 0 and sprout stage 1. Dry crops pause; maturity removes the owner and produces two items. CropSystemPart gates non-player stamped TickEnd. | Explain active-area growth: gladroot needs 40 moist player rounds, emberwheat 70. No offscreen farming promise. |
| Water | Conjure Rain supplies 40 moisture ticks with max/top-up semantics, not additive watering. | Emberwheat needs another watering after growth. No water-from-well crop action is implied. |
| Crop graphics | Existing EnvironmentSpriteRenderer has 2D stage support; current 3D recipes lack these two CropPart species. | Close the actual 3D gap with state-sensitive forms and stage/wet/dry/removal tests. |
| Oven | Raw Oven and village OvenSite are not Campfire cooking sources. | Attach CampfirePart only to the new exact site oven, AllowRest=false. Use existing legacy cooking semantics; cot supplies rest separately. No global oven behavior change. |
| Rest | Existing bed/rest service checks owner, occupancy and nearby threats, heals and advances the clock. | Retain it. Rest consumes no food and does not simulate a surprise attack during sleep. |
| Nearby placement | Frozen native v9 seeds 64/1729 show 13.10 already has CoolingWorkPatch. 11.8 and 12.11 are quiet. | Keep 13.10. Compute the complete prior allocation before assigning new residents to quiet rows, avoiding changes caused by ordering or adjacency. |
| Scope and voice | Existing protected sources, quiet assignments and canonical material-life voice remain authoritative. | Domestic, concrete dialogue; no deep-lore revelation, universal markers or displaced old encounter families. |

Sources read: SpreadExplorationPlan/Builder/Worksites, WorldTravellers, TraderPart/TraderRestockSystem, Objects.json, LootTables.json, SeedPart, CropPart/CropSystemPart, Hydromancy_ConjureRain, CookingService, CampfirePart, OvenSitePart/VillagePopulationBuilder, existing native audit/preview, and Lore/Voices/VOICE-CARDS.md §11. Each delegated implementation also records any further correction before applying production edits.

## Evidence and review ledger

The RED, implementation, counter-check and native acceptance ledger is below. Distinguish authored selection from actual committed generation, native logic from pixels, and a setup-assisted action witness from ordinary unaided discovery. Existing saves retain their literal exploration manifest as well as their cached chunks; loading an older world does not add the new resident assignments.

## Deferred boundaries

No offscreen farming, farming weather rewrite, growing-maturity harvest menu, general merchant specialization, mandatory quests, dog-fetch repair, full game balance claim or new fire scale. Significant defects in the new gameplay block the affected slice; minor unrelated problems are recorded and left for later.

## Follow-through: people can explain nearby places

Implemented second slice: each new resident has a separate Nearby conversation choice that offers at most two nearby, different useful places from the frozen exploration manifest. It covers alembics, tempering shelters, trapper stores, seed plots and kitchens within four world-map steps, excluding the speaker's own address. The report describes a past/unconfirmed place, not current stock, a living person, safety or successful generation. Remembering remains an explicit choice in the existing travel notes; it starts no quest and generates no remote graph. Old three discovery families and serialized records remain unchanged. New records reuse the same bounded schema and add five explicit family IDs; no arbitrary notes or map pins. Each family retains its latest remembered destination (eight records total including the earlier three).

Source sweep before this slice: SpreadDiscoveryReports admitted only Scribe/Innkeeper nodes and three frozen rare/wayhouse identities. SpreadDiscoveryNotes already has bounded, saved historical records and invalid-record guards. Extend these existing paths, snapshot the exact exploration-plan identity in offers, and recheck it plus role, node and eligibility before writing. Keep ordinary Scribe/Innkeeper behavior unchanged in this slice. Tests must show no note on opening, bounded offers, no remote generation, historical text unchanged by remote outcomes, saved readback and stale/dead/hostile/foreign-role refusal.

## Shipped behavior and directions

For a **new world** with the starting glade at (11,10), the native seeds 64 and 1729 both realize the seed keeper **two chunks north at (11,8)** and the wayside kitchen **one east and one south at (12,11)**. Other seeds still obey eligibility and placement refusal. Look for the broad straw hat beside four planted beds and a sign, or the pale cooking cap beside an oven, cot and chair. Use the ordinary interaction menu to Chat or trade. These are original people, not additional hostile packs or quests.

The plot begins with two dry gladroot crops and two dry emberwheat crops. Its keeper sells two seeds of each and a replacement watering grimoire. Plant a carried seed on unoccupied plantable ground; read the ordinary starter watering book and use Conjure Rain. Both planted species now have four real 3D appearances: dry/wet seeds and dry/wet sprouts. Wet soil is darker. Examine states the current stage and moisture, active-area growth, and how to collect the eventual produce. At maturity the owner disappears and two produce items remain, using the existing farming system.

The wayside cook sells raw and prepared food. The new site's exact oven is usable for the existing carried-item Cook action, and its unowned cot uses ordinary bed rest and hostile refusal. No other decorative oven is silently changed. Both residents are passive Villagers who can defend themselves; they are not invulnerable. Nearby creatures can approach after generation. Normal NPC restocking remains active.

The nearby-places question connects these stops to the previous alembic, tempering and trapper additions. Remembering directions is optional, and they appear under **Q → Tab (field notes)**. Reports cannot reveal current remote stock or promise that an optional placement succeeded.

## Allocation, source preservation and performance

Version 10 appends two enum values after the existing fifteen. Allocation first reproduces the entire version-nine assignment, then fills selected quiet rows. The broader domestic band uses stable quiet ranks 12–21, split by a separate resident-family rank; cardinal adjacency excludes repeated families. This is a candidate band, not a claim that ten percent of all chunks receive residents. Selection, cold generation, successful acceptance and saved disposition remain separate.

Creation uses a private deterministic stock RNG and restores borrowed factory/RNG statics. The helper pins all original graph owners and stock, validates new entity/child identity and ownership, keeps arrivals/ports/routes and two approaches open, and refuses unsafe footprints or a second household where an existing talkable creature already lives. Failed optional placement leaves the old encounter intact. Stock timestamps prevent immediate restock of the fresh five-unit shelf; ordinary later restock is deliberately unchanged.

The cold-generation placement search has at most 256 qualified trials. Crop presentation uses the existing cell-dirty hooks and current-owner recipe path; this adds no Update loop, per-turn observer or new cache. Examine/report string construction is demand-driven. Resident reporting examines the frozen manifest only on dialogue refresh/selection, not on movement or every frame. Ten source models extend the existing palette/importers/rig: eight crop forms and two bodies. The existing five animation clips are reused, not newly authored behaviors. Ground-contact capacity follows the extended environment library.

## Implementation corrections and self-review

- 🟡 **Resolved: rolled farmhouse conflict.** The first native generated-graph run rejected one seed-64 plot because later local naming changed an already present Farmer, violating the final original-owner proof. Refusing a second domestic site when a pre-existing creature has conversation preserves the original farmhouse lifecycle. Two explicit refusal controls and all four native near-spawn witnesses now pass; no naming exceptions or weakened source proof were added.
- 🟡 **Resolved: initially decorative oven.** Source inspection showed raw Oven lacks cooking. Only the new site's owner receives the existing cooking part, with rest disabled there; the bed handles sleep. Tests assert a raw Oven remains unchanged.
- 🟡 **Resolved: inherited purse and aggression.** Both Trader.Drams and the separate inherited Drams property are overridden; Passive is explicit. Opening stock, payment, consumed items and unchanged bystanders are asserted through real content actions.
- 🔵 **Corrected integration expectations:** the wider native run found eleven old catalogue/per-formation assertions across four fixtures. The two named resident families are now admitted only alongside the existing exact variant checks; old-family positive/negative witnesses, adjacency and complete v9 comparisons remain. For example, road (12,13) at seeds 2/1729 had quiet ranks 15/18, so both were quiet under v9 and are valid new domestic candidates. No production fix was needed.
- 🔵 **Corrected test isolation:** report fixtures preserve borrowed offers and revision as well as conversation state. The content fixture now preserves the same borrowed state, captured before scope creation and restored after disposal.
- ⚪ **Accepted existing mechanics:** local crop ticking, max/top-up rain, whole-stack cooking, periodic merchant restock, free unowned cot and legacy oven cooking are retained. Oven reuse also retains the old campfire crackle/ember presentation; bespoke oven ambience is a low-priority follow-up, not a new thermal/fuel claim.
- ⚪ **Original-content classification:** these sites and reports use CoO's existing farming, cooking, trade and exploration contracts. No decompiled-game parity claim or imported enemy content is introduced. BitLocker stays dev-only.

Cold-eye Q1–Q4: generation and reporting reviewed independently for frozen allocation, authority, graph/save reach and stale offers; art reviewed for current-part admission, source/asset preservation and state transitions; content/action witnesses reviewed for real stock, payment, starter-book use and clock semantics. No outstanding significant production finding. Root also checked the combined diffs, actual crop/journal images and kitchen/plot compositions. The player partially obscures the exact newly planted crop in the full-game frames; neighboring watered/grown rows remain visible, and separate native owner/submission tests cover the planted owner. Final evidence must not be read as a world-wide balance or discovery guarantee.

## Verification ledger

| Gate | Observed result / limit |
|---|---|
| Initial native feature RED | 42 cases: 12 controls pass, 30 fail on missing content/readout/presentation. Resident-art failures were missing-blueprint evidence, not a separate post-blueprint binding RED. |
| Native generation RED | 28 cases: 3 legacy controls pass, 25 fail on missing helper/v10. |
| Intermediate native logic | 58 cases: 48 pass, 9 expected missing-report failures and 1 Farmer conflict (fixed as above). |
| Focused final native EditMode | **144/144 passed**, zero failures/skips, Unity 6000.3.4f1, 176.115 seconds. Includes real crop water/dry/growth/maturity presentation and stale/foreign-owner refusals, both residents, graph save/load, old discovery regression and all four near-spawn placements. |
| Authoring checks | 2 new source checks RED→GREEN; 14 previous source checks remain green. All prior 48 environment and 54 humanoid source rows and palettes unchanged. |
| Imported art | All 653 previous files present; 651 byte-identical. Only the two expected libraries changed. Forty new resource files including metadata supply ten models; no old mesh/prefab/palette changes. |
| Native generated views | Eight full/close views across seeds 64 and 1729, zero missing models; four actual committed sites, one resident each, four growing crops per plot. Previews reveal the area for composition review. |
| Actual-input Play | **16/16 passed**, zero unexpected errors, nine screenshots, 25.277 seconds. Real seed purchase, starter-book learning, Plant, rain, waiting, growth, pickup, remembered direction/journal, food purchase/Cook/Eat and bed rest. Bed actually advanced 60 ticks. |
| Wider affected native regression | 1,509 cases: first sweep 1,498 pass / 11 stale test expectations fail (722.735s). Corrected four test expectation files and borrowed-state fixture preservation; reran seven complete fixtures **116/116 pass** (13.558s). Combined latest results cover all **1,509/1,509** cases. Production unchanged between these runs. This is an affected selection, not an unfiltered whole-game pass or a second complete sweep. |

**Can verify:** native content and exact state transitions, frozen prior allocations, refusal/counter-checks, saves of actual stock and crops, current-owner render admission, and the observed controls and pixels in these configurations.

**Cannot verify:** ordinary unaided discovery or the whole walk from spawn (the Play audit uses two labelled travel shortcuts), every seed's placement success, long-term economics, multiplayer, graphics backends or subjective balance/feel. New crops do not grow while travelling elsewhere, and older saved manifests remain older. The reference runner's patched hashing differs from Unity; its one seed-1729 optional-household refusal is not a native-map result.

Raw evidence: [focused results](Verification/SpreadFieldResidents/focused-result.json), [generated views](Verification/SpreadFieldResidents/Views/receipt.json), and [actual-input report](Verification/SpreadEverydayResidents/Native/5aec55bac36644118737c3b5c4bfafc4/report.json). Exact files and final results are recorded with the publication receipt.

## Files and next useful boundary

Production: two appended blueprints, two stock tables, one conversation file; `SpreadExplorationResidents` and v10 plan/dispatch; crop examine/readout; five additional bounded discovery-note families; existing art sources, importers, recipes and libraries. Seven new fixtures plus current-version pins cover these paths. Existing native audit and preview helpers gained bounded resident routes, with matching `.meta` files for new Assets sources.

The next content priority is consequence and local variation: meaningful differences in what an ordinary person or place lets the player do, connected to existing resources and travel. Avoid merely increasing the number of identical plots or kitchens. Offscreen farming, broad economy changes and minor ambient wording remain separate work.

The next candidate identified by a source review is a Reedback visiting an existing finite watering margin: use an existing ambient population allowance, the actual saved basin and normal animal movement; let the player fill first, startle the visitor or follow it toward water. Current well-visiting AI only walks to a well and does not drink. Exact-source drinking, one-sip persistence and interruption are therefore real follow-up work, not behavior claimed by this batch. Keep quiet banks and old saved graphs, and preserve some water for the player. This candidate is assessed but **not implemented here**.

### Final native observation and publication receipt

A repeat live audit exposed an observer mistake: a reserved cot truthfully refused sleep, but the driver recognized only hostile refusal and labelled the result incorrectly. No bed gameplay was changed. The corrected driver uses an actor/bed input marker and exact diagnostic outcomes, accepting a zero-time occupied/hostile refusal or an observed 60-tick sleep, with unchanged purse. The failed receipt is retained. Final run `b2a15be31b8644c097c0deb382855ba0` passes 16/16 with zero unexpected errors and actual 60-tick sleep. The live diagnostic stream contains all sixteen passing cases, the completed summary and the correlated bed outcome. [Acceptance receipt](Verification/SpreadFieldResidents/acceptance.json), [live diagnostics](Verification/SpreadFieldResidents/native-diagnostics.json), [final Play report](Verification/SpreadEverydayResidents/Native/b2a15be31b8644c097c0deb382855ba0/report.json).
