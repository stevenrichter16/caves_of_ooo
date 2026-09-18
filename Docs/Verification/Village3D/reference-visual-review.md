# Village reference comparison — independent visual review

Inspected with view_image, 2026-09-09:

- `Docs/References/Village3D/reference.png` (1672×941, including black native UI)
- `ArtSource/Village3D/renders/village_overhead.png` (1200×800)
- `ArtSource/Village3D/renders/village_cutaway.png` (1200×800)

Also read the relevant geometry, placement, material and studio-light code in `ArtSource/Village3D/build_scene.py`, plus the reconciliation ledger generator and plan. No repository edits or Unity execution.

## Verdict

The current kit retains the recognizable five-building/well/two-stall arrangement and useful distinct native owners, but is **not yet a close visual match in finish**. It reads as a simplified, clean procedural village; the reference reads as a richly dressed, softly sculpted miniature. The high-impact difference is not camera tilt (both should remain straight down) or adding random detail everywhere. It is the forms and scale of stonework, ground transitions, foliage masses, cloth and local shading. A functional test pass is not visual acceptance.

Compare the reference's world rectangle with the 3:2 Blender render; do not compare against the reference's whole image including sidebar/hotbar. The central well is already a sensible fixed registration point. Native Unity material parity remains unreviewed by these Blender-only images.

## Ranked actionable differences

### 1. Replace the visible paving grid with irregular fitted cobbles — very high impact, bounded geometry change

Reference: close-packed, varied pebble shapes; broad size distribution; warmer beige stone; winding paths with soft, broken fringes and dirt/moss visible between groups. Current: nearly identical rounded square stones visibly march in rows, with large regular gaps; junctions and the well surround read as intersecting stamped bands. This is the strongest repeated pattern in the image.

Source cause: terrain-detail generation iterates a 29×15 grid per patch with small positional jitter, dimensions around .32×.28, and broadly uniform spacing. Random color/rotation cannot hide that underlying lattice.

Change: replace the lattice positions with a deterministic irregular packing distribution following the same existing path mask. Mix approximately three size classes and rounded oblong/pebble outlines; avoid gaps with a constant repeated width. Modulate the edge density and path widths smoothly in world coordinates; keep a worn-earth underlayer continuous across patches. Use a shared world-space candidate set/consistent neighbor rejection so patch boundaries do not introduce seams. Keep collision and native movement unchanged. Prefer replacing current stones at comparable total triangle count to stacking extra detail over them.

Acceptance: at final 1200×800 size no immediately visible horizontal/vertical paving grid; broad walkable branches remain readable; three path crops at patch boundaries stay continuous.

### 2. Make roof surfaces read as worn mossy masonry, not ordered green bricks — very high impact, bounded hero-asset change

Reference: uneven ochre/olive stone plates, substantial moss/earth joints, subtle local flowers, irregular rounded pale coping, and distinct roof subdivisions. Current: ordered long green brick rows, thin dark mortar grid, very bright uniformly sized parapet beads; the inn's reference-like internal division is especially weak. Chimneys appear as pristine pale torus rings.

Change: break longitudinal course continuity; vary stone heights/shapes more than the current ±.018 height and ±.085 rotation. Reintroduce roof-specific low raised divider/ridge patterns visible in the reference, keeping them part of the roof owner. Reduce uniform coping brightness; alternate pale warm stone with darker weathered/mossy edges and modest size variation. Use broad localized moss pools along selected joints instead of a tiny number of disconnected flat moss marks. Add a dark inner chimney throat and uneven rim, retaining the chimney's recognizable opening.

Do not turn roofs into peaked/sloping conventional roofs: the reference's top-facing forms are the target. Preserve exact shell/roof roots, room footprints, doors and cutaway independence.

Acceptance: recognizable five variants at gameplay scale, softer/less geometric coping rhythm, and readable divisions without new blocking or exaggerated roof bounds.

### 3. Restore the reference's large foliage and edge composition — high impact, moderate asset/dressing change

