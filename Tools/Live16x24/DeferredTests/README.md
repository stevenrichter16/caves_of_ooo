# Deferred graphics test drafts — not active Unity tests

Status, 2026-10-27: `FlatPixelCindercallerMotionTests.cs` is a preserved draft
for the deferred East Cindercaller motion task. **It was not run as a native
Unity test, is not in an active Unity test assembly, and provides no runtime
RED/GREEN evidence.** Its proposed runtime implementation was not made.

The user reprioritized missing sprite coverage across all biomes and areas over
further nuanced animation work. The fixture and its existing `.meta` were moved
outside `Assets` without changing their bytes, so they cannot be mistaken for
an active test requiring unimplemented catalog and casting behavior.

| Original path | Preserved destination |
| --- | --- |
|`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelCindercallerMotionTests.cs`|`Tools/Live16x24/DeferredTests/FlatPixelCindercallerMotionTests.cs`|
|`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelCindercallerMotionTests.cs.meta`|`Tools/Live16x24/DeferredTests/FlatPixelCindercallerMotionTests.cs.meta`|

The integration reviewer `inventory_stat_review` measured the original and
destination files and reported a byte-preserving move. The source handoff
author independently read and hashed the destination without editing either
file; both hashes agree with that review:

| File | Bytes | SHA256 |
| --- | ---: | --- |
|`FlatPixelCindercallerMotionTests.cs`|33715|`df8c545f699e667deb0cba25a12e714d2ef17681eedc04707d43fbe94e0cf009`|
|`FlatPixelCindercallerMotionTests.cs.meta`|60|`a8c5fea1ef7945a99313199b3e9148c64c35db2d605c5aea251daaab1c6ea15f`|

This folder is outside Unity's `Assets` import tree. The saved `.meta` preserves
the draft's identity for a possible future return; its presence here does not
make the fixture an active Unity asset. Do not count its test attributes as
executed tests or include it in passing native-test totals. Source-only Python
verification of the separate art packet is not a substitute for running this
fixture.

The draft proposes local owner/state admission, five derivative frames,
unchanged idle recovery, a distinct cast gesture, existing WorldFx sequencing,
and disclosure/lifecycle counterchecks. Those assertions express intended
behavior, not implemented behavior or verified API compatibility. Review them
against the then-current runtime before any future restoration; do not implement
motion merely to make this deferred draft pass.

The recoverable art, first-build source evidence and outstanding gates are in
[the deferred source packet](../../../ArtSource/Live16x24/east-cindercaller-motion/README.md).
Current priority is [worldwide sprite coverage](../../../Docs/GRAPHICS-COMPLETION-PLAN.md).
No art edits, imports, regeneration or native tests were performed when this
preservation README was added.
