# Felling-Site component and removal contract

Status: historical design contract and continuing runtime acceptance requirements. The offline masks, local backing, RGBA component compositor, and viewer have since been implemented; see the current [package README](README.md) and [version 1 manifest](build/manifest.json). The recommended schema below is a broader production model, not the exact field layout of that shipped offline manifest. Complete unseen surfaces, gameplay state bindings, physics/visibility ownership, persistence, and Unity validation remain separate work.

## 1. What the package must prove

An intact scene must reconstruct its designated source composition. Removing an interactable must remove its actual image contribution, its owned shadow/effect contributions, and its gameplay presence, exposing coherent art that was previously hidden. The object cannot remain baked into an underlying plate.

These are two independent acceptance gates. A reference image with duplicate cutout sprites on top can pass an intact screenshot comparison while failing removal completely. A convincing empty patch can pass a removal screenshot while shifting the intact source composition. Both states need evidence.

The selected original reference and the decomposition target must each have an explicit path, dimensions, and SHA-256. If the target is the approved actor-free plate, exact reconstruction means exact reconstruction of that plate. It does not mean that Imagegen preserved every original pixel. Report original-reference differences separately. The player's live sprite must never be duplicated in the background.

## 2. Shared scene space

Reuse the existing `../layout.json` transform, never an inferred image scale:

- Source canvas: 1536×1024, integer source samples, origin upper left.
- 32 art pixels per logical cell. Embedded region: zone x 16..63, y 0..24.
- Image y 0..223 is visual overhang; it creates no additional walkable cells.
- Image sample to cell: `x = 16 + floor(px/32)`, `y = floor((py-224)/32)` for x[0,1536), y[224,1024).
- Source edge vertex to Unity world: `(16 + px/32, 32 - py/32)`. Vertex coordinates can include x 1536/y 1024; sample coordinates use half-open bounds.
- Cell footprint, render extent, sprite pivot, and foot/depth anchor are different data. A tall root can cover pixels many cells above its physical base.

`Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs`, at `RenderCellCore`, inverts zone Y as `Zone.Height-1-y`. `AnimatedEntityRenderer.cs:497` anchors actor feet at `(x+.5,24-zoneY)`, not at the logical cell center. `Assets/Scripts/Gameplay/World/Map/Zone.cs:13` fixes the 80×25 zone.

## 3. Recommended manifest model

Keep reusable image assets separate from placed instances. Stable IDs describe the authored identity, never list index, extraction bounding box, or Unity instance ID. A future crop refinement must not reset an object's saved state.

### Scene record

| Field | Required meaning |
|---|---|
| `schemaVersion`, `sceneId` | Versioned package contract and stable scene ID. Reject unsupported versions. |
| `sourceReference`, `decompositionTarget` | Separate records containing path, SHA-256, width, height, and provenance/status. These identify what is being matched. |
| `coordinateSystem` | Canvas and the fixed transform above, or an exact validated reference to layout version/hash. |
| `assets` | Textures, masks, alternate states, and generated reconstruction sources with dimensions/hashes. |
| `instances` | Placed component instances, each with stable ID, geometry, visibility ownership, state policy, and composition relationships. |
| `baseLayers` | Immutable terrain/backdrop contributions. Any present-day interactable still baked here must be declared as unresolved and blocks claiming its removal works. |
| `landmarks` | References to the existing six `strike-*` IDs and `seventh-absence`; do not create a seventh soil patch. |
| `composition` | Color/alpha convention, deterministic order, intact state, target hash, and comparison rules. |
| `supportedStateSets` | Combinations whose reveal art and overlaps are complete. Explicitly identify any prototype-only limitations. Production interactions must not create unsupported combinations. |
| `validation` | Machine reports plus visual review status; never use a single `ready:true` for partly reviewed work. |

### Asset record

| Field | Required meaning |
|---|---|
| `assetId`, `path`, `sha256` | Stable asset identity, package-relative path, exact bytes. Reject missing files, path traversal, case mismatches, and stale hashes. |
| `kind` | `color`, `coverage-mask`, `ownership-mask`, or another explicitly implemented format. A mask must not silently import as sRGB color artwork. |
| `width`, `height`, `format` | Actual pixel size and storage format, validated from the file. |
| `provenance` | `exact-source-extraction`, `generated-hidden-reconstruction`, or `mixed`, with source references and an authored/reconstructed-region mask where mixed. |
| `alphaConvention` | For color, explicitly straight RGBA or premultiplied RGBA. For masks, encoding and allowed values. No filename-based inference. |
| `approvalStatus` | For example `pending`, `study-reviewed`, `reconstruction-verified`. Human review and automated image checks remain distinct. |

### Component instance record

