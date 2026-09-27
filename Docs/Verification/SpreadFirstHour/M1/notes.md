# M1 first-hour guidance — implementation record

Status: implemented; native132/132 guidance subset PASS and matching standalone differential GREEN. Ordinary route passes six named gates and triggers the real stump stage; quest reward/shop/load remains unverified. Original CoO dialogue, no Qud reference. Baseline `0417e1458683dd0fbebb765ed5a8834e2d447066`. Root owns the plan, Unity and live acceptance. This slice owns conversation presentation/core, two quest JSON files as needed, and dedicated tests. No world placement, quest actions/rewards, save schema, item UI, rendering or encounter changes.

## Sweep corrections and readiness (before production)

| Verified source | Correction / implementation boundary |
|---|---|
| ConversationManager.CurrentText already composes LocalPeople and WorldTravellers | Add one read-only contextual layer; do not mutate shared NodeData. Visible choice labels also need a copied presentation value for the glade's misleading “this village” option. |
| ReferenceGladePlan.IsActive plus WorldLocationContext/current CachedZones | Exact glade address alone is insufficient for managed worlds; require actual map/content authority and live current participants. Preserve ordinary village Warden text. |
| RegionalGuidance.BuildSignpost / BuildDestinations | Reuse geographic current-map selection without work promises, auto-notes, generating zones or consuming RNG. Existing sign behavior stays intact. |
| VillagePopulationBuilder.PlaceBmoQuest | Actual OldStump carries QuestMarkerTrigger(Fact=bmo_stump_reached,Value=1), with no east placement contract. Resolve only one current, ground-owned, matching marker in canonical Sill. Missing/ambiguous/foreign marker gives unavailable wording. |
| RootBeerGuyCase objectives / CompleteObjectiveOnTaken / fact | Notebook completion is sticky once earned; a carried notebook or preacceptance gremlin fact also counts. The report stage clears current-stage objective flags, so presentation must consult report/completed state as well. Do not require already-finished work again or change action gates. |
| Shared quest conversations | Remove unconditional east/building claims from Ellun fallback JSON; Hallun fallback explicitly names both objectives. Keep all choice actions, predicates, targets, rewards and IDs unchanged. Wardens.json need not change: contextual speech and copied labels leave genuine village wardens intact. |
| Lore/Voices/VOICE-CARDS.md §11 and MYSTERY-LEDGER | Plain local concerns, no new cosmology, lore people, disease or invented payable hunt. |

## Bounded plan

1. Tests through real ConversationManager.CurrentText/VisibleChoices against current production (executed RED).
2. Narrow FirstHourGuidance presentation helper plus composition/choice hooks and geographic helper reuse; surgical quest prose replacements.
3. Focused core GREEN, dedicated adversarial owner/map/state/save/read-only checks, nearby dialogue/content regressions and independent review.
4. Native focused tests and ordinary player route remain root-owned pending gates. Standalone verifies core logic/data/save only, with stubbed Unity and non-Unity-identical generation hashes.

## Evidence

Initial standalone RED executed: **25 cases, 23 intended failures, 2 reward/gate controls pass**. `red.xml.gz` retains the original output. Assertions exercise unchanged production CurrentText/VisibleChoices; no missing-API compile shortcut. New .cs files receive fresh .meta. Raw before/after evidence will be retained here. No claim of native or visual acceptance until actual root receipts exist.


### Dedicated adversarial pass

Executed **66 combined cases: 61 pass, 5 failures** (`adversarial-red.xml.gz`). Three stale inventory cases (wrong owner backlink, equipped book left in list, ground/list double ownership) could falsely count a carried book. A solid stump could give an impossible step-on lead. The fifth original failure used a mistaken faction premise: Player resolves to the Player faction despite a Villagers tag. Corrected the test to use FactionManager.GetFaction and added an unlike-faction control; the exact pre-fix helper produced **1 RED / 1 control PASS** (`faction-corrected-red.xml.gz`) before confirming the player-excluding trigger fix. Candidate fix now requires current carried physical ownership and an operable nonblocking player trigger. Other 36 dedicated cases passed, including current-map invalidation, hostile/dead/stale owners, duplicate markers, saved graph identity, sticky/report/completed progress and read-only repeated rendering.

Fixture-only first compile corrections: use the real PointOfInterest constructor; guard UnityEngine.Random.state assertion with the Unity-version symbol because the standalone stub lacks that type. No production dependency or native assembly reference added. Player-property immutability uses actual key/value arrays, not JsonUtility dictionary serialization.


## Implementation and files

