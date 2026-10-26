# East and kitchen current-scene follow-through

Status, 2026-10-26: kitchen initial service failure corrected and accepted in
ordinary current-source gameplay; three East sprite defects remain the next slice. Nella's corner
was accepted and pushed as `0a64468e5`; this is its next connected-scene pass.

## Actual route and observations

The isolated run continued the unchanged earned Nella fixture224401440 through
normal SampleScene, keyboard controls and existing world systems. No placement,
inventory, combat, camera position or service state was injected. Captures62–72
are actual Unity Game views. The read-only owner snapshots include offscreen
owners for diagnosis; they do not establish visual coverage of those owners.

| Evidence | Observed result |
| --- | --- |
|62/63|Nella31,18→North31,0→glade24,0. North's hedge required the established row0 detour.|
|64|East0,16, tick2320, HP40. In the glade the moving warden blocked40,9; Left, Down8, Right41 bypassed him. The initial suspicion of a water obstruction was incorrect.|
|65|East15,11, tick2540, HP31: actual scrabbler combat. Finite census1399 visible onscreen cells, zero unmatched receipts. Native forge, stoneburr, flowers and actors;11 plain Wall glyphs, one pen-gate glyph, two peat glyphs remain.|
|66|Inventory during combat recovery, HP12 after cindercaller encounter. DMG/DEF and rightmost equipped labels are visible. Ordinary tonic use consumed one carried tonic and restored15HP to27; no grant.|
|67/68|Forge reached17,5 at2730 and inspected at2×. Native world/reader identity matches. Crafting reports no carried components; no successful crafting claim.|
|69|South exit crossed at35,24→kitchen35,0, tick3110. Orven alive at67,5 in this first snapshot.|
|70|Only twelve Down inputs later, tick3230, Orven is dead. The game log identifies a viper; stock lies at63,7. This is not a staffed-service acceptance.|
|71/72|The player continued toward the court, then died to the two vipers at tick3580. The general living-player observer correctly could not record a removed player; a dedicated read-only death receipt and actual screenshot preserve the failure. No kitchen menu or job-state acceptance.|

The entire isolated replay was archived before finishing its owned isolation.
Normal preferences, save root, clean SampleScene and borrowed GameView restored.
Storage remains inspection-only.

## Named graphics defects for the next art slice

Independent actual-image review identified these bounded reuse opportunities:

1. East's plain Stone-material `Wall` owners are faint dark `#` marks. Reuse
   approved opening-cellar frame8 through exact East world/subject admission.
   Eleven were disclosed; the owner snapshot contains fifteen in total.
2. `FrontierPenGate` at54,19 remains `+` within the native hedge pen. Reuse
   sound gate frames1/2 from opening-states. Actual `DoorPart.IsClosed`, not
   `Physics.Solid`, owns its blocking state; there is no repair stage.
3. East peat at10,5 and11,5 retains `~` over native water. Reuse accepted
   neutral nella-corner frame16 while preserving actual water independently.

Keep approved forge, flowers, actors, hedges and water stable. Covered loot is
not a proven art gap. The older forge action menu is readable but a separate UI
polish candidate. Blank ground alone is not evidence of a rendering regression.

## Immediate prerequisite: a reachable living cook

### Design and source verification

Goal: fresh connected kitchen placement provides an ordinary speed100 approach
from its authored north port before an initially hostile creature can consume
the geometric clearance. This is an initial placement margin, not a promise of
permanent safety, every entry route, slow builds, player detours or later combat.

| Initial premise | Verified correction |
| --- | --- |
|Connected residents already share the Nella safety rule.|`SpreadExplorationResidents.initialSafety` admits only plot11.8; kitchen12.11 gets null faction and unlimited budget.|
|Three-cell footprint clearance protects the cook.|Viper SightRadius10 and Speed130 permit immediate acquisition of the cook seven cells away.|
|Passive/staying protects a service NPC.|BoredGoal checks hostile acquisition before idle behavior; Passive prevents the cook initiating combat, not enemies attacking him.|
|Reuse Nella's southern route literally.|Kitchen's connected entrance is north `(plan.NorthX,0)`. Keep Nella's south port unchanged.|
|All kitchen stamp cells are blockers.|Court stones, cot, chair, escrow and pickup are passable. Existing CourtPayload/CourtBlocks establish their actual semantics.|

