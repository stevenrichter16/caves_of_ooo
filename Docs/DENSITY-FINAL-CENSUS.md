# C12 — recorded-cohort census and truthful access measurement

**Status:** published measurement repair, private and published-source68/68 GREEN with exact150-row replay; native acceptance and natural campaign/balance remain separate. Historical C0/C1 receipts remain immutable. C10/C11 full baseline-file differential is clean at its separately recorded checkpoint.

## Verified corrections

| Earlier measurement | Actual contract | Required repair |
|---|---|---|
| ContainerPart.Locked represents every lock | Key-based LockedChest uses LockPart; C11 now exposes the combined live ContainerPart.IsLocked authority | Count either lock as locked and skip the opening attempt. |
| Handled OpenContainer means successfully opened | Refusal and intercepted actions are also handled | Observe the actor's actual OpenContainer event for that exact container; do not infer success from Handled. |
| Candidate.Objects represents physical occupancy | Secondary footprint cells appear in Occupants | Check full occupancy before claiming a legal adjacent measurement position. |
| Same seed and first four non-POI cells is the same cohort | POI/authored-site changes can alter selection | Replay the150 recorded C0 zone rows by seed and exact zone ID, preserving their order and baseline labels; record current classification separately. |
| Old contents/value totals prove acquisition | Contents are counted even behind locks or unavailable approaches | Separate generated contents/value from actual observed opening, locked skips, blocked approaches and refused attempts. |

## Bounded repair and contract

Keep the five historical seeds,120 wilderness rows,15 lairs,15 depth1/3/5 rows, one designed starter grant and the actual generated Morrowfast shops. The default selection input is the preserved `Loot/census-before.json.gz`; an explicit test-run override may point to a reviewable recorded-cohort file. No silent rescan fallback for missing/malformed/duplicate/unsupported selection data. Parse and validate before measuring; legacy source labels remain available for deltas, while current biome/POI/authored-column metadata makes classification changes visible.

Opening measurement counts: `containers` = observed container entities; `locked` = either lock authority refused access before a dispatch; `noApproach` = no legal adjacent physical cell; `openingAttempts` = actual dispatched actions; `opened` = that actor received an OpenContainer event naming that exact container; `refused` = a dispatched attempt without the matching event. The temporary observer must be removed and the census actor detached even on an exception. This is core observation using legally placed measuring actors, not native travel or proof of winning the intervening encounters.

Before implementation, matched tests cover unlocked/legacy/key/both locks, handled refusal versus actual open, an event naming another container, a blocked physical approach and preserved actor/item state. Recorded-cohort tests cover unchanged exact IDs/order, copied metadata, seed filtering, duplicate/invalid IDs, unsupported source types and absent data. A full repaired run must match all150 historical zone keys; any current source-classification change is reported, not replaced by a different cell.

## Integration and honesty boundary

Keep raw historical values and the corrected definition side by side. The old447 opened figure is handled attempts under the old lock behavior. Generated item quantities/value are still useful, but should not be relabeled as acquired loot. C11's separate60-zone depth8/9/12 census is an explicit expansion, not part of the old150-zone denominator. C13 species aliases are display-only normalization; retain raw before/after identities.

After private RED, repair only the census test/helper and add focused adversarial cases. Publish after coordination with the native window. Then collect a new integrated receipt with identical source membership and report source/category distributions, quiet zones, commodity values and measured access outcomes. This still cannot prove native rendering/input, Unity-identical seeds, difficulty, preparation, pacing or feel; those need root's ordinary-stat campaign/native acceptance.

## Review gates

- 🟡 Avoid silent sample drift and success inferred from a handled refusal.
- 🟡 Never mutate the archived baseline report or replace a disappeared source row.
- 🔵 Keep generated contents, core access observations, factory kit trials and ordinary-player acquisition distinct.
- ⚪ This measurement repair does not declare all C12 campaign/balance work complete.

## Results and evidence

The first26 matched cases reproduced22 failures (including the missing cohort API) and retained4 controls. After repair all26 passed. The dedicated adversarial sweep and independent `combat_density` review then proved three additional defects: a closed archive barrier lacking a Physics.Solid flag was accepted as an approach; an unexpected extra seed was silently omitted; and offset container bodies were approached by their empty canonical anchor. The corrected helper uses `Cell.BlocksMovement`, rejects seeds outside the recorded five-seed contract, and searches the actual occupied body. A final positive counter-check observes an exact opening event even when the outer action is unhandled, proving that the success count is independent of Handled in both directions.

Final private result: **68/68 passed** (27 audit,25 dedicated adversarial,16 original census/control),4.302669 seconds. Raw RED/GREEN, preserved old helper, final data, code hashes and summary are in `Verification/DensityCompletion/FinalCensus/`. The source-only publication is six paths: the census helper, two new fixtures with their metas, and one neutral runtime-boundary sentence in the T4 generated-source fixture. No game behavior changes are part of C12.

The150 recorded zone keys, order, source labels and baseline biomes match exactly; none changed current classification. There are161 total source rows including10 actual Morrowfast shops and one designed starter grant. The corrected observation is **445 containers,39 locked skips,406 attempted opens,406 exact actor opening events,0 refused attempts and0 blocked approaches**. No keys were acquired in this census; the39 locks remain closed. Historical447 is retained as the old handled-attempt count and cannot be compared directly with the corrected406 success count.

