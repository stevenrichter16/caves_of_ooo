# Live 16×24 — current-state examine portraits

**Status: IMPLEMENTED; native integration629/629 GREEN, followed by254/254 affected-reader regression after the F1 correction. Actual keyboard Play accepted ground/carried tonic, lit torch, F1 pagination and return.** Source audit and implementation, 2026-10-23. Earlier actual Play exposed a legacy 3D torch portrait and a separate text-only inventory F1 route; both now have corrected native captures. [Live art direction](LIVE-16X24-ART-DIRECTION.md) owns overall implementation and release status.

## Goal and limits

While the current world uses flat 16×24 presentation, an authorized examine popup should show its exact subject's current native sprite. This includes genuinely carried/equipped items inspected over inventory. Preserve the earlier 3D portrait route when flat mode is inactive. Borrow existing art; do not make a new atlas, copy textures, allocate a preview camera/model, simulate an item or insert it into the world.

This is original CoO presentation work following the user's requested flat style. No decompiled Qud implementation was consulted. It adds no item mechanics, disclosure authority, new sprites or gameplay state.

## Verification sweep and corrections

| Premise | Verified correction |
| --- | --- |
| A flat world automatically produces flat portraits. | [ExaminePortraitRenderer](../Assets/Scripts/Presentation/Rendering/ExaminePortraitRenderer.cs) tries `SpawnRing3DLibrary` before its sprite fallback. It has no active flat-mode dependency. |
| A native sprite can replace the image without UI changes. | [AnnouncementUI](../Assets/Scripts/Presentation/UI/AnnouncementUI.cs) draws every portrait into a square. A 16×24 subject needs its actual source aspect and preferably integer pixel scale. |
| A document is permanent disclosure permission. | [ExamineDocumentBuilder.IsCurrentSubject](../Assets/Scripts/Gameplay/Entities/ExamineDocumentBuilder.cs) revalidates the captured cell/viewer or actual carried ownership. Keep this gate; do not rerun description providers to get art. |
| Inspecting a cell means inspecting its top owner. | The explicit reader may select a lower visible occupant. Resolve the captured subject, not `FlatPixelGreyboxCatalog.TryResolve(cell).Owner`, which might select someone else. |
| Inventory pause disables flat portrait authority. | [ZoneRenderer.FlatPixel](../Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixel.cs) deliberately retains `FlatPixelModeActive` while fullscreen UI owns square canvas geometry. Do not gate portraits on `!Paused`. |
| A carried owner needs a temporary world location. | `BuildInventory` verifies actual inventory/equipment membership and no spatial placement. Use the viewer's validated current presentation zone without moving the item. |
| No native frame means the old 3D model is a safe fallback. | In active flat mode, missing/unknown art must remain a flat/glyph fallback. Otherwise malformed optional resources silently restore the discrepancy. |
| `DropModel` releases the whole preview. | It removes only the current model/materials. The camera, render target and fog texture survive until `Clear`; release them when switching from 3D to flat. |
| Adding the two-argument document overload is source-compatible with every null call. | The legacy `ExaminePortraitTests` call `GetPortrait(null, null)` becomes ambiguous against `(Zone, Entity)`. Preserve that countercheck with an explicit `(Zone)null` cast when the overload lands. |
| The draw callback can validate art by resolving it again. | Keep art resolution on explicit refresh. Before repaint, validate only captured subject/current graph and whether flat mode still matches the cached image; clear stale images without invoking description providers or captures. |

Existing owner-local state selectors in [FlatPixelOpeningArtCatalog](../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs) already distinguish torch flame, campfire, repair, finite pool, crop and door state. Share these selectors through the world catalog rather than duplicate the rules in the UI.

## Proposed API and implementation sequence

Names below are proposed; keep contracts if implementation naming changes.

