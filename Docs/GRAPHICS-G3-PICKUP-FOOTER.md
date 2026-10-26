# Pickup footer backing

Status, 2026-10-26: implemented and accepted in bounded ordinary Unity Play.
All 9 focused tests pass within a 139/139 native EditMode group; independent
review of the six actual 1280×800 captures found no remaining footer defect.
The frozen source package `5a3c7435985f1842e162a7cfbfb7f38bd554eaac` predates
this correction. Corrected-package and physical-controller acceptance remain open.

## Visible defect and bounded design

The ordinary Mac capture
`Verification/Live16x24/current-journey-05-crate-stock.jpg` shows the pickup
keyboard hints below the opaque panel, directly over the world. This is a
readability defect in an existing shared popup, not a missing sprite. Reuse the
existing black backing and light-gray footer treatment already used by
`WorldActionMenuUI`; preserve pickup dimensions, borders, positions, controls,
item ownership, scrolling and command behavior. No new artwork or imagegen
reinterpretation is needed to correct this missing backing.

This is Caves of Ooo's existing Qud-inspired popup grammar, not a new claim of
exact Qud implementation parity.

## Source verification before implementation

| Assumption | Verified source / correction |
| --- | --- |
| The panel is too short overall. | `PickupUI.ComputePopupPosition` already reserves `visibleCount + 6` rows, including hints and refusal status. Keep that footprint. |
| Only a small list is affected. | `PickupUI.Render` fills `visibleCount + 4` rows for every list size; both reserved footer rows remain unbacked, including capped/scrolled lists. |
| Extending the fill requires a new clearing path. | Existing `DrawBgFill` records the exact painted rectangle; `ClearBgRegion` uses the recorded prior rectangle on redraw, reader hide and close. Reuse it. |
| Hints should remain dark gray. | `WorldActionMenuUI.Render` uses the complete popup height for backing and `QudColorParser.Gray` for its footer. Match those two choices. |
| A background change should move item hit targets. | Item rows remain at local row3; mouse hit testing uses only the item row range. No input or geometry change is planned. |

## Original implementation and verification plan

1. Save focused native tests for opaque backing through both footer rows at
   one, three, twenty and scrolled twenty-six items; actual hint color; status
   clipping; shrink/reader-hide/close cleanup; and optional-background fallback.
2. Run the actual saved `PickupUI` source through a small scratch C# recording
   Tilemap harness before production edits, then after the two-line fix. This
   can prove emitted tiles/colors and cleanup, but cannot establish Unity
   rendering, resource availability, screen contrast or input-device behavior.
3. Change only pickup background fill height and action-hint color. Root owns
   native EditMode execution and ordinary before/after screenshot acceptance.
4. Independently review the diff. Report native/packaged acceptance as pending
   until root supplies those results; the frozen journey package predates this
   source correction and must never be presented as containing it.

## Verified native and ordinary-play gates

Root's native EditMode job `f3a622cdf13a4bd29c1260af087163e2` completed
139/139 tests with zero failures or skips. This selected group includes the
9 `PickupFooterPresentationTests` cases and neighboring centered-popup and
pickup-control tests, alongside the separately scoped Nella population tests;
139 is not a count of new pickup tests. The independently reviewed
[raw job receipt](Verification/Live16x24/nella-population-native-green.json)
is retained with the verification evidence.

Ordinary keyboard play continued the unchanged earned cellar checkpoint,
seed `242967504`, at `Overworld.11.10.1`, player `(64,9)`, HP31. Root used
inventory filtering and normal drop actions to place two FireClay units and
two SootrootPulp units into a real two-stack ground pile. The following are
actual corrected-source Unity captures, not mockups or the frozen Mac package:

