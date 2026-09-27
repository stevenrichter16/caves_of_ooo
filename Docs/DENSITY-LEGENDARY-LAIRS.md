# C11.3 — exceptional lair keepers and one visibly equipped enhancement

Status: published; native C11.3 scoped tests and the combined19-check acquisition/save route passed. Actual successful Play receipt: `HigherTiers/NativeAcquisition/2863b2680c074bc9bd7aece4fddc6381/report.json` (complete=true,19/19,0 failures and0 unexpected errors). CoO-original content, not a Qud-parity claim. The earlier private/native RED stages and pending statements below are historical checkpoints, preserved as evidence.

## Outcome and narrowed scope

A minority of fresh, later marlback lair keepers should have an original saved name and a visibly described, actually equipped exceptional item. The player faces that item's real existing mechanic and may recover the same entity through normal disarm/death/pickup. The encounter keeps its existing stats, faction, AI skills, stock count, ordinary corpse and loot table. No free hidden reward or extra spawned creature is added.

First source: new C10.2 final floors, selected boss exactly MarlbackWallkeeper, actual POI tier2–3 in Spread/Sodden. Default35% stable source roll; test-only explicit0/100 controls. Actual current Spread region is tier1, so its eligibility is future/custom-map compatibility, **not claimed current reachability**. Measure all lairs in the five established world seeds plus a bounded wider cohort before accepting the source. Do not fabricate a higher-tier Spread POI as evidence.

Two original templates use the actual guaranteed current loadout:

| Template | Existing equipped entity | Existing enhancement | Encounter/readable distinction |
| --- | --- | --- | --- |
| Notched edge | LongSword in hand | Serrated tier1:10% additional bleed on hit | Notched edge on the actual visible sword; no damage/stat multiplier |
| Lacquered harness | LeatherArmor worn on body | Lacquered tier1:+1 AV while equipped | Lacquer on the worn armor; actual creature examine lists the current carried item and effect |

Names use a small original pool (Rusk, Drel, Marn, Tav, Vek, Borr, Kerr, Venn); physical epithets use notches, shale, scars and harness wear. No names were found in current Lore or Content. No gods, hidden history, prophecy, Urqu agency, under-text explanation or Gin Frog speech. New generated people are not canonical fixed actors. Display retains `(marlback wallkeeper)` for threat/species recognition; BlueprintName, source art binding, faction and body stay intact. No new models are implied; native held-weapon/armor fallback plus examination must be inspected.

## Verified corrections before production

| Tempting premise | Actual source | Consequence |
| --- | --- | --- |
| Legendary templates already exist | No active legendary generator/part/source was found; C11 plan remains open | Implement one bounded source, not a broad generation framework |
| Spread has later eligible lairs | WorldGenerator uses WorldMapAuthoring.TierAt; current Spread examples are tier1 | Require actual-source census; label current source as Sodden if borne out |
| Every boss can hold an enhanced reward | Beating uses DesertProwler, Grovelands JungleStalker; neither is an equipped marlback keeper | Exact family/loadout allowlist, no manufactured beast/Choir reward |
| Upgrading requires creating another weapon | MarlbackWallkeeper already equips LongSword and LeatherArmor | Improve one existing equipped entity; item count and ordinary value stay unchanged |
| ItemEnhancing adds names or Commerce premium | Apply attaches a saved enhancement and runs equip hooks; no name or value policy | Explicit readable prefix; preserve Commerce, do not invent a premium |
| Lacquered automatically affects a held item | Apply requires the actual wielder to run OnEquipped immediately | Pass the real keeper and prove +1 AV and later unequip/death removal exactly once |
| A name/ID claim survives zone unload | Ordinary unload discards native graphs | Reuse C10.2 retained final-floor ownership; no extra global ledger or wilderness source |
| Blueprint mutation upgrades old saves | Save streams rebuild serialized parts | Only fresh staged final bosses opt in; legacy lairs and cached bosses retain serialized content |
| Generic random enhancements are table metadata | Loot tables return blueprint strings; no enhancement metadata exists | This slice wires a specific fresh encounter; general enhanced finds remain open |
| Equipped items retain InInventory=actor | InventoryPart clears InInventory and sets Equipped for worn gear | Check separate equipped versus carried backlinks; the initial over-strict prototype was corrected before source wiring |
| An enemy's static description always tells carried loot truth | Items may be stolen, disarmed or dropped later | Legendary part produces a live carried/equipped reward line; absent reward produces no false item claim |

