# Felling south chunk: component and hidden-surface contract

This contract guides the new overhead settlement immediately south of the Felling, at world `(3,6)`, with a north connection to the existing Felling at `(3,5)`. It is a production plan for the new source package, not a claim that ungenerated art, NPC behavior, trade or Unity integration already exists. The settlement design document owns faction names, lore, inhabitants and shop contents; this document owns how those decisions become independent visual and mechanical parts.

## What the deliverable must contain

The intact chunk must be reconstructable from exported components and their backing. Opening a door, revealing a roof, collecting a loose object or moving a creature must expose a deliberately prepared surface. A transparent rectangle, an unchanged duplicate baked into the ground, and a crop that includes neighboring objects do not meet this contract.

The reference is an overhead composition. Measure its actual dimensions after generation and declare the image-to-grid mapping explicitly. Do not inherit the original Felling's `224`-pixel visual overhang, `[16,0]` region origin or `1536×1024` canvas just because a previous helper used those numbers. If the new source is `1536×1024` at `32` pixels per cell, its direct overhead authoring grid is `48×32`; native Unity's `80×25` zone requires a separate, reviewed mapping decision. Do not distort the art to make the two grids agree silently.

There are two kinds of fidelity:

1. **Visible source fidelity:** an unchanged component at its original placement uses exact source RGB, with explicit authored alpha. Full intact reassembly must match the approved baseline pixel for pixel.
2. **Exposed state fidelity:** hidden floor, building interiors and concealed object portions are newly authored reconstructions. Record the generation source, support mask, any deterministic palette correction and visual review. They cannot be described as recovered source pixels.

## Produced inventory

The initial 41-owner estimate was superseded by the inspected source. The finished package is **ArtSource/Morrowfast** and its manifest is the authority for exact geometry.

| Owner class | Delivered | Preview behavior |
| --- | ---: | --- |
| Outdoor props and inhabitants | 35 | Independent selection and removal/restoration; owned hidden ground and neighbour repairs |
| Interior furnishings | 21 | Revealed under five roofs; independent removal and collision |
| Building roofs | 5 | Lift/replacement exposes a furnished room |
| Original wooden doors | 5 | Open/close changes the threshold and walking channel |
| Fixed wall shells | 5 | Inspectable; retained masonry around reconstructed floors |
| **Total** | **71** | **145 cropped RGBA visual layers** |

The source contains six visible adults, two animals, two market stalls and an already-open entrance arch. It contains no hinged main-gate leaves. Five reconstructed rooms replace the initial four-room budget. A counter's display objects share its owner; named takeable inventory items must acquire individual owners during native content authoring. Permanent perimeter rocks, fences and tiny ground growth remain fixed texture.

## File contract

The delivered package uses the final settlement name:

```text
ArtSource/Morrowfast/
  README.md
  reference.png                  # immutable approved overview
  provenance.json                # prompts and generated-source hashes
  prompts/                       # eight exact Imagegen prompts
  sources/                       # retained generation/inference iterations
  authoring/                     # silhouettes, offsets, content cards
  build/
    manifest.json
    base.png
    reassembled.png
    interiors.png
    all-removed.png
    sprites/<owner-role>.png      # RGBA, including contact and inferred layers
    masks/<owner>.png
    review/components.png
    review/removal-sheet.png
    review/removals/<owner>.png
    reports/verification.json
  reports/                       # test logs, visual review, independent hashes
  index.html
  scene.js
  style.css
```

The actual schema records layer ownership, bounds, z order, provenance, room visibility, physical footprints, anchors and door/roof/bridge dependencies. PNG hashes are in `reports/asset-hashes.json`; authoritative masks and placement offsets are in `authoring/`. Source image generation is nondeterministic; the preserved outputs are the extraction inputs. Rebuilding the component package is deterministic from those files. The exact pre-polish working input of Imagegen pass08 is described in provenance rather than falsely advertised as a separately archived source.

