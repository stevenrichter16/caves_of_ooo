# Corrected-head full regression — November22 evening

Run started November23,00:49:49UTC (November22 America/Chicago). This is a start
receipt, not a completed test result or full-machine acceptance.

Runtime a9de3d3d9de9c21a917748c0d74c387b17ab7b3d, documentation head
7602dc783dcde98d153638f2ecb83abe13aa61dd. Canonical Tools/CloneVerification idle
clone2 sync/native EditMode runner, filter CavesOfOoo. All3887 selected code,
assembly-definition/reference and blueprint/config pins match main and clone
after sync; all6corrected well/flask pins match their qualified receipt. Working
tree includes preserved foreign changes: this is not a clean-tree export.

Live progress: /private/tmp/coo-full-a9de3d3d9-20261122/progress.json.
Result: /Users/steven/coo-verification/results/machine_full_a9de3d3d9_20261122.xml.
Log: same path with.log. Wrapper: /private/tmp/coo-full-a9-20261122.sh; unified
session56408. Check live status/process; run-start.json is only the initial
snapshot. No second native play/build/batch while active, no clone2resync or
runtime/test edits. Read-only source/design work can continue.

After completion, require zero compiler errors and exact pinned code; compare
failure names/messages with both baselines, rerun every new failure alone before
calling a regression, then investigate source/test fixtures without erasing
independent state. fullrun_h is inherited e89-era evidence (40376tests/92fails),
not current-head qualification. full_s9 has39500tests/91fails. Their hashes are
retained in run-start.json. The known Waterskin spill expectation already failed
alone on ef5 and is not silently dismissed or changed.

Current Mac candidate a9 is boot/render verified, with native input unavailable;
full regression does not substitute for packaged interaction. All prior builds,
saves/preferences, foreign changes and complete-machine limits remain preserved.