| Actual capture | Action and independently reviewed result |
| --- | --- |
| [01: two-stack picker](Verification/Live16x24/pickup-footer-01-real-pile.png) | `g` opens both real stacks. The full Enter/Tab/F1/Esc footer is on opaque black and uses readable light gray. |
| [02: item reader](Verification/Live16x24/pickup-footer-02-item-reader.png) | F1 shows FireClay ×2 details and carry consequences in the normal reader. No visible pickup text remains over it. |
| [03: reader return](Verification/Live16x24/pickup-footer-03-reader-return.png) | Return restores the same two-stack picker and backed footer. |
| [04: one-stack picker](Verification/Live16x24/pickup-footer-04-one-stack.png) | Return takes FireClay ×2; the picker shrinks to SootrootPulp ×2 with no stale lower footer strip or duplicate border. |
| [05: closed](Verification/Live16x24/pickup-footer-05-closed.png) | Escape returns to the world with the full popup footprint cleared. |
| [06: both stacks recovered](Verification/Live16x24/pickup-footer-06-recovered.png) | `g` automatically collects the remaining single stack. The world remains clear of popup backing. |

The paired observational receipts confirm no movement or HP change during this
gate. The [final receipt](Verification/Live16x24/pickup-footer-06-recovered.json)
records `Normal` input, tick770, FireClay owner4747 ×2 and SootrootPulp owner4756
×2 in inventory. The reader round trip remains at tick750; normal pickup/close
progression reaches tick770. PNG headers independently confirm 1280×800 output;
the observer's separate `screen` field describes the Editor host and is not the
capture resolution.

**Can verify:** native tile coverage/cleanup and neighboring regression results;
actual keyboard opening, reader return, stack take/shrink, close and single-stack
pickup; visible footer contrast and absence of stale strips in these captures.
**Cannot verify from this gate:** actual controller input, physical Steam Deck,
large-list/refusal appearance in ordinary play, or a packaged version containing
the fix. Large-list and refusal layout have focused native test coverage. The
accepted ordinary route is a ground pile; container-mode lifecycle also has
native fixture coverage. A later package must include the correction before
corrected-package acceptance can be claimed.

## Self-review and implementation log

- 🟢 Resolved defect: backing previously stopped two rows before the existing
  foreground footprint. The correction remains limited to `PickupUI.Render`.
- 🟢 Independent source/test review found no significant issue. Recorded prior
  background rectangles continue to clear the complete owned area on shrink,
  reader hide and close; no other tile owners are cleared.
- 🟢 Native and actual corrected-screen gates now pass as recorded above.
  Physical Deck/controller and corrected-package acceptance remain open.
- 🟢 `PickupUI.Render` now fills its already-owned `_popupH` footprint and uses
  `QudColorParser.Gray` for unchanged action hints. No dimensions, input paths,
  authority, transfers or status text changed.
- 🟢 Saved `PickupFooterPresentationTests` has 9 cases: four list sizes, clipped
  status/clear, ground/container reader hide/restore/close with foreign sentinels,
  shrinking footprint and optional missing background. The same source fixture
  and complete saved `PickupUI` compile unchanged in the recording harness.
- 🟢 Recording RED: 9 total, 3 passed, 6 expected backing failures. GREEN: 9/9.
  Reverting only hint brightness in a scratch source copy produced 4 expected
  failures while 5 cases remained green. The production file stayed unchanged
  during that mutation check. See the
  [explicit-boundary summary](Verification/Live16x24/pickup-footer-recording-summary.json)
  and linked raw NUnit receipts; runtime was .NET 10.0.5/NUnitLite 3.14.0.
- 🧪 The scratch harness substitutes recording Tilemaps, glyph objects and inert
  input/core seams. It cannot validate Unity raster output, resource generation,
  screen-to-cell mouse mapping, real command execution or controllers. This is
  useful RED→GREEN evidence; the later native and actual gameplay gates above
  supply the separate engine/rendering and ordinary-use evidence.

Files changed: `Assets/Scripts/Presentation/UI/PickupUI.cs`;
`Assets/Tests/EditMode/Presentation/UI/PickupFooterPresentationTests.cs` and its
metadata; this plan; the four `pickup-footer-recording-*` verification records;
root's six `pickup-footer-01` through `06` actual captures and paired receipts.
