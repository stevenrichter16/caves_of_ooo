# One knotflax stand for cord or seed

November 25, 2026 UTC. Roadmap requirement 33 now has one bounded player
choice at the optional Morrowfast stand. During the early growth phase,
an available sound cutter can harvest one cord in one action. Cut now
and this stand yields no seed later. Leave it standing and the usual
late bundle becomes available. Both use the same fixed source, custody
and spent state. There are no new saved fields.

## Actual Editor play

Seed 65 used the ordinary guided starting pack with its one dagger.
The player walked out of Gleaners’, across the world map into western
Morrowfast, and stood beside the actual generated knotflax stand. No
items, obstacles, creatures, player stats or site actors were supplied
or removed. Both paths use the same prechoice save at tick 480, in
`Overworld.2.6.0`, with the actor at (59,8) and stand `7200` at (60,7).

| Tick | Ordinary actions and observed results |
| --- | --- |
| 480–490 | Native Look and Enter selected the early cut. The exact stand disappeared. One cord `7201` entered the pack, its weight rose from 12 to 13 pounds, and the starting dagger `2702` stayed in the pack. |
| 490 | A fresh Editor Play and Continue kept the cord, dagger, actor and depleted stand. |
| 500 | The cord's ordinary inventory preparation made bandage `7202` and spent `7201`. The separate available binding and snare rows were not executed. |
| 9600 | On the spent branch a controlled clock advance changed the growth phase. The exact bandage remained and the stand stayed absent. |
| 9600–9610 | A separate Continue of the uncut tick-480 save was advanced to phase 1. Only the usual ripe gathering row was offered. It exhausted `7200` and supplied one bundle `7201`. |
| 9620 | The public frame `7198` winnowed the bundle into plantable seed `7202` plus one fair-weather chaff `7203`. The early cord did not appear in this branch. |
| 9620 | Fresh Editor Play and Continue matched the exact seed, chaff, dagger, actor and absent stand. |

The comparison advanced the clock directly in each owned save copy.
The cuts, gathering, recipe, winnowing and saves used ordinary game
inputs. Seed planting and fire use of the same product types have their
own prior play evidence. This trial establishes the distinct uses of
one source, with broader discovery and seasonal frequency still open.

- [Early cord choice](01-early-cord.png)
- [Late seed choice](02-late-seed.png)
- [Prior planting and watering](../SeasonalSeedFrame20261125/README.md)
- [Prior fair-weather chaff use](../FairWeatherChaff20261125/README.md)

## Native checks and review

The first compiling test run passed 19 of 23. All four failures were
missing early offers or their consequences. Independent review spotted
that an unsafe output creation callback could publish a cord before
the receipt could refuse it. A second run reproduced exactly that
failure with 23 of 24 passing. The early route now requires the same
creation-safe output template used by other material work, and verifies
the exact item after creation. The unsafe definition leaves the late
harvest available. A changed clock window, broken or claimed cutter and
stale location invalidate the early quote without spending the stand.

The final run passed 197 focused Unity checks with zero failures, skips
or compiler errors. It covers existing harvest, material processing,
preparation, rain collection and the four early/late cases plus the
unsafe output countercase. The captured menu uses the final plain-text
hint. It was opened and cancelled without moving the clock or the stand.
Independent review checked all 16 save bodies, both fork histories, exact
serialized stock ownership, the two screenshots and the final native results.
The 86 selected foreign and 861 selected normal file bodies matched.
The inherited full-project regression and packaged play of this change
remain open.