The following is the **native integration metadata contract**. Fields such as Unity pivots, state-variant animation and freely movable runtime geometry remain native handoff work; do not treat a source-anchored authoring cutout as a fully animated Unity actor.

Every component record needs:

- A stable safe `id`, readable `name`, `kind`, semantic `ownerId` and `parentId` when it belongs to a building or composite prop.
- `bounds` as source-image top-left `[x,y,width,height]`; a source-space `foot`; a normalized bottom-left `pivot`; explicit deterministic `depth` or layer ordering.
- Exact local PNG paths and SHA-256 records for `sprite`, optional `contact`, removal/reveal `mask`, backing `surface` and variants. All paths remain confined to the package.
- A provenance class: `source-visible`, `inferred-hidden`, or `derived-existing-sprite`. A reconstruction must never acquire the `source-visible` label merely because it was later composited into the baseline.
- `defaultVisible`, `contributesToComposite`, `mutable`, and state-dependent `blocksMovement`; true multi-cell `footprintCells`, distinct from its art bounds.
- One or more explicit interaction approach cells or access edges. An anchor is insufficient for a long counter, multi-cell door or roof-covered room.
- A declared interaction list, allowed states, initial state, state-to-art mapping, and whether the preview action is a real semantic state change or an authoring-only hide/restore operation.
- `revealDependencies`: which lower surfaces must exist before the owner may be hidden or moved. Explicit coverage must include every exposed pixel.
- `movementPolicy`: `anchored`, `state-variant`, or `freely-movable`. Do not mark a visible-only cutout freely movable when it has clipped unseen geometry or a baked ground patch.

The grid record needs width, height, pixel mapping, spawn, terrain walkability, entrances, north-Felling connector, south/world connector, important waypoints and blocked footprints. Record a north entry that aligns with the Felling's physical approach, not merely its world-map coordinate. Preserve a two-cell-wide primary approach where possible; guards should stand beside a choke point rather than accidentally occupy its sole walkable cell.

## Extraction and reconstruction sequence

1. **Review the composition before tracing.** Check true overhead camera, clear building footprints, readable gate across the north approach, open south arrival, distinct shop access, small cast and adequate spacing. Keep the palette and materials compatible with the existing Felling. Reject a generation with merged inhabitants, objects cut by image edges, illegible structural joins or paths that lead into walls.
2. **Inventory every intended owner.** Trace source-space silhouette contours and exclusion contours at native size, then inspect at 2–4× nearest-neighbor zoom. Use rectangular bounds only to constrain a reviewed mask; a box is not an object silhouette. Any visible remainder that intentionally stays baked in must be classified as fixed scenery.
3. **Generate the complete supporting surfaces with Imagegen.** Request the same canvas, camera, roads, water boundaries, building outlines, lighting and palette. Remove people, animals and intended movable props; remove roofs separately to show plausible room floors and wall interiors. The ground plate and the roof-hidden interior plate solve different problems and should be reviewed separately.
4. **Limit inferred pixels to explicit reveal masks.** Preserve original pixels outside the support. For small objects, local palette correction and a narrow feather inside the support can reconcile generated backing. Large roofs need complete reviewed room surfaces, not an automatic five-pixel repair assumption. Reject hidden surfaces containing copies or shadows of the removed object.
5. **Extract subject, contact and state art separately.** Subject pixels preserve source RGB. Ground contact/shadow stays attached to its original surface. A moved object receives a separately generated or procedural runtime contact later, instead of dragging the original stone/grass pixels. Open door leaves must rotate/translate through an authored pivot or use a reviewed variant without carrying threshold pixels.
6. **Build a dependency-aware composite.** Ground → floors → wall bases/interior fixtures → people/props → upper walls/roofs → effects is only a starting order. Source overlaps must decide exact order, and native actor sorting must have compatible foot anchors. Generate dependency layers for interiors obscured by roofs and for props obscured by other props.
7. **Export cropped PNGs and a manifest.** Include one transparent guard pixel where possible, unclamped valid pivots, stable IDs, bounds, hashes and independent collision footprints. Atlas packing must copy pixels without resizing, quantization or trimming anchor space. Store both top-left and Unity bottom-left atlas rectangles.
8. **Build the authoring preview.** It must walk the chunk, use alpha-aware picking, identify overlap owners, reveal roofs, change door/gate states, invoke the declared scene interactions, hide/restore safe components and show inferred support. A separate inspection mode may show diagnostic partitions; it must not make opaque buildings traversable just because the artist hid a roof.
9. **Verify exported files independently.** Read the written PNGs afresh, assemble them according to the manifest and test both raster identity and allowed state transitions. Record per-owner removal/reveal crops and review the all-removed and all-roofs-hidden views. Fix actual masks/backing, not the acceptance thresholds.

