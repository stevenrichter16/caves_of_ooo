# Fullscreen trade and faction canvas leak — bounded release polish

Status: implemented in the isolated verification copy after R2 (4aab2aac); R115 focused, R116 native and R117 full acceptance passed; publishing to local main. The investigation below records the pre-fix evidence. The R111 native trade screenshot already demonstrates the player-visible defect. This is a new polish gate, not evidence to amend earlier R2 receipts.

## Observed root cause

The native image `Docs/Verification/ChunkGameplayImplementation/R111-regional-outcomes-native/artifacts/CGN-ab459547131d466f8baf556ee9398bf7-regional-trade-stock.png` contains dim world glyphs in unused lower trade rows. The world is 25 rows; the fullscreen page is 45. That explains why the upper portion looks clean while the lower portion leaks.

1. `GameBootstrap.cs:581` assigns `TradeUI.Tilemap = ZoneRenderer.PopupFgTilemap`; `:588` does the same for faction standings. These are the only two wrongly wired immediate fullscreen siblings. Inventory (`:515`) and QuestLog (`:525`) use `ZoneRenderer.GetComponent<Tilemap>()`.
2. `InputHandler.OpenTrade` (`:3786–3794`) pauses the world and switches the camera to the 80×45 UI view. It does not clear/acquire the main canvas. `OpenFaction` (`:3521–3529`) has the same ownership premise.
3. `TradeUI.Render` (`:423–485`) clears its assigned map and writes only text/dividers. Empty rows are transparent. `FactionUI.Render` (`:105+`) does the same. PopupFg sorts at 21 above world (ZoneRenderer `:528–541`); it has no opaque full-page background.
4. `ZoneRenderer.LateUpdate` paused branch (`:808–876`) deliberately clears auxiliary render layers/backgrounds, but retains the main map because fullscreen UIs are expected to have replaced it. The old main-map world remains visible through the sparse popup page.

Centered popup consumers are correctly separate: pickup, container picker, world action menu, dialogue, announcement, pause, skills and ability manager use CenteredPopupFg/Bg plus popup camera. They must remain unchanged. The additional `SetUIView` at InputHandler:4048 merely restores the inventory framing after an announcement.

## Important second source hypothesis, tested separately

Changing only two bootstrap assignments may not fully solve empty-row leakage on an actual sprite-rendered world. On the first paused LateUpdate, `ZoneRenderer.cs:829` calls `EnvironmentSpriteRenderer.ReleaseAllClaims()`. `RestoreMainClaim` (`EnvironmentSpriteRenderer.cs:1269–1279`) restores the remembered old glyph whenever the main cell is null. A correctly main-bound UI just cleared its empty rows, so its null cells can be refilled with world glyphs after its Render method returned.

R113 subsequently confirmed this source hypothesis as three independent failures. The initial three first-paused-frame cases deliberately bind to main before opening and establish a real Resources-loaded grass sprite claim. They distinguish this hypothesis from the original popup-map wiring error. QuestLog is included as the same-owner control; an apparently clean one-shot screenshot is not evidence that every sprite-claim state is safe.

## Minimal proposed repair, after actual RED

- Correct the two fullscreen assignments in GameBootstrap to main.
- In `OpenTrade` and `OpenFaction`, acquire the actual renderer's main Tilemap before opening. This is two guarded field assignments, so the real input path is authoritative even if an existing component retains the old dedicated map. The two staged entry cases initialize exactly the old bootstrap configuration and require that the real open path consumes the main canvas. They do **not** claim to invoke the monolithic GameBootstrap.DoStart; that source wiring is a separate two-line audit/native assertion.
- If the first-paused-frame cases fail as source inspection predicts: in the paused transition use the existing `NotifyMainTilemapCleared()` invalidation path instead of `ReleaseAllClaims()`. All four shipped assignments of `ZoneRenderer.Paused = true` are the fullscreen openings (InputHandler 3447, 3457, 3528, 3789), whose Render clears their main canvas. `NotifyMainTilemapCleared` already clears overlay tiles and invalidates displaced snapshots without resurrecting stale glyphs; the same API is used by `ZoneRenderer.RenderZone` after a full map clear. The immediately following existing background clear makes discarding bg claim snapshots appropriate here. Retain ordinary ReleaseAllClaims semantics unchanged.
- Retain UI.Close, camera RestoreGameView, Paused=false and MarkDirty, which already redraw the native world. No owner removal, inventory mutation, gameplay camera setting change, mass black-tile fill, generic popup redesign or new material is needed.

The defensive open-path assignment is the testable production seam proposed here. If bootstrap-only wiring is preferred instead, extract only the existing UI map-assignment block into a small production helper called by DoStart and test that helper's output; do not simply edit a hand-copied fixture binding and claim the integration was covered.

## Initial staged files and nine cases (expanded to ten after review)

- `/tmp/coo-trade-opaque/FullscreenUiCanvasOwnershipTests.cs.pending`
- `/tmp/coo-trade-opaque/FullscreenUiCanvasOwnershipTests.cs.meta.pending`

Suggested destination: `Assets/Tests/EditMode/Presentation/Input/FullscreenUiCanvasOwnershipTests.cs` and `.meta`, only after the current frozen runner ends.

