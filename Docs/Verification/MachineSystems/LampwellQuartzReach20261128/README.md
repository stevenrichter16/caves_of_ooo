# Lampwell quartz lights a passage

An ordinary seed 65 Continue started on the third depth at tick 8890.
The traveler climbed the existing stairs and walked to vein 68557 on
the second depth. Harvest at tick 9250 removed the finite vein and
put two Glow-Quartz in one pack stack. The traveler had a small innate
light and carried no worn light.

The player stood at (71,18), opened Interact to the west and chose
**Crack quartz light**. At tick 9280 one mineral became a stationary
light at (70,18), with 29 of its 30 turns left. At the same player
position, 49 sampled cells became brighter and the 166 visible cells
stayed the same. At the source the brightness rose from 0.55 to 1.00
on the game's 0–1 scale. At (68,18), two steps beyond the source, it
rose from 0.42 to 0.78. The ordinary game frame showed the light where
it was placed. The second piece remained available for other uses.

[Before placement](01-dark-before.png) · [Interact choice](02-placement-choice.png) · [After placement](03-lit-same-position.png)

The traveler walked onto the lit tile, saved and used Continue after
a fresh Editor launch of GameBootstrap. It restored the same crystal,
one unspent piece, 27 HP, 28 turns left, and all 210 measured cells.
Twenty-seven more waits left one turn. The next normal wait at tick 9570
removed the light and dimmed the same 49 cells. The square below the
traveler returned to 0.62 from their own light. The spent piece stayed spent.

[Fresh Continue](05-fresh-continue.png) · [Light expired](06-expired-same-position.png) · [Recorded values](observations.json)

Independent source and gameplay review checked the shared light
calculation and the stationary comparison. This is an ordinary test
of existing runtime in the Editor. It covers one vein and one active
passage. This pass did not add code, run a new package or measure
performance. The existing [Posy trial](../QuartzSupply20261122/README.md)
shows a different use for one piece at the Setting Bench.