1. Add `ExaminePortraitRenderer.GetPortrait(AnnouncementDocument, ZoneRenderer)` while retaining existing overloads and constructor seams. The new overload first uses `IsCurrentSubject`; when flat mode handles the request, it must return before loading any 3D library. The old overload retains legacy tests/behavior.
2. Add `ZoneRenderer.TryGetFlatExaminePortrait(document, out portrait)`. Its return value means **flat mode handled the request**, not “a texture exists.” When flat mode owns the current presentation, invalid/unsupported subjects cannot fall through to 3D. Require exact current-zone identity for world documents. For carried documents, reuse the existing grounded-viewer and manager/current-graph checks in `CurrentPresentationZone`; do not weaken `BuildInventory` authority. A stale/foreign context returns handled with no image.
3. Add a narrow exact-subject foreground query through `EnvironmentSpriteRenderer` and its existing `FlatPixelGreyboxCatalog`. Factor shared owner-to-foreground resolution out of the cell method, preserving owned-part guards, independent optional groups and honest procedural fallback. Do not scan the zone, create a second catalog per popup, or change tile claims. World subjects use captured coordinates for stable positional variants; portable state frames are position-independent.
4. Return `ExaminePortrait(sprite.texture, normalizedSpriteRect, catalogTint, false)`. Native art uses white tint; procedural art uses its existing tint. These texture/frame references remain catalog-owned. `Clear`/`Dispose` must not destroy them. On entry from a previous 3D portrait, release the old preview stage/target/materials first.
5. Bind a `WorldPresentation` reference on `AnnouncementUI` in the existing announcement setup in [GameBootstrap](../Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs). Use it for the document overload. Do not discover a scene renderer on each popup or alter `AnnouncementDocument` with presentation settings.
6. Extract a pure layout helper, provisionally `AnnouncementUI.MeasurePortraitRect(Rect slot, ExaminePortrait portrait)`. For a sprite, derive source width/height from `Uv × Texture` dimensions, preserve aspect, fit an integer scale when the slot can hold one source pixel per output pixel, and center on integer screen coordinates. Handle an undersized slot by aspect-preserving containment. Preserve the previous centered-square model layout. Call this helper from `OnGUI`.
7. Before drawing a cached portrait, cheaply recheck current-subject authority and clear/hide an invalid result. Do not capture, allocate art, refresh prose or simulate state during `OnGUI`. Normal popup simulation is stopped; explicit reopen/refresh reads current state.

The explicit requested subject may be an authorized non-top occupant or a visible footprint cell. Do not copy the world renderer's top-owner restriction into document authority. Conversely, a familiar blueprint or old document must not authorize a subject in another graph, fog, another inventory or an empty stack.

## Test and acceptance stages

| Stage | Positive behavior | Required counterchecks |
| --- | --- | --- |
| Native RED | Import staged tests only after motion relinquishes the shared editor. Run against the missing API before production edits. | Report actual failing assertions/cases; reflection bridges make missing APIs runtime assertions, not a false compile pass. |
| Exact world subject | Lit/unlit torch, current wet/frozen suppression, repaired well, finite basin and lower selected occupant use the same native frame as the catalog. | Foreign/missing state part, moved/removed/hidden owner, fog/unexplored/replaced cell, viewer departure and same-address replacement graph cannot reuse an old portrait. |
| Carried/equipped | Actual inventory/equipment document borrows the torch's current frame while inventory keeps world mode paused. | No placement/inventory/world-count mutation; lost/transfer/hidden/empty-stack subject rejects. A detached/foreign viewer cannot borrow current flat-world authority. |
| Fallback/lifetime | Missing one optional atlas preserves other groups; unknown subject remains visible through honest flat fallback. Repeated opens reuse borrowed resources. | Never instantiate/capture 3D in flat mode. Clear/dispose preserve shared atlas and peer world frames. A previous 3D preview is fully released before flat output. |
| Mode/UI | Inactive mode retains existing exact 3D portrait behavior. Flat image keeps 16:24 aspect and readable integer pixels. | Flat→3D→flat twice, fullscreen inventory and return, 1280×800/1920×1080/odd viewport layout, no queued-popover stale image. |
| Actual Play | Examine the visible ground torch, carry/equip it legitimately, inspect over inventory, close and return to the unchanged world. | Capture current state and UI at 1280×800; verify cursor/keyboard/controller return and no old model. Native assertions alone do not establish visual quality. |

Reuse `ExaminePortraitTests`, `ExamineInventoryPortraitTests`, `ExaminePortraitAuthorizationTests`, ledger inventory/queue/gamepad/typography tests and fullscreen ownership controls. Add focused new tests rather than repeat unrelated full suites. Draft test source is staged outside `Assets` at `/tmp/coo-flat-examine-draft/`; it must be reviewed/imported and receive fresh native RED evidence before production work. Temporary drafts are not permanent repository verification artifacts.

## Review and implementation log

