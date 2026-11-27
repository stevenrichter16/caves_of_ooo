# Gantry's guest-cloth dries gear in a shower

A repaired guest-cloth at Gantry gives travelers a free place to air out a
carried damp item during a local shower. Stand directly beside the hung cloth
on dry footing. The menu and Examine show the selected item and its moisture
before and after. Each action removes up to 15 percentage points. Existing
fair-weather shelves still remove up to 25. Rest, hearth drying, ground
evaporation and ordinary exploration remain available.

## Actual play

The seed-65 traveler continued the ordinary repaired Gantry save at tick
2060. They went to western Morrowfast and chose the rainy seam's gather
action at tick 2390. It yielded the exact clay-and-grit packet at 25% moisture.
The same packet was still damp on return to Gantry in a shower. Cancelling
the first quote and reading the cloth used no time. Two ordinary actions at
the original pole dried the packet from 25% to 10% to zero by tick 2880.
Fresh Continue kept the same dry packet, hung cloth, 40 HP and 31 drams.

A separate follow-through supplied one ordinary hand sieve after the
dry-only checkpoint. The normal weight rule refused it until the player
set aside both the full flask and watering grimoire. The player picked up
the exact sieve, screened the dried packet into two fire clay and one coarse
grit, and scattered the grit through the normal underfoot action. Fresh
Continue at tick 2920 kept the two clay, exact sieve, two set-aside items and
11 turns of firm footing. This branch used a supplied sieve. The gathering,
travel, weather, drying and dry-only reload all used the ordinary save.

## Verification

The prior source passed 13 cases and failed the five expected missing-action
cases. The final connected native filter passed all 336 cases. Those cases
include the original Gantry and North Works actions, hidden-pole refusal,
late changes to footing and weather, independent moisture on rollback,
reader parity, and one native test where drying the same striker makes
ordinary torch lighting succeed.

All 876 normal quick saves and 87 pre-existing tracked changes matched
their original hashes. The private save root cleared and the main Editor
returned to Edit Mode. The full current regression and physical Intel
execution remain open. [Recorded states](observations.json)

[Rain seam](01-natural-clay-seam.png) ·
[Drying quote](02-cloth-moisture-quote.png) ·
[Examine](03-cloth-examine.png) ·
[Firm footing after Continue](05-footing-after-continue.png)
