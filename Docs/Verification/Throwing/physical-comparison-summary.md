# Physical throwing and finite expedition comparisons

**Evidence status:** W1 is the preserved historical **+2 Measured baseline**, loaded explicitly from `comparisons/baseline-measured-plus2/`. It is not acceptance evidence for the current +4 revision; read [the separate revision report](measured-revision-summary.md) and [retention decision](measured-revision-1.md). W6 uses the corrected fallback-stun stream receipts; their terminal execution and replay checks are recorded separately in the precision report.

These are controlled native Unity EditMode trials of the real registry, purchases, commands, scheduler, creature AI, effects, inventory and terrain boundaries. They are not naturally earned progression, rendered play, human enjoyment, or Steam Deck performance. Read the terminal test XML separately; a generated receipt is not a claim that every other test passed.

The arena, enemy coordinates, 0/4/8 AV experiments and explicitly listed supplementary supplies are controlled fixture inputs. Each package at a given budget receives the same starting build, levels, gear and declared supply. Banked points count. Twenty seeds are frozen for each variant. Differing actions and retries consume different production RNG draws; do not interpret individual paired outcomes as identical luck after divergence. Gas poison duration also uses its existing TestRng seam with a separate per-trial seed and restoration. Self-use tonics use a separate test-only seeded stream injected through the existing InventoryAction Random parameter, preserving real commands/consumption. The initial W6 receipts before that correction were confounded by wall-clock-seeded healing and must not support skill attribution.

W1 uses skill-aware policies: melee alternatives keep their blade, while the declared ordinary-throw baseline and Throwing owners can spend it at range. W6 deliberately uses two common finite-stock policies; it is a stock/consequence comparison, not an optimized policy for every purchased active. In particular its Driving and Focused purchases are not themselves activated. Do not call differences from those unused buttons their causal benefit.

## Aggregate receipts

### W1

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | bank | free-cold | 240 | 160 | 128 | 0 | 0 | 23.26 | 12.72 |
| 0 | bank | melee | 240 | 146 | 100 | 0 | 0 | 26.95 | 14.53 |
| 0 | bank | ordinary | 240 | 141 | 97 | 0 | 0 | 23.99 | 17.64 |
| 0 | bank | retreat | 240 | 240 | 0 | 0 | 240 | 0.00 | 1.00 |
| 2 | bank | free-cold | 240 | 160 | 128 | 0 | 0 | 24.63 | 12.92 |
| 2 | bank | melee | 240 | 148 | 102 | 0 | 0 | 28.53 | 14.79 |
| 2 | bank | ordinary | 240 | 142 | 96 | 0 | 0 | 25.69 | 18.03 |
| 2 | bank | retreat | 240 | 240 | 0 | 0 | 240 | 0.00 | 1.00 |
| 2 | dodge | owned-opportunity | 240 | 176 | 118 | 0 | 0 | 25.02 | 16.08 |
| 2 | jet | owned-opportunity | 240 | 172 | 126 | 0 | 0 | 23.80 | 14.56 |
| 2 | measured | owned-opportunity | 240 | 159 | 94 | 0 | 0 | 22.08 | 17.91 |
| 2 | puncture | owned-opportunity | 240 | 200 | 152 | 0 | 0 | 22.23 | 13.79 |
| 2 | root | owned-opportunity | 240 | 145 | 93 | 0 | 0 | 24.45 | 18.10 |
| 4 | bank | free-cold | 240 | 170 | 133 | 0 | 0 | 25.79 | 13.55 |
| 4 | bank | melee | 240 | 162 | 109 | 0 | 0 | 29.90 | 15.60 |
| 4 | bank | ordinary | 240 | 145 | 99 | 0 | 0 | 27.30 | 18.59 |
| 4 | bank | retreat | 240 | 240 | 0 | 0 | 240 | 0.00 | 1.00 |
| 4 | blades4 | owned-opportunity | 240 | 218 | 174 | 0 | 0 | 14.77 | 12.24 |
| 4 | measured-jet | owned-opportunity | 240 | 182 | 102 | 0 | 0 | 18.00 | 18.90 |
| 4 | physical4 | owned-opportunity | 240 | 162 | 96 | 0 | 0 | 23.42 | 18.00 |
| 6 | all5-water | owned-opportunity | 240 | 164 | 97 | 0 | 0 | 24.70 | 18.35 |
| 6 | bank | free-cold | 240 | 176 | 139 | 0 | 0 | 26.89 | 13.85 |
| 6 | bank | melee | 240 | 170 | 113 | 0 | 0 | 31.12 | 16.06 |
| 6 | bank | ordinary | 240 | 148 | 100 | 0 | 0 | 28.85 | 19.07 |
| 6 | bank | retreat | 240 | 240 | 0 | 0 | 240 | 0.00 | 1.00 |
| 6 | blades4 | owned-opportunity | 240 | 218 | 174 | 0 | 0 | 15.13 | 12.34 |
| 6 | physical-dodge | owned-opportunity | 240 | 170 | 101 | 0 | 0 | 22.99 | 19.37 |
| 6 | water-acrobatics | owned-opportunity | 240 | 200 | 142 | 0 | 0 | 22.23 | 16.15 |

