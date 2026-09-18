# Felling-Site — reference scene implementation

Status: offline plan, interactive study, geometry export, validators, staged C# core, and an extracted RGBA component package prepared. The current package has 55 layers: 39 removable candidate props and 16 fixed/effect partitions, with local inferred backing and an actual transparent atlas. Two earlier opaque Imagegen results remain preserved as historical attempts. Final aggregate results belong in the current verification reports. Unity has not been opened, controlled, compiled, or tested for this work.

## 1. Outcome and scope

Recreate the user's selected revised Felling-Site image as a playable authored location: enormous pink-grey fossil wood above a six-position clearing, root buttresses, blackwater at the left edge, sparse living color, and a small traveler approaching from the south. Preserve this particular composition and palette as closely as possible.

The approved reference is `exec-3c41243a-cf1b-4066-b595-523cc93174bb.png`, copied into `ArtSource/FellingSite/reference.png`. The earlier image with carvings is not the target. The plan concerns this location, not a global art-style migration or a new ending mechanic.

User-authorized first stage: complete useful work that does not require Unity. Stage artwork, editable scene geometry, a playable browser study, offline validators/tests, and integration source outside `Assets/`. The user subsequently authorized deterministic image scripts for source inspection, extraction, masking, local backing composition, and export. Work inside the open Unity project and integration tests are a later stage. Existing uncommitted gameplay and renderer work is not replaced.

## 2. Visual and lore requirements

- Present-day aftermath, approximately 1,080 years after the Felling. The wall is petrified sandstone/tree anatomy, with continuous grain and cream seams. No added temple carvings, animal fossils, faces, or runes.
- Exactly six bare positions. Their centers and readable soil boundaries should survive the reconstruction. The seventh is one subtle point of discontinuity, with no monster, portrait, portal, or spotlight.
- Preserve the reference's open center, silhouette of the root walls, left waterfall and stream, dark green ground, small wine-red sundews, restrained teal/magenta living accents, and southern approach.
- Separate the traveler from the environment. Future actors can move, turn, and stand behind scenery. The background must not retain a duplicate player.
- An authored backdrop is the first faithful visual layer; it is not by itself the completed playable scene. Moving water, foreground occlusion, cell visibility, changing objects, and gameplay geometry need separate treatment.

Authority: `Lore/10_Bible.md` §IV; `Lore/11_SecondSpine.md` C1; `Lore/MYSTERY-LEDGER.md`; `Lore/History/02_Geography.md` (the Felling-Site); `Docs/FELLING-WORLD-DESIGN.md` §3.6. These constrain scene content. The selected image and this document supply staging proposals, not new canon.

## 3. Verified constraints and decisions

| Finding | Evidence | Decision |
|---|---|---|
| The game zone is fixed at 80×25 cells. | `Assets/Scripts/Gameplay/World/Map/Zone.cs:13` | Keep the existing logical zone; embed the scene in its middle 48 columns. |
| Reference is 1536×1024; normal terrain uses 16 source pixels/cell. | selected PNG; `EnvironmentSpriteRenderer.cs:674` | Use 32 art pixels per logical cell for this scene presentation. The simulation remains one cell per step. |
| Existing import code forces 16 pixels/unit in its sprite folder. | `Assets/Editor/SpriteImportPostprocessor.cs:26` | Stage outside `Assets`; later give this art a separate import path and 32 PPU rule. No silent global importer change. |
| Ground already supports 8×8-cell macro fields. | `EnvironmentSpriteRenderer.cs:649` | Reuse data concepts; do not force large cliff illustrations through the 16×16 macro loader. |
| Actors and normal environment occupy fixed sorting bands. | `AnimatedEntityRenderer.cs`; `EnvironmentSpriteRenderer.cs` | Add foreground occlusion/depth relationships for large roots, with footprint anchors. |
| Visibility and environment changes are cell-based. | `EnvironmentSpriteRenderer.cs:935,973`; `AnimatedEntityRenderer.cs:129` | Large layers need a cell mask and ownership data; static art must not reveal hidden entities or survive a destroyed owner. |
| Normal camera framing includes HUD space and follows the player. | `Assets/Scripts/Presentation/Cameras/CameraFollow.cs` | Author an art-bounds camera profile and test it with the actual HUD. |
| Main scene, renderer, importer and gameplay files have unrelated local edits. | initial `git status --short` | Add staged files only during offline work; integrate against then-current code in a separate phase. |

