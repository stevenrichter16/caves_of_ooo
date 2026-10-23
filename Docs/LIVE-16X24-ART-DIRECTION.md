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

**User-facing screenshot delivery (requested 2026-10-23):** share actual in-game screenshots inline in this chat as each visible milestone is reached, rather than waiting for the final report or only saving files in the repository. Briefly name the location and visible change. For substantial changes, include comparable before/after captures. Clearly label imagegen concepts, source contact sheets and greyboxes; never present them as finished gameplay. Reuse the verified captures from the production pipeline and provide full absolute image paths for inline display. This reporting preference applies to future agents continuing this session.

**Current handoff:** the opt-in playable glade has accepted native base, cast, functional-anchor, state, motion, loose-item and exact-subject portrait integration. Its connected cellar now adds9 native frames and tested floor/admission, passing714 native cases. Actual cellar Play verifies loot/harvest/dismantle, return/revisit and in-session save/load. A live-discovered portrait color defect is fixed, with9 GPU cases, overlapping348-case regression and exact actual-popup pixel comparison. Earlier glade keyboard/F1/item evidence remains valid. The two exact north graphs now have tested opt-in greybox admission, ordinary border travel/still examine/Nella terms/trade and actual1280×800 captures. Three imagegen concepts/parts references and a measured owner/state plan are saved. The first native north packet now adds Tree/Hedge/BerryBush/DryBrush and reuses ordinary Bush/Reeds, with312 distinct Unity cases and actual entry, hedge collision, berry examine/harvest and plot captures. The next nine-frame [north works packet](LIVE-16X24-NORTH-WORKS.md) is also integrated: still, shelf, three forage sources, magpie, viper and the patchbearer healing-slot pair. It passed 755 selected native cases (69 new) and ordinary travel, combat/Calm, inventory healing, exact-subject examine, finite Stoneburr harvest and two-tonic batch brewing. Nella’s [plot packet](LIVE-16X24-NORTH-PLOT.md) now integrates the unchanged approved keeper, six growth frames, neutral tray and persistent ground cords, with860 distinct final native passes. A second ordinary keyboard visit now verifies the approved keeper portrait, paid rights, rain-fed crop stages, both reserve harvests/pickups, trade return, save/load and border revisit with14 actual captures. Continue direct products next; static new actors and other ordinary-Play coverage remain explicit gaps. East/kitchen remain unadmitted. Complete surface art, reaction/item journeys, release activation and physical Deck acceptance remain outstanding. Generated concepts are reference only. Continue from the records at the end, not historical pending-status paragraphs.

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

### Opening cast completion — bounded implementation plan

Four remaining base subjects: exact blueprint `Villager`, `PetDog`, `MarlbackScrabbler`, `MarlbackGleaner`, each with Creature tag and its own owned visible RenderPart. No glade VisualID override exists on these owners. Their current positions, deaths and visibility come from the simulation, not initial coordinates. Route these identities only inside the already admitted flat opening; named keeper/cook, unrelated raiders, corpses and carried objects retain their existing paths.

Source sweep: Villager has civilian trader/conversation behavior and no authored held weapon; PetDog wanders and can retrieve through allied behavior but is not a guaranteed player follower. Marlbacks are short-faced shale-plated bank burrowers with grasping limbs. Scrabbler has a variable dagger/hatchet/cudgel loadout; Gleaner has dagger/short-sword plus ally/retreat behavior. Current faction is OutlandRaiders; earlier replacement proposals are not authority for adding new faction costume. Do not bake variable equipment or the separate Patchbearer's medical straps into either creature.

Approved concept motifs: sloped civilian shoulders, low canine with negative space under its belly, squat broad scrabbler versus taller asymmetrical reaching gleaner. The imagegen sheet has excess detail, uneven pixels and a somewhat toy-like dog face; reconstruct fewer native clusters rather than downsampling it. Preserve original silhouettes and the accepted restrained base palette. Produce a separate64×24 atlas with four static16×24 frames, in the owner order above; no animation claim.

Integration: extend the existing cached catalog with an optional borrowed cast texture, keeping injected base-only construction explicit and deterministic. Default production construction loads both Resources once. Avoid a second world traversal, per-cell allocation or duplicate actor renderer. Missing/malformed cast resource must preserve known base art and explicit rough cast fallback. Dispose only generated frames/tiles, never either borrowed texture. State/fog/top-owner rules stay before art selection.

Tests first: each exact identity and wrong/absent Creature tag; unrelated identity with a familiar VisualID; hidden/dead/removed or remembered actor; current-owner cell move; distinct frames; shared cache; independent base/cast missing-resource behavior; dispose ownership. Actual resource admission must fail before import and pass afterward. Then real Play capture and ordinary movement/combat observation, inventory return, and isolation cleanup. This slice completes living base silhouettes only; action/directional animation, objects/services and dynamic corpse/drop coverage remain next.

### Functional anchors — bounded implementation plan

Next group follows the actual native opening capture through `concepts/anchor-parts-prompt.md` and `anchor-parts-v1.png`. Keep useful silhouette/state distinctions; reject the generated bevels, shading and excessive pixel density. Reconstruct twelve flat16×24 frames in a192×24 atlas: cracked/sealed well, filled/empty basin, stairs, beam, closed chest, barrel, sign, mushroom ring, quartz vein and lit masonry. State pairs share their outline and differ only where the actual mechanic changes it. Wicket, campfire, Drawgourd, roads/water boundaries and dropped objects stay separately scoped.

Verification sweep corrections and exact APIs before implementation:

| Owner | Admission/state | Countercase |
| --- | --- | --- |
| RepairLinedWell | Owned WellPart + nonportable RepairablePart, clay-well-lining recipe; Repaired selects0/1 | Other wells, missing/foreign part, wrong recipe retain fallback; no health or unlimited-water inference. |
| SpreadDrawPoint | Owned LiquidPoolPart with LiquidId water; Volume>0 selects2, depleted selects3 | Nonwater or malformed source must not advertise clean water. Existing permanent coating remains independent of volume. |
| StairsDown | Owned StairsDownPart, frame4 | No closed hatch state; actual destination is registered ZoneConnection. |
| FallenBeam | Owned PhysicsPart + HandlingPart, frame5 | Solid one-cell haulable beam; not a throwable item or fixed backdrop. Current cell owns all art. |
| Chest / WoodenBarrel | Owned ContainerPart, frames6/7 | No persistent open-lid state. Contents emptied does not mean broken/open. Removed or AshPile owner must not retain container image. |
| Signpost | Exact blueprint, frame8 | Neutral board, no invented travel-direction arrow. |
| MushroomRing | Owned unspent HarvestablePart yielding Mushroom, frame9 | Harvest consumes source; a retained spent source must not display ripe caps. Passable owner remains passable. |
| GlowQuartzVein | Exact blueprint + lit-wall VisualID + owned unspent HarvestablePart yielding GlowQuartz, frame10 | Dropped quartz and ordinary Wall sharing the VisualID must differ; consumed source disappears. |
| Wall | Exact blueprint + lit-wall VisualID, frame11 | Never a harvestable mineral sprite. Destruction follows replacement Rubble. |

