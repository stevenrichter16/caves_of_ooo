# Opening paintover v1 — independent visual review

**Decision: partial motif reference only. Reject the complete image as a production target.** The image is more immediately readable than the rough atlas, but changes native owners, state, path meaning and the approved flat style. None of its extra scenery or creatures is authorized for integration by accepting a visual motif.

Compared directly on 2026-10-23:

- Actual Unity input: [greybox-game-20261023-043026.png](../../../Docs/Verification/Live16x24/greybox-game-20261023-043026.png), 1280×800.
- Generated concept: [opening-paintover-v1.png](opening-paintover-v1.png), 1586×992.
- Generation brief: [opening-reinterpretation-prompt.md](opening-reinterpretation-prompt.md).
- Native owners/states and approved sprite measurements: [PARTS.md](../PARTS.md).

The resolution change alone is not a gameplay defect in concept art, but it means generated pixels are not native sprite pixels. Do not crop/shrink the whole image into a tile sheet or judge the original camera's pixel scale from this output.

## Motifs worth carrying into native studies

| Motif | Why it helps | Boundary for reuse |
| --- | --- | --- |
| Sparse, uneven reed clumps around dark still water | Tall pale stems separate the pond edge from short field vegetation. The irregular rhythm is better than the greybox's identical three-stem marks. | Author two native 16×24 clumps with clear gaps. Use only existing `Reeds` owners; no added edge blockers, boulders or hidden reeds. |
| Small forked olive ground plants | Low branching silhouettes read as vegetation while leaving dark ground between plants. | Map to existing `Bush` with `reference-glade-green-grass`. Keep them lower/darker than reeds. No decorative flowers or extra scatter between owners. |
| Dark ruin versus paler boundary masonry | Material/value separation helps the western ruin and eastern working enclosure read as different structures. | Retain the idea of distinct masses, not the bevelled rock rendering. Native walls use restrained flat color cuts; low crossing and passable rubble stay visibly lower. |
| Bent pale grain heads versus short brown stems | A silhouette difference can explain harvest readiness at a glance. | Apply ripe heads only to an unharvested `RipeCropRow`. Plain `CropRow` and harvested rows stay stubble. Drawgourd remains a separate vessel-shaped crop. |
| Quiet dark-green spaces around focal objects | The player's warm silhouette is easy to locate against the ground. | Keep the ground nearly solid. Remove the concept's soft illumination, mottling and halos during native authoring. |

These motifs are provisionally selected for isolated native studies. Their final pixels, integration and motion have not been accepted.

## Rejected changes

| Category | Observed change | Required correction |
| --- | --- | --- |
| Owner identity | A recognizable cream-hatted cook is inserted beside the field, borrowing the style reference's character rather than the input's population. | Remove it. Orven remains at his real kitchen; a style reference does not place him in this opening. |
| Extra actors | Two large plated quadrupeds appear on open ground without matching input actor silhouettes. They also occupy more visual space than the native single-cell owners. | Remove them from the scene reconstruction. Design actual marlbacks separately from their short-faced, shale-plated burrower description, and place only current visible owners. |
| Misidentified service | A tall glowing terracotta oven appears at the right end of the working road, approximately the native chest location. The actual campfire is farther left beside the well. | Keep `Chest`, `Campfire` and `RepairLinedWell` distinct. No oven, chimney, oven service or relocated fire. The chest must not glow like a cooking station. |
| Collision / paths | Flat road bands beside the pond and working area become raised mossy ledges, visually reading as barriers. | Rebuild these as flush passable stone/earth. Keep the basin distinguishable without turning the bank into a wall. |
| Resource state | Many cut field cells acquire bright ripe grain heads. | Preserve the input's sparse ripe cells and use live `FieldHarvestPart.Harvested` after play. Never let aesthetic filling imply extra harvests. |
| Owner substitution | Small gravel/rubble marks become conspicuous cream flower patches around the player and ruin. | Keep low stone/debris shapes for `Rubble`/`BrokenColumn`. New flowers imply a different material or harvestable plant and are outside this art pass. |
| Footprint / projection | Thick top faces, bevels, tiny cast shadows and raised banks pull the result toward a miniature 3D scene. | Reconstruct within native 16×24 flat frames, with simple graphic material cuts and no volumetric extrusion. One-cell owners must not conceal neighboring action cells. |
| Palette / pixels | Smooth local gradients, glows and uneven fine-detail density appear in ground, water, walls and actors. | Use deliberate solid-color pixel clusters, binary alpha and the approved sprites' restrained vocabulary. No automatic downsampling as final production art. |
| HUD | HUD text, typography, proportions and tiny hotbar details are regenerated rather than preserved byte-for-byte. | Ignore all concept UI pixels. Retain the existing live HUD and its rendering. This is not a UI redesign brief. |
| Fog / topology | The broad fog staircase is recognizable, but the generated image is not an exact world mask; added detail and objects cannot be trusted at its edges. | Reuse native visible/remembered/unexplored ownership. Do not import the concept's fog, black areas, walls or openings as a background. Actual world topology is authoritative. |

## Small first extraction/reference batch

Extract **six isolated reference parts**, with no ground scene, labels, characters, pedestal shadows or lighting effects:

1. Two pale reed clumps: one taller narrow cluster, one bent/open cluster.
2. One low forked olive sprig for the glade's `Bush` owner.
3. One dark ruined-stone cell: sparse broken face and negative gaps, flattened from the concept.
4. One ripe grain-head cluster.
5. One cut-stubble cluster paired with the grain.

The extracted images are intermediate references, not imported assets. Re-author each at exactly 16×24 and inspect on `#032F23` at native size. Use the unchanged approved sprites beside the studies to check pixel weight and palette. These six frames improve repeated environmental marks across the actual view without requiring dozens of assets or introducing new owners.

Integrate and recapture this small group before making extra variants. Then produce the player's base silhouette and the **actual visible cast** from approved pixel vocabulary and blueprint identity, rather than extracting the concept's invented cook/animals. The well/basin pair and wall/low-crossing distinction remain the next high-value functional studies. Keep all unconverted owners visibly accounted for as greybox placeholders; this first batch is not opening completion.

## Next concept prompt corrections

For an extraction call, request individual subjects only. Specify native reconstruction references, no whole-scene recomposition, no gradients and no additional species or props. For any later paintover, explicitly say: preserve the count/positions of existing actor silhouettes; marlbacks are short-faced burrowers with overlapping shale plates and grasping foreclaws; passable road bands stay flush; only the exact ripe input cells receive grain heads; the right-hand storage box is a chest; preserve all original HUD pixels or leave that region untouched.

**Verification boundary:** this review directly compared the two supplied images and checked their dimensions. It did not change Assets, invoke Unity, test gameplay or establish final-sprite quality. The native capture remains the composition authority; the generated image supplies only the selected motifs above.
