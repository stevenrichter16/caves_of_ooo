# Independent first Sodden whole-view review

2026-10-27. Read-only visual/source review. Inspected the actual1280×800 captures and matching observational paint receipts:

- `Docs/Verification/Live16x24/sodden-after-03-east-board.png/.json`
- `Docs/Verification/Live16x24/sodden-after-10-shelter-entry.png/.json`
- `Docs/Verification/Live16x24/sodden-after-12-shelter-room-matched.png/.json`
- New source study `ArtSource/Live16x24/sodden-travel/contact.png` and its frozen handoff (28-frame quiet-mire revision).

No Unity, Git, source/art edits, gameplay run or newly generated evidence. The three screenshots show the **earlier first integration**, not the quiet-mire/backing corrections now being implemented. The contact sheet is source art only. This review does not accept the district, a whole chunk, all Sodden or packages.

## Overall judgment

The material pass makes the existing route and shelter architecture much easier to read, but the screenshots still look like a partially converted game. The greatest remaining purpose/readability gap is the **real interactive people, service objects, signs and crops staying tiny glyphs inside otherwise native scenes**. The shelter room especially reads as a largely empty stone rectangle because the two owners that explain its purpose are visually absent at a glance. Fix those owners before adding more furniture or plants.

The east-board view has a clear raised route, wall-like peat cuts, one dead-tree silhouette and shoreline reed clusters. That is useful existing composition. The large remembered/unseen areas are not evidence of bare generation. The new quiet-mire source looks materially more coherent than the bright repeated pool marks in the gameplay captures, but only a new actual scene can establish that it works across hundreds of wet cells.

## Findings in priority order

### 1. Current material blockers — root already owns both fixes

- **Bright repeated mire motif:** screenshot03 repeats gold sprig-like marks across363 selected mire cells; screenshot10 repeats them across281. They compete with route/actor visibility and can read as a crop carpet rather than a continuous wet surface. This is a material/tiling defect, not a shortage of objects. The quiet olive/brown28-frame contact removes the strongest bright-grid motif and is suitable for an actual trial. Do not claim it fixed from source alone.
- **Black backing around native objects:** black rectangular cells are visibly distinct beneath reeds, dead tree and short isolated board pieces in the first integration. Root is correcting actual Grass-backed cases. Preserve real cultivated soil/water/hazard priority; do not fix all black squares by indiscriminately painting Grass beneath everything.
- Recheck both together. Quieting the mire changes which ground patterns dominate; repaired backing changes the perceived spacing of stands and routes.

### 2. Service-room purpose is missing from the image — biggest next static family

In screenshot12, the native stone envelope and subdued floor are coherent, but the two small marks above the player do not communicate a working dressing shelter. The receipt identifies **SoddenDressingBench1 and PeatCutter1 as glyph fallback**, with70 selected StoneFloor cells and39 StoneWall cells. The bench occupies34,6 and cutter35,6 in the actual district plan (`SoddenDistrictPlan.cs:43,47`). The present empty appearance is therefore partly a missing-owner-art problem, not proof the room needs more generated objects.

Recommended static silhouettes: an immediately legible low wooden workbench with dressing materials implied within the owner's single16×24 cell, and a recognizable peat-cutter person using the established character scale/palette. Do not add unattached tables, chairs, baskets or service decorations from a concept image. Keep current collision, service and conversation ownership exact. The bench blueprint is a real solid Wood physical object (`Objects.json:1771-1818`); it must not resemble a floor stripe or be confused with Duckboard.

Only after those two owners render properly should the actual room be judged for remaining excess empty floor. This is a real candidate for later layout refinement because the source room contains only those two service owners, but it is **not yet an accepted generation defect**. Preserve the entrance route and player interaction space.

### 3. Exterior landmarks and cultivation remain code-like glyphs

- Screenshot10 has a barely legible `I` at the left approach. Receipt: **SoddenRouteNotice1 fallback** at24,11. A readable wooden notice silhouette would make the existing entrance meaningful without adding new scenery. The source describes a solid, non-takeable route notice (`Objects.json:1992-2035`); do not replace it with a directional arrow whose direction is not actually supplied by state.
- The two green/dark crop glyphs to the right are **SumpsieveCrop** at47,9 and50,9. Receipt confirms stages2 and0, both moisture0. Draw actual growth states, not two copies of a mature plant. Stage0 still needs to read as a cultivated bed/young crop rather than a black punctuation cell. Soil remains a real separate state/owner relationship. Stage1 and harvested/absent behavior require source verification before production is called complete; this screenshot proves only stages0 and2 are currently visible.
- Screenshot03's upper-left pair remain **SoddenPassageGuard1 and SoddenPassagePost1 fallback** according to the matching receipt. The log identifies a cutbank watcher. A native guard/post should explain this existing gameplay constraint in the same visual vocabulary as the player and notices; do not turn the guard into an inert decorative figure. The screenshot is not evidence of its entire encounter state machine.