`ConversationManager.CurrentText → FirstHourGuidance.Describe(...)` reads current context. `RefreshVisibleChoices → PresentChoice(...)` copies labels only where needed, retaining exact authored action/predicate/target references. `RegionalGuidance.BuildGeographicDirections` shares the existing sign's map resolver without generating destinations or recording notes. Ellun reads the current Sill marker; Hallun reads sticky objectives/report/completed state plus actual carried book and existing facts. No setter is called by these presentation methods.

Production: new `Assets/Scripts/Gameplay/Conversations/FirstHourGuidance.cs` (+meta); modified `ConversationManager.cs`, `RegionalGuidance.cs`, `Assets/Resources/Content/Conversations/BMO_Quest.json`, `RootBeerGuy_Quest.json`. Tests: new `FirstHourGuidanceTests.cs` and `FirstHourGuidanceAdversarialTests.cs` (+metas) in `Assets/Tests/EditMode/Gameplay/Conversations/`.

Scope divergence: the plan named LocalPeople and three dialogue JSONs as possible surfaces. A separate narrow presentation helper keeps LocalPeople's culture/naming contract unchanged. Wardens.json remains byte-identical so ordinary village Warden prose remains intact. The two quest JSON edits touch text only (`content-diff.json`); quest source definitions, actions, predicates, targets, rewards, IDs, world coordinates and save schema remain unchanged.

## Core evidence at private handoff (historical) and readiness

- New fixtures: **25 core + 42 dedicated adversarial = 67**. Nearby five existing fixtures contribute65. Exact seven-file standalone corpus: baseline **67PASS/65FAIL** → current **132/132PASS**, zero newly failing. All65 newly passing cases are new guidance assertions (many adversarial baseline failures intentionally occur at the positive setup witness before testing refusal). Two unchanged reward/gate controls already passed. `nearby-baseline.xml.gz`, `nearby-final.xml.gz`, `differential.txt` preserve exact results.
- Selection: FirstHourGuidanceTests; FirstHourGuidanceAdversarialTests; ConversationTests; NoFightConversationTests; QuestBmoContentTests; QuestRootBeerGuyContentTests; QuestObjectiveConversationTests. The runner lives only in `/tmp/coo-first-hour-m1/runner`; baseline source is copied before production under `/tmp/coo-first-hour-m1/before`, with the final same test corpus. No tracked runner selection was touched.
- Peer read by combat_density found no additional concrete blocker after physical ownership and trigger corrections. Its review covered copied choices, current graph/quest owner gates, report-stage progress and marker ownership. The author also read actual planner/M2 peers independently; those are separate scopes.
- Native same fixtures, Unity RNG immutability assertion and native save behavior await root. No ordinary keyboard discovery, visual fit, actual Sill task reward/purchase or first-hour pacing is claimed by these unit cases.

## Q1–Q4 and remaining limits

- **Q1 symmetry:** speech and choice labels use the same live context. Genuine village Warden speech/choices remain authored; missing/dead/foreign context does not borrow glade/quest progress. Sign path still uses its exact former geography resolver.
- **Q2 consistency:** all new presentation is read-only, with no new gameplay command, inventory transfer, save field or recurring diagnostics. Existing action routes emit their existing diagnostics; unavailable marker speech explicitly states that no current direction is available.
- **Q3 counterchecks:** map changes, dead/removed/replaced graphs, hostile actors, forged part backlinks, invalid/duplicate/foreign/inoperable markers, stale books, preacceptance/sticky/report/completed state, repeat reads and saved owner IDs are exercised. Native-only Random.state assertion is intentionally not claimed by the stub runner.
- **Q4 drift:** no fabricated service, bounty or surviving loot promise; glade gives current geographic directions only. Ellun direction is relative to the current speaker, not a fixed east/west table or path-safety guarantee. Marker discovery does not mark cells explored or reveal an enemy; no route safety promise. Hallun completion wording follows existing sticky objective semantics even if the notebook later leaves inventory.
- 🟡 Fixed: stale book ownership and inoperable marker claims (executed adversarial RED above).
- 🔵 Corrected fixture premise: actual Player faction, constructor and standalone Unity-type boundary; raw evidence retained rather than relabeled production defects.
- Native guidance67 now passes; the ordinary journey has bounded partial acceptance and is closed after two observer refusals, as detailed below. Reward/purchase/save journey gates remain unmet. Existing full-suite results are not borrowed as new-work evidence.

## Actual focused native and ordinary-route readiness

