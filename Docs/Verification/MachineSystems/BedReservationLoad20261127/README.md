# An empty bedroll is usable again after Continue

NPCs reserve a bed before they reach it. The save kept the occupied
flag, while the arrival callback that could release it was transient.
Continue now checks whether a living creature on that exact bed has
the matching sitting effect. An orphaned flag clears. A real sleeper
keeps the claim, and that sleeper's own effect releases it on standing.
Canceled idle offers now release beds and chairs even when the NPC was
already standing on the destination cell when it made the reservation.

## Matched ordinary play

The seed-65 traveler continued from the fair-weather sumpsieve harvest
with 20 of 42 HP. Ordinary map travel reached Brine Camp and 26 dry steps
reached the unowned bedroll `12536` at `(14,11)`. At tick 6530 it had no
other occupant. The original Sleep action called it in use and spent
neither health nor time. A separate branch rested at the fixed campfire
one cell west and recovered 22 HP over 60 ticks.

The updated runtime loaded the exact same tick-6530 save. The bedroll
was available. Ordinary Sleep at the same position restored 22 HP
and advanced time to 6590. A fresh Continue kept 42 HP, the same available
bedroll, the nearby gin frog, the packed still, the remaining clean
water, and both harvested pads and seed. Neither action needed new
equipment or supplies.

[Empty bedroll refusal](01-empty-bedroll-refusal.png) ·
[Existing campfire alternative](02-existing-fire-rest.png) ·
[The restored bedroll](03-bedroll-restored.png)

## Verification

The old runtime failed three of four focused cases. With the bed repair
and the prior idle-goal path, the two cancellation tests still failed
for a bed and a chair. All 173 focused cases passed with the full fix,
including completed sleepers, stale effects, canceled offers, player
rest and binary reload. The existing nearby-hostile rule and the idle
offer tests remain covered. The matched live run used an empty bedroll.
Controlled native tests checked the sleeper and cancellation cases.

Independent reviews verified the exact occupancy checks, both archived
save sets, the matched before and after state, and the screenshots.
The Editor exited Play Mode. All 876 normal Quick files and 87 earlier
tracked edits matched. [Observations and save pins](observations.json)

## Mac package

The universal Mac package for `20c2b16e7` built from 32,276 checked
inputs with zero errors and five warnings. GameBootstrap started on
Apple M5. Both apps matched their 186-file records, and all 876 normal
Quick files matched. Changed startup preferences were restored.
The matched recovery playtest ran in the Editor.

[Startup receipt](mac-startup-verification.json)