These six exact observed families—bench, cutter, route notice, crop, passage guard and passage post—are the strongest bounded follow-on for this circuit. The two sign identities may share visual source structure only where their real semantics fit; keep their runtime identity distinct. This is not a request to polish every district item before the worldwide queue resumes.

### 4. Ground has a regular speckle pattern, not regional clusters

Across screenshot03's right half and screenshot10's central clearing, tiny yellow/green marks recur at almost every cell on saturated dark green. The repeated grid is visible at normal1× even though the variation is subtle within each tile. The receipt shows the base Grass frame20 dominates496 of662 native grass submissions at03 and437 of574 at10, consistent with the regular appearance. This is not a frame-count acceptance claim.

This becomes more important once mire is quiet: the new source water may read calm while the dry bank still looks like a dotted carpet. Treat this as a named **scene-scale repetition** observation, not permission to repaint all accepted Grass art. First inspect the corrected gameplay. If it persists, prefer a bounded Sodden ground-selection/composition solution using existing quiet pixels or a separately justified regional material variant, preserving ordinary Grass ownership. Never add colliding reeds, rocks, harvestables or hazards just to break a rendering pattern. Preserve the clear travel lane and recognizable shoreline clusters.

### 5. Some shapes are utilitarian; defer changes until the larger gaps close

The horizontal board route in03 is a thin, regular strip and the corners in10 are very square. At1× it reads as a constructed line, which is useful, but is less clearly a walkable plank surface than the enlarged contact. This is a lower-priority1×/2×comparison item after the backing fix, not a reason to reopen the accepted board pixels now. Its geometry follows actual route owners. Likewise, the rectangular pool edges and block-shaped masonry follow real generated geometry; fog boundaries must never be mistaken for intended shoreline or room outlines.

Peat rows and the gray dead-tree silhouette already provide material differences. Avoid increasing contrast everywhere to compensate for tiny fallback service glyphs; that would erase the calm ground/important subject hierarchy the quiet-mire revision is restoring.

## What these views do and do not say about fullness

| Actual view | Current positive composition | Remaining evidenced gap | Placement conclusion |
|---|---|---|---|
|03 crossing, player60,3 | Raised bend, wet direct route, peat rows, reed groups and snag distinguish travel choices. | Repeated bright mire and black object backings; fallback watcher/post; regular dry-ground stipple. | Existing objects already supply a layout. No new filler generation justified from this frame. |
|10 shelter entrance, player40,12 | Masonry edges, board approaches, three separate wet areas, reed cluster and dry central route. | Notice and two crop stages do not read as native objects; ground pattern/background issues. | Central clearance is part of travel/interaction access. Judge after owner-art fixes; do not fill the center blindly. |
|12 matched room, player34,8 | Clean stone wall/floor contrast, obvious entrance, player remains readable. | Bench and cutter appear as punctuation, leaving a purpose-poor room. | First render the two real owners. Later room-density work needs an actual after view; no automatic furniture addition. |

Dark gray unexplored areas and dim remembered glyph terrain are disclosure states. Do not call them bare land, redraw hidden ownership, or infer unvisited furnishings from them. The three receipts count on-screen selected cells only:1091,937 and184 respectively; they omit offscreen/covered owners and do not establish whole-zone coverage.

## Bounded next acceptance sequence

1. Capture the same three views after the quiet-mire and substrate correction. Require calm continuous wet bodies, truthful dry/wet/peat/wood differences, no artificial black framing for known Grass-backed owners, and a clear player/route silhouette. Keep unrelated status/hazard behavior truthful.
2. Record the remaining fallback identity list again. Produce the smallest real-owner static packet above using the actual service/entry views as imagegen constraints, then native editable sprites and essential state mappings. No optional motion needed.
3. Actual service-room view must communicate a worker and a workbench. Entry must distinguish young/ripe Sumpsieve and a notice; crossing must visually identify guard/post. Verify interaction/readers/state changes and save/revisit without claiming unseen encounter variants.
4. Name any remaining scene-scale repetition or genuinely underfurnished area after those corrections. A generation change must have a concrete local before/after purpose and retain path/collision/state invariants; it is a separate reviewable slice.
5. Continue the same accepted materials into works/ordinary Sodden formations and then Beating's major static families, following `/tmp/coo-sodden-next-coverage.md`. Shared containers, crops, actors and services remain explicit world-coverage axes. Do not let local bench polish replace all-biome coverage.

## Honesty boundary

Verified here: the listed image pixels, matching receipt identities/counts, quiet-mire contact appearance and cited source owners/placement. Not verified here: corrected runtime output, native test results, interaction success, fluid-state behavior, animation, package delivery, performance/Deck, other viewpoints, the whole district or any whole biome. No active repository files were modified.
