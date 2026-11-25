# Different loading approaches for the same clay shipment

November 25, 2026 UTC. Fresh version 23 paid delivery layouts can prefer a
short or longer loading approach. Generation checks the actual porter path
with the physical shipment placed. The existing quote and deadline rules
then charge for the real trip. The same finite box and kitchen receive the
same two measures. The supplier pays the fare. The clay's retail value and
the pan's repair requirement continue to come from their existing rules.

The seeded preference uses one route band of up to eight steps or one of nine
to sixteen. A failed preference falls back to the original admissible
placement order. If an optional kitchen hand would push a preferred route
out of its band, placement looks for another safe place for the hand.
The carried parcel trial sits alongside the earlier optional sorting and
timber choices. Broader road geometry and loaded sled trials remain open.

## Editor play

Both successful branches used fresh worlds, ordinary map travel, movement,
inspection and F5. The player kept the starting kit and 50 drams. The
branches used generated geometry, populations and ordinary time.

| Observation | Short route, seed 5 | Longer route, seed 64 |
| --- | --- | --- |
| Actual quote at tick 70 | 8 steps, 5 drams, 5 lb | 12 steps, 6 drams, 5 lb |
| Expected arrival | 170 | 210 |
| Actual handover | 160 | 200 |
| Supplier's remaining purse | 3 drams | 2 drams |
| Trade stock at tick 310 | Two opening measures and the two delivered measures | Two opening measures and the two delivered measures |
| Fresh Continue at 310 | 33 selected fields matched | 32 selected fields matched |

On the longer approach, the loading marker reader gave the expected time at
160 while the actual porter carried the box. On the short approach, the
player walked to the shelf and read the real receipt at 310. Both saves kept
the actual local actors, parcel, shelf and exact clay stock.

Two other fresh short-preference worlds had nearby hostile reedback grazers
in sight of the worker. Seeds 1 and 3 refused the initial quote. Seed 1
kept its full box, unstarted order and eight-dram supplier purse through
tick 190. These separate archives are safety observations. The successful
short comparison comes from seed 5.

- [Longer route while the box is in transit](01-long-due-date.png)
- [Short route after the shelf handover](02-short-receipt.png)
- [Quotes, routes and checkpoint receipts](observations.json)

## Verification scope

The two compiling RED cases found the opposite actual step bands in the
predecessor at seeds 64 and 1. The first candidate passed all 91 targeted
cases. The final fixture also restores a literal v22 manifest. That seed 64
control still gives eight steps and a five-dram fare. The final connected
run passed 462 executable cases with one utility skip, zero failures and
zero compiler errors. The compatibility fixture uses the existing native
manifest restore seam after correction of an initial compile failure.
[Native run pins](test-results.json)

Independent source review confirmed route measurement, one physical lot and
the fallback order. A forced absence of preferred geometry remains a future
native countercase. The full-project regression remains open.

Independent recorded-play review verified both quotes against the saved
agreements, the two fresh comparisons, both screenshots and all 16 files
across the four checkpoint archives. It found no remaining high-priority
issue. The 86 tracked foreign and 861 selected normal file hashes matched.
