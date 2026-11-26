# Mac build for Wayside testimony and Cinderhold's hand seal

The universal package at
`/Users/steven/coo-verification/builds/mac-35d919f88-20261126/CavesOfOoo.app`
contains published runtime `35d919f88d6dba481a876f0bee62fe8e952797ca`. It includes
the inventory icons, Wayside testimony, saved pacing progress and
Cinderhold's mixed clay handover. Actual tool and project play is recorded
in the corresponding Editor receipts.
[Wayside](../WaysidePlayerWitness20261126/README.md) · [Cinderhold](../CinderholdMixedClay20261126/README.md)

The isolated build verified 32,236 project inputs against Git blobs and
SHA-256 pins. Unity built in 61.68 seconds with zero errors and five
warnings. It updated 84 private build inputs and added three. All runtime
C# files matched their source hashes. The player reached GameBootstrap
startup on Apple Silicon. All 186 package files and four ordinary startup
save files matched after the player closed. Logical preferences were
restored. Packaged gameplay, Intel execution and the full regression
remain open.
[Startup verification](startup-verification.json)
