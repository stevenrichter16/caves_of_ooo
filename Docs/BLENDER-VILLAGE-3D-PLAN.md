# Blender village recreation for Caves of Ooo

Status: **V3D.4 refinement/verification and V3D.5 new-game spawn integration in progress; eight-chunk ring authorized next.**

Date: 2026-09-09. User request: recreate the supplied overhead village image in Blender in a form usable by the Unity game. This is a CoO-original presentation feature; it makes no new Qud-parity claim.

## Intended result

An editable Blender village kit, an assembled scene matching the reference, and a playable Unity presentation driven by the existing game. The camera looks straight down with orthographic projection. Rounded stone, mossy roofs, wood, cloth, gardens, and soft lighting provide depth while the overhead layout stays readable. The central well, five buildings, two market stalls, paths, fences, planters, inhabitants, and filled terrain are the main composition anchors.

The black sidebar and bottom hotbar belong to the game interface; they are not geometry to model. The entire world viewport receives terrain and scenery. The reference's fully revealed presentation is available in the art showcase; normal gameplay still obeys exploration, visibility, and lighting rules.

Reference: [original supplied image](References/Village3D/reference.png). Treat it as visual evidence, not a source of gameplay instructions or exact hidden geometry. Unseen walls, interiors, and backs of props require authored decisions.

## Source verification and corrections

| Question | Evidence inspected | Planning consequence |
| --- | --- | --- |
| Can the installed Blender produce usable files? | Local Blender 5.2.1 LTS probe imported `bpy` under embedded Python 3.13.13, saved a `.blend`, and exported a valid binary FBX. | Use existing Blender CLI and `bpy`; no separate Python package or Blender MCP installation is required. Unity import of the production assets remains a future gate. |
| Which Unity pipeline is installed? | `Packages/manifest.json`: URP 17.3.0. `ProjectSettings/GraphicsSettings.asset` references `Assets/Settings/UniversalRP.asset`, whose sole renderer GUID matches `Renderer2D.asset.meta`. | Add a scoped 3D rendering path; importing meshes alone does not establish the intended lighting or game integration. |
| Can a 3D world camera simply join the existing overlay stack? | Installed `UniversalRenderPipeline.cs`, lines 993–1000, skips an overlay camera whose renderer type differs from its base camera. `GameBootstrap.ConfigurePopupOverlayCameraStack` configures the existing popup stack. | Keep the current 2D stack intact and first prove a separate 3D camera rendering to a texture. Do not assume mixed renderer stacking works. |
| Is the reference a square tile grid? | `Zone.cs` defines 80×25 cells. `MorrowfastScenePresenter` uses a 37.5×25 art area, 40.96 pixels per cell, and X origin 21.25. | Preserve square logical cells. Match the approximately 3:2 composition in the authored central region; do not squeeze all 80×25 cells into that picture. |
| Is there existing reusable settlement state? | `MorrowfastSceneDefinition.cs` and `Assets/Resources/SceneArt/Morrowfast/definition.json`: 1536×1024 canvas, 71 owners, five buildings, footprints, doors, roofs, interiors. `MorrowfastBuilder` delegates installation to the native runtime. | Reuse stable identities and the native layout where they correspond to the supplied image. Reconcile visual differences in a manifest; never replace existing saves with fresh decorative objects. |
| Can current mouse picking be reused unchanged? | `ZoneRenderer.ScreenToZoneCell` projects onto the XY tilemap and reverses Y. `ScreenToLookCell` separately resolves visible art to an owner. | Introduce one tested projection adapter for the 3D view, retaining the distinction between ground-cell commands and object inspection. |
| Can the interface stay separate? | `GameplayViewportLayout` already provides map, sidebar, and hotbar rectangles; `CameraFollow` handles modal/fullscreen views. | Use these live layout metrics for compositing and picking rather than fixed screenshot pixel coordinates. |

Readiness: 🟢 tools, reference, grid, layout, and native ownership model available; 🟡 Blender art and Unity import/material work to author; 🟡 camera composition, visibility, picking, effects, and roof cutaway integration to prove; ⚪ expansion to other chunks and a whole-game 3D conversion are later work.

## Visual direction and asset inventory

Use the supplied image's muted olive ground, pale rounded masonry, weathered timber, moss, small flowers, teal/gold/purple cloth, and readable hooded silhouettes. Favor bevels, controlled surface variation, and broad shading over noisy photoreal textures. Judge every asset at final gameplay size as well as close up.