Both quartz and lit walls have real LightSourcePart and ReferenceGladeQuarterTurns in the builder. Keep restrained color; no baked halo. This flat group intentionally uses a neutral front-facing frame, not a90-degree rotation that distorts a16×24 cell. All routes remain below existing top-owner/visible/remembered policy. Reuse optional borrowed atlas and cached frame lifetime; missing/malformed anchors cannot disable base/cast or mutate state. Native RED first for both state pairs, foreign/missing parts, shared VisualID collision, harvest/removal, independent fallback/disposal and actual resource binding. Review source art, then import and focused GREEN, actual normal-input Play/repair/water/readability checks as feasible, record limits and cleanup.

### Opening cast completion — integrated, 2026-10-23 UTC

Four static native frames now represent the actual Villager, PetDog, MarlbackScrabbler and MarlbackGleaner. The current native-game capture plus base-art contact drove the saved imagegen cast prompt/reference, then `Tools/Live16x24/build_opening_cast.py` rebuilt original flat geometry. `ArtSource/Live16x24/cast` contains editable Blender source, exact64×24 atlas, manifest and native/contact verification. The dog is14×11; the squat scrabbler is14×15 and the reaching gleaner13×20. These are real silhouette differences, not palette swaps. All five approved PNGs and the prior base atlas remain unchanged.

Native tests: `opening-cast-red.xml` records79 cases with19 expected new API failures and60 passes (including the already-correct base-only countercase). Synthetic GREEN79/79 followed; real missing cast resource RED80 cases with79 passes and1 expected failure preceded import. Final `opening-cast-integration-green.xml` is **146/146, zero skipped**:21 new cast cases + the prior125. Tests cover exact identity/tag, foreign RenderPart, unfamiliar/corpse identity, current-owner movement/removal, real HandleDeath, memory/visibility, independent missing/malformed groups, caching, borrowed-texture disposal and actual four-frame default Resources binding.

Actual Play: N fresh game, all seven living owners visible (player/warden/villager/dog, two scrabblers, one gleaner); ordinary L/arrow targeting selects the actual scrabbler at41,20 with the red outline on its native sprite. Fourteen wait turns run ordinary NPC movement/combat: gleaner dies and disappears, dropped gear remains as explicit fallback items, dog and surviving actors move with their sprites. Inventory open/close returns to the same native world at40,12. Captures `native-cast-start.png`, `native-cast-target.png`, `native-cast-after-turns.png` and read-only owner/geometry receipts are real1280×800 Game output. `native-cast-cleanup.json` verifies original study restored and isolation ended.

Cold code review plus independent native art review found no P1/P2. No gameplay rules, spawn positions or item loadouts changed. Renderer art remains cached; this group introduces one small shared texture, no actor GameObjects or additional world scan. The repeated cast now distinguishes people from animals and mineral-backed enemies at native size.

**Can verify:** actual actor resource binding, state/visibility/lifetime counterchecks, keyboard targeting, ordinary turns/death/drop disappearance and UI return. **Cannot yet verify:** action/directional animation, equipment overlays, final corpse/drop sprites, mouse/controller acceptance or Steam Deck performance. Cast base coverage is complete for this opening's initial living owners, not the whole game's creatures.

Anchor pre-implementation correction: `RememberedTerrain` excludes CropPart, FieldHarvestPart, LiquidPoolPart and RepairablePart, but does not yet exclude HarvestablePart. Native mushroom/quartz RenderLayer values normally hide this gap; a low-layer/tagged harvest source could be admitted as remembered terrain and reveal its current spent state. Add a failing low-layer harvest-source memory countercase and narrowly exclude HarvestablePart before anchor state routing lands. This is a visibility guard correction, not permission to reveal or snapshot live harvest state through fog.

### Next motion seam — source audit, not implemented

A bounded future slice can reuse existing pooled views and real MovementResolved/melee/damage hooks for player, dog and marlbacks. It cannot simply enable the old actor renderer: `AnimatedEntityRenderer.SyncZone` performs a whole-zone scan, `EntityVisualCatalog` requires a64×384 sheet, and the flat branch in `EnvironmentSpriteRenderer.ResolveCell` currently returns before old actor suppression. The next design should admit compact native frames and observe actors from already visited cells, suppress static bodies only after a valid view accepts them, then reconcile tracked views. No padded fake sheets, duplicate bodies, extra world scan or gameplay timing change.

Author two real walking poses, one attack pose and one hurt pose per selected actor after its base silhouette is accepted; reuse static idle. Explicitly handle bottom-center actor pivots versus centered tiles, flat unlit material/no legacy shadow, fog/removed actors/hidden dying views, forced relocation, interrupted movement and bounded reactions. Existing accepted melee-attempt hooks include misses; damage feedback must follow committed damage. Test blocked moves, misses, zero damage, immediate retaliation, transition/pause and unknown-asset fallback. This is a future proposal; no animation has shipped in these static groups.

### Remaining opening state subjects — source/design plan

The five remaining initial owner types need twelve static state frames. New generated reference is `concepts/state-parts-v1.png`, exact prompt in `state-parts-prompt.md`, based on actual `native-cast-start.png` plus accepted native base style. Reconstruct flat native clusters; reject generated shading and irregular pixels. Atlas order:0 buckled wicket,1 repaired closed,2 repaired open;3 cold campfire,4 hot coals,5 actual flame;6 gourd seed,7 sprout,8 ripe vessel-shaped gourd;9 unlit torch,10 lit torch;11 waterskin. Imagegen is reference only; all current base/cast/anchors and approved source PNGs remain unchanged.

