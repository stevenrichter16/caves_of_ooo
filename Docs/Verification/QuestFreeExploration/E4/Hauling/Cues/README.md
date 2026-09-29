# Haul and Let Go contextual cues

Status: private candidate is ready for root publication and native GREEN. Root owns all shared files, editor execution and release. This is an original, small extension of the existing CoO current-action cue, not a new hauling mechanic or an external-parity claim.

The query now offers the actual built-in Haul menu command for a currently eligible visible adjacent owner, and Let Go for the exact reciprocal live grip. Existing Harvest, Container, Door and sign/read checks stay ahead of the new branch. Existing one-marker/HUD consumers, input handling, action costs, models, movement and saves are unchanged. Normal hints direct the player to `C, direction: menu`; focused hints use `Enter: menu`. Execution still belongs to the actual menu and command.

## Evidence and correction

- Root native RED: job `ec95cf673e164ebb9e8fb5ae22119b7f`, 44 cases, 34 PASS / 10 expected missing-cue FAIL / 0 skipped, 2.3127685 seconds. This includes all 27 new cases and all 17 existing UI cases. Raw root receipt is `Hauling/Integration/native-cue-red.json` relative to the E4 evidence root.
- Initial private baseline: 68 cases, 58 PASS / 10 expected missing-cue FAIL. The 41 existing query/adversarial controls all passed.
- The first candidate exposed three fixture failures: a synthetic Speed stat used the default maximum 30 while the test intended ordinary Speed100. They are retained in `private-candidate-premise-red.xml.gz`. The test now declares Min0/Max1000 and asserts Speed100 during setup. No behavioral assertion was relaxed.
- Corrected matched baseline: 58 PASS / 10 missing-cue FAIL. Unchanged candidate with the corrected fixture: **68/68 PASS**, 0 skipped. Thus the setup correction did not remove the missing-feature RED.
- Actual Unity assembly-reference compilation: runtime and complete EditModeTests assemblies both exit0. These are compilation checks, not native execution.

Raw private NUnit receipts are compressed byte-for-byte from the original XML. `verification-summary.json` records the original hashes, test names, durations and input hashes. The original test-only manifest is retained as history; its working source path was subsequently corrected. `WorldAffordanceHaulingTests.before.cs.txt` preserves the originally published source. The separate final test-delta manifest names the exact current shared preimage.

## Review and Q1–Q4

**Q1, symmetry:** actual grab and release flip the same cue owner and invalidate the old command. Release deliberately does not reuse grab strength/weight admission; it requires current reciprocal DragPart/DraggedPart references and never repairs them. No Enter/C key behavior changes.

**Q2, cross-feature consistency:** the new sealed Handling branch is last in the existing verb chain; current player, zone, visibility, reach, physics and owner-part gates remain upstream. Querying does not dispatch GetInventoryActions or actions. Real menu rows are checked in tests separately, because menu declaration alone omits reach and another holder. Arbitrary future callbacks remain execution authority.

**Q3, counter-checks:** actual beam/barrel and strength/minimum-grip positives are paired with heavy, hidden, foreign, held, carried, equipped and distant refusals. The dedicated seven-case adversarial fixture covers cached command changes and malformed grip backlinks without cleanup side effects. Repeated queries preserve grip, speed, time, log and owner position; ordinary container priority is retained. The native 17 UI controls passed before this candidate and are selected again for root's matched GREEN.

**Q4, documentation:** this verifies current built-in availability only. It does not prove all possible event vetoes, normal-world discovery, generated placement, paid drag movement, pixels or performance. The proposed F9 native witness owns actual menu/Haul/Let Go and visual acceptance. No new broad observer or renderer was added.

Bounded peer review by `/root/hauling_mechanics_audit` found no concrete blocker in current authority, priority, reciprocal release or read-only behavior. It confirmed DragRules→HandlingService→GetDisplayName has no callback dispatch. Existing CanLift can format a discarded refusal string; **zero allocations are not claimed**. No world scan, per-query collection or new cache is added.

Self-review: the Speed fixture premise was corrected and rechecked RED→GREEN. No remaining substantive source finding. Native GREEN and actual cue pixels are explicitly pending; no completed release claim follows from these private results.

## Frozen adoption package

`production-manifest.json`: one WorldAffordanceQuery path, SHA256 `344bbeb30a0490a1fe87a6f0c897c7699f7522a81e8c0766dfe000e97bf261a1`.

`test-delta-manifest.json`: one existing fixture setup correction, SHA256 `c3b236b77ec5e904e0d34d944f9c02a2f0f024a552a0534cc5f117c26e5323bb`.

The complete source-level native selection is `WorldAffordanceHaulingTests`, `WorldAffordanceHaulingAdversarialTests`, `WorldAffordanceQueryTests`, `WorldAffordanceAdversarialTests`, and `WorldAffordanceUiTests` (85 cases). Root may include existing pixel controls separately. Tests and source package are frozen; this document does not authorize shared publication by this worker.