| Field | Required meaning |
|---|---|
| `instanceId` | Stable placed identity, for example `felling.prop.rock.southwest.01`. Reuse an asset through separate instances, not a shared mutable state. |
| `type` | An explicit classification such as `immutable-backdrop`, `removable-prop`, `removable-vegetation`, `water-surface`, `waterfall`, `ambient-effect`, `landmark`, `actor-proxy`. Type alone does not grant an interaction. |
| `assetId`, `sourceRect` | Visible art source and target-canvas `[left,top,width,height]`. Rectangles are integer, nonempty, in canvas bounds. The color/mask crop dimensions must match unless an explicit asset-atlas rectangle is supplied. |
| `coverageMaskAssetId`, `maskSpace` | Actual object silhouette/coverage in `crop` or `canvas` space. A bounding rectangle is not an object mask. Support outside the declared rectangle is an error. |
| `pivotPixels`, `imageAnchor` | Local pivot measured from the crop's upper left and its absolute source-canvas placement. For a source-aligned crop, anchor must equal sourceRect origin plus pivot. No implicit centering. |
| `footAnchorImage` | Absolute authored foot/depth anchor used for actor occlusion. Never inferred from the center or bottom of a loose rectangle. |
| `footprintCells` | Explicit occupied/owning logical cells. These are not automatically all cells touched by visible pixels. Immutable backdrop can have zero collision cells but still requires visibility owners. |
| `visibilityPatches` | Source coverage regions or an ownership texture, each mapped to exactly one owning footprint cell. Overhang requires explicitly assigned owners; no nearest-visible-cell fallback. |
| `drawBand`, `depthAnchor`, `drawAfter`, `drawBefore` | Stable visual ordering constraints. The order is deterministic and acyclic; contradictory constraints fail validation. Actor interleaving is a runtime contract, not an arbitrary high sprite order. |
| `initialState`, `allowedStates`, `transitionPolicy` | Defaults and allowed transitions. Usually `present`/`removed` for a removable instance. An immutable landmark/backdrop has no removal transition. |
| `gameplayBinding` | Intended stable entity/feature key, actual blueprint or adapter status, collision cells, opacity cells, and interaction policy. Unknown blueprint/binding remains unresolved. |
| `ownedContributions` | Shadow, splash, rustle, light, or decal IDs whose state follows this instance, with any explicit exceptions. Owner removal cannot leave a detached shadow or active loop. |
| `reveal` | Compatible base/underlay layers exposed on removal, mask/coverage, dependencies, and state prerequisites. Missing reveal content marks the interaction incomplete. |
| `dependencies` | Separate asset availability, visual occlusion, physical support, and state-variant prerequisites. Do not make an object disappear merely because it used to be visually behind another object. |

The existing `Integration/Core/FellingSceneCore.cs` already supplies fixed transforms, logical footprint validation, per-sample visibility, and invalidation of every footprint cell belonging to directly affected scenery. It does not parse this proposed schema, validate raster silhouettes, create ownership textures, solve alpha matting, or render state changes.

## 4. Masks, transparency, and exact reconstruction

Store color and coverage explicitly. For pixel-faithful opaque cutouts, a 0/255 selection mask can assign each currently visible source pixel to the corresponding component with its exact original color. The mask must follow the object, including holes between branches and around leaves. A rectangle containing the object plus old background does not constitute a transparent cutout.

Soft edges need more care. Taking an already composited source pixel and using it as semi-transparent foreground over a different base will change that pixel. Under source-over composition, `C = alpha*F + (1-alpha)*B`; the source screenshot supplies `C`, not necessarily `F`. Either solve/author the foreground consistently, use a declared source-pixel ownership method, or mark the boundary as reconstructed and measure the error. Do not claim an exact extract solely because its RGB values came from the reference.

Use one declared color/alpha pipeline for offline reconstruction. The final Unity capture also needs comparison under its actual sRGB/linear, shader, tint, and sampling behavior. A passing offline byte comparison cannot prove that Unity's material will reproduce it.

Shadows are owned contributions, even if they are stored in a separate layer. A shadow patch that also contains baked portions of another removable object creates a state dependency. Resolve it through correct shadow/foreground separation or an explicitly validated state variant. Leaving a dark silhouette after the object disappears is a failed removal.

For every sample, distinguish three concepts:

1. Pixel visibility/coverage in the current composition.
2. Physical ownership and visibility-cell ownership.
3. The original or generated art that will be exposed after an occluder is removed.

They are not interchangeable. Hidden reconstruction may extend behind another object without being visible in the intact scene. Its visible-source part must remain aligned and preserved.

## 5. Reveal art and overlapping objects

