# Felling layered scene in Unity

Status: implemented and verified in connected Unity, 2026-09-06. The extracted Felling scene is reachable through normal play, with native interactions, additional wildlife and harvestable/collectible sprites. The final native playthrough passed 120/120 checks. This supersedes the earlier offline-only boundary for this scene. CoO-original environment integration; no new Qud parity or lore resolution is claimed.

## Goal and milestones

1. Import the exact 55-layer component package, including 39 mutable sprites and their owned contact contributions, with the existing complete local backing. Preserve the actor-free baseline and 32 PPU transform.
2. Upgrade the existing Felling world location at (3,5), Overworld.3.5.0, to match the source landmarks and physical geometry. Author paths so every removable prop has a reachable adjacent interaction cell. Keep ordinary map/edge travel and return paths.
3. Bind every readable component to an examinable simulation owner. Loose props/vegetation receive an explicit removal action through the normal world menu, guarded by actual zone membership, reach and stable identity. Fixed geology and sacred landmarks remain fixed. Persistence records state independently of Unity objects; reload/re-entry must not resurrect removals.
4. Render the real layered sprites/backing in Unity, with cell fog, foot depth, actor interleaving, normal gameplay overlays and a camera that includes the upper trunk. Suppress duplicate stock environment art only while the authored presenter is valid. Retain glyph fallback.
5. Run Unity EditMode tests with red/green evidence, a dedicated adversarial sweep, a deterministic live scenario and actual input/visual review. Verify map access, interaction mutation, collision, re-entry/save behavior and camera lifecycle. Record raw results and material limits.

## Pre-implementation verification sweep

| Assumption checked | Actual evidence | Correction / decision |
| --- | --- | --- |
| Site needs a new travel system | WorldGenerator, OverworldZoneManager and WorldMapTraversal already place and enter the named POI at (3,5). | Reuse existing routing; test access and upgrade content. |
| Current circle matches art | FellingSiteBuilder's six/seventh coordinates differ from ArtSource/FellingSite/layout.json. | Match the source six cells (36,10),(43,10),(33,13),(46,13),(36,16),(43,16), seventh (40,8). |
| Offline walkability gives full interaction | Prepared occupancy contains 306 connected cells; 23/39 prop anchors have no adjacent reachable cell. | Author side paths and test all 39 actual approaches. |
| Exported footprints define collision | Manifest assigns one provisional anchor even to the giant stump. | Separate physical obstruction, visual bounds and interaction anchors. |
| Sprite atlas is the whole scene | Atlas contains 39 props; the manifest contains 55 layers, and base retainsStatic is false. | Import every contribution and remove contacts with their owner. |
| Existing sprite import is safe | SpriteImportPostprocessor forces 16 PPU under Resources/Sprites. | Dedicated Resources/SceneArt/FellingSite policy at 32 PPU. |
| Saved cached zones regenerate | ZoneManager returns cache; missing-POI repair preserves old ground. | Idempotent upgrade with durable revision; preserve player/items/unknown occupants and missing-seventh state. |
| Browser tests prove Unity | Offline core and raster checks never compiled or rendered the Unity adapter. | Actual connected Unity editor, import checks, tests and live render/input evidence. |

## Contract and implementation decisions

Source canvas 1536×1024. Image-to-world is (16+px/32,32-py/32); ground samples y224..1023 map into zone x16..63,y0..24. Upper224 pixels are visual overhang. The simulation stays80×25. Components retain original placement and stable IDs; no arbitrary sprite motion exposes unprepared sides.

Gameplay removals represent clearing small plants/loose stones. They consume a normal turn, change actual presence/collision and expose the prepared backing. They do not grant a debug restore spell, invent loot rewards or remove the monumental trunk. Fixed scenery and all seven lore positions remain examinable. Existing seventh-position mechanics are preserved.