Cases:
- 2: actual OpenTrade/OpenFaction remove a seeded world glyph from an unused page row despite the legacy PopupFg assignment; visible main-map header required, old popup canvas empty.
- 3: correctly main-bound Trade/Faction/QuestLog remain clean after the first paused LateUpdate despite a real displaced sprite claim. Claim and immediate pre-pause blank-row preconditions prevent vacuous passes.
- 2: close redraw restores terrain and exact camera position/rotation/zoom/viewport; native zone owners, player position, trader stock, currencies and inventory remain unchanged; UI-only rows are cleared.
- 1: disabling ordinary sprite rendering still restores the displaced glyph outside fullscreen UI (counter to replacing global claim restoration).
- 1: a centered-popup camera continues showing the underlying world and uses its separate layer without pausing or changing gameplay framing.

The fixture owns only its objects and real minimal native entities. It uses the existing private InputHandler/LateUpdate reflection seam already used by CenteredModalUIViewTests. It does not mutate PlayerPrefs, registry contents, save paths or gameplay world data. Source verification confirmed all invoked methods and tilemap names; actual compilation and execution are recorded below.

## Native acceptance proposal

Reuse the existing isolated R111 journey/harness rather than author a new bootstrap. After buying/inspecting delivered stock, capture the trade screen after one settled frame with intentionally sparse stock and empty rows, then close and capture the same world. Also open the immediate sibling Faction screen once and close it. Compare player/zone/camera state and native inventories before/after; visually inspect normal text and every empty lower row. Existing centered popup tests and a dialogue→trade→world round trip should remain green. This is not a new performance claim or an expansion of the previous benchmark.

## R113 confirmed RED and implementation

The isolated R113 run compiled without errors: 9 cases, 4 controls passed, 5 failed. Both wrong entry bindings and all three first-paused-frame stale-claim cases failed exactly on the old terrain glyph. Production now corrects the two bootstrap bindings, acquires main at the two actual input openings, and uses the existing invalidation path at fullscreen pause. Ordinary release/restoration remains unchanged. No gameplay state or save format changes.

## Scope and performance

CoO-original presentation correction; no Qud parity claim. No new artwork, content or UI architecture. The fix changes one existing transition-only cleanup call and two UI-open assignments; no new per-frame allocation, scans or caches. Existing native scenario records cover entry/exit and state preservation; no performance improvement claim. Native image review and focused renderer/modal regressions remain required.

## Focused GREEN and independent review

R114: 98/98, zero compiler errors. Independent review confirmed all four fullscreen openings own the main canvas; added Inventory to the paused-frame matrix after the review. R115: 107/107, zero compiler errors, including ten new canvas cases, existing centered modal tests, environment sprite tests, animated environment tests and native-audit tests. The ten new cases preserve ordinary sprite disable and centered-popup behavior as controls. The staged legacy-popup case begins with no popup text; native dialogue-to-trade inspection separately verifies real popup cleanup.

R116 native acceptance passed 65/65 checks, 916 queued movement/input steps, 21 captures, no compiler errors or unhandled exceptions, frozen source and successful private-save/settings cleanup. Its three new checks are: clean trade rows, clean faction spacer rows, and exact world/camera/owner/inventory/currency restoration. It captures the two screens and restored world at native 1920x1080. Root inspected native Trade, Faction and restored-world captures: no terrain leakage in blank rows, item/faction text visible, coarse voxel world restored. This verifies those concrete views; it does not certify every menu, subjective accessibility, balance, or performance. The ordinary opening remained vulnerable and finished at 35HP; only the later regional leg used explicit F12 protection.

## Four-question cold-eye and adversarial gate

- Symmetry: all four fullscreen entry sites paint main; all four paused overlays are invalidated once. Centered windows still have their own layer/camera and do not pause the world.
- Cross-feature consistency: Trade/Faction now match Inventory/Journal. The defensive two input assignments handle previously configured components, while bootstrap is independently corrected and exercised by native N.
- Counterchecks: genuine sprite ownership preconditions precede the pause checks; ordinary disable still restores glyphs; UI close restores exact camera and native owners/stock; centered popup keeps the world. Existing renderer/modal regressions cover incremental claims and dialogue transitions.
- Doc consistency: corrected the renderer API comment that formerly told callers to restore claims after another UI already owned main. No new save fields, resources, gameplay actions or per-frame allocation.

🟡 Fixed: both wrong canvas bindings and stale snapshot restoration (actual R113 failures).
🔵 Covered: fourth fullscreen sibling Inventory added after independent review; current tests total ten.
🧪 Bounded: native visual acceptance covers the photographed sparse trade stock and faction view, not arbitrary modded fullscreen UI owners.
⚪ Deferred: general text contrast/accessibility review remains a release gate; this patch makes no readability certification.

## Files changed

`GameBootstrap`, `InputHandler`, `ZoneRenderer`, and `EnvironmentSpriteRenderer` (last is API comments only); one new ten-case test fixture and GUID-only copied meta; native scenario/batch/runner acceptance; this living document and compact verification artifacts. Full manifest records nine source/test/tool paths, pre-patch and verified-candidate hashes. GUID audit: 6,773 unique clone metadata GUIDs, zero collisions.

## R117 full closeout

14,941/14,941 passed, zero failures/skips/inconclusive/compiler errors; 341.92 seconds. Independent final review verified all nine candidate hashes against the clone and baseline 4aab2aac; no material findings. Exact source and evidence published with this doc. User editor stayed open; no remote push or release certification.
