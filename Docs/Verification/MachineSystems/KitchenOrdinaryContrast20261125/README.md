# Two ordinary paths to Orven’s field meal

Actual GameBootstrap sessions used two saved wayside layouts. In seed 2 a fresh
v26 world yielded a local sorting lot and the player earned the sieve through
ordinary trade and crafting. Seed 64 continued the earned paid delivery in a
retained v18 layout. Both paths spent two compatible clay measures, paid the
same cook for the same work and used the resulting meal. Each broken kitchen
already offered free ordinary cooking.

## Seed 2, local sorting

The v26 run started at Gleaners’ with 50 drams. Ordinary actions supplied the tool, clay and food. Navigation followed a guided route. A purchase that selected an entire lot was excluded by resuming the exact tick 310 Morrowfast arrival autosave. The recorded route uses the successful purchases below.

| Tick | Observed result |
| --- | --- |
| 140 | Three public ripe rows at Gleaners’ yielded three emberwheat sheaves. |
| 570 | Ordinary trade with Orrit in Morrowfast bought two salvaged timber and two knotflax cord for 36 drams. |
| 1150 | In Cinderhold, Perret sold one iron spike at a 22-dram quote. The player traded the Watering Grimoire for 11 drams of credit and paid 11 cash, leaving three drams. The ordinary forge consumed the spike, two timber and two cord and made sieve `9271`. |
| 1470 | At the wayside kitchen in `Overworld.12.11.0`, the free public oven `11472` converted one sheaf into toasted emberwheat `11508`. The player still had three drams, and the batch pan `11484` at `(55,18)` was still broken. |
| 1480 | Ordinary Gather on the public ripe claspbean `11487` produced two pulp `11509` and one seed `11511`. |
| 1500–1510 | Ordinary pickup took sorting lot `11503`. Screening it in the pack with sieve `9271` produced two fire clay `11512` and one coarse grit `11514`. The sieve stayed in the pack. |
| 1570 | Ordinary repair spent both screened measures on pan `11484`. The cook’s opening two prepared lining clay `11470` stayed available. |
| 1570–1580 | The native commission took two emberwheat from `2722`, one pulp split from `11509`, and two drams. Orven `11464` held 37 drams after the fee, up from 35. The active batch showed Working at tick 1580. F5 and fresh Continue matched that exact Working state. |
| 1690–1710 | Ordinary waits reached Ready at tick 1690 with meal `11515`. Opening the pickup board and taking the meal put that same item into the pack at tick 1700. Eat consumed it once at tick 1710. The player had one dram and 40 HP. |

The tool route cost 47 cash drams plus the traded grimoire, and left the reusable sieve and coarse grit. The local sorting itself used one action and no cash. The two-dram batch fee was separate. The free oven supplied a cooked sheaf before the pan repair or commission.

## Seed 64, paid import

This branch continued the exact tick-190 play save from the
[previous shipment trace](../PorterCausalTrace20261125/README.md). Supplier
payment of five drams belonged to the earlier real porter job. The player
arrived at this stage with 50 drams, no sieve and the batch pan still broken.

| Tick | Observed result |
| --- | --- |
| 250 | The cook sold the entire delivered two-measure lot `6898` for 11 drams and one raw meat `6855` for nine. The standard lot `6860` stayed in stock. The player held 30 drams. |
| 330–340 | At `(26,16)`, the free oven `6862` produced cooked meat `6899`. Pulp `6900` ×2 and one seed `6902` came from the separate ripe claspbean crop `6877`. The pan remained broken and the player's purse stayed at 30 drams. |
| 490–530 | Ordinary map travel went back to Gleaners’. Two public rows supplied two sheaves in `6907`. The player carried 21 pounds against the pack’s 150-pound hard intake limit and brought them back to the kitchen normally. |
| 740 | Ordinary repair of pan `6874` at `(29,17)` spent the exact purchased lot `6898`. No sieve was acquired. Cook `6854` still had the opening clay `6860` ×2. |
| 740–750 | Commission held both grain in `6907`, one exact split pulp from `6900`, and the two-dram fee. The cook’s purse went from 100 to 102 at this action. |
| 860–880 | Native waits reached Ready at 860. The board put the exact wrapped meal `6919` into the pack at 870. Native Eat consumed one at 880, with 28 drams and 40 HP. A separate fresh Continue at the 870 F5 checkpoint matched the exact meal, repaired pan, empty tray and board, still-available opening lot and 17 pounds in the pack. |

## What the paths establish

Both ordinary repair menus asked for the same two measures. Both commission
menus asked for two grain, one claspbean and two drams. Each commission
became ready exactly 120 world ticks after starting, produced one identifiable
meal, and left one spare pulp. Both actors stayed at 40 HP on the guided
paths. These trials exercised food use at full health. The meal’s healing and
bleed relief were outside this comparison.

Starting without a sieve, the observed local run paid more up front and
walked farther. The purchase was a reusable tool, and the sorting retained
coarse grit for other work. Once the tool was in the pack the local lot supplied
both measures in one action. The paid route spent 11 drams on the same two
measures with no tool purchase. The separate food purchase and each cook’s
fee were priced independently. These observations cover two ordinary paths
and one retained world. Discovery by a new player, additional regional routes
and fresh current-generation seed 64 remain open.

The repair action line clipped its final “in any mix” at the observed screen
size. The existing details command remains available. The HUD also showed a
separate 270-pound handling threshold alongside the lower hard pack limit.
Both are concrete reader checks for the later play-quality pass.

The native runtime was `5954d0e2a` for both branches. This evidence increment
uses the existing production rules. An independent archive read verified the
exact working escrow, both collected meals, imported custody and saved layout
versions. All seven checkpoint archives verified at four files each. After the
Editor stopped, the 86 selected foreign files and 861 selected normal files
matched their entry hashes. The current startup-verified Mac remains
`b47282110`. [Observed states](observations.json) · [Checkpoints](receipt.json)

- [Crafting the earned sieve](01-earned-sieve.png)
- [Commission with the local repair](02-local-commission.png)
- [Taking the wrapped meal](03-local-pickup.png)
- [The same repair using imported clay](04-imported-repair.png)
