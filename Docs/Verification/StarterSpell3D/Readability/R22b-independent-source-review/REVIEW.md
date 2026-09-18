# Independent source review after R22b

Frozen review recorded after **449/449 focused tests passed**, zero compiler errors/skips. The final native run and full-suite comparison are pending. This is source acceptance, not final visual acceptance.

[receipt.json](receipt.json) records exact runtime/importer/shader/scenario and seven new test-file hashes, all 27 hitch-case classifications, evidence hashes, Q1–Q4, taxonomy and hypothesis results. No production, test, art or previous receipt was edited for this review.

## Findings

- **Confirmed:** one existing long-frame presentation policy defect could remove a cast before an unseen contact interval was sampled. The fix chooses an eligible native contact step before advancing both native handles and views. Raw wall time still reaches the five-second deadline; legacy/sprite clocks and direct native Update semantics are unchanged.
- **Correction retained:** R21's new fallback fixture accidentally inherited a farther affected cell, giving the actual sprite effect a .74s lifetime. Only that new fixture was corrected. R21c then passes strict fallback/legacy completion and reaches the native recovery RED.
- **Implementation defect resolved:** R22 stopped on CS0266 (float contact assigned to int). Contact eligibility now uses the exact renderer interpolation, including fractional samples, world projection and projectile progress. No behavioral GREEN is attributed to the compile-stopped run.
- **No remaining source blocker found.** Q1 clock/cancellation symmetry, Q2 backend/schema consistency, Q3 paired boundary/branch coverage and Q4 the explicit timing-scope correction pass within the stated limits.

## Classification and preservation

The 27 hitch cases comprise **6 reproduced presentation-defect regressions, 1 new diagnostic contract, 12 existing-behavior positive/counter controls, and 8 post-implementation adversarial boundary pins**. They do not represent 27 bug fixes. R21 was 12 pass/7 fail, with one of those failures initially a wrong new-fixture assumption; R21c independently confirms its corrected native failure. All 27 pass in R22b.

All **773 original R00 test .cs files are byte-identical**, with no missing files. The receipt stores matching canonical SHA256 digests of expected and actual file-hash maps and the exact baseline manifest hash. All reviewed inputs present in the R22b 993-file freeze match that freeze.

## Limits and documentation handoff

Recovery is restricted to raw wall frames at least .4s that would cross a complete still-unseen eligible contact interval. It does not guarantee a minimum visible duration under repeated hitches, solve Editor stalls, render hidden/occluded geometry, rewind caster gestures, or override cancellation and the raw watchdog. Actual command results and authored contact geometry remain authoritative. No Qud parity claim.

Root documentation should cross-reference the follow-up from the earlier “retain current clocks” milestone and original preflight, include WorldFxCoordinator in the changed-files table, and label old P1 integration/design media as the earlier revision when linking the active Readability work. Keep historical receipts and completed prior-phase claims intact. Final native/media/full-suite status should be updated only from the fresh results.
