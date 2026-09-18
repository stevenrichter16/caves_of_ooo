# Spawn-ring first-import art budgets

Actual source triangle counts; native GPU/profile gate remains separate.

| Zone | Placed triangles | Native ground entities | Unique prototypes |
|---|---:|---:|---:|
| Overworld.2.5.0 | 91,838 | 2,173 | 34 |
| Overworld.2.6.0 | 1,244,392 | 2,511 | 34 |
| Overworld.2.7.0 | 1,178,984 | 2,416 | 50 |
| Overworld.3.5.0 | 84,030 | 2,068 | 70 |
| Overworld.3.7.0 | 1,265,330 | 2,432 | 42 |
| Overworld.4.5.0 | 106,318 | 2,209 | 37 |
| Overworld.4.6.0 | 1,111,772 | 2,383 | 45 |
| Overworld.4.7.0 | 1,348,066 | 2,358 | 45 |

The reusable kit contains 218 models and 132,578 unique source triangles. Full Grovelands scenes contain hundreds of native trees and vine walls; their leaves are the main triangle cost. Shared prototypes and static batching do not establish a frame-rate result.

Per-model bytes/triangles and per-zone native-blueprint/model totals are in `asset-budgets.json`. The catalog triangle sum differs slightly from rendered water masks because shared wet edges use the native16-mask geometry recipe.
