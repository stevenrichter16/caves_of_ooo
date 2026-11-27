# Stored oil feeds an installed camp hearth

Keep your installed camp box beside an installed hearth and stand within
reach of both. The hearth menu can spend up to three whole oil units from
one exact stored flask, adding one fuel per oil. It names the box position,
previews the amount and leaves the same flask in the box, including when
empty. Cold coals still need lighting. Carried oil, timber and combustible
reagents remain useful alternatives under their existing rules.

## Actual connected play

This controlled GameBootstrap trial reused the retained seed-64 camp at
tick 2330, with 343/400 health and its previously supplied equipment and
materials. Only one camp-box kit was added. Ordinary camp acquisition is
not established by this trial.

The ordinary inventory set box `2731` down at 40,9 and put the existing
three-oil flask `2231` inside it. Ten ordinary waits let the exhausted
hearth cool below its existing 150-degree cooking threshold. At tick
2440 the hearth menu offered the exact stored flask and showed +3 fuel.
Choosing it reached 2450 with three fuel, no flame, and the same empty
flask in the box. No carried flask substituted for it.

One existing resin added five fuel. One existing fire-moss lit the coals
without adding fuel. At 2470 the hearth had 7.75 fuel after ordinary burn.
Fresh Continue preserved the empty flask's exact box custody, fuel, heat,
light, actor and other selected physical state. The comparison is by
entity ID; loaded enumeration order changed.

The player opened the box and took back that flask. The ordinary still
then spent four brine from `2235` and five hearth fuel, receiving two
water in original flask `2231`. At 2490 the hearth had 2.25 fuel. Two
ordinary steps returned the player to the original cell at 2510 with
1.75 fuel. A second fresh Continue matched the selected physical state,
including the reused water flask and exhausted brine flask. Health stayed
343 throughout. The full hearth Examine reader was opened in the game.

The screenshots show the stored-oil choice and the later still preview.
The latter is a preview, not an after-output image. Actual outputs and
both reload comparisons are in the observation record and raw saves.

## Verification and limits

The old runtime compiled and failed fourteen new cases on the absent
stored-oil offer; 23 controls passed. The connected native run passed all
263 cases across nine relevant fixture families. The final source, including
the updated help text, passed the same 263-case run. Countercases include
stale contents, withdrawn stock, foreign or locked boxes, aliasing,
nested vessels, replaced grid cells, independent injury and quantity
changes, destruction with a spilled flask, and saved exact custody.

Both current source and recorded play received independent bounded
review. All 876 Quick files across the normal Editor and player roots
freshly matched after isolated play. Input settings, the original keyboard
and isolated save root were restored. Durable checkpoint bodies and hashes
remain under `/Users/steven/coo-verification/checkpoints/StoredHearthOil20261127`.
This verifies one controlled Flat Editor layout and singular stored
flasks. Packaged play, broader acquisition and layout comparison,
hardware performance and all twelve whole-machine gates remain open.

[Stored choice](01-stored-oil.png) · [Reused receiver](02-reused-flask.png) ·
[Physical state and receipts](observations.json)
