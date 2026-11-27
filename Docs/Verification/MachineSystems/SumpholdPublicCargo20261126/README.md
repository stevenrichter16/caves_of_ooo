# Sumphold public cargo

Fresh version 30 can stock one existing boatyard crate with two pounds of
salvaged timber. The chosen finger depends on the world seed and the final
usable ground. Placement checks the route with the load, the receiving
berth, and continued access to ordinary services. Earlier cached sites
keep their original furnishings. Taking or losing the stock does not
reset the allocation.

The 72-pound shell counts toward hauling. Its public storage allowance
is eight entries and 24 pounds of loose goods. Beside the released,
unlocked crate, ordinary **Open** offers its exact contents and the pack's
**Put in wooden crate** action accepts eligible carried goods. The
transfer rechecks both owners and the exact destination object or stack.

## One-board withdrawal

A released crate now offers a one-board withdrawal for eligible salvaged
timber stacks. The exact stack remains under the crate with one less board.
Opening and hauling remain available.
[One-board playtest](../CargoOneBoard20261127/README.md)

## Actual play

Two private GameBootstrap runs selected the ordinary Duelist and took
the world map from the Reference Glade to Sumphold. The driver selected
the seed and tracked exact items. Walking, hauling, opening, depositing,
taking and shaping used game controls. Both branches kept 40 HP and
50 drams.

| Seed | Choice | Result |
| --- | --- | --- |
| 64 | Haul the 74-pound western crate and use it near the road | From beside crate `7470` at `(20,19)`, six moves took it to `(22,15)` behind the player at `(22,14)`. Speed was 71 while hauling and 100 after release. At tick 385, two rations went into the crate, bringing it to 78 pounds. |
| 64 | Use the timber in the field | Fresh Continue kept the parked crate and both stacks. The player took the original two timber and closed the pickup menu at tick 395. The ordinary field recipe used one timber at tick 405 and made rough haft `7473`. The other timber stayed in the pack and both rations stayed in the crate. |
| 65 | Carry the timber and leave the eastern crate | The player walked to `(64,19)` and opened crate `7479` at `(65,19)`. Taking original stack `7480` put both timber in the pack at tick 380. The 72-pound empty crate remained at its source. |

The parked, worked and emptied states were checked through fresh
Continue. The exact relevant owners, positions, items, quantities, time
and health matched. Unrelated crate enumeration order could differ.
The seed 65 loaded turn has native movement coverage. Actual play there
used the loose-timber alternative. The earlier scout walked both paths
with an empty crate left in place.

## Verification

The first compiling RED run had 11 expected failures in 16 cases. The
second had seven expected failures in 73, covering missing generation
and loss of exact custody during a transfer callback. The final targeted
Unity run passed 467 cases and skipped one opt-in save-contract generator.
It includes both loaded turns, stale commands, protected fixtures,
limited pack space, equal-mass substitution, current saves and old layouts.
An independent reviewer checked the exact transfer source, screenshots,
observations and all 16 archived save files. The full inherited regression
and current packaged play are still open. The two Editor routes did not
measure frame times or other hardware.

[Loaded turn](01-loaded-turn.png) · [Putting rations into the crate](02-deposit-menu.png) ·
[Taking and shaping the original timber](03-shaped-haft.png) ·
[Taking in place at the eastern crate](04-east-source.png) · [Observations](observations.json)
