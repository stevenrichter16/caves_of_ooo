# Fifty further improvements — October 2026

Status: **complete — all fifty implemented, reviewed and accepted in native Unity.** Baseline `2885ad5c6`, main, 2026-10-06. This program follows the completed first fifty in `FIFTY-IMPROVEMENTS.md`.

## Goal

Deliver fifty further distinct player outcomes that deepen ordinary exploration, preparation and tactical play. Each outcome requires an individual design, a real player route, acceptance and counterchecks. Supporting tests, review repairs and documentation do not count as improvements. Preserve the persistent RPG, original creatures, sparse environmental discovery and normal-play exclusion of BitLocker. No numerical Qud parity claim.

## Prompts being executed

**Brainstorm:** Read the current living documents and actual implementation. Identify disconnected mechanics whose connection changes a player's expedition or fight. Compare against the first fifty and prune duplicates. Prefer usable actions, distinctive authored choices and world sources over inert catalog entries. Separate authoritative lore from proposals and historical roadmaps.

**Design:** Select fifty concrete outcomes in coherent streams. For each, state the current gap, the intended player choice, source and action, costs and limits, save behavior, acceptance and an opposing-case test. Read every referenced API before production work and record corrected premises. Share existing systems rather than invent parallel frameworks.

**Implement:** Write and observe failing tests before production, then implement the design with counterchecks, adversarial cases, useful diagnostic records and finite resource ownership. Review independently, correct significant findings, run affected native Unity tests and controlled Play witnesses, inspect relevant visuals, update these documents in the same commits, fetch/rebase and push main. Record exactly what each witness proves and what remains a playtest question. Preserve unrelated files and user saves.

## Verification sweep

| Premise | Verified result / correction |
|---|---|
| Another fifty should repeat unfinished first-batch work | Incorrect: all fifty previous outcomes are already on main. The new program must have separate outcomes. |
| `CONTENT-ROADMAP.md` is a current inventory | It contains historical Snapjaw entries and old unimplemented-status claims. Use actual code and newer feature documents for scope. |
| Lore v2 is canonical replacement | Incorrect: `Docs/Lore/v2/README.md` calls it a parallel redesign, and `Docs/BIOME-LORE-FIRST.md` identifies both `Docs/Lore` corpora as superseded Palimpsest-world material. Canon is root `Lore/`, with `Lore/10_Bible.md` authoritative. Use shipped regional content and that corpus. |
| Native editor is unavailable | Unity 6000.3.4f1 is connected in EditMode in `Assets/Scenes/Main/SampleScene.unity`. Root alone operates it. |
| All test output is a full receipt | MCP failure previews are capped. Save Unity's complete current `TestResults.xml` and verify timestamps/counts. |

## Delivery discipline

Root owns shared blueprint edits and Unity integration. Contributors own separate streams. Every new Assets C# file receives a fresh Unity metadata GUID. `Objects.json` changes remain surgical and receive parsed before/after proof. Existing logs, unrelated artwork and untracked files are excluded from commits.

Plans and numbered outcomes follow the source audits; no outcome is marked complete before implementation and verification. Existing saves retain their serialized definitions unless a narrowly justified migration is explicitly designed. Controlled fixtures do not establish natural discovery, long-session balance or novice comprehension.

## Selected workstreams

- **01–15, tactical interactions:** `FIFTY-IMPROVEMENTS-II-COMBAT.md`. Terrain-aware cones; drying shallow water; crosswise ice walls and meltwater; a corrosion setup/payoff pair; deliberate long-blade guard and follow-through; collision damage; vault landing advantage; caster spacing and line clearing; enemy weapon recovery and medicine; an original enemy that uses a preparation/payoff sequence.
- **16–30, preparation and specialization:** `FIFTY-IMPROVEMENTS-II-PREPARATION.md`. Recover forged components; shape common stock; stronger haft and guarded binding; respiratory, acid and cold clothing tradeoffs; torch refilling; cord bandages; concentrated medicine and detoxification; spent-brew compost; seed-focused harvesting and young-crop relocation; physical mineral infusions.
- **31–50, regional activity:** `FIFTY-IMPROVEMENTS-II-EXPLORATION.md`. Exact-book copying; local repair labor; selected rental return and buyout; guest storage; local locksmith; asking for passage; medicine and useful equipment for willing neighbors; finite burial; portable descent light; installed rope shortcut; trap and cloth salvage; haulable Sodden supplies; a local territorial bargain; opening a grazer pen; collector barter; book loans; a finite locked store with independent access routes.