This is CoO-original scene presentation; no Qud-parity claim is involved.

## 4. Coordinates and composition

The layout's source coordinate space is the original image: origin at upper left, x right, y down. The reference and clean plate retain the exact 1536×1024 canvas.

- Art density: **32 pixels per cell**.
- Image ground origin: **(0,224)**. The upper 224 pixels are seven cells of visual overhang above the logical zone.
- Embedded logical region: origin **(16,0)**, size **48×25**, within the existing **80×25** zone.
- Pixel to cell: `x = 16 + floor(px/32)`, `y = floor((py-224)/32)`.
- Cell center in image: `px = (x-16+0.5)*32`, `py = 224+(y+0.5)*32`.
- Image pixels above y=224 have no walkable cell; the cliff's visual height is not additional traversable rows.
- Existing cell-to-world convention gives continuous artwork bounds x=16..64, y=0..32. Reference camera target: center **(40,16)**, orthographic half-height **16**, visible game aspect **1.5** before HUD adjustment.
- Full reference at one source pixel per display pixel is the comparison baseline. A 16×24 existing actor displayed at 2× becomes 32×48 pixels, close enough for a proxy; an exact final actor scale is a visual QA decision.

The initial layout contains conservative hand-authored walkability, cliff/root/water blockers, named focal points, a south entrance, source-space effect regions, and root occluder polygons. Geometry is an editable proposal. Reachability tests establish that it is internally coherent, not that every collision edge looks correct.

## 5. Art preparation

| Asset | Preparation | Acceptance |
|---|---|---|
| `reference.png` | Byte-for-byte copy of selected image. | Dimensions and SHA-256 recorded; immutable source. |
| `clean-plate.png` | Imagegen removes only the traveler and reconstructs the small obscured floor area. | No duplicate actor; six positions and overall layout retained. |
| `ground-repair.png` | Imagegen creates a matching quiet ground/vegetation repair study for later erased objects. | Same canvas; explicitly an alternate repair source requiring alignment review, never silently replaces the main plate. |
| `Components/build/prop-atlas.png` | User-authorized deterministic extraction supplies 39 reference-derived candidate prop sprites after the opaque Imagegen attempts. | Actual RGBA with source rectangles and proposed pivots in `prop-atlas.json`; final silhouette/import review remains separate. |
| `Components/build/manifest.json` and backing layers | Compose 55 cropped layers from the baseline and locally repaired backing, with source-position contact contributions and inference masks. | Declared ordering, local changes, source/provenance comparisons, and limits remain visible; no implicit gameplay readiness. |
| `player-sheet.png` | Copy of existing project actor sheet for the browser proxy. | Existing art only; correct frame rectangle, no invented animation promises. |

Initially, Imagegen performed all image edits while offline code only validated images and prepared JSON. After explicit user authorization for image scripts, `ArtTools/felling_components.py` added deterministic masking, extraction, local backing adjustment, compositing, and RGBA export. Imagegen still supplies the preserved hidden-environment source. Its full output does not replace the reference: inferred material is applied only within authored local support. Save exact prompts, source paths, selected outputs, dimensions and hashes. A generated hidden surface has no original-image ground truth.

The first scene study draws the original reference and overlays only the 80×96 pixel rectangle at (736,800) from the generated clean plate to remove the painted traveler. This preserves 99.51% of the base image source area exactly; it is not a measured final-render similarity score. Clipped samples of the same composition provide foreground occlusion, preserving alignment. This is a rendering technique over unchanged source artwork. Final root layers can retain this technique or use approved separate cutouts. Movable/destructible objects must eventually be fully separate from baked scenery and reveal an approved repair underlay.

The [component package](../ArtSource/FellingSite/Components/README.md) uses that actor-free baseline as its intact assembly target. It has 39 removable candidate props and 16 fixed/effect partitions. Fixed partitions can contain inferred backing beneath candidate props; the full-canvas inference mask intersects each sprite's alpha to identify those pixels. A fixed component's `repair.inferred: false` concerns its own removal backing, not the absence of all inferred pixels inside its sprite.

These cutouts contain only reference-visible surfaces. They do not provide complete unseen sides for free movement, rotation, or arbitrary newly exposed overlaps. Contact contributions belong to the original ground location. Broad fixed sections are review/depth partitions; hiding one can reveal diagnostic transparency rather than a traversable replacement surface. Each component currently carries one provisional anchor cell, not a validated physics or multi-cell visibility footprint.

