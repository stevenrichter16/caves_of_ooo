# Firm footing reopens a porter route

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple silicon.

The freight route now recognizes live, ordinary grit on dry ground and
on a loose slippery film. The route still checks actual movement cost,
occupants, pools, traps, heat, cold, charge, clouds and other ground
layers. Move revalidation uses the same check after before-move callbacks.
This affects new quotes, loaded trips, returns and shipment searches.
Grit protects against slipping for its existing duration. An untreated
film becomes an obstacle again when the grit expires first.

## Matched playtest

Both private branches continued the same retained seed 65
ordinary Gleaners' delivery at tick 1060. The porter already held the four-pound box with one measure
of lining clay under the seven-dram order. In each branch the test supplied
one ordinary coarse grit and a 40-turn loose oil film at the porter's
next step, (27,19). Both branches kept the original worker, cargo, booking,
money, route geometry and positions. [Original delivery](../GleanersOneMeasure20261127/README.md)

| One ordinary turn | Porter at tick 1070 | Tile and load |
| --- | --- | --- |
| Wait | Stayed at (27,20) with a blocked-route note | Oil 39 turns, one grit still in pack, same box and clay |
| Scatter grit west | Stepped onto (27,19) | Oil 39 turns, firm footing 11 turns, grit consumed, same box and clay |

The porter stayed at 28 HP and the player at 40 HP in both branches.
Fresh Continue kept the exact treated step, oil, grit and cargo across
19 selected fields. Ordinary waits in that fresh process let porter
2697 put the original box 2694 and its exact clay on shelf 2695 at tick
1340. The agreed fare was seven drams. Oil still had 12 turns at the
treated tile and its grit had expired. The film and grit were controlled
inputs for this comparison.

## Verification

The compiling RED passed 13 controls and reproduced both blocked-route
failures on dry grit and treated oil. All 202 targeted native cases then
passed. Countercases preserve route refusal for expired traction, embers,
heat, acid, a pool and a trigger. The suite includes quotes, orders,
shipment receipt, grit payment and ordinary NPC terrain navigation.
Independent source and gameplay reviews checked the normal game inputs,
matched branch outcomes and retained save hashes. Editor play and its
private save root were closed. Normal saves and foreign source changes
were checked separately in the receipt. Gameplay verification used the
Editor on Apple silicon.

- [Scatter one grit](01-scatter-grit.png)
- [Porter and firm footing after Continue](02-continued-porter-and-footing.png)
- [Untreated tile after waiting](03-untreated-1070.png)
- [Treated tile after scattering](04-treated-1070.png)
- [Matched state and archive evidence](observations.json)