These streams are implemented. Source-sweep substitutions and their reasons are recorded below; acceptance remains separate from implementation.

## Numbered plan ledger

Each linked stream document supplies the gap, design, source, cost, persistence, acceptance and countercase for its numbered rows. All fifty rows are implemented and accepted. The integrated native regression and Play receipts below are the final evidence.

| # | Intended player outcome | Stream |
|---|---|---|
| 01 | Cone spells affect terrain across their actual fan | Combat |
| 02 | Dry shallow water to alter a local elemental route | Combat |
| 03 | Raise a crosswise barrier of ice in a chosen direction | Combat |
| 04 | Use the finite meltwater left by an expired ice wall | Combat |
| 05 | Prepare a corrosive ground lane | Combat |
| 06 | Cash in corrosion with a bounded focused attack | Combat |
| 07 | Deliberately guard with a long blade | Combat |
| 08 | Follow through into a positional long-blade attack | Combat |
| 09 | Slam a target into another physical obstacle with consequences | Combat |
| 10 | Gain a bounded advantage from a successful vault landing | Combat |
| 11 | Fight ranged enemies that preserve firing distance | Combat |
| 12 | Fight ranged enemies that move around allied blockers | Combat |
| 13 | Deny a disarmed enemy its recoverable dropped weapon | Combat |
| 14 | Interrupt enemies trying to use finite appropriate medicine | Combat |
| 15 | Encounter an original enemy with an elemental setup and payoff | Combat |
| 16 | Recover components from an existing forged weapon | Preparation |
| 17 | Shape common physical stock into basic forging supplies | Preparation |
| 18 | Choose a high-Strength haft with an accuracy cost | Preparation |
| 19 | Choose an accurate binding with a penetration cost | Preparation |
| 20 | Trade a head slot for respiratory protection | Preparation |
| 21 | Trade physical protection/mobility for acid protection | Preparation |
| 22 | Trade back-slot evasion for cold protection | Preparation |
| 23 | Refill an unlit torch with finite oil | Preparation |
| 24 | Make a bleeding-only field bandage from cord | Preparation |
| 25 | Concentrate healing plants for a stronger reagent | Preparation |
| 26 | Detoxify a dangerous grove harvest at a still | Preparation |
| 27 | Turn spent brew sludge into finite compost | Preparation |
| 28 | Choose seeds instead of a normal crop harvest | Preparation |
| 29 | Relocate a young crop without resetting its history | Preparation |
| 30 | Infuse equipment using finite physical minerals | Preparation |
| 31 | Select the exact grimoire a scribe copies | Exploration |
| 32 | Pay a repairer for labor on selected portable equipment | Exploration |
| 33 | Return one selected rental | Exploration |
| 34 | Buy out one selected rental | Exploration |
| 35 | Store belongings under a local guest-right agreement | Exploration |
| 36 | Hire a locksmith for an actual nearby locked container | Exploration |
| 37 | Ask a friendly blocking person to step aside | Exploration |
| 38 | Apply finite medicine to a willing nearby person | Exploration |
| 39 | Give selected spare equipment to a willing person who actually equips it | Exploration |
| 40 | Deposit a corpse voluntarily with persistent finite aftermath | Exploration |
| 41 | Take a real descent lamp, leaving its original place darker | Exploration |
| 42 | Install rope to open a reciprocal local shortcut | Exploration |
| 43 | Recover material from an already disabled trap | Exploration |
| 44 | Strip abandoned cloth into cord at the cost of its cover | Exploration |
| 45 | Choose hauling a sealed load versus unpacking it on site | Exploration |
| 46 | Negotiate one local territorial passage | Exploration |
| 47 | Release a penned grazer through a real gate | Exploration |
| 48 | Exchange food for a collector's exact carried salvage | Exploration |
| 49 | Borrow an actual finite book from a loan shelf | Exploration |
| 50 | Open a finite abandoned store by key, paid help or force | Exploration |

