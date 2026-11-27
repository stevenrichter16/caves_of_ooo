# Natural rain on an exposed crop bed

During a local player turn, a supported regional shower brings an
eligible young crop up to ten units of moisture. The soil records
the spent rain window once it actually supplies the water. A roof,
flooded soil or an already adequately watered crop leaves the dose
unused. The existing wet-soil appearance and crop growth clock read
the same moisture meter. Examine names rainfall that reached the crop.
Ordinary water and Conjure Rain remain available.

The retained seed-65 western Morrowfast allotment loaded dry at tick
520 during a real regional shower. Two ordinary steps gave the existing
young knotflax two growth units and eight moisture. Nearby ripe crops
stayed ripe. I harvested the ripe hearthbulb, then planted one carried
emberwheat seed in its exposed bed at tick 550. The new crop had
nine moisture and one growth unit at 560. Fresh Continue preserved
both exactly and kept the same bed and crop.

I used the same unplanted save to wait until the shower had passed
before planting the same seed in the same bed. At tick 610 the
rain-planted branch showed four moisture, six growth units and visibly
wet soil. The fair-weather branch started dry at the same tick and
location. Ordinary waiting in the first branch used up the ten units and
paused further growth. It did not create a harvest. Both branches
kept the player's 40 HP and 50 drams.

Four native cases reproduced the missing behavior on the prior
source. One control passed. All 136 targeted cases passed on the
final source, including roofs, normal watering, inactive time,
same-window replanting, reload, eviction, and rain during a frozen
turn. The covered-bed comparison was native test coverage. Both
Editor branches used ordinary inputs without an item grant, clock
edit or changed roof. A brief Editor delay cleared during harvest.

All 12 archived save files were pinned. The 876 normal quick saves
and 87 pre-existing tracked files matched their starting hashes.
The Editor returned to Edit Mode and cleared the private save root.

The universal Mac package for `d90a01796` built from 32,262 pinned inputs
in 21.32 seconds with zero errors and five warnings. Startup loaded the
world on Apple M5 and preserved the same 876 normal saves. Each of the
six binaries includes both architectures. Packaged gameplay and
physical Intel testing remain open. [Mac receipt](mac-startup-verification.json)

[Examine after rain](02-shower-fed-the-new-emberwheat.png) ·
[Wet at 610](03-same-plant-on-wet-soil.png) ·
[Same bed planted in fair weather at 610](05-same-bed-planted-after-the-shower.png) ·
[Dry again at 670](04-same-plant-after-the-soil-dried.png) ·
[Recorded states and pins](observations.json)
