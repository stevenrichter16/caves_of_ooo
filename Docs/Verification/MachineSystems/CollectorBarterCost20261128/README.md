# A Tatterjay’s exchange names the one portion it costs

A visible, willing Tatterjay with a real carried find makes the exchange
legible. The action names one portion of the selected food and the exact
kind of item being offered. A player with no suitable food sees the
requirement before trying to pay. The offer closes when the bird loses,
deposits or trades its find.

## Ordinary play

The earned seed 64 save started at Sill at tick 580 with two dried meat.
Ordinary atlas travel reached the newly generated collector site at
(9,8). The bird went around a walled court to pick up a loose cudgel.
The traveler followed around the south wall and waited within reach.
At tick 880 the visible Tatterjay 9891 held the original cudgel 9897.

The old action named the entire two-unit stack. Accepting it used one
meat and one turn and transferred the original cudgel. The updated
action reads “exchange one dried meat for cudgel.” It gives the same
result at tick 890, with one meat still in the pack and 40 HP.

In the separate no-food replay, the player set down the original
two-unit stack through the inventory. The disabled exchange explained
that one food was needed. Attempting it kept the food, cudgel, health
and clock in place. After the paid replay, fresh Continue kept the one
meat, exact cudgel and completed exchange.

[Missing food](needs-food.png) ·
[One-portion quote](one-portion.png)

## Native checks

The preview and existing transaction use the same eligible payment
and exact carried item. Both preview branches require a visible bird.
Saved costs and item custody use the original one-action exchange.
Tests include visibility, no eligible food, successful single-unit
payment, stale quotes after deposit, same-blueprint decoys and rollback.
A malformed test assertion was corrected before the old-source run.
The corrected baseline passed three controls and failed the missing
offer check. A separate native replay removed the barter part during
rollback and exposed a stale carrying phase. The fix kept the original
collector carrying its restored find. The final focused run passed 92
cases and skipped one explicit save-contract generator.

The trial ran in the Unity Editor on Apple M5.

[Exact custody, save pins and regression counts](observations.json)