## In-phase review and evidence

Initial working tree contains unrelated MCP logs and two AllotmentNotice screenshots; preserve them throughout.

Native visual/source RED observed before production: `Verification/FiftyImprovementsII/visual-red.xml`, 21/21 failing as intended. Eleven missing items, three missing worn-item flows, the missing enemy and six currently rejected assembly families establish the required source and presentation gaps. This is baseline evidence, not acceptance. Existing imported models are reused through explicit aliases; no new geometry is claimed for those aliases.

Pre-implementation substitution: #39's pet interaction overlapped existing pet behavior and the user's instruction to deprioritize dog-fetch work. It is replaced by a finite equipment gift with actual equipment effects. #50 uses the new paid locksmith service rather than introducing a separate lockpicking subsystem. #29 preserves land claims by declining reserve-bed transplants rather than inventing transferable claims.


### Native baseline receipts, second batch

- `combat-preparation-red.xml`: 169 cases, 56 passed / 113 failed. Combat 73 (14/59); preparation 74 (36/38); then-current visual 22 (6/16). Existing guard counters passing before implementation are retained.
- `exploration-visual-red.xml`: 85 cases, 10 passed / 75 failed. Exploration 52 (4/48); expanded visual 33 (6/27). These were observed before gameplay production in the corresponding stream.
- One intervening test request used a stale assembly while a new gate test had a read-only property compile error; that run is discarded. The corrected 85-case run above is the authoritative receipt.
- Preparation Objects data: eleven new blueprints and ten explicit existing Part additions. Combat data: three new blueprints and four existing blueprint changes. Each surgical edit was parsed before/after; the final combined `content-diff.json` proves 24 additions, 14 changed existing owners and 671 unchanged owners (685 → 709 unique definitions), with no removals.
- Model aliases preserve the real equipment/body/container/door owner. Existing geometry is reused. The pen gate reads its actual saved open/closed state; hiding or removing an owner must remove its visible form.
- UI timing RED, complete gameplay GREEN, adversarial review, measured Play evidence and final source/commit review are pending. No iteration is claimed accepted merely because its code or blueprint is present.


### Integration and review corrections

- `integration-first.xml`: 165 cases, 139 passed / 26 failed. It includes the observed ordinary menu payment RED (10 failing UI cases), initial model integration and source checks.
- `integration-second.xml`: 263 cases, 243 passed / 20 failed. Both new UI suites pass all 35 action/counter cases. New no-benefit compost and range-two locksmith counters reproduced their defects.
- `integration-third.xml`: 187 cases, 186 passed / 1 failed. Combat 82, natural-weapon regression 26 and visuals 34 all pass. The mineral callback rollback counter reproduces removal of an unrelated independently added enhancement.
- `integration-fourth.xml`: 183 cases, 180 passed / 3 failed. Combat replacement-save cases both pass (84 distinct combat cases now green across receipts); all 45 preparation adversarial cases pass after the narrow mineral rollback fix. Remaining failures were two Sodden fixtures bypassing the actual post-acceptance installer and a gate fixture reading raw Physics.Solid instead of native Door blocking.
- `review-red.xml`: 55 cases, 47 passed / 8 failed. All six preparation source cases now pass real acquisition, revisit and replacement save. All real regional source cases pass except the deliberately added disabled-manifest counter. Five service-review cases reproduce blocked-action bypasses, malformed copy creation and a deferred medicine mutation. The new real Tally grave-marker rendering test reproduces missing geometry; reciprocal-rope save count is under investigation.