`../focused-integration-01.xml.gz` contains all67 new guidance cases plus65 conversation/quest neighbors passing in actual Unity EditMode (132/132 in that subset). The larger233 selection had unrelated presentation failures and is not claimed wholly green. Standalone132 is the separate matching core differential.

`NativeDriver/PLAN.md` and exact manifests describe the private seed1 ordinary glade→Sill native-key route. Runtime/editor-reference compilation is clean and independent peer source/reward/keys/save review is clear. The4 driver/meta files were subsequently published after the separate2 missing-launcher restoration RED; the two actual journey outcomes are recorded below. Actual current-map directions, Ellun current marker/120XP40drams, affordable real gear purchase and F5/paid-step/F6 graph are required; finite source/threat failures remain incomplete. No grants, transfers or fabricated services.

Root published the4 ordinary-route source/meta paths only after the actual2 missing-launcher restoration failures in `../source-art-save-paging-01.xml.gz`. The actual route then ran twice; neither completed all planned gates. The closeout below replaces the earlier queued status.

## First ordinary route and one bounded observer correction

Actual native run `58010485df1d45d7aa2fa5f5012b7041` passed only `ordinary_start`, then took two real paid steps to `(39,14)` at tick30 and40HP. The observer could not find a radius-3-clear approach to Warden2077 while hostiles occupied `(40,20)`, `(41,20)` and `(21,3)`; live records show ordinary Villager/Scrabbler combat, not player injury or a gameplay path defect. Raw report and screenshots remain immutable in `M1/Native/58010485df1d45d7aa2fa5f5012b7041`. Parent viewed the start image: approved3D rendering was active; this failure establishes no new graphical issue. Editor/start scene/seed/save-root restoration completed.

Parent authorized one harness-only ordinary-risk adjustment before deferral: named threat clearance1 across path/prestep/checkpoint, preserving current physical/door/hazard ownership and >10HP stop, finite inputs and no grants/AI/RNG/world changes. No waiting or reroll loop is added. Exact target/approach/current-neighbor refusal diagnostics use copied scalar state, and report `passedChecks`/`unmetChecks` are separate from intended capability. combat_density independently reviewed this bounded change and found no concrete blocker. Native retry remains pending; quest, purchase and replacement-save gates are still unproved by this journey.

Q1–Q4: the failure is observer conservatism, not a confirmed broken Warden/quest; action/payment and live owners stay unchanged; refusal observations now expose goal and local reasons; one seed and one retry cannot establish broad first-hour safety or campaign balance. Implementation core native67 remains separate evidence.

## Second ordinary route: bounded closeout, no third broad retry

Native `4fdbfe4fa8b24a21bb8a80e05feabfe1` ran26.55s with65local inputs/one world-map step. Six named gates passed: ordinary start, current glade Warden lead, actual sign, native map arrival in Sill, current Ellun directions, and quest acceptance. Player remained40HP. Parent viewed all five retained images; actual objective completion is visible at the stump.

The raw `native_marker_reached` FAIL is an observer false-negative: the player reached exact `(66,3)` with `bmo_stump_reached=1`. Fresh actual quest records `ObjectiveFinished` (`a832065f`, reach_stump/search) then `StageAdvanced` (`399404e6`,0→1) prove normal progression. The driver incorrectly queried the previous stage's objective after automatic advancement; `StoryletPart.IsObjectiveFinished` intentionally covers only the current stage. Raw report remains unchanged.

The later return observer stopped at `(54,3)` while actual Ellun moved from `(51,8)` to `(25,5)`. All eight player neighbors and five of Ellun's current adjacent approaches were admitted, with no nearby hostile at those cells. The BFS could not connect them under its physical/hazard/creature/clearance policy; this diagnostic does not identify a gameplay path defect. No third broad bot iteration is planned. Authored reward, merchant purchase, replacement-save and repeat-reward checks remain unmet in this journey, backed only by separate core tests where applicable. Exact cleanup/restoration completed. `NativeDriver/retry-closeout.json` records this correction without retroactively changing the raw audit.

Q1–Q4 closeout: distinguish current-stage and historical quest evidence; use actual moved source coordinates; retain the two real native failures rather than infer an economy/path defect; six passed gates plus exact search progression are useful bounded acceptance, not a completed first-hour campaign. The old objective prose “out past the village” is an inexpensive neutral-text candidate; root owns any separate data change.

Root journal closeout: changed only the neutral stump objective text from “Find the old stump out past the village” to “Find the old stump”; quest/stage/reward/trigger IDs are untouched. Parsed data proof: `journal-copy-diff.json`.