Runtime assets are loaded/validated once, views cached, and state/FOV refreshed only on existing invalidation paths. Fog data is cell sized; no full reference-image repaint every frame. Profile the real editor scene following Docs/PERF-FOUNDATION.md. Tests follow CLAUDE.md and ADVERSARIAL_TESTING.md, including negative controls, save/reach/atomicity cases and final cold-eye review.

## Content readiness

- Ready: exact component extraction, actor-free baseline, local backing, source layout, existing POI and world travel, existing world menu and persistence APIs.
- Implemented: physical routes, persistent owner bindings, native Unity art/materials, source-aware picking, camera/lifecycle, legacy hydration and sparse wildlife/dressing.
- Fixed scope: monumental trunk/root partitions cannot be removed because full hidden geometry is unprepared; all remain inspectable.

## Added wildlife and dressing (user steering, 2026-09-06)

The user then requested other newly created sprites, including creatures, integrated naturally and with real interactions. Reuse the existing animated glasspane frog and yellowfoot wayfarer sheets. Add two passive frogs on the stream-side ground and one wayfarer on the eastern moss verge. Keep their native Brain, health, movement, damage, death and examination behavior. A cascade-father was considered but rejected for these dry cells because its existing habitat contract requires a real spray pool.

Add three small extracted sprites from the Catacomb source sheet: a pink mushroom, a cyan mushroom cluster and loose rubble. Their bytes are copied unchanged to `Art/extras`; provenance records native dimensions, feet and SHA256. Bind the fungi to native MushroomRing harvesting, and the rubble to a native takeable Tepuibone item. Keep the original 55-layer manifest and its exact reconstruction test unchanged. Supplemental rendering follows the current simulation owner and must disappear after harvesting/pickup, move after dropping, and survive save/load correctly.

Population and dressing use separate persisted revisions. Migration must not refill harvested items or resurrect killed wildlife. Newly added animals in an active legacy save must join the existing turn scheduler; inactive cached zones must not acquire active turns. Sparse placements avoid all seven landmarks, the south approach and the original props' interaction cells.

The first live capture revealed three issues that isolated source tests did not show: native river coating glyphs over the painted water; vertical black strips from cliff visibility projection; and a camera that could lose the player on exterior approach routes. Reproduced them in 12 Unity cases (10 failures and two correct spill/oil controls) before correction. The facade uses only its own column's boundary and three cells of ground in front as an observation band; ordinary ground and mutable objects retain their direct cell visibility. The camera pans when a player or Look target leaves the artwork's frame. Suppress only the redundant permanent river-water coating, retaining new spills and hazards.

## Validation and review record

