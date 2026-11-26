# Mac package with Wayside player testimony and forge guidance

The universal app at
`/Users/steven/coo-verification/builds/mac-69ae7ee85-20261126/CavesOfOoo.app`
contains published runtime `69ae7ee85fc0ddca9b6681b0b501ba5c2049c77f`.
The Editor playtests exercised the player report that redirects Orven’s
search and the forge reader that explains how to take back a reserved
component.
[Wayside testimony](../WaysideLivingPlayerWitness20261126/README.md) ·
[Forge guidance](../ReservedMaterialReader20261126/README.md)

Unity reported 64.08 seconds, zero errors and five warnings. The isolated
build verified 32,244 committed inputs. The new and previous packages each
match 186 file hashes. Six Mach-O binaries in the new package include
Apple Silicon and Intel architectures. Independent review verified both
packages and the recorded build input changes.

The new app completed GameBootstrap on Apple M5 with 2,692 entities. Four
normal save files stayed byte-exact and the player preferences matched
after logical restoration. Packaged gameplay, physical Intel execution
and the full native regression remain open.

[Build and startup verification](startup-verification.json)
