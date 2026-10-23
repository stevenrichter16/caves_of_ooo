# Live 16×24 art direction and integration

Status: implementation brief, 2026-10-22. This supersedes the crop-only next step in the overnight clearing plan. The user clarified that the intended result is a striking visual transformation fully integrated into the playable game, and explicitly allows the time each sprite and animation needs. Asset counts, arbitrary iteration totals and an overnight deadline are not completion criteria.

## What completion means

A coherent playable opening: original characters, terrain, architecture, props, items, animation and feedback share one visual language. The real player can move, fight, inspect, use services and change the world; the art responds to those changes. The user-approved native 16×24 characters are the anchor. Existing 3D art and its return path remain recoverable.

The current clearing is a static asset study. Its seven passing tests establish scene routing and asset persistence, not live integration. Do not present that study, an imagegen render or a crop-only reskin as completion of this goal.

## Visual direction

Build a memorable place around the real opening's three functional anchors: pond/draw point, cultivated garden/repair well, and ruined cellar. Give each a distinct large silhouette and material character. Connect them with quiet traversable ground. Concentrate human-made detail where people actually work, then let it thin into wilderness.

The existing study reads as evenly spaced samples. Replace that composition with purposeful clusters, broken rhythms and a strong focal ruin. More props alone will not solve the problem. The dark green ground, restrained warm accents and square native pixels remain useful. Avoid a flat carpet of repeated tufts, microtexture, indiscriminate glow or opaque scenery that conceals interactions.

Use the [reference library](ART-REFERENCE-LIBRARY.md) and built-in imagegen to develop original designs. Keep reference IDs and exact prompts. Concepts must survive native-size reconstruction and real game captures. Use editable Blender sources where useful and native sprite authoring where better suited; do not force every asset through a 3D pipeline.

Animations should explain action and character: directional movement, readable anticipation/contact/recovery, occasional purposeful idle gestures, plant or water motion where appropriate. Keep effects legible and restrained. Preserve the approved base images; author animation derivatives separately. Do not fill every sheet with duplicated frames or replace all movement with generic bouncing.

## Required production pipeline and agent handoff

**User-confirmed target: flat 16×24 pixel art in the actual game.** The original multi-pass proposal mentioned image-to-3D generators; the user explicitly clarified that the final presentation is 2D. Preserve the multi-pass method, replacing model generation with native sprite and animation production. Tripo/Meshy/Pixal3D are not required stages. Blender or another 3D tool may help author a source asset, but its output must serve the approved flat presentation.

**Tool delegation:** the user gives current and future agents full discretion to choose and use any available tools needed for this work, including imagegen, procedural tools, pixel editors, Blender, Unity MCP, browsing, code and tests. Continue autonomously without asking about routine tool or art-production choices. Use the access actually available and record tools used honestly. Take the time each sprite and animation needs; keep the approved source art and previous presentation recoverable.

1. **Greybox the real playable opening.** Use simple colored 16×24 tiles and rough silhouettes attached to the existing world entities in the actual spawn at Overworld.11.10.0. Exercise ordinary movement, collision, targeting, combat spacing and interaction access. Check composition, landmarks, sightlines, quiet ground, scale, camera and selection. The separate static clearing cannot satisfy this gate.
2. **Capture and critique the greybox.** Save actual Unity Game captures at 1280×800 plus the seed, zone, renderer mode and tested route/actions. Identify weak composition and gameplay readability before detailed art. Any meaningful layout change belongs in the real world generation/owners, not an unconnected backdrop.
3. **Reinterpret the captured scene with imagegen.** Use the actual greybox screenshot as the composition input, approved sprites as style anchors, and selected external references for specific shapes/materials. Preserve traversable paths, functional landmarks, flat view and native cell proportions. Save exact prompts, inputs, outputs and critique.
4. **Extract a practical parts list.** Separate the chosen concept into terrain, wall/edge variants, props, characters, items and animation/state frames. Record each part's real blueprint/VisualID, footprint, pivot, palette and required states. Extract/cut out useful reference parts with imagegen or an appropriate editor, then resolve irregular generated pixels during native sprite authoring. A complete scene image is not a substitute for usable game assets.
5. **Produce native sprites and animations.** Create editable sources and exact-resolution frames with square pixels, consistent baselines and Point imports. Use purposeful movement/action/state animation. Keep concept art separate from production sprites; do not assume shrinking an image produces a coherent pixel grid. Preserve approved base frames and author derivatives separately.
6. **Reintegrate with the real simulation.** Map art to actual owners and states, including visibility, damage, harvest, repair, death and removal. Use shared materials, atlases and batched/static tile rendering where suitable; profile the result rather than assuming fewer objects are faster. Keep dynamic actors and state changes correct.
7. **Play, capture, compare and revise.** Re-run the same route/actions and inspect actual frames and motion. Check camera, UI, selection, visibility, transitions and rendering cost. Compare the accepted concept with the real game, fix the largest visible or functional mismatch, and repeat the relevant passes. Reuse a verified greybox while its geometry/composition remains valid; do not redo it for every small sprite correction.