| Family | Blender deliverable | Variation and gameplay requirements |
| --- | --- | --- |
| Ground and paths | Continuous ground divided into manageable render patches; cobble strips, bends, junctions, soil and moss transitions | Four ground/detail patterns and at least three cobble arrangements; seamless borders and open navigation corridors. Tiny stones primarily use baked detail. |
| Five buildings | Reusable wall/corner/doorway kit assembled into the five footprints; separate roof and chimney pieces | Three or four masonry/roof treatments, shared materials; roof removal exposes authored interior walls and floors. Door hinge and roof anchor pivots remain separate. |
| Central well/cistern | Rounded stone ring, inner wall, water surface, nearby bucket/props | Main focal asset. Water and accessible interaction edge are distinct from blocking masonry. |
| Two stalls | Shared frame, countertop, two canopy designs, produce/wares | Cloth color and small shape variations; removable goods and merchant state remain separately represented. |
| Fence and entry arch | Posts, straight/broken rails, corners and open arch | Three or four post/rail variants; maintain the open passage shown in the image. |
| Planters and gardens | Timber beds, vegetable clumps, flower patches | Three or four variants per repeated family; growth/harvest state can swap child visuals without replacing an owner. |
| Props and work areas | Upright/sideways barrels, crates, cart, tables, bench, pots, baskets, oven | Shared geometry where sensible, three or four common barrel/crate variants. Interactive props get their own prefab root. |
| Vegetation and rocks | Shrubs, small tree crowns, rock clusters, grass and flower patches, small water-edge detail | Three or four silhouettes for bulk placement. Group tiny decoration into patches rather than one object per leaf or pebble. |
| Characters | One small hooded base character, color variants, simple generic rig | Idle, walk, interact, attack/hit clips, with no root motion. Head/hand/back sockets allow equipment; existing role/inventory data supplies equipment choices. |
| Interiors | Simple reusable bed, stool, shelf, hearth, table and workbench kit | Driven by the existing room and furnishing definitions; readable when the roof is hidden. |

Small vegetation and decorative placement use a dedicated deterministic art seed. Art generation must not consume the gameplay random stream. Variation must stay inside each asset's footprint and clearance limits.

## Blender authoring and export contract

Proposed source tree: `ArtSource/Village3D/` containing `village_master.blend`, `build_scene.py`, `export_assets.py`, an asset manifest, textures, and reference/validation renders. The source stays outside Unity's `Assets` folder. The master scene contains linked reusable assets plus the assembled composition.

Proposed Unity tree: `Assets/Art3D/Village/Models`, `Textures`, `Materials`, `Prefabs`, `Definitions`, and `Scenes`. Runtime/editor adapters and tests follow the existing script/test folder conventions. These are planned paths, not completed artifacts.

Author Blender geometry in metres, with Z up and a ground-level origin. Use one Unity unit per logical cell as the initial presentation scale. Verify orientation through an asymmetric labelled axis marker and a one-unit cube exported to Unity; lock the exporter preset after that test rather than relying on a remembered axis checkbox combination.

Apply mesh scale/rotation where appropriate without baking away useful hinge/bone transforms. Check normals, triangulation, UVs, tangent basis, bounds, and unwanted negative scale. Origins: ground centre for freestanding props; defined footprint anchor for buildings; hinge for doors; feet/root for characters. Keep names stable so re-export and Unity GUIDs remain stable.

Export explicit FBX assets with only intended mesh/rig/animation objects. Keep Blender cameras, lights, and construction helpers out of runtime exports. Export animation clips with stable names and test their ranges and root transforms. FBX is Unity's primary supported model format, and direct export avoids a native `.blend` import depending on Blender being installed on every build machine. [Unity model import documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/ImportingModelFiles.html)

Bake complex Blender surface effects into base color, normal, and AO/roughness textures. Build the matching Unity URP materials explicitly; the Blender node graph is not the runtime shader contract. Share atlases and materials across prop families. Start with 1K maps for small families and 2K for major environment surfaces; increase only where the final camera shows a need. Convert roughness to Unity smoothness and pack channels deliberately. Verify color space, normal-map import, UV seams, and material assignment in Unity.