## What can be reused

| Existing implementation | Useful API or pattern | Caveat for this chunk |
| --- | --- | --- |
| `ArtTools/felling_components.py` | `rgba_cutout(rgb, mask)`, `polygon_mask(size, points)`, `bounds_with_anchor(mask, foot)` | These are broadly reusable. `polygon_mask` also validates polygons through `felling_scene.validate_polygon`. |
| Same file | `make_mutable_layers(source, backing, masks, protected=None, barriers=None)` | Requires matching RGB canvases, nonempty disjoint subject masks and no subject/protected overlap. Gives each contextual pixel one owner by nearest subject/stable ID within five pixels. Useful for small props; insufficient for large building reconstruction without explicit support changes. |
| Same file | `compose(base, layers, removed=())` | Exact opaque source restoration with contact then subject. Dictionary ordering is stable ID order; it is not a complete roof/state/depth renderer. |
| Same file | Source-vs-removal comparison strips, independent cropped-file reconstruction, exact atlas crop checks | Reuse the methodology. Its `build()` hardcodes the old package, actor-clean patch and Felling coordinate mapping. Do not call it on the new package unchanged. |
| `ArtTools/catacomb_extract.py` | `validate_authoring(authoring,size)`, `extract_sprite(source,entry)` | For genuine RGBA source sheets only. It normalizes alpha at a threshold and copies visible RGB exactly. It cannot discover silhouettes in an opaque overhead painting; do not treat opaque scene alpha as an object mask. |
| `ArtTools/catacomb_scene.py` | `compose`, `blocked_cells`, `reachable`, `atlas_export` | Good for assembling separate assets over a complete substrate. Its default package/source are hardcoded, and the atlas width is 1024. Reachability permits diagonals only when both side cells are clear. |
| `ArtSource/CatacombVillage/scene.js` | Pure exports: `validateScene`, `createState`, `occupancy`, `isWalkable`, `neighbors`, `findPath`, `reduceState`, `hitTest`, `comparePixels`; key input and interpolation helpers | Best existing movement/inspection preview foundation. Its reducer implements generic removal/restoration, not trade, dialogue or building state machines. Extend with truthful semantic actions; do not relabel a hide button as working commerce. |
| `ArtSource/FellingSite/Components/viewer.js` | `hitTestComponents`, `compareSpriteToBaseline`, `intersectInferenceWithAlpha`, exploded/solo/inference inspection | Useful focused inspection functions. This viewer intentionally contains no walking gameplay. |
| `ArtTools/verify_catacomb_scene.py` | Confined paths, strict PNG checks, independent source derivation, navigation and assembly checks | More parameterized than the original Felling validator, but has package-specific catalogs and mutable-mask assumptions. State variants and roof support need additional gates. |
| `ArtTools/felling_components_contract.py` | Fail-closed full-request validation and immutable `remove_components` / `restore_components` pattern | Its schema hardcodes 1536×1024, 32 pixels per cell, original origins and cell ranges. Generalize through a new contract rather than breaking the existing Felling tests. |

