# A paid porter order can wait while the player travels

The loading marker's existing guide now tells the player that carrying
work pauses while they are away and the order stays booked. The quote,
route, fee, escrow and receipt rules are the existing ones.

## Native departure and return

The isolated trial started at the earned tick 390 checkpoint in the
Gleaners' area. Order `d613f68e99004fd8b53e9a691cd42ed8` already had
porter `2703` holding parcel `2700` at `29,20`. Inside was exact
prepared clay stack `6861` of two. The payer had 31 drams, the porter
one, and the loading marker held seven in escrow.

- The player ascended normally and spent four movement actions on the
  world map. Arrival back at the same map position was tick 480.
  The active order, porter position, cargo and three balances matched
  the departure. Fresh Continue with the world map active matched
  the same paid order and physical authorities.
- Descending returned the player to the Gleaners' area at tick 490.
  The next ordinary wait moved the original porter to `28,20` on
  the same order. By tick 750 the exact parcel was held by receiving
  shelf `2701` with the same untouched clay. The worker had eight
  drams, escrow was zero and the payer still had 31.
- A further ordinary wait at tick 760 retained the same terminal
  order, receipt and balances. The player stayed at 40 health and
  fresh Continue matched the same terminal state.

## Checks

The old native run passed 18 controls and reproduced the absent
pause line. The final connected run passed 76 cases with no
compiler errors, failures or skips. It covers ordinary routes,
interruption, refunds, loaded and unloaded schedules and custody.
In the separate packed checkpoint at tick 460, F1 on the actual
eight-dram hire option displayed the new explanation. Reading kept
the order open, the payer at 39 drams and escrow at zero.
Independent review verified 12 raw save files and the byte-identical
clay body across the booked, away and terminal archives.

Full regression and current standalone Mac play remain open.

[Observed states](observations.json)