Source corrections before implementation:

- `GleanersBuckledWicket`: repair-blocked state before `DoorPart.IsClosed`; repair never means open. Destruction removes the owner and leaves a gap, no permanent painted wreck. Require actual repair/door state.
- `Campfire`: initial thermal500°, fuel200, no BurningEffect → hot coals, not flame. BurningEffect is actual fire; otherwise finite temperature at/above `CookingService.MinimumFiniteCookingTemperature`150f → hot coals; below threshold → cold. Hot art does not promise cooking service, which separately requires fuel. Relight raises thermal450° without applying BurningEffect. FuelExhausted has no source consumer; do not promise automatic AshPile replacement.
- `DrawgourdCrop`: owned GrowthStage0/1/2; stage2 requires HarvestAtMaturity. Initial plant ripe. MoistureTicks selects existing wet/dry ground, not extra sprites or invented withering. Harvest removes the plant and gives shell+seed; prepared bed persists. Products remain separate coverage entries.
- `Torch`: real controlled ground/equipped light uses TorchLightPart and LightSourcePart.Enabled plus actual flame/fuel/temperature rules; destructive BurningEffect is independent fire. A lit blueprint name or temperature alone is not sufficient. Use/reuse authoritative predicates rather than diverging duplicated thresholds. Pickup removes world art.
- `Waterskin`: one soft leather vessel; charges change on retained owner, and emptying does not remove it or become a terrain water source.

Two material runtime risks must be tested first: Campfire is currently low Terrain and can pass remembered-terrain filtering, so exclude live heat/fire state from fog memory. Torch switching bumps EquipmentChangeBus without directly dirtying its cell, and burning transitions also need a stationary/wait-turn redraw check. Correct only the affected presentation invalidation seam if RED confirms a stale sprite; do not depend on the player moving. Native tests need state boundaries, missing/foreign part fallback, real toggle/thermal transition redraw, removal, memory and actual resource binding. Shared cached atlas and existing rendering traversal remain requirements. This group is planned, not integrated yet.


### Functional anchors — integrated, 2026-10-23 UTC

Twelve native frames now cover the cracked/sealed repair well, filled/empty basin, cellar steps, haulable beam, chest, barrel, sign, mushroom ring, quartz vein and lit masonry. The actual cast-start capture drove the saved anchor imagegen concept; two Blender passes rebuilt it as deliberate native pixel geometry. The second pass shortened stairs into a ground opening and tapered the barrel away from the chest silhouette. State pairs retain identical outlines. Editable source, pixel cells, manifest and read-only verification live in `ArtSource/Live16x24/anchors`; the192×24 Point/Clamp/no-mip/uncompressed texture is imported under Resources. Approved five sprites and prior base/cast PNGs are unchanged.

Native evidence: `opening-anchors-red.xml` ran118 cases,82 passing and36 failing:34 missing atlas API cases plus2 genuine remembered-harvest leaks. The narrow fix excludes HarvestablePart before remembered-owner selection. Synthetic GREEN118/118; actual-resource RED119 with exactly1 missing imported atlas failure; final `opening-anchors-green.xml` **185/185, zero skipped**, including39 new anchor cases and the prior146 controls. All twelve real-resource frame rectangles, well/basin pairs, blueprint-first quartz/wall distinction, foreign/missing parts, removed/harvested sources, independent missing atlases and borrowed-texture lifetime are covered. Root self-review and two independent code/art reviews found no P1/P2.

Actual ordinary-input Play evidence:

- Fresh N at40,12; `native-anchors-start.png` is genuine1280×800 Game output. Wells, basin, signs, chest, barrels, mushroom ring and quartz are readable within their real cells. Lit masonry now connects vertically rather than showing the old rough horizontal bars.
- Walk to37,10, G picks up the actual waterskin. Inventory → waterskin → fill drains all three drams from36,9 and closes back to the world. `native-anchors-water-state.json` records source Volume0 and retained waterskin Charges3; `native-anchors-water-empty.png` shows the empty bowl while surrounding wet ground remains.
- Walk to32,10, L/Left/Return/H harvests31,10 quartz. The source sprite disappears, inventory gains GlowQuartz, ordinary adjacent Wall31,11 remains and the other vein31,22 is still unharvested. Actual capture/receipt: `native-anchors-quartz-harvest.png` / `native-anchors-quartz-state.json`.
- Walk through the open y16 ruin passage, inspect the real cellar stairs28,19 with L/arrows. `native-anchors-cellar-target.png` verifies outline/owner alignment. The existing worn crossing28,17 correctly blocks direct movement; this check did not descend into the cellar.
- Console UI during Play showed0 errors and2 existing Metal depth-surface warnings. Final cleanup receipt confirms Play false, original study scene restored, pending/isolation false.

**Can verify:** native state/fog/lifetime counterchecks, real imported art, actual basin depletion and quartz removal, keyboard target alignment, collision, fullscreen return and isolated cleanup. **Cannot yet verify:** real repair performed during this slice, full cellar/travel/save journey, complete motion/ground/item art, mouse/controller acceptance or Deck performance. Repaired well imagery is native-test verified; no repair playthrough is claimed. This remains an opt-in development presentation.

### State integration — stationary-redraw audit addendum

Independent source audit confirms thermal150° campfire cooling already dirties its exact owner cell. Preserve that implementation. Torch toggle/exhaustion currently changes light/heat and EquipmentChangeBus but does not dirty the loose owner's foreground. BurningEffect application/removal already emits owner notifications, but aura updates do not repaint the static tile. Write RED around the ordinary renderer frame with the player stationary, then narrowly invalidate the current loose torch cell on successful toggle/exhaustion/rollback and let the finite campfire/torch observe their own Burning transitions. Test rejected/unchanged actions, removed/foreign owners and hidden changes. Do not add a world scan or unconditional turn redraw. The twelve-state Blender contact passed root and independent visual review; actual hot-coal contrast remains an acceptance gate.

### Eventual ordinary-release activation — audit, not implemented

Normal SampleScene currently has no serialized flat-mode enablement; bootstrap binds camera metrics but does not enable it. Only the isolated editor launcher enables it transiently. Builds therefore include these atlases/code but still launch the previous presentation. F11 can leave flat mode but cannot enter; Graphics has no presentation choice. A saved SpriteEnvironmentEnabled=0 also vetoes flat admission. These are explicit remaining release requirements, not hidden defaults.

