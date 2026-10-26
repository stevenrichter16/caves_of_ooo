# World popup raster coverage

Status: native GPU and ordinary Play verification complete for this bounded popup fix.

## Player-visible defect and scope

Actual seed65 Mendleaf Examine and Nella action-menu screenshots show narrow
green lines through nominally opaque panel backgrounds. Inventory's full-screen
reader is clean in the observed captures. Keep accepted fonts, borders, portraits,
sprites, camera framing, layout and controls. Fix only backing coverage.

Evidence: `Verification/Live16x24/seed65-composition-north-mendleaf-reader.png`,
`seed65-composition-nella-young-control.png`. This is CoO-original rendering work,
not an assertion of Qud source parity. No new bitmap art is proposed, so this
does not call for replacing approved pixels or generating a new art concept.

## Pre-implementation verification sweep

| Premise | Verified correction/evidence |
| --- | --- |
| World and inventory readers use different background materials | They share the centered popup backing path; their camera viewport/aspect differs. |
| Existing coverage tests prove no screen cracks | `WidePopupBackgroundCoverageTests` checks occupied tiles, opaque sprite pixels and CPU quad bounds. It explicitly does not prove GPU coverage. |
| The grid is missing whole columns | Leaks at x211,305,399,446,493,587,634,681 have pixel centers exactly on integer popup-cell boundaries at 940/80 = 11.75 pixels per column. |
| The current updater overlaps backing quads | `ZoneRenderer.UpdatePopupOverlayGridLayout` scales backing to exactly cellWidth × 1; adjacent quads only meet. Foreground orientation remains identity. |
| GPU readback needs a new test framework | Reuse `FellingCameraClearTests`' bounded RenderTexture/readback cleanup. Its standalone request is not suitable for this overlay camera. |
| Render the popup Overlay camera with SingleCameraRequest | Installed URP skips non-Base cameras on that path. Use `RenderPipeline.StandardRequest` on an owned Base camera with the actual overlay stack. |
| Raster cracks are fully proven as the root cause | They are the leading inference from image coordinates and source. Confirm with native RED before changing production. |

## Execution prompt

1. Re-check disk space, git ownership and saved editor state. Keep the native
   runner isolated through delayed Play resumption; root alone owns Unity.
2. Extend or accompany `WidePopupBackgroundCoverageTests` with an actual GPU
   backing test using a contrasting world field. Match the ordinary 1280×800
   cropped map viewport (940 pixels wide), not just a full-screen camera.
   Use `RenderPipeline.StandardRequest` on the owned Base camera and real overlay
   stack; do not render the Overlay camera standalone. Paint through real
   AnnouncementUI and WorldActionMenuUI bindings. Read interior
   pixels away from text/borders and require opaque backing, including the
   fractional-cell boundaries observed in the screenshot.
3. Run and record native RED on current code. If it does not reproduce, inspect
   real viewport, render order and material before proceeding; do not weaken the
   assertion or invent a different failing contract.
4. Minimum candidate: conservative backing-only overlap in the existing layout
   updater, derived from pixel/world scale or a bounded precision margin. Keep
   glyph matrices, cell spacing, alignment and camera projection unchanged.
   Compare a single continuous backing only if the smaller correction cannot
   satisfy raster coverage without visible edge growth.
5. Counter-checks: disable only the backing renderer and require the sentinel
   world pixels to appear, then restore it and require coverage. Include
   full-screen integer-width view; fractional and wide resize
   while open; unchanged glyph geometry/content; closed popup exposes the world;
   separate back-to-back popups do not retain fill; no contamination of inventory
   or surrounding map. Require actual GPU RED/GREEN plus existing CPU contracts.
6. Independent code review before commit. No per-frame allocations/resource
   creation or growing caches; preserve the existing layout-update guards.
7. Ordinary isolated Continue: open the real world Examine and action menu at
   1280×800; capture both and compare the previously leaking columns. Also inspect
   inventory reader and close/reopen. Save the earned checkpoint **before** stop:
   `NativeSaveIsolation` deletes its disposable root automatically on Play exit.
