# Pitch metadata preflight — staged, not applied

The current importer plans a new `worldVoxelSize`, then computes
`row.VoxelSize *= entry.Row.worldVoxelSize / row.WorldVoxelSize` inside the asset
publication loop. Catalog validation currently allows zero/nonfinite
`WorldVoxelSize`; recipe pitch also lacks its own finite/positive gate.

Concrete invalid-input flow: an otherwise valid binding with `WorldVoxelSize=0`
passes catalog validation. Import planning creates valid geometry. Publication
updates the generated mesh first, then produces infinite local pitch; the final
catalog validation rejects after assets have already changed. Likewise, a
zero/NaN recipe pitch produces invalid metadata after geometry publication.
Current U10 real artifacts have valid positive pitch; no current asset corruption
is claimed. This is a separate invalid-metadata failure from destination
preflight, which has already been fixed.

Suggested minimum correction, owned by root:

1. Add private static `ConvertedLocalPitch(float local, float world, float next)`
   to reject nonfinite/nonpositive inputs and nonfinite/nonpositive converted
   result. Compute the ratio in `double` to avoid a needless float-intermediate
   overflow, then validate its float result.
2. During preparation, before `CreateMesh`, compute the recipe world pitch and
   the converted local pitch. Store the latter in `Pending` or its report row.
3. During publication, assign the already validated planned local/world values;
   perform no fallible metadata calculation after modifying the mesh asset.

`VoxelWorldToolkitPitchTests.cs` is staged alongside this proposal: 13 tests,
including the ordinary conversion/repeated-import control, invalid inputs and
overflow. It targets the proposed private helper through reflection so it can
record a missing-validation RED before the implementation. All files remain
outside `Assets` during native U11; neither tests nor production were changed
in the live source tree.