**Continuation record required for each asset group:** current pass; greybox capture and tested actions; concept inputs/prompt/output; extracted parts and owner mappings; editable sources; production assets; actual integration capture; tests/performance observations; unresolved issues and next action. Future agents must read this record before continuing. Static studies and concept renders must remain labeled as such.

**Current handoff:** milestones1–2 have an opt-in playable opening, corrected native captures and an imagegen paintover. The first ten native base sprites are now integrated and checked in actual Play, including ripe grain changing to stubble. See the continuation record at the end. Remaining cast, anchors, animation and the full release journey remain pending; generated concepts remain reference only.

## Verification sweep before runtime work

| Assumption | Source / correction |
| --- | --- |
| Normal spawn is the old village constant | FreshGameStart prioritizes ReferenceGladePlan.ZoneID, Overworld.11.10.0. Use this real opening as the first complete visual target. |
| Keeper and cook belong together at spawn | SpreadExplorationResidents places Nella at 11.8.0 and Orven at 12.11.0. Integrate their actual sites after the opening; do not move or duplicate them to mimic the study. |
| Changing Grid.cellSize is sufficient | AnimatedEntityRenderer.CellAnchor, CameraFollow and LightSourceSpriteHook contain unit-cell positioning. FineWaterGrid is an independent square grid. All world layers must use one projection contract. |
| UI is independent of the world grid | Fullscreen inventory and other screens paint the main Tilemap. Rectangular world spacing requires an explicit world/UI lifecycle or separate square UI surface. Sidebar, hotbar and popup grids are already separate. |
| Turning off 3D selects only the new sprites | ZoneRenderer.SyncVillagePresentation also arbitrates older authored paintings. One shared admission policy must control the new mode and restore previous modes on exit. |
| Approved single PNGs can enter the actor catalog unchanged | EntityVisualCatalog expects 4×16 animation sheets. Add explicit static-frame support or a deliberate compatible resolver; do not silently fabricate animation. |
| Crop presentation is the entire opening | The native glade also contains actors, water, roads, ruin topology, repair services, cellar access, drops and fieldwork states. Drawgourd is part of this opening; distant Marlroot alone is not sufficient coverage. |
| All reference props already have functional owners | Several shelter/table/rack props are only study objects. Map art to demonstrated game owners; do not make scenery imply an available service. |

Inspect current signatures and tests before coding; this is a source audit, not a passing runtime result.

## Implementation sequence

