# Hotbar pointer: match the painted card to the native click

Status: corrected and boundedly accepted in native Unity and ordinary mouse play,2026-10-25 12:15UTC.

## Bounded plan and verification sweep

During ordinary seed64 Warden play, the new icons and keyboard activation worked,
but two visible Lunge-card clicks did not change selected Follow Through. Keep
accepted icon artwork and card geometry stable. Correct this input boundary as a
separate change, then return to the opening's whole-scene graphical gaps.

| Premise | Verified source / correction |
| --- | --- |
| Clicks already share keyboard's backend support | InputHandler.TryHandleHotbarInput uses legacy Input.GetMouseButtonDown and Input.mousePosition directly. Keyboard uses InputHelper and native controller routing. |
| Resize icons or transform screen coordinates | GameplayHotbarLayout already derives the hit from the painted Tilemap/camera. Native editor Screen readback can differ from actual Game output; do not compensate using that discrepancy. |
| No existing solution | InventoryUI.LedgerInput reads position and button edge from one enabled Mouse.current, falling back to legacy only when no enabled native device exists. Its actual native correction was verified in inventory play. Reuse that policy. |
| Merge both mouse button sources | Mixing native edges with legacy positions can activate the wrong card. Read one authoritative backend pair. |
| Broader input migration required | Scope is hotbar click recognition only. Leave pending targeting/world mouse routes, keyboard/gamepad, card bounds and skills unchanged. |

Evidence baseline: `Docs/Verification/Live16x24/warden-hotbar-mouse-observation.json`
and prior inventory pointer audit. Baseline editor readback is not a capture of the
button's exact press frame. Need native regression and ordinary mouse confirmation.

Implementation prompt: establish RED on a real Input System MouseState feeding
InputHandler and the real rendered hotbar. Verify hovered/right-click/outside/
held/repress/pending countercases and disabled/no native device legacy fallback.
Then use a private hotbar pointer reader with the inventory-proven backend policy,
run relevant native input/HUD tests, launch an isolated normal game, click the
actual cards, inspect ready/aim/cooldown/cancel, record true captures and review.
Restore normal saves/preferences/view, push exact owned files and document limits.
No new art/imagegen cycle is needed for this existing input defect: accepted native
icons and the actual-game baselines are reused unchanged.


## Implementation and test evidence

Native RED:11cases,10expected failures/1passing keyboard-pending control, before
production change (`hotbar-pointer-native-red.xml`). Real queued native presses
failed the painted-card→full InputHandler.Update route, including both1280×800
and1920×1080. Private pointer reader was deliberately absent until RED.

Implementation: `InputHandler.ReadHotbarPointer(out Vector2,out bool)` snapshots
one enabled native Mouse and reads its position/left-button edge; absent/disabled
native device uses the corresponding legacy pair. The existing normal-state,
keyboard priority, hit-test, selection sync and activation/cooldown policy stay
unchanged. No camera/layout rescaling, polling allocation or broad input migration.

GREEN:107distinct native Unity passes,0fail/skip, including11new pointer cases,
18Warden icon cases, inventory interactions, hotbar save/selection adversarial,
controller hints and renderer/snapshot regressions. Actual hover/right/outside,
held-after-cancel and release/repress countercases prevent vacuous edge handling.
Legacy fallback tests compare the real returned legacy pair; they do not fake or
prove physical legacy-device delivery. Independent fixture and production reviews
found no actionable correctness blocker.

## Ordinary acceptance and honesty bounds

Fresh normal SampleScene/seed64/Warden with no forced presentation or grants.
Actual OS/CUA mouse clicks on the visible cards:

| Action | Observed state |
| --- | --- |
| Click Lunge (display1063,1083) | Selected1, pendingLongBlades_Lunge, tick10/HP40, cyan AIM and line2 summary. |
| Escape, click Follow Through (1157,1083) | Selected2, pendingLongBlades_FollowThrough, same tick/HP, adjacent AIM. |
| Escape, click En Garde (972,1083) | Selected0, actual stance/CD11, tick20/HP40. |
| Repeat En Garde click | Visible cooldown refusal, same tick20/HP40/CD11. |
| F5 | Isolated earned checkpoint saved/backed up. |

Actual1280×800 screenshots and raw receipts use`hotbar-pointer-*`; independent
`hotbar-pointer-gameplay-review.md` accepts the bounded state/visual sequence.
Post-click pointer/Screen readbacks come from an editor callback, NOT the exact
click frame, and must not be used as screen-coordinate calibration evidence.
Can verify: actual click→state transitions, correct icons and refusal, native
edge/counter behavior and preserved normal data. Cannot verify: hardware Deck
comfort/performance, every other world/popup pointer route, physical legacy mouse,
new packaged binaries or whole opening graphical completion.

## Self-review / protected state / continuation

🟡 Fixed: legacy-only hotbar ignored native pointer clicks. Genuine RED and actual
mouse replay now cover the correction; no accepted artwork changed.
🔵 Backend symmetry checked against InventoryUI.LedgerInput; both position and
button come from one enabled device, with a matching fallback pair.
🧪 Other legacy pointer routes are outside this bounded correction; no global
mouse acceptance claim. Physical Deck/package delivery remains pending.

Root saved earned test state at`/tmp/coo-hotbar-pointer-earned`, stopped Play,
restored normal SampleScene/view/preferences/save isolation; console0errors and
2warnings. Normal four save/backup hashes and current Linux archive remain
unchanged. No current build is deleted/rebuilt. Next graphics work: ordinary
Look/Examine of incidental north LeatherArmor6954 at16,17 and kitchen P glyph,
record exact current owner/state, then choose a coherent visible missing-item
slice through the established capture/imagegen/native source loop. Do not invent
an identity from a glyph or modify accepted world art without a named defect.

Storage: initial bounded project check found no worthwhile stale target (0bytes
removed). Separate Unity-only inspection found two obsolete3.20.1Hub installer
ZIPs while3.22.2is installed and newer pending state exists elsewhere. Exact hash,
version/size/inode and no-open-handle proof are saved; cleanup remains conditional
on this complete source/test/evidence commit reaching verified origin/main.

Files: `InputHandler.cs`; new`HotbarPointerInputTests.cs/.meta`; this living doc,
shared graphics continuation/coverage records and `hotbar-pointer-*` evidence.
CoO-original integration fix; no Qud art/code or new image assets used.