## Proposed narrow implementation

New `LegendaryLairEncounters` and simple saved `LegendaryIdentityPart`. Invoke the helper in the C11 private copy of `OverworldZoneManager.CommitGeneratedZone` **after generation callbacks and immediately before C10.2 final commit**. The first implementation still trusted a post-callback blueprint search; independent review demonstrated that this was insufficient. The reviewed design uses `LairStacks.TryPlan` for source eligibility, then a non-consuming `TryGetValidatedFinalBoss` query for the exact staged owner and the same all-owner/route preflight used by commit. Refuse legacy, wrong biome/tier/floor, previously generated final, missing/replaced staged owner, invalid pending cache/stair or cached counterpart, and changed authored display.

Require living native zone membership and complete body cells, expected blueprint, Boss+Creature tags, ordinary hostile faction, and unchanged expected loadout. Refuse Player/service/conversation/unique/protected/rental/temporary/transformed/stale actors. Resolve exactly one chosen equipped reward through InventoryPart and Physics backreferences, require supported ordinary blueprint, no prior enhancement, no stack/mixed owner, and the expected enhancement type registration. Do not rename an already named actor or overwrite modified item labels. No creation of replacements when gear is missing; keep baseline encounter and diagnose refusal.

Use an explicit stable hash of world seed, canonical surface and domain separator for the35% roll, template and name. Do not consume passed generation/combat/Loadout RNG. Save name, epithet, template, source address and reward ID in simple public fields; actual Render/item parts carry their native state. Applying twice is a no-op. Existing saved final graphs retain the same item identity, modifier flags, injury and cooldowns. No hook on cached retrieval, zone attachment, EndTurn or frame update.

`ExaminablePart` receives one narrow optional line from the identity part. It names only an item still in the actor's actual inventory/equipment with matching ownership and current enhancement; it does not claim a remote/destroyed/transferred item remains carried. Item inspection already lists exact enhancement mechanics. No conversation is added.

C11 implementation must be a new directory/copy and tiny reviewed diff over finalized C10.2 sources. No C10.2 file is changed. No Objects.json, LootTables or save-format extension is needed: the existing public-field part serializer records primitive metadata and current item parts.

## TDD / acceptance sequence

1. Before production, execute API-missing RED and actual blueprint/kit/source controls. Source census enumerates real map POIs, their tiers/bosses and final graphs. Record the current original entity/gear count/value.
2. Core tests:0/100 and default roll has both outcomes; deterministic selection without gameplay RNG consumption; exact source gates and refused legacy/early/Choir/Overwrit/Stump/beast/service contexts; two templates modify exactly one real item; stat/skill/faction/item-count/Commerce invariants; correct live inspection.
3. Integration: hook-off RED with production helper present proves no orphan helper. Actual unforced world seeds encounter at least one marked final boss; its real gear is enhanced, normal controls unchanged. Cached/revisited/final lower-first and save before/after injury preserve name, item ID, bonus and cooldowns; corpse/death spill grants exactly that item once. Removed/looted reward and dead boss never regenerate under C10.2.
4. Adversarial: wrong/missing equipped backrefs, foreign zone/body, duplicate IDs/gear, stale/dead/renamed actor, existing modifiers, unsupported factory/registry, at-slot-cap, invalid chance/canonical address, changed source callbacks, part reflection and no reroll after save. Diagnostics channel-on/off and descriptive text stay pure.
5. Appropriate existing enhancement/equip/save/lair affected suite; full integration differential remains root-owned. Dedicated new tests/metas and durable raw RED/GREEN hashes before publication.
6. Native: ordinary-stat actual-source encounter, both item inspections/renderer fallbacks, real fight or disarm/drop, pickup/equip, save/reload and finite return. Staged demonstration separately labelled if needed to see both profiles. Core runner proves neither Unity-identical layouts nor combat balance/feel.

