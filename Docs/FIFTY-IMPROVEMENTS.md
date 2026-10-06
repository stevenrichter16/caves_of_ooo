# Fifty improvements — October 2026

Status: **50 of 50 improvements implemented and verified**, with individual plans and designs below. Final native Unity EditMode run: **831/831 passed**. Final Play-mode run: **60/60 checks passed**, with six reviewed screenshots. Baseline main `bfa625f44`; completed 2026-10-06.

## Delivery rules

Each numbered iteration is a distinct player-facing outcome, with its own plan, design, acceptance and counter-check. A test, metadata file, review correction or documentation edit does not count as another iteration. Related iterations share infrastructure and may ship in coherent milestone commits. No numerical Qud parity claim; preserve sparse discovery, original creatures, and existing save/content contracts. BitLocker remains unavailable in ordinary play.

## Workstreams

- 01–15: combat, equipment and consumable decisions — `FIFTY-IMPROVEMENTS-COMBAT.md`.
- 16–30: readable player decisions — `FIFTY-IMPROVEMENTS-CLARITY.md`.
- 31–45: exploration, cultivation and resource interactions — `FIFTY-IMPROVEMENTS-WORLD.md`.
- 46–50: differentiated cooked expedition meals — `FIFTY-IMPROVEMENTS-MEALS.md`.

## Implementation procedure

Read each cited source before writing production code. Record RED for the new invariant, implement with counters, run affected native Unity tests, perform independent review, and record exact receipts. Root alone operates the editor and commits; contributors have disjoint files. New C# files receive unique Unity metadata. Blueprint edits are surgical with parsed before/after comparisons. Existing logs and unrelated untracked assets are excluded.

Native Play evidence must identify controlled fixtures separately from natural discovery, and measurable behavior separately from balance/feel. Fetch/rebase before pushing each completed milestone to main. If a low-value fix stalls, record and substitute another concrete improvement; never mark unfinished work completed to reach the count.

## Progress

All 50 outcomes are complete. Tests, review fixes, documentation and metadata are supporting work and do not inflate the iteration count. The three delivery milestones group combat/meals, world interactions, and readable decisions.

## Numbered delivery ledger

| Iteration | Player outcome | Individual plan/design | State |
|---|---|---|---|
| 01 | Disengage refuses a completely blocked escape without payment | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 02 | Rooting prevents self-propelled mobility without resisting enemy shoves | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 03 | Thrown tonic splashes follow complete physical bodies | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 04 | Poison and bleeding kills retain their actual inflictor | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 05 | Stronger bleeding is chosen by damage potency, not text order | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 06 | Hooks release when their wielder can no longer hold them | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 07 | Hammer-damaged ordinary gear suffers a bounded, repairable penalty | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 08 | Repair damaged carried gear with finite physical materials | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 09 | Two-handed axes gain a modest dismembering role | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 10 | Speed and Strength tonics become finite tactical preparations | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 11 | Antidote also treats lingering gas poisoning | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 12 | Burn Salve actually cools a burning patient | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 13 | Surviving a complete ordinary burn leaves usable charred aftermath | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 14 | Elemental flasks can affect physical scenery | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 15 | Toxic brew potency creates a real, bounded difference | [Design](FIFTY-IMPROVEMENTS-COMBAT.md) | Complete; verified |
| 16 | Equipment contribution deltas | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 17 | Find an item in a large inventory | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 18 | Inspect loot before taking it | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 19 | Explain partial Take All results | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 20 | Container destination fit | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 21 | Understand a trade's immediate consequences | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 22 | Actor-relative item handling card | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 23 | Current hauling cue | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 24 | Full player status reader | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 25 | Visible surface properties reader | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 26 | Full crafting outcome reader | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 27 | Target-specific modification preview | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 28 | Disassembly yield before destruction | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 29 | Food and prepared-meal decision card | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 30 | Controls as a readable reference | [Design](FIFTY-IMPROVEMENTS-CLARITY.md) | Complete; verified |
| 31 | Trustworthy drinking from nearby wells | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 32 | Campfire rest reports and pays only a real safe rest | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 33 | Glean field grain without losing or duplicating the row | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 34 | Plant a carried seed into an adjacent prepared bed | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 35 | Clear an unwanted young crop deliberately | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 36 | Gather ripe crop produce into the pack, keeping overflow real | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 37 | Transfer clean water between carried vessels | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 38 | Pour one measured unit instead of the whole flask | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 39 | Douse a burning world object with carried clean water | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 40 | Dry wet gear beside genuine heat | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 41 | Feed and relight finite cooking coals at an actual material cost | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 42 | Pass fire from an equipped torch to another torch | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 43 | Reclaim already-cut field stubble as a prepared growing bed | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 44 | Poured finite clean water can irrigate a real planted bed | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 45 | Frozen water must thaw before it can be drawn | [Design](FIFTY-IMPROVEMENTS-WORLD.md) | Complete; verified |
| 46 | Heat-protection grain meal | [Design](FIFTY-IMPROVEMENTS-MEALS.md) | Complete; verified |
| 47 | Cold-protection hearthbulb meal | [Design](FIFTY-IMPROVEMENTS-MEALS.md) | Complete; verified |
| 48 | Acid-protection mushroom meal | [Design](FIFTY-IMPROVEMENTS-MEALS.md) | Complete; verified |
| 49 | Endurance meat meal | [Design](FIFTY-IMPROVEMENTS-MEALS.md) | Complete; verified |
| 50 | Evasion fruit meal | [Design](FIFTY-IMPROVEMENTS-MEALS.md) | Complete; verified |

## Historical first native baseline