**Root presentation review:** 🟡 Native integration exposed missing portable-palette registration outside the Spread, which could force regional presentation back to the fallback. Register the shared palette once per native bind and permit the exact new wearables/held jar on existing compatible body rigs. Native floor, real Body equip, removal and visibility counters pass. 🟡 The newly sourced Tally burial ground lacked a model; an actual cold-generated owner test was observed RED before assigning the existing carved plaque mesh as its physical marker. No new mesh or animation clip is claimed: original forms and the Marlback rig's existing clips are reused explicitly.

**Root command review:** 🟡 New paid physical commands were reachable but absent from the two presentation turn-payment routes. Observed RED preceded the narrow command-family inclusion; 35 native success/refusal checks now verify a successful command pays exactly one action and refused/unknown commands pay none.

**Pending final gates:** current-source integrated regression, all three detached runtime benches invoked during native Play, native key menu choices and screenshot inspection, source/meta/data validation, final cold review, commit and push. Until those gates pass this document does not claim completion of the second fifty.


### Native menu inspection and further review

Initial controlled Play `Native/e57bcc38312f455880a418c6796369c8/report.json` completed all four native-key physical actions with exact costs and ten-tick action advancement; combat 15/15 and preparation 18/18 detached checks passed without altering the campaign snapshot. Exploration reached 20/21: its collector fixture assumed Magpie already had its optional configured role, producing a fixture null reference. This receipt is not final Play acceptance. All six screenshots were inspected. Long new action labels clipped costs/consequences; fixed labels are shortened and variable paid services put prices first. This is text polish within the existing menus, not a new UI system or extra counted outcome.

`regional-mesh-red.xml` records 85 cases, 83 passed / 2 failed. All 29 exploration adversarial and 19 source cases pass after the service/manifest corrections, as does the actual Tally marker. Two additional actual regional equipment cases reproduced missing voxel registration in the Sodden and Last Counter. Shared portable/fitted mesh registration now follows the same cross-region scope as its palette, once per bind. `travel-retention-red.xml` verifies both regional cases now pass.

The five-case `travel-retention-red.xml` also observed two independent world defects before fixes: an unloadable ambient vault received a finite salvage reward that could regenerate, and an installed rope arrived at a competing older staircase. The existing generated Spread yard passed real jam → one salvage → repeat refusal → unload → replacement-save → unload controls. Salvage admission is now limited to fixed retained destinations or exact accepted Spread graphs. Rope installation records both actual native stair tags and Parts in the existing reversible receipt; no generic travel rewrite.


### Completed native Play witness

Final receipt: `Verification/FiftyImprovementsII/Native/f73a91c469ca4d4fac171f4321e94149/report.json`, **complete=true, 0 failures, 0 unexpected errors**. Eleven driver checks pass: ordinary Classic start, four exact native-key menu actions, three complete benches and three unchanged-campaign snapshots. The bench totals are **combat 15/15, preparation 18/18, exploration 21/21**. These 54 observations are verification, not additional counted features.

All six final PNGs were opened and inspected. Recipe material/cap, scribe price and the selected original, guest-right requirement, and cloth's resource/cover consequence are readable. The controlled scene visibly contains the existing borrowed forms and fitted hood; no new geometry, biome composition overhaul or novel animation is claimed. A long unrelated watering-grimoire title still clips after its visible cost, using the existing menu convention. The current coarse whole-zone framing is not a close-up art-quality comparison with the user's earlier reference.

**Can verify (script-observable):** ordinary paid menu dispatch, exact quantities/currency, ten-tick player action timing, real floor/equipment ownership, finite source generation and replacement-save persistence, and the explicit controlled combat/preparation/exploration contracts. **Cannot verify:** novice discovery, long-campaign economy or combat balance, subjective fun, a complete campaign playthrough, or every biome's visual feel. Source placement tests establish the named destinations separately from the controlled Play supplies.

