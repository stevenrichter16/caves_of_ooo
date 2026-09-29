# Final cold review: physical obstacle pursuit

Read-only review on 2026-09-29. No shared source, Unity, Git or frozen evidence was changed; no tests were rerun for this review.

**Conclusion:** no additional significant gameplay defect found in the one-file candidate. The 32-fixture native request is appropriately scoped and all 32 names resolve uniquely to current test classes. Candidate adoption still requires the controlled native baseline and subsequent native acceptance. The two reported live setup failures contain no paid pursuit baseline and must not be counted as gameplay RED.

## Q1–Q4

- **Q1, symmetry:** the candidate always evaluates the existing intermediate-cell Physics occupancy check, omitting only actual Creature-tagged owners when requested. This agrees with the relevant physical-owner distinction in `Zone.CanPlaceFootprint`; that body/contact branch is unchanged. With `ignoreCreatures:false`, the predicate's truth table is unchanged. The legacy single-cell Solid-tag check still precedes the omission, while the body branch has its existing own semantics; this repair does not claim to unify every structural-tag edge case.
- **Q2, cross-feature consistency:** doors still receive their permission/opening cost before occupancy, including at the endpoint. Legacy target-cell exemption, explicit bodies, ToContact, diagonal policy, terrain/gas cost, heuristic and movement execution remain unchanged. `Physics.Solid` is authoritative for physical props; adding Solid tags would incorrectly alter their existing sight behavior. No public API, saved field, event, scheduler action, cache, model or blueprint changes.
- **Q3, counters:** all 11 maintained methods / 43 parameterized cases were read. The paired coverage below exercises the changed policy and real execution rather than accepting a nonempty route as success. The selected existing `FindPathTests.PathToSolidGoal_IsUsable` retains the deliberate endpoint exception. No additional speculative tests are warranted for this predicate.
- **Q4, documentation:** the current living document accurately narrows this to CoO navigation and distinguishes private, native-test and native-input evidence. Its draft/pending labels must be finalized with the actual release receipts. One minor selection-metadata omission is identified below. Existing FindPath's broad “zero GC” header is not evidence of whole-search allocation freedom; this delta introduces no new allocations, and the living document correctly makes no broader performance claim.

## Maintained counter map / taxonomy

| Player-flow hypothesis | Actual paired coverage |
|---|---|
| Ordinary beam/barrel/Hedge blocks motion without becoming a sight wall | Three actual factory blueprints: Physics, movement refusal, passability and LOS controls. |
| Ignoring actors accidentally ignores stationary props | Both creature policies across three props and ordinary Wall; actual Creature occupancy true/false route counter with real move refusal. |
| Every blocked direct step means a broken chase | Thin-owner fallback controls versus complete open-ended boundaries and identical Solid-tagged controls. |
| A returned route may be physically unusable | Entire independently planned bypass executes real MovementSystem steps; sealed boundaries reject instead of reporting a usable detour. |
| Combat scheduling hides a stuck route or extra action | Real Brain/KillGoal/TurnManager reaches contact across single-cell/wide-body controls, bounded waits/ticks, energy and unchanged-HP assertions. Explicitly assigned KillGoal isolates navigation; it does not prove ordinary hostile acquisition. |
| Declared body or contact handling changes the result | Legacy, explicitly declared one-cell and genuine two-cell bodies; correct contact preflight. |
| Doors become free passage | Operable, incapable and locked counters retain stationary paid opening then movement. |
| Hauling a load into a throat grants permanent immunity | Loaded throat requires actual open-end detour; removing only the load opens the direct route. |

Relevant taxonomy surfaces are anti-exploit reachability, cross-owner classification, boundary/sealed-route refusal, planner/executor consistency, body/contact consistency and repeated scheduled actions. The change adds no transaction, callback, collection mutation, parser, probability, effect stacking, serialization or independent diagnostic dispatch surface. Existing action diagnostics remain in their normal goal/movement/turn paths; no per-node hot-loop emissions are appropriate. Original ten hypotheses were frozen before the detailed audit; this review does not replace that test-first receipt.

## Original versus parity

`Docs/QUD-PARITY.md` describes the older goal-stack/A* architecture as a Qud source port, and FindPath retains a “mirrors Qud (simplified)” header. Those historical labels do not establish external source equivalence for this change. The present milestone is an original repair of CoO's already implemented Physics/Creature/movement contract. No matching external Qud source was inspected, and exact external parity is not claimed. The relevant reference for this slice is the actual CoO body-aware predicate, ordinary wall counter and real movement authority. The living document records that distinction explicitly; do not describe this shipment as a verified Qud parity port.

## Evidence and native request

Unchanged source: corrected private baseline 43 = 31 pass / 12 fail. The initial 27/16 receipt includes four rejected wide-body test-preflight assumptions and remains preserved. Private candidate: 43/43 and supported affected 695/695 (including the 43), plus separate actual-reference runtime/test compilation. The native-only decode-isolation fixture remains in the native request despite exclusion from the private runner.

Root's actual native test-only receipt is 45 = 31 pass / 14 fail: 12 navigation failures and 2 observer API failures. That supplies genuine navigation test RED; the separate live setup failures do not. I did not execute native tests or inputs during this review.

The 32-fixture request covers planner/execution, multi-cell/contact, door operations, hazard preferences, ordinary goals/following, turn accounting, drag lifecycle and newly shipped finite predator/prey callers including arrival. It is sufficient for this narrow source delta; unchanged art or whole-world census repetition is not justified without a new concern. All 32 names are unique and present. Current native execution remains root-owned.

**Minor evidence-label correction:** `native-regression-selection.json` lists `SpreadHuntArrivalTests` among its 32 fixtures but omits it from `absentFromF9`. A read of the actual F9 receipt confirms it is absent. There are seven selected fixtures absent from F9, not the six listed. Preserve this frozen selection receipt and record the correction in final integration evidence; the executable request already includes Arrival correctly. The historical 531 F9 cases are not a claim that the new F11 fixtures ran in F9.

## Reviewed pins (SHA-256)

| Artifact | SHA-256 |
|---|---|
| Current shared FindPath / private preimage | `a17334766c9cf75dcd535d7cc09024774672ece05c28737d99297daca493f724` |
| Candidate FindPath | `968ce0542beb732fc9adbebcaceb9ec659d4745aca9a2316cebe97d83478fddb` |
| Maintained 43-case fixture | `1eaeb8c6dc0901c07e3718583f27c689d01a91fb46db07c434b37fade6179526` |
| Native 32-fixture request | `01c029c54aa56fd8d9fb1c66be07638ff834fbaba9ad3dc432705333f8e05333` |
| Frozen selection metadata | `3f1d53909eed947d2a37fdd7e950ae1e20a0e7502b666303eb848e1307ad6205` |
