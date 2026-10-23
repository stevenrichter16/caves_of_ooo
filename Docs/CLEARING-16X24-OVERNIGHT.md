# Seed keeper clearing — overnight art pipeline

Status: started 2026-10-22 evening America/Chicago. The user subsequently clarified full gameplay integration and allowed the time each sprite and animation requires. The morning deadline and crop-only next step are superseded by [Live 16×24 art direction and integration](LIVE-16X24-ART-DIRECTION.md). Finish the defined live opening release and report regional coverage honestly.

## Direction and boundaries

Build an original, spare, flat 16×24 environment around the five user-approved sprites. Preserve their exact artwork and the blank five-sprite study. Create a separate clearing scene and reusable environment kit. Use editable Blender models as sources where they make sense; render them to the flat pixel presentation. Preserve the previous 3D style and quick-return menu. This is an art production pipeline with real Unity evidence, not permission to invent a second gameplay simulation.

The clearing should read as a lived-in seed exchange and cooking stop: seed sorting shelter, clay oven, water well, harvested and growing beds, drying rack, baskets and sacks, a short broken wall, quiet grass and a grazer enclosure. Keep paths and interaction spaces legible. Sparse dark green ground, small restrained palette, square pixels, discrete silhouette groups and warm accents. No text labels floating over every object, bloom or noisy ground texture. Existing NPC gameplay and lore should guide integration; do not add unsupported lore claims.

## Historical study prompts — superseded for active production