### W2

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | bank | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 2 | bank | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 2 | measured | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 4 | bank | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 4 | measured | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 6 | bank | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |
| 6 | measured | ordinary-throw | 160 | 160 | 0 | 0 | 0 | 0.00 | 1.00 |

### W4

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | bank | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | bank | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | driving | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | jet | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 4.00 |
| 2 | root | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | bank | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | physical4 | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | root | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | water-tumble | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 4.00 |
| 6 | all5-water | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | bank | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | root | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | water-acrobatics | lane-control | 140 | 140 | 0 | 0 | 0 | 0.00 | 4.00 |

### W4-reversal

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 2 | driving | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |
| 2 | root | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |
| 4 | physical4 | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |
| 4 | root | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |
| 6 | all5-water | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |
| 6 | root | existing-gas-lane | 40 | 40 | 0 | 0 | 0 | 0.00 | 2.00 |

### W5

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | bank | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 0 | bank | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 0 | bank | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 2 | bank | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 2 | bank | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | bank | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 2 | dodge | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 2 | dodge | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | dodge | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 2 | recovery | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 2 | recovery | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 2 | recovery | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 6.82 |
| 4 | bank | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 4 | bank | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | bank | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 4 | blades4 | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 4 | blades4 | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | blades4 | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 4 | physical4 | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 4 | physical4 | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 4 | physical4 | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 6.82 |
| 6 | all5-water | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 6 | all5-water | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | all5-water | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 6.82 |
| 6 | bank | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 6 | bank | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | bank | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |
| 6 | water-acrobatics | abandon | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.20 |
| 6 | water-acrobatics | ground-rethrow | 100 | 100 | 0 | 0 | 0 | 0.00 | 3.00 |
| 6 | water-acrobatics | secure | 100 | 100 | 100 | 0 | 0 | 0.60 | 7.62 |

### W6

| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | bank | conserve | 20 | 3 | 0 | 0 | 3 | 39.10 | 15.05 |
| 0 | bank | spend-for-distance | 20 | 15 | 11 | 0 | 4 | 21.90 | 36.05 |
| 2 | bank | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | bank | spend-for-distance | 20 | 16 | 12 | 0 | 4 | 23.40 | 37.40 |
| 2 | driving | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | driving | spend-for-distance | 20 | 16 | 12 | 0 | 4 | 23.40 | 37.25 |
| 2 | focused | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | focused | spend-for-distance | 20 | 16 | 12 | 0 | 4 | 23.40 | 37.25 |
| 2 | measured | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | measured | spend-for-distance | 20 | 15 | 9 | 4 | 2 | 22.45 | 36.45 |
| 2 | puncture | conserve | 20 | 11 | 9 | 0 | 2 | 31.05 | 23.35 |
| 2 | puncture | spend-for-distance | 20 | 16 | 12 | 0 | 4 | 23.40 | 37.40 |
| 2 | recovery | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | recovery | spend-for-distance | 20 | 16 | 14 | 1 | 1 | 21.85 | 37.80 |
| 2 | tumble | conserve | 20 | 2 | 1 | 0 | 1 | 43.45 | 15.95 |
| 2 | tumble | spend-for-distance | 20 | 16 | 12 | 0 | 4 | 23.40 | 37.40 |
| 4 | bank | conserve | 20 | 2 | 1 | 0 | 1 | 47.30 | 16.05 |
| 4 | bank | spend-for-distance | 20 | 16 | 13 | 0 | 3 | 25.85 | 38.55 |
| 4 | blades4 | conserve | 20 | 13 | 10 | 0 | 3 | 34.55 | 24.05 |
| 4 | blades4 | spend-for-distance | 20 | 16 | 13 | 0 | 3 | 25.85 | 38.55 |
| 4 | focus-tumble | conserve | 20 | 2 | 1 | 0 | 1 | 47.30 | 16.05 |
| 4 | focus-tumble | spend-for-distance | 20 | 17 | 13 | 1 | 3 | 25.70 | 39.00 |
| 4 | physical4 | conserve | 20 | 2 | 1 | 0 | 1 | 47.30 | 16.05 |
| 4 | physical4 | spend-for-distance | 20 | 14 | 11 | 3 | 0 | 26.10 | 36.90 |
| 4 | water-tumble | conserve | 20 | 2 | 1 | 0 | 1 | 47.30 | 16.05 |
| 4 | water-tumble | spend-for-distance | 20 | 16 | 13 | 0 | 3 | 25.85 | 38.55 |
| 6 | all5-water | conserve | 20 | 7 | 3 | 0 | 4 | 49.70 | 18.80 |
| 6 | all5-water | spend-for-distance | 20 | 16 | 11 | 4 | 1 | 29.10 | 38.60 |
| 6 | bank | conserve | 20 | 7 | 3 | 0 | 4 | 49.70 | 18.80 |
| 6 | bank | spend-for-distance | 20 | 16 | 15 | 0 | 1 | 28.20 | 38.85 |
| 6 | physical-dodge | conserve | 20 | 8 | 6 | 0 | 2 | 43.85 | 24.95 |
| 6 | physical-dodge | spend-for-distance | 20 | 17 | 10 | 6 | 1 | 24.55 | 39.25 |
| 6 | water-acrobatics | conserve | 20 | 8 | 6 | 0 | 2 | 43.85 | 24.95 |
| 6 | water-acrobatics | spend-for-distance | 20 | 18 | 18 | 0 | 0 | 24.05 | 41.55 |

## Interpretation boundaries

- W2 validates actual universal floor/cover/owner intent and committed commands, not an equipment-retrieval objective. Its zero objective column is therefore not failure.
- W4 observes short-term force/entry and withdrawal. All these W4 arms had zero player HP loss; no survival improvement follows from that fixture. Root-only banked arms isolate Driving from the root's accuracy retry. Brace and wall cases should be compared against that control, not against an untrained thrower. Dense poison gas is an explicit arena hazard, not a guaranteed world opportunity. The paired W4-reversal holds the same gas lane with cloud at the enemy's current position: ordinary root throws defeat 15/20 after withdrawal versus Driving 4/20 because an unnecessary push can rescue the enemy from its imminent dose. The dry control is 4/20 for both. This is a harmful positioning reversal, not evidence of player deaths caused by pushing.
- W5 has distinct objectives: ground rethrow and abandonment intentionally do not secure portable custody. Recovery's one-action saving must be weighed against its two-node cost, unchanged entry harm and a free dog's actual ownership. A free dog can remove the player's recovery opportunity.
- W1 and W6 record death, early retreat, timeouts and weapon loss separately. Lower mean action count can mean an earlier death; read it with survival and objective success. Lower raw HP loss across different SP budgets is confounded by their different earned levels/max HP, so compare within a budget.
- A purchased package with no eligible use in a fixed script is not evidence that the skill is useless generally. No broad superiority, universal safety, merchant replenishment or natural acquisition claim follows from these small controlled ensembles.
