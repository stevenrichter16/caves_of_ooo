# Independent review — world-action cost readability

2026-10-26. Read-only review of the saved `WorldActionMenuUI.cs` diff, `WorldActionCostReadabilityTests.cs`, and `Docs/GRAPHICS-G3-ACTION-COST-READABILITY.md`. No Unity, player, tests, Git mutations, or project edits performed by this reviewer.

## Finding

No material production blocker found within the bounded adaptive-width change. Final follow-up independently read the native XML receipts: `action-cost-final-green.xml` contains 97 distinct passing cases; `action-cost-boundary-green.xml` contains 21 distinct passing cases, including the same 16 original focused cases. Their union is 102 distinct passing cases, with no failures/skips in either receipt. This reviewer did not execute them. Actual corrected gameplay acceptance remains root-owned and was underway at final review.

- The width calculation uses every action at `Open`, including offscreen choices, and clamps to 48–78 columns. It uses the same `Display ?? Name ?? ""` fallback as rendering. The exact 50-character pan label fits at 57 columns; the actual authored 60-character dressing and 56-character ink labels fit at 67 and 63 columns respectively.
- Centering, title/status clipping, labels, arrows, background, border, and pointer bounds consistently use `_popupW`. Height, action-row offsets, 12-row scrolling, shortcut mapping, selected action identity, activation keys, and input dispatch are unchanged.
- Recording the old foreground rectangle fixes the necessary wide-to-short cleanup: clearing occurs before computing the new position. Close/reader hiding clear that recorded footprint, including when background is absent. The rectangle remains bounded by the overlay. No source or world state is changed.
- Null action lists remain supported and produce the existing empty-menu row; null `Display` falls back to `Name`, then empty text. Null elements inside an action list were already unsupported by the existing render loop; this patch does not newly promise that contract. No scope expansion is needed for that pre-existing limitation.
- Dynamic labels beyond 71 characters remain explicitly truncated with `~`. The existing world F1 reader supplies complete current action text. Generic choice menus have no equivalent details hint; the plan appropriately excludes universal disclosure claims.

## Fixture review and limits

- The revised mouse test correctly projects the real legacy pointer through the camera's actual clipped `pixelRect`, shifts the camera by that measured delta, and asserts `ScreenToGrid` reaches the intended expanded cell before checking menu hit testing. This removes the prior assumed-centered-viewport failure without relaxing border/footer rejection. It is function-level hit-testing coverage, not an OS mouse click or physical-input acceptance.
- The test matrix meaningfully covers actual painted pan text, short/empty menus, capped overflow, old foreground cleanup with and without optional backing, close preservation of foreign tiles, offscreen longest labels, absolute shortcut/Enter selection, and reader hide/reopen/highlight preservation.
- The five additional cases at 40, 41, 42, 71, and 72 characters correctly test both the 48-column expansion threshold and 78-column cap using actual painted labels, unchanged one-row height, and a `~` only when the label truly exceeds 71 characters. The 21-case fixture has no material setup issue found in this final review.
- The dressing/ink test strings are synthetic long-cost examples, not the authored recipes. Keep that distinction in the final verification log or replace them with the actual labels before making an authored-recipe coverage claim. The pan test is exact.
- The reader test calls saved menu lifecycle methods and checks connected-device hint text. It does not dispatch F1 through `InputHandler`, execute a gamepad button, prove physical Deck operation, or prove a packaged correction. Those broader claims require their own existing regression suite or actual play evidence.
- Actionable documentation follow-up: the plan's pre-implementation log still says production is held for RED. Root should update that status with the retained initial RED, the pointer-fixture correction, verified final native result, and later actual screenshot/version boundaries before delivery. The retained 16-case RED contains 5 passes and 11 failures, including the initially unreliable pointer fixture; do not describe that pointer failure as independently proven behavioral RED before its projection precondition was repaired. The exact pan clipping is directly demonstrated by the original emitted-pixel failure.

## Scope assessment

Adaptive width is sufficient for the audited fixed cost labels and avoids introducing wrapped-row scrolling or a new interaction model. No wrapping framework or gameplay text changes are required for this defect. Preserve the stated overflow limits and obtain ordinary corrected-menu/F1/back/close screenshots before claiming gameplay acceptance.

## Follow-up: actual Editor pointer discrepancy

Root reported an ordinary click dismissing the menu and a later CodeDOM receipt in `/tmp/coo-action-cost-pointer.json`: legacy mouse `(1106,436)`, popup pixel rectangle `(0,120,940,680)`, `Screen=(2321,1139)`, and no mapped row. Read-only source review finds no legacy mouse assignment, `SimulateTouch`, or native-pointer override in the new test, its `ShortcutFixture` base, or `WorldLogHistoryInputTests`. Those tests read the legacy pointer and position owned cameras around it.

`ShortcutFixture.CleanupShortcuts` destroys its owned objects and invokes the embedded `InputTestFixture.TearDown`. The installed Input System package's teardown invokes `InputSystem.Restore()` and disposes the test runtime; its mocked screen-size default is 1024×768, not 2321×1139. An older `WidePopupBackgroundCoverageTests` case uses a 2321×1139 render target, but its disposal clears the camera target and releases/destroys the texture. That numerical coincidence alone does not prove a leak.

The observed x1106 is outside the receipt's940-pixel popup viewport, so that receipt correctly yields -1. It does not establish which coordinates the actual input frame saw. A useful nonmutating discriminator is the menu's `_lastMousePosition`: `HandleInput` saves the actual frame sample before hover/click resolution, and closing leaves it intact. Compare that retained value after a click against the game-relative click and camera viewport; also inspect actual camera target textures and GameView target size. Until those are compared, neither a runtime hit-test defect nor a test-input leak is established. No production change is recommended solely from this later Editor-context receipt.
