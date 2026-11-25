# Orrit’s mended trap and a natural Stump catch

Fresh Morrowfast gives Orrit one mended fish trap beside his ordinary goods.
The mending changes the exact item’s name, description and saved base value.
It uses the same carry weight, placement rules, catch limits and pond
supply as the usual kit. The one unit is staged during cold generation and
excluded from recurring stock. The saved settlement stays available through
unloading, so a sale cannot cause a new opening allocation. An existing
saved shop keeps its original stock.

## Actual GameBootstrap route

The guided seed 64, version 26 run used the ordinary starting pack, purse
and health. Native movement reached Orrit’s stall at tick 470. He sold the
single 14-pound mended trap for 35 of the starting 50 drams. Exact trap
`6748` entered the pack and the two coarse grit remained in Orrit’s stock.

Native travel reached a natural Stump cascade at tick 530. The first
approach crossed shallow water and applied Wet. The player set the same
trap down east onto `(41,8)`. A sky-sari approached, so the player went back
to the world map and waited by ordinary turns. A normal return at tick
1180 placed the player back beside their trap. It had one fish ready.
Taking the fish moved exact perch `9426` to the pack. The pond’s saved
stock on anchor `7667` fell from a capacity of four to three remaining.

The player took the empty trap back up, keeping its identity and saved
value. A sari-snake and a sky-sari landed hits during retrieval, leaving
22 HP. The player returned to Morrowfast and ate the perch for eight HP.
At tick 1270, the player had 30 HP, 15 drams, the same trap and both
original tonics. Orrit’s original two grit remained and the trap had not
been replaced. Fresh Continue matched the exact player, kit and stock as
well as the saved three remaining fish in Stump. This is an optional
food source with ordinary travel and wildlife costs.

## Native and independent checks

The compiling first run passed 19 checks and failed on the missing
opening kit. A subsequent trial used an entity-only test round trip for
a carried object. The corrected test uses the supported save graph.
All 76 connected cases passed with zero compiler errors. They cover the
actual shop allocation, standard kit and price, ordinary purchase, exact
save data, unload protection, restock, missing or altered optional stock,
and the existing camp and Stump controls.

The independent review checked source, the recorded native screens and
the exact archived save bodies. This is one guided route on a fresh
current layout. Wider seeds, player-led discovery and package delivery of
this change remain open.

[Orrit’s offer](01-morrowfast-offer.png) · [Catch menu](02-natural-catch.png) · [Selected observations](observations.json) ·
[Save and test hashes](receipt.json)