Prefer opaque geometry for leaves and flowers initially. Any cutout foliage needs tested depth, shadow, and visibility behavior. Water gets a small dedicated shader rather than expensive screen-space refraction. Add lightmap UVs only for assets actually using baked lighting. Bake stable surface detail/AO, not changing roof or door shadows into the ground.

## Unity presentation design

### Camera and compositing

Create a separate overhead orthographic camera using a URP Universal Renderer for the meshes. Render it into a texture sized to the live map viewport, then composite that image into the existing 2D gameplay view underneath its interface overlays. Add the new renderer without changing the current default renderer index. This is the initial design to validate, not an already-tested implementation. [Unity render-texture workflow](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/rendering-to-a-render-texture.html)

The existing 2D popup stack remains internally consistent. Render only the dedicated 3D world layer into the texture; exclude UI and the compositor itself. Hide the displaced 2D world art only while the 3D presenter is ready and active. Mode exit, asset failure, domain reload, and zone transition must restore the original view and release resources. Resolution changes resize the texture only when its dimensions change. Inventory/fullscreen/modal states follow the existing layout contract.

The overhead camera has no tilt or orbit in this first feature. Use a soft directional light, restrained ambient fill, and short readable shadows. Compare the Unity result directly with the reference; matching a Blender beauty render alone does not pass.

### Coordinates, identity, and movement

Keep simulation cells authoritative. The initial Unity XZ presentation mapping is:

```text
cell centre (x, y) -> (x + 0.5, visual height, Zone.Height - y - 0.5)
```

Wrap scale/origin in one projection service with a tested inverse. The authored central art area starts at X=21.25 and spans 37.5 units at the current Morrowfast scale. Its geometry is not a request to change the 80×25 simulation zone. Layout reconciliation must verify the source crop and building anchors against the supplied reference before final placement.

The 3D view binds existing entity/owner identities to prefab instances. A data manifest maps stable owner IDs, prefab IDs, anchors, footprints, room IDs, and state variants. Repeated static decorations need no gameplay entities. Mutating a GameObject is never a substitute for mutating the native game state. Save/load reconstructs views from real state; it does not serialize scene objects as a second world model.

Movement and actions continue through the current turn/grid systems. Visual movement may interpolate briefly between confirmed cells, with immediate interruption/reconciliation on teleport, death, mode switch, or zone change. No Rigidbody, NavMesh, or root-motion controller takes over gameplay movement. Simple colliders, where useful, support selection only; native footprints still decide blocking and reach.

### Interaction, visibility, and dynamic scenery

Screen input first rejects sidebar/hotbar/modal regions, then maps through the displayed texture rectangle and 3D camera. Move/throw/target commands use ground cells. Look/interact selection may resolve a visible mesh to its owner, but must validate visibility and native action rules. The cursor, ranges, projectile endpoints, status markers, and combat feedback must share the same projection contract; they cannot remain accidentally attached to the old XY world.

FOV/exploration and gameplay light data drive the 3D materials and object visibility. Distinguish visible, remembered, and unseen terrain; hidden actors and their indicators must remain hidden. A large building crossing the visibility boundary requires per-cell material masking, not only an on/off check at its centre. Shadow casting, reflections, selection, and particles must not reveal hidden entities. Use simplified shadow behavior at visibility boundaries if necessary for correctness.

Roof meshes respond to the existing room-reveal state, exposing the actual interior while leaving walls/collision intact. Doors, harvested beds, looted containers, destroyed props, displaced actors, equipment, and dropped items update from native state. Register and unregister views symmetrically. Provide a clear representation for unsupported entities or effects in the 3D view; never silently hide them. Full effect conversion outside this chunk is a separate expansion, but representative combat and targeting must work here before calling it playable.

## Implementation milestones and completion gates