After coverage/visual gates, add a persisted runtime Pixel/Previous presentation choice through the ordinary Graphics controls and initialize it on fresh/load bootstrap without forcing seed64 or relocating players. Resolve sprite-toggle precedence and restore existing3D preferences on return. Cover first launch, saved opt-out/restart, both bootstrap paths, sprites-off preference, menu/F11 no-turn behavior, foreign-zone exit/re-entry twice and existing setting preservation. Finally run ordinary launch and packaged-build acceptance separately from editor isolation. Keep current authority checks and extend regional admission only when those owners are covered.

### Residual opening coverage — source audit

The saved native opening census (`flat-mode-green.xml`) contains28 initial top-owner blueprints. With base/cast/anchors plus the pending twelve-state group, all26 foreground identities have an intended route; Grass and RoadStone are background-only. This is an intended-coverage statement until a real resource/fresh-generation test confirms it. Keep the quiet dark green grass. RoadStone can later receive sparse packed-ground fragments; pond coating and cultivated-soil state stay independent and must not become painted resource sources.

Next bounded item group, after state/motion acceptance: Emberwheat, ToastedEmberwheat, DrawgourdShell, DrawgourdSeed, Mushroom, RoastedMushroom, GlowQuartz, HealingTonic, DriedMeat, FireClay, SalvagedTimber and MarlbackCorpse. Their real owners come from harvesting, cooking, starter/chest supplies, the connected cellar and actual deaths. No spawn additions are implied. Draw only loose owners selected by the real top-owner rule, including legitimate overflow; carried outputs need no decorative duplicate. GlowQuartz item has no LightSourcePart, FireClay is matte repair material, and MarlbackCorpse has70% creation chance with no default harvest yield. Preserve these distinctions. Variable dropped weapons/leather remain a separate group driven by the next actual drop capture. Destroyed-wall Rubble can reuse the current rubble frame via exact admitted blueprint identity; it lacks the glade VisualID and currently falls back.

### Opening motion — art production underway

Actual `native-anchors-start.png` plus the accepted base/cast contacts drive `concepts/motion-parts-v1.png`; the saved exact prompt and critique are in `motion-parts-prompt.md`. Useful concept decisions are a committed reach/bite and asymmetric recoil. Reject shaded plates, invented dog detail, oversized poses and nearly duplicate generated walk frames. Author native limb changes from the editable pixel geometry, retaining the original base PNGs.

The new atlas will hold16 genuine derivative cells: walk A, walk B, melee and hurt for Player, PetDog, MarlbackScrabbler and MarlbackGleaner, in that order. Idle reuses each accepted base. The dog has a forward snout/bite; figures have equipment-neutral reach. Two horizontal facing directions may mirror the source deliberately; this does not establish separately drawn north/south animation. Frame admission, pooled rendering and action timing remain unimplemented until the API/source plan and RED tests are complete. Animation art alone cannot close the motion gate.

### Opening motion — verified implementation plan

Independent source sweep of `AnimatedEntityRenderer`, `EntityVisualCatalog`, `EntityVisualHooks`, `EnvironmentSpriteRenderer.ResolveCell` and both `ZoneRenderer` synchronization sites confirms the following corrections. This is CoO-original presentation work, not a claim of copied Qud animation behavior.

| Premise | Verified correction / required behavior |
| --- | --- |
| Re-enable the old actor renderer | Its `SyncZone` scans every entity and the flat branch returns before actor suppression. Admit bodies from the existing cell pass, then reconcile tracked views only. |
| Reuse the existing tile sprites directly | Tile pivots are centered; actors need bottom-center. Create cached actor sprites from the same borrowed texture rectangles, not padded sheets. |
| Accepted combat hooks always show each action | Immediate retaliation overwrites Attack with Hurt in one frame. Preserve a minimum pose exposure with one bounded pending reaction. |
| A new move can always start at its source cell | Interrupted moves currently rewind; consecutive moves must start at the displayed anchor. Attacks interrupting a walk anchor at the authoritative destination. |
| Hidden actor hooks are already guarded | Move/attack/damage/death lack complete presentation guards, and dying views can survive visibility loss. Gate hooks and validate tracked owners before ticking. |
| Using the old body material is harmless | Legacy tint, shadow and generic lunge/shake undermine the native silhouettes. Flat bodies use unlit white tint and no shadow, including state transitions. |

Implementation sequence after the state group is committed:

1. Add a compact `FlatPixelActorMotionCatalog` and narrow frame-reference adapter for `EntityVisualAsset`. Borrow base/cast/motion textures; cache at most20 sprites and4 assets. Reuse base0/cast1–3 idle. Exact player role or exact Creature identity plus owned visible RenderPart determines eligibility. Missing motion preserves static art; a missing base/cast group disables only the affected actor group. Dispose owned sprites only after releasing views.
2. Add `ObserveFlatCell` at the already-resolved cell boundary. Suppress foreground only after a valid body is accepted; retain ground paint. Flat mode bypasses both whole-zone `SyncZone` calls. Reconcile tracked views without an incremental `_seen.Clear()` that would remove unchanged actors outside dirty cells. No sprite creation or zone scan on no-op frames.
3. Preserve100ms walk,160ms attack and140ms hurt durations. Walk A/B split the movement; attack holds its authored pose about120ms and hurt about100ms, each followed by40ms idle recovery. Keep one pending reaction and approximately60ms minimum current pose exposure; coalesce duplicates without restarting time. No simulation delays. Adjacent forced displacement uses idle art on an80ms slide; nonadjacent relocation snaps. Cast remains idle because no casting frames were authored.
4. Validate current zone/cell, top owner, owned visible RenderPart and current explored visibility before ticking and after rendering. Pause/mode/grid/zone/disable clears views and reactions; resume repaints idle normally. A visible death may briefly fade a captured hurt pose only while its cell remains visible; hide on fog, transition or replacement living owner. Ordinary removal releases immediately. Preserve additive subscriptions and destruction cleanup.