Implement the smallest extension of the current cold-only rule for exact
connected WaysideKitchen12.11: obtain the baked cook faction without creating a
trial trader; consider the original live initiating hostiles and their actual
speed; use the north entry for the cheap lower bound and real cardinal route;
repeat the same proof at publication. For the guarded court use actual blocking
roles, preserving passable access. Keep the existing all-stamp Nella proof.
Do not remove/move enemies, resurrect saved residents, change poison/damage,
grant items, add invulnerability, or replay generation on cached/load graphs.

### Verification plan

RED first in a separate native kitchen fixture; seeds224401440,64,65,200893250
must retain a complete service and independently prove initial acquisition and
North-port speed-aware route margin. Pair initiating threats with benign/passive
population; normal and fast threats; no-safe-footprint refusal with source-owner
preservation; cached re-entry with exact identities and positions. Run neighboring
Nella and resident/court suites. Independent source review before commit.

Then start from an earned pre-kitchen fixture (never the already dead cached
zone), follow the new actual north-port route, capture the living cook, normal
conversation/trade or work menu, reload and revisit. Keep old failure evidence.
Native test success alone cannot close this gameplay gate.

### Scope and limits

Qud influence is readable native objects and systemic consequences, not copied
Qud code/art. No new sprite or conceptual reimagining is required for this
generation prerequisite; the prior approved native art and its production
pipeline remain intact. Current packages47d678995 still predate recent source
fixes. No packaged or physical Steam Deck acceptance follows from this pass.

### Implementation log

- Captured ordinary East and kitchen failure; archived the isolated save graph.
- Independent route/source and actual-image reviews agreed on the three art
  defects and the kitchen initial-safety omission.
- Native first launch ran zero cases during fixture import and is not evidence.
- Confirmed RED: all four native seed cases failed on initial acquisition or
  northern approach margin before production changed (kitchen-approach-red.json).
- Production now selects the exact kitchen north port, baked cook faction and
  existing role-proven passable court; Nella keeps its southern whole-stamp
  policy. Counter-checks, GREEN, independent review and actual replay pending.


### First implementation review

The initial extension passed two seed cases but refused the entire kitchen in
224401440 and64. Expanded run:8passed/4failed; two were those genuine site
refusals, two were an invalid test precondition that equated real cardinal path
length36 with Manhattan34. The speed oracle must use its actual BFS distance.
The source failure is `no-safe-footprint`, not trial-budget exhaustion.
Investigate the guarded court's passable stones versus reserved-route placement
before accepting any broader exception. No acceptance or final GREEN yet.

### Verified cause and revised design

The candidate census disproved the reserved-floor theory: seed224401440 had66
budget-eligible orientations but zero physical fits even with a virtual floor
exception; the nearest failures were existing hedges, not reservations. Seed64
had zero budget-eligible candidates. No floor-admission exception is justified.

The placement-only plan cannot reliably create this required inhabited stop.
Use the existing pure population rewrite seam instead: after the ordinary table
roll and exact cold-attempt admission, the connected WaysideKitchen12.11 replaces
each ordinary Viper/MarlbackScrabbler encounter roll one-for-one with an existing
ReedbackGrazer. Preserve count/order, other rolls, and placement RNG selection.
Validate the exact two-row encounter schema; malformed tables and a missing
replacement blueprint retain the original roll. No new creature definition,
post-publication sweep, health change, invulnerability or global faction change.
The neighboring East retains its predators. This kitchen becomes a quieter
inhabited beat in the journey; grazers remain ordinary interactive creatures.
Keep the initial kitchen safety proof as defense against other initiating source
threats. Saved/cached graphs remain literal, including the already dead cook.
This is an explicit design divergence from a placement-only correction.

