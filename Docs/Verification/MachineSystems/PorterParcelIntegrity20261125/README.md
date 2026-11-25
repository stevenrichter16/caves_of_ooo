# Original parcel recovery and ordinary storage

November 25, 2026 UTC. The recovery receipt now requires an original parcel
whose cargo stayed inside after it was packed. Opening and inspecting the
box is free. A successful withdrawal ends its eligibility for the missing
shipment receipt. Both the box and the shelf still work as ordinary storage.

Independent review of the previous delivery and timber slices verified 84
archived checkpoint files and found a case where the stack kept its original
identity after one measure was replaced. Both new cases reproduced erroneous
case closure, including one with a real save round trip. The fix records a
successful withdrawal at inventory transaction commit. Its independent
source recheck found no remaining high-priority issue. Older development
saves lack the withdrawal history and default to intact packing.

## Editor play

Seed 64 resumed the earned reported case at tick 660. This branch used
ordinary keyboard controls with no new supplied items, positions, damage or
time. It inherits the initial porter delay documented in the recognition
evidence. The previous intact branch returned the original shipment and
spent the recovered clay on the pan.

| Tick | Observation |
| --- | --- |
| 670 | The player walked to the parcel, chose it from the objects on the tile and opened it. The two measures stayed inside and the parcel remained eligible. |
| 680 | The ordinary loot picker took both measures, and the inventory action returned the same stack to the same box. The successful withdrawal remained recorded. |
| 690–720 | The player picked up the box, walked to the kitchen, put it on the receiving shelf and waited. Both measures stayed in the box. The cook kept his two opening measures, and the original case remained open. |
| 730 | The shelf reader advised keeping the original box packed. Native F5 created a four-file checkpoint. Fresh Continue matched all 23 selected state fields. |
| 740 | The ordinary shelf picker let the player take the box back with both measures. The case remained open. Native F5 preserved this branch in a separate four-file checkpoint. |

The live counter used both original measures. The two native cases exercise
the subtler replacement of one measure in the original stack. The positive
native case exercises opening and a refused take before an exact recovery.
Both isolated play sessions ended with the main Editor stopped and its save
root override cleared.

Independent recorded-play review checked both screenshots, all 23 selected
fields, eight checkpoint hashes and the earlier intact receipt. It found no
remaining high-priority issue. The main checkout retained all 86 tracked
foreign baselines and 861 selected normal save and preference files.

- [Overdue notice](01-overdue-notice.png)
- [Shelf storage after fresh Continue](02-shelf-storage.png)
- [Play and save observations](observations.json)

## Native verification

| Run | Result |
| --- | --- |
| RED | 1 passed and 2 failed as expected |
| Focused | 313 passed, one utility skip |
| Connected world regression | 615 passed, one utility skip |

The final connected run includes physical stock and searches, structural
repair, timber passage, seeded layouts, screening, rain collection and the
seasonal seed frame. The runs overlap. The inherited full-project regression
still has open failures. [Run pins](test-results.json)
