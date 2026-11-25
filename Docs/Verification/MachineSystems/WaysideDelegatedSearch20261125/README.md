# A paid kitchen hand can check an overdue delivery

November 25, 2026 UTC. In an eligible fresh delivery layout, Orven can send
one kitchen hand to check the loading point. The hand starts at an existing
safe standing position. One two-dram wage transfers from Orven's purse with
the saved assignment. The hand follows the local path and reports in person.
Orven keeps the kitchen open. He can search personally if the hand is
unavailable or misses the agreed return time. The same assignment cannot
earn another wage.

The optional worker uses the existing kitchen's protected court lanes and
border access rules. If placement or staffing fails, the kitchen and original
shipment still work. This trial covers one seeded site, one paid search and
one recipient. The existing supplier remains a separate potter.

## Editor play

Two fresh seed 64 worlds used ordinary map travel from Gleaners' to the
version 22 kitchen. Both branches applied one 60-turn Rooted effect to the
existing porter after his real pickup at tick 90. This controlled delay let
the search and report play out before the parcel arrived. No staff, goods,
money, positions, time or player effects were supplied.

| Tick | Final branch |
| --- | --- |
| 70 | The kitchen hand stood at 26,18 with his saved home there. The cook had 35 drams, the hand had zero, and the shipment was still at the loading point. |
| 310 | The hand was walking toward the loading marker and held two drams. Orven stayed at the kitchen with 33 drams. Examine named the hand and showed the agreed return time. The report was still unset. |
| 340 | Ordinary chat opened Orven's trade while the hand was away. The opening clay stock remained available. No purchase was made in this branch. |
| 580 | The hand had checked the loading point and reported at tick 480. He stood at his original safe position. The same porter still held the parcel, and the inquiry stayed open. Fresh Continue matched all 23 selected fields. |
| 810 | After the imposed delay wore off, the same porter delivered the box. The real receipt closed the inquiry at 760. Orven stocked its exact clay lot alongside his opening clay. The hand still held two drams and stood in his usual spot. |

An earlier guarded revision had an independently saved outbound checkpoint at
310. Fresh Continue matched its selected positions, route state, parcel custody
and purses. The report and its physical handover played out in that branch too.
All four earned checkpoints were archived before the isolated sessions stopped.
The final branch did not exercise body recognition or a missed return in play.
The inherited personal-search branch covers recognition of the actual body.
The new native cases cover a missed return, an unavailable cook, invalid
employment, shortage, duplicate payment, and a search saved mid-route.

- [The pending assignment and return time](01-pending-report.png)
- [Orven's trade while the hand is away](02-ordinary-trade.png)
- [The hand's returned report](03-returned-report.png)
- [Observed state and save receipts](observations.json)

## Native and independent checks

The initial RED produced one intended failure and two passing refusal
controls. Focused runs passed 159 and 194 executable cases. The final
connected run passed 392 executable cases with one utility skip, zero
failures and zero compilation errors. These runs overlap.
[Native run pins](test-results.json)

Independent source review caught the lost-worker fallback, report disclosure,
blocked corners and kitchen approach risks. A bounded recheck cleared those
findings after correction. The geometry check preserves border access and
protected court cells. It does not establish identical shortest paths from
the north entrance for every seed.

Independent recorded-play review matched all 16 archived save files and
the selected fresh comparisons. It confirmed trade while the hand was away,
the exact wage, return, physical report and the same parcel's later stock
receipt. It found no remaining high-priority issue. The 86 tracked foreign
and 861 selected normal file hashes still matched.
