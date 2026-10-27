# Live 16×24 art direction and integration

Current status, 2026-10-26: the En / Garde hotbar correction now ships in fresh
Mac/Linux packages from `3aca7c18e`. Ten actual Mac captures verify mouse
activation/cooldown refusal, keyboard AIM/cancel, inventory and full restart.
The earned Nella resin→North still→East forge route also passed:32 actual
captures and26/26 independent saved-state checks, including full restart.
The same equipped sword has one Burning temper; one resin remains. See
[package delivery](GRAPHICS-G3-HOTBAR-DELIVERY.md) and
[earned route](GRAPHICS-G3-EARNED-RESIN-TEMPER.md).
The suspected coating portrait gap was resolved by audit: it is the approved
shared native flask, correctly mapped. Keep it stable and continue whole-scene
acceptance. Full opening/regions, Linux execution, physical Deck and sustained
performance remain open. Normal saves/preferences, prior builds and34 foreign
edits are preserved; storage remains inspection-only.

User-requested hourly reporting is active: list work completed/in progress/blocked and concrete coming-session goals, plus test/capture/push status. Last report checkpoint: 2026-10-26 22:40 UTC. See the [complete graphics-run change inventory](GRAPHICS-RUN-CHANGELOG-2026-10-26.md). Intermediate ten-minute storage checks remain quiet and inspection-only.

Original implementation brief, 2026-10-22: this supersedes the crop-only next step in the overnight clearing plan. The user clarified that the intended result is a striking visual transformation fully integrated into the playable game, and explicitly allows the time each sprite and animation needs. Asset counts, arbitrary iteration totals and an overnight deadline are not completion criteria.

**Delivery order established 2026-10-23, still active:** read [Graphics completion plan](GRAPHICS-COMPLETION-PLAN.md)
and the [coverage ledger](../ArtSource/Live16x24/release-coverage.json) after this
pipeline. The user's latest priority is to finish consistent graphics and
unblock normal play. **Graphics is the game's #1 priority and a release blocker,
without an imposed deadline.** The user delegates decisions beyond individual
prompts and asks for continued work across sessions. The new plan supersedes
the historical next-packet order:
normal-launch 2D continuity first, then scene-level opening finish and the
complete six-zone journey. Existing evidence and pending ordinary-Play product
gates remain valid; no release gate is waived. The initial audit found the old
presentation starting by default; G1 now fixes ordinary editor launch and travel.
Read the latest continuation below for the current accepted scope.

## What completion means

A coherent playable opening: original characters, terrain, architecture, props, items, animation and feedback share one visual language. The real player can move, fight, inspect, use services and change the world; the art responds to those changes. The user-approved native 16×24 characters are the anchor. Existing 3D art and its return path remain recoverable.

The earlier standalone clearing is a static asset study. Its seven passing tests establish scene routing and asset persistence, not live integration. Do not present that study, an imagegen render or a crop-only reskin as completion of this goal. The later opt-in live integration is recorded below; it does not yet complete the visual release.

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

**Historical handoff, retained and superseded by the dated continuations below:** the opt-in playable glade has accepted native base, cast, functional-anchor, state, motion, loose-item and exact-subject portrait integration. Its connected cellar now adds9 native frames and tested floor/admission, passing714 native cases. Actual cellar Play verifies loot/harvest/dismantle, return/revisit and in-session save/load. A live-discovered portrait color defect is fixed, with9 GPU cases, overlapping348-case regression and exact actual-popup pixel comparison. Earlier glade keyboard/F1/item evidence remains valid. The two exact north graphs now have tested opt-in greybox admission, ordinary border travel/still examine/Nella terms/trade and actual1280×800 captures. Three imagegen concepts/parts references and a measured owner/state plan are saved. The first native north packet now adds Tree/Hedge/BerryBush/DryBrush and reuses ordinary Bush/Reeds, with312 distinct Unity cases and actual entry, hedge collision, berry examine/harvest and plot captures. The next nine-frame [north works packet](LIVE-16X24-NORTH-WORKS.md) is also integrated: still, shelf, three forage sources, magpie, viper and the patchbearer healing-slot pair. It passed 755 selected native cases (69 new) and ordinary travel, combat/Calm, inventory healing, exact-subject examine, finite Stoneburr harvest and two-tonic batch brewing. Nella’s [plot packet](LIVE-16X24-NORTH-PLOT.md) now integrates the unchanged approved keeper, six growth frames, neutral tray and persistent ground cords, with860 distinct final native passes. A second ordinary keyboard visit now verifies the approved keeper portrait, paid rights, rain-fed crop stages, both reserve harvests/pickups, trade return, save/load and border revisit with14 actual captures. Continue direct products next; static new actors and other ordinary-Play coverage remain explicit gaps. East/kitchen remain unadmitted. Complete surface art, reaction/item journeys, release activation and physical Deck acceptance remain outstanding. Generated concepts are reference only. Continue from the records at the end, not historical pending-status paragraphs.

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

### Graphics completion reset — user-directed priority, 2026-10-23

The user identified unfinished/inconsistent graphics as the main blocker to
the game. [The completion plan](GRAPHICS-COMPLETION-PLAN.md) now owns delivery
order, iteration/acceptance rules, researched workflow and remaining milestones.
The native art already exists in four opt-in graphs; normal launch and travel
continuity remain unresolved. This is the principal delivery correction, not
another art-style restart. Preserve approved artwork and the previous mode.

Independent source audits cover activation/fallback and the production path.
The new [initial ledger](../ArtSource/Live16x24/release-coverage.json) separates
integrated art, ordinary-play evidence, unknown coverage and remaining release
gates across the six-zone journey. It does not claim an exhaustive census.

Ordinary keyboard input started a fresh isolated seed-64 run, traversed the
north connector and reached Nella's plot at `(20,10)` with 40 HP. The actual
[1280×800 baseline](Verification/Live16x24/graphics-finish-baseline-plot.png)
shows the current scene, with a [read-only state receipt](Verification/Live16x24/graphics-finish-baseline-plot.json).
The latest product harvest/prepare/plant/brew gates were not exercised and
remain pending. No gameplay/rendering code changed or native tests were rerun.
The current screenshot is evidence to critique, not final visual acceptance.

Cleanup stopped Play, restored the original SampleScene and prior 2× editor
view zoom, then explicitly completed the stopped-session restore when the
delayed callback remained pending. A readback confirmed Play/pending/save
isolation/view flags all false. Import metadata may be restored only after
linewise whitespace-equivalence checks. The next implementation is G1's
identity-preserving fallback, global projection continuity and reversible
normal activation, tested before enabling the default. The heartbeat remains
active; the original finite opening brief is unfinished.

### G1.1 foundation verified — next: live rendering

The [fallback catalog slice](GRAPHICS-G1-PRESENTATION.md) now has actual
corrected RED (40 cases) followed by40 focused and90 disjoint regression
passes in Unity. Two independent source reviews found no blocking issue.
It preserves actual world glyphs/colors in cached16×24 frames with canonical
ownership, fog and lifetime checks. This is not new regional artwork and is
not yet connected to the live renderer; normal launch still uses its earlier
behavior. Continue G1.2's shared selection, travel continuity, event color and
hazard work, then G1.3's reversible default/menu. Do not count these native
catalog tests as actual gameplay or physical Deck acceptance.


### G1.2/G1.3 ordinary delivery accepted — 2026-10-24 UTC

[Continuous rendering](GRAPHICS-G1-RENDERER.md) and
[normal launch/controls](GRAPHICS-G1-LAUNCH.md) now connect the existing
native art to the ordinary SampleScene/GameBootstrap path. Fresh settings
choose flat16×24; explicit Previous preserves the older graphics. Graphics
menu/F11 share the mode choice, and inventory temporarily borrows square
cells then restores the rectangular world. Current graph/owner/fog rules
and one Render event per painted visible owner also apply to glyph fallback.

Actual ordinary-input seed64 evidence covers fresh New Game without a forced
art override, a Previous→flat menu round trip without advancing tick10,
inventory/Look, west travel into an unadmitted art zone and return, F5/F6
from changed position/health, and fresh-session C Continue from that actual
private save. See [normal start](Verification/Live16x24/g1-normal-start.png)
and its raw receipts. Ten new PNGs are1280×800; early script Screen reports
2321×1139 are retained verbatim, not represented as physical Deck evidence.
The outside zone remains glyph-based where native identities are missing.

Native corrected renderer/settings foundation passed52/52 before launch
production. Launch/override RED recorded23 failures; final targeted run is
126/126. Across the separately run renderer regression and focused repairs,
758 current distinct tests have passing latest results; the renamed obsolete
scope pin and corrected fixture assumptions are explained in the raw
[union receipt](Verification/Live16x24/g1-final-test-union.json). This is not
one all-project sweep. Independent source reviews found no remaining P1/P2.

The98.364s active/editor observation includes movement, menus, reload and
idle/tool time. Renderer mean0.0528ms/p990.1164ms/max15.7728ms; main-thread
max849.2627ms; draw calls mean29.16/max34. No speedup/GPU/physical-Deck claim.
Both isolated sessions restored save root, settings, scene and view; final
Play/isolation/art-pending/view-borrowed flags are false. Caffeinate remains
running at the user's request. The accidental background-removal request
was cancelled and is not part of this work.

**Next:** G2 connected opening masonry from the already authored source and
reviewed runtime-test draft. Preserve accepted sprites; resolve connected
solid walls versus low crossings, then pond/path finishing and useful1×/2×
framing. G2 was not yet imported at this checkpoint; the following continuation
records its bounded integration and Play acceptance. The plot/works-product
ordinary gates, east/kitchen kits, complete six-site journey, packaged build
and physical Deck performance/readability are still outstanding. Continue
through the completion plan; do not treat this delivery fix as finished art.


### G2 connected masonry integrated — 2026-10-24 UTC

[Boundary continuation](GRAPHICS-G2-BOUNDARIES.md) is current. The same approved
source hash now supplies48 cached native wall masks through ordinary flat
presentation. This corrects detached-post wall runs without changing actual
world geometry. Strict current owner, variant, custody and disclosure controls
preserve glyph identity when native art is invalid. Eight real fallback RED
cases exposed and fixed an old-art bypass; final60focused and overlapping
552regression cases pass. The fixture/import corrections are recorded honestly.

Actual seed64 N start, northern wall/examine/destruction, F6 reset, y16 western
passage and blocked low crossing have8 real1280×800 captures. Root and an
independent art reviewer found continuous courses readable, empty routes and
separate wicket/beam/quartz intact, and removed-wall caps updated. See
[ordinary start](Verification/Live16x24/g2-normal-start.png),
[ruin](Verification/Live16x24/g2-ruin-passage.png) and
[destruction](Verification/Live16x24/g2-wall-destroyed.png).
The81.902s active+idle editor profile retains66.93ms renderer/1084.46ms main-thread
maxima; it does not establish a speedup or Deck performance. Normal build lacked
Vault, so no ordinary Vault or physical Deck acceptance is claimed. Stopped
cleanup restored all owned state, and caffeinate remains running.

**Next:** G2 pond/road/soil scene finishing from these actual captures, with
imagegen reinterpretation, state/owner extraction, native authoring and another
ordinary route. Preserve accepted walls/actors. Full opening coverage/motion,
product ordinary-use gates, east/kitchen, complete six-site journey, packaged
build and physical Deck remain unfinished. Continue the #1 graphics priority;
this family is progress, not a finished graphical release.


### G2 opening ground integrated — 2026-10-24 UTC

[Ground continuation](GRAPHICS-G2-GROUND.md) records the actual capture→imagegen
paintover/parts→editable native source→runtime→Play loop. Pond banks follow
current disclosed dry neighbors; quiet roads and old/prepared soil use the
existing background layer. Fixed-row soil now survives a covering player/item.
No reaction, planting, crop or collision rule changed; approved actors/walls stay.

The final71 focused and563 overlapping regression tests pass; two real runtime
bugs have saved RED/GREEN (covered field and empty footprint anchor). Actual
ordinary seed64 screenshots show New Game, road/pond walking, Rime Grip, ice
expiry/new bank, F6 restore, finite Harvest→Prepare and player over frame19.
See [opening](Verification/Live16x24/g2-ground-start.png) and
[prepared field](Verification/Live16x24/g2-ground-field-prepared.png), with
[gates/receipts](Verification/Live16x24/g2-ground-play-verification.json).
Independent code and visual review found no remaining significant blocker in
this family. Wet20 is native-tested only; Ember Spit thaw attempt failed, and
ice expiry is not a melt claim. Current generic ice cue is not new native art.

Wrapped editor profiling retains only the last131072samples, including idle:
renderer max36.03471ms/main-thread max841.23625ms; no speedup or Deck claim.
Stopped cleanup restores all owned state. Caffeinate remains active. An existing
underfoot action menu targets Player while describing the crop; adjacent
interaction works. This is documented for a later focused fix, not hidden.

**Next:** G2 framing/motion/combat and remaining opening coverage, then close
plot/works product ordinary gates and east/kitchen. Whole journey, packaged
launch and physical Deck remain outstanding. Continue the completion plan;
do not treat these accepted ground pixels as a finished graphics release.

## Latest continuation — G2 action readability, 2026-10-24 UTC

See [GRAPHICS-G2-ACTION-READABILITY.md](GRAPHICS-G2-ACTION-READABILITY.md) first.
The earlier disk-full and unread-dialog failures remain dated in that record.
Source preservation and selective cleanup restored ordinary Unity operation,
with source, editable art, saves and the selected current build preserved.
The package warnings match the installed6000.3.4f1 signature-verification
defect; applying the official editor patch is still an environment follow-up.
No user/modal blocker remains for the next ordinary-input graphics work.

Grouped numbers and actor-body avoidance fixed the observed false12/62 and
Player overpaint. Recovered captures then exposed a further attribution P2:
a high enemy word could appear beyond a nearer player. The bounded correction
refuses those misleading lanes while keeping hidden actors private and omitting
a whole readout when no truthful placement remains. Current native results are
**61/61 focused and211/211 prior regressions**, zero skipped, after the exact
geometry failed before production. Do not sum the older253-case result as new
distinct coverage. Approved art, gameplay and animation timing are unchanged.

New post-attribution sessions contain [24frames/20events](Verification/Live16x24/G2Action-20261024-134107-270/index.json)
and [17frames/6events](Verification/Live16x24/G2Action-20261024-134257-723/index.json),
all actual1280×800 EOF captures. Independent review found no new P2 in the
bounded replay. [A12](Verification/Live16x24/G2Action-20261024-134107-270/frame-12.png)
shows nearby Shaken and distinct1/8; B07 shows lethal2 and B14–B16 later Idle
and readout clearance. The original Bleeding/Player event did not recur under
these different genuine rolls: native tests prove its exact geometry, and
the captures establish ordinary visual regression only. Selective stills do
not establish continuous motion, audio synchronization or device performance.
The [final cleanup](Verification/Live16x24/g2-attribution-launch-cleanup.json)
confirms stopped Play and all owned isolation/view/save state released.

The [plot-product record](LIVE-16X24-PLOT-PRODUCTS.md) now has partial ordinary
acceptance: paid rights, actual Pitchpod harvest, two resin plus one seed picked
up, and carried resin Examine with native art. Its nominal ground-examine PNG
is a pickup list; actual ground Examine, planting, clod preparation and other
products remain pending. Works-product use has no new ordinary acceptance.

**Next:** continue G2 scene/framing and Player defence/hurt coverage, close the
remaining plot/works loops, then east/kitchen and the full six-site journey.
Whole opening/graphics release, packaged activation, physical Deck and broader
regional coverage remain unfinished. Keep the heartbeat active.


## Latest continuation — ordinary plot loop and reader polish, 2026-10-24

The plot product packet now has actual keyboard ground resin/clod portraits,
acquired-seed planting, rain-fed stage0→1→2 growth, Marlroot harvest and finite
clod→FireClay preparation. F5/F6 preserves all197 recorded owners and10
inventory rows after canonical owner sorting; this is bounded observation
evidence, not proof of every serialized field or other zones.

Ordinary Play found and repaired two presentation faults: harvest left the
right-side FOCUS naming the removed crop, and ordinary carried seed Examine
only logged text despite existing native portrait art. Current source has
**184 distinct native passes** (57focused +127regression), zero skipped,
after behavioral RED. A second real route confirms immediate FOCUS update,
exact native seed portraits, free reading/return and replanting. Approved
sprite bytes remain unchanged. See
[plot reader record](LIVE-16X24-PLOT-READER-POLISH.md),
[actual seed reader](Verification/Live16x24/plot-products-reader-seed-carried.png),
[harvest focus](Verification/Live16x24/plot-products-reader-focus-after-harvest.png)
and [independent visual review](Verification/Live16x24/plot-reader-replay-visual-review.md).

Current Play/view/save isolation are restored; console0errors/2existing Metal
warnings. The previous locked-machine and cleanup blockers are resolved; no
new packaged build or physical Deck proof is implied. An optional local F5
checkpoint and its hashes are named in
[checkpoint receipt](Verification/Live16x24/plot-reader-replay-local-checkpoint.json);
it came from actual input and may be resumed only inside owned save isolation.