## Acceptance gates

**Source and export integrity**

- All declared files exist as valid PNGs with the declared dimensions and hashes. RGBA cutouts contain actual transparent pixels and opaque subject pixels; no checkerboard baked into RGB.
- Safe unique IDs, finite coordinates, simple nondegenerate polygons, valid bounds, normalized pivots and package-confined paths. Reject duplicate JSON keys, stale source hashes and out-of-range footprints.
- Exact intact reassembly from exported files: zero changed RGBA pixels versus the approved baseline. Report separately any deliberate difference between baseline and untouched Imagegen reference.
- Every pixel labeled source-visible matches its original source coordinate. A nearest-neighbor resized existing asset is a documented derivative, not exact native extraction.
- Repacked atlas crops equal individual PNGs exactly. No scale changes, partial alpha caused by blending, or lost pivot padding.

**Hidden surfaces and combinations**

- Removing each mutable object changes zero pixels outside its declared reveal support and zero protected landmark pixels.
- All mutable objects removed leaves an opaque, coherent backing. All roofs revealed leaves complete rooms, wall joins and thresholds; no roof texture remnants, halos, double shadows or ghost inhabitants.
- Review every overlapping pair in both removal orders; contact ownership cannot erase a neighbor. Exercise representative combinations: door open plus roof hidden, stock consumed plus stall present, creature moved plus nearby prop removed, gate open plus guard present.
- Restoring an occupied blocking footprint is rejected atomically. Restoring a roof does not alter movement occupancy. Door/gate collision changes only when their state changes successfully.
- A roof cannot be revealed until all required interior surfaces exist. No state may expose unprepared transparent areas. Unreconstructed fixed structures remain explicitly anchored/inspectable.

**Interaction and navigation preview**

- The south arrival can reach every shop's access side, each residence door, every named NPC approach and the north gate approach. Test the initial gate-closed state and the authorized gate-open state separately; reaching the Felling through a locked gate is not required before authorization.
- Collision uses terrain plus all active blocking owners, including creatures. No diagonal corner cutting and no route through a closed door, wall, deep water or a creature-occupied one-cell passage.
- Alpha picking ignores transparent sprite areas, honors depth and presents all valid overlapping owners. Source imagery of a stockpile must not swallow its nearby merchant's hit target.
- Each visible intended owner has a named, meaningful action. Dialogue/trade/service actions must show their current implementation status. Review-only remove/restore stays labeled as authoring functionality.
- Keyboard, mouse movement, selection, reset, focus loss and held-key release are tested. Inspect/solo/exploded views cannot continue a hidden movement route.
- A current browser smoke report records actual loaded asset counts, missing/failed asset requests, console errors and demonstrated interactions. It is separate from PNG tests and does not claim native Unity verification.

**Visual review at normal play scale**

- Overhead objects share a camera, scale, light direction and contact treatment. Reused oblique sprites that look tilted against the new camera are replaced or deliberately excluded.
- Entrance, shops and dwelling doors read clearly without marker clutter. NPCs and creatures remain distinguishable against the ground; walkable lanes are wide enough to understand.
- Roof-hidden and collected-object states look intentional, not merely technically opaque. Inspect at both native pixels and the expected in-game viewport. Retain screenshots for intact, roof-hidden, gate-open and all-removable-hidden views.

## Scope boundary for native integration

An interactable authoring package establishes component ownership, supported visual states, dialogue/stock definitions and reversible preview behavior. It does not establish native faction reputation, vendor economy, AI scheduling, path transitions or persistence by itself. When this chunk is integrated in Unity, map these owners to native entity parts and test the real player route from `(3,5)` south to `(3,6)`, the guard's entrance policy, shops, saving/reloading, reentry, actor depth/FOV and runtime performance. Retain the existing Felling and its saves; do not replace its source manifest or special-case its world location to add the new settlement.
