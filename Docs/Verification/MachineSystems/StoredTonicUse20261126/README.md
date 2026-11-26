# Drink a healing tonic from carried storage

The item actions on an expanded satchel can use a compatible simple
healing tonic directly. The action spends one unit from the exact
selected stack and applies the ordinary tonic payload. Taking the
stack to the loose pack remains available. Either use costs one
action.

## Actual GameBootstrap play

Both private Sill branches started from the same tick-160 archive.
The audit set the player's health to 22 to make healing observable.
Original satchel 2714 held exact tonic stack 2712 with two units and
the colored `field meds` label. The neighboring meat stack held two
units and the combined pack weighed 38 pounds.

- The player expanded the satchel and chose Drink on the tonic child.
  At tick 170 the player had 40 HP and the pack weighed 37 pounds.
  One dose stayed in the same satchel with its original label. The
  meat was untouched and no tonic appeared in the loose pack.
- In the control, Take to pack moved both doses to the loose pack at
  the same tick and weight. The ordinary loose Drink action then
  spent one dose at tick 170. It left 39 HP and 37 pounds.
- Fresh Continue in both branches preserved health, weight, the
  labeled remainder and the respective owners. Each tonic rolled
  ordinary healing independently. Nearby NPC combat also diverged
  between the private branches. One villager died in the unpack
  branch, so the comparison is scoped to player payment and use.

## Verification

Two native UI tests failed on the previous missing Drink row. Four
adversarial cases reproduced loss of a dose with a late dropped
holder or presentation of invalid healing dice. The current
connected native run passed 606 cases without failures, skips,
inconclusive cases or compiler errors. The run includes last-unit
removal, cancellation, quantity changes, late locking, normal
inventory actions and existing tonic behavior. Independent
source and play review matched all twelve archived files.
The current slice admits simple healing payloads. Other stored
consumables still use their ordinary unpack-and-use path.

[Native satchel action](01-drink-from-satchel.png) · [Observations](observations.json)
