# Cathedral and Stillleaf — completed verification

Implemented six native levels and 72 model variants. All 287 additional tests
pass. SC14: 537/537 focused checks. SC17: 12,825 passing tests and exactly the
32 failures already present in SC00; their names and messages match. No C#
errors or new failures. See [regression.json](regression.json).

[Open the 18-view native gallery](../SC10-refined-preview/index.html). SC10
renders actual manager-generated zones at three seeds, not a manually arranged
scene. Both missing meshes and unmodeled visible owners total zero. Static
images cannot establish live input feel or sustained frame rate.

All 294 generated asset/metadata files are installed and reproduce byte for
byte on rebuild. No task GUID collides among 5,735 scanned metadata files.
`source-synchronization.json` verifies 47 final source/input paths against the
isolated validation project. `integration.json` identifies three initially
clean shared files and eight already-mixed files; `implementation.patch` records
this phase's exact deltas without committing unrelated pre-existing work.

The reusable preview command is Caves of Ooo → Composition → Render Cathedral
and Stillleaf previews. Rebuild the HTML viewer with
`python3 Tools/sanctum_gallery.py <preview-directory>`.
