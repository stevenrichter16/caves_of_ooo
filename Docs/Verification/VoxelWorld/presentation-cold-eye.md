# Voxel presentation/importer independent cold-eye

Review scope: `VoxelWorldPresentation`, its two presenter hooks and ground-template hook, `VoxelWorldToolkitImporter`, and native binding specifications. No existing production files were edited by this reviewer. Unity launches and RED/GREEN receipts belong to the root agent.

This is CoO-original rendering/tooling. Qud parity is not claimed; the relevant authority comparison is the unchanged CoO simulation and current native presenters.

## Findings handed to implementation owners

| Finding | Concrete reproduction / consequence | Gate |
| --- | --- | --- |
| Generated asset key accepts path components | Set a valid catalog binding's `SourceKey` to `../../foreign`. Prior validation only required nonempty text; the importer concatenates it under its output folder and can reach another asset path. Current offline baker emits safe keys, so this is an edited/corrupt catalog boundary, not a claim of current bake corruption. | Eleven unsafe-component cases in `VoxelWorldPresentationAdversarialTests`; catalog fix assigned to `voxel_assets` after root RED. |
| Two bindings can target one output asset | Duplicate `SourceKey` across distinct source meshes collapses their generated destination. | Already caught/fixed by the mesh agent's earlier U05 RED; independent duplicate-key countercheck retained. |
| Only half of a quad is validated | Replace `[0,1,2,3]` with `[0,1,2,0]`. First triangle remains valid; second triangle has zero area. Bow-tie order `[0,2,1,3]` also produces conflicting face directions. | Toolkit adversarial quad tests, prior to root fix. |
| Full determinant disagrees with point transform for projective matrices | Supply identity with `m33=-1`: `MultiplyPoint3x4` leaves all points unchanged, while the full determinant triggers a winding flip. Nonzero bottom-row xyz coefficients are likewise ignored by the point transform. | Five non-affine matrix rejection cases; true affine reflection/translation are positive controls. |
| Integer topology silently coerces JSON values | Use `1.1` or `"1"` as a face index, or `0.1` / `"0"` as a palette index. Prior explicit JToken casts can coerce these into integers instead of rejecting malformed topology. | Four typed-index cases. |
| Asset publication validates destinations late | Pending geometry is fully prepared first, but the publication loop checks a wrong-type/foreign destination only when it reaches that row, after earlier mesh assets/catalog bindings have changed. A late invalid destination can leave a partially published import. | Source-confirmed publication-order issue, reported to root for whole-set preflight. No destructive reproduction against shared project assets was performed; do not claim an observed disk failure. |

## Correctness checks and limitations

The two presenters create their adapter before adding owned models and call `Apply` before creating selection colliders and before native material preparation. The ring's batched ground builder resolves a borrowed template into a fragment; it does not assign back into the source prefab. Native entity IDs, parts, positions, footsteps, damage, inventories and saves are not edited by these hooks.

The adapter keeps a generated-mesh set, so the town's double `PrepareModel` path is idempotent. Its counters belong to one bind. Unsupported meshes retain their exact references and report once per distinct source, even when names match. Materials, skeleton transforms and item sockets remain on the existing instances; replacing a skin retains/enlarges its motion bounds. The runtime contract is covered by ten independent owned-instance tests using only disposable objects, not mutations of real Resources assets.

The actual neutral toolkit recipes inspected in `Output/native-region/assets.json` are centered in XY with minimum Z=0 and exact geometry/bounds agreement. Their X/Z/Y axis conversion has negative determinant and needs exactly one winding reversal. Native fitting is uniform in prefab-root coordinates and anchors at the existing native bottom/center; it does not independently stretch width, height and depth. Aspect mismatch can leave unused space inside an original object's envelope; artistic readability still needs native screenshot inspection.

All 38 authored binding rows resolve only static, one-mesh/one-material native models; actors keep the baked original rigs. Toolkit palette colors are mapped into the existing native atlas via per-face UVs, keeping fog-compatible native materials. The review does not claim exact RGB identity between toolkit palette and a limited native palette: nearest-color mapping is an explicit conversion.

Added tests:

- `VoxelWorldPresentationAdversarialTests`: 22 cases, ten owned-instance/counter controls plus twelve catalog key boundaries.
- `VoxelWorldToolkitAdversarialTests`: 20 cases across malformed geometry, typed indices, matrix shape, affine reflection, UV ownership, finite channels and isolated failure.

Root U07 confirmed 22 review-owned RED cases and 20 passing controls, with zero
compiler errors. Root fixed the catalog keys and importer inputs; U08 passed all
148 focused tests, including all 42 independent review cases. The detailed RED
failures, GREEN receipts and hashes are retained in
`presentation-review-receipt.json`.

The actual U09 regeneration found a separate notable failure: Unity normalized
new mesh names to filenames, so a generated-name prefix check rejected valid
earlier outputs. Root replaced that check with explicit importer `userData`
ownership, tagging only the 38 verified initial outputs. U10 then imported all
38 successfully. The reviewer independently checked those tags and captured a
post-U10 GUID baseline in `toolkit-guid-pin.json`; root reported the tag update
preserved its preexisting GUIDs. This baseline does not pretend to be an
independent U06 before-state.

A further invalid-metadata publication case was confirmed: zero/nonfinite
old `WorldVoxelSize` or recipe pitch can create invalid local-pitch metadata
during the write loop, after valid geometry was published. Current baked values
were positive; no corruption of those artifacts was claimed. The 13 tests were
kept outside Assets during native U11, then landed and failed RED in U12. Root
added `ConvertedLocalPitch` with finite/positive input and output guards and a
double-precision intermediate, invoked during preparation before mesh creation
or publication. The validated value is stored in the pending row and assigned
without division in the write loop. All 13 tests pass in U15. The six real
imported-art/provenance/ownership/GUID tests also pass in both U12 and U15.

Final independent XML comparison is recorded in
`U15-final-full-regression/independent-comparison.json`: all 167 voxel cases
pass, as do the supported and outside-region equipment cases. The full suite
has 11,678 passes and the same 32 failures as U00, with identical failure names
and messages and no C# errors. This closes the review's pending test status; it
does not relabel the baseline failures as green or expand the U11 native
profile's stated scope.
