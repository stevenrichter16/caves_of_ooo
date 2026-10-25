# Fresh forge / kitchen evidence review

Independent read-only review, 2026-10-25. Inspected original 1280×800 PNGs and paired JSON receipts under `Docs/Verification/Live16x24/fresh-forge-*`. No Unity control, source changes or test execution. The supplied ordinary-input capture provenance belongs to root; this review independently checks the saved evidence, not its capture machinery.

## Verdict

The repaired-pan → paid work → ready meal → collection → Continue chain is supported by visible gameplay and identity-bearing receipts. The full-screen inventory and shared portrait render the actual collected meal, and the sword reader shows the frost-quenched long blade. This is bounded gameplay evidence, **not polished-reader acceptance**: the known throwing contradiction and food-category error remain visible in these captures and require the queued correction and recapture. No additional significant visual defect was established in the reviewed frames.

## Visible evidence

- `kitchen-before-repair`: readable ordinary interaction offers “patch the batch pan (2 fire clay)”; live player, cook, work surface, paths and vegetation remain behind the popup.
- `kitchen-repair-levelup`: the level-up reader is legible and the log records restoration, 30 XP and level 2; body and controls fit the popup. The screenshot explicitly displays HP 42/42 and +1 SP.
- `kitchen-working` / `kitchen-ready`: the pan changes from a covered light cloth shape to an open basin. This is a small native-scale world readout, not enlarged concept art. The working log states 120 world ticks and the retained two-dram fee if interrupted. Root identifies pickup13703 at[29,19], reached by C+n from[28,18]. A separate parcel disappearing from that board is **not independently established** by these small frames; no claim is made about that cue. The JSON subset does not include a board row.
- `kitchen-collected`: actual log says one item was in the pickup board, the wrapped field meal was taken, and the board is now empty. Player and cook remain distinct.
- `kitchen-meal-ledger`: selected meal name, quantity 1, weight 1 lb, parcel portrait and useful healing/bleeding description fit. The current grouping incorrectly says MISCELLANEOUS. The F1 affordance remains visible.
- `kitchen-meal-reader`: native parcel portrait and readable paginated content are present. The same page says “existing throwing remain available” and “wrapped field meal cannot be thrown.” This is the known significant presentation contradiction; do not publish the frame as corrected.
- `tempered-reader`: sword portrait, Cutting LongBlades family, 1d8 damage, +2 penetration and separate 25% bleeding / 40% freezing attempts are visible. This frame establishes the reader, not an actual combat proc.

## Transaction and identity checks

- Before repair: player2699, pan13701, cook13683, pickup13703; fire clay4728 count2. After repair the clay row is absent, pan identity is unchanged, `repaired=true`, `by=2699`, repair cause and zone are recorded; tick5650→5660. Pan HP remains0 in both receipts; this review does not reinterpret that field as a new destruction defect because repair authority is separately recorded.
- Paid work: drams42→40, tick5670 with batch start5660/due5780. The whole emberwheat stack9207 count2 leaves inventory and appears as batch input9207/count2. Claspbean pulp13721 decreases2→1; the other batch input is a distinct split ID/count1. The receipts are consistent with a one-unit split, but do not independently include a split-parent link or cook-wallet credit.
- Ready at tick5800: batch output13723, input custody cleared, still associated with the same pan/cook/pickup. No claim that this frame catches the exact due tick.
- Collected at tick5830: **the same output13723** is carried as FieldMeal/count1; pan13701 is Idle with null output, empty inputs and reset timers. The meal remains unconsumed in this evidence.
- Collected→continued: canonical JSON is exactly equal after excluding only capture timestamp and sorting the identity arrays. This covers **12 inventory rows, 2 equipped rows, 4 captured world objects and 1 weapon record**, plus every other recorded field. Player remains[28,18], HP42, drams40, tick5830; repair provenance and pan Idle persist. Equipped sword2703 retains frost-quenched name, durability8/8, `Frozen,40,,0,2`, temper count1; Buckler2704 remains equipped.
- Collected and continued PNGs are byte-identical, SHA256 `b5d3a7c53c475c9f5810f14aa0b5582276a717ee2894e7dbbfc92cf9dfe560bd`, 43,647 bytes each. This confirms identical saved visible output, not an audit of all save-stream fields or proof of a new launch by itself. Reopening the original continued image confirms populated world/HUD; an incomplete multi-image tool preview is not a game defect.

## Limits and next gate

These are actual saved Unity gameplay frames at 1280×800; no physical Steam Deck, packaged-player, performance, full regional-coverage, animation, consumed-meal or combat-proc acceptance follows. Exact-source native test results are a separate gate. Temporary compile diagnostics while a test helper was being written are not evidence of an exception during this replay. Fix the two identified information defects, run the relevant tests and capture the corrected meal ledger/reader before calling the reader polished.
