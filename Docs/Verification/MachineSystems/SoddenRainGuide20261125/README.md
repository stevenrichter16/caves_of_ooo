# One shower portion for the sumpsieve bed

Sella’s shelter has a small public channel beside its dry outdoor bed. During
a mild shower the player can direct a single local portion into that bed.
The action uses the ordinary crop top-up. It takes one action, spends the
window’s saved portion, and uses no carried water. The ripe bed, hand watering,
Conjure Rain and passage through the shelter remain available.

## Actual GameBootstrap play

Fresh seed 64 used the ordinary default kit, money and health. The player
started at Gleaners’, used the world map to reach the real Sodden shelter and
walked the dry approach to `(50,10)`. No inventory or world objects were
supplied, and waits used normal input.

| Tick | Observed result |
| --- | --- |
| 230 | Fair weather. The real channel menu offered no watering. The other sumpsieve remained ripe, and the ordinary dry approach was passable. |
| 610–620 | Normal waits reached the first shower. The native menu offered one portion. Using it spent the actual saved allowance. The selected crop gained 39 remaining wet rounds after the action’s one round of growth. Pack, purse and health were unchanged, and the other crop stayed dry and ripe. |
| 1010 | The same crop had used its 40 wet rounds, reached stage one and grown 16 rounds into that stage. The same shower still gave no second offer. |
| 1030 | The player stepped one cell east and cast Conjure Rain, learned from the ordinary starting Watering Grimoire, for a second top-up. The first channel use remained spent. |
| 1100–1130 | Normal time ripened the exact crop. Ordinary harvest put two pads and one seed on the soil. Pickup stacked the two pads in the pack. The separate original ripe bed remained untouched. |
| 1130, fresh Continue | The exact two pads, seed, untouched neighboring crop and one spent shower portion persisted. |

Native cases also compare with a real carried flask, preserve older worlds,
exercise late failures and reload the exact fitting and crop while the same
window is spent. Editor play used the grimoire for its second watering. The
harvested pads and seed remained in the pack for later use.

- [Shower action](01-shower-action.png)
- [Exact harvest and pickup](02-harvest.png)
- [Final Examine reader from the untouched tick-610 checkpoint](03-final-reader.png)

## Native and independent checks

The first compiling test run failed four missing-guide cases and passed the
old-world control. The final broader run passed 318 checks with one explicit
generator skip. The sole failure matches the previously reproduced missing
Peat Works rig model. The new channel uses an approved shallow wooden-tray
mesh and the native Sodden Stop model checks passed. No new art assets were
created. Old manual watering retains its result and render notifications.

Independent review verified all twelve saved bodies, the backup links, the
actual cell occupants and the native log. The final Examine reader agreed
with the earlier native menu and left that untouched checkpoint unchanged.
The selected 86 foreign and 861 normal file bodies, seed settings, save root
and seven views matched their prior values after the final reload.
The current Mac package predates this change. The broad inherited full-project
regression is still open.

[Exact test and save hashes](receipt.json) · [Actual snapshots](observations.json)