The implemented source substitution retains exact changed roll indices in the
ordinary encounter receipt. Pre-existing grazers and Magpie-origin ambient
receipts remain separate. New grazing jobs or finite-food bindings are not
created. The original pure-policy reflection fixture passes the explicit false
opt-in so its former family behavior remains under regression coverage.


## Accepted kitchen correction — 2026-10-26 17:04 UTC

**Native verification:** 33 kitchen checks passed (12 placement/speed/cached-state
checks and21 pure/caller population checks), then71 neighboring Nella, working
court, resident and prior source-policy regressions passed. Earlier RED/failure
receipts remain, including the disproved reserved-floor hypothesis. Independent
source review found no material remaining issue.

**Actual gameplay:** the earned East backup was copied unchanged into an owned
isolated save root. Before entering, its cached zones contained East, opening,
cellar, North and Nella; kitchen was absent. Normal movement took East's southern
approach to its north entry. Capture74 shows both actual grazers and living
Orven. The full previously fatal approach reached66,5 at tick3250 with player
HP40 and Orven HP20. No actor, inventory, clock or position was injected.

| Capture/receipt | Actual observation |
| --- | --- |
|73/74|Earned East0,16 tick2310; kitchen35,0 tick2750. Two grazer owners13611/13612 replace the ordinary predator slots.|
|75|Real2× kitchen court, live cook13631 at67,5.|
|76|Interaction action menu, despite the filename saying conversation.|
|77|Actual Orven conversation with matching native cook portrait.|
|78|Actual five-item trade stock.|
|79|First purchase confirmation was canceled; no transaction occurred yet.|
|80/81|Confirmed ordinary raw-meat purchase: player39→30drams, cook35→44; exact item13632 transferred. F5/F6 preserves the complete observed service/player/stock/custody snapshot at tick3250.|
|82/83|Kitchen→East→kitchen border return. Same cook13631 remains HP20; same pan13649 remains Idle and bound to him. Ordinary time advanced to3630, cook moved to67,7 and his money became100 through existing restock behavior. Four remaining stock IDs and player30drams persist. This is not a claim that time freezes during travel.|

The new unchanged earned kitchen fixture is preserved for subsequent work.
Normal preferences, SampleScene and GameView were restored. Hash verification
keeps all776 normal save files,374 approved source/art files and2 unrelated
screenshots unchanged. About421GiB remains free. Storage inspection only; no
cache or drive reclamation occurred.

### Review and limits

- 🟡 Fixed: ordinary cold predator placement killed the required cook before
  service contact; a placement-only guard also omitted the required site in two
  tested seeds. Exact source substitution plus approach guards resolves both.
- 🔵 No new animal/mechanic/art, no post-generation sweep, no protection or
  resurrection of saved residents. Future cold kitchen generation in an opted-in
  world changes; existing generated kitchen graphs remain literal.
- 🔵 Script-observable: exact live actors, currency transfer, stock ownership,
  pan binding, persistence, source counts and restoration. Visual evidence:
  current-source Unity77/78 and world75 at actual1280×800.
- 🧪 Not verified here: physical Steam Deck, updated packaged runs, every seed,
  every kitchen job state, permanent NPC safety, or complete whole-world art.
- ⚪ Existing trade and small interaction menus still use older UI styling;
  readable in this run, separate from the newer inventory and portrait work.

Files changed: PopulationBuilder.cs, SpreadExplorationPopulationPolicy.cs,
SpreadExplorationResidents.cs, two new native fixtures, prior source-policy
reflection call, this design/review and real captures/receipts. No approved art
changed for this prerequisite. Next: the three exact East fixture art defects,
then consolidated current packages and remaining whole-scene acceptance.
