# Civic quartet — completed verification

Four native areas, three semantic formations each, 92 voxel models and 434 additional passing checks.
CQ13 targeted: **433/433** (219 dedicated adversarial cases). CQ18 full suite: **14,476 passed,
32 unchanged baseline failures**, zero C# errors and no new failures. `regression.json` compares exact
failure names/messages and accounts for the retained plain-village negative controls.

[Explore twelve gameplay-camera captures](../CQ12-final-preview/index.html). Each area has three distinct
formations with requested seed equal to effective `actualWorldSeed`, never zero. Every capture has zero
missing meshes and zero unmodeled visible owners. Reusable editor menu: Caves of Ooo → Composition →
Render Gantry, Tine, Quillhold and Tally previews. These static native captures do not measure live input,
animation or sustained performance.

CQ15 reproduces all 380 final asset/metadata files byte for byte. CQ19 verifies all 92 installed models.
No task GUID collision among 6,589 metadata files. The source freeze verifies 2,099
original/isolated-project inputs remained identical throughout the final suite. The earlier CQ15
art rebuild used identical production inputs; only the equipment-test table changed afterward.
`installed-artifacts.json`, `guid-audit.json`, `review.json` and `full-run-source-freeze.json` preserve evidence.
Raw Unity logs/XML remain local; compact receipts are committed.

`integration.json` and `implementation.patch` record exact phase changes to already-mixed files.
These changes are installed in the working tree. Mixed files themselves remain unstaged, preserving
unrelated work. Initially clean shared files and new phase files are committed normally.
This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.

Fresh surface sites: Gantry **Overworld.7.8.0**, Tine **Overworld.13.7.0**,
Quillhold **Overworld.14.9.0**, Tally **Overworld.10.14.0**. Existing graphs, spawn,
1.2x camera and full reveal remain intact. Original Unity was not restarted.
