# Portable treatments and Sella’s dressing

The shelter reader and Sella now explain two practical routes. A carried
sumpsieve pad treats ordinary poison or poison from gas. A bandage made
from one knotflax cord stops bleeding. Sella combines one pad and one cord
for two drams after her bench is repaired. The resulting dressing treats
ordinary poison and bleeding in one application. These are existing item
effects and payment rules.

## Actual GameBootstrap comparison

The seed 64 version 26 branch continued the two exact pads earned in the
[Sodden shower trial](../SoddenRainGuide20261125/README.md). The player
traveled two stretches east to Peat Works. Ordinary harvest took four
timber `9752` from the finite drying frame. The native locker menu supplied
only the two cords `9744`, leaving every other item. Walking and normal
world-map travel returned the player to Sella with 40 HP and 50 drams. Both
branches used the same earned tick-2000 save.

| Route | Preparation | Treatment |
| --- | --- | --- |
| Portable | One cord made bandage `9756` in one action at 2010. The bench remained broken and the purse held 50 drams. | The pad removed ordinary poison at 2020, then the remaining bleed took one HP. The bandage stopped bleeding at 2030. One pad, one cord and all four timber remained. |
| Sella’s bench | Two timber braced the legs at 2010. Sella took one pad, one cord and two drams at 2020 and produced dressing `9753`. | One application at 2030 removed both ordinary effects before either took HP. One pad, one cord and two timber remained. The player held 48 drams and Sella held two. |

At the same physical position, each branch supplied only the same controlled
ordinary poison and bleeding after preparation. The application time, item
use and effect transitions came from native input. The portable route can
target one condition independently, and the combined route pays for one
fewer application under pressure. Both finished at the same tick in this
trial because the bench also needed an initial repair. The gas distinction
was covered by native tests.

Both the bench Examine and Sella’s Supplies dialogue displayed the options
without advancing world time. Fresh Continue preserved both exact outcomes,
including the treatment materials, player health, bench state and purses.
This is a guided choice trial.

## Native and independent verification

The first compiling run passed 17 of 18 cases, with the missing reader copy
as the single expected failure. All 409 cases passed in the final connected
run. Direct gameplay controls used the authored pad, cord, bandage and
dressing. They checked exact supplies and costs, ordinary effects, the
remaining gas poison after the combined treatment, and no direct healing.
Independent source and recorded-play review checked the shared wording and
the exact saved state. The full project regression and a newer Mac package
remain open.

- [The shelter explains both options](01-treatment-options.png)
- [One prepared dressing in the pack](02-apply-dressing.png)

[Actual observations](observations.json) · [Exact evidence](receipt.json)