- Initial core/presentation integration: Unity job `30ee6620c6b34549a2a501c636493c98`, **219/219 passed**, zero skips. Includes exact imported renderer ordering (0 changed source pixels), all prop approaches, state/save, immutable-owner negative cases, turn costs, glyph fallback and camera lifecycle. Raw `Docs/Verification/FellingScene/felling-unity-green.json`.
- Broader existing save/world-map/presentation regression job `089d7298b6c14f109591b157fe612f7a` ran 279 cases and failed with at least 25 missing-container-blueprint errors (failure listing capped). Actual Editor.log stacks enter unchanged `ContainerPlacementService`/`ContainerBuilder` from minimal world-map fixtures at (10,10), not the Felling path. No clean-HEAD Unity rerun was performed; this is a scoped diagnosis, not a broad green claim. Raw `broader-regression-initial.json`.
- First native run `8d7b786e0a444055aa0f893d9e38707d`: normal map travel, all 39 menu removals, all 390 expected ticks, collision opening, re-entry and two F5/F6 persistence checks passed. It reached four landmarks before one movement key was not accepted at (37,13); the run **did not pass overall**. Preserved as `first-native-audit.json` and `first-native-*.png`. The driver now observes the actual input repeat gate and records detailed state on a failed key; it never retries an unexplained failure.
- First live profiler sample: 1,414 samples across 76.7 seconds. ZoneRenderer average 1.14 ms; main-thread average 3.25 ms. Save/load/capture spikes and diagnostic traversal allocations are included (main-thread max355 ms, GC max89.7 MB); these are not steady-state build benchmarks. Final populated-scene measurements appear below.
- Populated scene: job `a1b9955de46e49f0876b3dc1610394de`, **254/254 cases succeeded**, no reported failures. Separate focused save/render/camera/input/isolation job `eca26b5414784f1ab4826d102eb5de30`, **151/151 cases succeeded**. Raw `populated-felling-unity-green.json` and `focused-regression-green.json`.
- Final presentation adjustment: job `cbd97c238b3f4c7ea8900b737fe96bf4`, **29/29 passed**, zero skips, covering 3:2 framing, restoration of exterior/ordinary views, all actual imported art tests and visible-actor color. This overlaps earlier groups; counts must not be added as independent tests. The scoped tint correction removes duplicate CPU LightMap darkening only for visible actors inside the ready Felling art.
- Second native run is preserved in `second-native-audit.json`. It passed the 39 clears and persistence, then the audit's `IsPassable` path query attempted to walk into a creature with `PhysicsPart.Solid`. Production correctly blocked the move and processed native combat. The audit now uses `BlocksMovement`, including a pre-key blocker guard. No production movement or combat was changed to make the test pass.
- Third native run `7343c2b07c5047389171ce1dcb3697c8` completed 648 native movement steps and 120 checks, with one failure. All clears, dressing actions, persistence checks and seven landmarks passed. Examining the second frog described its shared tile instead of the selected animal: `WorldActionMenuUI` kept a pile flag after entering an individual entity's menu. This is a production interaction defect, not duplicate-message suppression. The failed run is retained as `third-native-audit.json`. Two focused tests reproduced the defect before correction; explicit summary-menu context then passed **47/47** interaction/return-state/shortcut cases (`pile-context-red.json`, `pile-context-green.json`). The native audit additionally requires the exact selected entity and a newly emitted authored description.
- Final GPU review caught stale exterior pixels in the letterbox margins after menu/travel changes, despite the camera's source rect being correct. The original cropped camera left those excluded pixels uncleared. An owned empty camera now clears the original map rectangle before the narrowed scene view. Four focused tests failed first, including two real GPU cases at wide and square sizes. The corrected final camera/presentation job `bd183555595b44c9bfa42be1859ce9c6` passed **53/53**, zero skips, including zero stale margin pixels, preserved HUD pixels, reuse and lifecycle checks. Evidence: `letterbox-clear-focused-red.json`, `letterbox-clear-focused-green.json` and the prior live-capture pixel comparison `letterbox-gpu-red.json`.
- Final native run **`3786680f8cee4b168ffa8e43f1454673`: 120/120 passed, zero failures, no fatal error**, 122.27 seconds. It used 574 native movement steps, 39 actual clear actions costing 390 ticks, three exact-owner creature examinations with visible animation, two harvests, one pickup, re-entry and F5/F6 persistence, and all seven lore positions including native seventh exposure. Raw `native-audit.json`; no gameplay mutation or fog reveal was performed through MCP observation code.
- Final GPU screenshot comparison `letterbox-gpu-green.json` found zero nonblack pixels in either measured margin on arrival, after all original clear actions, and after the creature/harvest/pickup menus. The latest `native-initial.png`, `native-all-removed.png`, `native-fauna-and-harvest.png` and `native-seventh.png` are actual Unity Game views. Local occluded ground remains hidden; there is no blanket reveal.
- Final editor profiler: 2,318 retained samples over 117.79 seconds. ZoneRenderer average **1.10 ms** (p99 13.38 ms); main-thread average **2.69 ms** (p99 19.22 ms, max 77.81 ms); per-frame recorded GC average **33,737 bytes** (max 426,306 bytes). The record includes traversal, menus, save/load, screenshots and audit observations, not just steady rendering. No FPS or global profiler setting was changed.
- After stopping the completed audit, Unity reported `SaveRootOverride == null` and inactive audit isolation; the audit's own temporary directory had been removed. A separate intact preview can be opened without overwriting the full audit result. The last console query returned only two graphics notices about memoryless depth load/store actions; the final run had no gameplay exception or failed native check.
- Final preview run `c1c92487cfdc43e4bb5453477cbe9d2b` passed **10/10** setup/access checks and remains open in Unity Play mode at `Overworld.3.5.0`. Read-only verification found all 55 source owners, all three supplemental objects and all three animals present; the audit keyboard was cleaned up. `native-preview.png` is the intact review capture. Stopping this preview restores normal save settings and removes its disposable saves.

