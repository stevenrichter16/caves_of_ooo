# Wellmeet prices while the pan scales work

Wellmeet's two-silver-sand patch offers the same local weighing rate as a
working iron repair for the life of the patch. Both use the existing 0.97
price factor once. The temporary service follows the saved repair clock and
the actual pan scales. It does not complete the Salt Master's lasting report.
The trade screen explains the working scales. Combined local and reputation
rates at this market can narrow the buy and sell spread to parity.

## Actual GameBootstrap play

The branch started from [the ordinary quarry route](../OrdinaryQuarryLoop20261125/README.md)
at tick 2640 with 40 HP, three drams and two earned silver sand
`19769`. Guided normal world-map travel brought the same player to
Wellmeet. All three quotes were read in Sennel Saltstep's actual trade
screen for his exact three healing tonics `22026`.

| Tick | Observed result |
| --- | --- |
| 2990 | Sennel `22025` quoted a barter value of 129 for all three tonics with the scales still low. |
| 3010 | The actor beside scales `22167` read the temporary bill for free and used the ordinary Patch action. It consumed both silver sand and set expiry at tick 3910. |
| 3020 | F5 and fresh Continue kept the exact working scales and spent sand. The lasting report was still unrecorded. |
| 3410 | After reaching the moving merchant again, the same three tonics were quoted at 125. The actual trade screen named the working pan scales. |
| 3910–3920 | The player waited out the remaining 490 ticks on the world map, returned at tick 3920, and saw 129 for the same three tonics. Fresh Continue kept the expired state and ordinary rate. |

A dirt gnome hurt the player during the first merchant chase.
An ordinary carried tonic restored 15 HP from 16, then another hit
left the player at 26.
The player kept 26 HP through the working and expired quotes. The three
merchant tonics were never bought or split. Sennel's measured stock and
the player's three drams remained the same. Actual play used the sand
patch. The matching unreported iron repair and exact expiry before a
settlement update are covered by native tests.

## Verification

The first compiling RED showed the active patch still quoted the baseline.
The permanent iron path passed. A second compiling RED demonstrated how
reputation could combine with the new local factor to reverse the spread.
The final connected run passed 146 cases with no compiler errors. It
includes the physical owner, cost, expiry, both repair approaches,
unclaimed lasting condition, buy and sell formulas, and the combined
Tare Book and reputation rate. Independent review covered source and
recorded gameplay.

- [Ordinary quote](01-ordinary-rate.png)
- [Working scales quote](02-working-rate.png)
- [Observations and archive hashes](observations.json)