| Phase | Work | Exit evidence |
| --- | --- | --- |
| V3D.0 — layout and baseline | Preserve reference; map visual anchors to existing owners; establish current test/performance baseline before implementation. Add a labelled cube/axis export and simple 3D viewport prototype. | Correct scale/orientation, working 2D UI/modal composition, screen-to-cell round-trip and no mixed-renderer warnings. A discrepancy ledger identifies any unmatched image objects. |
| V3D.1 — playable art sample | Build the well, one house with removable roof/door, one stall, path patch, barrel/crate and one character. Integrate actual owner binding, selection and basic visibility. | In Unity: move around well, inspect/interact, open door, enter room, expose interior, leave; all at the intended camera size. Blender/Unity/reference comparisons verify the visual target. |
| V3D.2 — complete village kit | Finish five buildings, two stalls, gardens, fences, props, vegetation, rocks, oven, water detail and character variants. Fill the entire authored terrain area. | Editable `.blend`, deterministic export manifest, reusable FBX/materials/prefabs and a composition scene matching the supplied image's main positions and proportions. No visible tile seams or repetitive wallpaper clusters. |
| V3D.3 — full gameplay binding | Wire all applicable owners, roofs, interior furnishings, loot/harvest/destruction states, equipment visuals, save/load, zone changes, cursors and representative effects. | Self-auditing native scenarios show state persists and visuals agree with simulation. Unmapped active content produces an explicit fallback and diagnostic. |
| V3D.4 — polish, performance and close-out | Lighting/material refinement, motion tuning, batching/instancing, cleanup, adversarial tests, cold-eye review and fixes. | Full suite green, native playable acceptance, Unity screenshots, paired profiles, rebuild/reimport check, living docs and user-facing asset guide. |

Each phase follows implement → review → fix before advancing. Production behavior follows RED → GREEN with positive and flipped-condition counter-checks. Pure art receives export validation and visual review rather than meaningless tests of individual modelling operations. Do not restart Unity or kill a live play session just to complete this planning document.

## Performance

Follow `Docs/PERF-FOUNDATION.md`: shared meshes/materials; pool transient views/effects; cache prefab lookups and keep missing entries cheap; use dirty cells/owner state rather than rescanning all owners each frame; no new per-frame LINQ or collection allocation. Group static decoration into small render patches to balance draw calls with culling and visibility masking. Keep mutable owners, doors, roofs and characters independently updateable. Measure SRP batching/instancing choices in the actual scene; do not assume every batching technique combines.

Initial asset budgets are design targets, not measured results: common props roughly 200–1,500 triangles; hero well/building assemblies roughly 3,000–12,000 each; character roughly 2,000–5,000; aim below 300,000 visible triangles and 150 draw calls for this chunk. Record exceptions where the image requires them and let profiling decide further optimization. Small stones/leaves should not exhaust the budget.

Target a stable 60fps at a recorded 1080p test resolution on the user's current machine, subject to measured hardware performance. Capture matched 75-second native scenarios before/after with frame-time p95/p99/max, CPU/GPU timing where available, draw calls, triangles, allocations, texture/render-target memory, and zone-load cost. A lower-detail setting reduces scatter, shadows and render-texture resolution while preserving UI clarity. No performance success is claimed before these captures.

## Verification and review plan

Asset validation: units, axes, floor pivots, applied transforms, normals, UVs, material slots, finite/nonzero bounds, stable asset IDs, FBX import without errors, rig/clip round-trip, independent roof/door pivots, and deterministic rebuild. Unity material screenshots are the export acceptance gate.

Integration tests cover cell/world/screen round-trips, viewport edges, resize, zoom, UI exclusion, owner identity, multi-cell footprints, missing assets, cutaway transitions, visible/remembered/unseen states, dynamic updates, saved state, fallback, and zone unload/rebind. Test a supported and unsupported or flag-flipped case for each invariant.

A dedicated 20–60-case adversarial fixture is required for the stateful integration. Include stale owner references after load; two instances with one blueprint; destroyed owner during a visual transition; roof boundary straddles; out-of-bounds clicks; shared materials leaking state; hidden actors casting shadows or remaining selectable; interrupted animation; rapid mode switches; render-texture resize/release; asymmetric event unsubscribe; and malformed manifest entries. Hypothesis review adds 6–12 player-flow probes where the project methodology requires it.

Native self-auditing scenarios must use actual runtime owners and normal gameplay paths, with fresh run IDs, preconditions, control cases and raw results. Cover moving/targeting, door/interior transition, harvest/loot, equip/drop, save/load, zone exit/re-entry and a combat effect. Implementation diagnostics should identify view binding/fallback/projection failures without logging every render frame.

Run the cold-eye symmetry, cross-feature, counter-check and doc-drift passes. Review preservation of current gameplay contracts; classify the new renderer as CoO-original. Fix notable findings before each commit and update this document with the same commit. Protect pre-existing dirty files, stage only owned hunks, and follow the user's headless test procedure with compile-error checks before trusting fresh XML results.