1. **Presentation contract and live route.** Reuse SampleScene/GameBootstrap, actual opening generation and normal controls. Introduce an explicit opt-in 16×24 presentation mode with a shared zone/owner policy. Keep the study for art inspection. Define world-to-cell projection and fullscreen UI transitions before adding assets. Write RED tests for rectangular corners/anchors, movement endpoints, click/target round trips and mode restoration.
2. **Playable greybox and captured art target.** Connect rough ground, player and ruin/pond silhouettes to the real opening. Suppress conflicting presentations only where the new mode owns the view. Validate paths, interactions, camera, fog and selection through actual Play, capture it, then use imagegen on that capture and extract the production parts list. Follow the required pipeline above before detailed opening assets.
3. **Complete opening cast.** Design and integrate player, warden, villagers, dog and local marlbacks. Review individual native sprites, then directional/action animation, then the crowd in context. Preserve identity through equipment, damage, death and removal. Integrate approved bird/grazer/jar art wherever the actual owners occur.
4. **Complete environmental coverage.** Finish grass/roads/reeds/bushes, pond edges, walls and their VisualIDs, glowing quartz, broken columns and beams. Then services, campfire, well/draw point, chest/barrel/signpost, cellar stairs and wicket. Use state-aware art for repaired, destroyed, emptied and spent owners. Fit existing clearing-kit pieces only where their silhouette and function match.
5. **Interaction states and feedback.** Finish field rows, Drawgourd and other crops actually present; dry/wet, growing/ripe/spent are based on real parts. Present dropped equipment and harvested produce consistently. Ensure animation/FX, cursor, Vault/throw outlines and examine artwork match ownership and cell geometry. Keep existing gameplay rules.
6. **Connected sites.** Extend the same contract through the connecting Spread chunks to Nella's seed plot and Orven's kitchen. Integrate their approved actors and functional props at the real sites. Review ordinary wilderness transitions, not only authored landmarks. Record remaining biome coverage honestly; do not claim a whole-world conversion from three locations.
7. **Review and release.** Test the full player journey below, inspect actual 1280×800 captures and motion, measure rendering cost, fix significant faults, update the living record and push exact owned changes after fetch/rebase. Preserve restore paths and clean up only task-owned temporary state.

Each useful slice should be independently reviewable and committed. Work at the quality required; do not replace patient art iteration with more feature breadth.

## Coverage and release gates

- Inventory every owner/VisualID rendered in the fresh seed-64 opening. No accidental old-style fallback remains in the admitted opening. Dynamically introduced foreign objects retain an explicit, truthful fallback until covered.
- Exercise walking, targeting, combat, examine, drawing water, harvest, repair, cellar entry/return, save/load and travel through ordinary input.
- Hidden entities remain hidden. Remembered terrain does not reveal current occupants. Full and incremental updates agree.
- Changed or removed owners visibly change without ghost props. A crop image must never promise a harvest its part cannot provide.
- Mouse and controller cell selection align with rendered tiles. Inventory, full-screen menus, popups, sidebar and hotbar retain correct geometry.
- Toggle modes and revisit zones twice; compare original square/3D behavior. Keep the approved study images byte-identical.
- Capture actual gameplay at 1280×800 and inspect motion in Unity. Report editor measurements as editor measurements; no hardware Deck performance claim without Deck evidence.
- A release report shows actual game images and states completed coverage and outstanding coverage. Do not substitute concepts for gameplay proof.

## Implementation prompt

Read this brief, CLAUDE.md, the overnight progress record and current git status. Follow Required production pipeline and agent handoff: playable greybox → capture → imagegen reinterpretation → sub-part extraction → native 16×24 sprites/animations → real integration → playtest and iteration. Choose the next unfinished slice above and use any available tools needed under the user's delegation. Recheck cited source semantics, write failing tests for changed behavior, implement through existing owners and rendering paths, then test the counter-cases. Inspect native frames and animation in the real scene. Review, fix significant issues, update the continuation record and push only task-owned files to main after fetch/rebase. Continue from evidence, not from asset or test counts. Keep all user-approved art and the previous presentation recoverable. Stop calling an area complete until its coverage and gameplay gates pass.

## Review record

### Projection foundation — complete, native integration pending

First runtime slice: give actors an explicit Tilemap coordinate source, add opt-in camera projection, and align fixture/player lights to their overlay. Ordinary unit-cell/null paths remain counter-checks. This slice does not enable the new presentation mode or claim the playable greybox exists; fullscreen UI grid lifecycle, fine water, mode admission and actual greybox art remain next.

Native Unity RED evidence: camera/light suites ran25 cases with10 expected failures (7 missing camera projection,3 misplaced lights); actor suite ran11 cases with8 projected-position failures and3 legacy/foreign-zone counter-cases passing. The actor job-status tool reported an initialization timeout despite a completed native XML result; retain both receipts and use the XML as the test result. Source edits follow these failures.

