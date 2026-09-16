# Olderdeep and Wellmeet — completed verification

Implemented four native chunks and 116 model variants. All 344
additional tests pass. FC18: 652/652 focused checks. FC20b: 451/451 native/art/scope
checks. FC22: 13,169 passing tests, exactly 32 unchanged baseline failures, zero C# errors and no new failures. See [regression.json](regression.json).

[Open the twelve-view native gallery](../FC17-final-preview/index.html). These
are actual manager-generated zones at three seeds. All captures have zero
missing meshes and zero unmodeled visible owners. Static images do not establish
live input feel, animation quality or sustained FPS. New compositions apply to
freshly generated zones; existing saved/current graphs are preserved.

All 470 generated asset/metadata files are installed and reproduce byte for byte
on successful FC21b rebuild. FC21's earlier Burst shutdown crash is retained as
a failed execution receipt, not counted as successful validation. No task GUID
collides among 5,988 scanned metadata files.

`source-synchronization.json` verifies final source/input bytes against the
isolated validation project. `integration.json` separates initially clean
shared files from already-mixed files; `implementation.patch` records only this
phase's installed deltas for the latter, without committing unrelated work.
Raw Unity logs and XML remain local; compact receipts and final previews are
checked in. The user's original Unity session was not restarted.

Menu: Caves of Ooo → Composition → Render Olderdeep and Wellmeet previews.
Rebuild the viewer with `python3 Tools/founding_camp_gallery.py <preview-directory>`.