**Next:** finish remaining individual product uses/portraits recorded in
release-coverage, then the existing works forage/preparation/brew/coin loop.
The complete eight-product group, purchased crops, remaining opening scene/
action acceptance, east/kitchen, six-site release and hardware are still open.
Use the full capture/concept/extraction/native-art/integration loop when new
art is needed; do not redraw accepted sprites without a named defect. Keep
this continuation active. This is a verified slice, not graphics completion.

## Latest continuation — plot product route accepted, 2026-10-24 15:00 UTC

The existing native plot-product packet has now passed its **bounded ordinary
keyboard route**, documented in [plot products](LIVE-16X24-PLOT-PRODUCTS.md).
Twenty-four actual1280×800 captures show purchased sludge, finite compost,
rain-fed growth, gladroot ground/carried readers and eating, Marlroot seed
inspection/planting, clod preparation/FireClay reader, and resin brewing into
a burning coating. [Native gladroot reader](Verification/Live16x24/plot-products-uses-gladroot-carried.png)
and [brew reader](Verification/Live16x24/plot-products-uses-coating-reader.png)
are real game images, not concepts. Independent review found no significant
mismatch in the sampled native portraits. Approved art and runtime code did
not change in this acceptance pass; no new native-test count is claimed.

ActualF5/F6 preserves198 recorded owners and11 inventory rows, including
`Coating`/`Burning:1`, at18,7/tick1550/36drams/40HP. This is equality of the
recorded fields, not proof of every saved property or other zones. The
[local checkpoint receipt](Verification/Live16x24/plot-products-uses-local-checkpoint.json)
allows the next route to resume inside owned save isolation. Final cleanup
confirms stopped Play, released isolation/view and restored save root;
console0errors/2existing Metal warnings. No worthwhile further cache/build
removal was found;0bytes removed, current delivered archive and source kept.

**Next action:** resume that real checkpoint, travel south to north works,
and finish finite forage/preparation/brewing and currency discovery with
native visuals. The one observed brewed flask advances works-products to
partial ordinary coverage; it does not accept the other six outputs. Then
complete east connector/kitchen through the full playable capture→concept→
parts→native-art→integration loop. Do not keep replaying the accepted basic
plot route or redraw its sprites without a named defect. Carried MarlrootSeed
remains unobserved (exact ground/planting accepted); purchased crop families,
G2 scene/framing/Player defence, all-seed variants, packaged launch, physical
Deck and the complete six-site release remain open. Continue the hourly work.

## Latest continuation — works products accepted and save blocker fixed, 2026-10-24 16:01 UTC

The existing works product art passed its bounded ordinary route. Seven native
readers match source pixels exactly across named pre-/post-fix sessions. Real
crate destruction/pickup converted4coins into20drams; berries were harvested
and eaten; burr and lichen were harvested and brewed. A genuine continued-save
ID collision blocked preparation, so two runtime files now reserve loaded
numeric identities before load hooks allocate new objects. Native RED had6
expected failures; focused12 plus affected220 passed,232 distinct cases.
[Save correction](SAVE-ENTITY-IDENTITY-RESTORE.md) includes source counterchecks,
independent review, test-discovery/transport limitations and no-migration bounds.

The untouched earlier checkpoint was replayed through actual input, including
an intervening Patchbearer fight, healing and Calm. New sprigs7202→concentrate7204
→mending flask7205; F5/F6 preserved253 observed zone rows and12 carried rows.
Drinking after reload consumed the flask and healed26→33HP. Actual images and
receipt index: [works route](Verification/Live16x24/works-products-uses-index.json).
Existing already-colliding saves are not repaired; unrecorded/other-zone state
and every product use remain outside this bounded acceptance. No new sprite art.

Editor Play/isolation/save root/view restored; native console0errors/2existing
Metal warnings. The short unwrapped22.85second profile is an editor observation,
not Deck performance; the earlier long profile wrapped and only measures its
tail. No new packaged build or physical Deck acceptance.

**Next action:** east connector/kitchen via real playable greybox and captures,
imagegen reinterpretation, exact owner/state extraction, editable native sprites
and integration, ordinary controls and visual comparison. Reuse approved Cook.
The accepted basic plot/works product loops need no arbitrary repeat. Remaining
G2 scene/Player defence, north actor motion/self-treatment, incidental/purchased
content, full six-zone journey and packaged/device release remain open. Graphics
is still priority1; continue the heartbeat and regional work after the opening.

Post-push maintenance: source/evidence commit `ee0f180cf` was confirmed on the
server and all103 owned blobs verified. The subsequent read-only
[storage audit](Verification/Live16x24/works-products-uses-storage-audit.md)
reconfirmed the current delivered archive and six preserved source variants.
No safe worthwhile additional cleanup:0bytes removed,3.10GiB free at the
recorded snapshot. Active caches, sources, saves and current package retained.
This is not a new Deck build; the next graphics pass remains east/kitchen.


## Latest continuation — east/kitchen native route, 2026-10-24 17:07 UTC heartbeat

The two remaining surface addresses now use18 new native16×24 frames and the
unchanged approved Cook. Actual playable baseline captures led to two builtin
imagegen concepts, rejected/included part contracts, editable Blender/source
cells, a single native atlas and exact owner/state admission. Both world and
shared-reader portraits use the imported assets. See
[east/kitchen record](LIVE-16X24-EAST-KITCHEN.md) and
[actual kitchen revisit](Verification/Live16x24/east-kitchen-integrated-kitchen-revisited.png).

Ordinary keys repaired the clay pan using2FireClay, harvested Claspbean into
2pulp+1seed, collected it, saved/reloaded and walked east/kitchen both ways.
Forge inspection and real component picker worked; missing forge components
and Emberwheat grain were not invented. The approved Cook and repaired pan
portraits match native pixels exactly. Visual review found repaired-pan text
still claiming damage; this is fixed after11RED→11GREEN,152regression passes
and actual already-repaired Continue/Examine. Combined art/readout verification
has**495 distinct native Unity cases passing**, zero skipped. Independent
source and visual reviews have no remaining material finding in this slice.
[Corrected actual reader](Verification/Live16x24/east-kitchen-readout-fixed-reader.png).

Final Play/isolation/view/save-root/scene cleanup is recorded; console0errors,
2existing Metal memoryless-depth warnings. The bounded editor profile wrapped,
so its statistics describe only the32768-frame tail, not the full66.45seconds
or Deck performance. No new packaged build or physical device proof.

**Next action: scene-level working-landscape composition.** Existing east/kitchen
views have repeated hedge rectangles and parallel flower bands that overwhelm
the useful service clusters. Read the concrete cold-generation plan in
[visual review](Verification/Live16x24/east-kitchen-integration-visual-review.md).
Greybox exact opening neighbors with asymmetric courts, short broken hedge runs,
unequal flower drifts and truthful owner-backed ground. Preserve existing saved
graphs, approved sprites, mechanics, finite resources and safe approaches.
Use current actual captures→constrained imagegen scene study→real owner mapping,
then native finish and seed64/65 tests, ordinary six-stop travel and1×/2× review.
Do not substitute another disconnected sprite packet for this visible work.

Paid meal/authoritative working-ready trays, full forging and Claspbean lifecycle
remain open along with G2 defence/motion, incidental/purchased identities, full
six-zone release, package/device and regional completion. Resume the actual
18,7 kitchen checkpoint only inside owned isolation, using
`east-kitchen-integrated-checkpoint.json`. The complete release is not accepted;
keep the hourly work active. Graphics remains the release blocker and priority1.


Post-push maintenance: `8d02f27fc53cb6ff2e8c534b299701c16ed1871a` is on
origin/main and all143 owned file blobs were verified. The bounded read-only
[storage audit](Verification/Live16x24/east-kitchen-storage-audit.md) found no
safe worthwhile stale cache/build candidate:0bytes removed,2.82GiB free.
The current delivered archive hash is unchanged; sources/saves/backups and
active editor caches remain preserved. It does not contain today's graphics.
Continue the scene-composition plan above without clearing useful caches.


## Latest continuation — working-landscape repetition reduced, 2026-10-24 18:12 UTC heartbeat

Exact fresh east/kitchen layouts now use unequal flower drifts and surviving
hedge fragments rather than repeated ribbons/rooms. Accepted sprites, shared
routes, finite service owners and loaded graphs are unchanged. Actual captures
fed two constrained imagegen concepts; no generated pixels were imported.
The small cold-generation mask change also clears the sampled kitchen front.
[Plan, scope and results](GRAPHICS-G2-WORKING-LANDSCAPE.md).

Native15 focused RED(5 expected failures)→15 GREEN plus197 affected regression
cases pass:212 distinct cases,0skip. Outside/Legacy hashes and real-owner/state
counter-checks pass. Source and nine-image independent reviews found no P1/P2
in this bounded change. Fresh ordinary keyboard routes on seeds64/65 visited
glade/east/kitchen, opened real pan menus, gathered Claspbean, saved/reloaded,
departed and revisited. Recorded137/169 owners match by ID and8/8 inventory rows
match after load; harvested beds stay empty. Seed64 also exercised the actual
forge picker after a real fight/Calm/tonic; no forging or fresh pan repair claim.

[Actual seed64 kitchen at2×](Verification/Live16x24/working-landscape-64-kitchen-2x.png)
and [seed65 at2×](Verification/Live16x24/working-landscape-65-kitchen-2x.png)
show native pixels and real harvested soil. Temporary presentation zoom restored;
physical Deck input remains untested. Both Play/save-isolation/view sessions are
restored, checkpoints preserved. MCP initialization timeout/late TestRunner
cleanup, known Metal depth messages and an Autosaved log misclassification are
recorded without claiming a clean transition console. Unwrapped82.97second editor
profile: renderer mean0.0600ms/p99 0.1252/max36.4736; main thread mean1.2831ms/
p99 2.4907/max888.4825; max41draws. Not a benchmark or Deck performance proof.

**Next: finish working-court framing using real owners and approaches.** Services
are clearer but still loosely arranged on broad grass, and long hedges show a
repeated bright mosaic at2×. Preserve the accepted sprite identities and new
mask result; improve place recognition with purposeful boundaries, not filler
props. The larger scene design, G2 motion/defence, service/crop outcomes, complete
six-site journey, package/device gates and regional kits remain open. No new
build exists from this pass. Continue the hourly work; graphics is still priority1.


Post-push maintenance: source/evidence commit `9728a7c652609d0c706ad3f797af8a710491a08e`
was verified on server main with all 95 owned blobs matching. The subsequent
[storage audit](Verification/Live16x24/working-landscape-storage-audit.md)
found no confirmed worthwhile stale cache or superseded build: **0 bytes removed**.
Free space was 2.52 GiB at the recorded snapshot. Current delivered archive hash
is unchanged; sources, saves, backups and useful active caches remain protected.
That older package does not contain this pass. Unity is stopped/restored and
usable. Continue the working-court framing and remaining release gates above.


## Latest continuation — working court integrated, 2026-10-24 19:14 UTC heartbeat

[Working-court plan and evidence](GRAPHICS-G2-WORKING-COURT.md) closes the local
kitchen framing slice:9real masonry owners and15 sparse stepping-stone owners
join the unchanged services on fresh exact-address/non-Legacy Hedgerow layouts.
The real capture→imagegen→part extraction→editable native art→integration loop
produced three16x24 ground cells; approved18 art PNGs are unchanged. Grass joints
remain plantable and original Grass is preserved. Barren paving was rejected
because it would erase original vegetation during placement/load.

272affected native Unity cases pass after recorded REDs and independent source
reviews. Actual ordinary routes on seeds64/65 reached both court orientations,
opened the pan menu and gathered Claspbean into2pulp+1seed. Seed64 entered the
unoccupied cot; seed65 had a real solid Farmer there. Actual stone reader artwork
and prose agree. F5/F6 preserved recorded owners byID and inventory on both;
seed65 border exit/return preserved all31 remaining court packet owners. The
first order-sensitive comparison is retained alongside its independent keyed
correction. Two-seed1x/2x visual review accepts this bounded scene improvement.

[Actual rotated court](Verification/Live16x24/working-court-64-kitchen-2x.png)
and [actual seed65 court](Verification/Live16x24/working-court-65-kitchen-2x.png).
Both editor sessions are stopped/restored and checkpoint saves preserved. The
68.59second unwrapped editor profile measures idle/zoom/look/menu/tool overhead,
not walking or Deck performance; max41draws, renderer mean0.05267ms/max45.63ms.
No new build, physical device proof or full release acceptance is claimed.

**Next:** close the visible incidental-actor native coverage gap, starting with
this actual Farmer, after auditing existing approved actor routing. Also retain
an existing multi-owner Look heading/reader discrepancy as a named UI follow-up;
do not misreport Player-covered floor Examine as passed. Then continue G2
motion/defence, paid meal/ready trays, full forge/Claspbean lifecycle, complete
six-zone journey, package/device acceptance and regional kits. Preserve accepted
court and landscape work. Graphics stays priority1 and the heartbeat continues.


Post-push continuation: `679ae9e2b6fae1b46eff8430b9e9066fc15cb331` was verified
on server main with all104owned blobs matching. The
[storage audit](Verification/Live16x24/working-court-storage-audit.md) reclaimed
**0bytes**: only the current delivered archive remains and current Unity caches
are active. Free space was2.25GiB. All source/saves/checkpoints/current archive
remain preserved; the older delivered package does not include these graphics.

The [next actor audit and plan](GRAPHICS-G2-INCIDENTAL-ACTORS.md) distinguishes
missing art from a missing connection: Farmer needs a distinct native frame;
ReedbackGrazer can use the user's unchanged approved `03-ridge.png`, currently
unconnected to live flat selectors. Magpie/Viper/Scrabbler/Cook already have
native routes. Begin with source-checked RED, actual capture-based Farmer concept
and native editable source, then narrow owner routing and ordinary move-away /
examine evidence. Do not silently alias inherited identities or reset the court.
The named Cindercaller gap and other release gates remain open. Keep scheduled
work active; this checkpoint completes one visible slice, not the overhaul.


## Latest continuation — two real actor gaps closed, 2026-10-24 20:19 UTC heartbeat

[Incidental actors](GRAPHICS-G2-INCIDENTAL-ACTORS.md) now has bounded ordinary
acceptance: a newly authored editable16×24 Farmer replaces the kitchen cot
glyph, and the user's unchanged approved Reedback Grazer is connected to the
actual eastern-route animal. Both exact subject readers show the same body.
Native RED2/2 preceded production; final311+277distinct Unity cases pass
(588total,0failed). Source/production reviews found no P1/P2. Existing23approved
art files stay unchanged; no AI/world-generation/motion policy was altered.

[Actual Farmer beside Cook](Verification/Live16x24/incidental-actors-65-farmer-2x.png)
and [actual Grazer after opening its pen](Verification/Live16x24/incidental-actors-64-gate-open.png).
Seed65 ordinary Continue, movement and F5/F6 retain the Farmer/cot and8inventory
rows. Fresh seed64 ordinary travel, Examine and real Open-door interaction make
Grazer5036 move51,17→50,18; F5/F6 matches300owner/6inventory rows at tick1000.
These are two static body identities, not new animations or a whole-bestiary claim.
Farmer move-away/underlying Bed repaint has a native test, not ordinary replay.

Both editor sessions are stopped/restored with null save override. Native console
has0errors and2known Metal warnings. The206.37second editor profile wrapped its
131072sample ring; retained renderer mean0.05170ms/p99 0.10325ms, max43draws.
This includes idle/UI/load and is not a complete-window/device benchmark. No new
package was made; the retained delivered SteamDeck archive still predates this art.

**Next: execute [ordinary paid kitchen service](GRAPHICS-G3-KITCHEN-SERVICE.md).**
The source-checked route uses real glade grain and cellar clay, repairs the pan,
commissions with carried pulp/drams, waits real turns, collects and reads a meal.
Current neutral trays suffice for the first acceptance; derive any new working/
ready cue from actual captures and authoritative job/custody, not arbitrary stock.
Retain Cindercaller, aggregate Look heading, intermittent court overlay, G2 defence/
motion, complete crop/forge loops, six-site journey and packaged/device gates as
open. Graphics remains priority1; the hourly continuation remains active.

Post-push/storage: `3e67ab983a543a4fd1851a89e07df863bd603975` was verified on origin/main with all84owned
blobs matching. The [storage audit](Verification/Live16x24/incidental-actors-storage-audit.md)
removed nothing (0bytes recovered); useful caches and the current archive are
preserved. Free space was2.19GiB. Continue the source-checked kitchen
service route next; full release and updated package remain open.

### Current continuation — paid kitchen state cues, 2026-10-24 22:10 UTC

