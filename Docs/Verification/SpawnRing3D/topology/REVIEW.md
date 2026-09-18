# First real ring import geometry failure — independent read

The failing export has invalid cone-tip topology. Unity discarded degenerate geometry; the strict imported-triangle equality check correctly stopped publication. No validation or ModelImporter setting should be weakened.

## Actual evidence

- Import receipt `/tmp/codex-ring-import-LmTBcuuq/import.json`, run9b69d285c9c74d168a661b640255eca4:53 models validated before first failure, `ring-glow-quartz-vein-0 actual=210 expected=260`.0 C# errors per retained compile receipt.
- Raw Unity log lines1228–1272 contain five `self-intersecting and has been discarded` polygon warnings. Lines1287–1339 repeat the same five during configured reimport. First pass is CopyIfChanged/ImportAsset; second is ConfigureModel/SaveAndReimport, not ten distinct source polygons.
- Actual imported FBX meta: meshCompression0, generateMeshLods0, keepQuads0, weldVertices1, optimizeGameObjects0, preserveHierarchy1, isReadable1, globalScale1, useFileUnits1, bakeAxisConversion0. Compression/LOD are not removing50 legitimate triangles.
- Actual failing FBX equals its current ArtSource copy byte-for-byte. SHA256 `32a82c465842876b803ed8794811c1ca51f209eb367994f6d17eba6e65a03d5f`.
- Read-only binary-FBX parser directly examined its vertices/polygon indices:152 vertices,106 polygons,260 polygon-fan triangle equivalents, **50 exactly zero-area triangles**, **210 nondegenerate triangles**, **five whole zero-area polygons**,35 polygons containing coincident vertex positions. Details retained in `failing-export-geometry.json` with parser `inspect_fbx.py`. This is raw geometry evidence, not another Blender/Unity import.
- Full218-source-FBX survey retained in `all-source-topology-scan.json`: only the four GlowQuartzVein variants exhibit the defect; each has the same260/50/210 counts. No other source file triggered the narrow area/coincident-position check. This does not prove absence of every possible self-intersection or nonmanifold defect.

## Exact source mechanism

`/tmp/codex-spawn-ring-art/source/build_ring.py:291–296` authors five Quartz_point tips using the shared cylinder helper with6 sides and `radius2=0`.

`mesh_kit.py:104–112` creates two full vertex rings for every cylinder, including a zero-radius endpoint. The upper ring therefore contains six different vertex indices at the same point. It emits a zero-area six-gon cap (four nominal triangles) and six side quads with repeated apex positions (one valid and one zero-area triangle each). Thus every tip contributes10 nonexistent triangles to `calc_loop_triangles`; five tips overcount by50. The valid shape has a four-triangle base and six triangular sides.

Unity's exact210 outcome is consistent with rejecting the five collapsed caps and removing/welding the collapsed side geometry. The warning proves invalid polygons are rejected. We do not have Unity C++ internals tracing each removed side triangle, so we do not claim a specific internal optimization order; the raw geometry arithmetic independently accounts for all50 losses.

## Bounded repair / acceptance

1. Fix source cylinder/cone construction for a zero-radius endpoint: one apex vertex, a triangle side fan, no zero-area apex cap. Preserve ordinary nonzero frusta. Handle either endpoint symmetrically if supporting both; reject two-zero radii/zero depth rather than generating empty geometry.
2. Recompute metadata from the repaired actual mesh, not by hand changing260 to210. With all other geometry unchanged, each repaired variant should contain210 nondegenerate triangles. Re-export four FBXs and corresponding source/catalog metadata; preserve stable IDs/paths and Unity asset metas.
3. Add source validation for finite coordinates, repeated coincident polygon points and zero-area triangles/caps across the complete218-model set. A simple Blender mesh validation alone may not reject six distinct indices at one apex. Explicit triangle-area/whole-face checks are needed. Do not blanket-triangulate malformed polygons merely to silence warnings.
4. Rerun the unchanged strict importer. Require actual triangle equality, bounds/axes/UV/material tests and no geometry-discard warnings in the raw Unity log. Then rerun to prove stable GUIDs. Keep both failed and repaired reports; root retains partial owned writes for review, no rollback claim.
5. The current built-in warnings array is empty even though Unity emitted these mesh warnings; it collects authored importer warnings rather than all FBX warning output. For this gate, explicitly inspect raw import warnings (particularly discarded/self-intersecting/degenerate geometry) before acceptance. Optional future targeted warning capture should be tests-first and limited to owned FBX import warnings, not a broad suppression or failure on unrelated plugin logs.

No additional Unity diagnostic run is needed to establish this defect; the exact source/raw-FBX/count/warning agreement is sufficient. A diagnostic import would only be useful if repaired source still disagrees.

## Preservation / scope

External receipt reports protectedAssetsUnchanged=true, sourceFilesUnchanged=true, no existing GUID churn/collisions. Clean and dirty scene controls, original scene identity/flags and owned-control teardown all passed; overall scene receipt properly remains FAIL because the importer threw. No code, assets, source art, importer settings, Unity sessions or tests were modified/run by this agent. The analysis is CoO-original asset correctness, not a Qud mechanic claim.
