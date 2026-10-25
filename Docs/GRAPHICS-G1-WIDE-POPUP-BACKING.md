# G1 — Continuous popup backing at wide window sizes

2026-10-25 UTC. **Implemented; native93/93 and actual keyboard wide/1280×800 captures accepted within the bounds below.**

## Observed problem and verification sweep

A normal Unity FreeAspect play session showed vertical transparent gaps through
the inventory Examine popup. Cyan inventory text was visible between dark
background strips. Root observed this through CUA without sending game input.
The player closed the popup before a file capture; the later read-only world
capture is not a reproducer. It reported popup grid cell width1.1860311.

Source audit confirms the geometric mismatch: `AnnouncementUI.DrawBgFill` paints
unit-width CP437 solid sprites; `ZoneRenderer.UpdatePopupOverlayGridLayout`
increases cell spacing with camera aspect. Inventory Examine intentionally uses
the full display via `CameraFollow.SetCenteredPopupOverlayOverUIView`. Above16:9,
spacing exceeds sprite width. The overlay camera clears depth only, so gaps
reveal the inventory as observed. This is a source-backed hypothesis until a
native geometry test and actual live reproduction establish it.

| Premise | Correction |
| --- | --- |
| Opaque tile presence means a continuous backdrop | Tile presence tests miss uncovered space between adjacent sprites. Measure transformed bounds and rendered pixels. |
| The new world1×1.5 grid causes this | PopupOverlayGrid is separate; its horizontal spacing follows camera aspect. Do not change the world projection. |
| Fixing Examine alone covers shared popups | Announcement, Dialogue, Pickup, ContainerPicker, WorldActionMenu and other centered panels use the shared background map. Correct its backing transform without resizing glyphs. |
| Every popup redraws on resize | Existing cached fills may remain while the grid changes. The background fix must follow layout changes without requiring another UI Paint call. |

## Minimal plan

1. RED: actual filled background geometry at widths0.9,1.0,1.1860311; inventory
   camera wiring and resize while open. Keep a counter-check that foreground
   glyph dimensions and cell positions remain unchanged.
2. Candidate: apply only the shared background tilemap's custom orientation
   scale to match its grid cell width during layout update. Verify actual Unity
   transform semantics before accepting. No font atlas edits, per-tile polling,
   palette changes, new objects, camera-layout changes or input changes.
3. Run focused and existing centered-modal/examine/fullscreen ownership tests.
   Inspect a real wide-window inventory→Examine before/after through ordinary
   input, then standard1280×800 and resize/close restoration. Test shared popup
   backing as well as the specific observed Examine path.
4. Independent review, same-commit evidence, fetch/rebase and exact owned push.

## Ownership and limitations

Root initially preserved the active non-isolated normal game. The user approved
save/stop; by the next observation Play had already stopped before root sent any
input. Preserve the latest existing save and preferences before isolated tests;
do not claim root saved unsaved progress. No game-state mutation is required.

This supporting delivery fix takes priority over the Nella material pass because
the observed UI obstructs reading the existing game. Nella's prepared plan and
accepted art remain unchanged. This is original presentation repair, not Qud
parity or new artwork; the established greybox/art pipeline is reused because
the defect is backing geometry rather than a new visual design.

## In-phase review

- 🟡 Actual visual defect; fix before claiming wide inventory popup acceptance.
- 🧪 Native RED/GREEN, GPU/live comparison, resize and shared-popup verification pending.
- 🧪 Physical Deck and new package performance remain unverified; no build planned
  on current low disk headroom.

## Implementation and acceptance — 2026-10-25 04:31UTC

`ZoneRenderer.UpdatePopupOverlayGridLayout` now sets only the centered
background tilemap to a custom horizontal orientation scale matching its grid.
Assignments are guarded; no per-tile iteration, new sprite, font stretch, input
change or gameplay change. Existing caller paints remain authoritative.

Native RED: five cases, two passing unit/narrow controls and three expected
vertical-gap failures. GREEN:93/93,0failed/skipped across the new fixture plus
centered-modal, fullscreen ownership, Examine UI/typography/inventory/queue and
gamepad suites. First attempted run occurred before the new fixture imported
and executed zero cases; it is not counted as RED. The subsequent actual
five-case XML supplies the RED evidence. Independent source review found no
P1/P2; SkillsScreen's compatibility BgTilemap is unused by its Canvas and is
not claimed as a beneficiary.

Ordinary isolated Play: continued the preserved earned seed64 north checkpoint,
opened inventory, selected the equipped long sword, used Enter→a Examine, then
resized the still-open reader from FreeAspect to1280×800 and Escape returned to
the same selected sword. Actual `wide-popup-before.png` and `wide-popup-after.png`
show the same wide dialog with vertical leaks eliminated. The1280×800
`wide-popup-resized-1280.png` retains solid backing and the same content;
`inventory-ledger-before.png` shows the restored selection. The captures are
Unity ScreenCapture PNGs, not concepts. Their pixel dimensions are authoritative:
wide1160×523, resized1280×800. The execute-code tool's Screen.width/height snapshot
reports editor dimensions2321×1139 even after GameView resize, so that field is
not used as rendered-resolution evidence.

**Can verify:** source-only backing scope, bounded native geometry/state cases,
actual GPU gap removal, ordinary keyboard inventory/Examine/resize/back flow.
**Cannot verify:** every popup content combination, physical Steam Deck input,
new package behavior or performance. Current builds predate this correction.

User-save ownership: user approved save/stop but Play was already stopped before
root input. Latest existing Quick/backup files were copied byte-for-byte to
`/tmp/coo-user-save-before-popup-audit/`; no unsaved-progress save is claimed.
Tests use owned NativeSaveIsolation and never intentionally load that user game.

Self-review: 🟡 visible leak fixed and actual rendering checked; 🔵 one shared
background owner, unchanged foreground/portraits/camera/pointer mapping; 🧪 device
and packaged gates remain open. User additionally requested an inventory
redesign; its separate plan/concept are preparation, not shipped UI.

Files: ZoneRenderer.cs; WidePopupBackgroundCoverageTests.cs/.meta; this record;
focused RED/GREEN XML/summaries; actual capture receipts/PNGs; living continuation.