- 🟡 Implemented the missing active-mode routing and square image correction after native RED; GREEN and visual acceptance are recorded separately below.
- 🟡 Explicit lower-occupant selection, inventory pause and previous-preview cleanup are required cross-feature counters.
- 🧪 Real keyboard/controller flow and rendered appearance remain pending; final native regression is recorded below.
- Files planned: portrait renderer, announcement UI/bootstrap binding, narrow ZoneRenderer/EnvironmentSpriteRenderer/catalog query, new tests/metas and this document. No gameplay/data changes expected.
- 2026-10-23: initial source audit and draft staging preceded implementation. The checkpoints below distinguish preparation, native RED, production changes and outstanding acceptance.

Update this document and the root-owned live art direction in the same implementation commit. Coordinate the editor before import/refresh/tests/Play. Preserve previous presentation and borrowed resource ownership.

### Prepared implementation checkpoint

2026-10-23: the draft was rechecked against the settled motion API and expanded to42 portrait cases and10 layout cases outside `Assets`. Added managed same-address graph replacement, absent inventory viewer, footprint-cell selection, owned state-part fallback, explicit popup binding/cache invalidation and tight fractional-slot containment. No native RED or production change is claimed by this checkpoint. The shared editor remains with motion/item validation; import follows its agreed window.

The catalog seam is `TryGetSubjectSprite(Entity, int, int, out Sprite, out Color)`: use the same owner-local native selectors and procedural fallback, with owned-visible-render and disposal guards, without requiring world placement. Document/graph authorization remains above this selector. Ground/Road examine subjects use their corresponding solid native tile and ground tint rather than another cell occupant. The root owns this small catalog change; the projection agent owns the environment/zone forwarding methods.

### Implementation after authoritative native RED

Native Unity receipt `Docs/Verification/Live16x24/opening-items-examine-red.xml`, 2026-10-23 06:44:48–06:44:57 UTC: combined98 cases,44 passed,54 failed. All42 new portrait cases and10 layout cases failed at their intended missing-API assertions. The other two failures belong to the separate item slice. A draft NUnit constraint typo was corrected before this run; that compile failure is not behavior RED.

Implemented after this receipt:

- `ExaminePortraitRenderer.GetPortrait(document, presentation)` validates the actual document and current graph before selecting art. The active flat presentation returns before any legacy model lookup. Entering from 3D calls `Clear`, releasing the stage, camera, render target and material copies. Borrowed native textures are never disposed by the portrait.
- `IsCurrentPresentationSubject` shares cheap authority checks with the zone accessor and cached popup. World documents require the exact currently presented graph; managed zones require the same cached graph object. Carried subjects retain inventory/equipment authority and additionally require their viewer's grounded current presentation zone. Fullscreen pause does not revoke the mode.
- `AnnouncementUI.WorldPresentation` is assigned by the existing bootstrap setup. `RefreshPortrait` resolves art only on explicit open/refresh; `ValidatePortrait` only clears an image whose owner, graph, binding or mode is stale. The actual `OnGUI` draw path calls validation and `MeasurePortraitRect`.
- Native portrait dimensions come from the UV subrectangle, not the whole atlas. Whole-pixel enlargement and positioning are used when a complete source frame fits on integral screen coordinates; smaller or fractionally constrained slots retain aspect. Existing model captures retain centered-square geometry.
- Root-owned catalog selector and projection-owned environment/zone accessors reuse current owner state and existing cached art without top-owner substitution, temporary placement, a second catalog, model capture or scene search. Flat-mode rejection is handled with an empty image and cannot fall through to 3D.

Files changed: `ExaminePortraitRenderer.cs`, `AnnouncementUI.cs`, `GameBootstrap.cs`, the narrow accessors in `ZoneRenderer.FlatPixel.cs`, `EnvironmentSpriteRenderer.cs`, `FlatPixelGreyboxCatalog.cs`; two new focused test files with fresh metas; the existing `ExaminePortraitTests` null-subject call now explicitly selects the legacy `(Zone, Entity)` overload. After native RED, public API tests replaced their temporary missing-method reflection bridges. Private popup lifecycle reflection remains to exercise explicit refresh and cheap invalidation in EditMode without pretending a GUI render occurred.

Self-review: 🟡 corrected square stretching, legacy-model fallthrough, incomplete preview cleanup and stale cached portrait authority. 🔵 legacy overload behavior and preapproved resources remain preserved. 🧪 actual1280×800 world/inventory/return capture remains pending; subsequent native verification and independent review are recorded below. Script tests cannot establish visual quality or controller feel.

### First targeted GREEN and cold review

