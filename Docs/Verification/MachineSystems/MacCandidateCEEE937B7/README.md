# Mac package with shared steam catches and applied weapon feedback

The universal app at
`/Users/steven/coo-verification/builds/mac-ceee937b7-20261126/CavesOfOoo.app`
was built from published `ceee937b778338eaab26fd3af93fb6e07abe09b2`.
The Editor playtests covered finite steam catches into portable vessels
and the applied damage display for an enhanced quarry dagger. The Sill
playtest also used this runtime.
[Steam catches](../CondenserWaterVessels20261126/README.md) ·
[Quarry dagger](../QuarryPaleSaltFieldUse20261126/README.md) ·
[Sill filtration](../SillEarnedSandFiltration20261126/README.md)

Unity reported 58.13 seconds, zero errors and five warnings. The
build verified 32,244 committed inputs, and the 84 importer and settings
changes and three additions exactly match the previous isolated build.
Independent verification matched all 186 files in each package and
confirmed six universal Mach-O binaries in the new app.

The new app completed GameBootstrap on Apple M5 with 2,693 entities.
The four normal startup saves remained byte-identical and preferences
matched after logical restoration. Packaged gameplay and physical Intel
execution remain open. The whole native regression is also open.

[Build and startup receipt](startup-verification.json)