The component-plus-wiring run completed38 cases:37 passed and the rectangular `ZoneRenderer` wiring case failed. Passing the real tilemap into `AnimatedEntityRenderer.Init` fixed that final disconnect. The final native Unity sweep completed **73/73 passing, zero skipped**: camera, light, actor projection, actual renderer wiring, actor state, authored actor/presentation, spawn-ring camera, Morrowfast/Felling framing and world cursor suites. The selected suite grew from48 existing cases to73, adding25 projection cases. Receipts are in `Docs/Verification/Live16x24`; `projection-regression-green.xml` is authoritative.

Implementation: optional `CameraFollow.WorldTilemap` projects normal gameplay camera centers, bounds and zoom; it is deliberately not globally enabled yet. `AnimatedEntityRenderer.Init(tint, tilemap)` uses cell-derived foot positions and is wired by the real ZoneRenderer. Fixture and torch lights use overlay cell centers and existing fixtures reproject on refresh. Null/unit defaults and authored/3D camera paths remain passing counter-checks.

- 🟡 Fixed: the actual renderer omitted the new projection source, caught by a separate native wiring RED test.
- 🔵 Independent cold review found no actionable P1/P2 defects within the axis-aligned, zero-gap grid contract. `git diff --check` is clean.
- 🧪 The activation slice must clear/reacquire actor views on grid changes: active movement/death caches world anchors. Pixel snapping stays at16 pixels/world-unit, and light radii remain world-unit values. Rotation, gaps, reflected transforms and actor scaling are outside this contract.
- 🧪 Camera fullscreen tests prove camera framing only. They do not prove main-grid UI restoration, water, targeting or other effects in an activated rectangular world.

**Can verify:** native Unity EditMode tests exercise real movement and lethal combat hooks, visibility, pooling, tilemap projection, renderer wiring and camera/light counter-cases. **Cannot yet verify:** actual Play appearance/motion, whole opening coverage or Deck performance. The new mode and playable greybox are not active; those are the next slice, with real Play capture required.

- 🔵 Current clarification adds a production brief and changes the scheduled continuation criteria; it does not itself implement the new mode.
- 🟡 Resolved plan issue: crop-only integration was too narrow for the requested visible transformation. The real opening's complete visual coverage now leads.
- 🟡 Resolved plan issue: an arbitrary morning cutoff could reward unfinished art. The user explicitly allows the time needed; continuation should finish the defined live opening release, then report remaining regional work.
- 🧪 Runtime and visual acceptance remain pending. No additional Unity test result is claimed by this document.


### Playable greybox activation — implementation plan

Next bounded slice, after projection commit6f62c4734: an explicit opt-in mode on the existing ZoneRenderer, admitted only by `ReferenceGladePlan.IsActive`. Requested mode and zone ownership remain separate from fullscreen visibility. Preserve previous settings and assets; no world-layout or gameplay mutation is needed for this first capture.

Verification corrections before code:

| Inspected seam | Decision |
| --- | --- |
| `ZoneRenderer.Paused` is a serialized field; inventory/quest/faction currently paint before pausing | Preserve serialization via a backing field/property; apply square UI geometry synchronously and order page entry before painting. Restore world geometry before camera restoration. |
| Both GameBootstrap paths create CameraFollow after initial SetZone | Explicitly bind the helper in both fresh and loaded paths, before SnapToPlayer. |
| Grid spacing does not resize16×16 art | New disposable greybox atlas uses native16×24 tiles at16PPU. This is temporary shape/blocking study art, not production sprite acceptance. |
| EnvironmentSpriteRenderer has complete full/incremental claim restoration | Intercept ResolveCell before legacy art tiers and reuse existing claims; no extra per-frame world scan. |
| Reference glade pond uses actual TileState coating | Resolve visible water from current state; never paint from hardcoded pond coordinates. Fog memory remains terrain-only and cannot expose hidden occupants or changed water. |
| Several legacy presenters can reactivate outside SyncVillagePresentation | Gate all native/authored scene admission and ordinary actor/ambient passes through the same ownership flag. Fullscreen must not hand the zone back to3D. |
| World cursor/affordance/intent already use Tilemap corners; sprite spell FX does not | Keep existing cursor paths and add rectangular round-trip tests. Give sprite FX the same optional Tilemap source. |