## 6. Offline implementation phases

### P0 — baseline and plan

- Copy the selected reference outside `Assets/`, record provenance, and capture relevant source constraints.
- Create this plan before integration. Record corrections and work status here as implementation progresses.
- Keep changes additive; do not start Unity or refresh its asset database.

### P1 — asset preparation

- Generate the clean plate first, inspect the actor removal and the six/seventh positions.
- Prepare supporting ground repair and transparent prop studies in parallel.
- Verify dimensions, alpha where requested, metadata and file availability.
- Preserve originals and exact prompts. Use explicit statuses for pending/approved-for-study assets.
- Following the user's authorization for image scripts, author source-scale masks and local repair support; export the actual RGBA component package and atlas. Preserve rejected opaque attempts rather than treating them as cutouts.

### P2 — authored layout and offline correctness

- Create `ArtSource/FellingSite/layout.json` as the single reviewable geometry source.
- Implement `ArtTools/felling_scene.py`: coordinate mapping, polygon validation, cell occupancy, safe paths, reachability and export.
- Test negative art coordinates, out-of-region positions, duplicate or seventh-overlapping marks, malformed/self-intersecting polygons where supported, unreachable focal points, blocked spawn/entrance, and diagonal corner-cutting.
- Export occupancy and a Unity import data document without launching Unity.
- Store raw validation results in the scene package. Do not treat approximate geometry as observed game behavior.

### P3 — interactive browser study

- Create `preview.html`, `preview.css`, `preview.js` loading the same layout and selected art.
- Allow keyboard movement and click-to-path with collision; offer reference vs clean-plate comparison, grid/collision/landmark overlays, foreground occlusion, and illustrative fog/effects.
- Label preview lighting, visibility and movement as approximations of future integration. Browser movement does not prove Unity input, combat, saving or FOV behavior.
- Make reduced motion available and keep the actual image the dominant part of the viewer.
- Inspect the viewer in a browser at the source aspect ratio and a smaller viewport. Check load failures and fallbacks explicitly.

### P4 — staged integration source and handoff

- Prepare isolated source/data for image-to-zone mapping, landmark validation, visibility masks, owner changes, and scene presentation configuration where it can be compiled/tested offline.
- Prepare the Unity-side installation/building recipe against verified existing APIs. Keep unverified Unity adapters outside the compiled `Assets` tree.
- The handoff must identify exact import location, PPU, layer order, camera bounds, terrain/actor suppression, reference hash and validation commands.
- Run meaningful offline tests and an independent code/asset review. Resolve correctness findings before reporting completion of this stage.

### P5 — component extraction and hidden-backing review

- Keep the original reference, actor-free baseline, generated backing source, authored extraction inventory, and exported layers distinct and traceable.
- Compose the base, then each contributing component in ascending `(depth,id)` order, with its source-position contact contribution before its sprite.
- Provide independent visibility changes for the 39 candidate props. Fixed/effect layers can be hidden for diagnostic inspection, not treated as game-removable terrain.
- Use `Components/index.html` to compare original/baseline/reconstruction/base, inspect source/sprite/underlay crops, select actual alpha coverage, remove/restore/solo layers, and show inferred backing at its real depth.
- Verify manifest/data contracts, local asset dimensions, exact intact baseline assembly, outside-support stability, protected six/seventh regions, and current-state recomposition. Record results instead of inferring them from the existence of an atlas.
- Review masks, local material transitions, broad structural boundaries, overlaps, and all supported reveal states at native size. Treat inference, fixed-section voids, provisional anchors, and incomplete unseen geometry as explicit integration limits.

## 7. Unity phases — explicitly remaining

### U1 — import and isolated scene assembly

1. Recheck the live project's current changes and compilation health.
2. Import approved scene files into a dedicated resource family with 32 PPU, point filtering, no mipmaps/compression, source size preserved, and correct pivots.
3. Add a scene-specific environment component and a manual showcase entry. Keep live world routing disabled until tests pass.
4. Draw the authored ground/background at the planned bounds. Enable existing animated actors above the ground; apply foreground roots through depth-aware overlays.
5. Ensure legacy environment art and its baked floor/lighting do not double-render under/over the new family. Sprite-off mode must still show the normal glyph representation.