The preferred compositing model is a coherent base with **all supported removable objects and their shadows removed**, plus complete independently stateful objects above it. Complete means that any part exposed by removal of an overlapping object has valid art, even though the reference never showed that part.

Where a fully clean base or complete object is unavailable, local conditional reveal variants are acceptable only if their prerequisites are explicit and all supported combinations are tested. Example: removing rock A exposes fern B behind it; a reveal patch containing B is valid only while B is present. Removing both A and B must choose a fern-free reveal. Replaying A's old patch after B is removed would resurrect B visually.

Render the scene from the current state graph; do not progressively paste rectangular repairs into a mutable background. Sequential patch painting makes A-then-B and B-then-A produce different results, leaves stale shadows, and loses the source required for save/load reconstruction.

A local ground patch is sufficient only when:

- Its visible support is confined to the removed object's reveal/shadow region.
- It contains no pixels of any independently removable or moving object.
- The exposed material genuinely is ground: not the hidden trunk, another rock, water, or a background plant.
- Its seams, perspective, light, and palette remain coherent at native and reduced size.
- Every overlapping removal state is compatible, or the supported states are explicitly restricted during a labeled prototype.

A rectangle alone establishes none of these. Root removal may require a large behind-root reconstruction, exposed water/bank boundaries, new actor occlusion, and changed collision/FOV. It should remain disabled until that full contract is satisfied. The monumental petrified trunk should be immutable in this scene pass; current `TepuiWall` inherits destructible `Wall`, so visual immutability must have a corresponding simulation definition rather than relying on a sprite remaining on screen.

## 6. Runtime state and removal transaction

Persist stable instance state separately from immutable art definitions. Minimum record: `sceneId`, `manifestVersion` or compatible content revision, and a map of `instanceId -> present/removed`. Resolve live entity IDs through the simulation's existing persistence rules; never save a Unity object pointer or renderer ID. Unknown states/IDs require an explicit migration or diagnostic, not automatic respawning.

Before a removal can be enabled, verify that the actual game action supports it, the object is currently present, all required reveal states/assets are ready, and the simulation transaction can commit. The renderer observes successful state changes; an animation or texture hide is not gameplay removal.

On committed removal:

1. Capture old footprint and visual bounds, including shadows/effects and overhang.
2. Apply the simulation's removal result and stable state. If simulation rejects the action, leave the art present.
3. Disable every owned present-state contribution; select compatible reveal/base variants from the **complete current state**.
4. Rebuild affected collision/opacity according to the actual game entity/terrain change, recompute FOV when relevant, and invalidate the entire old/new owner footprint plus dependent patches.
5. Redraw the affected region from state. Refresh ordinary ground independently of the scenery owner's invalidation list.
6. On re-entry/load, rebuild the same result from saved state and the immutable manifest. Repeated removal of an already removed instance is an idempotent no-op.

Two removals with no declared semantic ordering must commute: the final rendered image and gameplay state are the same for A-then-B and B-then-A. Visual occlusion alone is not a semantic ordering. If an object physically supports another, define the actual result through gameplay rather than silently copying parent state to the dependent object's presence.

State restoration can be useful in the offline comparison viewer or tests. It does not automatically grant a resurrection/replant action in the game.

## 7. Game and lore constraints

- The six positions remain barren. `Lore/History/02_Geography.md:89` explicitly says no plant grows there; neither a reveal underlay nor a vegetation state may fill them with plants. Use the existing six stable layout IDs and geometry, not newly generated circles.
- The seventh is one point of wrong air (`02_Geography.md:89`, `:99`). It is not a removable creature, seventh soil patch, portal, or named object invented by the extraction process. `Lore/11_SecondSpine.md:20` and `Lore/10_Bible.md:109` establish pressure without agency; animation/state labels must not attach desires or actions to Urqu.
- Current era is approximately 1,080 years after the Felling (`Lore/10_Bible.md:116`). The fossil-grain reference is present-day petrified anatomy. Generated hidden areas must preserve that material, not introduce masonry, temple carvings, or a living wooden tree.
- `Cell.BlocksMovement` uses Solid/Physics.Solid (`Assets/Scripts/Gameplay/World/Map/Cell.cs:119`); `Cell.IsWall` uses the Wall tag (`:135`). Collision and sight blocking require distinct authored intent. A texture mask is not a physics mask.
- Current environment sprites occupy order 3, actors shadow/body 5/6, cursor 7, FX8, and tile state 4 (the `Stable order` comment in `ZoneRenderer.cs`). Full-size root cutouts require depth-aware patches; placing every component at a higher order would hide actors and gameplay feedback.
- Existing sprite import rules force 16 PPU below `Assets/Resources/Sprites/` (`Assets/Editor/SpriteImportPostprocessor.cs:26`, `:34`). Future 32 PPU component art belongs in the separate `Assets/Resources/SceneArt/FellingSite/Art/` policy described in `../Integration/UNITY-INTEGRATION.md`. Ownership masks require their own data-texture policy.
- A single giant image cannot bypass cell visibility. `EnvironmentSpriteRenderer.ResolveCell` respects unexplored/remembered cells; owned component samples, including overhang, must obey corresponding owners. State removal must invalidate complete multi-cell coverage, not only the clicked cell.