RED gates: real resource missing; compact dimensions/rectangles/pivots and distinct walks; exact admission plus missing-group/caching/disposal counterchecks; single accepted body with no static duplicate; incremental untouched owner retention; no-op allocation/scan checks; successful versus blocked movement; consecutive/interrupted/forced movement; melee miss versus veto; positive versus zero damage; same-frame retaliation with bounded completion; hidden/removed/replaced/dead owners; pause/resume and zone/mode transitions twice. Run existing `EntityVisualStateTests`, `EntityVisualCatalogTests`, `RectangularActorProjectionTests`, flat projection/catalog suites and fullscreen ownership controls. Independent review precedes real Play movement/combat/visibility capture. Frame timing and appearance require actual Game inspection; native tests alone cannot close those gates.


### Opening object states — integrated, 2026-10-23 UTC

Twelve native state frames now depict the buckled/closed/open wicket, cold/hot-coal/burning fire, seed/sprout/ripe drawgourd, unlit/lit torch and loose waterskin. The source capture, saved imagegen prompt, two native Blender passes, exact cells and preservation checks are in `ArtSource/Live16x24/states`. Resource import is192×24, Point/Clamp, one mip, uncompressed, SHA256 `9f4697147f14166ca6615672e12b2da0cbfa4a8ebeb70c7072151fca72f4983d`. Previous sprites remain byte-identical.

The optional fourth borrowed atlas extends the catalog without creating sprites on a render. Exact blueprint and owned parts select current state. Fire follows actual owned BurningEffect independently from retained150° cooking heat. Torch reuses the tick's controlled-flame predicate. Campfire/torch current state is excluded from remembered fog. Narrow exact loose-cell invalidations cover actual torch toggle/exhaustion/rollback and owned Burning/Wet/Frozen transitions; existing thermal crossing invalidation is retained. No new world scan, gameplay turn delay or altered fuel rule.

Verification sequence in `Docs/Verification/Live16x24`: initial RED165cases/46fail; renderer cadence RED106/5fail; synthetic GREEN139/139; ownership/resource RED65/6fail (missing resource, foreign Burning and Wet/Frozen repaint); final pre-import RED65/2 missing-resource failures. Actual-resource final GREEN **330/330, zero skipped**, including65 new state/cadence cases and existing gameplay/presentation controls. A generated seed64 opening census confirms all initial foreground owners resolve to the four accepted atlases; Grass/RoadStone remain background-only. This does not claim late-created drop coverage.

Ordinary-input Play: fresh isolated seed64, real keyboard walking and Look/actions. At52,8, extinguish the loose torch at52,7: its sprite immediately loses flame while the player remains still (`native-states-torch-off.png/json`). Taking it removes the ground owner and equips it. Trying to light it beside residual coals correctly fails; that is not a successful relight claim. The opening fire has retained cooking heat without BurningEffect, so its coal image is truthful. Walk around the buckled wicket to46,5; harvest46,4, producing one shell and one seed and removing the ripe crop (`native-states-gourd-ripe.png`, `native-states-gourd-harvest.png/json`). Step onto the bed, take both, use the seed's Plant here action, then step south. `native-states-gourd-replanted.png/json` shows a new stage0 plant with moisture0 in the same prepared bed; the carried shell remains. No state or supplies were injected.

- 🟡 Fixed: loose torch and fire changes could leave stale stationary sprites; the five cadence RED failures and later Wet/Frozen countercases now pass.
- 🟡 Fixed: foreign Burning ownership could select flame. Owned effect and part checks now reject it.
- 🔵 Independent source/cold review and root production diff review found no remaining P1/P2 within this slice. Actual Game output retains current fog and interaction outlines.
- 🧪 Not exercised live here: repairing/opening the wicket, spontaneous fire/cooling boundaries, watering through full crop maturity, controller/mouse input or save/travel. These need later journey checks; applicable static and renderer transitions are native-test covered.
- 🧪 Existing examine illustrations still use the older presentation; native examine parity remains an outstanding release item. Loose harvested items still use explicit greybox fallback pending the next item group.

**Can verify:**330 native Unity tests, real imported frame ownership/cadence/fog, actual keyboard torch extinguish/pickup and harvest/collect/replant,1280×800 Game captures. **Cannot verify:** Deck hardware performance/control feel, complete release journey, whole-region coverage or production readiness. Console UI during Play showed0 errors and3 warnings (two pre-existing Metal warnings plus the rejected torch action); MCP console connection timeout is not an empty-console result. Cleanup receipt `native-states-cleanup.json` confirms Play false, original study restored, pending/isolation false. This remains opt-in development presentation; ordinary builds are unchanged.

Files: `FlatPixelOpeningArtCatalog`, `FlatPixelGreyboxCatalog`, `TorchLightPart`, `CampfirePart`; new `FlatPixelOpeningStatesTests` and `FlatPixelStateRenderCadenceTests` with metas; state Resources atlas/meta; source geometry/generator/import snippet and evidence. Scope matches the twelve-state plan. Next: motion integration from the verified plan above, then loose items using actual harvest capture and `concepts/loose-items-v1.png`.


### Motion and loose items — source-art acceptance

The motion atlas now contains16 native derivative cells (walk A/B, melee, hurt for four actors), with originals preserved. Root and independent reviewers accepted its distinct limb silhouettes after a second cleanup pass. Native/source/contact checks pass; exact hash and semantic constraints are in `ArtSource/Live16x24/motion`. Its runtime work is tracked separately in `LIVE-16X24-MOTION.md`; authored poses alone do not prove timing or gameplay.

The actual gourd-harvest capture drove the twelve loose-item concept. Two native reconstruction passes yielded `ArtSource/Live16x24/items/opening-items.png` (192×24, hash `1ead2618c2d82d2ca7cae8765ab09f690255998ad0bb5b36027d7835fc292394`), editable Blender source/cells and integer8× contact. Root inspected the accepted contact: tiny seed and detached shard differ from their planted/vein sources; the gourd has an open neck, cooked food differs in shape/color, clay stays matte, timber uses split pieces and corpse uses collapsed plates. Read-only native/source/contact matching, binary alpha,3–6color bounds and prior-asset hashes pass. No items are imported or runtime-bound yet. Follow `LIVE-16X24-LOOSE-ITEMS.md` for owner tests and ordinary play gates.

Source-only review: 🔵 native integrity and independent static review passed; 🧪 actual animation timing, loose-item visibility and native examine remain unverified. Native art source and plans can be preserved independently from runtime integration. Additional site scope and honest acceptance journey are in `LIVE-16X24-CONNECTED-SITES.md`, explicitly not implemented. No whole-biome or release claim is implied.