`Verification/FiftyImprovements/all-red.xml`:232 cases, 112 passing controls/existing behavior, 120 failures. Clarity 39 fail; combat 37 fail/33 pass; world 40 fail/43 pass; meals 36 pass/4 new stack failures. An earlier run used invalid bare-number dice fixtures in two combat suites and the MCP summary capped failures; it is retained as an incomplete first receipt, not the authoritative baseline. The corrected full XML above is authoritative.

## Historical first combined implementation and review run

`Verification/FiftyImprovements/first-implementation.xml`: 298 cases, 246 passed, 52 failed. The current Unity automatic XML export was copied after verifying its case count and timestamps; the separate callback export did not survive this run. This is an intermediate receipt, not acceptance.

The run reproduced the intentionally staged repair-rollback and liquid-description threshold defects. Most world and two real input failures exposed one shared API mistake: `IsFootprintCurrent` describes explicitly registered multi-cell bodies, so it must not reject ordinary single-cell entities. Two kill-credit cases lacked the required Player tag; the meal scenario fixture lacked the required caller clock. These fixture corrections do not change gameplay expectations. Remaining world counterexamples are being isolated after the shared gate correction, before claiming their hypotheses confirmed.

## Historical focused acceptance

`Verification/FiftyImprovements/focused-green.xml`:342/342 native Unity EditMode cases passed (300 new program cases plus 42 existing bed contracts). All 50 individual implementations satisfy their focused tests. The 881 affected-class selection plus the new fixtures expands to 891 filters / 15,889 discovered tests; broader acceptance and native keyboard/screenshots remain pending. A test-diagnostic assembly reference briefly blocked import; the accidentally started stale-assembly run was excluded, the unsupported dependency was removed, and the successful 342-case run followed a zero-error native compile.

## Broad regression and correction history

`Verification/FiftyImprovements/broad-first.xml` completed 15,889 native Unity EditMode cases: 15,848 passed and 41 failed. The complete XML, rather than the tool’s 25-failure preview, drives the correction pass. This is not a clean acceptance receipt. Failures include shared null/source guards, rest API compatibility, old fixtures lacking a real physical actor or clock isolation, the deliberate Antidote gas-poison behavior change, and a pre-existing canonical-document source assumption. Crop discovery, harvest capacity and additional placement/visual fixtures are being checked individually before final acceptance.

## Final acceptance and limitations

- [Final native Unity EditMode XML](Verification/FiftyImprovements/final-green.xml): **831 passed, zero failed or skipped**. This includes all 303 dedicated program cases (95 combat, 55 clarity, 112 world, 41 meals), the 19 previously failing fixture classes and relevant existing bed/document controls. The extra gas-only antidote counter is in the existing poison fixture.
- [Final Play-mode report](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/report.json): **13 native keyboard/UI checks**, plus **20 meal, 12 combat and 15 world command/scheduler/save checks**, all passed with zero unexpected errors. Unity returned to SampleScene in EditMode. The earlier Play report remains as historical evidence before the final rest correction.
- Six final captures cover normal/Pause controls, current effects, prepared food, partial pickup and the whole-stack trade quote. Root and an independent reviewer inspected the first six; root re-inspected the three changed final captures and verified the other three were byte-identical. No clipping prevents reading these panels; a repeated carry-refusal sentence is a minor presentation follow-up. The long-name confirmation-price fix has a separate actual-painted-row test.
- [Parsed content proof](Verification/FiftyImprovements/final-content-diff.json) confirms only 14 intended Objects.json blueprints changed: five cooked meals, two finite-duration tonics, seven repairable ordinary equipment items. The 30,000-line file was not reserialized. Every new C# file has unique Unity metadata.

**Can verify:** actual action results and refusal costs, bounded resource use, effect replacement/expiry, owned target selection, return navigation, rollback, save replacement, native UI input and readability of the captured panels.

**Cannot verify:** campaign balance, novice comprehension or ordinary discovery from controlled fixtures. The separate meal/combat/world witnesses deliberately construct finite fixtures and directly exercise commands; they are not keyboard-driven wilderness expeditions. Existing saved blueprint parts retain their stored definitions; newly generated/cooked objects receive the authored data additions.

The large first run completed 15,889 cases with 41 failures. All previously failing fixture classes passed the final 831-case correction run, but the full 15,889-case selection was not repeated. One unchanged crop-site discovery test failed only in the large run and passed the subsequent runs; order/global-state sensitivity is plausible but its exact cause was not established. No speculative generation change was made, and this remains a bounded test-order follow-up rather than a claim of proven baseline behavior.

## Final review

- 🟡 Fixed transaction rollback leaks, stale/null ownership checks, single-cell footprint admission, forged equipped-torch ownership, rest method compatibility, and long-name price clipping; RED receipts precede their corrections.
- 🟡 The final cold review reproduced deferred rest resurrecting a terminal actor in two cases. Committed time remains paid, benefits are withheld and `RestInterrupted` replaces the success receipt. [RED](Verification/FiftyImprovements/rest-lifecycle-red.xml) and final GREEN cover both zero HP and explicit death handling.
- 🔵 Independent meal review checked stat apply/remove symmetry, saved exact contributions and incompatible food stack payloads. Other reviews checked live-versus-deferred rest, physical source compatibility, actual body equipment binding and reader ownership.
- 🧪 Long-term tuning, the isolated broad-run crop-site failure and minor repeated refusal prose remain recorded follow-ups. None is counted as a completed improvement.

The delivery manifest records the exact owned code/content/test files and their final hashes. Raw Unity XML is retained without rewriting native stack-trace whitespace; authored source and documentation pass whitespace checks.