Honesty bounds: automated checks can establish transforms, object identity, state synchronization, visibility masks, resource cleanup and measured frame costs. They cannot establish that stone feels hand-crafted, that motion feels pleasant, or that the image composition is convincing. Inspect actual Unity captures and the live scene for those claims.

## Planned deliverables

- Editable master `.blend`, reusable asset collections, build/export scripts and deterministic manifest.
- Explicit FBX meshes/rig/clips, baked texture files, authored Unity materials, independent prefabs and assembled reference scene.
- A playable 3D presentation for this village with the existing interface and native state integration, plus a full-reveal art showcase.
- Automated validation/tests, native scenario evidence, visual comparisons, performance report and concise re-export/use instructions.

## Divergences and scope boundaries

| Topic | Deliberate decision |
| --- | --- |
| Existing 16×16 sprite rules | Continue to apply to the existing sprite pipeline. This requested 3D kit uses meshes and material textures in a separate directory; it does not convert them into 16×16 images. |
| Exact screenshot reconstruction | Preserve the visible composition and art direction. Hidden surfaces/interiors are authored; gameplay footprints and persistent identities take precedence over ambiguous generated pixels. |
| Fully filled reference | Fully revealed showcase; gameplay FOV remains authoritative. Filling terrain does not expose unexplored actors. |
| Render approach | Start with a separate 3D render texture because the installed pipeline rejects mixed renderer stacks. A different approach requires measured benefit and documented compatibility evidence. |
| World scope | One complete playable village chunk and reusable kit. Generalizing every biome, creature and spell to 3D is a later plan. |

## In-phase planning review

- 🟡 Corrected: assuming a mixed 2D/3D camera stack would work. The installed package explicitly rejects it; the render-texture proof is first.
- 🟡 Corrected: treating the visible village as the full 80×25 zone would distort square cells. Use the existing central authored area and projection adapter.
- 🟡 Corrected: exporting one static village mesh would lose independent roofs, containers, crops and actor state. Asset boundaries follow mutability and owner identity.
- 🧪 Pending implementation: material parity, compositor/input behavior, FOV/shadow masking and native performance are unproven until the milestone gates run.
- ⚪ Intentional: no all-world 3D replacement or alternate gameplay movement model in this plan.

## Implementation log

- 2026-09-09 — Planning: inspected the user reference, project methodology, installed render pipeline, current projection/viewport code and native settlement definition; checked official Unity model-import/render-texture documentation. Authored this plan and preserved the reference. No production implementation or test run performed in this planning task.

Files changed by this planning task:

- NEW `Docs/BLENDER-VILLAGE-3D-PLAN.md`.
- NEW `Docs/References/Village3D/reference.png` (byte-for-byte copy of the supplied image).

- 2026-09-09 — V3D.0: baseline 9,901/9,932 passing, zero compile errors. All 31 failures pre-existed this feature (29 pending GA03j equipment-sprite tests; PlaceProfileTests.TheProfileTable_IsData_AndComplete and WorldMapAuthoringTests.Sill_IsAtTheCentre_OnSpread_AtTierOne). Preserved the preimplementation state under `/tmp/codex-village3d-before`. No baseline-green claim.
- 2026-09-09 — V3D.0: authored and ran projection/visibility tests RED (missing production types), then implemented the two helpers; 34/34 GREEN. Real FBX probe: unit scale/floor and vertical axis pass; both horizontal directions are reversed under the initial export settings. Export correction is in progress before adopting the full kit. Native paired BEFORE capture started using ordinary bootstrap and isolated saves.


### Implementation verification corrections (2026-09-09)

