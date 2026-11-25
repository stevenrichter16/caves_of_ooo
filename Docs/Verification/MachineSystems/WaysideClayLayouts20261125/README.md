# Two ways to supply the wayside kitchen

November 25 UTC, 2026. Local candidate for the first seeded logistics contrast.
Independent review and publication remain open.

Fresh Spread version 19 chooses one optional allocation at the established
wayside kitchen. Some seeds offer a sorting lot a few steps from the kitchen.
Others keep the existing paid shipment with a supplier, carrier, crate and
receiving shelf. Ordinary opening stock and the pan and oven stay available in
both. A saved version 18 keeps its shipment policy.

The local allocation places one known three-pound lot with two measures of
fire clay and one measure of coarse grit. The site must have an accepted
kitchen graph, the actual pan and an ordinary safe approach. It may omit the
lot when placement fails. The saved item can be picked up and spent once.

## Editor play

Both branches started fresh at Gleaners' with 40 HP and 50 drams. Native map
travel and ordinary ground steps reached the established wayside kitchen.
The kitchens used the same batch-pan recipe and reported the same restored
meal service after two compatible measures were spent. The routes were run
once each, so their turn totals describe the observed paths.

| World | What happened |
| --- | --- |
| Seed 2, local sorting | The player examined lot `6953` at tick 160 and picked it up at 170. An Editor fixture then supplied one canonical hand sieve `6958`. The lot, player position, currency and products came from the normal game. Screening at 180 created two fire clay `6959` and one coarse grit `6961`. The pan repair at 220 spent the clay. The player had 50 drams and retained the sieve and grit, with the cook's opening clay still available. |
| Seed 64, paid shipment | The original crate `6895` contained two prepared clay `6896` at arrival. The carrier handed it over by tick 150 and earned five drams from the supplier's budget. The cook stocked the actual lot by 170. The player bought it through ordinary trade for 11 drams, then spent it on the pan at 190. Opening stock remained available. The branch used the ordinary starting equipment. |

F5 and fresh Continue matched the selected repaired states at 220 and 190,
including the saved layout, original and spent stock, player purse, retained
grit, and shipment custody where applicable. The same meal service appeared in
both free Examine readers. No meal was commissioned. Each branch had separate
saves and used its normal generated route. The supplied sieve bounds the claim
to what a player can do after bringing that tool.

- [The nearby sorting lot](01-sorting-lot.png)
- [The pan restored with screened fire clay](02-local-pan.png)
- [The paid physical handover](03-paid-handover.png)
- [The pan restored with imported lining clay](04-paid-pan.png)

## Native tests

Both compiling RED cases reproduced the missing sorting lot on seeds 2 and
1729. The first pass was 37 of 37. Native coverage checks the paid seed, the
two local seeds, a retained version 18, omission with unavailable sorting
content, and conservation when the lot is picked up and the zone revisited.
All 518 expanded Unity cases passed with zero compiler errors, skips or
inconclusive cases. That run includes the prior shipment and recovery cases,
general size screening, and the original allotment source.
