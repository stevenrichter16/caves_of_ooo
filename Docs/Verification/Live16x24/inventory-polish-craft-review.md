# Independent cold review: craft completion feedback

Read-only source review, 2026-10-25. Scope: new BrewReagentsCommand receipt counters, inventory success/failure status, retained status color, craft completion prose and modified receipt/feedback tests. No Unity, execution, source changes or staging. Selection and meal changes have a separate review.

**Verdict: no significant correctness regression found in this diff.** Native GREEN is root-owned and pending at review time; prior RED counts are not acceptance. Actual ordinary UI capture remains necessary.

## Counts and transactions

- BrewReagentsCommand resets all four counters at Execute entry. It populates them only after TryBrewBatch reports at least one committed iteration; failure before any output leaves zero. BrewingService adds exactly one result and increments madeCount once per successful iteration. Brew, inert sludge and mishap counts remain separate; a mishap is not described as a created item. Existing payment, per-unit rollback/commit, nonlethal damage, discoveries and audio publication are unchanged.
- Counting outcome records, rather than output references or resident stack quantities, correctly handles units merged into an existing stack. Forge completion likewise uses ForgedWeapons.Count: WeaponForgingService explicitly appends one recipient entry per produced unit, including repeated references to a resident stack. The count is therefore unit count, not unique stack count.
- Partial success reports actual/requested units. The UI creates a fresh command per activation and reads counters only after Success. The documented API is last Execute, so a later validation refusal would retain the previous Execute receipt; this is not a new UI bug because these callers never reuse the command and ignore receipts on failure.
- Successful forging sets completion before optional quenching. A quench refusal overwrites it through ShowActionFailure, which resets the success flag and retains explicit “Forged weapon; quench failed” partial-outcome prose. It does not claim the forge was rolled back or that the quench succeeded. Existing weapon/remaining-component custody remains unchanged.

## Presentation and lifecycle

- Success is stored in the same menu-owned state with current panel/mode/context as refusal. ClearActionStatus and SetActionFailure explicitly reset the success flag, preventing old green state from coloring the next refusal.
- ExecuteCraft clears the prior message, executes once, rebuilds previews and paints once. Completion stays through incidental repaints and does not re-execute or republish the command. Mode changes, a new pick, clearing picks, panel changes and Close retire the old message.
- The retained painter changes only the outcome color and keeps the same bounded y712 status row after modal rendering. The action legend remains separate. No additional renderer, animation, audio, turn charge or gameplay command is introduced.
- Moving the craft cursor without changing marked inputs retains the last completion; this is reasonable because cursor navigation alone does not replace the selected recipe.

## Test strength

- Receipt tests cover actual partial Brew, Sludge and Mishap attempts (two available units, three requested), exact physical output units, reagent exhaustion, mishap-only HP loss and a reused command reaching Execute with no stock. Those are outcome-level assertions rather than counter mirrors.
- Feedback tests exercise actual single crafts, merge into an existing resident brew stack, cached partial batch and callback removal of the remaining reagent. Repaint checks preserve identities, quantities, HP, tick and message count; no pending everyday/throw request appears.
- Retained success assertions require exactly one active correctly colored label, real bounds, no later covering panel, and a readable non-overlapping action legend. The quench-failure counter still checks red failure, untempered output, retained quench custody and committed forge payment.
- No broad runtime test hook or command bypass was added. Physical Steam Deck behavior, final visual quality, full command-pipeline exception behavior and unrelated crafting mechanics remain outside this bounded review.

## Frozen reviewed bytes

- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs`: `f2d6e27178c98f5354a14cfd47973bc4714acf9dd93b1c676d55ae182b4c015c`
- `Assets/Scripts/Presentation/UI/InventoryUI.cs`: `65331f041cf37fc9b025d431265ad8f9084e9dea399ff7be63a72e377f259ae9`
- `Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs`: `660d9d6017f99a7f420c9a2d378f04535260140302a93b2acbb9ce25288df672`
- `Assets/Scripts/Presentation/UI/InventoryUI.LedgerView.cs`: `6963e0613a47622719f7a1de0e98b1f21521ea805df91ee0e4bb889a9c47addd`
- `Assets/Tests/EditMode/Gameplay/Alchemy/BrewReagentsBatchCommandTests.cs`: `376ccac5a4990d9fe6f662bc37ee0436c393e647a190b24489f510d5cd3134ed`
- `Assets/Tests/EditMode/Presentation/Input/GameAuditActionFeedbackTests.cs`: `2b3a929c09a303fb9eccfb615bc7210493d808d6ec769df53d9beb1031cf7d81`
- `Assets/Tests/EditMode/Presentation/Input/GameAuditActionFeedbackAdversarialTests.cs`: `4ececae5c29d812f25e252dc78ff92f4e35ea55ea02cab0011534e87fb842e1b`