**Where the content lives:** current skill trees supply the new tactical abilities; authored depth tables supply the stormbinder. Cinderhold supplies the haft and paid repairs, Last Counter the binding and cold cloak, and the Sodden works its two protective garments. Quillhold has book copying/loans, Wellmeet guest storage, Tally selected rentals and burial, Olderdeep a recoverable descent light, Ginmere reciprocal rope landings, and retained Spread sites finite grazer/collector/trap decisions. The new regional packets require a **new v15 campaign**; existing saved graphs retain their prior owners and are never silently restocked. Old saves remain readable.


### Broad affected-system regression and cold review

`affected-regression-first.xml` ran the explicit 116-fixture selection: **2,572 passed, 2 failed, 1 intentionally skipped** (2,575 total). The skip is the existing explicit one-time whole-Spread seed-64 census, not a skipped new acceptance case. The two failures were corrected with their invariants retained:

1. The actual Tally marker test used an equipment-only fixture when cold generation needs consistent generation factory statics and seeded content RNG. It now scopes the same `HaulingContentScope` used by the successful source tests, then verifies the real generated owner, actual geometry and hidden/removed controls. In particular a stale `ContainerPlacementService.Factory` can otherwise mix foreign owner IDs into this test's generation. No production generation gate was weakened.
2. The existing exact loadout census pinned 31 kits although baseline already contained 34; the new stormbinder makes 35. `equipment-roster-proof.json` records the baseline comparison. Four literal kits are added to the same census, retaining exact set equality, unselected-creature counters and the ordinary ownership tests for every kit.

The same affected selection was repeated, including its original ordering, and passed. No production behavior changed after the accepted final Play receipt.

**Cold review Q1–Q4:** equipment apply/remove and exact floor ownership are symmetric; rope connections, tags and Parts undo together; prepared/transferred resources retain exact ownership and outer rollback; action-family classifiers charge only successful ordinary commands; saved optional site admission checks Enabled and literal v14 remains readable. Independent reviews found and corrected the service, mineral rollback, regional voxel, retention and arrival issues documented above. Normal-play BitLocker remains excluded. No Qud source-equivalence claim is made: these are original interactions using the project's existing contracts. Significant findings are fixed; remaining limits are the explicitly disclosed long-session balance, novice discovery and broad visual-feel playtests.


## Final acceptance — 2026-10-06

**50/50 outcomes complete.** `Verification/FiftyImprovementsII/affected-regression-final.xml` runs the full named selection of 116 fixtures: **2,578 passed, 0 failed, 1 existing explicit census skipped** (2,579 total). Every requested fixture matched; all **322 cases in the new focused fixtures pass** together. The earlier first-run failures and exact corrections remain preserved as evidence rather than overwritten. This is an affected-system regression, not a claim that every test in the repository was run.

Native Play: **54/54 controlled gameplay observations, 11/11 driver checks, 0 unexpected errors**, plus six inspected final screenshots. The can/cannot-verify bounds above remain applicable. `acceptance-summary.json` provides the compact machine-readable result.

Final data checks: 709 unique blueprints, exactly 24 additions and 14 intended changes, zero removals and 671 untouched existing definitions; changed JSON parses, all new C# metadata exists, and all 48 newly owned metadata GUIDs are unique across Assets. Authored source/doc whitespace checks pass; native XML preserves the tool’s verbatim stack-trace whitespace. The same commit includes the three detailed designs, review findings, current outcome ledger, source/ownership list and native receipts. Unrelated MCP logs and earlier AllotmentNotice captures are excluded from the commit.

The program ships together because the shared current-world manifest, blueprints, ordinary action-payment integration and presentation aliases must agree at the public main-branch boundary. Tests, review corrections and text polish are not extra numbered improvements. No outstanding significant issue was identified in the completed bounded reviews; subjective balance and long-session play remain future playtest work.