| Assumption checked | Evidence and correction |
| --- | --- |
| FBX axis preset alone establishes east/north | Both forward presets reversed horizontal directions in Unity. Export-copy Z rotation π fixes both; four real imported-marker/unit/floor tests now pass. Authoring coordinates remain unchanged. |
| Native pickup returns an item to the carried list | Normal pickup auto-equips eligible equipment. Audit now compares carried + distinct equipped ownership, quantities and back-references. No inventory behavior was changed to satisfy the audit. |
| Identical native workload can reuse an implicit new-game seed | Ordinary bootstrap uses Environment.TickCount. An editor-only explicit seed is honored only inside an existing exact native-audit private root. Fourteen tests protect ordinary/player-build behavior. |
| Door cell dirtiness updates stationary sight/light | Two tests reproduced stale FOV and cached torch light. Native door changes now request a full render, and LightMap includes native door state in its cache key. Both passed in the first integrated run. |
| Soft-shadow flag alone creates overhead shadows | Existing shadow distance is 50; a camera at height 60 misses ground shadows. Camera altitude is now 35 with unchanged orthographic framing; actual GPU shadow acceptance remains pending. |
| Existing visual hooks support multiple subscribers | Legacy renderer assignments overwrite subscribers and conditional whole-delegate cleanup leaks them. New coexistence tests reproduced both; symmetric additive/removal wiring is under verification. |
| Repeated owner material preparation is harmless | A second pass converted cloned well water to stone material. Dedicated actual-prefab water-slot test reproduced this; mapping now recognizes the owned water clone. |
| Editor destroy callbacks run for every manually bound component | The real cleanup test retained the independent camera after component destruction. Explicit editor execution is now enabled for presentation-only components; cleanup tests are being rerun. |
| A completed native capture always reaches delayed cleanup | The second BEFORE captured successfully but lost its delayed final callback after save-isolation teardown. Final scene/view/seed cleanup was resumed explicitly through MCP. Update polling and reload recovery replace reliance on the delayed callback. |

### V3D.0–V3D.2 working state

The editable source kit and 123 runtime models are complete. Unity import `c84117bcdabb4a99b0842e63c96cf9a8` validated all 123 prefabs, four Generic rigs, five named clips per rig and four equipment sockets. The importer appended renderer index 1, preserved default renderer 0 and enabled soft shadows. Source geometry covers all 71 native owners, five buildings, 111 static placements and the full 80×25 ground. The separate player/additional residents bind at runtime.

Art source uses a painted 512×512 shared palette and real geometry rather than complex Blender node materials requiring texture baking. Roofs, doors, interior furnishings and mutable props remain independent. Six equipment attachment models are supplied; runtime equipment binding is still in progress. The native 67-cell creek and room footprints are preserved; the reconciliation ledger records their differences from the generated reference image.

A fresh rebuild from the adopted source produces an identical parsed manifest and palette PNG. All 123 runtime FBX files match semantically after roundtrip comparison of geometry, UVs, materials, rigs, sockets and animation curves. FBX byte equality is deliberately not claimed because exporter metadata differs. Whole-zone source estimate is 342,551 triangles; the conservative reference crop intersects 295,071 triangles. These are source estimates, not runtime draw counts.

BEFORE capture `a73bb40e320042c8a22b99babfa1a10b`: 16/16 native cases, zero unexpected errors, 76.2469 measured seconds, 21,397 retained frames, requested/actual world seed 729490642. Scene/view/private-root/seed restoration receipts pass after the explicit finalizer recovery described above. Provenance detected only generated `Assets/UnityMCP/Log/mcp.log` changing during capture; application source/assets were stable. Retained failed first pilot remains in its run-ID directory. No paired performance or visual-quality success is claimed yet.