Reference: prominent dense shrubs/tree crowns with fine overlapping leaves, dark interiors and warm highlights; pink/yellow/white flowers form distinct clusters; irregular stones and plants frame the village. Current: smaller sparse crowns composed of conspicuous broad polygonal blobs, much open ground between props, and tiny evenly dispersed speckles rather than designed flower masses.

Change: enlarge selected peripheral crowns (individually, not every shrub) toward the reference's visual masses; use several overlapping asymmetric lobes, each with smaller elongated leaf clusters. Build 3–4 silhouettes with dark cores and light outer leaves. Concentrate flowers into a handful of deliberate border clusters, particularly pink groups near the lower-right fence and left side, with secondary white flowers around rocks. Preserve path clearance and do not cover native actors/owners. Replace some currently scattered micro-meshes with low-cost baked/vertex-painted ground detail to fund the larger silhouettes.

Do not add sheer object count as a substitute for composition. The source already estimates roughly 295k triangles in the conservative reference crop. Group cosmetic accents into bounded static decoration placements so low detail can actually remove the added cost. Keep water/ground/path meshes in their required terrain roles.

Acceptance: the village has a leafy frame and deliberate color accents at final size; foliage is varied rather than wallpaper; low detail retains terrain and native owners.

### 4. Improve local depth and material separation — high impact, cross-Blender/Unity validation required

Reference: rounded stone edges and well coping have clear highlights and crevice darkness; props sit firmly on the ground; short soft shadows create depth without flattening colors. Current: the broad earth/roof greens merge, stone is cool and rather flat, and foliage/props have weaker contact separation. Merely increasing exposure or saturation would not fix the shape hierarchy.

Change: first refine stone/wood/leaf palette separation (warmer pale limestone, darker moss recesses, warmer readable wood). Add mesh-local cavity/AO information or painted vertex modulation for static self-occlusion. Tune key/fill only after those changes, using the same camera: slightly more directional form light and restrained ambient fill, with short soft contact shadows. Preserve broad green surfaces rather than adding uniform high-frequency dirt everywhere.

Runtime caution: the current world shader samples the palette and explicit lighting; changing only Blender Cycles AO/studio light does not make Unity match. Any new vertex AO, normal or AO texture must be intentionally consumed and imported by the Unity shader. Do not bake movable roofs/doors/actors' shadows into the ground, or render remembered/hidden entities' live shadows. Keep the established fog/light contract intact.

Acceptance: actual Unity full-detail capture shows the same rounded depth and material hierarchy as the approved Blender revision. Blender-only improvement is insufficient.

### 5. Give canopies visible drape, edge detail and supported goods — medium/high impact, small hero-prop cost

Reference: teal/gold and mauve cloth sag and crease toward tied corners, have uneven borders/seams and visible rope/stitch highlights; wares and wood counter form dense readable silhouettes. Current: canopies look like flat two-color rectangles, despite an existing .26-unit sag mesh; produce baskets resemble uniform discs and repeated round dots.

Change: emphasize a few broad folds along the tension lines from posts, lower/uneven side hems, a seam and corner ties; use localized darker fold color and soft highlights that survive straight-down lighting. Keep two reference colorways and avoid glossy cloth. Vary basket mouth shapes, produce scale and groups, and add visible rim/woven band detail only at useful pixel sizes. Existing canopy/goods stay bound to their current stall owner.

Acceptance: each canopy reads as cloth at normal camera size before zooming; individual wares form distinct groups without pretending to be new pickup entities.

### 6. Improve the focal well and water — medium/high impact, small hero-asset change

Reference: irregular warm coping and deep inner wall, turquoise near-black water with broken soft ripple/caustic-like highlights. Current: near-perfect pale ring and a nearly featureless dark disc. The source already has glint geometry, but its .008 radius is around half a pixel in width at the preview's 32px/unit and is visually ineffective here.

Change: vary coping size/orientation/tint subtly, strengthen inner rim occlusion, and use a few broad broken water curves/highlights that survive the final raster size. Preserve opacity and visibility masks; do not add screen-space reflections/refraction that could leak hidden content. Keep the water below the coping, with selected small props providing context rather than cluttering the interaction edge.