Substeps: tests first for catalog states/fog, grid lifecycle, four actual fullscreen pages, popup retention, renderer admission and cursor round trips; native RED; implement; native GREEN and independent review; isolated ordinary fresh-game Play, real input and1280×800 capture. Then use that actual capture for imagegen reinterpretation before detailed opening art. Keep old ambience disabled only inside this rough mode until its rectangular replacement is ready. No final-animation, full-biome or Deck performance claim belongs to this slice.


Activation review in progress:

- Native RED:66 cases,40 passing and26 failures. Nine projection-owner assertions, nine spell projection/wiring assertions and seven mode/UI assertions failed as intended. The remaining failure exposed a fixture assumption: the real finite basin leaves a permanent wet-ground coating when emptied, so its empty foreground changes while the wet surface remains. Do not imply drawing the basin dries the surrounding ground.
- First implementation run:139 cases,135 passing; four fullscreen assertions assumed Grid depth0 rather than Unity's default1. Corrected the fixture to assert exact original metrics.
- Expanded regression:217 cases,214 passing. It reproduced the new component-disable ownership bug and caught inactive-mode code restoring a grid it had never borrowed. Fixed both: own projection only while admitted, release on disable/exit, reacquire on enable. The older camera-pan fixture also depends on the current free-aspect GameView and is under review.
- Independent reviews: gate Felling binds on changed zone to avoid per-frame retries after asset failure; perform parent-Grid lookup only when constructing the projection helper; restore the launcher's prior Play-start override, scene, seed and view; validate the temporary resolution's unique ownership before removing it. F11 leaves the greybox and truthfully reports restoration of the previous presentation. Approved source sprites remain unchanged.
- Current asset group: procedural greybox only. It uses one128×72 atlas,17 cached16×24 shapes and the existing incremental world-claim renderer. Actual seed64 exploration coverage includes28 blueprint types; this is shape/category coverage, not finished per-object art. Sprite spell FX now follows projected cell centers and retains native pixel size. Directional actor animation, authored status-color response and replacement ambience remain subsequent art work.

Live capture and imagegen reinterpretation are the next gate. Native Play input, screenshot and cleanup evidence will be recorded here; no finished-art claim is made from EditMode results.


Activation native verification, 2026-10-22/23: the reviewed integration run completed217 cases with216 passing; the only failure was the pre-existing camera test's dependence on an ultrawide free-aspect GameView. Its two affected fixtures now set16:9 explicitly and assert the viewport is narrower than the zone before testing pan. The final targeted native camera rerun passed12/12. This is217 selected cases covered across the integration run and targeted rerun, not a single all-green217-case execution. The first final camera request was rejected because Unity was in ordinary Play; it executed zero tests and is not counted.

A final launcher review found an Edit-mode recovery hole: a pending capture session whose deferred restore callback is lost during reload can finish save isolation without restoring its scene/view/seed. A narrow recovery fix and regression checks precede the actual live capture.

### First ordinary Play review — framing issue found

The isolated launcher entered the real seed-64 `Overworld.11.10.0` opening. Ordinary keyboard input started a new game, moved the player from `(40,12)` to `(41,12)`, opened/closed inventory twice and entered Look/action selection. The world continued its NPC turns. Actual Game captures are `greybox-game-20261023-041214.png` (inventory) and `greybox-game-20261023-041232.png` (world), both1280×800. The world projection was1×1.5 and inventory synchronously returned to1×1. This is a partial route, not the full release journey; mouse targeting still needs a clean retest after the framing correction.