## Self-review and explicit limits

🟡 Source reachability, exact equipped reward ownership and save/drop symmetry are blocking acceptance checks. 🧪 Tier1 Serrated's extra bleed and Lacquered's+1 AV require ordinary native fight evidence. ⚪ This is one original enemy family at one existing retained source, not a broad legendary bestiary, generalized random enhancement loot, extra high-tier bosses, reputation histories or new status systems. Those remain open C11 scope and must not be labelled complete by this slice.

## Private first implementation log

Initial10 cases:8 intended missing-feature/source RED and2 actual source/loadout controls GREEN. Helper present before source hook:9GREEN and1 source-wire RED. After the private post-callback commit hook:10/10GREEN. Five real maps contain18 lairs,7 eligible keepers (all Sodden tier2),6 Spread lairs all tier1,5 other boss lairs.

The first expanded28-case review found one real naming-distribution defect: direct low-bit modulo collapsed64 forced encounters to8 coupled name/epithet pairs. Stable final hash mixing now separates source bits before modulo; no gameplay RNG is consumed. A death/save test also initially searched only carried Objects, but real Pickup auto-equips compatible rewards; that fixture now checks the distinct carried+equipped union and is explicitly a setup correction, not a production fix.

## Current private evidence and boundaries

The source-hook-negative receipt is `helper-before-hook.xml` (9/10 pass, only actual generated source fails); the original10-case missing-feature receipt is `initial-red.xml` (8RED/2controls). The34-case ownership review had two intended REDs for missing boss ID and duplicate real reward IDs (`ownership-red.xml`,32controlsGREEN). Both are refused before naming/enhancement. Current affected sweep is **767/767GREEN**:34 new cases plus733 existing lair/enhancement/equipment/save/neighbouring checks. This includes the fixed C10.2 516-case unit.

Current natural cohort, seeds1–32:31 eligible Sodden final keepers,13 selected (6 serrated,7 lacquered),18 ordinary. Both profiles occur without forcing; this42% finite observation is not a new chance setting (configured35%) or a prevalence promise. The established five-seed source table remains18 actual lairs with7 eligible Sodden keepers and6 tier1 Spread controls. All36 old/new rows of the existing18-lair census retain exactly the same stock counts, species, cache contents and Commerce fields with the legendary hook enabled. Modified combat utility is not assigned an invented monetary premium.

Actual runtime source compilation against Unity reference assemblies succeeds with zero errors and existing warnings; no native Unity execution occurred. The source compiler does not prove test-assembly compilation or Play controls. Independent review is requested before publication. Native ordinary-stat boss combat, live reward inspection/render fallback and pickup/equip/save remain open.

Private changed files: `LegendaryLairEncounters.cs` (includes saved identity part), `DensityLegendaryLairTests.cs` plus fresh metas; tiny private `OverworldZoneManager.cs` and `ExaminablePart.cs` hunks in `reviewed-hooks.patch`. No Objects, LootTables, art, shared Assets or finalized C10.2 files changed. `candidate-manifest.json` records hashes. Root owns publication and final native gates.


## Independent staged-provenance review closure

Independent review found a real ordering defect: the helper selected an ordinary
blueprint match after callbacks but before C10.2 rejected a replaced staged boss.
A separately owned wallkeeper could therefore be named/enhanced in a graph that
never published. IDs alone did not provide authority. The first five new probes
were all RED (34 controls GREEN); expanded actual callback and invalid-route
coverage produced eight RED (34 controls GREEN) before the repair.

