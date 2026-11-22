# Mac build candidate — bc0511d4d, November 21

Built successfully in54.38s:347.2MiB, zero errors, five warnings. See
[actual build report](build-report.json). This is a candidate, **not a verified
Mac delivery**. No standalone play or replacement of the prior accepted Mac.

Player: `/Users/steven/coo-verification/builds/mac-bc0511d4d-20261121/CavesOfOoo.app`.
Dedicated editor: `/Users/steven/coo-verification/mac-bc0511d4d-20261121`.
Unity6000.3.4f1, StandaloneOSX, nondevelopment, DetailedBuildReport,
`Assets/Scenes/Main/SampleScene.unity`, no AutoRun/IncludeTests.

Assets/Packages/ProjectSettings/ArtSource exported from committed bc0511d4d,
with warm Library from clone3. Main's foreign runtime was not copied. Disclosed
editor-generated deltas mean this is **not a byte-exact committed input tree**:
importer metadata, URP runtime/prefilter lists, InputSystem preloaded asset,
Standalone batching, UnityConnect settings and package build resources.
External MCP local package is separately pinned. Clone-only Editor helper
CodexBuildCopyReconnect restarts the bridge after startup, guarded to this clone;
it does not enter the player runtime. Independent review found no confirmed
P1/P2 runtime defect in those extras. PerformanceTestRun JSON resources are
generated on every build by the performance package, then removed from Assets;
their consumers are test metadata callbacks, not ordinary game behavior.

Retained evidence bundle:
`/private/tmp/coo-mac-bc0511d4d-export-20261121` contains committed source archive,
31973 source pins,592 external-package pins, import delta inventory, exact
effective-inputs.patch,31979 post-build input pins,186 player file pins and report.
Post-build pins exclude transient performance JSONs; their observed generation
is in import-deltas.json. Preserve this bundle with the player before delivery.

## Connection recovery

GUI copy reached MCP but concurrent starts disconnected immediately. An actual
Packages with Errors modal then blocked its editor update loop; macOS input
permission is absent, so it was not dismissed through simulated input. Restarted
only the owned idle copy in batch mode; delayed bridge reconnect succeeded.
Both main4f5664cceff9ea5e and copydca240c657bd1174 were listed and a read-only
execute verified each project path. Build tools worked through the latter.
No shared external MCP edits or main connection-setting changes.

Remaining: independently review effective-input deltas, diagnose the GUI package
warning, and actual standalone boot/movement/readers/actions/save-reload with a
reviewed normal-save/preferences preservation plan and available native input.
The prior accepted M3c Mac remains the last delivery verified by this task.