- 🟡 **Live finding:** the world capture squeezed square source pixels horizontally and undersampled the rough sprites. The camera retained an aspect derived from the previous free-aspect editor view instead of the fixed capture target. `GameplayViewportLayout.GetDisplayAspect` reconstructs display aspect from the camera aspect that the layout itself previously assigned; changing output dimensions can therefore preserve stale framing. `TargetVisibleTileRows=34` with a1.2 gameplay zoom also produces a view too distant for native16×24 art. Correct the target-aspect path and establish native-pixel framing before accepting the greybox composition for imagegen.
- 🟡 **Launcher recovery fixed:** four native RED cases now pass with fourteen existing save-isolation cases (18/18 GREEN). `Finish` queues presentation restore even from Edit mode; delayed reload recovery leaves Play/entry transitions alone. Receipts: `greybox-launcher-{red,green}*`.
- **Actual cleanup verified:** ordinary Finish returned to the saved clearing study, left Play and isolation inactive, cleared the launcher's pending flag, and restored seed, inherited save root and last-game preference. `live-first-cleanup.json` records these booleans without publishing a user save ID. The source study was not saved over.
- **Capture caveat:** `Screen.width/height` queried by an editor command reported the editor context dimensions; the saved PNG dimensions are the authoritative output resolution. Do not call that metadata a1280×800 render measurement.

The first world image is retained as a failed visual check, not an accepted art target. The next pass corrects framing, repeats movement/menu/cursor checks, and captures again. The source audit and practical owner/state mapping are in `ArtSource/Live16x24/PARTS.md`; no new final sprites have been integrated yet.

Camera correction verification: eight new native cases produced seven expected RED failures and one passing legacy counter-case. Physical output dimensions now drive layout aspect; optional `CameraFollow.WorldPixelsPerUnit=16` chooses integer magnification after the requested zoom and aligns the view to source-pixel boundaries. The projection owner borrows/restores that option. The focused camera/projection run passed32/32, including resize, odd dimensions, UI return and legacy zoom checks. Actual1280×800 framing is still to be recaptured after the water overlay fix; a focused test does not substitute for that image.

Water overlay correction verification: the new flat-ground painter tests plus existing native-ground controls ran36 cases:27 passed and9 failed RED. Plain water was still painted as a blue CP437 wave over the teal ground, and that coating branch masked charge/temperature/cloud cues. The narrow fix yields36/36 GREEN, zero skipped: only a sole live water coating relinquishes its generic marker in the active flat mode, while later hazard branches still execute. Oil, ice, acid, mixed coatings, residue and legacy/nonadmitted modes retain their cues. The tests inspect actual painter tiles and positively establish catalog water coverage; they do not replace a fresh gameplay screenshot. Independent cold review found no P1/P2 issue in this fix.

### Corrected live greybox and first paintover — 2026-10-23 UTC

The combined native Unity regression passed **306/306, zero skipped**, in21.17seconds. `framing-water-regression-green.xml` is authoritative. It retains the previous217 selected cases and adds89 to the selection:33 newly authored cases (framing9, water20, recovery4) and56 existing controls (Felling24, native ground16, save isolation14, hotbar2). The extra odd1281×801 case failed RED before float pixelRect sizing and physical pixel-boundary alignment fixed it. This is a selected regression, not a complete project test run.

The ordinary isolated seed64 game was started withN. Two inventory open/close cycles restored the world. Keyboard Look selected the actual reed-bank water basin at36,9 and opened its matching action popup. From40,12, ordinary input walked down4 and west9 to31,16. Up struck the wall at31,15 without moving through it; Left entered30,16, then10Right and4Up returned to40,12. NPC turns, combat, deaths and drops continued normally. No movement, fog or gameplay state was injected.

Captures (all native1280×800):
- `greybox-game-20261023-042705.png`: inventory.
- `greybox-game-20261023-042904.png`: basin action popup.
- `greybox-game-20261023-043002.png`: ruin passage after movement.
- **`greybox-game-20261023-043026.png`: accepted geometry/composition input for the first art paintover**, after returning to40,12. The rough sprites are not finished art.

`live-corrected-start-state.json` records the actual camera pixelRect940×680 (sidebar consumes340pixels), ortho21.25, aspect1.382353,16PPU and1×1.5 grid. The saved PNG confirms correct square source pixels and teal pond coverage. `Screen.width/height` still reports editor context dimensions, so it is not used as render-resolution evidence. `live-ruin-passage-state.json` records the passage endpoint. `live-corrected-cleanup.json` confirms Play/isolation/pending flags cleared and original scene, seed, save root, last-game preference, view and play-start scene restored.