`Docs/Verification/Live16x24/opening-items-examine-preimport.xml`, 2026-10-23 06:48:43–06:48:51 UTC: all52 examine cases passed. The combined98-case run had97 passing and one expected missing-item-resource failure belonging to the separate item import. No examine fixture or production misses remained. The wider prior motion/flat regression plus legacy portrait/ledger controls is running separately; do not equate this targeted result with that sweep.

Independent read-only review of the renderer, actual UI draw path/bootstrap and exact-subject forwarding found no actionable P1/P2. It confirmed authority before model lookup, explicit lower subject, paused inventory, graph checks, complete old-preview cleanup, borrowed native resources and UV-based integer layout. A proposed additional actual-Resources torch portrait test remains outside Assets at `/tmp/flat-examine-resource-test.txt`; it has not run and is not part of the52-case count. The root deliberately keeps the current expanded run stable and will verify the actual imported sprite through ordinary Play instead of adding another compile cycle for this bonus check. Real world/inventory popup appearance and controls remain pending that Play acceptance.

### Final native regression — GREEN

Authoritative receipt [`opening-items-examine-green.xml`](Verification/Live16x24/opening-items-examine-green.xml), 2026-10-23 06:52:02–06:53:00 UTC: **629/629 passed, zero failed, zero skipped**, across32 fixture groups. This includes all52 new examine cases,96 existing examine/UI controls, the prior435 motion/flat checks and46 item cases. The imported item-resource positive passes in this combined run. An earlier initialization request executed zero cases while TestRunner cleanup was pending; it contributes no test evidence.

The examine agent independently parsed the saved XML and confirmed629 total/629 passed and52 matching new examine cases/52 passed. Production remains unchanged after that run. `git diff --check` is clean for the owned files, fresh test metas are present, and the independent cold review remains clear of actionable P1/P2 findings.

**Can verify:** native tests exercise exact owner/frame selection, current torch and repair/pool state, lower occupants and footprint cells, carried/equipped ownership while paused, stale graph/viewer rejection, old-preview release, borrowed-resource preservation, actual popup refresh/cache invalidation methods, pure16:24 layout geometry, legacy model behavior and existing popup controls. The actual bootstrap binding and `OnGUI` helper calls were also reviewed in source.

**Cannot yet verify:** the actual GameView appearance of the corrected popup, real keyboard/controller return, rendering feel or Deck performance. The proposed extra Resources-only portrait test remains unrun in scratch, outside all reported counts; root's ordinary Play route will supply actual imported-art acceptance. Root owns editor/Play and the main live art-direction status. No Unity/CUA calls or commits were made by this subagent.

### Actual Play acceptance and inventory F1 follow-up

Root's ordinary Play checks now distinguish two routes that looked similar: a world pile summary deliberately stays neutral even if it lists one item; selecting that individual item and choosing Examine opens its native portrait correctly. Captures `native-examine-tonic-ground`, `native-examine-tonic-carried` and `native-examine-torch-lit` under `Docs/Verification/Live16x24/` record successful individual subjects. The aggregate reader has not been promoted into false single-subject authority.

A separate inconsistency is real: inventory F1 enters `InputHandler.OpenInventoryDetailsReader`, which calls `InventoryUI.SelectedDecisionDetails()` and then the plain `AnnouncementUI.Open(text)`. Thus the useful handling/comparison/food/material prose appears under NOTICE / Field notes without the selected item identity or portrait. `native-examine-live-red.png/json` records that live route. Existing629 tests verified document-driven portraits and actual Examine actions, but did not cover F1 metadata; do not describe them as proving every reader entry point.

Bounded follow-up plan (before production):

1. Test the actual F1 dispatch and reader methods with carried/equipped selected owners and preserve the original decision prose, no action cost, pending notices and return selection.
2. Share the inventory selection logic between the existing string query and a structured presentation query, capturing the exact item before generating its text. Use existing `BuildInventory` authorization/metadata, retain the decision text as a displayed section, and reject invalidated owners rather than falling back to stale plain text.
3. Keep recipe and container-transfer previews neutral; they describe composite operations rather than one inspected owner. Preserve search ownership of F1. Revalidate the viewer/zone after description callbacks before entering the reader.
4. Pass the optional document through the existing decision-reader lifecycle without changing controls/status/pickup/trade readers. Run native RED→GREEN and affected regression; root repeats the real F1 flow afterward.

Prepared22 focused cases in `InventoryDetailsPresentationTests.cs`. Native RED and subsequent implementation/results are to be appended after they occur. No world-pile mechanic or authority change is included.


