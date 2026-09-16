# Cinderhold and Sumphold — completed verification

Two native chunks,64 voxel variants,282 additional passing tests. WB11 focused:
282/282. WB14 full suite: 13,451 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 282 additional tests pass.

[Inspect six native camera captures](../WB10-refined-preview/index.html).
All have zero missing meshes and zero unmodeled visible owners. The reusable
menu is Caves of Ooo → Composition → Render Cinderhold and Sumphold previews.
`python3 Tools/workshop_bog_gallery.py <preview-directory>` rebuilds the viewer.

All262 generated asset/metadata files are installed and reproduce byte for byte
on WB15. No task GUID collides among 6,137 scanned metadata.
`source-synchronization.json` verifies the final tested sources. `integration.json`
and `implementation.patch` isolate this phase’s changes in already-mixed files;
unrelated work is excluded from staging. Raw logs/XML remain local; compact
receipts and the final previews are checked in.

New layouts apply to fresh generation. Current graphs, spawn, camera and reveal
settings remain intact. Headless gameplay contracts and static camera views do
not establish live input feel or sustained frame rate. The original Unity
session was not restarted.
