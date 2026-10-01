# Positive-payment regression pin audit

The broad native biome-crop regression ran 1,930 cases and found two older failures (1,928 passed). Both were reproduced independently from the exact prior release commit 38252ce7e, not merely by reverting the current changed files.

## Reproduction and provenance

`38252-source/` was extracted with git archive from 38252ce7e. Production Gameplay/Data/Shared/Scenario sources, relevant presentation helpers, Content resources, original payment tests and their StackIdentity fixture came from that revision. The isolated reference runner uses its own files outside Assets; it did not stash or modify the live Unity checkout. `baseline-provenance.json` compares SHA-256 of the original baseline files to git show 38252ce7e. All compared hashes match.

The two affected test methods include 11 parameter cases. `baseline-38252.xml` records 9 passed / 2 failed, with exactly the same case identities and reasons as the broad native run. `baseline-run.log` and `baseline-build.log` preserve raw output.

The reference harness needed a tiny UnityLogAssertionShim to compile the original tests' native LogAssert symbol. The 11 selected baseline cases do not invoke it; it therefore changes no tested branch. It is confined to the scratch runner, not game or test source. This reference harness cannot verify Unity logging/visual/input behavior.

## Decisions

1. `GameAuditPositivePaymentAdversarialTests.PlantMenuUsesActualCarriageOfTheApplicableActor("missing_backref",true)` fails at line 40's Plant-success assertion, **not** at its preceding menu assertion. Menu discovery remains a coarse query against the applicable actor's positive carried list. SeedPart's transactional Current predicate, introduced by 38252, additionally requires an exact current Physics.InInventory owner before any crop is created. Refusing an inconsistent owner graph is the intended safer action contract. Preserve that production gate. The corrected pin asserts no successful planting, no payment, no crop, no AfterInventoryAction, then restores the owner link and proves the same seed can actually plant once.
2. `GameAuditPositivePaymentTests.RefusedPlantingReportsFailureAndNeverFiresAfterAction("barren","placement_refused")` still correctly refuses, preserves the seed and emits no success/After event. The prior 38252 Current predicate now checks BarrenGroundRules.IsBarren before creating a crop, so it emits source_changed before Zone.AddEntity could emit placement_refused. This is a stale diagnostic-stage pin; update only the expected reason.

No SeedPart production edits or new farming behavior are needed. Both test files were updated only after the baseline failures were captured and root authorized the narrow pin correction.

## Current validation

`pins-current-green.xml` records **67 passed, 0 failed** across the whole two-fixture reference check against the actual current checkout, after the narrow updates. Root owns the follow-up native test and final evidence curation.

Owned changed paths:
- Assets/Tests/EditMode/Gameplay/Inventory/GameAuditPositivePaymentAdversarialTests.cs
- Assets/Tests/EditMode/Gameplay/Inventory/GameAuditPositivePaymentTests.cs

This audit does not convert the broad native 1,930-case run into a pass; its original 2 failures remain in the preserved record. A new native run must establish the updated current state.