- 🟡 Corrected before capture: stale camera aspect, sub-native pixel scale, fractional viewport alignment, generic water wave overlay and launcher reload recovery.
- 🧪 **Mouse world selection remains unverified/unsuccessful in this editor session.** CUA clicking the visible basin did not move the Look cursor or open its menu. The cached legacy pointer was outside the viewport; direct reads of legacy and new input backends disagreed and neither matched the intended image coordinate. Earlier `THROWING-IMPLEMENTATION-PLAN.md` already records the stale legacy pointer limitation. Keyboard selection and native coordinate round trips pass; do not claim real mouse/controller acceptance. Investigate a shared pointer seam without speculative DPI multipliers.
- 🧪 Harvest, repair, cellar travel, save/load, mode/travel round trips, motion and hardware Deck performance remain release gates. No final-art or whole-world coverage claim.

**Art continuation:** built-in imagegen used the corrected actual game capture as image1 and `Docs/Verification/Clearing16x24/clearing-game-final.png` as style-only image2. Exact prompt: `ArtSource/Live16x24/concepts/opening-reinterpretation-prompt.md`; output: `opening-paintover-v1.png`. The pond reed silhouettes, sparse rubble and separated wall material are useful design references. The generator added a cook/oven and creatures not present in the capture, introduced gradients and altered text; those additions are rejected. Use only owner-matched motifs, then extract references and author native sprites. `PARTS.md` maps all28 generated top-owner types plus player and state layers.

**Next bounded art group:** equipment-neutral player and warden base frames; dark/low walls, two pale reed clusters, low olive bush, sparse rubble, ripe grain and cut stubble. Keep the plain ground. Native sprites must be integrated with actual owner/state selection and recaptured before adding more variation. Preserve approved character PNGs unchanged. The production art release remains incomplete.

Final bounded cold review found no new P1/P2 defect in activation, fullscreen ordering, claims, projection restoration or launcher recovery. The concept comparison additionally rejects road bands turned into barriers, a chest painted as an oven, gravel painted as flowers and spent rows painted as ready grain. A second built-in imagegen call produced the ten-part isolated reference `ArtSource/Live16x24/concepts/base-parts-v1.png` using `base-parts-prompt.md`. These are concept-guided design references; native reconstruction and integration are the next pass.

### Native opening base group — implementation plan

This slice reconstructs ten selected base frames from the generated parts reference and integrates them into the opt-in real opening. It is deliberately a first visible group: player, warden, dark/low masonry, two reeds, low bush, rubble, ripe grain and stubble. Other owners remain explicit greybox silhouettes until their next group.

Verification sweep: the existing catalog already selects actual top owners and excludes live occupants from remembered cells, so production lookup belongs after that owner resolution, preserving ground and memory policy. `RipeCropRow` harvest is `FieldHarvestPart.Harvested`; `CropRow` has no growth clock. Drawgourd cannot reuse grain. The low crossing has its Wall tag removed but retains its low-wall VisualID/role. Keep approved NPC sources and previous study untouched. Equipment-neutral base figures avoid a permanent weapon after disarm. A single160×24 resource atlas is sufficient for ten16×24 frames, sharing texture/material and existing tile claims. No additional world scan or actorGameObject is required.

Art: author editable flat-front geometry in Blender from deliberate pixel clusters; render exact native canvases rather than shrink generated images. Use binary transparency and Point imports. Produce a legible enlarged reference through Blender. Tests first for owner/state selection and unknown/missing-art fallback, then actual resource/import dimensions and existing greybox/memory/UI controls. Capture the same ordinary start/route before accepting the group. Base frames are static; animation, the rest of the cast and full release acceptance remain outstanding.

### Native opening base group — integrated, 2026-10-23 UTC

Pipeline receipt: corrected greybox capture `greybox-game-20261023-043026.png` → `concepts/opening-paintover-v1.png` (whole composition rejected, useful motifs retained) → `concepts/base-parts-v1.png` → authored Blender pixel geometry in `Tools/Live16x24/build_opening_base.py` → editable `ArtSource/Live16x24/base/opening-base.blend` and160×24 atlas → Resources import → actual Play captures below. All paths to concepts/base are under `ArtSource/Live16x24`; receipts/captures are under `Docs/Verification/Live16x24`.

