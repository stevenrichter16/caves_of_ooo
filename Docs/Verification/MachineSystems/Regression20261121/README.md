# Inherited full-suite assessment — November 21, 2026

`fullrun_h` completed its result XML for inherited runtime e89a81af6:
40,376 total, 40,267 passed, 92 failed, nine inconclusive and eight skipped.
Reported duration14,284.47 seconds. The log contains zero `error CS` lines.

Comparison with full_s9 found the same91 baseline failures, no fixed baseline
failures, and one new180-second timeout:
`ThrowingPayloadComparisonTests.W7_WaterAndAcidOrdersPayForTheirRealInterveningEnemyTurns(2)`.
The independent isolated family run `fullrun_h_new_failure_isolation_20261121`
passes30/30 with zero compiler errors, 56.15 seconds total; that exact case passes
in1.969366 seconds. Another worker's separate `fullh_rerun_w7` also reports30/30.
No new functional failure was reproduced. Resource/order sensitivity is plausible;
the specific cause of the full-run timeout has not been established.

A read-only macOS process sample at17:40 showed49.6GB physical footprint,
77.5GB peak, on the32GiB host, with most sampled main-thread work in managed
array allocation/garbage collection. The run subsequently advanced to later
families and wrote complete XML. Its process received guarded SIGTERM only after
results were complete and parsed, following more than three minutes of expensive
post-result teardown. XML and log remain outside the repo. The summary includes
the XML hash, paths and cleanup qualification; this is not an exit-code-zero claim.

This verdict concerns the inherited runtime. It does not cover the later published
source-stock/action-time fixes or the later scenario-capacity correction, and it
does not turn the known91 failures or17 nonpassing/nonfailed cases into a clean gate.

## Concurrent capacity draft, independently inspected

While this chat resumed, another worker edited ScenarioLoadout.MakeRoom and ran
`red_cap1` and `green_cap1`. The code was not committed or staged by this review.
Recorded results: RED35 cases/31 passed/four failed, including actual shipped-cap
truncation and the empty-loadout cap change; GREEN159/159. The two additional
countercases written in this chat (preexisting item cannot mask a failed grant;
duplicate requests report their combined count) are present and pass in that GREEN.
The empty-loadout guard also passes. These observations are not ownership or
acceptance of that worker's changes, mutation checks, native play or delivery.

The same worker was using clone3 (`green_cap1`, PID48493 when observed). This
chat held further source/Git/Unity mutations and requested that the overlapping
Claude session stop, preserving the user's single-owner coordination rule.
The user then explicitly authorized this chat to stop the overlapping session.
Claude PID12294 was identified through its cap_mut shell51399 and same-repository
working directory. Guarded SIGTERM stopped that session and its polling shell;
other Claude sessions and Unity were untouched. All36 scenario-file hashes in
the pre-stop manifest remained identical. Root resumes sole Unity/Git ownership
and subsequently validated/adopted the preserved draft with provenance retained.
Five compiling mutants were killed and fresh gameplay/Continue passed the bounded
checks in the [loadout receipt](../ScenarioLoadout20261121/README.md). Final bounded
independent source/artifact review found zero P1/P2; it did not operate Unity.
