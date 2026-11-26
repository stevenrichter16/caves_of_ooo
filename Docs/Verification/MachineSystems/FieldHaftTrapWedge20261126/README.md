# A loose rough field haft can wedge an exposed trap

November 26, 2026 UTC. The existing Jam mechanism action accepts one sound
loose rough field haft when the player carries no suitable salvaged timber.
The reader names both inputs, the timber preference, and the one-action cost.
The existing jammed state persists and the wedge is spent. An already
salvageable mechanism can still yield its original single spike.

## Two ways into the same dispatch yard

The guided seed 64 trial loaded an earned ordinary save with haft `7473`,
one carried iron spike, 14 drams and 40 HP. At the first visited yard
(`Overworld.11.11.0`), a Soursprayer stepped on the armed trap at tick 2375
and died. The original trap was gone. The player kept the haft and traveled
to a second yard. Both branches below use the same tick 2435 arrival in
`Overworld.9.12.0`, with the original exposed trap at `(54,5)` and the beam
blocking the other passage at `(48,5)`.

| Branch | Actual movement, tradeoff and result |
| --- | --- |
| Jam and salvage | At tick 2605, reading and cancelling the jam action left time and stock unchanged. At 2615 one action spent the exact haft. Fresh Continue preserved the jam. The player walked onto the original trap tile at 2625 without injury, then salvaged one spike at 2645. Fresh Continue preserved the removed trap and spike stack of two. The player took the original dried meat from the crate at 2665. |
| Move the beam | After reaching the blocked opening, the player grabbed the beam and hauled it west and north for 26 ticks. They released it at `(46,5)`, crossed the west gate at 2561, and took the same dried meat at 2591. Fresh Continue kept the beam's position, the armed trap, the haft and the single original spike. |

Both routes end with 40 HP and 14 drams. Crate `16743` kept its five coins,
dagger and pipe in both branches. Dried meat `16749` moved to the player once
in each independent branch. The observed 230 and 156 ticks include travel and
the chosen work and recovery.

- [The selected jam action explains both materials](01-jam-bill.png)
- [Walking onto the jammed trap tile](02-safe-crossing.png)
- [Recovering the spike with the wedge spent](03-salvage.png)
- [Hauling the beam opened the west passage](04-beam-bypass.png)

## Verification

The initial compiling native run reproduced four missing haft cases and
passed four controls. The final focused native run passed all 248 cases.
Coverage includes legal stacks, timber priority, hidden and unsupported
mechanisms, altered and attached hafts, and refund of the exact unit after
a late interruption while independent damage persists. Independent source
and archive review checked the original mechanism, goods, custody and
both cache outcomes. The beam Continue returned the same entity state by
identity. Presentation enumeration placed two creatures in a different
order. All 861 tracked normal bodies and 86 tracked foreign-work bodies
matched their baseline hashes after the Editor stopped.

This qualifies the guided Editor play above. Broader layout discovery,
the inherited full regression and packaged gameplay remain open.
[Checkpoint and native result hashes](checkpoints.json)