## 8. Failure-focused acceptance tests

| Area | Positive case | Failure that must be caught |
|---|---|---|
| Identity/schema | Stable instance survives manifest reorder/crop revision and reload. | Duplicate IDs, unsupported schema, unknown saved state, recycled ID for a different object. |
| Files/provenance | All color/mask files match declared size and hash; generated hidden areas identified. | Missing/stale file, atlas crop mistaken for full canvas, generated art described as exact extraction. |
| Placement | Sprite and mask align at original source position with correct pivot. | Off-by-one crop, inverted Y, pivot applied twice,32/16 PPU mismatch, actor center substituted for feet. |
| Mask validity | Transparent holes and silhouette follow the extracted object. | Opaque crop rectangle; checkerboard background baked as RGB; mask support outside bounds; wrong mask space. |
| Intact composition | All present-state components reconstruct the declared target and source landmarks. | Shifted rocks/soil circles, missing shadows, double alpha, halo seams, duplicated player, color-space mismatch. |
| Exact claim | Exact-source pixels match byte-for-byte under declared offline composition; any reconstructed boundary error is reported. | Passing only a loose global similarity score while object edges or six marks drift. |
| Removal authenticity | Removing a currently visible object removes its contribution and exposes approved, ghost-free content. | Original remains in base; only a duplicate top sprite is hidden; detached shadow; arbitrary grass rectangle. |
| Outside-region stability | Unrelated pixels remain unchanged after a local removal. | Repair patch wipes adjacent rocks/plants, overwrites water, changes a barren position, or shifts scene lighting. |
| Overlap combinations | A removed/B present, A present/B removed, both removed, and both present are all coherent for overlapping A/B. | Reveal patch resurrects B; two cleanups overwrite one another; incomplete hidden side of a rock. |
| Order independence | A-then-B equals B-then-A for independent objects; load matches both. | Destructive progressive paint operations or hidden state depending on click order. |
| State/game agreement | Simulation removal succeeds before visual state changes; saved removal survives re-entry. | Visible object gone while collision remains, invisible entity still interactable, object silently respawns. |
| Ownership invalidation | Old/new full footprint, overhang, and owned effects refresh. | Only clicked cell changes; stale root fringe or active water/light loop remains. |
| Visibility/depth | Hidden samples remain hidden; visible actor crosses behind/in front of correct root edges. | Any-visible footprint reveals whole sprite; canopy depth hides cursor or foreground actor. |
| Unsupported operation | Immutable trunk/landmarks cannot be removed; incomplete reveal is labeled and unavailable in prototype. | Gameplay exposes unprepared voids or silently treats every inventory label as a removable object. |
| Lore | Six bare positions persist through every supported state; seventh remains one subtle discontinuity. | Ground repair grows plants in six; seventh becomes an actor, portal, or additional circle. |

Evidence for each extractable/removable object should include its masked cutout, intact crop, removed crop, reveal source/provenance, and status. Compare native 1536×1024 plus a smaller gameplay viewport. Freeze actors, animation frames, FOV, camera, and lighting for the offline intact/removal comparisons. Include the declared shadow/effect influence bounds in the permitted change region. Use numerical image difference to locate errors, then inspect actual silhouettes/materials; numerical metrics alone cannot identify semantic ghosts. A fully occluded object may correctly cause no immediate visible change when removed; test its absence again after removing the foreground occluder.

Test all pairwise state combinations for overlapping components, and every multi-object combination within any small shared reveal group. If a larger overlap group cannot be represented without exponential variants, complete the clean base and hidden object art instead of implying arbitrary removal is supported.

## 9. Completion boundary

A faithful interactable component package needs exact placement/masks, reconstructed hidden surfaces, coherent owned shadows, a compositing order, defined state combinations, simulation bindings, full invalidation, persistence, and successful intact/removal comparisons. Rectangular crops plus a ground texture provide asset studies, not that package.

The offline package now includes an extraction inventory, shared manifest, actual masks, local inferred backing, a state compositor, and image comparisons. Their current scope and evidence are recorded in the [package README](README.md). Complete reveal geometry and the full runtime behavior described here remain integration requirements; an offline asset review does not satisfy the Unity acceptance gates.
