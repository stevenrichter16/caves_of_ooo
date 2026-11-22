# Mac candidate with poured-fuel fix — November 21, 2026

**Built and actually booted; standalone gameplay qualification remains open.**
Source74d85569a includes the accepted bounded poured-oil correction. Prior
bc0511d4d candidate and every older build are retained. This does not replace
the last accepted M3c delivery.

Player: `/Users/steven/coo-verification/builds/mac-74d85569a-20261121/CavesOfOoo.app`.
Nondevelopment StandaloneOSX universal x86_64/arm64, Unity6000.3.4f1,
`Assets/Scenes/Main/SampleScene.unity`, DetailedBuildReport, no AutoRun/IncludeTests.
55.23s,347.21MiB,0errors/5warnings: [actual report](build-report.json).
Warnings are the same two hidden Owner fields, unreachable bootstrap branch,
unused backstab field and empty Unity Cloud symbol-upload token as prior candidate.

## Input provenance

Dedicated copy `/Users/steven/coo-verification/mac-74d85569a-20261121`, instance
7e116fa67ffb2811; path, batch mode and new fuel-rule field verified before build.
Assets/Packages/ProjectSettings/ArtSource exported from committed74d85569a,
not the dirty main tree. Warm clone3 Library reused only after source replacement.
No tests run in the main editor.

This is committed source plus **disclosed generated/editor deltas**, not a
byte-exact committed tree:83 normalized committed inputs (importer metadata,
URP runtime/prefilter lists, InputSystem preloaded asset, Standalone batching,
UnityConnect settings). A clone-path-guarded Editor-only reconnect helper is
additional tooling; external local MCP package separately pinned592files.
The performance package normally writes transient test metadata resources at
build time and removes them afterward; these do not activate test gameplay.

Evidence bundle `/private/tmp/coo-mac-74d85569a-export-20261121` retains committed
archive/source31975pins, post-build31980pins, exact effective-inputs.patch,
input-deltas.json, external592pins, Editor tooling fingerprint and186player pins.
No committed build inputs missing. Retain the bundle with the candidate.
Independent read-only review verified all31980 effective-input/186player hashes,
executable identity and architecture, boot image and current logical preference
restoration; zero P1/P2 publication blockers. It did not operate Unity or the app.

## Actual boot/render and restoration

[Process receipt](process.json), [own-window screenshot](boot-preview.png).
Root launched only this executable at requested1280×800 windowed, PID42236.
The actual Mac player generated the ordinary world, registered7creatures and
rendered its character chooser/world/HUD. No OS input was sent. `CGPreflightPostEventAccess`
was false; AppleScript keystrokes/assistive access had also been denied previously.
This is **not** movement, inventory, camp action, save/reload or controller proof.
No pixel/performance/normal-player balance acceptance follows.

The [reviewed launcher/restorer](boot-check.py) refused other players or any
preexisting standalone Saves. Existing app preferences were exported before
launch. Only the recorded matching executable was stopped. Any boot-created
Saves were archived under `/private/tmp/coo-mac-74d85569a-native-20261121/owned-saves-after`;
normal standalone Saves returned to its original absent state.

First preference comparison caught four newly added Screenmanager keys:
`defaults import` restored original values but retained added keys. The guard
stopped; root inspected the exact difference, then removed only newly added
Screenmanager keys absent from the original snapshot. A fresh export equals
the original logical preferences. [Restoration receipt](restoration.json).
No byte-exact plist restoration claim. Independent follow-up review records
this narrower correction; the initial mismatch was not silently ignored.

Fresh final checks: main idle/clean/nullroot;84foreign source and861normal game
file hashes unchanged (Unity analytics/TestResults excluded); original standalone
Saves absent and app preferences logically equal. No player process remains.
Player log and original/after preference snapshots remain in the private native
bundle; no personal preference values are published here.

Next packaged gate requires native input access (macOS Accessibility for the
controlling app) or a human operator, then actual movement/readers/actions and
save/reload on an isolated earned checkpoint. Continue useful Editor machine
qualification meanwhile; all twelve complete-machine gates remain open.
