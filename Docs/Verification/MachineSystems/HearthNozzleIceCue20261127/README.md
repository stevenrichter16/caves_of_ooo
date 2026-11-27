# Visible ice in the hearth nozzle menu

The existing hearth nozzle names an icy target in both its inventory
actions and the ground interaction menu. It quotes the same five fuel
and one action. Heat can thaw the ice into temporary meltwater. Other
coatings or opposing cold can change the outcome. A quote goes stale
if ice appears or disappears before payment commits. It then costs
nothing. Unseen ground offers no ice cue.

## GameBootstrap play

This Flat Editor trial continued a supplied seed-64 camp at 2740 and
added one nozzle. The original earned seed-2 nozzle has published
acquisition evidence, but its temporary save is gone. No town stock,
price, loot odds or deployment rule changed in this slice.

At (41,9) the player used the ground menu to pour one water south from
flask 2234, leaving two in the flask. The same menu spent one of seven
frost lichen at 2760. Puddle 2733 still held one measure, and the ground
showed three turns of slippery ice. The nozzle named that exact patch.
Cancelling kept the ice, tick and 9.25 fuel. Reopening and selecting the
row reached 2770 with four fuel, after the five-fuel cost and the normal
quarter-unit burn. The ice disappeared and five turns of meltwater
remained over the same one-measure puddle. The new water layer added no
recoverable liquid.

A fresh Continue kept the same nozzle, fuel, puddle, meltwater duration,
flask and lichen. The menu offered no second nozzle use at four fuel.
Walking one step away and one back kept 343 HP and aged the wet patch
normally. A separate fresh Continue from the same frozen checkpoint used
Flaming Hands south. At 2770 it had the same ground result and nine hearth
fuel. Only the ordinary quarter-unit burn was paid by the hearth in that
branch. The tool and remaining materials stayed available in both branches.

[Visible ice](01-frozen-ground.png) ·
[Five-fuel quote](02-ice-heat-quote.png) ·
[Nozzle thaw](03-thawed-by-nozzle.png) ·
[Spell comparison](04-thawed-by-flaming-hands.png)

## Evidence and limits

The old implementation passed 77 controls and failed four cases for the
missing cue and stale quotes. The final run passed all 323 targeted tests
across eight suites. Separate countercases cover an unobserved tile,
ice changing before or during payment, the reverse change on a dry quote,
and a valid heat action whose fuel cancels cold while leaving ice intact.
Source review found no blocking issues. Independent gameplay review
matched all 12 checkpoint files, all four images and the exact frozen
backup in both completed branches. The initial Editor session reloaded
while importing the change and was discarded before setup.

The retained Sill market has actual hearth, striker, spike and lichen
stock. The combined normal price exceeds the retained purse. The ordinary
purchase-to-thaw chain and a natural icy target in a lawful camp area
remain open. This run verifies an existing player-created ice patch,
the supplied kit, ordinary actions and a saved comparison. All 876
normal Quick files and 87 pre-existing tracked changes matched, and
Play Mode and the private input and save context were closed.

[Recorded states, test receipts and save hashes](observations.json)