### Inventory F1 implementation and native verification

After the actual route audit, `InventoryDetailsPresentationTests` confirmed native behavioral RED in [`inventory-details-red.xml`](Verification/Live16x24/inventory-details-red.xml): **22 total,5 passed,17 expected failures**, 2026-10-23 07:13:31–07:13:32 UTC. The failures cover missing selected-item documents and stale owner/viewer/zone details being opened. An initial keyboard test called `HandleInventoryInput` below the F1 dispatch; it was corrected to the actual `UpdateInput` dispatch before the authoritative RED. That fixture mistake is not counted as a production finding.

Implementation changes only two existing partials:

- `InventoryUI.Clarity.cs`: the existing `SelectedDecisionDetails()` string API shares its selection logic with `SelectedDecisionPresentation(out text)`. The latter captures one exact subject, uses current `BuildInventory` metadata/authority, and displays the original decision text in a “Decision details” section. It does not replace equipment comparisons, handling, food or material guidance with generic flavor. Invalidated subject metadata clears the text too. Recipe and container-transfer previews keep their neutral reader because neither identifies one examined item.
- `InputHandler.Clarity.cs`: F1 supplies that optional document to the existing decision-reader lifecycle. It rechecks the inventory instance, viewer, zone and input state after description providers and rejects stale subjects. The plain wrapper still serves controls/status/loot/trade unchanged. Pending notices, modal return and action costs retain their existing behavior.

The new test fixture and fresh `.meta` exercise carried row/action-popup selections for four item categories, actual equipped ownership, real InputSystem F1 → `UpdateInput`, original prose preservation, no energy/quantity cost, stale render/examinable/stack/ownership, description callback invalidation, neutral crafting/container previews, search and pending-notice return. No world-pile behavior changed.

Native results:

- [`inventory-details-green.xml`](Verification/Live16x24/inventory-details-green.xml): **22/22 passed**, zero skips,07:15:05–07:15:06 UTC.
- [`inventory-details-regression-green.xml`](Verification/Live16x24/inventory-details-regression-green.xml): **254/254 passed**, zero failures/skips,07:15:56–07:16:38 UTC, across15 affected fixture groups. This includes the22 new route cases, prior52 flat portrait/layout and96 legacy examine/UI cases, plus84 existing clarity and world-examine controls. The earlier629 broad graphics result remains separate evidence; it was not rerun for this two-partial input correction.
- [`inventory-details-cleanup.json`](Verification/Live16x24/inventory-details-cleanup.json): editor stopped, no pending Play/compile, `Assets/Scenes/Main/SampleScene.unity` restored, native save isolation inactive.

Independent read-only cold review found no actionable P1/P2: exact owned subject and original prose, no invalid-subject fallback, neutral composite previews, post-provider context checks, and unchanged queue/return logic were verified. `git diff --check` is clean. 🟡 The missing F1 document and stale callback-context cases are fixed. 🧪 The native keyboard dispatch is exercised; the actual F1 popup appearance and physical Deck controls are still awaiting root's ordinary Play recapture. Existing controller mapping routes the modified trigger to F1, but this follow-up does not claim a physical Deck test.

### Final ordinary F1 recapture and cleanup

Root restarted the isolated opening and used only ordinary keyboard controls: N → I → Tab → select starter healing tonic → F1. `native-examine-f1-corrected.png/json` shows the EXAMINE title, count2, exact native bottle and original decision details; the receipt records `opening-items` frame7, `IsModel=false`, paused inventory and unchanged player40,12. Right advances to page2 with the remaining pack-weight guidance and metadata. Escape returns to the same selected tonic in inventory; another Escape returns to the world, and Right moves to41,12 normally. `native-examine-f1-world-return.png/json` records that return. No inventory contents, turn state or entity placement were injected for verification.

The earlier actual world/carried tonic and lit-torch captures establish the same image route outside F1. Root and independent visual review found crisp16×24 proportions and no portrait overflow. Native tests establish authority/countercases; this ordinary run establishes visible keyboard routing/pagination/return. It does not establish physical controller feel, Deck performance or all-object coverage. The editor console showed0 errors and2 existing Metal depth-surface warnings. Final `native-motion-items-examine-cleanup.json` confirms Play/compile/pending/isolation false, original SampleScene and view selection restored. The bounded examine slice is accepted; overall presentation remains opt-in until the separate release journey.
