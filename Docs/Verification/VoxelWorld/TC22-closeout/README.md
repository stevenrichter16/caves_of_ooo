# First Tent and Last Counter — completed verification

Two native areas,three semantic layouts each,40 voxel variants and280 additional passing checks.
TC17 targeted:280/280. TC20 full suite: 14,042 passes,32 unchanged baseline failures,
zero C# errors and no new failures. All280 added cases pass. `regression.json` compares exact
failure names/messages and accounts for four retained negative controls moved to depth1.

[Eight gameplay-camera captures](../TC16-final-preview/index.html) cover seeds64,1729,729490642,1
in both areas. Each has zero missing meshes and zero unmodeled visible owners. Reusable menu:
Caves of Ooo → Composition → Render First Tent and Last Counter previews. These are native
static captures, not a live input, animation or performance test.

TC21 rebuild reproduces166 final asset/metadata files byte for byte. TC22 installs all40 models.
No task GUID collision among 6,364 metadata files. `full-run-source-freeze.json`
verifies2033 original/isolated project inputs remained identical throughout the final full run.
`installed-artifacts.json`, `art-source-audit.json` and `review.json` preserve the final evidence.
Raw logs/XML remain local; compact receipts are committed.

`integration.json` and `implementation.patch` record this phase's exact changes in already-mixed
files. Those changes are installed in the working tree; the mixed files themselves remain
unstaged so unrelated work is preserved. Initially-clean shared files are committed normally; the integration manifest lists each one.
This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.

First Tent: Overworld.5.17.0, Village/TentCampFirst. Last Counter: Overworld.18.18.0,
Village/ConcordPost. Fresh generation receives the new layouts; existing graphs, spawn,
1.2x camera and full reveal are preserved. The original Unity editor was not restarted.