The [paid kitchen slice](GRAPHICS-G3-KITCHEN-SERVICE.md) is now bounded-accepted:
actual glade grain/cellar clay, repair, payment, completion, pickup, reload and
consumption preceded the new art. Actual Working capture→imagegen→two editable
native16×24 frames→real world/Examine integration→ordinary replay is complete.
The pan gains a cloth only for the actual paid ingredients; the board gains its
actual commissioned parcel and clears on collection. Original bases remain
unchanged. Eleven new-art screenshots and exact job/submitted-tile observations
are [indexed here](Verification/Live16x24/kitchen-service-states-summary.json).

481 distinct native cases pass;5allocation tests explicitly skip after a known
allocation demonstrated this runtime's counter is unavailable. Independent quiet
frame tests and source review still cover the renderer's stable path.85.12s
Editor profile includes save/load/tool overhead and is not Deck performance.
Both owned Play sessions are restored; no current build replaced. Complete G2,
remaining crop/forge states, the literal six-site journey and packaged/device
release remain open. Continue from the current completion plan/coverage ledger;
prioritize visible identity/action gaps over disconnected extra prop sheets.


## Latest continuation — Cindercaller native body, 2026-10-24 22:43 UTC

[Exact Cindercaller integration](GRAPHICS-G2-CINDERCALLER.md) replaces a real
red-g fallback in the east wilderness with an editable16×24 native body and
matching exact-subject portrait. The actual baseline→imagegen→native Blender
cells→integration loop is recorded; all65 protected art hashes are unchanged.
The source sweep corrected the earlier Player-identity premise: Cindercaller is
an NPC, and the accepted Player body/motion stay unchanged.

[Actual Examine](Verification/Live16x24/cindercaller-native-reader.png) and
[actual moved/reloaded world](Verification/Live16x24/cindercaller-native-reloaded.png)
verify visible integration. Ordinary keyboard movement advances tick5250→5310,
moves the real caster78,7→78,5, and keeps its new body. F5/F6 matches all6 recorded
actor/18inventory rows. Native behavioral RED2 precedes final334distinct passes,
0fail/skip. Independent code/source/gameplay reviews find no significant issue.
This is one static identity, not a cast animation or whole-bestiary completion.

Owned Play/save/view/preference state is restored. The250.934s Editor profile
wrapped its131072-sample rings; retained max45draws and renderer mean0.05664ms
are not physical Deck or full-window performance. Only the two existing Metal
memoryless-depth diagnostics remain; MCP labels them Error while Editor shows
warnings. No new script/exception diagnostic was found. No new package was made.

**Next:** separately fix and test the direct Riposte counterstrike's missing
Attack visual/facing event using the saved
[action-source audit](Verification/Live16x24/riposte-action-source-audit.md).
Preserve accepted Player art and existing parry/guard outcomes. Then complete
crop/forge lifecycles, the literal six-zone journey and packaged/device gates.
G2/G3/full release remain open; the graphics continuation stays active.


Post-push/storage: `c76654faa17c0162bb6390007951ff37b14a657f` was verified on
origin/main with all59owned blobs matching. The independent
[post-push audit](Verification/Live16x24/cindercaller-storage-audit.md) confirms
source/saves/current archive and both unrelated local Notice variants preserved.
About1.71GiB is free; no worthwhile confirmed stale cache/build candidate remains,
so **0bytes were deleted**. The active Editor's useful caches are retained. The
current delivered archive still predates this art. Continue the next bounded
action-feedback slice; no whole-release or packaged acceptance is implied.


## Latest continuation — Riposte and travel status feedback, 2026-10-25 UTC

## Delivered source and native verification

- Riposte now explicitly opts into the existing facing/Attack event before its
  actual single-strike resolution. Local exception containment protects combat;
  reserved/spent reentry gates and ordinary multihand/default-direct callers
  remain unchanged. No art replacement. Native `riposte-feedback-final.xml`
  passed206/206 at23:28:24–40UTC, including20 focused cases; no skips/failures.
- Player owner-turn status context now uses its canonical physical/current
  managed zone rather than stale Brain.CurrentZone. NPC context policy remains
  untouched. `status-zone-red.xml`:11 total,7 intended failures/4 passing controls.
  Focused GREEN11/11; `status-zone-regression.xml` passed274/274 at00:04:08–18UTC,
  no skips/failures. Source review found no remaining P1/P2.
- Exact XML full-name comparison:206 and274 are separate selected sweeps with
  **37 overlapping FlatPixelActorMotionTests**, hence **443 distinct cases**.
  Status focused11 is already included in274. Do not report480/491 unique tests
  or imply this was the entire project suite.

## Ordinary evidence and limits

The Warden earned/used a skill point through ordinary play and learned Riposte.
The19-frame north capture `G2Action-20261024-234223-659` establishes actual Player
Hurt pixels at01/07/11, Guarded/En garde words at01, ordinary Attack at15 and
recovery to Idle. Independent source+PNG review was completed. Guarded is not a
full parry. No captured attack has reactive counter provenance; **ordinary
Riposte remains unobserved**, despite its native event/renderer tests passing.

That capture exposed old-zone periodic damage while fighting in11.9.0. The
status fix above is natively proven; a separate ordinary post-fix poison/Hurt/
numeric replay is **still pending**. Do not reuse pre-fix pictures as acceptance.

The forge route harvested FrostLichen and root observed brewed output8995, but
the24-frame east encounter `G2Action-20261024-235149-310` interrupted the approach.
Root observed the blade-bearing hand severed, both tonics consumed and eventual
world-map retreat at5HP. The limited captured window ends earlier at1HP; later
events/session observations are separate evidence. No tempering, forged output,
on-hit freezing, full parry or Riposte is accepted. Preserve the failed route.

Cleanup `riposte-feedback-cleanup.json` records stopped SampleScene, isolation/
artPending/viewBorrowed false and null save-root at23:56:08Z. This is that
checkpoint, not proof of every subsequent editor state. No current build was
replaced; packaged/device/performance gates remain unchanged.

## Next bounded work

Parent-selected next art slice is **LongSword**, after its exact identity/source
audit; it is not already authored or accepted by this packet. Keep approved
Player art unchanged and use the established concept→native→integration→actual
reader/world pipeline. Before resuming the injured character, inspect actual
save location, HP/effects, missing limbs, equipped/carried weapon and brewed8995
custody. Do not assume the severed weapon is carried or world-map time is safe.
Use reachable ordinary recovery/services; no grants, forced counters or erased
failed approach. A separately labeled fresh game is possible if recovery is
not safely attainable. Complete forge/crop loops, six-site continuity, ordinary
counter acceptance and packaged/device validation remain open.


## Latest continuation — LongSword integrated, 2026-10-25 00:25UTC

See `Docs/GRAPHICS-G3-LONG-SWORD.md` and `Docs/Verification/Live16x24/long-sword-play-summary.json`. ExactLongSword now has native16×24 world/Examine art from actual baseline→imagegen concept→editable Blender cells→runtime import. Initial2behavioralRED failed as intended;37focused and391distinct affected native regressions passed, no skips. Ordinary pickup/equip, matching ground/equipped readers, F5/F6 and cellar descent/ascent accepted for the same2699owner. Old Player/items/3D and user saves preserved. This does not complete forge/crop/combat/six-zone/package/Deck gates.

Correction to earlier instructions: the injured5HP east-route disposable save was not exported before NativeSaveIsolation cleanup and cannot be resumed. Preserve its failed-route images/events. The sword pass separately replayed the unmodified earned healthy branch and exported its final checkpoint to `/tmp/coo-long-sword-verified-checkpoint` before cleanup. It is42HP, level2, RP1, glade28,19 with2699equipped and no brewed8995; confirm live state on continuation. No forged result or recovered severed weapon is implied.

Root stopped Play and restored ordinary SampleScene/settings/save root at00:25:02UTC. Follow the connected ordinary material→forge route next; verify remaining combat/status feedback when naturally encountered. Do not author disconnected packets to avoid outstanding gameplay gates. Existing current build unchanged. Graphics/storage heartbeat remainsACTIVE every10minutes, checking safe candidates only after successful remote preservation; no useful cache purge. Bounded inspection removed0bytes and preserved source, saves and current archive.


## Latest continuation — ordinary forge and crop loops, 2026-10-25 01:05UTC

[Actual connected route](GRAPHICS-G3-FORGE-CROP-JOURNEY.md) now accepts north
FrostLichen harvest→frozen coating brew→east forge quench of the same2699 blade,
ordinary re-equip, matching native Examine and F5/F6. TemperCount1, Frozen40,
HP8/8 persist; no actual Frozen hit or component-forged weapon is claimed.

The same ordinary run reached Orven and completed Claspbean harvest→seed
planting→Rain→same13511 seed/sprout/ripe stages→harvest/pickup/reload→second seed
replant/reload. Native stages and reader are actually visible. Final kitchen
25,14,42HP,tick2990 has4pulp, cultivated12611 and dry new13515stage0. Its exported
checkpoint is `/tmp/coo-forge-kitchen-final-checkpoint`; do not copy its audit
marker. Failed prior east-route evidence remains separate.

Independent source/PNG review accepts these bounded loops. Existing accepted
art is unchanged; no runtime changes or new native-test result in this packet.
Actual screenshots and raw receipts are indexed in `forge-kitchen-play-summary`.
Play/settings/save root restored at01:03:46UTC;0errors,2existing Metal warnings.
No updated package or Deck acceptance. Full G2/G3/six-site/regional release is open.

**Next named defect:** east EOF frame18's lethalNPC1 briefly overlays the departing
Player tween, although logical damage ownership is correct. Fix presentation
occupancy with RED→GREEN and an ordinary replay before further disconnected art.
Actual Riposte, post-travel Player periodic hurt, Frozen proc and pulp application
remain unobserved. Rain tooltip/historical growth-doc wording is a separate
followup. Continue the active10minute graphics/storage schedule; push exact
owned work before safe stale-cache inspection, preserving useful active caches.


## Latest continuation — moving-body readout guard, 2026-10-25 01:25UTC

[Named readout correction](GRAPHICS-G2-TWEEN-READOUT.md) now supplements logical
cell occupancy with the actual disclosed native Walk body. This prevents a
neighbor's receipt painting across a departing actor without changing damage,
recipient, motion, fog or accepted pixels. Seven intended RED failures preceded
production. Wider native run:285passed,0failed,1 calibrated allocation-counter
skip across286 cases, including the focused61; counts are not additive.
Independent geometry/authority/lifecycle review found no actionable P1/P2.

Ordinary replay used the unmodified prepared checkpoint, normal SampleScene,
Continue and real controls.24 EOF frames show En garde, melee, Hurt/death and
Walk A/B with42HP maintained. The Scrabbler died on the first15-point strike,
so the former departing-player/periodic-lethal overlap did not recur. Native
geometry proves the correction; exact live reproduction, ordinary Riposte and
post-travel Player periodic Hurt remain unobserved. Evidence and hashes are in
`tween-readout-play-summary.json`. Profiling's ring wrapped; retained tail
includes idle/capture/tool overhead and is not a performance or Deck gate.

The distinct untempered replay branch was exported before cleanup to
`/tmp/coo-tween-replay-checkpoint`. The accepted frost-tempered/crop checkpoint
remains `/tmp/coo-forge-kitchen-final-checkpoint`, kitchen25,14,42HP,tick2990;
confirm its real state on resume and copy only the actual character folder.
Root stopped Play and restored SampleScene/view/preferences/null save override
at01:24:42UTC. Direct console0errors,2existing Metal warnings. Current build
and save backups remain intact. Repeated random counter hunting must not delay
the six-site continuity/scene gate; record ordinary feedback if it occurs
naturally. No full G2/G3, packaged, device or regional release is claimed.


## Next executable slice — six-site service and return, 2026-10-25 01:30UTC

The moving-body correction is on remote main at43b43c6a6; its82 exact owned
files were byte-verified remotely. Post-push storage inspection found4.96GiB
free and no worthwhile safe stale candidate;0bytes removed. Current archive,
editable source, user saves and all checkpoint branches remain protected.
The active graphics/storage heartbeat is every10minutes, not mandatory deletion.

Next use the accepted **frost-tempered/crop** checkpoint, not the separate
untempered tween replay. Start at kitchen25,14,42HP,tick2990 and verify actual
2699blade,2clay4716,4pulp and dry13515. One continuous service-and-return route
has more release value than another disconnected item packet or RNG hunt:

1. Water the existing13515, repair the actual pan using carried clay, then ask
   Orven **“ask Orven to vouch for the repair.”** The real CookIntroductionPart
   requires this player's recorded repair; do not synthesize knowledge.
2. Walk kitchen12.11→east12.10→glade11.10. Revisit the forge, descend/return from
   cellar11.10.1, then continue north works11.9→Nella11.8. Capture real approach,
   interactions and return state, retaining finite depletion and owner identity.
3. Inspect public/tied beds, read Nella's terms, and choose **“relay Orven's
   introduction”** if rights are Unknown. Legitimate grant avoids8drams. Inspect
   and harvest one authorized reserve product, replant if supplied, and verify
   the ground cord persists. Do not manufacture theft or grant state in code.
4. Return through the same route to kitchen. Verify crop13515's actual growth,
   repair, introduction and custody. Save, stop gameplay, Continue and compare
   the same owners. Export the real checkpoint before isolation cleanup.
5. Fix only measured blockers in that flow. The existing ripe Claspbean Examine
   reader has visibly cramped prose leading; inspect actual glyph bounds,
   pagination and footer clearance at1280×800 before a small typography fix.
   Source places body rows one cell apart with1.6× glyphs, but the screenshot
   does **not** prove duplicate text or actual vertical overdraw. Keep the
   approved ledger style. Runtime corrections still require RED→GREEN.

Accept only a continuous six-site receipt, readable readers, correct return to
world, truthful permission/state cues and preserved finite custody. Record
encountered fallback identities rather than pretending every incidental item
is complete. This checkpoint-based pass does not prove fresh-New-Game continuity,
packaged activation or physical Deck. The retained **October11 current package
still predates this graphics work**; update packaging after the connected
acceptance, and keep hardware proof separate. Ordinary Riposte, Player periodic
hurt, Frozen proc and pulp application remain named unobserved opportunities,
not reasons for unbounded random encounter hunting.


## Latest continuation — six-site journey and readable Examine, 2026-10-25 02:03UTC

[Connected service journey](GRAPHICS-G3-SIXSITE-JOURNEY.md) now has an ordinary
earned-checkpoint route through kitchen, east shelter, glade, cellar, north
works and Nella, then back. Actual2clay repairs pan13489; Orven13471's
introduction carries cause`ed272954d6e94cedbb3937df123dba5a`; Nella15712 grants
reserve rights without8drams. Pitchpod15729 yields2resin+1seed; real planting
and Rain create/water15735. On kitchen return13515 is ripe and the repair/
knowledge persist. All26 sampled captures are flat16×24 and42HP, not concepts.

[Reader leading](GRAPHICS-G3-READER-LEGIBILITY.md) preserves the font size and
approved portrait while increasing body baseline advance to1.25 and reducing
page capacity to20 within the same37-row maximum. Seven intended native RED
failures precede production;108 distinct GREEN cases pass with0fail/skip.
Independent source/image review finds no P1/P2; the actual same paragraph has
4–5 clear pixel rows between lines instead of1. Ordinary two-page sword reader
forward/back/close and return to world preserve tick7160.

The final exact character checkpoint is `/tmp/coo-sixsite-final-checkpoint/`
(character`70a87e9febf34b499e8aaf47a44601ed`). Its Quick333298bytes SHA256
`c9add40b3996a82f5d7d9d196e78ba19c7c7a29e1a25ceec33f0e7dfd701db56` is
recorded with all four files. Normal stop/C Continue matches all ten sampled
gameplay fields. Do not copy audit markers or replace prior accepted branches.
Root restored Play/settings/view/scene/null save override at02:02:13UTC;
console0errors,2existing Metal warnings. No new performance measurement.

**Next:** verify exact owned push, inspect storage, then prepare a current
packaged flat build; October11 current archive still predates these graphics.
Keep launch/package/hardware evidence separate. A bounded prepared-bed cue
should follow: original decorative “cut emberwheat row” masks cultivated soil
after reserve harvest; real seed planting works. Preserve historical row names
and add current/disclosed bed context through the shared readout with native
RED/counters. Fresh-New-Game whole-journey, incidental native art, ordinary
Riposte/Player periodic Hurt/Frozen proc and full regional coverage remain open.
Do not turn these named gaps into endless RNG hunting or disconnected art.


## Latest continuation — real packaged flat delivery, 2026-10-25 02:22UTC

[Packaged delivery](GRAPHICS-G1-PACKAGED-DELIVERY.md) closes the stale-build gap
for runtime source `9ffd7c973`. The normal production scene built for both Mac
and Linux with **0 errors / 4 warnings each**. Actual standalone Mac/Metal
Continue, crop Examine and movement use the flat grid without a forced art
preference. Four unchanged window captures are JPEG1280×856, including56pixels
of macOS chrome; the game viewport is1280×800. These are packaged gameplay,
not concepts or editor-only captures. Shutdown disposal warnings are recorded.