### Motion/item/examine integration — current continuation

The compact motion runtime, twelve loose-item frames and exact native examine portraits are implemented and imported. Final native `opening-items-examine-green.xml` records **629/629 passing, zero skipped**: the motion/state/projection controls,46 item cases,52 new examine/layout cases and96 existing portrait/ledger controls. Earlier missing-resource and ownership RED receipts remain in the dedicated living documents. Native examine borrows the exact authorized subject/state, including carried objects, with aspect-preserving integer fitting; active flat presentation does not instantiate the old 3D preview.

Editor recovery: Unity9009 stopped responding during Mono domain unload after compiling the46-case item fixture; a process sample confirmed the wait. No new test job had begun. SIGTERM and a same-project restart recovered the editor, and its offered scene backup was preserved under untracked `Assets/_Recovery`. Two newly imported examine-test NUnit constraint typos were fixed before accepting compilation; they are fixture faults, not feature RED evidence. Normal imports and MCP reconnection completed after leaving startup Safe Mode. Previously saved native435GREEN and item44resourceRED remain valid; subsequent results must use fresh assemblies.

Current scope remains opt-in development. Ordinary launch/build defaults, connected-six-zone art, complete item/gear coverage and hardware acceptance are outstanding. Do not report the whole overhaul as released from these partial milestones.

Ordinary-input Play now verifies walking, an actual bump attack and existing NPC combat, menu re-entry, starter tonic drop→ground examine→pickup→carried examine, and the real lit torch's native state portrait. Actual1280×800 captures and read-only receipts are `native-motion-*`, `native-items-tonic-dropped.png`, `native-examine-tonic-ground.*`, `native-examine-tonic-carried.*` and `native-examine-torch-lit.*`. The damage-event still shows an attack pose, not hurt; two screenshot requests in that frame yielded only the latter PNG. Independent visual review accepted the captured item/prop proportions. Full reaction sequencing, complete item journeys, controller feel and physical Deck performance remain unproven.

Live review also found inventory F1 details still used the neutral text reader while the actual Examine action correctly showed the native subject. `native-examine-live-red.png/json` preserves that discrepancy. The bounded follow-up adds structured subject identity to that shortcut while preserving its existing decision prose; aggregate pile summaries remain neutral. Follow `LIVE-16X24-EXAMINE.md` for RED, correction and repeat Play evidence before this integration batch is accepted.

The F1 follow-up is now accepted: actual-route native RED22/17fail → GREEN22/22 → affected regression254/254, zero skips. Ordinary N/I/Tab/select/F1 shows the native tonic and preserved decision prose; Right pagination, Escape to the same inventory selection and return to movement work. `native-examine-f1-corrected.*` and `native-examine-f1-world-return.*` preserve actual Game evidence. Final cleanup confirms Play/pending/compile/save isolation false and SampleScene/view restored. No unresolved P1/P2 remained in independent code and captured-image reviews. Native suites are overlapping scopes, not883 unique tests.

That next-packet plan is now superseded by the accepted cellar record below: the census covered all six destinations in two seeds, but admission stayed restricted to glade+cellar. Solid-wall review expanded the initial6/optional8 frames to9. The build default remains the prior presentation until the explicit release gate; the old3D preservation tag and approved source PNGs are retained.

### Connected cellar — accepted bounded packet, 2026-10-23

The real gleaners' cellar now shares the native16×24 presentation. Nine frames cover rising stairs, the supply crate, reclaimable pallet, three sootroot growth stages, harvested pulp, seed and continuous solid masonry. Quiet cool floor replaces generic prop markers. Current owned state, visibility, removal and foreign-art fallback remain authoritative; no supplies, layouts or enemy behavior were changed. Full plan, corrections and acceptance: [LIVE-16X24-CELLAR.md](LIVE-16X24-CELLAR.md).

Pipeline completed: exact scope/floor RED/GREEN → actual detached12-graph census (six addresses, seeds64/65) → ordinary cellar greybox → imagegen reinterpretation and two parts passes → editable integer-cell Blender source/native atlas/contact → actual Resources import → native tests → ordinary gameplay/capture/review. Exact native source and integration assets are saved under `ArtSource/Live16x24/cellar`, `Assets/Resources/Sprites/Live16x24/opening-cellar.png` and the verification directory. Earlier approved PNGs remain byte-identical.

Native verification passed714/714, zero skips (85 new cellar cases plus629 previous controls). Actual Play exposed an extra-gamma examine defect despite those tests. A narrow cached native portrait drawer/resource shader fixes it;9 GPU cases and the overlapping348-case examine/cellar regression pass. An independent comparison of the actual corrected popup finds zero source color/geometry mismatches. Do not sum714 and348 as unique cases.

Ordinary keyboard seed64 verifies both cellar routes: service-passage outbound around the beam; crate emptying, pallet dismantling, ripe sootroot harvest and collection; actual Calm on the approaching guard; broad-corridor return upstairs and descent again. F5 save → move away → F6 load restores the stair tile with28HP, inventory and depleted store preserved. The initial failed return/death and old portrait image remain saved as iteration evidence. Final cleanup confirms stopped Play, no pending compile/session flags, isolated saves released and original SampleScene restored.

**Can verify:** native test contracts, actual palette pixels, readable native cellar silhouettes, ordinary gameplay/return/revisit and in-session save/load at1280×800. **Cannot verify:** physical Deck graphics/controls/performance, fresh-process load, complete crop maturation, every equipment drop or normal packaged activation. At this cellar checkpoint, the opt-in development view covered two zones. The later north foundation below supersedes that scope; surface art, overall release journey and profiling remain unfinished.

**Historical next packet, superseded by north foundation below:** north connector `Overworld.11.9.0` and Nella's seed plot `Overworld.11.8.0`, using the saved actual two-seed census. Audit ordinary vegetation and actors as well as the destination props. Start exact admission/counter-tests and an ordinary playable greybox; capture it before imagegen/native art. Bind the approved keeper without moving or duplicating the real resident. Design the persistent reserve-soil cue through the existing cell pass, retaining claim visibility after harvest without a frame-time zone search. Leave east connector/kitchen unadmitted until their own accepted packet. Continue with all available tools under the user's delegation, preserving this required production pipeline.


### North route foundation and captured concepts — 2026-10-23

