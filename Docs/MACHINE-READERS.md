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


## Update 2026-11-21: "how to use" (IDescribeUse), the owner's descriptions request

The owner: descriptions of machines and tools should "tell the player how to use them, if they're usable in that moment, or what materials are needed, or what environmental factors are needed", not a long flavor text. This is a second contract beside the state readers, because use is not state.

- **`IDescribeUse { Order, DescribeUse(DescribeContext, UseGuide) }`** (`Gameplay/Entities/UseGuide.cs`) on the Part that owns a thing's verbs. A Part adds to three lists on the shared `UseGuide`: `How` (sentences, in the order a player does them), `Needs` (phrases for what must be carried, true or near; a machine with several verbs writes "to brew: ...", "to settle: ...") and `Limits` (what it costs or risks), and may set `WhyNot` (the reason it offers nothing at this moment; the first reason given stands, so a kit still being carried is not overruled by a machine's narrower one). Every number comes from the constant of the rule that enforces it (`SourceStockPart.MinCapacity`, `LiquidSettlingPart.Ratio`, `ProofingRules.PermanentUnits`, `PlacementCheck.ZoneCap`, `RestSystem.HostileScanRadius`...), so the text cannot drift from the rule.
- **One collector, `UseGuides.Collect(owner, zone, cell, viewer, into)`,** composes the block: `How to use: ...`, `Needs: a; b.`, `Cost and limits: ...`, then the live line. A thing being carried is asked about its carrier, but only when the carrier is the player (a thing in a chest, or in a merchant's stock being previewed, is not asked about as the chest or the merchant). A throwing describer is skipped and recorded (`event/DescribeUseFailed`). Both readers call it: the quick Examine puts the block ahead of the flavor text, and the full document has a "How to use" section between "Current state" and "Description" (its first line does not repeat the heading).
- **The live "Right now" line is built from the thing's own menu rows, not from a second set of predicates.** The collector asks each describing Part for the `GetInventoryActions` rows it would show this viewer (the same event, answered by that Part alone, with the same two parameters the menus pass). Enabled rows become `Right now you can: <row>, <row>` (four named, the rest counted; a crowd of rows under one `Group` heading is said the way the interact menu shows it, `lay a device here (8)`; inert picker headers are left out), disabled rows `Not now: <row> (<reason>)` or `Right now you cannot: ...`, and when the Part shows no row at all its `WhyNot` is given. A machine standing in the world with the viewer more than one step away says only `Right now: you are not beside it.` (its menu cannot be opened from there), and a carried tool lying on the ground says to pick it up first.
- **`IDescribeUseRows`**: a Part whose menu lists rows that are always there but only sometimes work states its rows for describing itself instead (the collector then does not fire its menu handler). The forge's Craft row is enabled only for a valid pick (a weapon's parts, a weapon and a part, a weapon and a quench), the still's brew rows only with reagents picked, a waterskin's drink and fill only when it holds water or is not full and fresh water is beside you, and a hearth's rest rows are refused with a hostile creature near; each says so, with the reason, as a disabled row.
- **State moved out of how-to.** The providers that used to mix instructions into their state line (the scraper, tray, fish trap, awning, proving post, frame, setting bench, spring trap, trough) keep only state in `DescribeState`; the instructions are in `DescribeUse`.
- **Static descriptions are flavor only.** `MachineUseGuideTests.AMachinesStaticDescriptionIsWhatItIsAndNeverHowToUseIt` pins every shipped machine and tool blueprint at 130 characters, no number and no instruction word; the how-to lives in the guide. A new machine ships a guide (the kit census test fails otherwise).
- **Not covered, and why:** the torch (its live flame and refuelling lines already say how: `ItemExamineService`); crafting materials, which keep their "Field uses" section (`MaterialUseDescription`); the Concord's carriage, a conversation; the field work rig, whose accepted M1b description is a tested contract that already states its yield, loss and hauling rules; the older machines (oil strainer, dressing bench, kitchen batch pan, ink desk), whose accepted `Describe()` already gives their recipes, costs and limits; reagents. Add `IDescribeUse` to any of these when a defect is found.
- **Every wording in one place:** `Docs/MACHINE-USE-TEXTS.md` is generated by the explicit test `MachineUseGuideTests.ExportUseTextsForTheVoicePass` (run it by name in a clone and copy the file back) and shows each machine's block in the states a player meets it in, for the owner's voice pass.
