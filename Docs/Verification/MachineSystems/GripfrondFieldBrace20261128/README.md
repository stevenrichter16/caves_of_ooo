# Field gear names the brace and its footing

The Gripfrond Wrap points to **Brace against wall** beside a solid
wall or stone outcrop. Ironshod Boots point to **Plant feet** in the
pack while standing on firm ground. Both inspections give the
one-action cost, three-turn stance, one shove or pull and movement
release. The descriptions explain the limits of each option.

## Stump and Ditch

Ordinary movement reached ripe Gripfrond crop 31017 in Stump.
Gathering gave the exact wrap 31019 and a seed. The nearby young
crop remained in place. Wearing the wrap let the traveler brace
against pink stone outcrop 30316. A fresh Continue kept its exact
handhold and three turns. A blocked step passed time and kept the
stance. The next successful step released it.

The same worn wrap reached the Ditch hedgerow at (22,6). Its
Interact menu offered no bracing action. A return to Stump fetched
the original Ironshod Boots 31000 from unlocked crate 30985. The
worn boots supplied the ground brace independently of a handhold.

Both recorded Slam comparisons began at tick 8779 from the same
saved state. The traveler stood at (13,11) with 42 HP and the same
boots and wrap. The same DitchMate 33194 stood at (12,11) with its
original cudgel and a ready Slam. One branch planted the feet.
The other used ordinary waits. The AI chose its real Slam at
different times in the two branches.

| Choice | Position after the observed Slam | Stance outcome |
| --- | --- | --- |
| Plant feet | (13,11) | Exact boots absorbed the shove and the brace was consumed |
| Wait without bracing | (16,11) | Slam pushed the traveler three cells east |

The braced hit still stunned the traveler. A separate later cudgel
strike caused 17 damage in the braced branch. The comparison
measures displacement without attributing HP or stun differences
to the boots. A separate attempt recorded the three-turn brace
expiring and a subsequent Slam moving the traveler normally.
Fresh Continue preserved the consumed stance and held position.

[Braced with boots](01-braced-slam.png) · [Unbraced displacement](02-unbraced-slam.png) ·
[Wrap guide](03-wrap-guide.png) · [Boots guide](04-boots-guide.png)

## Checks

Both native RED cases failed on the old action wording after their
eligibility controls passed. The final suite passed 230 with one
explicit save-contract helper skipped. Controls include the outcrop
and hedge, pack-only action, standing liquid and a slippery oil film.
Both revised item readers were opened from the earned pack without
spending a turn. Independent source and gameplay reviews agreed.

This was native Flat Editor play on Apple M5. The recent Mac app
predates the guidance. [Exact observations](observations.json)