## Player access and controls

The production location is world **(3,5)**, `Overworld.3.5.0`. From the standard starting zone, ascend with **<** (Shift+,), move seven world cells west and five north, then descend with **>** (Shift+.). Ordinary neighboring-zone edge travel also remains available.

Use the normal **C**, direction, object picker and action menu to examine or clear source props, harvest the new fungi, or take the tepuibone. The original 39 clearable objects expose their prepared backing and consume one normal action each. Creatures use native examination, AI, health and combat; they are not clearable scenery. Native F5/F6 save/load preserves removals, harvests, pickups and wildlife identities.

For an isolated editor tour, use **Caves Of Ooo → Scenarios → World → Felling Scene Play Preview**. It starts the ordinary game, travels through the world map and leaves the intact scene playing. Its save destination is temporary; stopping Play restores the user's prior save settings and removes only that tour's disposable save directory. The adjacent **Felling Scene Native Play Audit** performs the full automated control audit. Neither menu changes the saved scene or requires a special runtime teleport.

## Implementation map

| Area | Main files |
| --- | --- |
| Reproducible source export and physical layout | `ArtTools/felling_unity_export.py`, `ArtSource/FellingSite/Integration/physical-layout.json`, `Assets/Resources/SceneArt/FellingSite/definition.json` |
| Simulation owners, clearing and persisted revisions | `Assets/Scripts/Gameplay/World/FellingSceneDefinition.cs`, `FellingSceneRuntime.cs`, `FellingSceneStatePart.cs`, `FellingScenePropPart.cs` |
| Native creatures and harvestable/collectible dressing | `Assets/Scripts/Gameplay/World/FellingScenePopulation.cs`, `Assets/Resources/SceneArt/FellingSite/extras-provenance.json` |
| World access and existing-save upgrade | `FellingSiteBuilder.cs`, `OverworldZoneManager.cs`, `SaveSystem.cs` |
| Source and supplemental rendering | `Assets/Scripts/Presentation/Rendering/FellingScenePresenter.cs`, `FellingDressingPresenter.cs`, `ZoneRenderer.cs`, `EnvironmentSpriteRenderer.cs` |
| Import, shader, framing and interaction | `Assets/Editor/FellingSceneArtImporter.cs`, `Assets/Resources/SceneArt/FellingSite/FellingScene.shader`, `CameraFollow.cs`, `InputHandler.cs`, `WorldActionMenuUI.cs` |
| Repeatable real-editor verification and preview | `Assets/Scripts/Scenarios/Custom/FellingScenePlayAudit.cs`, `Assets/Editor/Scenarios/FellingScenePlayAuditMenu.cs` |

The working tree also contains earlier art, spell and rendering work. This integration was performed in place without reverting those changes; this table identifies the relevant integration surfaces, not an exhaustive claim of ownership over every changed line.

## Scope and remaining artistic limits

The original source uses 95 renderers (base, 55 layers and 39 contacts); supplemental dressing adds three separate renderers. Native animated creatures use the established actor renderer. Contact and sprite ownership remain coupled during source clearing. The 55 source layers still reconstruct the actor-free baseline with zero pixel differences before game fog/lighting and the added population are applied.

Fixed geological partitions stay fixed because their unseen complete geometry is not authored. Exterior drops of supplemental tepuibone use the ordinary item appearance beyond the painted art bounds. Water uses the selected source surface; no new animated water frames were created in this integration. Fog, existing gameplay overlays and the project's actor artwork mean the live scene is not a flat screenshot of the reference.
