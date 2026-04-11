# Caves of Qud Message Log Side Menu Implementation

This document records the implementation plan that was used for the Qud-style persistent gameplay sidebar now added to `caves-of-ooo`.

## Summary

- Replace the old bottom message ticker and top look overlay with a single always-on right-hand ASCII sidebar.
- Follow the classic Caves of Qud software shape: keep message/state gathering separate from the renderer.
- Keep the existing popup/modal flow for important announcements, but also mirror those announcements into the sidebar log with flash emphasis.
- Reserve screen space for the sidebar in gameplay view so the world viewport, camera framing, and look-mode hit testing all agree about the occluded right edge.

## Sidebar Contents

The sidebar is intentionally limited to the first-pass “core HUD”:

- `Vitals`
  - HP
  - MP
  - level
  - XP / XP-to-next
  - AV
  - DV
  - carried weight / max weight
  - drams
  - active status effects
- `Focus`
  - active `LookSnapshot` while in look mode
  - fallback to the player’s current cell when look mode is inactive
- `Log`
  - recent messages
  - newest messages at the bottom
  - wrapped in a classic console-style block
  - duplicate adjacent messages coalesced before wrapping
  - first line prefixed with `> ` and continuation lines indented

Out of scope for v1:

- left/right sidebar swapping
- hide/show toggles
- Qud-style alternate sidebar modes
- embedding the sidebar into fullscreen inventory/dialogue/trade screens

## Software Design

### Pure state/model layer

The implementation keeps gameplay-state collection out of `ZoneRenderer`.

- [`Assets/Scripts/Presentation/Rendering/SidebarSnapshot.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SidebarSnapshot.cs)
  - immutable presentation snapshot for the sidebar
- [`Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs)
  - gathers vitals from `InventoryScreenData`
  - gathers active status effects from `StatusEffectsPart`
  - chooses active or fallback focus from `LookSnapshot` / `LookQueryService`
  - reads recent log entries from `MessageLog`
- [`Assets/Scripts/Presentation/Rendering/SidebarTextFormatter.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SidebarTextFormatter.cs)
  - pure text formatting and wrapping helpers
  - formats vitals, focus lines, and wrapped bottom-aligned log text

### Log data source

`MessageLog` remains the authoritative runtime store for gameplay messages.

- [`Assets/Scripts/Gameplay/Events/MessageLog.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Events/MessageLog.cs)
  - gained a non-breaking structured `Entry` type
  - gained `GetRecentEntries(int count)` so the renderer/model layer stops coordinating parallel message/tick lists manually

### Presentation layer

- [`Assets/Scripts/Presentation/Rendering/SidebarRenderer.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SidebarRenderer.cs)
  - draws the sidebar using the narrow CP437 text tile atlas
  - owns section headers, divider line, background fill, colors, and bottom-aligned log rendering
  - caches wrapped log lines by width/height/content shape
- [`Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs)
  - no longer renders the bottom ticker or the old top look overlay
  - now builds a `SidebarSnapshot` and hands it to `GameplaySidebarRenderer`
  - still owns world rendering, cursor outline rendering, popup tilemaps, and camera-facing presentation timing

## UI and Viewport Handling

### Sidebar rendering approach

The sidebar is implemented as a classic ASCII overlay, not a Canvas window.

- It uses the existing narrow-text atlas from `CP437TilesetGenerator`.
- It renders on dedicated sidebar tilemaps above the world tilemap and below popup/modal tilemaps.
- It uses a fixed-width right-hand strip of approximately 34 narrow text columns.

### Shared layout math

- [`Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs)
  - centralizes the sidebar width, world-space reserved width, top/bottom text rows, and gameplay-right boundary
  - allows rendering, camera framing, visible-zone bounds, and mouse hit testing to use the same layout calculation

### Camera integration

- [`Assets/Scripts/Presentation/Cameras/CameraFollow.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Cameras/CameraFollow.cs)
  - gained a reserved sidebar width configuration
  - biases the tracked gameplay framing so the player remains centered in the left gameplay viewport, not under the right sidebar
  - uses the narrowed gameplay viewport when handling look-mode override panning
- [`Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs`](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs)
  - wires the renderer sidebar width/reference zoom into `CameraFollow`

### Input and hit testing

`ZoneRenderer` now treats the sidebar strip as non-gameplay space.

- `ScreenToZoneCell(...)` rejects screen positions that fall inside the reserved right sidebar area.
- `TryGetVisibleZoneBounds(...)` excludes tiles that are under the sidebar strip.
- Existing look-mode cursor clamping then inherits the corrected gameplay viewport automatically.

### Modal/fullscreen UI behavior

The sidebar does not render while gameplay is paused for fullscreen or blocking UI.

- inventory, trade, dialogue, faction, and announcement screens still pause `ZoneRenderer`
- popup tilemaps continue to render above the sidebar/world layers
- `AnnouncementUI` remains a centered blocking modal
- announcement messages still enter `MessageLog`, so they also appear in the sidebar’s recent log once gameplay view resumes

## Visual Behavior

- classic right-edge vertical divider
- section headers for `VITALS`, `FOCUS`, and `LOG`
- dense single-row text layout rather than the old spaced-out ticker
- recent-log aging tint from bright white to darker gray
- short flash/highlight on new announcements via `MessageLog.FlashStamp`

## Tests

The implementation added and updated EditMode coverage:

- [`Assets/Tests/EditMode/Presentation/Rendering/SidebarStateBuilderTests.cs`](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/SidebarStateBuilderTests.cs)
  - vitals formatting
  - status effect line generation
  - fallback focus behavior
  - duplicate-log coalescing and wrapping
- [`Assets/Tests/EditMode/Presentation/Rendering/SidebarRendererTests.cs`](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/SidebarRendererTests.cs)
  - sidebar renders at the right edge
  - focus block reflects look snapshots
  - announcement flash affects the sidebar background
  - screen-to-zone hit testing rejects sidebar coordinates
  - visible bounds exclude sidebar-covered columns
- [`Assets/Tests/EditMode/Presentation/Input/SidebarAnnouncementTests.cs`](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Input/SidebarAnnouncementTests.cs)
  - announcements still open the blocking modal
  - the same announcement is also present in sidebar log data
- updated:
  - [`Assets/Tests/EditMode/Presentation/Rendering/WorldCursorRendererTests.cs`](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/WorldCursorRendererTests.cs)
  - [`Assets/Tests/EditMode/Presentation/Cameras/CameraFollowOverrideTests.cs`](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Cameras/CameraFollowOverrideTests.cs)

## Verification

The final verification pass for this implementation was:

- Unity compile refresh: clean
- targeted sidebar/camera/look-mode EditMode tests: passing
- full EditMode suite: `1023 passed, 0 failed`
