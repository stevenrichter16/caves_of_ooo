# Refined village art handoff

Final source/export and overhead/cutaway images are complete. Both images were visually inspected at their normal 1200×800 size. The root reviewed preview V3 and accepted this refinement direction before the final bare-garden addition.

Changes: irregular fitted rounded cobbles with shared world-coordinate neighborhoods; flatter worn roof slabs, raised moss joints and inn divisions; denser closed leaflets over larger crowns; grouped flowers and granular ground paint; draped cloth with hems/ties/folds; irregular well coping and broken water curves; stronger capped/bound fence posts/rails; smoother hoods; six bare planting cells at X41/X42, Y21..23.

Contract checks passed: 123 model IDs/paths/kinds/pivots/rig/clip/socket contracts unchanged; all 71 owner records, all 111 static placements and all five building mappings byte-equivalent after JSON parsing; native definition snapshot unchanged; all 123 FBX files present and nonempty. The informational starterGardenCells mask exactly matches the approved six cells. No fake crops are present.

The representative Blender roundtrip passed for door, well, shell, teal rig, blade and pack. It confirmed one mesh per ordinary static model, separate well water, valid UVs, one skinned rig mesh, exact four sockets and five animation takes. This is export evidence, not Unity integration or GPU evidence.

Final conservative source budget: 362,905 whole-zone triangles and 328,705 triangles in 142 placements intersecting the reference crop, versus 342,551/295,071 before refinement. The crop estimate includes both roofs and interior furnishings. The roughly 9.6% excess above the initial 300k target is deliberate for dense rounded crowns, readable cloth/timber and planting beds; native profiling must determine whether it is acceptable. Palette grows from 512 to 1024px, still 64 stable inset swatches; no vertex-AO shader change or baked moving-object shadows.

Remaining reference differences are explicit: 67 native creek cells and five native room footprints retain their gameplay extents; hidden interiors are authored; starting bare soil is an authorized gameplay addition absent from the reference. The village remains a stylized approximation, not an exact pixel reconstruction. Source-local self-shadow and warm Cycles light do not establish Unity material parity. Root owns native visual/performance acceptance.

A fresh self-contained rebuild passed all 123 FBX semantic comparisons: geometry/topology/normals/UV/colors/materials, weights, skeletons, socket matrices and animation curves match after six-decimal normalization. Parsed manifests and palette PNG bytes are identical. Zero of 123 FBX containers are byte-identical because export metadata differs; binary FBX determinism is not claimed. The detailed result is refined-rebuild-verification.json. Read refinement-contracts.json, asset-budget.json, fbx-blender-roundtrip.json and layout-reconciliation.json for exact evidence.
