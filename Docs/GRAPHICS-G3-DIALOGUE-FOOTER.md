# Dialogue footer backing

Status, 2026-10-26: implemented and accepted in ordinary Unity Play.
Corrected RED 8/11 failures → fill-only counter 2/11 failures → final native
GREEN 100/100 focused and neighboring cases. Four real screenshots and
independent review pass. Packaged parity is the next gate.

## Visible defect and bounded treatment

Actual corrected-source Unity capture
[Nella conversation](Verification/Live16x24/nella-population-13-conversation.png)
shows the Enter/quick-choice/Escape hints below the dark dialogue panel, over
the green world. Their dark gray also reduces contrast. Reuse the dialogue
body's existing dark backing through the final reserved row, and use the same
light-gray actionable hint treatment as the action and corrected pickup modal.
Preserve dimensions, wrapping, choices, pointer rows, native speaker portrait,
disposition colors, typewriter reveal and command behavior.

This is a consistency correction to Caves of Ooo's existing Qud-inspired popup
grammar, not a new claim of exact Qud parity. No new artwork, generated concept
or renderer infrastructure is warranted for the two existing draw arguments.
The observed scene and accepted native portrait remain the visual reference.

## Source verification before implementation

| Assumption | Verified source / correction |
| --- | --- |
| The popup needs another row. | `DialogueUI.ComputePopupPosition` already reserves both borders and the footer; `_popupH` is the complete foreground footprint. Keep its dimensions. |
| The missing backing is restricted to short conversations. | `DialogueUI.Render` always calls `DrawBgFill` with `_popupH - 1`, excluding the footer in short, native-padded and paged layouts. |
| The body backing is completely opaque. | `PopupBgColor` is `(0.05,0.05,0.08,0.92)`. Reuse its existing opacity and palette rather than claiming a new opaque-black treatment. |
| Filling the footer needs a new cleanup system. | `DrawBgFill` records the exact painted rectangle; `ClearBgRegion` clears that prior rectangle on redraw and close. Existing foreground clearing also records the full footprint. |
| Dialogue has a pickup-style decision-reader round trip. | It does not. `InputHandler.Clarity` suspends inventory, pickup and trade for that reader, not dialogue. Do not add a synthetic API or a misleading reader test. Exercise actual node transitions, paging, close and `OnDisable` instead. |
| Making the footer visible should alter selection. | `GetChoiceRowAtCell` restricts hits to actual choice rows; the footer stays noninteractive. Preserve this boundary. |
| Native portrait authority needs rebuilding. | Its accepted binding and layout are independent of these draw arguments. A forced captured-layout flag in one fixture case is geometry-only; existing identity fixtures retain authority coverage. |

## Implementation and verification plan

1. Run `CavesOfOoo.Tests.DialogueFooterPresentationTests` in the native isolated
   EditMode session before production changes. Four full-footprint cases and
   two independent hint-color cases should expose the baseline defect; lifecycle
   and optional-background counters must remain meaningful.
2. Minimum production change in `DialogueUI.Render`:

   ```csharp
   DrawBgFill(0, 0, _popupW, _popupH);
   // Existing action string and position remain unchanged.
   DrawText(0, y, actions, QudColorParser.Gray);
   ```

   Update the nearby fill comment to match. Do not change body alpha, dimensions,
   input handling, portrait rendering or conversation state.
3. Run the focused fixture and neighboring dialogue layout, portrait identity,
   paging and input tests. Independently review the source/test diff. A hint-only
   rollback must fail the separate color cases even if backing is correct.
4. In ordinary Unity Play, reopen the same Nella conversation, enter a short
   topic and return, then close. Capture the readable backed footer and verify
   that reflow/close leaves no stale strip. Root supplies actual evidence.
5. Keep the frozen `5a3c7435985f1842e162a7cfbfb7f38bd554eaac` package boundary
   explicit: it predates this source correction. A later package is required
   before corrected-package acceptance can be claimed.

## Tests, counter-checks and acceptance boundaries

