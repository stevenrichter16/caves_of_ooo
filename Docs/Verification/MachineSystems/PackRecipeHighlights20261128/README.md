# Recipe stat and effect highlights

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple silicon.

Opened Pack & Equipment, Crafting, and Recipes through ordinary keyboard
input after using Continue in two private copies of earned seed 65 saves.
The real ledger displayed the following effects beside product names,
with craft availability in its own column.

| Selected recipe | Inline readout | Status at this location |
| --- | --- | --- |
| Rough field haft | Strength cap 1 | Available |
| Toasted emberwheat | Heals 3d4 HP / +20 heat resist (100 turns) | Unavailable, no usable fire |
| Serrated edge kit | +1 penetration / 20% bleed on damaging hit | Unavailable, two spurgrass spine missing |
| Knotflax bandage | Stops bleeding | Available |

In Gleaners' Glade (turn 4190), pressing Enter on toasted emberwheat
explained the missing fire and kept the same inventory, menu and turn.
In Cinderhold (turn 7450), the guard lashing showed both signed modifiers.
The plain knotflax binding had no highlighted effect. Navigating the
Cinderhold examples consumed no turn. The right pane showed the complete
selected effect and recipe requirements in both branches.

- [Haft and food recipes](01-haft.png)
- [Food effects and missing fire](02-food.png)
- [Weapon effects and missing materials](03-weapon-effects.png)
- [Bandage and plain binding](04-bandage.png)

Long equipment and combined output rows show the complete effects with
output or component attribution in the right pane. The inline space
shows whole summaries that fit. Independent review checked the display
width, the effect conditions and the product attribution. Standalone
packaging is pending.

Three focused controls passed and three new assertions failed on the
original presentation. The final focused Unity Edit Mode run passed all
679 cases, including crafting, cooking, meals, proofing, traps, mineral
infusion, field binding and the inventory ledger. Both Unity branches
closed their isolated save locations. The 880 ordinary Quick files
and 87 unrelated tracked paths matched their pre-play hashes.

The 16-second gameplay recording is in
`/Users/steven/coo-verification/runs/PackRecipeHighlights20261128/recipe-effects-preview.mp4`.
