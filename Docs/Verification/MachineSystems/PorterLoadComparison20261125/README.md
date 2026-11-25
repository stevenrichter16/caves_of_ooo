# Compare boxed and loose clay at the loading marker

The local hire menu shows the actual load, distance, one action, both carrying
limits and the fee. The packed load includes the box. Hard pack room uses the
player’s current hard inventory limit. The HUD continues to show the separate
strength allowance. The marker’s guide explains how the fare settles.

## Actual GameBootstrap play

Both positions began from the earlier earned seed 64 crossing branches. They
retained player `2704`, box `2700`, worker `2703`, and the two purchased
prepared-clay measures in stack `6861`.

| Tick | Observation |
| --- | --- |
| 460 | The intact route quoted eight drams and 25 steps for five pounds boxed or two loose. The worker had 150 pounds of carrying room and the player had 138 pounds below the hard pack limit. Closing the menu preserved time, purse, stock and positions. |
| 890 | The earlier physical opening gave seven drams and 23 steps for the same exact load. All quote terms fit the three visible hint lines. |
| 900 | Ordinary hire held seven drams. Free cancellation before pickup refunded all seven, left the box and stack untouched and held no escrow. |
| 900, fresh Continue | The cancellation, purse, stock and route matched. The new available quote kept the same terms and carried a different selection from the now-cancelled order. |

The opening, loaded walking and completed delivery to the well come from the
earlier [crossing trial](../PorterCrossing20261125/README.md) and [delivery
trial](../LocalPorter20261125/README.md). This comparison exercised the
current readers and cancellation from the existing earned local positions.

- [Intact crossing quote](01-intact.png)
- [Open crossing quote](02-open.png)

## Native and independent checks

The initial run passed 24 cases and reproduced both gaps. Changing the box
from five to six pounds could previously apply a stale offer at the same
fare and route. The final run passed 241 checks, with one explicit utility
skip, including the heavy-box refusal, fresh same-fare hire, saved player and
NPC delivery, and the narrow-menu text. No new compile errors or test
failures occurred. The final editor state was stopped with its temporary
save root released. The 86 selected foreign files and 861 selected normal
files matched. The earlier Mac package remains current.

Independent review checked both rendered quotes, native receipts and exact
save hashes. It decoded the booked backup and final cancellation separately,
verifying the same order and cargo, the purse and escrow exchange, and the
empty shelf. It found no remaining issue in the supported scope.

[Native and save hashes](receipt.json) · [Actual snapshots](observations.json)