The 11 cases cover four legacy/panel/native-padded/paged footprints; actionable
hint color during and after reveal; actual long-to-short node selection; real
last-page shrink and endpoint stability; Escape and component-disable cleanup;
and missing optional background. The disable case directly invokes the saved
`OnDisable` method: this is function-level lifecycle cleanup coverage, not a
claim that Unity dispatches that callback during EditMode activation changes.
Every footprint check also rejects backing
outside the owned rectangle and rejects footer-as-choice. Cleanup preserves
foreign sentinel tiles, checks a genuinely larger prior footprint and uses the
real recorded draw rectangles. The input fixture owns and restores its devices,
conversation globals and created objects through the established fixture base.

**Can verify with the planned native gate:** emitted tile coverage/colors,
unchanged geometry, actual keyboard handler transitions and owned cleanup.
**Cannot verify from these tests:** final raster contrast or visual feel,
ordinary actual-device input, physical Steam Deck, or a corrected packaged run.
Those remain explicit acceptance gates, not implied by native test counts.

## In-phase self-review and log

- 🟡 Confirmed visible defect: existing final row is unbacked and uses dark gray.
  Runtime correction is held for native RED.
- 🔵 Source correction: body alpha is .92, not 1; preserve it.
- 🔵 Scope pruning: no dialogue decision-reader suspension API exists. Test real
  lifecycle paths without adding a new feature.
- 🔵 Initial native RED is preserved in
  [the raw receipt](Verification/Live16x24/dialogue-footer-native-initial-red.json):
  11 cases included eight expected backing/color failures and one fixture
  assumption failure. `SetActive(false)` did not dispatch `OnDisable` for this
  non-ExecuteAlways behaviour in EditMode. The fixture now invokes the actual
  saved lifecycle method, matching the existing portrait fixture; runtime is
  unchanged. Corrected native RED is required before implementation.
- 🔵 Independent read-only source/fixture review found no other material issue.
- 🧪 Corrected native RED/GREEN and ordinary corrected screenshots remain
  pending. Root owns Unity execution; no Unity, input automation or production
  edits were performed while authoring or correcting this plan and fixture.

Files planned: `Assets/Scripts/Presentation/UI/DialogueUI.cs` (root's minimum
runtime edit); `Assets/Tests/EditMode/Presentation/UI/DialogueFooterPresentationTests.cs`
and metadata; this record; later native and actual-play evidence.


## Native progress — saved-source boundaries

- Corrected native RED:11 completed,8 expected coverage/contrast failures and
  3 passes. Initial extra disable failure was a fixture scheduling assumption;
  the corrected case calls the real lifecycle method as documented above.
- Fill-only counter:11 completed,9 pass, only the two hint-color cases fail.
  This isolates the second defect independently of the now-correct backing.
- A subsequent broader job timed out in the MCP initialization watcher while
  Unity actually ran100 cases against the earlier fill-only assembly:98pass,
  2 hint failures. Both raw job and native XML are retained as stale-assembly
  evidence, not GREEN. The saved Gray source was then explicitly force-imported
  and compiled; the final accepted run must follow that completed reload.

## Completed source gate and actual acceptance

- Final native GREEN after completed force import/reload: 100/100, zero skips,
  11.52 seconds. Raw JSON and native XML are retained under
  `Verification/Live16x24/dialogue-footer-native-green.*`. The stale-assembly
  98/100 run above remains a failed attempt, not an accepted result.
- Actual ordinary keyboard session continues the unchanged earned Nella save,
  opens chat, selects planting guidance, returns to the main topic and closes.
  Four 1280×800 original screenshots plus lossless observation receipts are
  hash-indexed in `Verification/Live16x24/dialogue-footer-captures.json`.
  Player remains at63,9,HP31,tick2270,42drams; no gameplay grants or relocation.
- Independent source/test and four-image review found no material regression.
  Full backing and light-gray controls remain legible; short-topic reflow,
  return and close leave no stale strip, border, text or portrait.
- Isolated test saves archived; Editor stopped/restored to SampleScene, seed0,
  no SaveRoot override, successful isolation finish. 776 normal save files,
  374 protected art files and both foreign PNGs match prior hashes. No storage
  cleanup. Old packages are unchanged.
- 🟡 Visible backing/contrast defect resolved with two draw arguments.
  🔵 Disable-fixture assumption corrected; stale compiled assembly explicitly
  rejected. 🧪 New package, Linux execution and physical Deck remain separate
  gates. No new art asset or exact Qud-parity claim.

Final owned files: DialogueUI.cs, DialogueFooterPresentationTests.cs/.meta,
this plan/log, screenshot/test/restoration receipts and living continuation.