8. Restore editor/save preferences, verify protected hashes, update this living
   plan and coverage ledger in the same commit, then fetch/rebase/push exact owned
   changes. Do not claim opening, packaged build or physical Deck acceptance.

## Historical pre-implementation limits

No runtime code has changed. Native execution is pending because the latest
recovery left only about221.5MiB and this session already encountered ENOSPC.
Source/image inspection is useful preparation, not a substitute for the GPU
test or actual gameplay captures. After this narrow defect, resume second-seed
return/growth and the northern worksite composition cycle.


## Implementation continuation — 2026-10-26

Native reproduction at the actual 940×680 map viewport found 1,458 interior
world pixels leaking through the reader; the wider 1171-pixel viewport leaked
172. Full-screen integer spacing was already clean. See
`Verification/Live16x24/popup-raster-exact-viewport-red-summary.json`.

The first overlap candidate passed 43 tests, but independent review identified
translucent DialogueUI (alpha .92). A new actual GPU counter-check measured
.2196 red-channel variation across its interior: double blending. Rejected.
A shared-transform integer backing grid also failed; reverted completely.

The accepted native candidate changes only the UI SolidBlock texture: its
16×16 opaque pixels now have a one-pixel opaque sampling gutter in an 18×18
texture. Quad geometry, pixel count, foreground glyphs and grid layout remain
unchanged. Fractional UV edges no longer sample adjacent transparent glyphs.
No overlap, new updater or per-frame allocation. One small texture is made per
UI bank generation, following the bank's existing cache lifetime.

Native `popup-raster-padded-green` passes all 44 selected popup/input tests,
including six new GPU cases. Opaque, translucent, resize, disabled-backing,
closed-menu and fullscreen-inventory counter-checks pass. The initial tilebank
filter used a wrong namespace and selected no tilebank cases; its corrected
separate run is required before commit. Earlier trial results remain evidence,
not acceptance. Source changes: CP437TilesetGenerator and
WidePopupBackgroundCoverageTests; ZoneRenderer is unchanged from main.

Self-review: 🟡 overlap transparency regression fixed by sampling gutter;
🔵 shared-grid complexity rejected; 🧪 ordinary Play screenshots, packaged run
and physical Deck checks must not be inferred from GPU tests. The current
build is preserved and predates this fix. No gameplay or approved art changed.

Storage: the user's separately authorized OpenClaw error-log truncation and
XCTest simulator-data deletion recovered about428GiB; current available space
is about432GiB. All further cleanup remains inspection-only. No project source,
current build, normal save or active Unity cache was deleted by this graphics
pass. Next: ordinary earned north replay, Examine/action-menu and inventory
captures, then second-seed return/growth and north-yard composition.


## Acceptance — final padded fill

Corrected tilebank namespace selected five cases: 5/5 passed, alongside 44/44
popup/input cases (49 total, no skips). Original 16×16 fill ink matches the game
bank; all other glyphs retain their atlas. Independent review of the original
candidate supplied the transparency finding; the final gutter fix was then
self-reviewed against that finding and the GPU/shape counter-checks.

Ordinary isolated Continue of earned seed64 north save: Look selected actual
Mendleaf at (15,19), Return opened its action menu, X opened Examine. Real
1280×800 captures `popup-raster-padded-menu.png` and
`popup-raster-padded-reader.png` show continuous opaque backgrounds. Inventory
and long-sword F1 details also open, close and reopen through actual keyboard
input; see `popup-raster-padded-inventory.png`, `-inventory-selection.png` and
`-inventory-reader.png`. `-pause.png` truthfully records an extra Escape reaching
Pause, not inventory. No teleport, item grants, forced daylight or gameplay
state changes were used. The existing earned checkpoint remains available.

Can verify: actual keyboard routing, painted glyphs/backing, normal flat mode,
GPU opacity and no double-blended translucent edges in the tested projections.
Cannot verify: physical Deck/controller, updated packaged build, whole-world
art acceptance or every future viewport. The native uniform-alpha test models
the dialogue backing; this pass did not talk to a live NPC. These are bounded
rendering acceptance claims. Next scene work remains north composition and
second-seed return/growth, not more detached assets.