A separate C11 copy of `LairStacks.cs` now extracts the existing commit checks into
`TryPrepareCommit`, preserving their order and publication behavior. The new
non-consuming `TryGetValidatedFinalBoss` checks pending actual references, plan
context, all native owners and proposed stair routes, then returns only that
pending boss. Legendary selection uses this returned reference. It never consumes
the pending claim or publishes a ledger/connection. Commit reuses the same checks.
The finalized C10.2 production files were not modified.

The eight paired counterexamples cover replacement with different/same ID, actual
unforced `OnZoneGenerated` replacement with different/same ID, missing staged
cache/stair, an obstructed cached counterpart, and a lookalike unstaged graph.
`staged-owner-expanded-red.xml` records 8 RED/34 GREEN; the reviewed repair passes
**775/775 affected cases**, including all **42 new C11 cases** and the unchanged
C10.2 516-case unit. Runtime source compilation against Unity references again
has zero errors. The independent reviewer reread the exact pending query and
shared preflight and found no further concrete ownership, save-field or canon
issue in this bounded review. No Unity execution is claimed.

C11.3 remains private until root completes C10.2 native acceptance. Its three
existing-source hunks are manager source wiring, a live examination line, and the
shared commit-preflight query in LairStacks. No whole private manager is to be
published; no standalone hashing adapter belongs in Assets.


## Publication and current acceptance boundary

The C10.2 native route completed14/14 checks with zero errors in actual Play, so root authorized this bounded unit. Published exactly two new source/test files with metas and three reviewed narrow hooks; the separate native acquisition player/launcher has two files plus metas. No full private manager, standalone hash adapter, Objects, loot tables, art or save schema was copied. `Docs/Verification/DensityCompletion/HigherTiers/Legendary/` preserves raw compressed RED/GREEN receipts, patch, manifests, review and native driver plan/compiler evidence.

The combined acquisition audit is compiler-green only. It uses ordinary new-game stats, one actual actor, two labelled source starts, bounded real generated-source selection and actual starting controls/earned gear. Native keys must obtain the actual key, unlock, take, equip, inspect, defeat the real guardian/keeper, acquire the exact enhanced drop, and prove F5→real mutation→F6 graph restoration. Equipment save proof includes exact inventory slots, body-part ownership and Physics links. All failures/candidates remain recorded. Root added two launcher restoration cases (12 total across launchers); native execution remains pending.


## Native scope and first acquisition environment correction

Root's second native selection recorded484 cases:481passed and3 unrelated humanoid-height failures. The42 C11.3 tests, C10/C11 source/ownership/save scopes and modified-find scopes were GREEN in that selection (`Integration/native-sixth-c11-second-red.*`). This is scoped acceptance, not a full-selection GREEN claim.

The first actual combined acquisition attempt (`HigherTiers/NativeAcquisition/afe6159bf2bd4b8fb89f6478a39c2ab8`) stopped before acquisition:96 generated zones produced16 finds, all lacking the enhancement-roll marker. The initial harness mistakenly labelled these ordinary chance misses. Read-only native inspection showed disabled domain reload had retained EnhancementFactory's initialized registry containing only four ItemEnhancingTests stubs and none of the required production types. The report, full log and exact restoration receipt are preserved; this run is invalid as a source-frequency census and does not establish sixteen random misses. No loot chance, seed, source or reward was changed.

The harness now performs normal lazy initialization before new-game input and requires exact registration of the five actual enhancement classes used by found equipment. It reports the registered type for each, refuses a contaminated domain without resetting or repairing it, and adds a required preflight check (19 total). Candidate records distinguish missing roll markers, marked zero-modifier chance misses and unexpected modifier counts. Fresh runtime and editor-source compilation against actual Unity references both pass with zero errors. Root will run Play after compilation and before unit fixtures; acquisition remains unverified until that real run completes.


