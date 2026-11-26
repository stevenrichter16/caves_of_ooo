# Mac build with the trap wedge, hearth oil and equipment sign fixes

The universal package at
`/Users/steven/coo-verification/builds/mac-aa016396d-20261126/CavesOfOoo.app`
contains runtime `aa016396d7bfae962cd6bb980cb42e3da2565598`. Playtesting
in the Editor covered both ways into the dispatch yard, the finite
hearth-to-still loop, and the broken equipment reader.
[Trap playtest](../FieldHaftTrapWedge20261126/README.md) · [Hearth playtest](../LiquidHearthFuel20261126/README.md) · [Equipment reader](../BrokenPenaltyReadout20261126/README.md)

The build completed in 61.57 seconds with zero errors and five warnings.
The isolated build verified 32,244 committed inputs. Its importer and
settings changes match the preceding build. The package contains 186
verified files and six universal Mach-O binaries.

The exact app completed GameBootstrap on Apple M5 with 2,681 entities.
All four normal startup save files matched after closing the player.
Preferences matched after logical restoration. Earlier builds remain
available. Packaged gameplay and physical Intel verification remain open.
The new whole-suite run is pending.

[Build and startup verification](startup-verification.json)
