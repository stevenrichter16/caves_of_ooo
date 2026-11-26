# Use quarry pale-salt against a Charred Husk

The player continued the ordinary seed 2 generation 26 quarry route.
Its same dagger 2716 had the tier-two pale-salt edge made from the
screened packet. The pack still held the earned hand sieve and two sand.
The equipment reader described the four-point bonus. The player equipped
the actual dagger at the Cinderhold forge, then walked to Spivenor.

An earlier reconnaissance fork descended eight levels beneath Cinderhold
at 40 HP. It found no suitable target. The Spivenor fork traveled on the
world map, took the ordinary down stairs twice and approached the natural
Charred Husk 31264 in `Overworld.16.4.2`. The player's HP went from 40 to
29 during the approach. The husk had 24 HP at tick 3430, adjacent at
`8,20` to the player at `9,19`.

The ordinary attack used the equipped dagger and dealt three points of
base damage and four points from its edge. The old combat log stopped at
the base result of 21 HP. The target had 17 HP and the existing
enhancement diagnostic recorded the exact four-point bonus. The husk's
counterattack left the player at 18 HP. F5 and fresh Continue at tick
3440 kept the equipped dagger, its bonus and both combatants' health.

## A combat log that shows the landed bonus

The replay used the exact tick-3430 pre-attack save with the updated log.
The base hit rolled four, leaving the husk at 20 HP. The same edge added
four and the log reported 16 HP, matching the husk's actual state. Fresh
Continue at tick 3440 kept the new line, the husk's 16 HP, the equipped
dagger and the player's 21 HP. The base and counterattack rolls differed
on the fresh load. The comparison measures the feedback for the same
enhancement and encounter, with a single landed hit in each branch.

The shared Pale-Salt and Choir-Iron feedback reports damage actually
applied. It respects a veto and the target's remaining health. A player
attack on a visible local creature can show the line. The native cases
cover both edges, lethal hits, independent damage callbacks and hidden
or non-player encounters. Seven message cases first failed. The final
connected run passed 379 of 379 native tests. The latest Mac package
predates the change.

- The original log reported only the base strike. [Original combat log](original-combat-log.png)
- The updated log shows the landed bonus and remaining health. [Combat bonus](visible-combat-bonus.png)
- Exact health and equipment through fresh Continue. [Original observations](original-observations.json) · [Corrected observations](corrected-observations.json)
- Test and archive hashes. [Receipt](receipt.json)