## Fresh-domain acquisition reached the native equipment confirmation

The fresh-domain retry `HigherTiers/NativeAcquisition/1f56e919e32d40b98c2567bc1285ab2f` passed the production-registry guard. Six real generated zones yielded the selected GlowQuartz CounterweightMaul in `Overworld.8.1.9`; the fixed world also supplied an unforced keeper in `Overworld.18.2.1`. Native acquisition passed the actual stamped-key pickup, key unlock and cache collection at40HP. It did not complete equipment/combat/save gates. The full failure report, key trace, log and exact restoration receipt remain preserved.

Source review matched the next failure to the audit's menu handling: `equip_auto` opens the real displacement confirmation when a two-handed maul replaces the starter dagger. The generic close helper sent Escape, cancelling that confirmation, then closed the item menu and inventory. This was a harness cancellation, not an equipment-production failure, death or loot miss. A private repair verifies the popup's exact requested item and actually equipped displacement owners, presses native Y, requires the confirmation to close and the item to be genuinely equipped before closing menus, and includes popup/item/status details in each key snapshot. Runtime and editor reference compiles pass with zero errors. Publication/native retry remain root-owned; no direct equipment mutation, changed source, changed seed or reward grant is introduced.


The equipment-confirmation repair is now published in the native player only, after an exact prior-source hash check and independent comparison with InventoryUI's actual displacement path. No bed or gameplay source changed in this window. Runtime/editor reference compilers pass; the19-check native acquisition retry remains pending.


## Earned advancement modal handling

Actual native retry `HigherTiers/NativeAcquisition/3dca03893470443c9c829cabf5949dec` passed9 of19 required checks, including actual modified-maul equipment, truthful inspection and the real guardian's defeat. Killing the guardian awarded550XP and naturally raised the actor to Level3 and44/44HP. The harness then encountered the deferred native Level2/Level3 announcement queue during the next stage's approach and stopped. This was an undismissed earned modal, not death or a gameplay regression. Original report, trace, log and exact restoration receipt remain preserved.

A narrow native-player repair now lets the actual UI open the pending queue at combat/walking boundaries, accepts only exact earned-level text corroborated by the actor's real level and message log, and presses Escape. It verifies each dismissal preserves every stat base/value/min/max, current gear/body slots, HP, position, tick and energy. It does not consume queue APIs, reset gains or dismiss unknown prompts; item inspection stays explicit. Independent review found no concrete blocker. Current runtime and editor reference compiles (including the newly published bed code) pass with zero errors. Only this audit player was published after the exact prior-source hash check; the19-check Play retry remains root-owned.


## Native owner approach repair

Actual run `1520cd447eb6498ca32fb875b38fc56d` preserved the naturally earned advancement and reached `Overworld.18.2.1` at `(37,11)`, Normal and44/44HP, before refusing a safe route. Its original report/log/restoration remain untouched. This was a conservative harness route refusal, not player death. The previous receipt did not record the boss coordinates; specific target geometry is therefore an inference.

Source review found that the audit selected one adjacent endpoint before walking, although native creatures and visibility can change after every action. The existing control helper only cast from the current ray, so a newly visible, nonaligned active enemy could invalidate the chosen route without a way to reach a valid casting position. The repair recomputes actual owner reach after each real key, and if needed walks through the same safe-cell predicate to a position with a ready, real Calm or Rime Grip first-impact line. Actual cooldown/effect checks and the two starting-tonic limit remain unchanged. It records nearby owner positions, visibility, control states and each planned path. It neither relaxes the threat exclusion nor changes the world, seed, rewards, actors, AI, stats or abilities.

Independent source reread found no concrete blocker. The safety predicate covers current visible active threats, physical occupancy and triggers; it is not a claim of immunity to every environmental hazard. Movement/control failure or the160-step bound still ends the audit honestly. Current runtime/editor compiles against actual Unity reference assemblies both pass with zero errors. These compiles and the source review cannot establish native-route success; root must run the19-check Play gate after publication.