Acceptance: the well remains the central focal point and reads as deep water, not a dark flat badge; its accessible rim and owner stay unchanged.

### 7. Thicken and articulate fences, gate and small timber props — medium impact, small asset pass

Reference: capped posts, collars/bindings, readable round wood grain and irregular rails; props have stronger handles, bands and slat highlights. Current: many fence runs read as one thin straight line with discs at the ends; the arrival gate is visually a plain bar. Top-down projection stacks vertical rails over each other, hiding much of the authored structure.

Change: make post caps/collars and top-facing grain deliberately readable; modestly thicken key posts and introduce gentle rail sag/breaks. Use slightly separated or angled rail shapes that create an informative overhead silhouette without changing the camera or implying new passage blockers. Add dominant crate diagonal braces, barrel hoops/endgrain and cart sidewalls before tiny ornament. Preserve separate fence variants and open native arrival/exit lanes.

Acceptance: main fence rhythm matches the reference's substantial boundary fragments, gate is an identifiable entrance, and no door/path loses clearance.

### 8. Refine top-down character silhouettes and roof-cutaway interiors — secondary to the environment, still visible

Reference: small round hooded figures with clear hood rim/face shadow, clothing value differences and grounded foot shadows. Current: figures are sparse angular cap/cloak blobs with little discernible face/hood depth. The cutaway has valid separate rooms/furnishings, but broad identical clean planks and boxy furniture make it look unfinished next to the reference's surface richness.

Change characters: smoother controlled hood profile, inset dark face opening/rim, a small shoulder/cloak split and boots with the same native pivot/rig. Keep teal/gold/purple colors; equipment attachments must remain readable rather than adding oversized generic weapons. Change interiors: vary plank tone/width and add restrained edge wear, hearth/bed/shelf visual cues, selected floor dressing that neither spawns loot nor hides the doorway. Do not overcrowd sparse native rooms to manufacture interactions.

The reference shows roofs, not its hidden interiors: cutaway polish is an authored extrapolation, not fidelity to unseen source detail. Review one room with roof removed while surrounding roofs stay on, in addition to the all-roofs-off diagnostic.

## Large composition deviations that should remain explicit

- **Western creek:** the current large dark vertical channel and bridge occupy a major visual band absent from the reference's evident western grass/path area. The ledger identifies all 67 native water cells as existing gameplay. Do not paint them as ordinary grass or move the bridge just to match the image. Reduce dominance through restrained dark-water color, irregular banks, vegetation framing and natural shoreline blending while maintaining obvious traversability. Exact visual reconstruction of that band would require an explicit native layout migration, beyond an art-only correction.
- **Building extents:** guesthouse and archive are visibly wider/larger and shifted relative to the generated roof silhouettes. The ledger shows their shells follow existing native rooms. Smaller cosmetic roof overhangs are possible only while the real shell remains completely covered and doors/cutaway align; shrinking full buildings or moving owner anchors to match pixels would misrepresent gameplay. Improve proportions of internal roof subdivisions and surrounding dressing within those bounds first.
- **UI:** black sidebar/hotbar belong to actual game UI, not Blender. Adding imitation UI to the beauty render would not validate runtime composition.

## Suggested implementation and comparison sequence

1. Refine one representative path patch, inn roof and central well. Render the same camera and inspect final-size crops, then port material needs to Unity and capture them there. This checks the new style before regenerating the full kit.
2. Roll the successful stone/roof changes across bounded variants; rebuild foliage silhouettes and placement masses; refine stalls and timber accents. Keep stable model IDs and native owners.
3. Compare full overhead composition, one native cutaway room, and normal gameplay FOV/lighting in Unity with UI at 1920×1080. Compare full and low detail; archive the previous renders and state actual differences/remaining native constraints.
4. Re-run asset validation/import identity and actual native profile after final art. Measure triangles/draws/target cost; do not describe the extra art as performance-neutral without evidence.

This review is visual/source-based. It does not certify shader output, image quality in Unity, actual frame rate, selection or native interaction correctness. No files in the repository were modified.
