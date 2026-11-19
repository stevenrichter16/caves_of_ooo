# Machine readers: one collector for the quick and full Examine

Living record for slice S5 of the machine architecture spec. Date 2026-11-18.

## Problem

The quick Examine line (`ExaminablePart.BuildDescription`) and the full Examine document (`ExamineDocumentBuilder.Build`) each listed, by concrete Part type, the Parts that contribute a "current state" line, and the
two lists disagreed. A new machine Part needed edits to both builders.

## The contract

`IDescribeState { Scope, Order, DescribeState(DescribeContext) }` on a Part, and one `StateReader.Collect(owner, full, zone, cell, viewer, into)`. Lines are ordered by `Order` then Part insertion order, filtered by
`ReaderScope` (Quick, Full, Both), deduplicated, skipped when empty, and isolated: a describer that throws is skipped and recorded (`event/DescribeStateFailed`), never allowed to hide the others. Describing is read-only.

The seven shipped providers moved onto it with their legacy order and scope: film (10, quick only), kitchen batch (20), sodden dressing (30), oil straining (40), contained liquid (50, quick only), repair (60), trap jamming (70).
Both readers now call the collector instead of naming those Parts, so a new Part appears in both without editing either builder (pinned by a source test and by a test-assembly Part that shows up in both).

## Decision recorded, not made

The full reader has never shown the film and contained-liquid lines. This slice keeps that exactly (they are `Quick` only) and records the question for the owner: should the full document gain them? Flipping their scope to `Both`
is a one-word change and a behaviour change; it is not done here.

## Evidence (native)

`StateReaderTests` (11): order, scope, duplicates, empties, isolation, read-only, null safety, a new Part in both readers, a quick-only line absent from the full document (counter), the shipped order/scope table, a real repair
line in both readers, and the source pin. The adjacent reader, examine, announcement, kitchen, dressing, oil, contained-liquid, trap, repair and film suites (2,423 tests) pass unchanged.

## Limits

Player-visible marker lines ("set here to be used") and a truthful "works here" line are not added: every authored string needs a voice card and the blind gate, and the forge's shipped description overstates its use
(single weapons assemble anywhere from the pack). Those are separate owner decisions.

## Update: "works here" for forges and benches

`ForgePart` is now an `IDescribeState` provider (both scopes, order 90): its Examine says "Cold work here: dismantle tool (from a released extraction tool); dress iron point (from a recovered iron blank); reclaim iron and timber (from a released extraction tool). Carry the find in your
pack and stand beside it." The text is built from the shipped `ForgeCold` process rows (`MaterialProcessRegistry.ForWorkKind`), so a new row appears with no edit, a hand-cutting row never appears at a forge, and no loaded rows means no line. It answers the long-standing gap that a player who had never carried a find could not learn what a forge was
for (forge dossier F4), and the same Part gives the camp bench and the field work rig the same line. It makes no claim about weapon assembly (a single weapon assembles anywhere from the pack), which was the overstated sentence in the shipped description. `StationReaderTests` (4) pin it; the generic collector tests now use a neutral
subject because a forge describes itself.