Ten static frames now replace exact supported owners: player, warden, dark ruin Wall, low Wall/crossing, tall/short pale Reeds, low green-grass Bush, low Rubble/BrokenColumn, ripe field row and spent field row. They are not ten animations. The existing catalog still supplies ground and an explicit rough fallback for everything else. A borrowed Resources texture supplies cached sprites/tiles; disposal destroys those generated objects, never the shared resource. There is no extra per-frame world scan or actor GameObject layer.

Verification and corrections:

| Check | Evidence / result |
| --- | --- |
| New resolver RED | `opening-art-red.xml`:57 cases,24 expected new API/injection failures,33 existing controls passed. |
| Synthetic art GREEN | `opening-art-synthetic-green.xml`:58/58; extra quartz-vein counter-check included. |
| Actual resource RED | `opening-art-resource-red.xml`:58 passed,1 expected missing imported atlas failure. Prevents synthetic-only false acceptance. |
| Native integration GREEN | `opening-art-integration-green.xml`:125/125, zero skipped;26 opening-art cases plus greybox/ground/fullscreen/projection controls. |
| Pixel contract | Exact160×24, ten nonempty16×24 frames, binary alpha;0 source-cell/render mismatches.8× geometry contact has0 mismatches. Point/Clamp/no mip/Uncompressed import,16PPU cached sprites. Approved five PNGs remain byte-identical. |
| Actual ordinary input | N fresh start; inventory open/close; L + arrows selects44,10; distant harvest rejected; walk to44,11 then harvest succeeds.44,10 becomes stubble while46,12 and50,14 remain unharvested. |
| Collision and movement | Walk back through40,12 to31,16. Up strikes existing wall and stays31,16; Left enters30,16 open passage. Player/camera remain aligned. Ordinary NPC combat/death/drop turns continued. |
| Cleanup | `native-base-cleanup.json`: Play false, original study scene restored, pending/isolation false. |

Actual screenshots: `native-base-start.png`, `native-base-harvest.png`, `native-base-passage.png` are1280×800 Game captures, not Blender scenes or paintovers. Read-only runtime receipts pin start/passage geometry and the three harvest booleans. The foreground uses native colors and existing remembered-terrain dimming; unrevealed actors remain excluded. Separate cold code review and native visual review found no P1/P2 within this bounded group.

- 🟡 Fixed documentation drift: initial art manifest claimed a lit-wall alias and broad rubble aliases absent from the exact resolver. Source generator/manifest now match the actual routes.
- 🧪 Actual Play confirmed ordinary `Wall` owners also use `reference-glade-lit-wall` (e.g.31,12 and31,15), in addition to quartz owners sharing that ID. These remain explicit rough wall fallbacks; the next boundary group must supply a distinct lit-wall frame without turning quartz into masonry. Do not claim complete wall coverage.
- 🧪 Repeated short wall pieces are readable as low obstacles but still need connected edge/end variants in the full boundary pass. Native base acceptance does not close final architecture quality.
- 🧪 Renderer snapshot:31 draw calls/batches,8,752 triangles,17,524 vertices in this editor view. This is a single editor snapshot including UI, not a before/after benchmark or Steam Deck performance evidence.
- 🧪 Mouse pointer routing remains the previously recorded open validation issue; keyboard targeting passed. No physical controller run claimed.

**Can verify:** genuine resource loading, owner/state/fog fallback behavior via native tests; square native pixels/import preservation; actual harvest visual change, keyboard target alignment, movement/collision, UI return and isolated cleanup. **Cannot yet verify:** final whole-opening appearance, complete state/animation coverage, connected-site journey, mouse/controller acceptance or Deck frame time. This is an opt-in development presentation, not the final default release.

Next bounded group: replace the four remaining living-actor placeholders (Villager, PetDog, MarlbackScrabbler, MarlbackGleaner). The native game start capture and accepted base contact now drive `concepts/cast-parts-prompt.md`/`cast-parts-v1.png`. Factual identities are grounded in Objects.json and ReferenceGladePlan; no weapon/armor is baked into these changeable owners. Generated proportions/detail still require exact native reconstruction. Functional anchors and lit-wall/connected boundaries follow; no new fictional backdrop or duplicated keeper/cook is introduced.
