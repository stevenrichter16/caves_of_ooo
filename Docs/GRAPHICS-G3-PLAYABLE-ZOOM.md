# Playable native-pixel zoom

Status: runtime/native and bounded actual Unity acceptance complete, 2026-10-26.
Standalone Mac travel/Continue and Linux archive acceptance also complete.
[Package evidence](Verification/Live16x24/packaged-playable-zoom-report.md);
wider six-zone scene review and physical Deck remain open.

The whole-opening framing review found a delivery defect: at the 1280×800 Deck
layout, ordinary controller zoom cannot reach 2× pixels. Keyboard has no world
zoom binding. Keep the accepted sprites and default framing; make the existing
close view reachable through real controls before diagnosing scene composition.

## Verified premises and corrections

| Premise | Source/evidence | Correction |
| --- | --- | --- |
| The player can compare 1×/2× already | InputHandler.Controller.ControllerZoom clamps multiplier to .5–1.5; CameraFollow floors native scale | At680px map height,34rows×1.5 world row height and16PPU, even .5 gives1.667→1×. Repeated inputs cannot reach2×. |
| Keyboard +/- can zoom | TryHandleSidebarLogScrollInput | Those keys scroll logs; preserve them. PageUp/PageDown are already mapped by InputHelper and unused in Normal world. |
| Lowering multiplier is enough | CameraFollow applies it to flat and legacy/authored scenes | Use a flat-only explicit integer scale,0automatic(default),1–4 selected. Preserve the legacy multiplier and its old clamp. |
| Old 2× captures prove ordinary reachability | Historical controlled framing was an audit setup | They remain visual references, not acceptance for real zoom controls. |
| New sprites would fix the tiny view | Actual current-package cellar and bank captures | No owner/state mismatch found. This is a camera/input repair; no new art or imagegen substitute is needed. |

## Implementation plan

1. Add failing tests at existing controller boundary plus keyboard dispatch;
   confirm failure at Deck viewport before runtime edits.
2. Camera owns one zoom-step helper. In projected native mode, step the rendered
   integer scale1–4 and keep pixel-edge alignment; outside it retain continuous
   .85/reciprocal and .5–1.5 limits. Reject fullscreen/paused camera changes.
3. Keyboard PageUp/PageDown acts only after modal gates in Normal world; same
   helper as LT+D-pad. No turn, move, hotbar or world mutation. Document bindings.
4. Pair tests with legacy/default/modal/no-key/held-key controls; exercise resize,
   fullscreen roundtrip, scale bounds and mode restoration. Independent review.
5. Isolated actual Unity fresh/earned play: compare same position/tick1×/2×,
   interact/Look, inventory roundtrip and travel. Preserve all normal saves.
6. Refresh dated current packages only after runtime acceptance; ordinary Mac
   controls prove delivery. Physical Deck remains a separate gate.

Qud reference: player's requested native-pixel readability and existing
LT+D-pad controller convention. No new online/source parity claim. This changes
camera access, not sprite style, tactical visibility rules or simulation.
Selected zoom is session camera state; no new save schema or persistent setting.
Default automatic framing is unchanged until the player uses a zoom command.

## Acceptance limits

Native input/camera tests verify math, ownership and dispatch. Actual screenshots
verify visible framing; neither proves controller feel or physical Deck speed.
Do not conflate release of this repair with complete six-zone scene acceptance.
Storage remains inspection-only. Keep current builds and all editable artwork.

## Implementation and verification

CameraFollow.StepGameplayZoom steps the actual projected integer pixel scale.
Zero retains automatic framing until used; selected scales survive ordinary
fullscreen UI and zone/resize changes. Existing authored/legacy cameras retain
.85 multiplier behavior. Keyboard PageUp/PageDown and LT+D-pad share the helper;
modal/Look gates retain their input and +/- retain sidebar log scrolling.