The new Linux archive is
`Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,
128610942bytes, SHA256
`76806cdcaf37c49883820f160fa2076963760b9d27fa5d27c4f243127c586bd6`.
All181 archived regular-file sizes/hashes/modes and paths passed streaming
verification. Current audio credits/licenses and a corrected README are included.
The October11 archive remains preserved. Linux/Vulkan execution and physical
Deck behavior/performance are **not** accepted by a Mac build or archive check.

Original770 user-save files (93965260bytes), names/hashes and both preference
domains are restored and verified. Temporary accepted checkpoints remain outside
the repository. Root returned the editor to StandaloneOSX, out of Play, no test
run/isolation, and restored reviewed build-generated serialization. No code or
approved artwork changed in this packaging pass. Native108-case reader evidence
belongs to the prior change, not a new test run. No new performance claim.

**Next:** the actual harvested reserve-bed ambiguity remains the smallest
measured presentation correction. Add neutral “prepared planting bed” context
through `CellStatusReadout` only for a current disclosed canonical cultivated
soil owner, suppress it for an actual current disclosed crop, and retain hazard
wording. Do not create TileState, reveal hidden owners, query/reconcile claims,
rename the historical row or promise planting permission. Follow native RED→GREEN
with stale/foreign/hidden/live-crop/hazard/read-only counters and an ordinary
harvest/reader capture. Then continue opening composition and uncovered identities
using the required actual-greybox/capture/imagegen/extraction/native-art loop.
Fresh-character whole journey, all owner/state kits, exact ordinary Riposte and
periodic Player-Hurt receipts, Frozen proc and regional coverage remain open.


## Latest continuation — prepared beds readable after harvest, 2026-10-25 02:55UTC

[Prepared-bed readout](GRAPHICS-G3-PREPARED-BED-READOUT.md) now exposes neutral
“prepared planting bed” through the shared current-ground service. Exact current
cell/zone and the same visible owned physical soil are required; a current
visible crop suppresses it. Original row names/prose, planting rights and growth
remain unchanged. Existing live hazard text/order remains intact; stale/inactive
queries are deliberately suppressed and tested.

Native RED25 had23 expected cue failures before production. GREEN72 plus the
expanded28-case fixture gives **75 distinct passes,0fail/skip**,28new cases.
Independent review found no significant defect and supplied three additional
hazard-authority counters. The real earned seed64 replay harvested7199, collected
2resin+seed7202, compared the exact empty soil5524/row5525 at tick1570, then planted
7203 through inventory. Actual C-menu/Examine show the new cue; the replanted
crop loses it and reports dry growth. F6 restores the empty checkpoint and actual
Look/FOCUS shows the cue. Nine accepted1280×800 captures plus one rejected Pause
attempt are retained; this is gameplay evidence, not concepts. Cleanup at02:54UTC
restores noPlay/noisolation/null save override; console0errors/2existing warnings.

The exported empty checkpoint remains at `/tmp/coo-prepared-bed-empty-checkpoint/`
(character33db4c719b3b4a5baba1d70a23632cd1). Current Mac/Linux packages use9ffd7c973
and **predate this follow-up**; do not claim the cue has already been packaged.
No approved art changed and no new performance or physical Deck result is claimed.

**Next:** fresh-New-Game whole-scene acceptance. Use normal chooser and owned
isolation with no copied checkpoint or forced art. A meaningful finite route is
glade→cellar→north works→Nella→glade→east forge→kitchen, then save/stop/Continue;
pay Nella's actual8drams rather than replaying the already-proven introduction
journey. Inspect real current positions, threats and finite materials. Do not
force progress through danger or pretend a historical coordinate is current.
Prior actual images suggest two composition checks: repeated bright north hedge
bands and Nella's working beds competing with decorative brown fragments.
Reassess fresh captures before changing accepted art; use the required imagegen/
part extraction/native production loop for any warranted art change. Ordinary
Riposte/periodic Player-Hurt/Frozen proc remain opportunistic gaps, not RNG hunts.


## Latest continuation — northern field composition and earned use, 2026-10-25 03:37UTC

[Fresh-origin journey](GRAPHICS-G3-FRESH-OPENING-ACCEPTANCE.md) now reaches glade,
cellar and northern works through ordinary inputs. The fresh north frame named a
specific defect: repeated bright hedge rectangles overwhelmed its field. Following
the actual capture→imagegen→part extraction loop, the
[north composition pass](GRAPHICS-G3-NORTH-COMPOSITION.md) reuses accepted native
Hedge/Grass art and subtracts selected boundaries only in fresh nonLegacy exact
north Fallow plans. Original pocket repair runs first; no RNG draw, per-frame
work, other planned terrain cell, hidden knowledge or saved graph is changed.

Native RED22 had5expected composition failures; GREEN138/138,0fail/skip.
A60-plan comparison verifies15exact-scope Hedge→null-only changes and45unchanged
outside/Legacy cases. Independent source and actual screenshot reviews found no
significant defect. Seed64 matched entry is less repetitive with retained cover;
the existing placement system legitimately moves the works to22,17. Real combat
forced a retreat and consumed two carried tonics before an ordinary Lunge kill.
Normal harvest of both finite sources, reagent selection and brewing produced
frozen-and-stoneskin coating6974. F5→glade autosave→F6 latest-load→ordinary return
preserved32HP, exact inventory and spent sources. See north-composition-play-summary
and actual fresh-opening-north PNGs. This is bounded acceptance, not full-scene
or six-site completion. No new native sprite or benchmark is claimed.

Owned cleanup restored normal scene/preferences/view/save root at03:37UTC,
console0errors/2existing warnings. Current Mac/Linux packages predate this change.
Continue the earned checkpoint at `/tmp/coo-fresh-opening-after-north/`
(character20cb263ebed24a8380168954e23981ca; tick2260/23,17north). It contains the
real coating, one remaining Stoneburr/FrostLichen each,2FireClay,32HP,50drams;
no healing tonics remain. Next: Nella's legitimate8dram access, crop/watering,
east forge and kitchen, with a seed65 ordinary visual counter-pass. Reassess
Nella's actual working-row hierarchy before changing art. Remaining feedback,
regional kits and physical Deck gates remain open. Preserve all prior checkpoints.


### Post-push storage checkpoint — 2026-10-25 03:45UTC

Prepared-bed work is on main at `37bbc54f4`; northern composition and actual
play evidence are on main at `00a188cac`. The latter push verified all52owned
files byte-for-byte and preserved the two unrelated AllotmentNotice PNG variants.
A fresh remote identity check still matched `00a188cac`; both protected Linux
archives were freshly rehashed unchanged. See
`Docs/Verification/Live16x24/north-composition-postpush-storage-audit.json`.

Free disk is1,479,159,808bytes (1.38GiB). No confirmed worthwhile stale cache or
superseded disposable build was found in the bounded inspected paths. **Nothing
was deleted;0bytes recovered.** Useful Library/Temp caches, current Mac/Linux
builds, source, editable art, saves and continuation checkpoints remain intact.
The pending Unity Hub ShipIt update has a live process and is preserved. The
roughly2GiB decrease since03:17 is not explained by measured cache growth; do not
attribute it without evidence or launch another build without checking headroom.
This is a material packaging constraint, not a completed storage fix.

The next graphics pass remains the earned fresh-origin Nella→glade→east forge→
kitchen route plus seed65 visual counter-evidence. Root left Unity out of Play
with normal preferences and save root restored; use the after-North checkpoint
above. Frequent storage inspections continue; they do not require deletion.


## Latest continuation — wide popup backing, 2026-10-25 04:31UTC

[Wide popup correction](GRAPHICS-G1-WIDE-POPUP-BACKING.md) fixes actual vertical
background gaps observed in inventory Examine at wide window sizes. Only the
shared background quads scale with their existing grid; glyphs, portraits and
input geometry retain their owners. Native RED5:2pass/3expectedfail; GREEN93/93,
0fail/skip. Actual ordinary keyboard before/after and resize-to1280×800 captures
show solid backing; Escape returns the same inventory selection. No new build
or physical Deck/performance acceptance. User's latest existing save was backed
up exactly after they had already stopped their normal Play session.

The user now explicitly adds inventory redesign/polish to graphics completion.
[Inventory plan](GRAPHICS-G3-INVENTORY-REDESIGN.md) and
`ArtSource/Live16x24/concepts/inventory-ledger/` capture the actual baseline,
imagegen exploration, extracted layout and rejected fabricated controls/art.
Preferred next slice: retained camera-bound full-screen ledger over existing
inventory state/commands, actual anatomy, native owner portraits, current facts,
all five modes and truthful device hints. Prove popup layering/pointer geometry
first with native RED/GREEN and actual GPU captures. Nella's ground plan/concept
are preserved, pending implementation after this user-prioritized screen.


## Latest continuation — retained inventory ledger, 2026-10-25 06:10UTC

The user's requested [inventory redesign](GRAPHICS-G3-INVENTORY-REDESIGN.md)
is implemented over the actual five existing modes and transactions. It follows
the approved baseline→imagegen concept→extracted layout→native retained UI
pipeline, reuses accepted owner portraits and preserves real anatomy/ownership.
New hierarchy separates equipment/status, carried items with quantity/weight,
selected facts and explicit actions. Native controller Commands expose otherwise
keyboard-only choices; F1/full details and F2/status return to the same owner.
A real mouse failure discovered in Play is corrected at the InputSystem boundary,
including stationary pointer on Open. Bound combat skills now use the existing
authoritative ability descriptions rather than an empty grimoire-only fallback.
The shared bitmap text normalizer also converts authored bullet separators to
plain single-width hyphens; final shared UI validation passed49/49,0skipped.

Native broad regression passed480/480; final ability/mouse/transaction affected
run passed233/233,0skipped. These overlap; do not sum them as unique coverage.
Actual isolated Play covers all five modes, item facts/Examine/throw cancel,
mouse equipment removal/re-equip, real finite brewing, grimoire selection/details
without binding, full status, En Garde/Lunge/Rain/empty-slot facts and actual
1280×800→1920×1080 resize. See `inventory-ledger-live-review.json` and actual PNGs.
Idle sample35.9seconds has zero additional inventory Render calls, with valid
21draw/batch p99 counters. This is bounded Editor evidence, not a speedup,
physical Deck acceptance or a packaged release. Current packages predate this.

Next world slice remains Nella's working soil, using the preserved plan/concept
and existing frames18–20. The current normal-save/scene state is protected and
root owns testing; the plan's older pending-permission paragraph is obsolete.
The earned after-North checkpoint is untouched. Do not replace its finite
progress with artificial grants. Continue Nella's8dram access/harvest/replant/
Rain, glade/eastforge/kitchen, seed65 counter and remaining regional coverage.

Final inventory acceptance: actual corrected Rain and return-to-world captures
complete the scoped Editor pass; cleanup06:16:08UTC restores all borrowed state.
Normal save/backups match. Native49/49 shared UI final check also corrected an
old13-discipline test pin to the shipped14 including Throwing; browser runtime
unchanged. Current packages and physical Deck acceptance remain open. Resume
Nella world graphics from the protected after-North checkpoint next.


## Latest continuation — Nella soil and earned crop loop, 2026-10-25 07:01 UTC

[Nella working soil](GRAPHICS-G3-NELLA-GROUND.md) now reuses accepted native
old-field/dry/wet frames18–20 in exact admitted11,8. Both rendering routes use
current owned crops; a shared-part ownership counter was RED before correction.
Native GREEN272/272, zero skipped. Independent source and actual-image reviews
accept the bounded material change, leaving the larger repeated field layout
and loosely composed service area open.

Actual seed64 earned-route Play paid8drams, harvested/picked up2resin+1seed,
replanted the seed at18,6, cast Rain and waited normally to observe wet20→dry19.
F5→south border→F6 latest save→north return retains crop9193, paid rights,42drams,
32HP and the two resin. Actual screenshots/receipts are `nella-ground-*` in
Verification/Live16x24. Earlier null painted readouts were helper-coordinate
errors, preserved and corrected in later receipts. No approved sprite pixels
or gameplay system changed. Profiler tail is explicitly bounded/wrapped, not a
speedup or physical Deck claim. Current builds predate inventory and this pass.

New earned checkpoint `/tmp/coo-fresh-opening-after-nella/` contains character
20cb263ebed24a8380168954e23981ca at tick3510,18,7 Nella, with the real coating and
finite route supplies intact. Normal save hashes and owned Editor restoration
are verified. No stale cache target justified deletion; zero bytes removed.
Next: resolve confirmed held-action input fallthrough if source audit reproduces
it, continue fresh-origin east forge/kitchen, then next scene-composition cycle
and seed65 visual counter. Soil acceptance is not complete six-zone/world art.


## Latest continuation — inventory action release, 2026-10-25 07:26 UTC

The [held inventory shortcut fix](GRAPHICS-G3-INVENTORY-ACTION-RELEASE.md)
closes the actual planting→unwanted-movement defect found during Nella Play.
Native RED reproduced it; GREEN124/124 (4new,120controls),0skip. Ordinary heldB
300ms plants once at18,6 while Player stays18,7; freshB90ms moves17,8 normally.
Exact earned seed9200 becomes crop9201, HP32/42drams/four resin intact. Actual
PNG/JSON evidence is `inventory-action-release-*`; independent reviews pass.

F5 checkpoint `/tmp/coo-fresh-opening-after-inventory-release/` preserves fresh
origin at tick3800,17,8Nella. Scene/prefs/view/save-root restored07:26:22UTC,
normal save hashes intact; no package or physicalDeck acceptance. Continue
the existing fresh-origin eastforge/kitchen route, using a pure-frost brew from
the remaining lichen rather than the mixed coating that can protect enemies.
Gather actual raw grain/pulp and preserve clay for pan repair. Next art cycle:
compose Nella's existing work area beside a field approach, retaining current
owners/rights/finite supplies and testing two cold seeds before acceptance.


## Latest continuation — fresh-origin forge/kitchen, 2026-10-25 08:02 UTC

The fresh New Game character now completed the six-site service route: finite
cellar clay/north reagent/Nella rights and crops/glade grain/east forge/kitchen.
Actual inputs tempered same sword2703 with pure Frozen9202, repaired pan13701,
paid2drams+grain/pulp, waited and collected one meal. F5→stop→ordinary Continue
retained exact captured identities,42HP/40drams at5830. Actual captures and
bounded acceptance are in [fresh route](GRAPHICS-G3-FRESH-OPENING-ACCEPTANCE.md).
Whole-scene art, seed65, latest packaging and physical Deck remain open.

Inventory follow-up fixes observed during the route are tracked in
[polish](GRAPHICS-G3-INVENTORY-POLISH.md): header focus, craft acknowledgment,
and truthful meal guidance/category. Then Nella scene composition is next,
using real greybox→capture→imagegen→native assets→ordinary play. Earned kitchen
checkpoint is `/tmp/coo-fresh-opening-after-kitchen/`; user save hashes intact.
No stale cache target justified deletion;0bytes removed.


## Latest continuation — inventory polish, storage recovery

Inventory selection, truthful craft-completion counts and meal guidance/category
are implemented and independently reviewed; native Unity233/233passed,0skip.
[Polish record](GRAPHICS-G3-INVENTORY-POLISH.md) preserves RED/GREEN and limits.
Final ordinary menu recapture was interrupted by ENOSPC. Root stopped Play and
terminated only the owned Editor after its cleanup snippet timed out, recovering
write capacity. Next: preserve these exact files on main, remove only the two
audited stale platform PlayerDataCache directories while Unity is closed, verify
normal save/isolation restoration, replay the menu and correct ForgeCommandsTests
namespace, then resume Nella whole-scene composition. No cleanup yet in this slice.


## Latest continuation — inventory replay accepted, 2026-10-25 08:43 UTC

[Inventory polish](GRAPHICS-G3-INVENTORY-POLISH.md) now has actual1280×800 menu
captures, exact earned stoneburr→tonic deltas, readable green completion and
correct meal guidance. Independent visual/readout review accepts this bounded
menu replay;245distinct native cases pass across233+12disjoint runs. This is
not latest-package, physical Deck or complete-opening art acceptance.

Earned checkpoint: `/tmp/coo-fresh-opening-after-inventory-polish/`. Normal save
and backup hashes and pre-audit LastGameID are restored; Play/isolation cleared.
A low-impact Editor custom-resolution cleanup index error remains separately
queued for native RED/GREEN. Continue with the documented Nella composition
cycle in [Nella plan](GRAPHICS-G3-NELLA-COMPOSITION.md), using a cold pre-Nella
checkpoint, actual capture→imagegen→deterministic real-owner layout→two-seed
ordinary Play comparison. Preserve approved sprite pixels and gameplay budgets.

Storage remains constrained. The first verified closed-Editor platform-cache
cleanup recovered9,453,568bytes despite261,492,736reportedallocatedbytes.
Push this evidence first, then revalidate/delete only the stale-clone manifest's
191rawlogs/fourgeneratedcachedirectories. Preserve every compressed evidence
counterpart, source, dependencies, saves, active main caches and current builds.


Storage continuation: after verified remote preservation at696a9ff35, exact
stale-clone cleanup completed:191redundant rawlogs/fourunusedgeneratedcaches.
Observed583,999,488bytes regained (about557MiB), not the2.418GiB allocation
total. Source, compressed evidence, saves, dependencies, active main caches
and current builds remain intact. See inventory-polish-stale-clone-cleanup.json.


## Latest continuation — restored graphics workflow, 2026-10-25 09:04 UTC

Inventory polish is accepted in bounded ordinary Play with actual screenshots
and245distinct native passes. Audit-view cleanup is also fixed: short full-token
labels and correct removal index pass22native cases and a direct create/remove
smoke. The single proven orphan from this root audit was removed; user presets
were not swept. Normal save files/backups and the exact pre-audit preference
remain unchanged/restored. Unity is open in clean SampleScene, out of Play,
without isolation or a presentation override.

The next scene cycle is [Nella composition](GRAPHICS-G3-NELLA-COMPOSITION.md).
The first imagegen concept is reference only; reject its changed UI, gradients
and exaggerated growth. A cold review found Nella already near the proposed
frontage, so retain a substantial primary field and clear service bay rather
than uniformly thinning an already sparse scene. No new layout code is applied
yet. Next: source-grounded RED for the bounded descriptor/mask, native integration,
then ordinary cold64/65 before/after gameplay checks. Preserve the current
approved sprite art, actual crop states, finite budgets and public approaches.

Current packages still predate this inventory/Nella material work; no new build
or physical Steam Deck acceptance is claimed. Storage has limited headroom
after exact guarded stale-clone reclamation; avoid large builds/downloads and
retain active caches. Full opening composition and regional coverage stay open.


## Latest continuation — Nella field/frontage accepted, 2026-10-25 10:21 UTC

[Nella composition](GRAPHICS-G3-NELLA-COMPOSITION.md) now has a coherent primary
field around its complete existing service group and shorter substantial remote
remnants. The first candidate passed tests but looked too empty and was rejected.
The revised candidate passes158 native Unity cases, with real cold seed64 and
fresh New Game seed65 screenshots independently reviewed. Seed64 ordinary paid
rights→gather→replant→Rain→glean→save/load/travel return preserves sampled
identities/state; seed65 verifies approach and payment only. Approved sprite
pixels and finite yield budgets are unchanged. Full opening/region art, latest
packages and physical Deck remain open.

Unity is out of Play in clean SampleScene, with audit view/prefs/isolation restored
and normal save/backup hashes unchanged. Earned checkpoints are
`/tmp/coo-fresh-opening-after-nella-composition/` and
`/tmp/coo-fresh65-after-nella-composition/`; the original pre-Nella64 checkpoint
is intact. Current packages still predate inventory/Nella changes. Next: review
the six whole scenes together, choose the most visible inconsistency and continue
the playable capture/concept/native integration cycle. Do not reopen this accepted
bounded layout solely to manufacture another iteration.

Storage follow-through at10:28UTC: verified all118 owned files and commit
`d313eac64` on origin/main before deleting four closed, regenerable database files
from the inactive verification clone's Library: ArtifactDB, ArtifactDB-lock,
SourceAssetDB and SourceAssetDB-lock. Observed available-space gain was
158,928,896bytes (151.6MiB), not the216,727,552byte allocation sum. The exact
[receipt](Verification/Live16x24/nella-composition-storage-cleanup.json) records
both values. Source, editable assets, saves/backups, dependencies, active main
caches and current builds remain intact. A bounded independent follow-up found
no safe target above500MiB; disk remains constrained at roughly340MiB. Do not
start a large build or erase useful active caches to force one. Smaller inactive
clone compiled/search caches remain candidates for the next guarded inspection.


## Next scene cycle — cellar floor, 2026-10-25 10:41 UTC

Six actual scene captures were reviewed together. The next bounded task is
[the cellar floor](GRAPHICS-G3-CELLAR-FLOOR.md): a quiet sparse stone material
beneath the existing walls/actors, preserving prepared sootroot beds, hazards
and fog. The real entry capture drove a built-in imagegen concept; its resized
HUD, altered lettering/actors and lighting are rejected. Only sparse broken
seams inform native authoring. Exact owner/state, RED/counter, submitted-Tilemap
and ordinary-return gates are recorded. No floor runtime/art import or new
gameplay acceptance is claimed yet. Existing Nella and inventory remain frozen.
Storage is still constrained; use the guarded stale-cache process, preserve
current packages and avoid heavy builds. Whole-opening/Deck/regional gates stay open.


Storage follow-up10:42UTC: after verifying `801f96d04` and all122 owned files
on origin/main, removed only the inactive clone's generated ScriptAssemblies
(164DLL/PDB files) and Search (11indexes/database files), with no open handles.
Observed additional available-space gain41,766,912bytes (39.8MiB), versus
81,870,848allocatedbytes. Combined observed gain for this Nella heartbeat is
200,695,808bytes (191.4MiB). Current free space remains about308MiB because
other activity consumes space between checks. No large safe remaining project
cache/build target was found. Main active caches, source, editable assets,
saves/backups, dependencies and current packages remain protected. Exact receipt:
[small-cache cleanup](Verification/Live16x24/nella-composition-small-cache-cleanup.json).


Cellar continuation10:54UTC: four editable native floor candidates and a34-case
uncompiled test draft are preserved outside Assets. No new floor runtime or Play
acceptance. Independent source review passed; actual lighting/repetition awaits
integration. The clean owned Unity Editor is normally closed. Exact older
player-build intermediates (477.7MiB allocation) are audited separately from
useful editor/import/JIT caches; preserve/push this owned work before any guarded
reclamation. See[the cellar plan](GRAPHICS-G3-CELLAR-FLOOR.md).


## Continuation — cellar floor accepted,2026-10-25 11:36UTC

The quiet native cellar floor is now implemented and boundedly accepted. See
`Docs/GRAPHICS-G3-CELLAR-FLOOR.md` and independent
`Docs/Verification/Live16x24/cellar-floor-gameplay-review.md`. Four editable16×24
frames resolve only actual visible ordinary Floor owners; prepared beds, crops,
coatings, pools, gas and memory retain their current priority. Foreground,
geometry and simulation are unchanged. Independent review's malformed duplicate
Floor finding was reproduced RED and fixed. Native Unity sweep290/290 passes,
0skips. Actual fresh seed64 entry→combat→spent seed harvest→pallet dismantle→
Examine→F5/F6→glade→cellar return preserves recorded floor/soil state; seed65
opposite lower passage is separately captured.All gameplay captures are1280×800. No package/Deck or complete-opening claim.

29.076seconds active stationary world produced no additional environment redraw
or resource growth. This is bounded counter evidence, not FPS. Normal saves,
backups and current Linux archive rehashed unchanged; ordinary editor preferences
and isolation restored. Before integration, source/evidence was pushed to main at
`f516e6d99`, then the closed Editor's exact stale player-only intermediates were
removed:341.2MiB actual recovery. Active caches/current builds were preserved;
about425MiB remains after testing, so no new package build or routine Library wipe.

Next: Warden En Garde/Lunge/Follow Through share generic diamond hotbar icons in
actual gameplay. Audit exact icon path; use the actual frame→imagegen→native
editable icon→real integration/test/play loop. Do not reopen accepted Nella,
kitchen or cellar art without a named defect. Whole opening and regional art
remain the priority; ongoing heartbeat continues.


## Continuation — Warden skill identity,2026-10-25 11:59UTC

[Warden hotbar](GRAPHICS-G3-WARDEN-HOTBAR.md) replaces three identical generic
diamonds with native guard/thrust/blade-and-step pictograms. Actual-cellar capture
→imagegen→editable pixel rows→native RED→implementation→105passing native tests
→ordinary New Game keyboard/cast/cooldown/aim/cancel/F5 and independent visual
review are complete. World art/layout and combat rules are unchanged. Actual
captures:`Docs/Verification/Live16x24/warden-hotbar-ready.png` and
`warden-hotbar-follow-through-aim.png`. Normal save/backup/current archive hashes
unchanged; audit prefs/view restored and Unity stopped cleanly. Packages still
predate inventory/Nella/cellar/Warden work; physical Deck and full opening open.

A separate click check found the hotbar still reading legacy mouse input while
inventory already uses the native device. Clicks failed; do not claim mouse
acceptance or rescale icon geometry from editor Screen readback. Next narrowly
fix/prove that input boundary with native RED and real clicks, then return to the
whole-scene opening gaps. No accepted art needs reopening. Storage about303MiB at
11:59UTC; preserve useful caches/current packages, no heavy build or speculative
cleanup. Heartbeat remains active for ongoing world/regional completion.


## Continuation — hotbar mouse corrected,2026-10-25 12:15UTC

[Hotbar pointer correction](GRAPHICS-G3-HOTBAR-POINTER.md) closes the actual click
failure found during Warden icon acceptance. One authoritative native pointer
pair with matching legacy fallback now feeds unchanged painted-card hit testing.
Native RED11/10fail→GREEN107/107, plus actual OS clicks for Lunge/Follow Through
AIM, En Garde cast and cooldown refusal independently reviewed. Same approved
icons/layout/world art. Editor stopped cleanly; normal save/backup/current archive
hashes unchanged. Earned mouse checkpoint:`/tmp/coo-hotbar-pointer-earned`.

Next: return to world completeness with ordinary owner discovery of the known
north incidental LeatherArmor6954 at16,17 and unidentified kitchen P glyph.
Use real current Look/Examine and capture before choosing the native item art
slice; do not infer identity from a glyph. Full opening/regions and current
package/physical Deck gates remain open. Two obsolete Unity Hub3.20.1installer
caches are audited for guarded deletion only after this work is verified on main;
current Hub/editor/dependencies/caches and packages remain protected.


## Preservation and storage closeout — 2026-10-25 12:21 UTC

Pointer code, native tests, actual mouse screenshots and living records were
verified on origin/main at `34fe9cc8b0758f3083e9abf5190712e430b8e41b`
(270 owned files byte-compared). Only then, after fresh version/hash/inode/type
and no-open-handle checks plus independent review, removed exactly the two
obsolete Unity Hub 3.20.1 installer ZIPs listed in
`Docs/Verification/Live16x24/hotbar-pointer-stale-hub-installer-cleanup.json`.
Observed filesystem availability increased by **381.2 MiB** to 567.5 MiB;
this is the measured delta, not an inference from file sizes. Earlier this
heartbeat's stale player-cache removal recovered 341.2 MiB separately.

All source/editable assets, saves/backups, current builds/archives, installed
Hub/editor, newer pending Hub update state and useful active caches remain.
Unity is out of Play with normal preferences restored. New packages remain
pending because current disk headroom is still inadequate for a safe build.
Continue the documented opening incidental-item identity pass next; this is
not opening or regional completion, and the graphics heartbeat remains active.


## Continuation — native opening equipment accepted, 2026-10-25 13:03 UTC

[Hatchet and leather armor](GRAPHICS-G3-INCIDENTAL-EQUIPMENT.md) now retain native
16×24 identity in actual ground, exact Examine, carried and equipped views.
Two earned seed64 branches verify pickup/equip/unequip/drop and F5/F6 continuity
without grants or teleport. Actual inventory captures:
`Docs/Verification/Live16x24/incidental-items-hatchet-inventory.png` and
`incidental-items-armor-equipped.png`. Native RED29→GREEN143, independent review's
raw-footprint RED2→final147/147; no skips. Editable rows, extraction/concept,
resource metadata and independent reviews accompany the implementation. Neutral
art does not assert condition/stats; no mechanics, geometry or loot changes.

61.592 seconds stationary active Play caused no environment redraw or equipment
resource growth; this is not FPS or Deck benchmarking. Normal save/backups and
current archive hashes are unchanged. Cleanup exposed a changed LastGameID
following native-runner activity; restored the verified original pointer with
Unity stopped, preserved the extra save and all evidence. Future native runs
must keep save isolation through delayed Play resumption. Corrected restoration
receipt: `incidental-items-final-restored-corrected.json`.

Next is whole-scene seed65 north approach and Nella lifecycle using the earned
Warden cellar checkpoint, not another isolated item packet. Full opening,
regional art, new package and physical Deck gates remain open. Read-only storage
inspection found no worthwhile stale data; no deletion. Current packages still
predate recent art/UI/input changes. Preserve them and useful active caches.


## Continuation — second-seed evidence and storage recovery, 2026-10-25 13:45 UTC

[Second-seed composition](GRAPHICS-G3-SEED65-COMPOSITION.md) records ordinary
cellar→glade→north→Nella travel, one mendleaf harvest, actual contested retreat,
Nella payment, mature harvest versus a young-crop counter-check, replanting and
initial watering/growth. Captures and exact owner receipts are real1280×800
Unity gameplay. Full north still use, crop maturity and save-return remain open.

ENOSPC interrupted the post-F6 capture. Evidence was verified on main via a
non-forced Git data API checkpoint (`3cec8c906`) before removing exactly the
superseded Oct11 fullscreen-skills archive; observed recovery122.16MiB. No source,
normal save/backup, current build/archive or active cache was removed. Original
save pointers, scene/view/preferences and all protected hashes are restored.
The isolated replay root was automatically discarded on emergency Play stop;
its original earned cellar checkpoint survives, so unfinished continuity must
be replayed rather than inferred. Current storage is still too low for a build
or long replay. No further clearly unnecessary large project cache was found.

Independent visual review confirms green world seams in the Examine backing.
Next narrow fix: world popup backing coverage with native RED/GREEN, then actual
Examine and action-menu captures after restoring sufficient disk headroom.
Next scene target remains the weakly grouped northern worksite and broad field
fragments; capture the unobscured yard before selecting a bounded layout change.
Do not replace approved sprites or add unrelated asset packets. Full opening,
regional graphics, updated package and physical Deck gates remain pending.


## Continuation — popup sampling fix, 2026-10-26

[Popup raster coverage](GRAPHICS-G3-POPUP-RASTER-COVERAGE.md) now records actual
GPU RED, rejected overlap/shared-grid candidates, 49 passing native tests and
ordinary earned north Play captures. UI SolidBlock has an opaque texture gutter;
geometry and approved art remain unchanged. Mendleaf actions/Examine and
inventory sword details have clean backgrounds. Editor isolation is restored.
Storage blocker is cleared (about432GiB available after separately authorized
OpenClaw error-log/XCTest cleanup); all other cleanup remains inspection-only.
Next: second-seed return/growth and north-yard composition. Updated packages,
physical Deck, full opening and regional graphics acceptance remain open.

## Continuation — seed65 crop return and readable north workpad, 2026-10-26

The real Nella marlroot loop now includes two ordinary Rain castings, outside-zone
F5/F6 and return to the same crop9223 at maturity; one harvest yields2more clods
and1seed. HP40 and42drams stay fixed. Exact receipts and limits are in
`GRAPHICS-G3-NELLA-COMPOSITION.md`; concepts/tests are not used as this evidence.

North still-court actual capture → built-in imagegen → extracted quiet existing
RoadStone footprint → transactional cold-generation placement → native RED38
(13expectedfails)/GREEN88 and review42 → ordinary fresh pre-north replay.
`GRAPHICS-G3-NORTH-COMPOSITION.md` records exact comparisons, preserved owners,
stock/routes and independent review.18visible ground owners survive ordinary
F5/F6 after real aisle combat; source terrain stays literal. Ground is added
only on fresh exact north enriched yards, not retroactively to saves. Accepted
sprites and former3D presentation are unchanged. Root restored Unity/save/view.

Approximately431GiB remains free; caffeinate is running. No storage cleanup is
needed or performed in this pass. Old current builds and archives are retained.
Next delivery action: freeze/push these source changes, build fresh dated Mac
and Linux/Deck -current packages, smoke-test actual Mac launch/input/inventory,
and record precise packaged/device limits. Full opening/regions remain ongoing.


## Continuation — fresh Mac/Linux delivery and glade composition next, 2026-10-26

Frozen source`c893ae58fea438d1d2bb6f803ace1e499df79acd` now has fresh dated
Mac and Linux`-current`packages. Both normal SampleScene builds succeeded with
0errors/4warnings. The new Linux archive is181files,128,641,647bytes,SHA256
`aa64b959de5c477f3dd10893f4207d4c81f469afe816cdd46f6f5a9b108b7618`;
all contained file hashes/sizes/modes match. See
[October26 packaged delivery](GRAPHICS-G1-PACKAGED-DELIVERY.md#9-fresh-delivery--2026-10-26-0405-utc).
Old October25 current packages remain preserved.

Actual Mac/Metal play accepts Continue into the earned north checkpoint,
current inventory`DMG / DEF`and far-right`[E]`,F1 sword reader,movement/F5 and
normal quit. A separate fresh Warden starts in the flat glade and moves one
cell. Six original screenshots are1280×856 including56pixels of macOS chrome;
game viewport1280×800. Evidence:
`Docs/Verification/Live16x24/packaged-flat-20261026-mac-play-report.md` and
`packaged-flat-20261026-mac-play-summary.json`. Both shutdown logs retain six
thread-finalization notices and one ComputeBuffer-disposal notice; no warning-free
or performance claim. Mac save/preferences are restored. Separate
`packaged-flat-20261026-editor-restored.json` confirms04:08:34UTC normal
SampleScene/OSX,no Play/compile/import/build/isolation and null save root;
`packaged-flat-20261026-project-restored.json` records29generated files restored.
All29hashes and four protected normal save/backup hashes independently match;
tracked`Assets`/`ProjectSettings`diff is empty. Platform reload first stalled in
Mono domain unload for aboutroughly9minutes: root sampled and TERM-stopped its owned
idle editor,restarted Linux64,preserved offered scene backups,finished the
build and restored OSX. This recovery is not a code fix or recurrence guarantee.

About430GiB is free; no deletion needed or performed. The next design uses the
actual `packaged-flat-20261026-mac-new-game.jpg` as the glade baseline: uneven
vegetation drifts should strengthen grouping while preserving existing service
placements,finite stock,paths and access. A concept is generated; no new glade
runtime changes or layout/art acceptance are claimed. Keep the approved16×24 sprites and complete the established capture→imagegen→
extraction→native implementation→ordinary Play cycle. Linux execution,physical
Deck,complete packaged journey,full opening art and regional kits remain open.


## Continuation — glade vegetation hierarchy accepted, 2026-10-26

[Opening glade composition](GRAPHICS-G3-GLADE-COMPOSITION.md) completes the actual
capture → built-in imagegen → native owner arrangement → test/play/review loop.
Wild reeds now form three uneven groups with genuine interior gaps; grass
clusters leave quiet field and ruin approaches. Approved sprites, all fixed
geometry and the per-seed blueprint/plant budget remain intact. Bush fuel
positions intentionally move on fresh generation; saved graphs remain literal.
No renderer layer or per-frame work was added, but this is not a measured
performance improvement or a purely cosmetic balance claim.

Original-plan RED:63 cases,42 expected failures. Initial167-pass candidate was
rejected after ordinary walking exposed solid ellipse/kite shapes. Added shape
RED91:28 expected failures. First fray candidate passed191/195; final shared
ground pockets and uneven edges passed195/195,0 skipped. Literal original
budget/fixed-footprint/ordered Blueprint/VisualID-slot pins were retained.
Independent source and native-scale visual review found no remaining material
issue within this bounded pass. Native checks do not prove every runtime ID or
visual state. Actual two-seed walking and return receipts are separate evidence.

Seed65 ordinary field read/eastern walk → cellar F5/F6 → return preserves all
447 Bush/Reeds IDs, positions and VisualIDs. Seed64 basin read/eastern walk →
east connector F5/F6 → return preserves all450. Both preserve40 HP/50 drams
at those return checkpoints. Final seed64 eastern view is Dusk/tick730 after
an unintended pond leg; it is not a matched-time comparison. All original
captures, rejected candidates and exact owner receipts are retained.

Next is the eastern tempering shelter. Current seed64 entry/contested approach
captures confirm that its forge and broken wall float separately on broad green
ground. Ordinary combat reduced the test character to12 HP near the forge; no
service-use acceptance is claimed. Built-in concept proposes only a small quiet
work footprint; generated texture/soft pixels are rejected. Existing owners,
finite supply and threats stay authoritative. The exact earned pre-east
checkpoint is preserved for comparison, as recorded in the next-scene plan.

Root stopped its isolated Play and restored normal editor/save state. All four
protected normal-save hashes and two foreign PNG variants are unchanged.26
importer-only metadata whitespace rewrites were backed up and restored. About
430GiB remains free; no deletion. Current dated Mac/Linux packages remain at
c893ae58f and therefore predate this glade layout. Full packaged journey,
physical Deck, whole-opening art and regional kits remain open. Continue the
next scene using the same actual-capture and native-integration gates.


## Continuation — eastern forge workpad accepted, 2026-10-26 05:52 UTC

[East shelter composition](GRAPHICS-G3-EAST-SHELTER-COMPOSITION.md) now groups
the existing forge and broken wall with an optional quiet native work surface.
No new sprite packet, stock, enemies or gameplay service was added. Twelve
candidate cells are filtered through actual ownership/ground; ordinary seed64
admits11, seed65 admits9. Flowers and the forage margin remain intact. Shared
north/east floor validation now protects malformed/late-mutated payloads and
Owned Grass. Native RED38:31fail/7pass →178-case seven-fixture GREEN →42-case
focused review rerun,182distinct cases total, zero skips.

Actual1280×800 ordinary editor screenshots show seed64 adjacent forge use,
menu/reader and F5/F6 plus glade exit/east return with all11floor/substrate IDs
retained. Seed65's first southern approach died; an ordinary upper-lane replay
reached the forge at65,3,31HP/50drams, used its menu/reader and preserved all9
floor/substrate IDs through F5/F6. No supplies were granted, and no crafting
success or seed65 border return is claimed. Before/after views differ in
runtime IDs, fog and combat; independent visual review accepts the bounded
composition improvement, not exact simulation equivalence or full-scene finish.

Root restored normal editor/save/view state and verified all four protected
normal save/backup hashes and two unrelated PNG hashes.26 importer-only meta
rewrites and an incidental generated census were backed up and restored.
About429GiB free; no storage deletion. The dated Mac/Linux packages are still
sourcec893ae58f, preceding glade/east changes; physical Deck remains untested.

Next: a coherent current six-zone1280×800 journey review with inventory,
interaction/state and earned supplies, then current-package parity and actual
Mac journey replay. Choose the largest remaining scene-level defect from those
views. Do not keep producing disconnected packets. Full opening/regional art,
complete forge lifecycle and device acceptance remain open; continue this work.


## Continuation — current packaged journey and underfoot fix, 2026-10-26

[Current packaged journey](Verification/Live16x24/packaged-glade-east-journey-report.md)
records fresh Mac seed200893250 visiting all six opening graphs with ordinary
keyboard controls and no grants. North harvesting/brewing → East tempering a
looted sword succeeded; kitchen dialogue and quit/Continue retained the earned
game. Current dated Mac/Linux outputs now include glade/east composition at
source dce87081d. Linux/device execution remains unverified. A viper killed Nella
before reserve service, so this is a six-zone visit, **not six-service acceptance**.

Actual play exposed underfoot Harvest being hidden by treating the player as
another pile occupant. [Underfoot consistency](GRAPHICS-G3-UNDERFOOT-ACTIONS.md)
now has RED15/18fail →176/176 native GREEN, baseline-confirmed fixture drift
repair, independent review and actual earned editor Harvest/depletion captures.
The frozen dce packages predate this small fix; do not describe them as containing
it. Existing sprites, harvest authority and yields are unchanged.

Next scene: kitchen court and adjoining orchard, using actual packaged18/22
captures as playable baselines. Independent review prioritizes Beehive glyph6
fallback and small indistinct service silhouettes/entrance. Preserve accepted
art; use the imagegen/extraction/native/editable/integration/playtest loop and
avoid unrelated packets. Nella service survival needs a separately scoped audit.
Normal saves (776 files), preferences, editor state and foreign work are protected;
no storage deletion. Whole-opening art, regional kits and physical Deck gates
remain open. Continue across sessions from this bounded next scene.


## Continuation — kitchen/orchard identity accepted, 2026-10-26 07:35 UTC

[Kitchen/orchard](GRAPHICS-G3-KITCHEN-ORCHARD.md) completes the actual capture →
imagegen scene/parts → editable native pixels → integration → ordinary Play loop.
Wild hives now have one neutral native 16×24 image in the six admitted graphs
and matching examination art. Three quiet court textures replace the small
pebble marks without moving the 15 floor owners or services. Original art and
3D sources remain recoverable. RED: 41/42 failed as expected; GREEN: 260/260
passed, with independent source, runtime and ordinary-scale visual review.

Actual earned seed 200893250 replay shows matching court views, hive examination,
harvest yielding two Honeycomb and removing the hive, F5/F6 and a western exit/
return without respawn. The two previously earned FireClay repaired pan 13771;
save/load retained that repair. The meal batch remains Idle. NPC positions differ
between hive replays, so they are not identical whole-world comparisons. The
sampled court view used 38 draw calls before and after; these are Editor samples,
not physical Deck timings.

All 776 normal save files, 65 protected art/source files and two foreign PNGs
are unchanged; normal editor/view/save settings are restored. The 26 incidental
importer whitespace rewrites were backed up and restored. About 429 GiB is free;
no storage cleanup occurred. Dated Mac/Linux packages still use source dce87081d
and predate both the underfoot fix and this artwork.

Next: improve repeated scalloped hedge silhouettes using disclosed-neighbor
end/corner/interior art, preserving exact owners, collision and approved props.
The earned fixture `Verification/Live16x24/Fixtures/kitchen-orchard-earned-repaired`
retains the actual harvest and pan repair for subsequent comparisons. Separately
reproduce Nella's initial threat exposure in seed 200893250 before changing
placement; mortality is intentional. Full opening/regional art and package/device
acceptance remain open. Continue from this bounded next scene refinement.


## Continuation — connected hedge accepted, 2026-10-26 08:17 UTC

[Hedge continuity](GRAPHICS-G3-HEDGE-CONTINUITY.md) completes actual baseline →
imagegen reinterpretation/extraction → two native source passes → narrow runtime
integration → native338/338 → independent review → ordinary Play. Existing hedge
owners now form continuous ends/corners/runs through disclosed cardinal neighbors.
The isolated approved image and every original source remain intact. Native RED
was30/30 expected failures; the first337/338 GREEN exposed only an overlapping
footprint test setup, corrected before final338/338 with zero skips.

Actual1280×800 earned kitchen play shows matching court/corner views, examination,
nonlethal hit, destruction, walking through the real gap, F5/F6 and a western
chunk exit/return retaining the gap. No grants or geometry edits. One added shared
atlas batch was observed (38→39draw calls); this is not a Deck timing benchmark.
All776 normal saves and two unrelated PNGs are unchanged;42 prior art/source files
are preserved. Normal scene/view/save settings restored;28 importer whitespace
rewrites backed up/restored. About428GiB free, no cleanup/deletion.

Next: fresh dated Mac/Linux current packages from this pushed source, including
underfoot actions, hive/court and connected hedges, then actual Mac ordinary
Continue/inventory/reader/movement/save-return. Existing dce87081d packages predate
those changes. Preserve previous outputs. Full six-service/whole-opening art,
regional kits, Linux execution and physical Deck remain open. Nella's service
loss is still a separate scoped audit; do not declare mortality itself a bug.


## Continuation — delivered kitchen/hedge packages, 2026-10-26 08:34 UTC

[Current delivery](Verification/Live16x24/packaged-kitchen-hedge-report.md) freezes
source8902d3731 in new dated Mac/Linux `-current` outputs, including underfoot
Harvest, hive/court and connected hedges. Both builds0errors/4warnings; archive
128645260bytes,181files independently hash/size/mode verified. Mac ordinary
Continue/inventoryDMG-DEF/rightmost[E]/item+hedge+hive readers/underfoot harvest
and quit/Continue are captured. Harvest yieldedone Honeycomb and remained absent
after return. These12 actual1280×800-viewport screenshots are not concepts.

Normal776savefiles and preference mappings preserved; editor idle inSampleScene,
StandaloneOSX with no isolation/saveoverride. All24765 tracked source hashes and
two unrelated PNGs unchanged after backing up/restoring32 build-generated files.
About427.7GiB free; no cleanup/deletion. Known shutdown thread/ComputeBuffer
notices persist. Linuxexecution,physicalDeck,fullsix-service journey and whole
opening/regional art remain open. Current archive path/checksum are inG1/latest.

Next selected scene defect: east meadow's repeated dense flower ellipses overpower
the small forge. Capture current matched entry/upperapproach views, reinterpret
withimagegen, then refine eligible dressing into unequal interiors/holes and a
quiet destination margin while preserving budgets,workpad,owners and routes.
After that, audit plain teal water rectangles outsideglade for shared bank
presentation. Nella active/fallow hierarchy needs a current matched view before
changing acceptedwork. Continue this scene-level cycle; no unrelatedassetpacket.

## Continuation — East meadow composition accepted, 2026-10-26

The [East meadow pass](GRAPHICS-G3-EAST-MEADOW-COMPOSITION.md) is accepted for its
bounded density/readability defect. Actual before captures, imagegen critique
and extraction led to generation-only thinning of existing flower bodies.
Approved sprites, routes, service owners, stocks and the optional forge pad
remain unchanged; no new atlas or rendering subsystem was added. Final native
regression passed **244/244**, zero failures or skips, at 09:25:18–09:27:24 UTC.
Full-generation controls preserve all compared nonflower fields, pad positions
and reservations for seeds 64, 65 and 200893250; flowers change from 185/201/216
to 120/130/139. Allocated equipment-cache keys are normalized to an equipped-item
multiset with duplicate counts preserved; this does not certify anatomical
slot assignment between independently generated worlds.

Ordinary seed 64 Play reached East (7,18), tick 1140, HP 40 and 50 drams, and
opened the forge reader through C/Down/X. F5/F6 preserved all 246 captured owner
records exactly when compared by ID. An ordinary westward exit and return
preserved all 120 flower IDs and positions; two Marlback hatchet hits reduced
HP to 15 before re-entry at (0,16), tick 1250. Seed 65 independently entered the
fresh East and reached (65,3), tick 1230, HP 40 and 50 drams, then opened the
same reader. A timed-out movement batch was corrected from the observed actual
position. Its older screenshot is a qualitative comparison only; no matched
before/after combat state, new seed 65 save-return or crafting is claimed.
Actual captures and route details are linked in the pass record.

Current Mac/Linux packages remain at `8902d3731` and therefore precede this
meadow change. No new per-frame work was added, but no FPS improvement was
measured. Linux execution, physical Deck, the complete six-service journey,
whole-opening acceptance and remaining regional art are still open. Repeated
brine application/expiry log messages were observed and left unchanged outside
this graphics slice.

Next: capture the actual East pure-water patches, then test a narrow reuse of
the existing disclosed-neighbor bank art. Preserve mixed liquids, ice, hidden
neighbors, hazards and authoritative owners; do not broaden ground admission
to every surface. The water handoff in the pass record is a plan, not implemented
or accepted work. Preserve current Nella art pending a matched active/fallow
scene review and its separate service-loss audit.


## Continuation — 2026-10-26: East surface-water material continuity

Bounded ordinary Unity Play accepted in [East water record](GRAPHICS-G3-EAST-WATER.md).
The exact admitted East graph now reuses opening-water bank frames only for a
sole live water coating; dry ground, other regions, mixed liquids, ice and
hidden-neighbor disclosure remain unchanged. Existing artwork/source bytes are
preserved. Built-in imagegen reinterpretation and part/state extraction used
an actual earned-bank screenshot, with no concept pixels in production.

Native RED36expectedfailures preceded the minimal ground-catalog change;
GREEN229/229passed,0skips across six fixtures. Independent review found no
material blocker. Actual1280×800 before/after shows the same earned(50,20)bank
and(51,20)wet crossing.371observed owner tuples match by exactID; inventory,
coatings,HP/drams/tick match before/after and F5/F6. RealDown5to kitchen(51,0),
Up5return retains bank art and water footprint. Freeze/melt remains native
rather than ordinary spell-input coverage.86.13s mixed editor profile includes
crossing/save-load/idle time and is not a Deck or controlled performance claim.

OwnedPlay ended; normalSampleScene/prefs/view/save root restored.776normal save
files,374preexisting art/source files and2foreignscreenshots remain unchanged.
No storage deletion. About427.5GiBfree. Exact owned source/evidence/doc changes
are the main-publication unit; current packages8902d3731still predate meadow
and water. Next refresh dated Mac/Linux packages, preserve all existing outputs,
and check ordinary Mac Continue/new-game/crossing/save-return. Then resume the
complete six-zone review and separate Nella service-loss audit; wholeopening,
remaining regional art,Linuxexecution and physicalDeck acceptance remain open.


## Continuation — meadow/water packaged parity, 2026-10-26 10:30 UTC

[Delivery report](Verification/Live16x24/packaged-meadow-water-report.md) records
new dated Mac/Linux `-current` outputs from remotely verified2a8e71044. Both
builds succeeded with0errors/4warnings. The128645547byte Linux archive SHA256
is`e0ddfc99a23e4446db3e2bfbb65f2efb3e7805c95994eb5322cd46fddb6b32b3`;
181files/hash/size/mode independently checked. Previous8902d3731archive is intact.

17original Mac JPEGs show ordinary earned water Continue/crossing/F5/quit/relaunch/
Continue/kitchen-border-return; current DMG/DEF/right[E] inventory and sword
reader; separate seed64 fresh East generation and forge menu/reader; and a clean
new Duelist start/move. No presentation forcing or gameplay grants. Independent
image review accepts those bounded paths, not a new whole six-service journey.
Existing shutdown thread and ComputeBuffer notices persist. Linuxexecution and
physicalDeck performance remain unverified.

Normal editor restored idle/OSX/cleanSampleScene with no isolation/view/save
root override.32reviewed generated build files backed up/restored;47580prebuild
tracked hashes match.776normal saves,374preexisting art/source files,two foreign
PNGs and both logical preference domains verified. Three test Saves runs were
moved to protection archives, not deleted. About427GiBfree; no storage cleanup.

Next: current-package Nella frontage/crop-state acceptance from the verified
seed65 north-border earned checkpoint, then whole six-zone scene review. Treat
her loss on randomseed200893250 as a separate gameplay audit: mortality is
intentional; existing evidence does not prove unavoidable initial acquisition.
Preserve accepted art until a current matched view demonstrates a named defect.
Wholeopening/regional art and physical-device acceptance remain open.


## Continuation — Nella current-package crop loop, 2026-10-26 10:42 UTC

[Actual replay](Verification/Live16x24/packaged-nella-report.md) extends the same
2a8e71044Mac package acceptance without changing runtime or art. The unchanged
seed65north-border checkpoint is now preserved inrepo with hashes. Ordinary
Up/Up17 reaches garden20,7;young moist marlroot matures after two actual waits,
Hharvests2clods+1seed,Tabpickup;F5quit/relaunchContinue preserves the depleted
prepared bed. Nella remains20/20HP and her tied-row explanation accessible.
Native seed/clod portraits agree between inventory and reader;prepareoneclod
leaves3clods+1FireClay.25originalCUAimages and finaltick2290metadata retained.
An extraEscape/Pause detour is explicitly recorded and not counted as waiting.

Independent review accepts this bounded current frontage/product lifecycle.
No new sprite/layout defect warrants remaking approved art. Pickupmenu glyphs
are its text convention; futureiconpolish should reuse existingnativeassets.
This is earned return state, not fresh generation/payment or a complete fresh
six-service journey. Bothprefs,776normalsaves,374art/source files and2foreign
PNGs verified;testSavesarchived,no source/ProjectSettings diff,no storagecleanup.

Next review the six-zone owner/state coverage together and select a demonstrated
scene-level gap. Cellar wateredgrowth,incidental loot/actor outcomes,additional
cropfamilies,wholeopening/regional art and physicalDeck remainopen. Keeperloss
onseed200893250requires separate reproduction before balance/protectionchanges;
currentevidence proves mortality,not unavoidablefirst-activationdanger.


## Continuation — combat aftermath identity, 2026-10-26 11:27 UTC

[Aftermath pass](GRAPHICS-G3-OPENING-AFTERMATH.md) adds original native16×24
Magpie/Viper remains and the existing VenomGland, following actual capture,
imagegen reinterpretation, part extraction and editable flat Blender source.
Living/approved art is unchanged. Native236/236passed,0skips, including51new
exact-identity/custody/state/adversarial cases. Independent reviews clean.

Actual isolated seed65 controls picked up the earned magpie corpse9225, displayed
it in inventory and full Examine, dropped it at51,13, stepped aside and F5/F6
retained its native body and metadata. HP35/tick2650 includes ordinary enemy
attack/bleeding, not fabricated safety. Original pile red% is RawMeat atlayer5
covering corpse atlayer3; priority is preserved. Viper outcomes remain native
test coverage, not ordinary encounter acceptance. Actual04–07captures linked
in the pass. Wholeopening/regional and physicalDeck acceptance remain open.

OwnedPlay restored;776normal saves,374approvedart/source files,two foreignPNGs
unchanged.29whitespace-only generatedmetas backed up/restored.No storagecleanup.
Current2a8e71044Mac/Linux packages predate aftermath. Next: current North sole-
water baseline and bounded shared bank reuse; preserve dry court/field, mixed
liquids, ice, hidden neighbors and every other graph. No broad loot reskin.


## Continuation — North water material continuity, 2026-10-26 11:49 UTC

[North water pass](GRAPHICS-G3-NORTH-WATER.md) reuses the approved native bank
frames for sole live water on exact admitted North. Actual capture, imagegen
reinterpretation and part/state extraction led to a minimal scope change;
no new artwork, entities or resource changes. Dry North terrain and mixed,
frozen, hidden or foreign graph states retain their existing presentation.

Native22expectedRED →299/299GREEN, zero skipped. Actual seed65 Continue at
(58,16), crossing the PeatBog, F5/F6, garden exit and North return are captured.
Both matched and reload comparisons retain205owners plus player/inventory/
coating state exactly. Bog6905 retains70water; its source lease4 is distinct
from the observed permanent surface coating. Independent review found no
material issue. A bounded26.4s editor profile is retained without a device
performance claim. Normal scene/preferences/save root restored;776normal
saves,374approvedart/source and two foreign screenshots unchanged. No cleanup.

Current2a8e71044Mac/Linux packages precede aftermath and North water. Next
refresh dated packages, preserve all earlier outputs, then replay actual
Mac Continue/corpse inventory/North crossing and save-return. Whole opening,
cellar watered growth, secondary crops, regional kits, Linux execution and
physical Deck remain open. Do not replace those gates with asset counts.


## Continuation — current aftermath/North packages, 2026-10-26 12:02 UTC

[Delivery](Verification/Live16x24/packaged-aftermath-north-report.md) freezes
runtime source `e49047200f28708d4bfd77fb7007d2c7843ba2ab` in new dated Mac and
Linux `2026-10-26-aftermath-north-e49047200-current` folders. Both builds
succeeded with0errors/4warnings; all181Linux archive files independently match
hashes/sizes/modes. ArchiveSHA256
`089a9cc4d06b1d3c20bd2c9052bd606fbeae8d13b97ef25c632edb181ae0e400`.
Earlier builds/archive preserved. Source and delivery are now aligned.

Fourteen actual Mac captures verify two earned paths: NorthContinue/crossing/
inventory/F5quitContinue/gardenexit/return, and magpiecorpse pickup/inventory/
Examine/drop/F5quitContinue. Native body persists at51,13; player52,13HP35
includes an ordinary enemystrike. Currentinventory DMG/DEF and far-right[E]
are visible. Existing shutdownthread/ComputeBuffer notices remain recorded.
No physicalDeck/Linuxexecution/performance/fullopening claim.

Normal editorOSX/scene/preferences/save root restored;47,731tracked prebuild
hashes,776normal saves,374approved art/source and two foreign screenshots
verified.33generated files backed up/restored; both test Saves runs archived.
No storagecleanup. Next: ordinary cellar watered-sootroot growth/revisit and
owner-state review, then remaining secondary crops/regional kits. Preserve
accepted artwork absent a named defect. Heartbeat remains active.

The next earned checkpoint is now retained at
`Docs/Verification/Live16x24/Fixtures/cellar-earned-dry-sootroot64`: unchanged
seed64cellar tick590/HP30, expected player65,8 and drycrop4729at65,10.
Read carried WateringGrimoire if needed; existing Rain radius3 and40moist
units require topping up to reach560wet worldticks for stage0→ripe2.
Capture actual young stage1 and save/reload before completion. Existing
cellarframes3/4/5 andproducts6/7 already cover these states; this is a pending
ordinary-play gate, not evidence of missing artwork or new acceptance.


## Continuation — current packaged cellar lifecycle, 2026-10-26 12:26 UTC

[Cellar growth](GRAPHICS-G3-CELLAR-GROWTH.md) closes the ordinary watered
sootroot gap in the e49047200 Mac package using the unchanged earned64fixture.
Twenty-six actual captures show dry seed→watered→young, save/quit/Continue young,
ripe, normal harvest2pulp+1seed, pickup with native inventory art, replant, water,
real surface stairs/return and dry sprout after finite water expired. Savedticks
620/890/900/1170/1800 andHP30 retained. No grants, forced growth, runtime/art
changes or new native test claim. Independent visual review found no mismatch.

Both preference maps restored, isolated Saves retained outside normal location;
776normal saves,374approvedart/source and2foreignPNGs unchanged.30whitespace-only
importer metas backed up/restored. Existing shutdown notices retained. No cleanup.

Next: fresh current-generated opening scene framing at1x/2x through6zones,
ordinary combat feedback and finite native/fallback census. Current packages
remain e49047200. Older saved Northgeometry is not a current generation defect.
Secondary crops, remaining regions, Linux execution and physical Deck remain
open. Keep approved art stable unless the fresh scene reveals a named problem.


## Continuation — 2026-10-26 playable native zoom

[Plan, RED/GREEN and actual captures](GRAPHICS-G3-PLAYABLE-ZOOM.md).
The fresh scene review found controller zoom could never reach2×atDeck layout.
Fixed real input access without changing approved art/default framing.
23native cases pass;3broader log pointer preconditions also fail on unchanged
code and are disclosed. Actual1280×800Game views verify2×, notice targeting,
inventoryreturn and ordinarycamera tracking. Package newsource next, then use
its ordinary zoom for six-zone scene review. Do not use oldsavedlayouts to
judge currentgeneration; newseed224401440fixture is preserved.


## Continuation — 2026-10-26 delivered playable zoom

Current packages freeze47d678995 and now contain the zoom fix. Actual Mac
captures verify1×/2×/4×, inventory return, ordinary stairs into the cellar and
back, save/relaunch/Continue. Source23native cases pass; disclosed3 broader
log-pointer precondition failures also occur on unchanged code. Independent
visual/archive review passed. No new artwork was warranted for this camera
repair. All normal gameplay state and approved assets remain preserved; storage
inspection only. Read the package record and continue the fresh seed224401440
six-zone scene review using the now-reachable ordinary zoom. The surface/cellar
path is accepted, but not complete regional or device coverage.


## Continuation — current-scene shrine and Nella blocker, 2026-10-26 14:00 UTC

[Current scene review](GRAPHICS-G3-CURRENT-SCENE-REVIEW.md) followed the fresh
seed224401440route atordinary1×/2×. It found RiverShrine6915at23,7 rendered
as an underscore. Actual capture→imagegen→editable native Blender geometry→
optional catalog integration now supplies a low stone/recess/token frame.
31focused+184surrounding native tests pass. Actual1×/2×, Examine, paid donation,
F5/move/F6 and North→Nella→North revisit verify one stable owned identity;
actor blessing/currency change correctly. Approved art unchanged; no mechanics
changed (SanctuaryPart stale comment corrected). Latest packages47d678995
predate this source addition. No physicalDeck/Linuxexecution/fullopening claim.

Next significant blocker: Nella spawned eight cells from a hostile scrabbler
and died during the first20arrival moves before service access. Captures20/21
preserve alive→dead, not a completed service claim. Investigate generation
clearance while keeping mortality and roaming enemies. Then reuse existing
art where possible for Nella's five bare CopperPipe glyphs and three PeatBog
cells as one conductive pipe/bog corner. HollowStump and loose Cudgel remain listed
fallbacks. Complete remaining fresh East/Orven views; don't call six-zone
composition accepted yet. Earned shrine checkpoint is versioned under
Fixtures/current-scene-earned-shrine224401440. Normal saves/preferences restored;
no storage cleanup; heartbeat remains active.


### 2026-10-26 — Nella initial service access accepted

The next [scene-review continuation](GRAPHICS-G3-CURRENT-SCENE-REVIEW.md)
records a failed radius-only fix, followed by the accepted southern-approach
margin. Cold connected11.8 placement now considers hostile sight, speed and a
real footprint-avoiding route; preserves existing population/RNG/stock and later
mortality. 15focused+89neighboring native cases pass. Ordinary replay reaches
Nella at31,17 from20,24 in17steps, opens chat/trade, buys a seed, saves/reloads,
and returns across the border with the same NPC and reduced stock. Capture34
shows the actual native close view; the complete region remains unfinished.

Use the new earned post-purchase fixture current-scene-earned-nella224401440
for the next scoped view. Conversation28 exposes a legacy letter portrait;
audit exact-subject reuse, then continue the conductive pipe/bog corner and
fresh East/Orven views. Packages47d678995 predate these source changes. Normal
saves/preferences/editor restored; no storage cleanup. Keep continuation active.

### 2026-10-26 continuation: dialogue identity integrated

Nella and the opening warden now use their exact approved native art in the
conversation owner pane. Reuses the current world/examine resolver and
color-correct flat drawer, with exact session/current-subject checks. No art
replacement, description callbacks, model capture or texture copies.
31 focused and68 adjacent native cases pass; actual35–46 screenshots show
topic changes, trade cleanup, reopening and distinct NPC frames. Both32×48
portraits fit with labels/choices. See the current-scene review for scope and
failed/pre-fix evidence. Ordinary test saves/preferences/view are restored;
storage remains inspection-only. Current packages47d678995 predate this work.
Next: Nella's conductive pipe/bog corner, then fresh East/Orven whole views.


## Continuation — 2026-10-26 current journey repairs

Read GRAPHICS-G3-CURRENT-PACKAGE-JOURNEY.md, GRAPHICS-G3-NELLA-POPULATION.md and
GRAPHICS-G3-PICKUP-FOOTER.md. Eighteen actual Mac-package captures extend fresh
Warden seed242967504 through cellar combat/harvest/replant/water and North
combat/finite frozen-coating brewing. Nella's entire service was missing; this
is a failed journey gate, not full-opening acceptance. Keep the failing fixture.

Corrected source now passes139native tests after reproduced RED. Exact cold
Nella encounter slots use the existing bounded grazer policy; other zones/saves
and published owners remain literal. Ordinary unchanged-cellar replay reaches
Nella, opens native examine/chat portraits, pays8drams and reloads the same
owner/rights. Six pickup screenshots verify backed readable hints, reader return,
shrink, close and actual material recovery. Source corrections are not in the
current5a3c74359packages yet. Approved art/source and776normal saves preserved;
owned tests stopped/restored. No storage cleanup.

Next: fix the separately observed dialogue footer in conversation13, freeze a
corrected delivery, then resume actual East/Orven and six-site revisits. The
corrected earned Nella fixture is current-journey-corrected-nella242967504.
Whole opening, regions, physical Deck/Linux execution and performance remain
open; keep the graphics heartbeat active. Avoid substituting asset counts for
playable acceptance or overwriting the old failed package evidence.


## Continuation — 2026-10-26 dialogue footer accepted

Dialogue now backs the complete owned panel and renders controls in readable
light gray. Native RED8/11failures → fill-only counter2/11failures → final
100/100focused/neighboring GREEN after a verified assembly reload. Four actual
Nella conversation/topic/return/closed screenshots pass independent review;
no state changes or lingering tiles. Normal saves/art/foreign work preserved,
Editor idle and isolation restored. See GRAPHICS-G3-DIALOGUE-FOOTER.md.

Next: freeze and push these fixes, build new dated Mac/Linux-current packages
per GRAPHICS-G3-NELLA-FOOTERS-DELIVERY.md, verify real packaged chat/save/Continue
and restore all state. Then continue East/Orven/revisits. Existing5a3c74359
packages predate Nella cold-population and both popup fixes; full opening and
physical Deck acceptance remain open. Storage checks are inspection-only.


## Continuation — 2026-10-26 corrected Nella packages delivered

Runtime99b86bb026dd19967f2fb7f1e6997b9a36df41f4 is pushed and now built into
new Mac/Linux `2026-10-26-nella-footers-99b86bb02-current` folders.17actual Mac
captures verify Nella chat/topic/close, equipment damage/defense/[E], native
portrait, real two-stack pickup/reader/shrink/close/recovery and save/quit/Continue.
Archive181files independently verified; SHA125eeb9cb067d8d5a92fcbb2c215011302739763cfad190db2aef5b5c22adabe.
See Verification/Live16x24/packaged-nella-footers-report.md for rejected initial
snapshot, corrected process guard, source test gates and limits. All48,445tracked
objects restored to baseline; normal saves/prefs, approvedart and old5a3builds
preserved. Editor idle OSX, isolation/AutoRefresh restored. No storage cleanup.

Next: continue ordinary East/Orven/revisits from earned fixture
Verification/Live16x24/Fixtures/current-journey-packaged-nella242967504
(player63,9,HP31,tick2290,42drams,seed242967504). This checkpoint is genuinely
earned but its already-generated Nella is not a second fresh-population test.
Full opening/region, Linux execution, physical Deck and frame-time gates remain
open. Keep heartbeat active and approved art stable; fix named actual gaps.


## Continuation — 2026-10-26 earned East/kitchen and action-menu correction

Read GRAPHICS-G3-EARNED-EAST-JOURNEY.md and GRAPHICS-G3-ACTION-COST-READABILITY.md.
The unchanged seed242967504 Nella checkpoint continued through North/glade/East
and Orven in actual package99b86bb02. Two glade grain, finite BogSap, real viper
combat/tonic/poison, two-clay repair, two-dram paid meal, Working save/full-quit/
Continue, Ready parcel, collection and East/kitchen return are recorded in22
actual screenshots. Collection levelled up and healed the player; the still-carried
meal did not cause that healing. Source art was unchanged.

Menu width now expands from48 to78 columns using all action labels and clears
its prior foreground footprint. An actual mouse failure exposed stale legacy
coordinates; world actions now read one native position/left-edge pair, with
coherent legacy fallback. Reproduced RED and final115/115 native GREEN, six
actual Editor captures, and independent code/visual reviews accept the bounded
fix. Full price/F1/close, real right-edge click costing40→38drams and short-menu
Examine all work. The native test wrapper's early startup timeout is recorded;
completed Unity XML confirms115 executed passes. Editor/test isolation restored.
776normal-save files,374protected sources and2foreign PNGs unchanged.32import
metadata deltas were proven whitespace-only before exact restoration. No cleanup.

Next: freeze/push the corrected source, build dated Mac/Linux-current outputs
without replacing old builds, test the actual packaged price/mouse path and
restore normal preferences/saves. Then resume the ordinary collected checkpoint
`Verification/Live16x24/Fixtures/earned-east-collected242967504`
(kitchen55,17,HP42,level2,tick5670,40drams; meal carried, empty board).
Complete North/Nella service revisits and remaining forge/crop gates; a visible
En Garde hotbar label clipping remains a named follow-up. Do not restart the
art pipeline or alter accepted sprites without a demonstrated defect. Full
opening, regional coverage, Linux execution, physical Deck and frame-time gates
remain open. Current packages99b86bb02 predate this menu correction. Heartbeat
continues; storage checks remain inspection-only.


Post-push restoration, 20:34 UTC: Unity auto-import rewrote the same32 proven
trailing-whitespace-only metadata files after the initial restoration. The idle
editor was closed normally with Command-Q, then those exact bytes were restored
again. Editor is now closed for handoff; reopen the existing project for the
next package gate. All776 normal-save files,374 protected sources and2 foreign
PNGs remain unchanged. See `Verification/Live16x24/action-cost-settled-restoration.json`.
No source, cache, save or build was deleted. Runtime fix cc963241d is verified
on origin/main; this follow-up records the settled environment only.


## Continuation — 2026-10-26 action-menu packages accepted

[Delivery report](Verification/Live16x24/packaged-action-menu-report.md) freezes
daa388643 (runtime correction cc963241d) into fresh dated Mac/Linux-current
outputs. Both report zero build errors; warnings and shutdown notices are
retained. Independent archive audit verifies181 files,180 checksums and source
licenses. Archive SHA256219a1876f19c5c58879d9cd16419f42f85f59a4492377fdd8747829a6210b860.

Fourteen actual Mac screenshots close the previously open packaged gate: full
2-dram recipe/F1, parked-pointer keyboard Examine, real widened-edge click,
40→38drams/tick4860→4870, covered Working art, clean48-column menu, mouse
110-tick reader, fullquit/Continue and clean save. No runtime or art changes.
Initial mistaken uncompressed fixture was archived and corrected before accepted
play; all evidence and limits are explicit. Existing En Garde label clipping
remains a named separate UI defect. No Linux execution/physicalDeck claim.

Both apps are closed. Exact48,565-object snapshot comparison, normal saves,
complete logical preferences, two foreign PNGs and previous packages pass.
32whitespace metadata/3build serialization files restored; one additional
UnityConnect enable-toggle separately reviewed/backed up/restored. No cleanup.
The Packages with Errors startup modal was dismissed without dependency edits;
reopen local MCP server before Unity and inspect fresh CUA state if it returns.

Next: continue unchanged earned-east-collected242967504 in the NEW package,
kitchen55,17,HP42level2tick5670,40drams,meal carried/empty board. Complete
North/Nella revisits and remaining forge/crop gates, then next named visual
repair; preserve accepted art. Complete opening/regions/device/performance
remain open. Heartbeat remains active; storage inspections are read-only.


## Continuation — 2026-10-26 collected plot cycle accepted

Read [collected revisit](GRAPHICS-G3-COLLECTED-REVISIT.md). Actual package
daa388643 travelled kitchen → East → glade → North → Nella and inspected the
cold forge. Pitchpod dry seed, moist sprout, ripe crop, cleared bed, two resin
plus seed, and replanting are now verified in ordinary play. Full quit/Continue
and a North border return preserve the replacement crop as a dry sprout with
paused growth.33 original captures include intermediate and failed input;
31/31 independent saved-state checks and nine original-image reviews pass.
Player42HP/40drams/SP1 and uneaten meal are unchanged. No code/art change or
native suite rerun was needed.

Navigation incident: held north input broke kitchen roof55,14; adjacent roof
is intact. Fast filter entry and a post-Rain command sequence dropped input;
separately observed retries worked. No accidental skill purchase. Capture11 is
mid-dialogue reveal, not complete prose acceptance. Shutdown notices are retained.

Normal saves, full logical preferences, current builds and48,597 tracked objects
compared exactly at restoration; zero tracked files needed restoration. Test
saves are archived intact,417GiB is free, and no cleanup occurred. After this
receipt, Unity was reopened externally from Hub at21:26:01 UTC. Its new play
session, changed Editor preferences and32 whitespace-only import deltas are
preserved, unowned and unstaged. Root did not stop or restore over that session.
Coordinate ownership before native hotbar testing; standalone remains closed.

Next unchanged fixture: `Verification/Live16x24/Fixtures/collected-revisit-returned242967504`.
Nella62,6, tick9470, HP42, level2,40drams, SP1, two resin, uneaten meal and
an equipped long sword. Next named visual repair: RED→GREEN seven-column name
wrapping for En Garde, with actual Warden rendering and counterchecks before
package delivery. North still remains guarded by the living patchbearer at62,6,
20HP; brewing and tempering are unaccepted. Whole opening, regions, device and
performance gates remain open. The heartbeat stays active.


## Continuation — 2026-10-26 hotbar delivery and earned Burning temper

Frozen source3aca7c18e now has fresh Mac/Linux packages; both succeeded with
0errors/4warnings. Ten original Mac captures verify1x/2x names, real mouse
En Garde/cooldown refusal, keyboard Lunge AIM/cancel, inventory and quit/Continue.
The Linux archive was independently checked; physical/Linux runtime gates stay
open. Prior source93/93 retained without rerunning unchanged tests.

The same ordinary standalone session carried the actually harvested resin from
Nella through the North guard encounter and still, via glade to East forge.
32 captures and26 independent saved-state checks accept the bounded route:
same sword2710 equipped, one Burning30 potency1 temper, maxHP10→8, unchanged1d8,
resin2→1/coating consumed, guard corpse provenance and genuine restart at
tick11960,East45,6,HP25/42. No natural Burning or Riposte proc observed.

The coating reader's muted bottle initially looked generic; source/visual audit
proved it is the previously approved shared native flask. No asset or resolver
change is justified. Keep accepted art stable. Next: fresh New Game whole-scene
six-site review, compare the actual framing/motion with accepted references,
then address the largest documented remaining visual gap. Full opening/regions
and device performance remain unfinished.

At22:29, player/Editor were closed. All48,657 baseline tracked objects compared;34foreign
edits, normal save tree/full logical preferences and prior packages preserved.
Three reviewed build settings plus the separately reviewed UnityConnect toggle
restored. Test saves archived; no cache/storage deletion. Available415.3GiB.
See [delivery](GRAPHICS-G3-HOTBAR-DELIVERY.md) and
[earned route](GRAPHICS-G3-EARNED-RESIN-TEMPER.md) for evidence and bounds.

A later Unity session changed Editor LastGameID/session keys after that verified
restoration. It was not started by this root and is preserved untouched. The
standalone player remains closed. Check active ownership before continuing
ordinary Play; do not restore the old preference map over a later session.


## Continuation — 2026-10-26 connected six-scene consistency pass

Fresh seed64 ordinary input reviewed glade, cellar, North, Nella, East and
kitchen at1x/2x across two isolated sessions.40 actual captures and runtime
receipts are indexed in [six-scene polish](GRAPHICS-SIX-SCENE-POLISH.md).
CopperPipe had three conflicting appearances: North glyph, East repeated
elbow, Nella joined native line. Exact North/East owners now reuse accepted
Nella cardinal frames in world and Examine; peat scope stays unchanged.
Native RED:10fail/37pass; GREEN:105/105 across the corner/East suites. Actual
walking covers/uncovering and Examine verified both affected sites.

Normal saves, complete logical preferences,34foreign tracked edits and previous
packages preserved. Before-fix disposable checkpoint was lost to automatic
Play-stop cleanup; retained captures are a fresh-seed comparison, not identical
tick acceptance. Post-fix checkpoints were archived before stopping. No storage
cleanup. Package delivery/restart acceptance follows this source checkpoint.

Next named visual defect: Kitchen BrinePool11528 at36,13 and11527 at36,14 still
use blue glyphs. Actual volumes90 each, effective coatingbrine, no current
heat/cold/charge, freeze−8°C. Author distinct native salt-water art through the
capture/concept/source/integration loop; do not label it freshwater or assume
empty stock removes an independently renewed source coating. Preserve approved
layout. Actor animation, incidental glyphs and full opening/regions/device/
performance coverage remain open. Current report:2026-10-26 23:27UTC.


### Pipe package delivery completed — 2026-10-26 23:35 UTC

Source6b1032459 is on main. New Mac package passed ordinary Continue, pipe
Examine, player cover/uncover, full quit/restart and North→glade→North travel;
eight original screenshots and earned return checkpoint retained. Linux retry
built with0errors/0warnings and its180-file archive passed independent integrity
review; first attempt with2import-worker errors is preserved separately and
rejected for delivery. Native105/105 tests remain the code gate. No Linux
execution, physical Deck or performance acceptance claimed.

Guarded restoration compared48,827 tracked objects;34foreign edits, normal
saves/full logical preferences and previous packages preserved. Test saves
archived, Editor idle/player closed; no storage cleanup. Current package paths,
SHA and actual evidence are in [six-scene polish](GRAPHICS-SIX-SCENE-POLISH.md).
Next: distinct kitchen brine identity from captured real owners and state,
then actor-motion/incidental-glyph consistency; whole opening remains open.


## Kitchen brine consistency — 2026-10-27 00:10 UTC

The exact Kitchen12.11 brine pools now have an original editable native16×24
identity, shared with their reader portraits. Actual keyboard Continue/Look/
Examine/cover/uncover/F5 captures establish this bounded Editor result;192/192
native tests cover the new state gates and neighboring water/pipes/fixtures.
Source RED preceded implementation. Freeze/melt/renewal uses existing semantics;
only a represented neutral brine layer loses its duplicate wave. See
[Kitchen brine](GRAPHICS-KITCHEN-BRINE.md) for pipeline, corrections, actual
screenshots, profile spike/limits and preservation receipts.

Normal saves/gameplay settings,34foreign edits and current packages preserved;
no storage cleanup. Current delivery still source6b1032459 pending new package
verification. Next: push current source, fresh Mac/Linux builds, normal kitchen
Continue/restart capture, then six-site actor-motion/incidental identity review.
Whole opening and remaining regions remain unfinished.


## Kitchen package milestone and next scene defect — 2026-10-27 00:25 UTC

Completed: native kitchen brine identity and shared reader,192/192 native tests,
actual Editor and seven fresh Mac captures including save/full restart. Source
447e82c4f verified on origin/main before builds; exact delivery receipts and
screenshots are the follow-up push. Fresh Mac/Linux builds0errors4warnings each;
all188 archived Linux files independently checked. Current package paths, hashes,
preservation and footprint correction are in [Kitchen brine](GRAPHICS-KITCHEN-BRINE.md).
Normal saves/preferences,34 foreign edits and prior packages preserved; no cleanup.

In progress: six-scene consistency/whole-opening acceptance. Next hourly goals:
return through ordinary input to East Cudgel9357 at29,6; capture the exact pile
and reader; use that baseline for imagegen/native wooden-club art, then verify
world/carried/reader, pickup/drop and save-return. Preserve accepted art. Do not
call static motion captures an animation defect. Physical Deck/Linux execution
and sustained performance remain unverified. Use explicit compress_lz4 for the
next fresh delivery. Last user-report checkpoint:2026-10-27 00:25UTC.


## East cudgel integrated — 2026-10-27 00:54 UTC

Cudgel9357 from the real East loot pile now has original native16x24 wood art in
world, inventory and Examine. Actual baseline travel, exact pile selection,
pickup/drop, unchanged-checkpoint comparison and auto-equip/sword-restore pass.
Source RED42failed/6passed → GREEN48/48 plus129neighbor regressions. Editable
Blender/cells, built-in imagegen prompt/concept and imported source checks retained.
Independent source/runtime/visual review found no significant defect. Captures
cudgel01–10 are actual Unity Editor gameplay; normal saves/preferences,34foreign
tracked edits and previous packages preserved. No storage cleanup.

See [Cudgel record](GRAPHICS-EAST-CUDGEL.md). Next freeze/push this source, fresh
compressed Mac/Linux packages and actual ordinary Continue/restart from earned
Fixtures/cudgel-after64 (3500,East26,6,28HP,club27,6). Existing current447e82c4f
packages predate this item. Whole six-scene opening/regional/physical Deck gates
remain open. Next named scene candidate is the visible East steam-vent glyph;
audit exact owner/state before authoring. Last hourly report remains00:25UTC.


## Packaged cudgel delivery — 2026-10-27 01:04 UTC

Native cudgel art is verified in the actual Mac build from source `0545aa9a2`:
world at 1x/2x, Examine, pickup, inventory, save and full quit/restart/Continue.
The matching compressed Linux archive passed independent checksum, size, mode
and contents checks. Native tests: 48 targeted plus 129 regression cases passed.
See [full delivery record](GRAPHICS-EAST-CUDGEL.md) for builds, seven original
screenshots, earned turn-3510 fixture and restoration evidence. Linux execution,
physical Deck, sustained performance and whole-opening acceptance remain open.

Next: [East steam tile cue](GRAPHICS-EAST-STEAM-PLAN.md). Exact saved-state review
confirms the pale marks are the generic tile-cloud apostrophe, not missing vent
body art. Preserve the approved grates and existing gameplay. Normal saves,
preferences, 34 unrelated tracked changes and previous builds are preserved;
no storage cleanup was performed. Last hourly report remains 00:25 UTC.

### East steam continuation — 2026-10-27 01:42 UTC

Scoped native steam tile-cloud cue now passes211native tests and actual Editor
before/after, exact-reader, movement/wait and F5 checks. Five real clouds;
approved grate/tonic preserved. Source and editable art in
`ArtSource/Live16x24/east-steam`; full acceptance/limits in
`Docs/GRAPHICS-EAST-STEAM-PLAN.md`. Refresh packages after source push.
Next scene-level pass is North Patchbearer encounter motion/readability, not
whole-opening acceptance. Last hourly report01:25UTC. Storage inspection-only.

### Packaged steam delivery — 2026-10-27 01:52 UTC

Runtime source`be1c52ca375bab35c202eb976a04449e8ef72991` is verified on
origin/main. Fresh compressed Mac and Linux builds each report0errors4warnings.
Linux archive181files,128688319bytes, SHA
`d27fbe7aef2e5e5dec524902966c4741c5909caffd897585ff2a972da48d3675`;
all payload hashes/sizes/modes and credits independently checked.

Six accepted actual Mac captures show ordinaryContinue3540,default/2×world,
a neighboring grass counter,exact vent29,7reader,wait/save3550 and full
quit/relaunch/Continue. An off-screen window capture was rejected and redone
after native Zoom; no screenshot was repainted or cropped. Known ComputeBuffer
shutdown notice persists. Linuxexecution/Deck and full scene/motion/performance
acceptance are unverified. Evidence:`packaged-east-steam-mac-play-summary.json`,
`packaged-east-steam-independent-review.json`,`packaged-east-steam-restoration.json`
under`Docs/Verification/Live16x24`. Normal saves/gameplay preferences,34foreign
tracked files and prior build/archive match protection; test saves archived,
reviewed Unity build serialization restored. No storage cleanup.

Next: ordinary pre-fight North capture and Patchbearer encounter motion/readability
according to`Docs/GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md`. Current steam is stable.
Last hourly report01:25UTC; this delivery is a visible milestone, not a duplicate
hourly report. Keep the continuation active.


### North encounter motion — 2026-10-27 02:50 UTC

[North motion record](GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md) now verifies
ordinary full/empty walk, attack, hurt and recovery, real finite tonic use,
exact-reader consistency, death, glade return and North revisit/F5. Eight native
editable derivatives borrow approved idles.278native cases pass,1allocation
probe skips after its counter-control fails. Independent code/art/capture review
found no material issue. Five sampled sequences60frames and seven original Game
stills retained; no continuous-video or whole-scene claim. Profile is a bounded
Editor stationary/reader sample, not hardware performance acceptance.

Next freeze/push source and package normal MacContinue/restart. Then capture
East Cindercaller body motion/cast attribution beside animated Scrabbler; existing
Ember Spit FX/audio is functional. All six scenes have bounded evidence; whole
opening, remaining actors/services/regional kits, Linux execution and physical
Deck remain open. Normal saves/gameplay preferences and34foreign tracked edits
preserved. Storage inspection only. Last hourly report02:25UTC,next03:25UTC.