| Recorded source | Containers before → current | Generated armor units | Generated commerce | Neutral sale quote |
|---|---:|---:|---:|---:|
|120 wilderness rows |330 →331 |13 →36 |9172 →10823 |2984 →3565 |
|15 lair rows |66 →66 |2 →9 |1061 →1357 |346 →448 |
|15 depth1/3/5 rows |51 →48 |4 →13 |1473 →1658 |479 →542 |
|10 shops |not container rows |5 →5 |1710 →1750 |575 →585 |
|1 starter grant |not container row |0 →0 |54 →54 |17 →17 |

These are generated quantities and quoted values, including inaccessible stock. The current integrated content includes the other completed milestones, so these are not isolated C1 armor effects. Twenty of120 wilderness rows have no Creature-tagged entity in both snapshots. That is a narrow empty-creature count, not a hostile-free, safe-route, combat pacing or difficulty measurement. Historical raw Snapjaw-era species identities and current raw Marlback identities remain in their respective receipts; this table does not normalize or overwrite them.

## Cold-eye review and remaining boundaries

- Q1: actual event delivery is observed independently from outer handled status; lock and refused paths preserve generated stock. Every temporary observer and measuring actor is removed in `finally`, including a throwing target handler.
- Q2: key and legacy locks share `IsLocked`; movement uses the shared physical blocker contract, plus explicit traps/stairs/creatures and the target body. Contents/value and access counters use separate fields.
- Q3: key/legacy/both/neither, actual/refused/wrong-target/unhandled-success, occupied secondary body/clear cell, closed/open archive barrier, offset open/blocked body, null/malformed/duplicate/extra-seed input, plain/gzip files and copied metadata all have matched checks. No new persistent gameplay state is introduced.
- Q4: all original source labels remain unchanged; current classification has separate fields. The150-zone cohort is preserved, not selected again. The60-zone deep T4 study is separately denominated. This is CoO measurement tooling, not a Qud parity implementation.
- ⚪ The runner does not prove Unity-identical maps, native input/rendering, normal-stat travel, key acquisition, intervening combat, balance or feel. Native and ordinary-player acceptance belong to the integrated root evidence.
- ⚪ Future C10.2 lair rewards may move below the recorded surface. Retain these150 original rows and add explicitly labelled whole-stack guard/reward rows after C10.2 publishes; do not interpret a surface reduction as a whole-lair economy reduction.

Published-source rerun: **68/68 GREEN**, 3.414518 seconds. Its report is byte-identical to the private candidate report; receipt `FinalCensus/shared-census-summary.json` and raw XML/data are preserved separately.


Native follow-up found67/68 passing: Unity rejects the serialized root `null` with ArgumentException before the helper reaches its explicit selection validation. The standalone JSON adapter returns null. Preserve this native RED, and normalize JSON parse errors to the helper's InvalidOperationException contract without accepting or rescanning malformed input. Two additional malformed JSON cases failed first in the private runner; after the narrow repair the full census/audit set is **70/70 GREEN**. The measurement report remains byte-identical. Native retest belongs to root; `FinalCensus/parser-normalization.json` records both histories.


## Post-foundation shared snapshot while native preview remains active

An isolated copy of the current shared rules/content passes **71/71 standalone census cases**, including all whole-lair-stack measurements. The same 150 recorded zone rows and 161 total sources remain in the same order; no classifications changed. Receipts, raw data, XML and source hashes are preserved under `FinalCensus/PostFoundation`. This is a fresh measured snapshot, not a new native or campaign acceptance claim.

The recorded surface/depth cohort now contains 394 containers: 37 locked skips, 357 actual opening events, zero refused attempts and zero blocked approaches. Its 120 wilderness and 15 ordinary underground rows are unchanged. The 15 *surface* lair rows have 66→15 containers and generated commerce 1,357→137 because the new lairs place rewards below ground. Those surface-only differences do not measure whole-lair reward loss. Ten shops add 40 total commerce from the five real liquid flasks; the designed starter grant stays unchanged.

The separately denominated whole-world study covers all 18 actual lair columns across the same five worlds. Old single-floor versus new complete stacks: 18→39 floors, 111→117 creatures, and exactly 18 bosses in each. Total container owners, including mimics, are 81→55; ordinary cache counts are 66→39. Cache plus ground commerce is 2,487→2,411 (−3.1%), within the prior bounded budget, while carried gear is counted separately. This study compares the current content under explicit old/new layout modes; it does not rewrite the archived historical sample or prove sale income, acquisition, encounter balance or ordinary walking routes.

The snapshot precedes the private late-villager door-capability/lighting follow-up. Refresh these receipts if final integrated source changes affect the measured generation. The runner first needed its omitted test support and authored non-Content JSON copied into the isolated workspace; those setup errors are not production regression evidence.

## Post-biome refresh, 27 September

After the published C15 biome/source checkpoint and feedback repairs through `a0b80738`, an isolated snapshot of894 current inputs passes the same71 census cases in5.897seconds. All inputs still match the checkout at archive time. The complete161 source rows, their150 recorded-zone keys/order/classifications and the separately measured whole-lair rows are **identical** to PostFoundation. Current totals therefore remain394 containers,37 locked skips,357 observed opens and generated commerce14,462; the later biome work did not silently change this cohort's stock or access counts.

Exact input hashes, raw reports, XML and log are retained in `Verification/DensityCompletion/FinalCensus/PostBiome/`. This refresh precedes hot-steam and pet production publication. It verifies current core generation/access under the runner's stable hash, not Unity-identical maps, native travel, acquired income or campaign balance. The continuous ordinary resource-loop acceptance remains separate.