RED:18cases discovered,11expected failures/7passing counters before runtime
edits. One earlier unimported discovery returned0tests and is retained only as
a diagnostic. Review found automatic scale above4could make Zoom In zoom out;
its added regression failed22/23then passed after preserving direction.
Final native zoom/input suite **23/23passed**. Real InputSystem keyboard and
virtual gamepad events cover dispatch, key edges, triggers, held keys, no-trigger,
resize/zone/legacy/fullscreen roundtrips and move/hotbar isolation.

Broader regression **230/233passed**. Three WorldLogHistoryTests fail while
establishing the initial pointer hit row (expected0,actual-1). Temporarily
restoring all4runtimefiles toHEAD and recompiling reproduces the exact same
three failures (11/14history cases pass). The unchanged-code baseline receipt
is preserved; these failures are not claimed green or fixed by this work.
All23zoom cases were rerun after restoring the final implementation.

## Actual gameplay and review

Fresh current e490Mac New Game/5Warden creates seed224401440; exact earned
checkpoint preserved in Fixtures/playable-zoom-fresh-opening. Old package
PageUp capture shows no response. The unchanged checkpoint was loaded through
normal Continue in isolated Unity Play. Ordinary keyboard input gives1×→2×→1×,
I inventory and Escape returns2×; L/Up2/Right selects the visible working notice
at41,10. Enter opens its action menu, Enter opens the matching native portrait
reader. No world/inventory mutation or turn occurs through these operations.
All2690ownerrecords and five inventoryrecords match across the zoom pair.
Ordinary movement then advances time and camera follows at2×.

Actual PNGs03–15 are1280×800Game viewport captures. JSON Screen fields reflect
the2321×1139Editorhost, whilecamera pixel rect andPNG identify viewport size.
Capture12is the pause menu after an extraEscape, not movement. Captures13–15
are ruin approaches, not the labeled anticipated stair arrival: current walls
blocked the assumed route. No zone crossing is claimed here. Fullsix-zone
play and packaged parity remain explicit next gates.

Independent code/test/paired-image review found no blocking issue. Accepted
art was not changed; no imagegen/asset packet was warranted by this camera bug.
Normal editor scene/view/seed/prefs/save-root restored; test saves retained
outside the repo.776normal saves,374approved art/source and2unrelated images
remain hash-identical.30generated whitespace-only sprite metas backed up and
restored. Storage inspection only, about425GiBfree; nothing reclaimed.

### Self-review and bounds

- 🟡 Fixed visible2×unreachability and above4direction reversal before commit.
- 🔵 Default framing and legacy zoom remain unchanged; selected native zoom is
  session state, not a new persistent preference or save-schema requirement.
- 🧪 Existing3log pointer-test setup failures are reproduced on unchanged code;
  separate from camera acceptance. No physical Deck/performance claim.
- 🧪 Remaining route composition, selection feel on hardware and wider world
  coverage are open. Screenshots prove this bounded playable view, not release.

Can verify: test math/input ownership, actual ordinary keys, visible aligned
notice cursor/reader, inventory return, unchanged simulation under zoom and
camera tracking under movement. Cannot verify: physical Deck controls/speed,
Linux execution, entire six-zone journey or subjective long-play comfort.

Changed runtime: CameraFollow.cs, InputHandler.cs, InputHandler.Controller.cs,
ControlsReference.cs. New FlatPixelZoomInputTests.cs/.meta. Receipts, actual
captures, earned fixture, this record and both living plans/coverage ledger are
versioned with the change. The test helper is the measurable scenario; ordinary
play capture is used for the visual gate, without a new production debug menu.


## Packaged follow-up — 2026-10-26

Both current packages now freeze47d678995. Twelve actual Mac captures verify
zoom levels, inventory, surface/cellar travel and quit/Continue. This closes
the packaging and zone-transition gates left open above. The same new session
returns to default automatic framing by design. Independent review and archive
verification passed; see the package record for restoration and exact limits.