The prompts and milestone list below record the completed static art study. Active agents must follow [the live brief's required production pipeline](LIVE-16X24-ART-DIRECTION.md#required-production-pipeline-and-agent-handoff): actual playable greybox, capture, imagegen reinterpretation, sub-part extraction, native 16×24 sprite/animation production, real integration and gameplay/performance review. The user authorizes any available tools needed. The older permission to finish only a kit does not satisfy the current task.

### External references added at the user's request

Read [ART-REFERENCE-LIBRARY.md](ART-REFERENCE-LIBRARY.md) before the next art iteration. Eight visually inspected sources cover Qud's spatial readability, practical seed storage and earthen cooking, dry seedheads, sundews, salt crust, wetland roots and reclaimed machinery. The approved sprites remain the style anchor. Choose one or two source IDs for each relevant imagegen concept, record the specific feature being translated, and keep the output original and readable at native size. Prioritize the real opening's playable greybox and complete visual coverage from the live brief. Source URLs, credits and usage notes are preserved in `ArtSource/Clearing16x24/references.json`; no external image files were added to the game's assets.

### Plan prompt

Inspect the approved 16×24 assets and current scene routing. Design one coherent clearing with modular environment assets, clear traversable routes and contrasting silhouettes. Verify integration assumptions against code before implementing. Make the result reusable without replacing approved sources. Choose a bounded set of high-impact assets and testable completion gates. Budget disk and runtime cost. Record decisions and evidence here.

### Art prompt

Greybox the clearing at the real cell proportions, capture it, then use built-in imagegen to reinterpret the captured composition in the restrained palette and silhouette language of the supplied sprites. Critique the actual generated image. Extract a practical modular asset brief, model the selected parts in Blender, and render on exact 16×24 canvases. Keep editable sources, provenance and rebuild commands. Judge the work in Unity at native and Steam Deck presentation sizes.

### Implementation prompt

Continue the next unfinished milestone below. Inspect current files before editing. Preserve unrelated work and the blank study. For behavior changes, write and run meaningful failing tests first, then implement and counter-check. Reuse existing gameplay and rendering surfaces. Finish each useful slice with native Unity import/compile checks, actual screenshots, review, living-doc update, and exact-path staging. Fetch/rebase before pushing HEAD to main. Do not claim completion from imagegen concepts or batch renders alone.

## Historical static-study milestones

1. Preflight and greybox: inspect assets, lore, scene routing, tools and disk. Create editable greybox and capture. Establish overnight continuation.
2. Imagegen direction: produce and critique screenshot-based art concept plus modular prop reference. Save prompts and sources.
3. Environment kit: create roughly 12–18 distinct props/terrain accents, with coherent scale, native pixel grid and editable Blender sources. Render/inspect actual native sprites; iterate on weak silhouettes.
4. Unity clearing: import the kit with Point filtering, uncompressed alpha, common unlit material and consistent pivot. Assemble a separate scene around the exact approved characters. Keep blank study and original world one-click accessible.
5. Functional integration: use audit findings to connect the presentation to existing real game entities where practical. Keep any visual-only preview clearly identified. Do not replace working gameplay with canned interactions. If actual renderer conversion is too broad for this night, finish the kit/scene and state the remaining conversion accurately.
6. Verification and polish: actual Unity screenshots, camera/cell/picking checks for any gameplay integration, bounded test run for changed behavior, performance/import audit, independent review, fix significant findings. Commit and push completed slices. Restore editor/test preferences.

## Verification sweep

| Assumption | Evidence / decision |
| --- | --- |
| Current study is playable | False: `Docs/PIXEL-16X24-STUDY.md` confirms a static five-sprite scene. Preserve it. |
| All world tiles are already 16×24 | False: existing environment renderer uses 16×16; actor source frames use 16×24. A wholesale conversion requires camera, cursor and effects work. |
| Unity is running | No Unity Editor process at initial preflight. Unity Hub is running. |
| Unity connector is ready | unity666 endpoint returns HTTP 404; local/editor fallback being investigated. |
| Space is unlimited | Only 4.3 GiB free. Avoid player builds, large texture packs and duplicate cache trees. Keep generated sources compact; remove only verified regenerable project caches if needed. |
| 3D generator is required | Installed Blender can produce editable modular sources; no configured Tripo/Meshy tool discovered. Use Blender rather than depend on a new paid service. |

## Progress / next continuation

- Greybox modeled and rendered in Blender; source and capture saved in `ArtSource/Clearing16x24`.
- Built-in imagegen scene pass saved as `concepts/clearing-concept-v1.png`. Review: composition useful, but ground noise and prop microtexture are too busy for the approved actors.
- Built-in imagegen modular pass saved as `concepts/modular-props-v2.png`. Review: distinct oven/well/rack/sack silhouettes are useful; native geometry is being rebuilt at exact resolution rather than shrinking an irregular generated grid.
- Existing local Blender is the mesh source tool. No automatic image-to-3D service or online generator is being claimed.
- Unity opened with a recovered scene backup preserved in Assets/_Recovery. Expired cloud connector endpoints bypassed via the installed local MCP server; no new service/account required.
- New clearing Play-route regression: RED 1 failure of 6 (redirected to SampleScene), then GREEN all 6 passed after exact allowlist/menu entry. Receipts in `Docs/Verification/Clearing16x24`.
- 16-prop kit and Unity assembly under way. Existing five-sprite study and approved PNG bytes remain unchanged.
- Hourly heartbeat `overnight-clearing-art-pipeline` continues this same chat. Its original morning cutoff was later replaced by the finite live-opening completion criteria in the revised brief. Caffeinate has its own bounded timeout; inspect task-owned process state before renewing it when needed.

- Native kit completed: 16 props, 15 at16×24 and shelter48×48 (3×2 cells), editable voxel geometry plus exact Blender renders. Native binary alpha and palette verification passed. Two consecutive builds produce identical native PNG bytes.
- Actual clearing Play frame captured. Final layout contains56 environmental tiles and the5 unchanged approved characters/objects. Static props use10 Tilemap layers sharing one atlas; actors remain sprites.
- Optimization countercheck: 61 SpriteRenderers produced62 observed drawcalls; tilemap version produces9. The complete1280×800 v1/v3 captures are byte-equivalent in pixel content (zero differing pixels). This is an editor observation, not a Deck performance claim.
- A failed optimization pass saved empty tilemaps. Added RED test (expected56, actual0), resolved Tile owners after scene replacement and marked changed maps dirty, guarded each placement. Corrected Play capture retains all art.
- Art polish: tilled beds changed from a framed-box silhouette to irregular low earth/furrows; other15 prop pixel buffers unchanged.

First art milestone complete and reviewed. All **7 focused native Unity EditMode tests passed**, including existing scene routes and the saved tile regression. Final actual Game frame is **1280×800** at exact4× native pixel scale; every captured pixel matches the approved/source composite, with **zero differences**. All16 imports are16PPU, Point, uncompressed, no mipmaps/NPOT scaling. One128×128 packed texture is used; final observer reports9 draw calls and123 triangles. Original8 selected asset/scene files match pre-work hashes byte-for-byte. Menu switches to the blank study, original3D scene and clearing were exercised. Play stopped, temporary capture size removed, original Game view index0 restored; clearing is clean and open.

Next: follow the live presentation contract and complete-opening coverage in [the revised brief](LIVE-16X24-ART-DIRECTION.md). The crop audit below remains useful source evidence, but crop-only integration no longer satisfies the next milestone. The completed static art milestone is not the requested playable visual transformation.

## Review and evidence

- 🟡 Fixed: scene rebuild now rejects ANY dirty loaded scene, including inactive additive scenes, before authoring.
- 🟡 Fixed: builder saves only owned assets/scene, avoiding global SaveAssets and unrelated pending edits.
- 🟡 Fixed: required approved sprites/material preflight precedes asset writes; saved clearing is explicitly assigned as Play start scene after NewScene→SaveScene.
- 🟡 Fixed: native tilemap placement/save failure now guarded and regression tested.
- 🔵 Scope: this clearing remains an art composition. It does not merge Nella and Orven's distinct authored world sites or simulate invented dialogue/harvest actions.
- 🧪 Actual Game capture and pixel comparison prove scale, color, layout and successful scene reload. They do not prove live gameplay, Deck hardware performance or interaction reachability. No Steam Deck hardware is connected.

## Previous crop-only slice — superseded by the live opening brief

After the art milestone is pushed, prioritize one genuine connection to existing game systems over another decorative kit. Investigate the existing crop presentation path and use the new emberwheat/marlroot designs for seedling, growing and ripe states. Use imagegen as needed for coherent state references, retain editable sources and prove that visual states follow the real CropPart rather than a showcase timer. Preserve the original 3D route and avoid a parallel miniature game. Test the chosen renderer path and validate actual growth/harvest through ordinary input with NativeSaveIsolation when possible. If the broad rectangular-grid conversion is required, document the concrete dependency rather than presenting the static study as playable. Continue further only after this slice is tested and useful.

## Local tools for continuation

Local Unity MCP is running on http://localhost:8080/mcp; instance caves-of-ooo@4f5664cceff9ea5e. Installed CLI: `/Users/steven/unity-mcp/Server/.venv/bin/unity-mcp -f json`. Editor-only snippets: `code execute --file Tools/Clearing16x24/build_unity.cs`, then `configure_capture.cs`; restore with `restore_capture.cs` after Play. Server executable if needed: `/Users/steven/unity-mcp/Server/.venv/bin/mcp-for-unity --transport http --http-url http://localhost:8080`.

Caffeinate for this task was started as PID9300 with a bounded14-hour timeout; stop only that owned process at overnight completion if it is still the same caffeinate command. Do not kill unrelated power or editor processes. Preserve any user-created unsaved work.


## First milestone receipts

- [Final actual Unity Game screenshot](Verification/Clearing16x24/clearing-game-final.png)
- [Full-frame native pixel comparison](Verification/Clearing16x24/capture-verification.json)
- [Actual Play observer](Verification/Clearing16x24/clearing-live-final.json)
- [Seven passing Unity tests](Verification/Clearing16x24/final-tests.json)
- [Import audit](Verification/Clearing16x24/import-audit.json) and [preservation hashes](Verification/Clearing16x24/preservation.json)
- [Source/prompt/rebuild guide](../ArtSource/Clearing16x24/README.md)

Cold-eye review: no remaining significant findings in the shipped art slice. Exact old-scene/asset restoration paths and current scope were counter-checked. Two Metal memoryless-depth warnings appeared during captures; pixels were correct and no script compilation errors were observed. Editor Screen.width reported a window-sized value in the generic observer, so capture dimensions are established by the actual PNG and verified projection, not that editor callback value. No player build was produced and no physical Steam Deck performance claim is made.

### Crop integration sweep — use this before implementation

Read-only source audit found a false premise in treating both crops as three-stage plants:

| Owner | Actual states | Correct art use |
| --- | --- | --- |
| EmberwheatCrop | 0 seed; 1 sprout; 35 moisture units per stage; maturity removes plant and drops two Emberwheat | Author a small green growing stalk. The golden-head kit image is not a standing ripe CropPart state. |
| MarlrootCrop | 0 seed; 1 sprout; 2 ripe; 26 units per stage; ripe plant waits for Harvest/Gather | Current kit plant fits ripe stage 2; add seed/growing frames. |
| RipeCropRow | Separate FieldHarvestPart, with harvested/spent state | Golden-head kit image can fit the unharvested row, without changing EmberwheatCrop mechanics. |

Source references: `Assets/Scripts/Gameplay/Farming/CropPart.cs` and `CropSystem.AdvanceStage`; `Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs` (`ResolveCropKind`, `ChooseTile`, `PaintGroundUnderAscii`); `AnimatedEnvironmentRenderer.GrassGlyphs`; `SpreadEnvironmentRecipes.PlantedModel`; `BiomeCropRecipes.ResolveModel`. Verify current signatures before editing.

Smallest implementation: an exact-owner stage-aware sprite resolver before the existing generic flat fallback. Place frames in Resources with native Point/full-rect imports. Retain unknown/invalid/reskinned/foreign/hidden owner fallbacks. Mark authored palette color so glyph hue does not recolor the art. Dry/wet soil must be represented in the frame or narrow owned overlay: PaintGroundUnderAscii replaces the CropPart wet background. Marlroot's sprout glyph `;` also claims animated grass; prevent a duplicate underlay. Do not alter the already functioning 3D crop recipe paths or crop clocks.

Tests first: legal stage/wet-dry selection and negative owners; real growth dirty-cell update; dry pause; Emberwheat removal/produce at maturity; Marlroot persistence then real harvest; full/incremental render agreement; palette and grass suppression; unchanged unknown/RipeCropRow fallback. Reuse EnvironmentSpriteRendererHarnessTests, PlantedCropArtTests, BiomeCropArtTests, MundaneCropWateringArtTests and ConnectedCropTimeTests.

Native route: isolated seed-64 game, north from Overworld.11.10.0 through 11.9.0 to Nella's 11.8.0 plot; F11 chooses flat/original view. Public Emberwheat and permission-gated tied Marlroot beds provide actual owners. Use real watering and paid waits/harvest through ordinary input. Ten world ticks equal one growth unit; observe transitions rather than assuming one key press equals one stage unit. Never assign growth fields or inject gameplay events to manufacture evidence.
