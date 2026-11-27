# One frost lichen, two ways to freeze water

## Ordinary acquisition

A seed 65 traveler started with 3 drams and 31 HP in the Drowned
Ledger. The traveler accepted Between the Lines from Nereth Tammet,
read the real survey sheet, and reported back for 25 drams. Sari-snake
fights on the way used one of the traveler's original healing tonics.
Vessil Vesk offered one spring trap for 39 drams. The traveler traded
four gladroot seeds, two emberwheat seeds and one dried meat for 11
drams of value, then paid the remaining 28 drams. The actual trap
`15246` and the original water flask `9873` went to North Works.

At North Works the ordinary deep patch yielded one frost lichen
`17584`. The traveler left through the atlas and entered the next
surface chunk to the east. A separate, discarded return through the
yard was stopped after an encounter with the marlback patchbearer.
A snake killed a wayfaring merchant during the isolated North Works
play. The retained comparison used the paid trap, harvested lichen
and previously acquired flask with no supplied items or clock edits.

## Matched ice comparison east of North Works

Both branches start at tick 3790 on the same dry shoulder, with 29 HP,
one lichen, one unloaded trap and twelve water in the same flask. The
dry destination (41,12) offered no direct Freeze action.

| Method | What the player did | Result |
|---|---|---|
| Direct lichen | Poured one water and chose Freeze from beside the puddle. | Tick 3810. Three turns of ice, one unit in puddle `19789`, eleven water in the flask, the unloaded trap still carried and the traveler still at 29 HP. |
| Delayed spring trap | Loaded the trap with the lichen, poured one water and laid the trap on the wet tile. After a fresh Continue, stepped onto it. | At tick 3820 the armed device `19788` waited above one unit in puddle `19787` and there was no ice. One east step and a forced frozen turn reached tick 3840 with three turns of ice. The device was spent. The puddle and flask volumes stayed the same. |

The spring trap froze the traveler who stepped on it. Sari-snake hits
for four and three damage took the traveler from 29 to 22 HP across
the entry and forced turn. The trap itself dealt zero damage. A fresh
Continue kept the exact armed device before the crossing and kept
the ice, injury, Frozen and Wet effects after it was spent. A separate
fresh Continue kept the direct ice and the unspent frame. The two
branches made separate puddles in the same destination cell.

## Reader and verification

The spring trap's guide now states that a frost lichen trap on water
makes ice and can freeze whoever springs it. The new assertion failed
against the original guide, and all 120 focused cases passed with the
warning. The current inventory F1 guide showed it on the same
ordinary trap from the shared save. Reading took no game time.

Independent review checked the trade, normal inputs, saved inventory
and terrain state on both branches. The Editor was stopped and all
876 checked normal save files and 87 pre-existing tracked changes
matched after play. The crossing was player-triggered. NPC triggering
and play inside the packaged app remain open.

The universal Mac package for `2c4931acc` built from 32,282 checked
inputs with zero errors and four warnings. Apple M5 startup reached
GameBootstrap. Verification checked six universal binaries, all
186 files in each of the current and prior packages, and all 876
normal save files. [Mac startup receipt](mac-startup-verification.json)

[Trap purchase](01-trap-purchase.png) · [Direct ice](02-direct-ice.png) ·
[Armed on water](03-armed-water.png) · [Spring trap triggered](04-trap-sprung.png) ·
[The warning in Examine](05-frost-trap-warning.png) ·
[Observations and save hashes](observations.json)