The next two exact managed graphs, `Overworld.11.9.0` and `Overworld.11.8.0`, now remain in opt-in16×24 presentation. This is **greybox admission, not completed north art**. Current graph/map/biome/canonical address and each site's null-POI checks preserve old glade/cellar authority and reject east/kitchen, neighbors, depth aliases and replaced graphs. No generation, spawns or gameplay mutation was added. Removing a service/keeper/crop does not revoke the graph's style.

Native RED95 cases:70 passed,25 expected missing-admission failures. GREEN95/95 plus117/117 bounded fullscreen/motion/portrait/ground consumers =212 distinct passing cases,0skips. Independent scope review found no actionable P1/P2. Receipt prefixes: `north-scope-red`, `north-scope-green`, `north-scope-regression`.

Actual ordinary keyboard seed64 journey went from glade to north works and Nella. It exercised still selection/examine, real blocking walls, Calm after a patchbearer attack, both north borders, tied-row terms, a finite two-seed purchase and full-screen trade/world restoration. Seven real1280×800 PNGs and sibling read-only receipts are saved under `Docs/Verification/Live16x24/north-greybox-*`; the actual plot image was shared inline in chat as requested. Player health ended25/40 after the real encounter; no safety injection or teleport was used. Isolated cleanup restored clean SampleScene and all pending flags.

Actual captures fed built-in imagegen plot/works concepts and16-part extraction, saved with exact prompts in `ArtSource/Live16x24/concepts/north-*`. The extracted sheet has non-native detail and imperfect alpha; it is reference only. Production must reconstruct integer-cell sprites, preserving approved actor PNGs and real owner coordinates. The concepts' boxed borders, extra foliage and changed keeper details are rejected for direct adoption.

Historical foundation continuation (superseded by the vegetation record below): read [north route](LIVE-16X24-NORTH-ROUTE.md), [measured contracts](LIVE-16X24-NORTH-CONTRACTS.md) and [parts/critique](../ArtSource/Live16x24/concepts/north-parts-plan.md). Its first native surroundings packet, especially Tree/Hedge and deliberate Bush/Reeds reuse, has now landed. Then works/actual actors, Nella/crops/tray and indexed persistent-soil cords, then direct products and the merchant crop follow-up. Use these valid greyboxes instead of another scope/census-only packet. Repeat actual routes after integration and show before/after screenshots. No north art, claim cue, growth/revisit, renderer profile, Deck acceptance or packaged release is claimed by this foundation. East/kitchen and overall release remain outstanding.


## 2026-10-23 continuation — native north vegetation

The [vegetation record](LIVE-16X24-NORTH-VEGETATION.md) completes the first native art packet after the accepted north greybox/concepts. Four original16×24 frames cover Tree, Hedge, BerryBush and DryBrush; ordinary Bush/Reeds reuse existing base frames. Named editable Blender geometry, exact source cells, direct-rendered native/contact art and a read-only verifier are retained. Approved sprites and earlier atlases remain byte-identical. No gameplay owners, layouts, yields, collision or sight rules were changed.

Native test sequence:49 expected RED failures before production,48 passed plus one missing-resource failure before import, then204+108=**312 distinct GREEN Unity EditMode cases**. Ordinary seed64 travel checked matching entry views, actual hedge collision/openings, berry examine portrait and finite harvest/removal, then the connected Nella plot. Seven actual Game screenshots and raw owner/inventory receipts live under `Verification/Live16x24/north-vegetation-*`; the entry comparison was shared inline with the user. Final cleanup restores SampleScene and clears isolated/Play flags.

The native profile retained73,154 samples without wrap; its ZoneRenderer mean was0.077379ms and p990.133459ms, with a28.004833ms maximum. Baseline retained only its last16,384 samples after wrap, and the live combat/positions differed. These are bounded editor observations, not a controlled speedup/no-regression claim or Deck acceptance. Full raw bounds and unresolved UI focus refresh are in the packet record.

**Historical next slice, now completed by the works record below:** native north actors and field-alembic works/forage/shelves. The current next slice is Nella's approved sprite, crop states, neutral tray and persistent reserve-ground cords, then products. Use real owners/parts, exact-subject portraits and ordinary-input before/after captures. Do not redo vegetation or restart the valid greybox. North full coverage, east/kitchen admission/art, reaction/item journeys, growth/save/load, normal release activation and physical Deck tests remain outstanding; the finite seven-milestone brief is not complete.


## 2026-10-23 continuation — native north works and creatures

The [works record](LIVE-16X24-NORTH-WORKS.md) completes the next bounded packet from the valid north greybox and saved imagegen/parts references. Nine original 16×24 frames now bind the real still, empty shelf, Stoneburr/FrostLichen/Mendleaf, Magpie, Viper and Patchbearer healing-slot pair. Editable Blender geometry, direct native/contact renders and exact cell verification are retained. Lowering and broadening Mendleaf addressed an actual silhouette problem. Approved assets and prior presentation remain preserved.

Native RED exposed quiet medicine-state staleness and explicit-reskin overwrite; the minimal fixes reconcile previously painted carriers and refuse those reskins. Final selected Unity run: **755/755, zero skipped, including 69 new works cases**. Five lifecycle/allocation additions were initially passing characterization checks. The suite covers actual default resources, submitted tiles, world/portrait ownership, finite removal, native self-treatment and quiet transfer/refill. Final independent review found no remaining P1/P2.

Seven actual 1280×800 screenshots and read-only state receipts are under `Verification/Live16x24/north-works-*`. Ordinary seed64 travel reached the works, where the real patchbearer reduced the player to 4 HP. Calm and a starter tonic stabilized the run at 23 HP. Real Stoneburr harvest removed its source and yielded two seeds; the actual still brewed two stoneskin tonics (`BrewedTonic` instances). Exact plant, still and patchbearer examine portraits were inspected. The patchbearer still carried its tonic in this playthrough: self-treatment/depletion/refill is native-test evidence, not an ordinary-Play claim. Actual images were shared inline. Cleanup restored stopped SampleScene and cleared isolation/pending flags.

A 69.4-second unwrapped editor profile retained 47,866 samples: renderer mean 0.077108 ms, p99 0.172708 ms, maximum 30.997417 ms. Whole-editor maximum Main Thread was 1197.081958 ms; the window includes tool/idle time. These are not a controlled before/after speedup or Deck result.

