# G1.2 — continuous flat world rendering

Status: **native integration and bounded ordinary Play accepted; regional art and packaged/Deck delivery remain incomplete**,
2026-10-23. This follows [G1.1's fallback catalog](GRAPHICS-G1-PRESENTATION.md)
and the [graphics completion plan](GRAPHICS-COMPLETION-PLAN.md). G1.1 passed
40 focused and 90 bounded native regression cases before this integration.
Parent owns Unity and release verification. The design below remains the
bounded contract; its acceptance gates are not inferred from source completion.

## Bounded outcome

While the existing explicit flat-mode request is enabled, every current world
zone uses the same 16×24 projection, including uncovered Spread, other biomes
and underground. Explicitly disabling it restores the earlier presentation.
Fullscreen screens still borrow square cells and restore the rectangular world
on return. Fresh-install defaults, persistent settings/menu work and packaged
normal-launch acceptance belong to the following G1 delivery slice.

This is **interim visual coverage**, not completed regional art. Keep the exact
four-zone `FlatPixelGreyboxCatalog.IsAdmitted` contract for regional native
art, ground styling, reserve cords and finite-supply observation. Outside that
scope, use the object's truthful glyph/color fallback, with one narrow native
exception: the existing Player, PetDog, MarlbackScrabbler and MarlbackGleaner
identities retain their approved idle/motion art. Their portraits must agree.
Do not silently expand all native props, crop states or Patchbearer supply art
to the entire world. Approved source images remain byte-identical.

## Source sweep corrections

| Premise | Verified source and required correction |
| --- | --- |
| A global mode is a broader `IsAdmitted` list | `ZoneRenderer.FlatPixel.cs` currently couples projection to that predicate. Separate presentation choice from local skin eligibility; preserve the existing admission tests. |
| A safe fallback catalog makes the existing render path safe | `ZoneRenderer.RenderCellCore` calls `CP437TilesetGenerator.GetTile` before the environment overlay. Unsupported Unicode still regenerates that old bank unless the active flat prepass bypasses this call. |
| The overlay can recompute color independently | `RenderCellCore` dispatches pooled `Render`, then applies `LightMap`. `DamageFlashPart` decrements its counter on each Render event. The overlay must reuse the exact owner's result, not dispatch again or blindly parse the base color. |
| A main tile's color proves which owner it describes | The environment pass releases and restores displaced claims, and reprocesses dirty neighbors. A color alone can belong to a previous or invalid top owner. Keep an explicit current owner/render/cell receipt. |
| Actor motion is scoped by the native catalog | `FlatPixelActorMotionCatalog` is semantic and zone-independent. `ObserveFlatCell` checks the renderer's current zone but not manager-cache freshness. Define the portable four-actor policy once and enforce graph freshness during admission, events and tracked-view reconciliation. |
| Contextless examine lookup will follow the world | `EnvironmentSpriteRenderer.TryGetFlatSubjectSprite` has no zone argument and always queries opening art. Give the new presentation route an explicit current-zone context; carried subjects use their authorized viewer's context, not their absent spatial zone. |
| Flat mode proves water is already represented | `IsFlatPixelWaterRepresented` currently checks only mode and a sole live water coating. G1.1 does not render tile-state puddles into its background. Outside admitted local ground, suppressing the mark would hide water. |
| Stopping ambient animation stops its work | `UpdateAmbientAnimations` already returns while flat, but `RefreshWaterCache` still scans the zone and allocates a centerline array. Retire old water layers/caches and skip rebuilding unused legacy caches while flat. |
| Global native Patchbearer art is a free extension | Local `ReconcileObservedOwners` clears tracking outside admission. Keep that native art local in this slice; its outside glyph cannot falsely assert a healing-slot state. |
| Every visible owner needs Physics | G1.1's corrected contract accepts a canonical Render-only owner. Reject foreign Physics and custody when Physics exists; retain the stricter existing requirements for animated actors. |

## Shared selection seam

Add one bounded presentation resolver on the existing catalog, rather than a
second world renderer or another full-zone discovery pass. Suggested names:

- `TryResolvePresentation(Zone, x, y, out result)`: validates the current graph,
  canonical cell and current owner using G1.1, then selects the treatment.
- `TryGetPresentationSubjectSprite(Zone, Entity, x, y, out Sprite, out Color)`:
  uses the same native-versus-glyph precedence without selecting a cell's top
  occupant. Existing document authorization remains mandatory at the caller.
- A small result wrapper or added result fields identify **native foreground**,
  **glyph foreground**, **local ground**, and **water ground represented**.
  These are render decisions, not new gameplay state or regional admission.

Selection order:

1. Reject invalid/currently replaced manager graphs and invalid canonical cells.
   Detached valid zones remain usable as in G1.1. Never generate or repair a
   world graph during presentation.
2. G1.1 selects the current visible owner or eligible remembered static terrain.
   Unexplored cells disclose nothing. Do not substitute a weaker local owner
   selected from a stale object list.
3. In an admitted graph, prefer the exact approved native mapping and existing
   local ground/decorations. An uncovered owner uses its real glyph rather than
   the old generic Actor/Prop/LooseItem category shape. Exact ordinary ground
   handling remains local; do not paint every biome green.
4. Outside admission, allow native art only for the four portable actor
   identities above. Factor their shared identity/owned-Render predicate out of
   the existing motion selection so static idle, motion and examine cannot
   acquire different eligibility rules. Missing motion retains available static
   idle; missing idle art retains the glyph and must not suppress that body.
5. Everything else outside uses G1.1 foreground and authored background. Do not
   consult local finite stock, reserve claims or crop-specific native state.

Keep the old `TryResolve`/`IsAdmitted` APIs unchanged for their existing scoped
tests and callers. Change the live environment and portrait integration to the
new presentation seam. Outside-scope region-specific subjects must use the same
glyph in world and examine, even if an opening atlas happens to be loaded.

## Render order and color ownership

The existing full path is: compute FOV/light → `RenderCellCore` for each cell →
environment `PostRender` → complete actor cell pass. The dirty path is:
`RenderDirtyCells` → incremental environment `PostRender` including neighbors →
complete actor cell pass. Preserve that order and both actor completion sites.

In active flat mode, branch before the legacy foreground `GetTile` call:

1. Resolve the current flat cell. Clear rejected/unexplored presentation without
   falling through to older art. A stale graph cannot become permission to draw
   its legacy model. Mode choice and disclosure validity are separate concepts.
2. For the actual visible selected owner only, run the existing pooled Render
   event and light calculation once. Factor that calculation into a helper
   shared with the unchanged legacy branch; release the event on every path.
   Remembered, unexplored, foreign and suppressed invalid owners receive none.
3. Paint the native-sized baseline and retain its exact owner, RenderPart,
   current Cell and resulting foreground color. A fixed-size cell receipt array
   is sufficient; no new entity-wide dictionary or scan is required. Clear its
   ownership on zone replacement, mode exit, fullscreen takeover, full main-map
   clear, disable and destruction. A repaint replaces its own receipt.
4. Environment claims use native RGB art with its established tint, or the
   matching glyph receipt. Neighbor reclaims may reuse a receipt only when the
   owner, RenderPart, canonical cell and graph still match. If none matches, use
   static identity color plus lighting without a second Render callback; the
   next actual dirty prepass supplies a fresh event result.
5. Apply lighting once to the chosen fallback background, including a valid
   lower owner's authored background. Do not borrow another owner's background
   receipt. Keep remembered dimming and established local ground treatment
   separate. Use a full16×24 fill, never a padded16×16 solid block.

Do not introduce event calls from catalog lookups, portrait painting, quiet
frames or animation ticks. The four existing native actors retain their
current authored-color/pose behavior in this bounded slice; this is not a
redesign of status tint on multicolor actor art.

## Actors, water and lifecycle

- Bind the environment observer only after the shared resolver selected an
  eligible native actor. Successful `ObserveFlatCell` remains the commitment
  that permits suppressing the static body. Unknown actors keep their glyph;
  do not send them to the old `EntityVisualCatalog` while flat mode is active.
- Extend current-graph validation to `CanRender`, `ValidFlatOwner`, tracked
  views and event admission. A manager replacing the same ZoneID must release
  old views even on a quiet frame. Preserve existing death/reaction ordering,
  visibility checks and bounded repaint queues. Do not add `SyncZone` scans.
- Portrait routing takes `CurrentZone` explicitly after
  `ExaminePortraitRenderer.IsCurrentPresentationSubject`. Invalid subjects stay
  handled by flat mode and return no image, never an old preview camera/model.
  Idle native actor portraits need not display the transient walk/hurt frame.
- Water suppression requires current, visible **actually selected local water
  ground**, not `FlatPixelModeActive` alone. Pass that representation decision
  into tile-state painting or provide an equivalent shared result query. A
  sole water coating outside local ground keeps its normal mark. Oil, mixed
  coatings and expired water do not qualify. Keep charge/heat/cold/cloud
  priority after a represented water coating is skipped.
- Do not retain legacy fine-water subtiles, flow reflections, animated foliage,
  old 3D presenters or particles across flat mode/zone/UI transitions. Clear
  their existing layers and caches; skip unused `RefreshWaterCache` rebuilding
  while flat. The older mode rebuilds its caches normally on return.
- Keep camera/grid borrowing and square fullscreen restoration in
  `FlatPixelWorldProjection`. Changing zones while requested flat remains
  active must not dispose/reapply projection merely because native art scope
  changed. Preserve integer framing and the real Tilemap coordinate source for
  cursor, actors, FX and lights.

## Test-first implementation sequence

Add focused integration cases to a new fixture, then record native RED before
production changes. Use the actual Awake graph where order/claims matter;
reflection-only missing-API setup is acceptable until the new seam exists.
Do not count an edit to expected legacy scope behavior as RED for a new feature.

| Group | Positive and counter checks |
| --- | --- |
| Continuous requested mode | Glade → uncovered Spread → another biome → underground → glade retains1×1.5 grid and camera binding; explicit off restores captured old metrics. `IsAdmitted` remains false outside its four graphs. |
| Current graph and fog | Same-address manager replacement rejects old world/portrait/view references on a quiet frame; restoring the exact current graph recovers. Unexplored and remembered cells never dispatch live Render or reveal low-layer live owners. |
| Real render order | Actual full and dirty paths with RenderProbe + DamageFlash dispatch once for the selected visible owner. Environment claim preserves event color×LightMap. A malformed top owner cannot donate its tint/event to valid lower terrain; valid Render-only owner still draws. |
| Glyph/cache identity | Two unknown identities in one old category remain distinct; unsupported Unicode through the actual renderer never replaces the global bank; variants follow coordinates. Missing native source leaves the correct padded glyph and no missing actor body. |
| Reclaims and lifecycle | A dirty-neighbor reclaim uses only a matching receipt. Move, RenderPart replacement, custody change, pause, full-clear, mode roundtrip and zone replacement cannot restore old colors/bodies. Repeat fullscreen and off/on twice. |
| Portable actor agreement | Each of the four actor identities outside admission has matching native idle art in world/examine and existing motion eligibility. Missing motion leaves static idle; absent idle leaves glyph. Unknown actor stays glyph. No preview model/camera is allocated. |
| Local art boundary | Outside native props/products/Patchbearer use matching world/examine glyphs and do not assert local stock or reserve cords. Return restores current exact native state, not a stale cached state. |
| Water and hazards | Outside sole live water retains its mark; admitted represented water suppresses only that water and still permits charge/heat. Oil/ice/mixed/expired/hidden counters remain correct. Fine-water/legacy layers are empty while flat and restore only after explicit off. |
| Performance/ownership | Warm unchanged frames allocate0; no new SyncZone/full discovery scan. Owned fallback copies dispose without destroying shared atlases. Existing four-zone quiet supply/cord invalidation still passes. |

Relevant controls: `FlatPixelFallbackCatalogTests`, `FlatPixelActorMotionTests`,
`FlatPixelActorMotionCatalogTests`, `FlatPixelGroundStateReadabilityTests`,
`NativeGroundStateReadabilityTests`, `FlatPixelStateRenderCadenceTests`,
`FlatPixelNorthPlotGroundTests`, native portrait/layout/color tests,
`FullscreenUiCanvasOwnershipTests`, camera/projection and `SpriteSpellFxRendererTests`.
Run affected controls once after focused GREEN; broaden only for an identified
failure or the later release boundary.

Some old tests deliberately equate leaving the opening with turning flat mode
off: the travel assertions in `FlatPixelCellarScopeTests`/`FlatPixelNorthScopeTests`
and the legacy preview controls in `FlatPixelExaminePortraitTests`. Preserve
their local `IsAdmitted` assertions. Update their *mode* expectation for
continuity, and explicitly disable flat mode when proving the old presenter
still works. Do not remove the restoration or legacy controls.

## Acceptance, limits and handoff

After native GREEN and independent review, use ordinary travel through an
admitted and an uncovered zone, examine a native portable actor and an unknown
subject, open/close inventory, and exercise targeting plus a visible hazard.
Capture real1280×800 Game output and an action sequence; compare world/examine
identity and projection on both sides of the boundary. Record seed, route,
coordinates, mode and exact test receipts. No source contact or synthetic
catalog result substitutes for that Play gate.

G1.2 is complete only when this integration behaves continuously and legacy
off remains recoverable. It does **not** complete native regional coverage,
new default/menu delivery, the pending product-use loops, a packaged release
or physical Deck acceptance. The coverage ledger must keep those gates open.

Implementation ownership is assigned by the parent after G1.1 GREEN. Expected
code areas are the existing catalog, ZoneRenderer flat/prepass paths,
EnvironmentSpriteRenderer, actor validation and portrait call site; avoid
unrelated gameplay, asset, save, control-layout or source-art changes.

## Implementation checkpoint — native RED established

`Docs/Verification/Live16x24/g1-global-settings-red.xml` records the combined
50-case run: 2 passed, 48 failed, 0 skipped. Of these, G1.2 contributes exactly
29 cases in 18 test methods: 27 intended failures and 2 pre-existing admitted
water controls passed. The other 21 cases belong to the separate G1.3 settings
fixture. Native compilation succeeded; there were no fixture exceptions.

The first implementation uses the existing catalog and actual Awake graph:

- `FlatPixelGreyboxCatalog.TryResolvePresentation` applies G1.1 ownership before
  native selection. `TryGetPresentationSubjectSprite` shares the same local-art
  versus four-portable-actor policy with explicit zone context. Old `TryResolve`
  and `IsAdmitted` retain their narrower contracts.
- `ZoneRenderer.FlatPixelPaint.cs` branches before legacy glyph lookup, invokes
  the shared pooled Render-color helper once, and paints full 16×24 backgrounds.
  The environment owns a fixed cell receipt array (owner, RenderPart, canonical
  Cell, Zone, tint). Neighbor claims reuse a matching receipt or static color
  with current lighting; they never dispatch another gameplay Render event.
- Mode ownership is continuous while requested and a current zone is bound.
  A changed manager-cache validity schedules one complete repaint to discard
  stale stationary props as well as tracked actor views. Invalid disclosure
  remains handled by flat presentation instead of falling through to old art.
- Native actor eligibility is shared through
  `FlatPixelActorMotionCatalog.IsPortableActor`; the actor renderer additionally
  validates current manager graph membership during admission, tracked updates,
  event handling and component re-enable. No new zone discovery scan was added.
- Water suppression consumes the resolved local-ground decision; outside water
  retains its mark. Legacy water caches are cleared but not rebuilt while flat.
  Frame/canvas/zone/disable transitions retire color receipts.

### Existing expectation changes

These are mode-wiring updates after new-feature RED, not substitutes for RED:

| Existing fixture | Change and retained counter |
| --- | --- |
| `FlatPixelCellarScopeTests`, `FlatPixelNorthScopeTests`, `FullscreenUiCanvasOwnershipTests` | Outside travel retains rectangular projection; explicit off and fullscreen still restore square cells, and outside `IsAdmitted` remains false. |
| `FlatPixelExaminePortraitTests` | Legacy-preview controls explicitly disable flat mode; on re-entry flat mode releases model/camera/target and retains borrowed atlas. |
| `FlatPixelGroundStateReadabilityTests` | Injected environment explicitly receives its flat flag before private painter calls. Foreign-zone water remains marked even while global flat mode stays active; off/sprites-off remain separate controls. |
| `FlatPixelNorthPlotGroundTests`, `FlatPixelNorthWorksTests` | Manager replacement retains mode ownership but removes cords/submitted bodies. Restoration reobserves the actual current graph and supply; quiet polling assertions remain. |

### Review and limits at this checkpoint

The new fixture received an independent source/harness review before native RED.
Independent production review found no actionable P1/P2 in the bounded source
scope. A second source pass checked the current settings foundation: its owner
method applies the sprite and flat requests together before the existing sync;
G1.2 has no dependency on the new settings enum, persistence or launch code.
The focused 29-case native fixture is now GREEN, as recorded below.
No new approved art, game mechanics, save data, default launch selection or asset
scope was added. Native regional art remains incomplete outside the documented
opening scope. At that checkpoint the affected regression sweep and actual travel were pending.
Their subsequent results are recorded below; packaged/Deck acceptance remains open.

### First GREEN fixture correction

The combined `g1-global-settings-green2.xml` run reported 52 cases, 43 passed
and 9 failed. Six G1.2 failures reached the positive portrait assertions after
world rendering succeeded: the four portable actors, missing-idle fallback,
and the stale-manager case. Their custom actor builder omitted an owned
`ExaminablePart`, so `ExamineDocumentBuilder.BuildWorld` correctly returned null
before presentation lookup. Authored factory subjects already passed their
world/portrait checks. The fixture now supplies that real gameplay prerequisite
and explicitly asserts non-null/current document authority. Every native frame,
glyph fallback, no-preview and stale-graph assertion is retained. This was a
fixture correction, not evidence of a production portrait defect; no production
code changed for it. The original 29-case count is unchanged. The fresh rerun
passed all 29 G1.2 cases.


### Focused native GREEN and concurrent launch RED

`Docs/Verification/Live16x24/g1-corrected-foundation-launch-red.xml` is the
fresh native receipt after the fixture correction. The combined run is
**77 total, 54 passed, 23 failed, 0 skipped**; it is not an all-GREEN receipt.
Its fixture breakdown separates this integration from newly introduced launch
tests in the concurrent G1.3 slice:

| Fixture | Passed | Failed | Meaning |
| --- | ---: | ---: | --- |
| `FlatPixelGlobalPresentationTests` | 29 | 0 | All G1.2 focused cases pass, including the retained portrait assertions. |
| `WorldPresentationSettingsTests` | 23 | 0 | Concurrent G1.3 settings foundation controls pass. |
| `WorldPresentationLaunchTests` | 2 | 17 | Newly introduced launch RED; outside this G1.2 implementation. |
| `WorldPresentationAuditOverrideTests` | 0 | 6 | Newly introduced audit-launch RED; outside this G1.2 implementation. |

G1.2 therefore progressed from **2 passed / 27 intended failures** to
**29 passed / 0 failed** in the same 29 cases. Six intermediate failures were
test setup corrections described above, not production fixes. The first bounded
25-fixture regression and its correction reruns are recorded below. Subsequent
ordinary Play closes the bounded launch/travel/restoration gate; individual
world/examine agreement is covered by the focused tests and earlier portrait
work, not newly claimed from these travel captures.

**Can verify from this focused native run:** exercised render/claim paths,
current-graph and subject authority, portable/native/glyph fallback selection,
mode geometry restoration, once-per-paint Render events, water marks and the
fixture's measured quiet-path behavior. **Cannot verify from it:** real scene
composition, visual readability or motion feel, ordinary keyboard/controller
travel, default launch, a packaged build or physical Steam Deck performance.


### First bounded regression and fullscreen expectation correction

`Docs/Verification/Live16x24/g1-renderer-regression.xml` contains **663 cases:
661 passed, 2 failed, 0 skipped**. One failure was the remaining historical
mode-scope assertion in `FullscreenUiCanvasOwnershipTests`: it expected leaving
regional artwork coverage to disable flat projection. The same test now keeps
its Inventory → explicit-off → original-square restoration check, asserts that
an unskinned zone remains unadmitted yet flat/rectangular, and adds explicit-off
and re-enable checks while outside native coverage before returning. It also
checks camera Tilemap borrowing on each transition. This updates the intended
global invariant; no production code or existing restoration counter was removed.

The other failure belongs to the concurrent graphics-menu native gamepad
fixture (`SteamDeckGraphicsSettingsAdversarialTests`), whose menu expectations
are being handled in G1.3. Neither failure is relabeled GREEN. The fullscreen
case count is unchanged. The later focused rerun and ordinary Play results below
supersede these pending gates without changing the historical failed receipt.


## Final native and ordinary Play acceptance

### Native correction receipts

- `g1-launch-green.xml`: **168 total, 166 passed, 2 failed, 0 skipped**. The
  G1.2 global fixture remained **29/29**, and the corrected fullscreen fixture
  passed **18/18**. The two remaining failures were concurrent G1.3 launch work;
  this combined run is not represented as all GREEN.
- `g1-launch-final-green.xml`: **126/126 passed, 0 skipped**. This includes all
  18 fullscreen cases, 42 graphics-menu adversarial cases, the launch/settings/
  audit-override fixtures and both control fixtures. Both fixture failures from
  the original 663-case regression are covered by this fresh all-GREEN run.
- The 663-case regression was not rerun wholesale. Its 661 original passes, the
  subsequent global 29-case pass, and the focused correction run are separate
  receipts; their overlapping totals are not added into a fictional new suite.

Independent source review also caught outdated F11 expectations in the previous
Village and SpawnRing native audit drivers. G1.3 updates those drivers to assert
Flat → Previous while retaining the 3D preference, restores their borrowed new
mode/sprite preferences and runtime values, and explicitly launches SpawnRing
in Previous mode. Final read-only review found no remaining actionable P1/P2
in that bounded compatibility change. Source review does not imply that those
complete historical native workloads were rerun.

### Ordinary-input route and saved-game checks

Parent performed actual Unity Play in `Assets/Scenes/Main/SampleScene.unity`,
seed 64, with isolated saves and **no forced presentation override**. This was
the normal bootstrap; no scene flag was used to enable flat rendering. All ten
associated `g1-*.png` captures are actual **1280×800** images. Early JSON
`screen` fields report a 2321×1139 editor-query context; the PNG dimensions are
verified separately, and that metadata is not presented as a Deck viewport or
performance measurement.

| Check | Observed result and receipt |
| --- | --- |
| Missing presentation preference → New Game | `g1-normal-launch.json` records no mode key and no override. `g1-normal-start.{json,png}` shows `Flat16x24`, the admitted opening `Overworld.11.10.0`, player (40,12), 40 HP, tick 10, grid 1×1.5. Initial loading did not persist a mode key. |
| Select Previous, then return to flat | `g1-previous-roundtrip.{json,png}` records Previous, saved mode 1 and square grid. `g1-flat-selection.{json,png}` records saved mode 0 and restored rectangular grid. Player position, HP and tick remain (40,12) / 40 / 10 across the display changes. Earlier 3D preference remains true. |
| Inventory and targeting | `g1-flat-inventory.{json,png}` records InventoryOpen with square grid while flat mode retains ownership. `g1-flat-selection` shows the subsequent LookMode cursor and restored rectangular world. `g1-graphics-menu.{json,png}` records the ordinary graphics menu. |
| Ordinary westward travel beyond native kit | `g1-west-outside-native-kit.{json,png}` records `Overworld.10.10.0`, player (79,9), tick 450, `nativeAdmitted=false`, flat still active, grid 1×1.5. The capture shows glyph terrain and visible water marks rather than a return to the previous 3D presentation. |
| Return to authored opening | `g1-return-to-opening.{json,png}` records `Overworld.11.10.0`, player (0,9), tick 460, `nativeAdmitted=true`, flat still active and rectangular. Ordinary combat occurred: the player had 27 HP on the west-zone and return captures; this was not a frozen or invulnerable traversal. |
| F5/F6 quick-save/load | After saving the return state, ordinary movement reached (0,10), 14 HP, tick 470 (`g1-before-quickload`). F6 restored (0,9), 27 HP, tick 460 (`g1-after-quickload`) with flat mode, saved mode 0 and rectangular projection intact. Both have matching 1280×800 captures. |
| Stop/relaunch/Continue | `g1-continue-launch.json` records normal launch with saved mode 0 and no override. Ordinary Continue restores (0,9), 27 HP, tick 460 with flat presentation (`g1-ordinary-continue.{json,png}`). |
| Teardown | `g1-normal-cleanup.json` and `g1-continue-cleanup.json` record Play stopped, isolation/art-pending/view-borrowed flags false, save root null and SampleScene restored. |

The native opening and the neighboring glyph territory are deliberately
different levels of art completion. The uncovered zone keeps each object's
identity and flat geometry; it does **not** have a finished native environmental
kit. This delivery milestone does not promote that fallback to finished art.

### Bounded editor profile

`g1-delivery-profile.json` records 98.364 seconds and 85,352 frames spanning
ordinary movement, graphics menu, inventory, save reload and idle/tool time.
Each recorder retained 85,353 samples without wrapping (capacity 131,072).

| Recorded counter | Mean | p99 | Maximum |
| --- | ---: | ---: | ---: |
| `COO.ZoneRenderer.LateUpdate` |0.0528ms|0.1164ms|15.7728ms|
| Main Thread |1.1492ms|2.1989ms|849.2627ms|
| Draw Calls |29.16|34|34|
| Batches |29.16|34|34|
| Triangles |8,335.90|10,859|10,861|

These are **editor observations, not an optimization comparison**. The sample
contains idle/tool time and mixed operations, has no controlled baseline, and
does not attribute the Main Thread spike or total draw work solely to this
renderer. It proves neither standalone frame pacing nor physical Steam Deck
performance, GPU cost, a guaranteed frame rate or all-zone worst-case behavior.

### Closure and remaining gates

**Can verify:** native regression assertions; the actual default bootstrap;
Previous/flat restoration; square inventory and rectangular return; ordinary
travel across one admitted/uncovered border and back; retained native/glyph
identity in those captures; actual F5/F6 and fresh-session Continue; recorded
teardown; the bounded editor counters above.

**Cannot verify from this delivery run:** individual world/examine visual
agreement for every item, actor animation/hurt readability, all biomes, complete
regional artwork, G2 scene polish, unseen environmental states, every spell or
controller flow, packaged build behavior or physical Steam Deck performance.
The focused native portrait/actor cases and earlier art captures remain their
separate evidence; no new portrait or motion acceptance is inferred from a
travel screenshot. G2 remains source-only at this checkpoint. No new source
artwork was imported by G1.2.

G1.2's continuous presentation integration is accepted at this bounded native
and ordinary-Play level. The graphics completion plan still owns scene-level
art completion and final packaged/device acceptance.
