# Felt on cave rubble and a second steam catch

November 26, 2026 UTC. The wander fix and the condenser extension use the
existing movement, hearing, save, source stock and crop rules. They make
one old route comparison possible again and add one new receiver choice.

## Pale Stalker and the felt comparison

The guided cave route continued the earlier hush-lane save at tick 540.
It had supplied felt, an elevated carry limit and 400 HP. Normal movement
and five down stairs reached `Overworld.10.11.5` at tick 1030 and found
the natural Pale Stalker `21359`. No creature, gear, health, time, noise
or pursuit was changed by a diagnostic. The route used read-only cell
and actor observations to guide native input.

At tick 1350, both old-runtime branches started from the same fresh
Continue. One bare step stamped noise and the felt step left the old
noise stamp intact. The stalker chased in neither branch. On the live
felt branch at tick 1360, the latest one-step wander child was finished.
A fresh Continue from that exact save showed it unfinished. The old
goal stored completion in a private field. The named-field save format
writes public fields, so Continue could make the already-spent child
take another movement action before the hunter scanned for sound.

The same goal now saves its completion and still uses the original
action and parent rules. New and pending children both support age zero.
Old saves omit the field and use the existing default. The fixed
runtime opened the old tick-1360 archive and observed that default.
The new fork was saved in play after laying one more strip and waiting.

| Fresh fork from tick 1480 | Landing at tick 1490 | Player noise | Stalker 21359 |
| --- | --- | ---: | --- |
| Ordinary step | Bare rubble at `15,12` | 1480 | Chase and KillGoal targeting `2126` |
| Step onto felt | Rubble at `15,11` with strip `21393` | 1320 | Wander with no target |

Each fork started at `16,11` with the same actor, 400 HP, the same
stalker at `7,6`, 900 energy, no target and an already-finished
wander child. Both landings were dry and eight cells away by the
hunter's distance rule. The bare branch put the stalker into active
pursuit and the visible log then described searching its last position.
The game reseeds AI random streams on load. The receipt measures the
actual targets and positions, without assuming matching wander paths.

The felt branch opened the recovery reader for `21393` without spending
a turn, then took up the strip in one turn. The pack stack `2136` rose
from two to three. The other two cave strips stayed laid and the removed
device was absent. At fresh tick 1500 the actor, held units, other strips,
old noise and the stalker's untargeted state matched. This is one natural
stalker and a single matched step. It measures neither a whole crossing
under pursuit nor damage avoidance. Earlier surface and sounding receipts
remain separate evidence.

## Same flask from the Sodden vent to the crop

The starting tick-2080 save came from the ordinary earned condenser
route and had 19 HP. The player carried condenser `9208` and flask
`11821`, which held one water from natural vent `17302`. The connected stock holder `17301`
had three of four catches. The new C menu quoted 1/12 to 2/12 and the
F1 reader showed the selected action. Cancelling left time, stock and
the flask unchanged. The ordinary selection took one action, filled
the same flask to two and dropped the shared catch stock to two.
A local bandfrog bite took two HP. Fresh Continue at 2090 kept the
same outcome, including 17 HP.

Native map travel reached Sella's shelter at `Overworld.15.7.0`.
A direct east approach was fatal from snake attacks at tick 2210.
It ended without watering or replacing the earned catch archive.
A second attempt from that archive walked south of the road and used
the existing Calm spell once. It reached the dry sumpsieve `18291`
at `50,9` without taking damage on the way. The ordinary crop menu
spent one water from the same flask at 2270. The crop had 39 remaining
moisture ticks and the flask had one water left. The adjacent ripe
crop stayed unchanged. The watering turn itself exposed the player
to attacks and bleeding, leaving 4 HP. Fresh Continue matched the
crop, flask and health. This qualifies the material path and shows
that the field route carries a real risk. It establishes no harvest or
safe approach.

## Verification

The corrected native RED run compiled and passed 95 of 100 cases.
The five expected failures were the missing condenser rows and the
lost completed wander state. An earlier fixture omitted the loaded
brain's active zone. A retry then ran out of space copying the native
test symbols. The final connected run passed 359 of 359 cases with
zero compile errors, failures, skips or inconclusive results. It also
checked distinct fill-level choices through the vent menu, an old
selection, a late veto and preservation of independent water changes.
Only the staged water and catch are refunded.

The exact paths and SHA-256 values for 44 archived save files, native
results and four real screenshots are in [observations.json](observations.json).
Independent source review checked the public save field, exact water
receiver and scoped rollback after correction. Independent recorded-play
review checked all 44 archive files, the native runs and the screenshots.
The crop reload has a longer log observation window. Its physical fields
match, with no whole-observation equality claim.
The space recovery removed identical disposable compiler scratch
copies from idle clones. It also compressed two older verification
outputs in place while preserving their paths, bytes, mode, ownership
and modification time. The filesystem assigned fresh storage metadata.
The main Editor ended stopped with an empty save override and a clean
scene. Selected checks matched all 86 foreign and 861 normal files.
The current Mac package and the full regression remain pending for
these changes. No performance or hardware qualification is claimed.

[Condenser selection](01-steam-fill.png) · [Bare-step pursuit](02-cave-bare.png) ·
[Crop menu](03-crop-water.png) · [Recovery reader](04-felt-reader.png)
