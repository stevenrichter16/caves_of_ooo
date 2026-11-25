# A physical clay shipment to Orven's kitchen

November 25 UTC, 2026. Local candidate for requirement 25. The route has native
and Editor evidence. Independent final source and recorded-play review and
publication remain open.

## The shipment

A compatible fresh kitchen can have one clay supplier with eight drams and
one box of two actual prepared measures. The supplier spends their own turn
and purse to hire the same kind of worker that serves player orders. One dram
is earned when the worker takes the parcel. The balance remains on the
loading marker until the worker deposits the same box at the kitchen shelf.
Orven stocks the exact clay when he can reach the shelf. The empty box stays
there and its reservation ends. The cook's opening clay and food stay in
their original stacks. The fare does not enroll either supply worker in
ordinary merchant purse replenishment.

The fittings are optional and stay within the local zone. Manifest version 18
admits them after the ordinary kitchen placement commits. Existing generated
kitchens and version 17 worlds keep their original layouts. The shipment is
one-time, with no route project, food shortage, calendar restock or cross-zone
simulation. Shipment intake waits when the cook cannot reach the shelf.

## Evidence from the Editor

The first fresh seed 64 branch hired and carried the original box, then
deposited it on the shelf at 180. Orven sat two tiles from that shelf and the
lot remained boxed at 220. The updated placement checks reach from both his
home tile and his existing chair. An earlier approach also ran into the east
hedge. The ordinary entry autosave was reloaded to approach from the south.
Those two draft archives remain distinct from the corrected playthrough.

The corrected branch used seed 64, game `18046b6f322b4f5f9c608d5a3b198a4b`.
The player started at Gleaners' with the normal kit, travelled by the world map
and walked in from the south.

| Tick | Observation |
| --- | --- |
| 70 | The supplier had eight drams and crate `6897` held stack `6898` of two measures. Orven had his original five food lines and opening clay `6860`. |
| 160 | The porter was visible with the same crate in his pack. The worker had one dram, the marker held four and the supplier had three. The player's purse remained 50. |
| 170 | The porter put the exact box and clay on shelf `6896`. The worker had five drams and the marker held zero. Orven still offered only his opening clay. |
| 190 | Orven's trade pack had the original stack and the exact delivered stack as separate two-measure entries. The shelf held the empty box. |
| 200 | Native dialogue and Trade transferred `6898` for 11 drams. The player held the two measures and Orven still offered `6860`. |
| 210 | The native repair spent both imported measures and restored the batch pan. Examine showed its ordinary paid commission terms and the free oven alternative. |

Separate F5 and fresh Continue comparisons matched the tracked state at 160
and 210, including exact selected owners, both nested cargo levels, currency,
receipt and pan condition. The restored pan reader was exercised. Commissioning
a meal remains outside this play branch. The trade list displays the two clay
stacks under the same item name. Its footer shows the quantity and price.

- [Visible carrier at the kitchen](01-carrying.png)
- [Handover in the game log](02-receipt.png)
- [The delivered stack selected in trade](03-trade.png)
- [The restored batch pan reader](04-pan.png)

## Native verification

The tests cover stock before and after handover, exact identity, a same-ID
replacement, capacity interruption, legitimate ordinary trade, fresh save
loads, a nonrefilling shipment and payer and worker purses. They also cover
the generation and geometry of the initial kitchen.

| Run | Result |
| --- | --- |
| First fixture attempt | The test assembly could not access the draft method. No test result. |
| Stocking before implementation | 2 of 4 passed. The two positive receipt cases failed. |
| Stocking implemented | 5 of 6 passed. Legacy restock credited a paid porter from 4 to 100 drams. |
| Booking before wiring | 7 of 8 passed. The opted-in supplier did not book the crate. |
| End-to-end fixture and retained porter tests | 39 of 39 passed. |
| First generated hook | 445 of 446 passed. The shipment never installed because the hook ran before the kitchen placement committed. |
| Generation hook corrected | 9 of 9 passed. |
| Chair-aware placement | 9 of 9 passed. |
| Final targeted delivery, trade, kitchen and generation regression | 446 of 446 passed. Zero compile errors, skips or inconclusive results. |

The checks overlap. The full inherited regression, other layouts, shipment
recipient death recovery, hardware performance and Mac delivery remain open.
Thirty source and metadata files match the final clone. The four checkpoint
archives contain 16 verified save files. All four private Editor sessions
finished, and their temporary roots were removed. The 86 selected foreign
files and 861 normal files match the previously recorded checksums.
