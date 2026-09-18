# R1 building-block art acceptance

**Accepted within the asset-review scope; no material geometry or UV defect found.** Reviewed every installed model in three independently rendered contact sheets: 18 families × four variants = 72 FBXs. Source files, Unity Assets and the live project were not altered by this review.

## Visual evidence

These are real installed FBX imports with their installed palette texture and original UVs, viewed under neutral Blender lighting. The models were positioned only for the contact sheet; no mesh repair, repainting, or alternate art was used. The labelled columns run from variant 3 on the left to variant 0 on the right.

| Contact sheet | Families | Observed differences |
|---|---|---|
| [1](installed-variants-1.png) | Post, beam, plank wall, window wall, doorframe, plank floor | Timber stain and grain differences preserve joining silhouettes. Floorboard paint sequences are visibly different. Window and door openings remain readable. |
| [2](installed-variants-2.png) | Brace, stone block, masonry wall, corner, doorway, stone floor | Brace stain and small joint peg vary. Stone count/course placement, irregular block shapes and paint distinguish the masonry variants. Corner and doorway forms remain recognizable. |
| [3](installed-variants-3.png) | Foundation, wooden stair, stone stair, roof slope, ridge, end | Foundation shapes/paint vary. Wood stair peg spacing and stain, stone stair wear marks and paint differ. Roofs retain their joining shape but change shingle paint arrangement; gable ends remain visibly distinct from open ridges. |

The four variants are not four radically different silhouettes. Paint-only variation is permitted by the existing contract, and grain, peg and chisel changes are deliberately small. This review confirms those features in the enlarged art sheets; it does not claim that every small mark is distinguishable at the gameplay camera's scale. This is the existing bevelled construction kit, not a new restyle of native voxel towns.

## Cause and reproducibility

The former Unity publication was stale. Its six duplicate-variant families were independently reproduced by Blender reimport, agreeing with RS28's unchanged Unity uniqueness test. The current generator already contained the required variation.

- [Installed negative control](../R1-installed-red/roundtrip-summary.json): six duplicate families, zero bounds/topology failures.
- [Fresh-generation comparison](../R1-source-equivalence/roundtrip-summary.json): all 72 freshly generated FBXs match the existing source exports by imported model-frame vertices, polygon topology and UVs; every family has four unique fingerprints.
- [Installed acceptance](../R1-installed-green/roundtrip-summary.json): all 72 current installed FBXs match the source exports semantically, with no duplicate families or geometry errors. Direct file comparison also found all 72 installed FBXs byte-identical to those source exports.
- [Metadata audit](../R1-metadata-audit.json): all 154 existing building-kit GUIDs preserved; zero collisions or malformed GUIDs across 6,767 project metadata files.
- [Portable tests](contract-tests.json): 15/15 pass, including eight new visual-fingerprint positive/counter controls. The initial missing-helper RED was captured during authoring; a durable [RED replay](contract-red.log) uses the preserved before-source because Unity cleared the original Temp log. Filename changes alone do not make duplicate geometry/UVs pass.

FBX creation metadata is not deterministic-art evidence. The fresh-export comparison deliberately uses imported geometry/topology/UV fingerprints, and the contact-sheet manifest separately records the exact installed file bytes reviewed. The reproduction script is [render_review.py](render_review.py); use Blender with `--python-exit-code 1`, passing the project root and output directory after `--`.

The parent-owned [R103 full-suite receipt](../../VoxelWorld/R103-release-full-candidate/receipt.json) records 14,901/14,901 passed, zero compiler errors and Unity exit 0. That automated result is separate from this visual review. No Unity instance or MCP server was launched or restarted for this work.

## Bounds and review

This acceptance covers exported/imported art, variation, one-cell bounds, topology, source parity and metadata. It does not add or prove player construction, native destruction, traversal on stairs, roof behaviour, gameplay palette lighting, performance or combat balance. The editable assembly remains art-only until separately integrated.

Q1–Q4: generation/publication contracts remain symmetric; all families retain four numbered variants and unchanged pivots; meaningful duplicate/missing/malformed countercontrols are present; the living document now distinguishes imported-art completion from future native construction integration. No substantive defect requires another art edit in this R1 repair.
