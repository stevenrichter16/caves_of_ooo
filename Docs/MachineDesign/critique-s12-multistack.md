# Independent critique of the S12 multi-stack claim design (2026-11-18)

Read-only review by a separate reviewer before any S12 code was written. Verdict: **PROCEED WITH CHANGES**. Condensed; file and line references were to the tree at `22fc152e7`.

## Findings (P1 must fix before RED, P2 before ship, P3 note)

1. **P1 rollback loses untouched takes.** The undo detached `products ∪ received ∪ inputs` while `InventoryTransferSnapshot.Restore` reinserts only entries the `Apply` actually changed. With several takes, a refusal part-way through consumption would detach an untouched stack and never put it back. Fix: undo detaches only inputs the transaction consumed from.
2. **P1 the parity rule was imprecise.** The legacy resolver returns null for (a) no sufficient stack, (b) sufficient stacks exist but none admissible, (c) several sufficient with a non-plain one (deliberately ambiguous). Pooling may engage only for (a). Trap: stacks of 1 and 3 with a row needing 2 must spend 2 from the stack of 3, not 1 and 1. Fix: call the unchanged legacy resolver first and pool only on a classified null; keep the legacy resolver as a differential oracle in tests.
3. **P1 "anchor" was undefined** for the case the proposal targets: in `serrated_kit` the source is the cord and the pooled ingredient is the extra (spines). All six shipped rows have `InputCount` 1, so pooling the source has no consumer. Fix: pool extras only, with one total order for takes.
4. **P2 the admission predicate was misdescribed.** `CanMergeProcessingInputs` is one of six clauses of `StackerPart.CanStackWith`; the real safety property is template-exactness. Require both a plain template snapshot and `CanStackWith`.
5. **P2 no minting or laundering path found** for inert tracking parts; instance-policy stacks never become siblings. Residual: the rollback snapshot does not restore `ThrownRecoveryPart.Revision` (legacy has the same gap).
6. **P2 sibling exclusions** must cover source, tool, station, actor and equipped items; every take must hold at least one unit and be guarded and listed once.
7. **P2 the `held` reason is unreachable** (a carried stack cannot hold a live claim); `terms` and `content` have no S12 producer; `no-tool` should split from "tool unusable"; `no-station` must not fire for an unready rig that already shows its own row.
8. **P2 disabled rows must not spam**: low priority, carried sources only, one row per (source, process), near-misses only, no quote or token, no per-menu-build diag.
9. **P2 the Zone capability index is unnecessary** for S12 (`GetReadOnlyEntitiesWithPart<ForgePart>` exists).
10. P3 entity IDs are decimal counters or random GUIDs, so ordinal order is effectively arbitrary, but admitted stacks are template-equal so it is not exploitable. P3 keep the public `TryCreate` overload. P3 preview and commit windows are already closed by the existing quote pattern.

## Smallest sound design (adopted)

S12a read-only reasons (deferred); **S12b extras-only pooling**, legacy first, template-exact siblings that `CanStackWith`, smallest stack first then ID ordinal, rollback fix first. Merge-first inside the commit was weighed and not adopted.

## Required tests (beyond the spec)

Differential parity oracle over stack-count vectors; legacy-null classification; rollback injection; conservation; exclusions; custody during `BeforeMaterialProcess`; preview-to-commit mutations per take; prepared-throw cancellation; disabled-row discipline; save/load and non-player actors.

## Owner questions (answered by the implementer, flagged)

Which units are spent: smallest stacks first, then ID ordinal (consolidating). Blocked rows only for near-misses: deferred with S12a. Disclose "from N stacks" in the label: not done (the label states definition counts only). Narrow S12 to extras-only: adopted.
