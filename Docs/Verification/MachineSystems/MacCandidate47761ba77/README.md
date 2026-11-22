# Mac candidate: finite fuel and complete garment/item choices

**Built and booted; packaged gameplay qualification remains open.**
Source47761ba77 includes the finite poured-fuel correction and Posy/protection
menu access fixes. Prior74d85569a and all earlier builds remain preserved.
Last accepted M3c delivery is unchanged.

Package: `/Users/steven/coo-verification/builds/mac-47761ba77-20261121/CavesOfOoo.app`.
Unity6000.3.4f1, nondevelopment StandaloneOSX, universalx86_64/arm64 (Mach-O
checked), SampleScene only, DetailedBuildReport, no AutoRun/IncludeTests.
Actual BuildPipeline report59.760674s,364067133bytes,0errors/5warnings:
two hidden Owner fields, unreachable bootstrap branch, unused backstab field,
empty UnityCloud native-symbol-upload token. No new full-suite claim.

## Inputs and output identity

Dedicated copy `/Users/steven/coo-verification/mac-47761ba77-20261121` from a
clean committed git archive of Assets/Packages/ProjectSettings/ArtSource;
warm clone3 Library reused after exporting source. No tests/build run in main.
Guarded Editor-only `CodexCandidateBuild` runs only in this copy/batch mode and
refuses an existing app path. It sets universal architecture and exits after
writing the report. Source and helper are retained here for review.

31977 committed-source pins,31980 effective-input pins,592 external local MCP
package pins and186 app-file pins are retained with `input.tar` in
`/private/tmp/coo-mac-47761ba77-export-20261121`. Independent review verified
committed archive versus input.tar/source pins, every effective/input/external/app
hash and architecture, with no mismatches. Runtime C# inputs did not change.

This is source plus **disclosed generated/editor input deltas**, not a byte-exact
committed input tree:83 changed importer/URP/Standalone batching+architecture/
UnityConnect inputs, no missing inputs,3 additions (Editor build helper+meta,
ProBuilder settings). Exact [deltas](input-deltas.json) and
[patch](effective-inputs.patch.gz) are retained. Temporary performance package
metadata during the build does not enable tests in the player.

## Actual boot and preservation

Guarded helper launched exact executable as ownedPID53179, requested1280x800
windowed. Player registered7creatures/generated2678entities, rendered ordinary
world/HUD and character chooser. [Own-window image](boot-preview.png),
[process](process.json), [window/preflight](window.json). Screenshot captured only
its layer0window70588; preview is resized, original is in private native bundle.

Native OS input preflight was false. No input was posted. Therefore no packaged
movement, inventory, machine action, save/reload, controller, FPS or balance
acceptance follows. Editor tests/play receipts remain separate evidence.

Owned player closed using exact PID/executable guard; no other player was stopped.
Original standalone Saves directory was absent and restored absent. Logical app
preferences were freshly re-exported/equal to the before snapshot after restoring
changed session/display keys and deleting only newly added Screenmanager keys.
[Restoration receipt](restoration.json). No whole-plist byte-exact claim.
Private snapshots/logs/original image remain in
`/private/tmp/coo-mac-47761ba77-native-20261121`.

Fresh main preservation:84foreign tracked-file hashes and861normal save/preference
file hashes unchanged, excluding Unity analytics/TestResults. Main left stopped,
save override null, input settings restored. Prior packages/export bundles retained.
This candidate makes current fixes available for further play; it does not close
the outstanding Mac controls or twelve-machine acceptance gates.

Independent final source/boot/restoration artifact review: zero P1/P2. Reviewer
verified source/effective/external/app hashes, own-window image, executable identity,
player exit, normal Saves absence and freshly exported full logical preferences.
The reviewer did not operate Unity or the app.