The repair is now published in the native player only after exact prior-source byte/hash validation. The narrow patch, independent review, compiler logs and updated source hash are preserved in the Legendary verification directory. No bed, art or gameplay production source changed in this window. Actual keeper acquisition and save acceptance remain pending the root-owned native replay.


## Real cooldown recovery at a blocked keeper approach

Actual retry `HigherTiers/NativeAcquisition/f398e421d1e640ba8b81b1353ae963ec` again retained9 required successes and the live44/44HP actor. The new observations prove that keyboard Rime hit the actual keeper at42,11 (Cold0.475); two active Bandfrogs at43,10 and43,13 then excluded its adjacent approach cells under the unchanged two-cell threat margin. The player at38,11 had Calm5 andRime34 ticks remaining. No currently ready control ray or permitted reach path existed, so the audit refused. Full original failure/log/exact restoration remain preserved; this is not a death or a failed Rime cast.

The audit now permits at most12 actual native wait/retreat actions across the run when a real starting control is cooling and both approach and ready-ray routes fail. It waits only at a cell meeting the existing predicate, otherwise chooses one retreat step satisfying the same predicate; no risk margin is relaxed. Each key must produce its true stationary/destination result, exactly one scheduler action and a lower actual cooldown. It then recomputes owners, threats and routes. This is ordinary time passing with active NPCs, not direct cooldown advancement or a safety guarantee; death, no permitted recovery cell and the finite bound remain honest failures.

Independent review found no concrete blocker; current runtime/editor reference compiles have zero errors. Only the native player changed after exact prior-source validation. Patch, review, compiler logs and updated source hash are preserved. Keeper acquisition, equip and save completion still require the next19-check native run.


## Necessary adjacent combat at the keeper approach

Actual run `8ee69ab842794f1caa7374c6449f7dba` retained9 required successes and44/44HP at38,11. The keeper remained frozen at42,11; one Bandfrog at40,11 was calmed, while another at39,11 was active and adjacent. Calm19/Rime30 were cooling. The two-cell threat exclusion left no permitted wait or retreat, so the audit stopped honestly. This was a conservative acquisition-script refusal, not a game defect. The original report/log/restoration remain preserved.

The native player now permits bounded actual melee against an attacking adjacent route blocker, using current exact identity, visibility, scheduler membership, living brain and reciprocal-hostility guards. The keeper destination is excluded until its required inspection. It sends a real movement key and requires a fresh marked diagnostic window containing the exact player-to-enemy HitRoll; misses remain valid attack attempts, not guaranteed damage. Ordinary retaliation, injury and death remain active. At most40 route attacks, the existing two-tonic budget, fixed seed and unchanged physical/safe-path rules bound the audit. Nested failure disposal preserves the diagnostic window even if death interrupts the key. No actor, source, reward, stat, AI or gameplay rule changed.

Independent read-only review and current runtime/editor reference compilation passed. Only the reviewed player was published after exact prior-source validation. Patch, review and hashes are in the Legendary verification directory. The subsequent actual native replay `HigherTiers/NativeAcquisition/2863b2680c074bc9bd7aece4fddc6381/report.json` completed19/19 required checks with0 failures and0 unexpected errors: real key/unlock/modified-find acquisition and equipment, actual guardian and keeper defeat, recovery/equipment/inspection of the same legendary item, and F5/real mutation/F6 replacement plus depleted-source persistence. It used ordinary starting stats and preserved naturally earned equipment/advancement, with real starter control spells permitted. Two labelled source-stage starts shorten travel; bounded fixed-seed source selection does not establish natural discovery, source frequency, typical combat difficulty, or a broad legendary-encounter system. This receipt does not itself establish that every screenshot has been visually reviewed.
