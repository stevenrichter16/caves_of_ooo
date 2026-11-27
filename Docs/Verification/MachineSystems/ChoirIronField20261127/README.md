# The ore reader explains choir-iron's forge use

Examine now explains that the public forge takes one unit of ore and
one enhancement slot on a compatible unequipped item. The ore reader
describes the registered effect and shows the actual item action when
the forge and item are ready. Away from a forge it names the missing
requirement. The existing action owns payment and execution.

## Mining, infusion and Continue

In seed 65 an ordinary traveler harvested the finite vein at (3,3)
one chunk west of Cinderhold. The existing vein gave two ore in stack
`28404` and worsened Rot Choir standing. The player returned to the
public tinker's forge, unequipped dagger `2703` and selected its exact
infusion from the ore's menu. The action advanced one turn, spent one
ore and added the first enhancement to the original dagger. A nearby
sari-snake landed a critical hit during that turn. The player equipped
the dagger and kept it and the last ore through travel and a fresh
Continue. The separate [iron supply request](../RegionalIronRewards20261126/README.md)
establishes a paid alternative from a different ordinary playthrough.

## Combat comparison

- The original enhanced dagger landed a reactive counterstrike for
  three piercing damage against a nonfungal sari-snake, with no
  extra damage event from choir-iron.
- The fungal check used a supplied equivalent enhanced dagger in an
  isolated old Lampwell save. The rotling was an original creature in
  that save. An ordinary attack did seven piercing damage and left it
  with one HP. Choir-iron's six-point bonus applied the remaining one
  damage and killed the rotling. The combat log reported the one point.

The ordinary seed-65 branch kept the earned dagger through all three
eligible Lampwell floors but found no Fungal target. The Grovelands
surface rolls list tier-one rotlings and the existing population floor
excludes them on every authored Grovelands surface. Broader ordinary
fungal encounter availability remains open.

## Verification

All three new cases failed before the guide was added and passed
afterward. The broader run passed 236 of 237 checks. The one failure
expects older still-baking wording. A separate run reproduced the
same assertion on unchanged main. Independent review checked the
new guide, receipts, exact save bodies and same-strike damage events.
All 876 checked normal save files and 87 existing tracked edits
matched after the Editor playtest.

The universal Mac package for `9dae6a99c` built from 32,282 checked
source inputs with zero errors and four warnings. Startup reached
GameBootstrap on Apple M5. Verification checked six universal binaries,
all 186 app files in each of the current and prior packages, and all
876 normal save files. The ore and combat playtests ran in the Editor.
[Mac startup receipt](mac-startup-verification.json)

[Ore harvest](01-harvested-choir-iron.png) ·
[Forge guide](02-forge-infusion-guide.png) ·
[Infusion receipt](03-infused-dagger.png) ·
[Controlled rotling hit](04-rotling-hit-controlled.png) ·
[Native results and observations](observations.json)
