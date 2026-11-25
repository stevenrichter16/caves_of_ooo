# Return the missing shipment by hand

November 25 UTC, 2026. Local candidate for the physical recovery requirement.
Independent source and recorded-play review and publication remain open.

After failed transit, the original complete shipment can reach the same shelf
through ordinary pickup and hand carrying. Orven can unpack it when he is
available at the shelf. Stocking and case closure commit together. The ledger
still records failed transit and keeps the settled fare. A player who shelves
other compatible clay can still use the shelf as storage.

## Editor play

Fresh Continue began from the earned reported tick 660 of seed 64, game
`e20820bd3ab048c892516952957e1e56`. This branch added no new supplied items,
positions, damage or effects. It preserves the previously earned branch and
inherits its declared porter delay before the combat.

| Tick | Observation |
| --- | --- |
| 670 | The ordinary ground picker took crate `6896` with both measures `6897`. The real body `6899` stayed at 24,16. |
| 680 | One player step put the shelf in reach. The box's ordinary inventory action deposited it on shelf `6895`. The two measures were still inside until Orven acted. |
| 690 | A normal wait let Orven take the same measures into trade stock. The saved receipt and case closure were stamped at 680. Examine reported the original shipment recovered. The empty crate stayed on the shelf. |
| 690 | From ordinary chat and trade, the player chose the actual recovered lot, bought it for 12 drams and left the opening clay stock available. |
| 720 | Two ordinary steps reached the pan. The repair spent both recovered measures. The pan's Examine reader showed the restored paid meal service. This run exercised repair and inspection. The public oven remained available. |

After recovery, supplier, porter and loading marker purses stayed at seven,
one and zero drams. The player purchase transferred the price to Orven through
ordinary trade. Stock included the opening two measures before and after the
purchase. No meal was commissioned in this branch.

F5 and fresh Continue matched the selected stocked and repaired states at
690 and 720. The pan's multi-page reader initially kept F5 from running. A
readback confirmed the open reader at the same tick. Closing it and using F5
produced the correct four-file checkpoint before Stop.

- [The recovered shipment receipt](01-shelf.png)
- [The repaired pan and available meal service](02-pan.png)

## Unity verification

The first compiling RED passed 22 of 25. It exposed the missing recovery and
two later failures caused by reputation left over from the native death fixture.
Restoring the saved reputation after the helper isolated the intended RED at
24 of 25. The next focused pass passed 63 of 63. Cases cover original and
unrelated parcels, a full receiving merchant, settled fare and repeated
reconciliation. The final expanded run also checks an original box repacked
with unrelated compatible clay. All 470 expanded cases passed, with zero
compiler errors, skips or inconclusive cases. The final source pins match 33
project and test-clone files.
