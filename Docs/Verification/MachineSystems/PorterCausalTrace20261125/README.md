# One order across the actual wayside shipment

The existing diagnostic channels record the committed booking, pickup and
settlement under the saved order ID. Merchant intake records the exact
stock arriving under that ID. A changed pause reason can be inspected once
without adding a record each turn. The game’s action, custody and service
rules stay the same.

## Actual GameBootstrap play

The initial state was the original earned tick-60 Continue backup from the
[wayside trial](../WaysideShipment20261125/README.md). The backup was copied
without altering its world data. The player used normal movement to reach
the kitchen approach. The existing diagnostic channels were enabled for
the isolated trial and restored afterward.

| Recorded tick | Committed result |
| --- | --- |
| 70 | The supplier paid five drams for eight steps. The booking captured crate `6897`, shelf `6896` and both measures in stack `6898`. |
| 70 | Worker `6894` took the crate and earned one dram. The loading marker held the remaining four. |
| 160 | The same crate reached the shelf. The worker received the remaining four drams and the marker held zero. |
| 170 | Orven moved exact stack `6898` into his trade pack, alongside original stock `6860`. The empty crate remained on the shelf. |
| 190 | Native F5 retained both stacks, the crate and the cleared escrow. The player's purse held its original fifty drams. |
| 190–250, fresh Continue | Continued stock and payment matched. Six ordinary waits let the worker return to his post without duplicating any of the four committed records. |

All four records share one exact order ID. The world turns in the records
come from the actual transaction boundaries. Saving and reopening stock
does not publish a fresh payment or intake for an already completed order.
This branch verifies the transaction evidence. The earlier wayside trial
contains the actual purchase and batch-pan repair from the same cargo.

## Native and independent checks

The first compiling run passed eighteen cases and failed only the missing
committed records. The final connected run passed 145 cases with one explicit
save-contract generator skip. A preview, a rolled-back booking and a refused
stock transfer yield no committed receipt. A valid retry publishes the same
physical order and stock once. Existing player and NPC delivery cases also
passed. No new fields or information are added to the saved world.

Independent source and archive review decoded the actual crate and exact
merchant stock from the immutable tick-60 and tick-190 saves. It matched the
four recorded events to the real owner and purses. The selected 86 foreign and
861 normal files matched their initial hashes. The latest startup-verified
Mac still predates these changes and the broad regression remains open.

[Exact save and native run hashes](receipt.json) · [Actual transaction trace](observations.json)