### U2 — gameplay geometry and visibility

1. Stamp terrain and blockers through actual zone/entities, reserve authored cells, and verify the south approach connects to the clearing.
2. Use existing material/physics definitions where appropriate. The cliff is a boundary; water traversal and damage use actual game rules. Art tags do not substitute for physics.
3. Put the six marks and seventh discontinuity in stable authored cells; defer endgame interactions to the narrative system's real state.
4. Connect every rendered region to visible/explored cell data. Do not allow a giant texture to expose unrevealed rooms, NPCs or effects.
5. Tie removable overlays to owning entity IDs/state. After removal or burning, reveal coherent ground and update affected visual regions rather than only the changed cell.
6. Save/load rebuilds presentation from gameplay data; textures, meshes and animation clocks are not new persistent simulation data.

### U3 — motion, lighting and camera

- Animate the left water surface and waterfall in their own masks; add restrained splash/spray. Keep the dark water footprint readable.
- Add sparse living motes and slight plant movement, with independent reduced-motion settings. Do not animate the entire giant cliff.
- Keep reference lighting as the initial color target. Prevent additional ambient tint/light multiplication from making already-shaded artwork too dark.
- Establish the camera art bounds and HUD-safe crop. Test reference framing, ordinary player following, edge behavior and input coordinate conversion.
- Keep the seventh effect localized and quiet. Its rendering does not assert a new Urqu mechanic.

### U4 — test and visual acceptance

- RED→GREEN tests around new zone generation, grid/art mapping, sorting, visibility, changed-owner invalidation, save/load, and sprite-toggle fallback; pair positive and negative conditions.
- PlayMode: walk the south entry and all six positions, stand behind each foreground root, approach water, change a removable object, leave/re-enter, save/load, toggle sprite mode and reduced motion.
- Capture a deterministic 1536×1024 game-only view and actual HUD view; compare silhouette, major landmarks, actor scale, palette, water path and negative space against the reference.
- Inspect both native and reduced display sizes. Measure draw calls, texture/mesh memory and frame time on the user's target hardware; budgets must be set from a baseline, not invented here.
- Route the finished authored content to the reserved Felling-Site at overworld (3,5) only after confirming existing content and narrative ownership.

## 8. Acceptance checklist and reporting boundaries

- [x] Reference and all generated study sources preserved with prompts/hashes.
- [x] Actual RGBA atlas and 55-layer component export created, with source rectangles and proposed pivots. Historical RGB studies remain rejected and preserved.
- [ ] Final native-size silhouette/backing review and production footprint/pivot approval complete; final aggregate outcome recorded in the current reports.
- [x] Traveler removed in the main study composition; six marks and seventh absence checked visually. Final patch texture approval remains an art review task.
- [x] Layout cells in bounds; south entrance, spawn, six positions and seventh mutually reachable under the conservative offline movement policy.
- [x] Coordinate/geometry tests pass, including negative cases.
- [x] Browser study loads selected assets, supports movement and visible review toggles; fit, native-size and narrow viewport checked.
- [x] Staged code and data have a documented route into the real renderer.
- [ ] Unity import/compilation and EditMode tests run.
- [ ] Actual FOV, ordering, gameplay changes, save/load and input verified in PlayMode.
- [ ] Reference and HUD screenshot comparison completed.

Only completed items are reported complete. Offline outputs can prove file validity, geometry consistency, coordinate math and browser behavior. They cannot prove Unity rendering, gameplay correctness, performance, lighting, platform import behavior or final art fidelity.

## 9. Implementation log

- Plan created; current repository constraints checked. The selected image is the revised fossil-grain version. Initial working tree already contains substantial renderer/gameplay changes.
- Key correction to earlier informal discussion: the visual's 48×32 art footprint does not fit the 25-row zone directly. Seven rows are explicitly visual overhang; no zone-size change is required.
- Historical alpha stage: two Imagegen prop outputs were RGB with painted checkerboards. The strict transparency gate correctly rejected them.
- Later user-authorized stage: deterministic source extraction created the true RGBA atlas and component package. `layout.json` now resolves `environment-props` to `Components/build/prop-atlas.png`; the transparency requirement was not removed.

## 10. Completion record

### Completed offline deliverables

