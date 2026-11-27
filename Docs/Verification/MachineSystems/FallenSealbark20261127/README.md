# Fallen sealbark in a local shower

The western Morrowfast allotment can offer one fallen slat in a shower
and for 150 ticks after rain. Leaf litter, exposed plate and the spent
mark use the same state as the gather action. The slat keeps the normal
Prepare action. The ordinary timber pile supplies four lengths on its
own and the gate needs two. Previously visited allotments keep their
saved layout. Older worlds with an enabled, keyed exploration plan can
find the source when they first enter the allotment.

I continued a retained ordinary save on the world map at tick 8480.
Seed 65, world version 17, had never entered the allotment. I walked south
from Stump and entered the field while it was raining. Nineteen local
steps reached 59,8 at tick 8700. I recorded the exposed plate on the map
and used the ordinary gather action. The spent source 74075 stayed on
grass 72297 and its one slat 74078 entered the pack at 8710.

The existing fallen frame salvage returned four timber. I used Separate
one, dropped the other three, then prepared the slat into timber. The pack
held exactly two boards at tick 8790. The original three stayed where
they were dropped. Two local sari-snakes disrupted the approach, and the
player drank two carried tonics while completing the repair and close.
The gate consumed both boards, gained its swing and latch at 8850 and
closed at 8870 with 15 HP. A fresh Continue restored the closed gate
74061 and spent source. Choosing Open reopened it at 8880 with 11 HP.

Six new tests found the missing source in the previous runtime. All 196
targeted cases passed on the final source, including seven feature cases.
They cover the window boundary, a dry canonical slat, preparation,
the clay seam, travel, binary reload and first entry in a version-17 world.
Dry weather and later-rain depletion were exercised in the native tests.
The directed Editor route establishes a working material loop. Broader
discovery frequency, a full regression on the newest runtime and Mac
gameplay remain open.

Independent source review cleared the code and gameplay review matched
all 20 archived files against their pins. The 876 normal Quick files and
87 pre-existing tracked changes matched their starting hashes. The
Editor exited Play Mode and restored the normal keyboard and save root.

The universal Mac package for `b204465e4` built from 32,264 pinned inputs
in 23.12 seconds with zero errors and five warnings. It reached world
startup on Apple M5 and preserved 876 normal Quick files. All six binaries
include both architectures. Gameplay in the package and physical Intel
verification remain open. [Package receipt](mac-startup-verification.json)

[Exposed plate](01-exposed-sealbark.png) ·
[Gather action](02-gather-exposed-sealbark.png) ·
[Repair bill](03-brace-the-gate.png) ·
[Closed gate](04-gate-closed.png) ·
[Reopened after Continue](05-reopened-after-continue.png) ·
[Recorded states and save pins](observations.json)
