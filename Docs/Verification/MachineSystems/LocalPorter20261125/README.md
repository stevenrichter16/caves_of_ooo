# Paid local clay delivery at Gleaners'

November 25 UTC, 2026. Working-tree candidate over `ec827d4bf`.
Native tests, Editor play and fresh-load comparisons are complete for the
scenarios below. Independent final source and recorded-play review is pending.

## Route and terms

The existing two-measure clay box and receiving shelf support manual carrying.
A fresh Gleaners' district can also generate a loading marker beside the cellar
and a local porter. The marker quotes one packed box, one real destination and
a declared fare. The quote tests live weight allowance and a short dry walking
route. It refuses a busy or movement-blocked worker and a hot or otherwise
unsafe route. The actual path is recalculated on worker turns as obstacles and
standing room change. The fare stays agreed and the destination stays fixed.

The marker holds a single saved order and the actual fare. Pickup moves the
same reserved box into the worker's pack and pays one dram. Receipt moves the
box to the ordinary shelf and releases the balance once. The saved reservation
blocks general pickup, opening, unpacking, deposit, drop, throw, drag and trade
while the job is active. A full or locked shelf pauses the handover. A displaced
receiver sends the intact box back. Cancellation refunds the whole fare before
pickup. After pickup the worker returns the same box and keeps one dram. If the
worker dies, ordinary death custody governs the goods and the held fare returns.
The prototype limits work to the active zone and one fixed route.

## Native verification

| Run | Result |
| --- | --- |
| Order scaffold RED | 0 of 4 passed. The four scenarios could not find an actionable booking row. |
| First order implementation | 35 of 35 passed. |
| First save-fixture attempt | Compile failure. The test supplied a base zone manager where save capture requires an overworld manager. |
| Expanded native order and save tests | 45 of 45 passed, including both pre-pickup and in-transit save branches. |
| Targeted inventory, trade, custody and district regression | 249 of 249 passed. |
| Final interruption and inactive-zone scope | 251 of 251 passed. Zero compile errors, skips or inconclusive results. |
| Reusing old hire and cancel actions | Both new cases reproduced the defect before the fix. |
| Order-bound actions and the targeted regression | 253 of 253 passed. Zero compile errors, skips or inconclusive results. |

The runs overlap. The initial and updated clone source pins are recorded
separately. The broader verification covers
the empty and full receiver, both cancellation phases, moved receiver return,
ordinary death spill/refund, incapacity, inactive-zone pause, stale offer and exact fresh escrow, along with
existing pickup, container, trade, drag and district cases. The project's
inherited full regression remains open.

## Editor play

Ordinary seed 64. Game `f312a9030c2f493baa646ef2d79eb9fd`. The fresh district
generated the marker at 29,19 and the carrier at 29,20. Orven sold two prepared
measures for 11 drams at tick 190. Native inventory actions packed the actual
stack `6861` into box `2700` at 380. No world object or currency was granted.

- At the marker, the native action quoted five pounds, an estimated 25 steps
  and eight drams. Acceptance and the worker's following turn moved the box
  into porter `2703` at 390. Purse 31, porter 1, marker escrow 7. Fresh Continue
  at 390 preserved those values, the order and both levels of physical custody.
- The player waited, then walked to the shelf. The porter moved independently
  through at least the observed positions 27,16 at 450 and 40,10 at 660. At 670
  shelf `2701` held the same box and clay. Purse 31, porter 8, marker escrow 0.
- The player collected the box at 680 and unpacked the same two measures at
  690. The repair consumed them and made the well usable at 700. Drawing water
  reported cool water without advancing the tick. Fresh Continue matched the
  selected ledger, pack, box and repaired-well states at 700.
- A separate Continue from 390 chose the marker's free cancellation action.
  At 400 the same packed box was on the original tile, the shelf was empty,
  and the seven held drams had returned. Purse 38, porter 1, marker escrow 0.
  Fresh Continue at 400 matched those values and identities.

Both actors retained their starting HP in this paid route. Nearby warden combat
changed the surrounding danger. The play evidence does not cover a hostile
interruption after pickup or a worker death. The native fixtures separately
verify incapacity and death after pickup. The quote menu was
reached by selecting the loading marker from the objects on its shared tile.

Four screenshots show the quote, in-transit status, handover and the restored
well. The five external checkpoint archives hold all twenty verified save
files. The stopped Editor has no isolated root, seed override or reload lock.
The selected 86 foreign bodies and 861 normal bodies match their stored hashes.
No package or branch publication occurred for this draft. NPC-owned shipment
automation and overdue searches are subsequent work.

## Cancel and rebook on the updated source

An earlier hire action could rebook a returned parcel, and a cancel action
could apply to a later booking at the same marker. Hire actions now carry the
last order ID along with the quote, and cancel actions carry the active order
ID. The ledger retains the last ID after closing an order.

The amended Editor loaded the original tick 390 carrying checkpoint. The live
cancel action named that order and requested a return without advancing time.
By 400 the marker had refunded seven drams and returned the same box and clay.
The new eight-dram quote booked that box under a different ID at 410. The
porter's inventory held the original contents, the payer had 30 drams, the
porter had two and the marker held seven. Fresh Continue reproduced the selected
tick 410 state exactly. The original delivery and repaired-well play above
used the preceding source. The native tests cover submitting the old actions.
[New quote after the return](05-rebook-returned-box.png)
