# Pack recipes playtest

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple silicon.

Opened the normal Pack & Equipment screen in two isolated copies of
earned seed 65 saves. The ordinary Crafting panel exposed its new Recipes
subtab by the P shortcut. Both cases used the normal recipe confirmation.

| Case | Before | Observed result |
| --- | --- | --- |
| Gleaners' Glade field haft | Salvaged timber 15311, tick 4190 | Shape haft consumed the one selected board and put rough field haft 15394 in the pack at tick 4200. A nearby snake acted during that turn. |
| Cinderhold missing material | Knotflax cord 15127 and no spurgrass spine, tick 7450 | The serrated edge recipe explained that it needed two spine. Attempting it kept the same selection and spent zero time and zero cord. |
| Cinderhold medical supplies | The same knotflax cord also offered a plain binding | Making the bandage consumed cord 15127 and put bandage 28411 in the pack at tick 7460. |

The captured game frames show the categorized names and availability.
The right pane shows the selected haft's Strength cap contribution and
the bandage's bleeding treatment. Toasted emberwheat shows its 3d4 HP
healing and +20 heat resistance for 100 turns, and explains why a fire
is required. Opening the pack after shaping shows the resulting haft.

- [Weapon parts](01-weapon-parts.png)
- [Food effects and missing fire](02-food-effects.png)
- [Haft in inventory](03-haft-in-inventory.png)
- [Unavailable material work](04-missing-material.png)
- [Medical supplies](05-medical-supplies.png)

Unity Edit Mode passed all 555 focused cases. The set covers exact source
ownership, output-only names, right-pane effects, mineral infusion's
resistance preview, repeated stale attempts, missing still requirements,
one turn per committed preparation, controller subtab navigation, Forge
and Brew, material processing, cooking, and proofing. Two existing
renderer assertions now read the selected color from the shared ledger
palette, which is where the shipping renderer reads it. Independent
source review checked the shared command path and the final wording
for whole-stack cooking. Independent gameplay media review checked the
observed recipes, rejection and outputs against the in-game receipts.
The Editor footage covers two preparations and a blocked material
process. Station-assisted work and cooking retain their existing native
test coverage.

## Mac package

The universal app at `/Users/steven/coo-verification/builds/mac-2af18031c-20261128/CavesOfOoo.app`
packages runtime `2af18031c`. The build completed with zero errors and four
warnings. On Apple silicon the standalone Recipes tab made the haft and
F5 saved the result at tick 4200 in an isolated slot. A fresh process used
Continue and displayed the haft in the pack. Both processes stopped after
verification. The five retained apps matched their prior hashes, and the
normal preferences, 880 Quick files and 87 unrelated paths matched their
initial bytes. Physical Intel execution remains untested.