First integrated test run: 142/143 passing (zero C# errors), including all 40 ownership/camera/state cases except the component-destruction camera cleanup pin. The following eight adversarial tests produced seven failures and one passing independent-material control; fixes are in the working tree and being verified. Current work is not a closed-out milestone.

Today's implemented work and fixes so far: editable Blender kit and export pipeline; explicit Unity materials/prefabs/rig controllers; native-owner 3D presenter with independent render texture, per-cell FOV/light, roof cutaways and selection; stationary-door FOV/light repair; deterministic audit bootstrap; corrected equipment ownership oracle; cleanup/reload recovery; material/hook/interruption review fixes. Player controls, equipment binding, native rendered acceptance, performance comparison and final review remain in progress.


### Reference-fidelity refinement (user steering, 2026-09-09)

The user explicitly requested assets and scene as close as possible to the generated reference. Independent actual-image review found the first complete kit recognizable but **not visually accepted**. Refinement targets irregular fitted cobbles, mossy roof plate/coping variation, denser organic foliage framing, stronger local depth/material separation, draped cloth, deeper well water, and readable timber details. Native creek and building footprints remain documented gameplay constraints. The ranked review is `Docs/Verification/Village3D/reference-visual-review.md`; final acceptance requires actual Unity captures, not only Blender previews.

V3D.3 controls: fourteen new tests observed RED for missing controls/settings, then fourteen GREEN after implementation. F11 switches presentation; Shift+F11 switches lower detail (31 cosmetic placements, owned shadows and 0.75 target dimensions). Neither control takes a turn. Paired audit forces full detail without changing saved preferences, then restores its runtime setting. Equipment ten-test RED is recorded; attachment implementation is now being verified.

Standalone GPU pilot `3b1cc849ac0c4188bab1d9139d6512d6`: ten groups passed (color visibility, memory, water/time, real shadow casting/suppression). Composite group stopped at its source-color positive control because Unity Color.yellow has a non-binary green value and material color conversion changes it in Linear space. Fixture now uses explicit (1,1,0,1), matching its intended gamma-independent marker contract. The failed pilot remains retained; no compositor success is claimed until rerun.


### Authorized follow-on scope (2026-09-09)

The user authorized two sequential additions without further decisions: (1) after reference refinement, make this village the ordinary **new-game spawn area**, preserving existing-save locations; (2) then extend the first ring of **eight surrounding surface chunks**. The ring phase must inspect actual existing generated chunks, canon, landmarks, creatures, blocking and exits before authoring one ImageGen 3D reference per chunk, then implement corresponding reusable Blender assets and native Unity presentation. ImageGen images are visual references, not replacements for playable mesh scenes. Reuse consistent scale/material/camera conventions, preserve gameplay identities and transitions, and verify shared borders. These follow-on phases are authorized and outstanding; the current village is still being refined and verified.


V3D.3 native AFTER pilot `6d3f271ce1bd4a5595d1bf01adf96c2a`: 15/15 functional cases, zero unexpected errors, 76.506 measured seconds /20,298 frames at1920x1080 on Apple M5/Metal. Ordinary movement, actual door/menu/room/loot/drop/auto-equip, save/load and zone re-entry passed. Automatic final cleanup now succeeds without manual resumption, private save root removed. Source provenance changed only generated MCP log. Archived under `Docs/Verification/Village3D/pilot-after-6d3f271ce1bd4a5595d1bf01adf96c2a`. This is a pilot before final art/materials/spawn integration and does not close visual acceptance or performance. Actual visible-frame light is too dark compared with reference; material calibration is pending. Native profiler counters include UI/shadow/editor costs; no claim that total rendered triangles equal source mesh triangles.

V3D standalone GPU rerun: 11/11 groups passed, zero unexpected logs, original scene/dirty/RT/async settings preserved. Evidence: `gpu-pilot-2/shader-gpu.json`. Actual Renderer2D composite orientation and FlipY counter-control now pass following pure-yellow fixture correction.

V3D showcase build now succeeds through a prefab-backed scene adapter. Unity cannot save preview scenes or create additive scenes beside an untitled startup scene; public PrefabUtility saves the hierarchy and a minimal Unity6000 scene wrapper uses its real persistent IDs. Candidate and final scene preview reload validate all71 owner transforms and roof/interior references. Existing open-scene handles/paths/dirty flags stay unchanged. First actual GPU showcase captures retained in `showcase-8e483b074ca04dfebc35d3e135aeaaf3`; they are the earlier art revision, not final fidelity approval. Scene artifact/cutaway and20-case adversarial gates are running.


### V3D.4–V3D.5 current verification (2026-09-09)

Refined art adopted from `ArtSource/Village3D/build_scene.py` SHA256 `3225513e4d8cc62a43e4a927959827c4dfeedff374a16f4bc5fde91232627524`:1024×1024 palette with64 stable swatches, irregular paving, fuller foliage, flatter moss-covered roof slabs, cloth folds and6 bare garden cells. All123 model contracts,71 owner bindings and111 static placements stay stable. Fresh independent rebuild passed123 semantic FBX comparisons with exact palette/parsed-manifest equality; container byte equality remains0/123 due exporter metadata. Source whole-zone362,905 triangles; conservative crop328,705 including both roof/interior states. This exceeds the provisional crop target and is an explicit visual-budget exception, not a performance claim.

Equipment and showcase regressions:18/18 GREEN after correcting attachment tilt from-65° to-25° (actual projected blade extent reproduced RED) and correcting a Unity6 SceneHandle boxing mismatch in the artifact test oracle. Dedicated presenter adversarial gate:20/20 GREEN; cutaway22/22 GREEN.

New-game spawn18-case RED run:11 failures/7 controls passed,0 compile errors. Garden9-case RED run:9 failures,0 compile errors. Production now chooses Morrowfast only on fresh world generation, prefers(40,23), and validates allsix garden terrain references before adding Plantable and updating the zone tag index. Shared WorldMap.StartingZoneID remains Sill because population/settlement identity also uses it; ApplyLoadedGame remains unchanged. GREEN and native proof pending.

Exposure calibration test reproduced RED for missing world/water property. After implementation all12 GPU groups passed, but the overall runner incorrectly still expected11 and returned failure; that stale count is corrected and a fresh formal rerun remains required. Presenter-owned material clones use exposure2.2 while retaining native relative light, black unlit cells and FOV masking. Showcase materials remain exposure1.


Refined import `c129c5b13d1d4272945e5d844f24154f`:123/123 prefabs validated, all original model GUIDs preserved, renderer/default pipeline state preserved, no GUID collisions across3101 GUIDs. Selected art/garden218/218 GREEN; fresh-spawn18/18 GREEN. GPU `gpu-refined-final/shader-gpu.json`:12/12 formal PASS,0 unexpected errors. Rebuilt/reloaded showcase and actual refined captures: `showcase-b8beed2305b5444193ede8f02223b6ea`.

Refined ordinary native play `ac97b7f2d82f4e91ab34d676b6f064f3`:35/35 cases,0 unexpected errors,75.452 measured seconds/19,200frames. Verified real N starts Morrowfast(40,23), exact six indexed garden refs,71-owner aliases, roofs/interiors, real looting/drop/auto-equipped dagger, F5/F6 reconstruction, exits/reentry, cleared crate, display toggles without turns, and ordinary JetBlast/cancel with1 native cast callback/12 FX atoms. Actual1920×1080 startup/interior/equipment/spell captures inspected. Automatic cleanup restored scene/view/preferences/seed and removed private saves. Source provenance changed only generated MCP log. See `Docs/Verification/Village3D/PERFORMANCE.md`: typical frame p95 is within16.7ms, but walking p99/long spikes miss stable60fps; no unqualified performance claim.

Full suite after refinement/spawn:10,165/10,196 pass,0 compile errors; exactly the same31 preexisting failures as baseline,264 added cases allpassing. Twelve independent late-cell garden adversarial controls passed with no new production changes. Cold-eye found one evidence-validator defect: AFTER acceptance did not require the preference-restoration receipt. Tests-first correction in progress; no preference leak was observed in the completed capture.


V3D.4 cold-eye correction complete: preference-restoration verifier test observed1RED/9passing then10/10GREEN with the AFTER receipt enforced; historical BEFORE remains accepted. This editor-only finalizer change does not alter runtime presentation. Final native receipt already records `displayPreferencesRestored=true`. Village refinement and fresh spawn are accepted for continued ring development, subject to the documented31 preexisting suite failures and measured long-frame/performance limits. No stable60fps claim.

Commit isolation: the working baseline contains2145 preexisting paths, including native Morrowfast/Felling infrastructure required by this feature. Preexisting hashes were checked: only the explicitly owned GameBootstrap/CameraFollow/ZoneRenderer/MorrowfastDoor/AnimatedEntityRenderer hunks, this living plan and generated MCP log differ. They are retained as reviewable working changes; staging unrelated infrastructure to create a falsely self-contained commit would violate the user's preservation instruction.


### Regression after the eight-zone ring integration — September 10

Shared surface/GPU regression passes12/12 groups,51 actual Metal captures, with scene/dirty/active-RT/async/source controls intact (`Verification/Village3D/regressions/20260910T062650Z-gpu-dfed68e9631e464c906cecba1a6a1c69/`). Native gameplay reruns `ee7aae1e179d4f7aba5a72b71389e4a0` and `7f9bc49c7af547988d244fe94e43c4ab` both pass35/35 plus owned cleanup/source preservation. Their stricter whole-Editor wrapper remains FAIL for recovered MCP startup connection errors and Unity AssetStoreOAuth failures during shutdown; neither is mislabeled a clean environment pass. Latest raw archive: `Verification/Village3D/regressions/20260910T070845Z-native-3eef38e6476d424389d407d906f59987/`.

Ring final actual art/native78/78/80.023s paced profile and independent raw recomputation pass. The new ring adapter and shared surface retain the existing Village spawn,71 owners, gardens, doors, roofs, equipment and native spells. See `BLENDER-SPAWN-RING-3D-PLAN.md` for the ring acceptance and performance/fidelity limits.
