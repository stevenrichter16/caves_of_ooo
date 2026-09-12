# Independent voxel pipeline review

Scope: `scene.py`, `generate.py`, camera/lighting/terrain, rendered Milestone1 and refined seed41 scenes, semantic/voxel/saved Blender/FBX consistency. Read CLAUDE.md and the full procedural-town request first. Production code remained read-only for this reviewer; root implemented the fixes below.

## Confirmed issues and verification

| Finding | Reproduction | Root fix and independent result |
| --- | --- | --- |
| Manual child jumps when its generated parent is removed | Parent at(4,8,3), manual child at local(1,2,1); regeneration changed world transform by8m | Detach surviving children before deletion and restore world matrix. RED→GREEN. |
| Manual collection instance loses borrowed source | An unowned collection-instance object in another scene references an owned generated collection; cleanup set its instance_collection to null | Treat unowned collection instances and nested source collections as borrowers. RED→GREEN. |
| Rejected dressing plan destroys previous generation | Inject the actual planner failure shape `ValueError('No accessible interior placement')`; old generated sentinel was deleted | Run fallible pure props/population/vegetation/structure planning before cleanup. RED→GREEN. This test does not prove rollback after arbitrary bpy/GPU/process failures. |
| Contradictory CLI modes silently ignore requested output | `--data-only --render` and `--data-only --export-fbx` returned success without the requested image/FBX | Reject contradictory modes with argparse exit2 before output writes. Both RED→GREEN. |

`blender_pipeline_review.py`:4 cases, originally3 RED and1 unparented-object countercheck GREEN; now4/4 GREEN. `test_pipeline_review.py`:3 cases, originally2 RED and1 ordinary-data-only countercheck GREEN; now3/3 GREEN. Initial failures remain in `independent-pipeline-red.log` and `independent-cli-red.log`; fixes in corresponding GREEN receipts. Tests are not broad guarantees of every manually authored Blender dependency type.

## Saved export verification

`blender_pipeline_export_review.py` independently loaded the saved `refined-seed41` Blender source and imported its FBX. All563 semantic entities match the voxel manifest. Blender source positions, rotations, scales, semantic IDs, asset keys and shared asset meshes agree; FBX semantic IDs and world positions agree. `independent-saved-exports.json` pins the artifact hashes.

The stricter `blender_fbx_roundtrip.py` checks all600 exported mesh objects, including terrain, and all45,917 world-space polygons. Every polygon retains its assigned base RGBA color: maximum error0. Object dimensions have maximum error0. Geometry, semantic IDs, asset keys and roles match. Receipt: `independent-fbx-roundtrip.json`.

The13,390 material-registration warnings are an installed Blender exporter diagnostic for repeated linked-mesh/material pairs, not visible material corruption. Inspection of bundled `io_scene_fbx/export_fbx_bin.py:3141-3147` shows it warns whenever a mesh/material pair has already registered, without first checking whether the material index is equal. All shared users have identical material-slot layouts. With600 objects and85 unique meshes,26 palette entries per repeated mesh produce exactly(600−85)×26=13,390 warnings. Do not remove instancing to silence this. Documenting or aggregating only this understood warning after layout validation is sufficient; pruning unused slots alone would reduce but not eliminate it.

These are Blender-to-FBX checks, not Unity integration, runtime destruction, native pathfinding, full BSDF reproduction or imported lighting verification.

## Visual review and scope boundaries

Inspected actual `Output/m1-seed41/gameplay.png` and `Output/refined-seed41/gameplay.png`, not just generation logs. M1 had prominent repeated rectangular interiors, coarse path stair steps, repeated pale water marks and a limited role vocabulary. The refined image visibly improves the path edges, water consistency, palette restraint, palm silhouettes and role variety; the animal enclosure, bathhouse, shrine/meeting space and storage activities are distinct at the gameplay camera.

The reviewed refined image still uses predominantly single rectangular open rooms and a consistent oasis-left environmental composition. Future river/cliff/desert-boundary anchor families, more varied room topology and district-specific regeneration are expansion areas, not capabilities proven by this milestone. Camera coverage is focused on the settlement rather than every terrain/water edge. Characters remain small at whole-town framing; the near-overhead view exposes interiors but is not a close character portrait.

The reviewed `town.json` has semantic building occupants but empty top-level props/npcs arrays. The separate voxel interchange contains the553 dressing/population instances and their IDs. Read-only pure planners exist, but a completely populated single abstract Town snapshot before realization remains a useful future unification; do not describe `--data-only` as exporting the complete dressed playable world.

All visual and export observations are scoped to hashes in the receipts. Other agents are continuing refinement; these checks do not automatically certify newer output directories or silently rewritten artifacts.
