# Cell-built architecture kit

Status: design and source asset implementation in progress. User explicitly
requested wood/stone building blocks assembled into buildings, like the game's
cell-based world, with genuine 3D art. This is part of the ongoing full world
conversion, not a replacement for its creature/item/terrain scope.

## Contract

One grid cell is one metre. Each placed block is an independent owner; a whole
building is an assembly, not one giant health pool or non-destructible mesh.
Model pivots are bottom-centre, horizontal bounds stay within the cell, and
quarter-turn rotations preserve the footprint. Height is an art dimension,
not a new movement layer. Roof pivots can be lifted onto wall height by an
assembly recipe. Rendering batches may combine geometry only while retaining
native owner/cell aliases and dirtying them after destruction.

Art families, four variants each: timber post, beam, plank wall, window wall,
doorframe, plank floor, timber brace, stone block, masonry wall, stone corner,
stone doorway, stone floor, foundation, wooden stair, stone stair, roof slope,
roof ridge, roof end. Each repeated family varies grain, joints or stone shapes
while preserving connection planes. Additional material families can follow
without changing the snap convention.

## Verification sweep

- Existing village and pilot sources export FBXs with ground-centred pivots,
  preserved normals and one Unity unit per native cell. Reuse these conventions.
- Native entity/footprint ownership already handles independent destruction;
  render meshes and Unity colliders are not the game simulation.
- No generic player construction mode was found in the initial script search.
  Do not pretend an art prefab creates a new building command. Asset/prefab
  acceptance and native placement/destruction integration are separate gates.
- Keep existing authored village/pilot buildings intact until a native assembly
  conversion is tested. No main-scene replacement or camera adjustment.

## Milestones

1. Blueprint-free source kit:72 real Blender volumes, measured manifest,
   four variants/family, snap metadata, assembled cutaway preview.
2. Independent geometry/FBX checks: finite geometry, no zero-area triangles,
   cell bounds, standard pivot/scale, snap planes, missing/duplicate model and
   variant controls. RED→GREEN before contract implementation.
3. Unity asset importer and prefabs, stable GUIDs, imported normals/materials,
   no Rigidbody/gameplay collider substitution, separate editable assembly.
4. Native block placement recipes and destructibility tests as part of world
   renderer integration; existing native material/HP/occupancy owns behavior.
   Verify destroying one wall changes only that owner's geometry and passage;
   neighboring blocks persist. No save migrations requested.

## Performance / honesty

Use existing patch batching and dirty-cell architecture for native integration.
No new per-frame loop is needed for an art kit. Actual integration will require
60–90-second profiling and dedicated ownership/visibility adversarial tests.
Blender gallery proves art only; Unity import proves assets only; neither is
reported as player construction or live destruction acceptance.

CoO-original art and assembly design, no claim of Qud code parity. Review and
implementation receipts live in Docs/Verification/BuildingBlocks3D.

## R1 release-candidate status — 2026-09-18

The installed kit's variant-publication defect (six families with fewer than
four distinct mesh/UV fingerprints) was repaired by republishing the source kit
through the existing asset builder, not by hand-editing meshes or weakening
uniqueness: 72 FBXs, `catalog.json`, `BlockHouse.prefab` and the authoring
material were regenerated (R102: Unity exit 0, zero C# errors, unchanged
`.meta` GUIDs). `contract.py` gained `validate_variant_fingerprints`, covered by
eight new offline tests (15/15 pass, re-run independently 2026-09-18). These
files are committed on branch `release-candidate`, not `main`. Visual inspection
of the imported variants in useful Editor views remains outstanding; milestone 4
(native placement/destruction) is unchanged and still not started.

## R1 imported-art acceptance — 18 September 2026

All72 installed FBXs match fresh source geometry/topology/UV fingerprints; all18families have four distinct variants. Independent and root review inspected all three labelled installed-FBX contact sheets.154kit GUIDs were preserved and the global metadata audit found no collision.15offline tests pass; the old installed kit reproduces its six duplication failures. See `Verification/BuildingBlocks3D/R1-art-review` and [R1 acceptance](RELEASE-R1-ACCEPTANCE.md). Main promotion closes the variant-publication repair. Native player construction remains unimplemented.
