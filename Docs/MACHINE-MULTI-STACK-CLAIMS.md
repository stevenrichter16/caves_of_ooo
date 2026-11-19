# Multi-stack ingredient claims (S12b, extras only)

Living record, 2026-11-18. Design critique: [MachineDesign/critique-s12-multistack.md](MachineDesign/critique-s12-multistack.md).

## Problem

A process row resolves each extra input to one carried stack with enough units. The shipped `serrated_kit` row needs two `SpurgrassSpine`; when the spines are two carried stacks (one bought, one recovered after a throw, or stacks split by other
code) the row silently vanished and nothing said why.

## Change

`MaterialProcessIngredient` now holds one or more `Take`s (owner, units, original count, state). `Resolve` runs the **legacy resolver first and unchanged**: any single sufficient stack keeps its exact choice (lowest ID among sufficient admissible stacks), and the deliberate ambiguity
refusal (several sufficient, one non-plain) stays a refusal. Only when no single stack is sufficient does it **pool**: carried, non-equipped, unprotected stacks of the one blueprint that are plain template-exact and all merge with the first by `StackerPart.CanStackWith`,
smallest stack first then ID ordinal, spending whole small stacks before touching a larger one, never the source, the tool or the actor. Source pooling is not done (no shipped row has a multi-unit source). Guarding, claims, `BeforeMaterialProcess`/`MaterialProcessCompleted`
fan-out, the Before/After checks and the quote's currency all run over every take.

**Rollback fix (reviewer P1).** The undo used to detach every input before `InventoryTransferSnapshot.Restore`, which re-inserts only entries the consumption changed; with several takes a refusal part-way would lose an untouched stack. The undo now detaches only inputs
the transaction actually consumed from.

## Evidence (native)

`MachineMultiStackClaimTests` (14): two single-unit stacks pool and commit conserving every unit with no zero-unit stack left (RED on HEAD: the row vanished); one stack of two and a low-ID insufficient plus high-ID sufficient pair keep the legacy choice (parity); lowest IDs
are spent first among equal stacks; one spine total, a live recovery claim, an effect-touched stack and another blueprint never pool (counters); an inert recovery mark pools; a live-claimed sufficient stack is ignored as before; an outer failure after the command restores every stack at its
original place; a stack removed between quote and commit refuses and spends nothing; a new lower-ID stack after the quote is not a take; and a differential sweep over every stack-count vector (three stacks, 0 to 3 units each) matches the legacy oracle or pools. The existing spurgrass, composite,
leather, reclamation and process suites pass unchanged.

## S12a: why a row is not offered

`MaterialProcessMisses.Explain` adds ONE dimmed row (priority 4, below a real process row's 19, no quote, no token, never dispatched) when a carried, permitted source is a near-miss: exactly one requirement group fails. Codes: `short` (source or an extra input, with
how many you have), `stacks-incompatible` (enough units that cannot be used together), `no-tool` (hand-cutting work with no usable tool), `no-station` (forge work with no forge or rig at all within reach). Never shown for a ground or shop source, when two or more groups fail,
or when an unready rig or forge is within reach (its own menu already says why). `MachineProcessMissTests` (8) pin each reason and each counter; the 735-case machine, process, action-row and inventory suites pass.

## Limits

`no-tool` does not separate a missing tool from a broken one; the menu label does not say a pooled quote spends from several stacks; the rollback snapshot still does not restore a recovery mark's `Revision`
(as in legacy); actual play of throw-one-recover-one is not done.
