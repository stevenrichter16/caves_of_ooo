# Pending native checks

Nothing in this directory has compiled or run. The three-seed full-pipeline fixture is also present as an owned untracked file under Assets/Tests/EditMode/Gameplay/World/SaltVaultApproachTests.cs. Its attempted native RED was rejected while Unity was compiling; there is no job/result receipt. Do not substitute an older XML or claim expected failures as observed.

Once the Editor is ready, run that RED fixture against unchanged production source before integrating the draft. Then add focused controls:

- Synthetic east-facing archive: interior StoneFloor x15–17,y5–9; file17,6 and Indexer16,6; real locked file with existing key; VillageDoor18,8 and SandstoneWall flanks18,7/18,9. Reserve17,8/18,8/19,8. Real Sand outside. Expect core x20–21,y7–9 and optional stem22–24,y8.
- Canonical labels/readers are required; renamed or custom-described Sand remains untouched. Malformed replacement StoneFloor declines.
- No-op ObjectCreated probe must succeed, proving instrumentation is not itself the refusal cause. Derive a probe from RenderPart, register the Render alias after setup, invoke a static callback only for StoneFloor ObjectCreated, clear it in finally. Do not add a fourth Part that vacuously fails the three-part guard.
- Callback scope changes, equivalent door replacement and equivalent target-Sand replacement must refuse further paving. Snapshot after the intentional callback mutation; do not expect rollback of external callback work.
- A callback exception must not abort zone access; verify the refusal diagnostic and unchanged original ground membership.
- Parameterize core reservation, water coating and a passable projected footprint anchored outside the core. Preserve all owners and tile state; reject the whole core.
- Block stem23,8: accept six-cell core plus22,8; never skip across the blocker.
- Wrong address/biome/POI/name/faction/profile, cached zone, absent or unlocked archive and missing floor blueprint must decline. Retain file/key uniqueness, quest/lock/stock state and cached town identity through the full-pipeline controls.

Use GrovelandsCompositionTests.Factory(), OverworldZoneManager.CreateDetached and an uncached new Zone for synthetic probes. Existing native Stillleaf archive and village/door suites supply relevant adjacent regression; choose targeted groups after the actual implementation is known.