- `ArtSource/FellingSite/README.md`: launch instructions, file inventory, verification commands and remaining work.
- `ArtSource/FellingSite/reference.png`, `generated/clean-plate.png`, `generated/ground-repair.png`, two prop studies under `generated/studies/`; exact prompts and SHA-256/source metadata in `imagegen-prompts.json`, `asset-provenance.json`, and `reports/alpha-attempt-prompt.json`.
- `ArtSource/FellingSite/layout.json` and `Prepared/`: 306 walkable cells; eight destination routes (spawn, six positions, seventh) from entrance. The source layout now references the actual component prop atlas; strict verification refreshes the prepared handoff.
- `preview.html/css/js`: fit/native-size browser study using the existing actor; keyboard and click paths, overlays, foreground sample clipping and illustrative ambient effects. No build dependencies.
- `ArtTools/felling_scene.py`, `test_felling_scene.py`, `test_felling_preview.cjs`, `verify_felling_scene.py`: validation, exports, regression suites and repeatable evidence.
- `Integration/Core/FellingSceneCore.cs`: pure C# mapping, immutable occupancy, no-corner-cutting paths, sample-level visibility ownership and owner invalidation. Original RED/GREEN evidence and subsequent results are preserved in the reports. Independent review found no concrete defect within this stated scope.
- `Integration/Unity/FellingSceneArtImporter.cs.txt` and `Integration/UNITY-INTEGRATION.md`: staged importer and verified source-level handoff. No runtime presenter/GPU ownership mask has been implemented or tested.
- `Components/README.md`, `authoring.json`, preserved backing source/prompt, `build/manifest.json`, 55 RGBA layers, local masks/contact contributions, reconstructed/removed comparisons, and the 39-prop atlas with slicing metadata.
- `Components/index.html`, `viewer.js/css`, and `ArtTools/test_felling_components_viewer.cjs`: separate asset-state/provenance viewer and reproducible contract checks. `ArtTools/felling_components.py` and the component Python suites supply extraction and export validation.

### Verification boundaries

The latest aggregate outcome and raw results are in `ArtSource/FellingSite/reports/report.json`. The strict asset check remains enabled, but its `environment-props` input is now the actual RGBA component atlas. Run `python3 ArtTools/verify_felling_scene.py` from repository root to reproduce the current scene verification. Do not infer an aggregate pass from the old failure's removal.

The parent scene runner's recorded 2026-09-05 run passed every step: strict alpha/assets validation, a fresh prepared export, and 61 tests (28 Python, 16 Node, 17 C#). This is an observed result in that report. Regenerated component assets require a fresh run; the separate component verification outcome is recorded by `python3 ArtTools/verify_felling_components.py` and is not inferred from those 61 tests.

Historically, valid geometry was exported without `--require-assets` while the opaque prop studies were pending. Current handoffs should be refreshed through strict asset verification. Component producer measurements live in `Components/build/reports/verification.json`; component test evidence lives in `Components/reports/`. Their source-pixel and protected-region checks do not certify the quality of unseen material or Unity behavior.

Browser observations for the earlier movement study are recorded in `ArtSource/FellingSite/reports/browser-review.md`. The component viewer provides a separate review surface. Final silhouette/backing approval, complete unseen surfaces, gameplay image fidelity, cell ownership, and every Unity phase above remain subject to their stated acceptance criteria.

Component browser review exercised an exact 1,572,864-pixel RGBA baseline comparison, boulder hide/restore/solo, all-39 removal, masks, inference, exploded layers, native 1536×1024 scrolling, and 390/1280-pixel responsive views without console errors. These are browser observations, not Unity tests or a final component test-suite count. The later work is specified in [FELLING-COMPONENT-EXTRACTION-PLAN.md](FELLING-COMPONENT-EXTRACTION-PLAN.md) and [FELLING-COMPONENT-EXTRACTION-PROMPT.md](FELLING-COMPONENT-EXTRACTION-PROMPT.md).

### Immediate continuation

1. Record the current strict scene/component verification outcome, then review the actual RGBA source rectangles, proposed pivots, masks, and scale.
2. Review the localized actor-removal texture and inferred backing at native size. Complete unseen surfaces and multi-cell ownership for the precise gameplay interactions to be supported; retain fixed-section diagnostic restrictions until then.
3. Begin U1 in an isolated showcase against the then-current Unity project; implement the real presenter, visibility masks and scene geometry through actual gameplay APIs.
4. Complete U2–U4 before production world routing. No Aseprite purchase is a prerequisite for using this package.