**Next unfinished slice:** Nella's unchanged approved Seed Keeper 3, six real crop growth frames, a neutral tray and validated reserve-soil cords. Read the source corrections in the works record before implementation: reserve crops lack Vegetation tags, public versus reserve maturity differs, wet-soil cues must survive covering actors, and cords need indexed current claim validation without a whole-zone frame scan. Reuse the existing captures/concepts; add actual watering/growth, permission-preserving harvest/tray and revisit evidence. Products and purchased-crop follow-up, incidental surroundings, new creature motion, east/kitchen admission/art, complete release journey, normal activation and physical Deck acceptance remain unfinished. Keep the heartbeat active; the finite seven-milestone brief is not complete.


### North plot integration —2026-10-23

[Nella’s plot record](LIVE-16X24-NORTH-PLOT.md) is the current handoff. Eight new native atlas cells plus the byte-identical approved keeper are integrated; real crop lifecycle, reserve-ground ownership, moisture under actors, incremental/full clears and allocation counters are covered by104 new tests. Final disjoint767 rendering +93 domain cases pass. The actual fresh journey reached Nella with40HP; [arrival](Verification/Live16x24/north-plot-arrival.png) shows the art in the live graph. Computer lock prevented subsequent ordinary service/crop acceptance; finish those checks before products. The final60.755s profile is unwrapped **idle editor** evidence only. The isolated session was stopped/restored. This advances connected-site implementation, not full milestone6/7 or release/Deck acceptance.


### North plot ordinary acceptance —2026-10-23 13:05 UTC

The earlier lock boundary is closed in [the plot record](LIVE-16X24-NORTH-PLOT.md). The real starter WateringGrimoire supplies Conjure Rain; no waterskin was assumed or inserted. Paid rights, wet soil, actual0→1→2 Marlroot growth, gladroot auto-yield, both reserve harvests and material pickup, neutral cords after depletion, fullscreen trade return, F5/F6 and actual border exit/revisit now have14 new1280×800 captures. Prior860 native passes remain the implementation evidence; this continuation is evidence/docs only. The active60.915-second editor profile includes actual harvest/pickup/menu/walking input and retains its23.88ms renderer maximum and893.89ms editor main-thread maximum. No Deck or controlled-speedup claim. Final stopped scene/isolation/view flags are clean, including editor-only restore after the Mac locked again during cleanup.

**Next:** native direct-product art, beginning with the clods/resin/seeds now visibly produced by the plot. Remaining products, merchant-added crop families, incidental owners, north motion, east/kitchen, complete release journey and normal/Deck activation are unfinished; the heartbeat stays active.


### Eight plot products — native integration, ordinary Play pending

[The plot-product record](LIVE-16X24-PLOT-PRODUCTS.md) is the current handoff.
Eight original loose-item sprites now cover gladroot, four planting seeds,
raw marlroot clod, pitchpod resin and inert compost. Actual harvest imagery
fed the saved imagegen parts target; native editable geometry reconstructs
truthful compact silhouettes. Approved actors/earlier atlases are preserved.
One independent optional atlas follows exact real item semantics through
existing ground and authorized carried-examine paths. Real planting selects
the existing stage0 mark; clod preparation selects existing FireClay art.

Native RED65 (8passed/57expected failures) → GREEN65/65, plus285 disjoint
existing regression cases: **350 distinct passes**,zero skipped. Static
source/import checks and independent cold review pass. An80,000-call warmed
selector observation used0 managed bytes,98.0444ms total; this is not a
frame-time or Deck benchmark. No new gameplay/normal-build activation.

The Mac is locked, so ordinary input and new Game screenshots were not
attempted by substituting state injection. Final editor state remains
stopped, original SampleScene, no pending/isolation/view flags. **Next:**
close the new live harvest/pickup/prepare/plant/examine capture gate when
input is available, then remaining works products. Do not repeat source
authoring or count a source contact as gameplay. Purchased crops, incidental
owners, motion, east/kitchen and release/Deck gates remain open. Keep the
heartbeat active; the finite seven-milestone brief is unfinished.


### Seven works products — native integration, both live gates pending

[The works-product record](LIVE-16X24-WORKS-PRODUCTS.md) is the current
handoff. Seven original16×24 cells cover StoneburrSeed, FrostLichen,
MendleafSprig, WildBerries, GoldCoin, neutral BrewedTonic and the previously
omitted ConcentratedMendleaf. The already verified works harvest/brewing
captures fed imagegen, then editable native source, independent review,
exact-byte Unity import and semantic integration. No scene supplies or
gameplay were changed. Explicit Food/unknown brew forms retain fallback;
existing healing-only Food metadata loss is recorded, not silently fixed.

Native RED58 (4pass/54expected failures) → GREEN58/58 plus338 disjoint
regression cases = **396 distinct passes**,zero skipped. Default resource,
real harvest→pickup, finite coin conversion, preparation, actual brewing,
authorized portraits, fallback and disposal are covered. Native source
verification and independent cold review found no remaining P1/P2.

The Mac remained locked. Advancing this independent source/integration
packet before the plot-product live gate closed is an explicit scheduling
divergence; **neither new-product packet has ordinary Play acceptance**.
After the tests passed, Unity's tool connection dropped: selector timing
and final editor-state readback failed. Preserve those failure receipts;
there is no new performance or successful cleanup readback claim. No Play
session was launched this packet. Twelve import-meta whitespace changes
were restored only after exact linewise equivalence checks.

**Next:** recheck the editor connection/state and ordinary input. Complete
the plot-product harvest/pickup/prepare/plant/examine route, then the works
forage/brew/coin route, with actual Game screenshots and a bounded active
profile. Do not add more unverified areas first. Merchant crop families,
incidental owners, new north actor motion, east/kitchen, normal activation
and physical Deck acceptance remain open. Keep the heartbeat active: the
finite seven-milestone brief is not complete.


**22:47 heartbeat recovery:** Unity tools respond again and a fresh readback
confirms stopped SampleScene with all pending/isolation/view flags clear.
The saved works selector probe completed70,000/70,000 cached matches in
655.6434ms with0managed bytes. This is stopped-editor detached selection,
not rendering/Deck performance or an improvement comparison. Separate
`works-products-*-reconnected.json` receipts preserve the earlier failures.
No source or gameplay was changed and no tests were rerun. The Mac is
still locked; the plot-product and works-product ordinary routes remain
the next gates. No new art areas were started.
