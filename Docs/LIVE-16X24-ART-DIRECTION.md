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
2. **First real frame.** Connect native ground, player and key ruin/pond silhouettes to the real opening. Suppress conflicting old presentations only where the new mode owns the view. Confirm palette, pixel size, camera framing, fog and selection in actual Play. This is a functional foundation, not final art approval.
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

Read this brief, CLAUDE.md, the overnight progress record and current git status. Choose the next unfinished slice above. Recheck cited source semantics, write failing tests for changed behavior, implement through existing owners and rendering paths, then test the counter-cases. Use imagegen and selected external references to improve actual assets; inspect native frames and animation in the real scene. Review, fix significant issues, update the docs and push only task-owned files to main after fetch/rebase. Continue from evidence, not from asset or test counts. Keep all user-approved art and the previous presentation recoverable. Stop calling an area complete until its coverage and gameplay gates pass.

## Review record

- 🔵 Current clarification adds a production brief and changes the scheduled continuation criteria; it does not itself implement the new mode.
- 🟡 Resolved plan issue: crop-only integration was too narrow for the requested visible transformation. The real opening's complete visual coverage now leads.
- 🟡 Resolved plan issue: an arbitrary morning cutoff could reward unfinished art. The user explicitly allows the time needed; continuation should finish the defined live opening release, then report remaining regional work.
- 🧪 Runtime and visual acceptance remain pending. No additional Unity test result is claimed by this document.

