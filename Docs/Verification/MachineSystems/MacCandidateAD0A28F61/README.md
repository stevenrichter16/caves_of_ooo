# Mac build with the door brace and cold-dressing anvil

The universal package at
`/Users/steven/coo-verification/builds/mac-ad0a28f61-20261126/CavesOfOoo.app`
contains published runtime `ad0a28f61db5be9bde9d6e4351e2643863f04823`.
The brace and anvil gameplay were verified in the Editor.
[Brace](../FieldHaftDoorBrace20261126/README.md) ·
[Anvil](../AnvilColdDressing20261126/README.md)

The build took 99.49 seconds and reported zero errors and seven warning
records. The archive verified 32,240 inputs. Runtime C# matched the pinned
source. The 84 effective input changes and three added files were recorded
in the isolated build, with no missing inputs. The App contains both
Apple Silicon and Intel executables.

The package reached GameBootstrap on an Apple M5 with 2,665 entities.
After the owned player closed, its normal four startup save files matched
their previous hashes. Preferences were restored and compared logically.
The package retains all 186 pinned files. Earlier packages and the normal
save directory were preserved. Packaged gameplay, physical Intel execution
and the inherited full regression remain open.

[Build and startup verification](startup-verification.json)
