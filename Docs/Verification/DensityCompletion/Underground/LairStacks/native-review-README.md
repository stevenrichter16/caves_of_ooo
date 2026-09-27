# C10.2 native audit review overrides

Gameplay remains the finalized immutable `/tmp/coo-density-lair-stack` candidate.
The published launcher now contains the current OriginalEnemy restoration retry.
The next unpublished driver-only patch strengthens its save/load evidence.

## Scene restoration

`DensityLairNativeBatch.cs`: editor-update retry stays registered through Play
teardown; repeated scheduling is idempotent; no-pending and successful completion
remove it. Finish restores old start scene and owned Game View before exit.
Four source checks RED on old source, GREEN on override. Editor-reference compiler
zero errors. Root owns the two actual NativeDensitySceneRestorationTests cases.

## Real checkpoint proof

`DensityLairNativePlayer.cs`, `save-proof-driver.patch`: native F5 must issue a new
save message, matching metadata and changed on-disk hash. One safe adjacent real
keyboard move changes position and pays exactly one scheduler action; saved bytes
remain unchanged. F6 must replace the player reference. Restored IDs, gear, cache
depletion, boss/player HP, player location, tick, energy and file hash must match
the checkpoint. No movement transfer or state mutation simulates this sequence.

Completion now requires all fourteen named checks, exactly fourteen outcomes and
seven captures. Four source-contract RED→GREEN checks are saved; actual runtime
compilation with Unity references and current C11.2 helper succeeds. Independent
read-only review found no further concrete false-positive or ownership issue.
Enemies still act on the one checkpoint step; harm/death/refusal fails honestly.

This is source/compiler and independent-review evidence, not a claimed native run.
Root must release the driver-only publication, then execute the Beating audit.
