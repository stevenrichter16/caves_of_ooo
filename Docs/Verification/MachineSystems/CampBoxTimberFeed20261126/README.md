# Feed a hearth from stored timber

Stand within reach of a finite hearth and your installed camp box.
The hearth's C menu can spend one salvaged timber directly from the
box. The menu shows the source position and actual fuel gain, up to
ten. Its ordinary pack feed remains available with carried timber.
Feeding adds fuel. Cold coals take a separate lighting action.

## Matched game branches

The seed 64 expedition checkpoint supplied the camp and used a high
capacity adventurer with 400 maximum health. In the ordinary pack, two
Put in camp box actions moved all 24 original timber to installed box
`2150`. Its own stack rules combined `2267` to 20 and retained
`2287` at four. No timber stayed in the pack. The save at tick 1840
had 9.5 fuel in hearth `2147`, no movement and no spent timber.

| Approach | What happened in the game | Timing |
| --- | --- | --- |
| Retrieve from box and feed from pack | Took original stack `2287` of four. Feeding reduced it to three in the pack. The 20-unit box stack stayed in place. Hearth reached 19 fuel after ordinary burn. | Two actions to tick 1860 |
| Feed directly from box | Spent one from original stack `2267`, leaving 19 and four in the box. The pack remained empty of timber. Hearth reached 19.25 fuel after ordinary burn. | One action to tick 1850 |

The same fuel accounting explains the quarter-unit difference. Direct
feeding skips the extra turn of burn during retrieval. The ordinary
hearth offered no feed row when the original pack held no timber.
The new row named the box location and selected one exact stack.
The active fire was already hot. It cooled on the ordinary turn.

From the direct-feed checkpoint, the connected still used the same
hearth for two normal distillation actions. At tick 1870 there were
four brine left in `2235`, two water each in `2236` and `2237`, and
8.75 fuel left. The remaining timber was still 19 and four in the box.
Fresh Continue matched those exact flasks, both stacks, the fire,
location and health. Independent review checked the two captures and
16 raw save files across the four checkpoints.

## Checks

The old runtime compiled and failed all seven new cases on the absent
action. The final connected run passed 144 native Unity EditMode cases
without failures, skips or compiler errors. Adversarial cases cover
the fuel cap, stale replacement, foreign and full storage, independent
injury and fuel changes, and a box destroyed during the transaction.
The exact remaining stack spills as usual and gets back the spent
unit if the action is interrupted. The box stays destroyed.

The fixture establishes local interaction, exact stock and the existing
fire's work. An initial test attempt hit insufficient disk space.
Lossless filesystem compression of owned verification logs kept their
paths and file hashes and provided enough space for the native retry.
Ordinary camp acquisition, whole current regression, the current Mac
package and hardware profiling remain open.

[Hearth choice](01-feed.png) · [Hot work](02-work.png) ·
[Observed states](observations.json)
