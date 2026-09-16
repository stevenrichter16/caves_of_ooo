# Drowned Ledger and Marrowstye — completed verification

Two native chunks,52 voxel variants,311 additional passing tests. WI13 targeted:
311/311. WI14 full suite: 13,762 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 311 additional tests pass.

[Six native camera captures](../WI10-refined-preview/index.html): each has zero
missing meshes and zero unmodeled visible owners. Inspect seeds64,1729 and
729490642. Reusable menu: Caves of Ooo → Composition → Render Drowned Ledger
and Marrowstye previews. These are static captures, not a live performance test.

WI15 rebuild reproduces all214 final asset/metadata files byte for byte.
52 models are installed. No task GUID collisions among 6,263
metadata files. `source-synchronization.json` confirms the tested sources;
`integration.json` and `implementation.patch` preserve this phase's changes in
already-mixed files without staging unrelated work. Raw logs/XML remain local.

Together with Cinderhold and Sumphold (`d040c598`), this completes four areas,
116 variants and593 added passing checks. [Earlier pair's previews](../WB10-refined-preview/index.html).
Fresh generation receives the new layouts. Existing graphs, spawn, camera and
reveal settings are preserved; the original Unity editor was not restarted.
