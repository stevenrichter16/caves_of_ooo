# Combat inventory verification

Native Unity 6000.3.4f1, on the real project and its normal assets. The implementation is described in [the living document](../../COMBAT-INVENTORY-IMPROVISATION.md).

- `red-native.json`: original native RED for the missing utility/sight/presentation behavior.
- `compatibility-red-native.json`: saved payload and null-lifecycle RED.
- `review-red-native.json`: partial review progress; the connector reported an initialization timeout. **Not** a completed-suite receipt. The first door fixture was accidentally open and was corrected before the meaningful door RED below.
- `pursuit-regression-red-native.json`: completed 63-case native run showing follower assistance, hidden body-contact aim and committed-melee regressions before fixes.
- `door-discovery-red-native.json` / `.xml`: 15 cases, 10 passed, 5 failed before fixes; the corrected closed-door test fails at the actual missing-screen assertion, and four help/discovery tests fail.
- `regression-native.xml`: initial broad run, 1,497 cases, 1,495 passed. Two old navigation tests assumed omniscient pursuit and were subsequently replaced with stronger explicit-waypoint navigation tests. This run omitted the 29-case utility adversarial fixture because its metadata GUID was malformed; that import fault has been repaired.
- `Native/9b13a1a30a5c4134b23ae376e4c227f7/`: initial playable witness; two harness assumptions failed (hidden-center rendering, frozen observer), with raw screenshots retained.
- `Native/798e2d9a7a2b4f448490ab7c99216bbb/`: corrected playable witness, all 13 checks passed, zero errors. Six screenshots inspected. The report states what native keys, direct calls and controlled geometry can/cannot prove.
- `final-native.xml`: final affected-suite rerun, including corrected metadata and navigation fixtures. **1,526 passed / 1,526 total, zero failures or skips**, including all 142 new cases. Native duration: 476.85 seconds.

The connector's early timeout is not used to infer test outcomes. Native Unity's completed XML, saved by its own test runner, is the completed-suite authority. This is an affected-suite verification, not a claim that every project test or every item was played.
