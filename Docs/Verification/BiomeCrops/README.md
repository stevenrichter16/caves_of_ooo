# Biome crops — verification receipts

Date: 2026-10-01. Baseline: `38252ce7e`. See [the living plan and roster](../../BIOME-CROPS.md) for implementation, uses and save limits.

## Automated evidence

- `Tests/` retains failing-before-implementation and passing reference receipts for processing, catalogue/content, equipment, player text, placement, saved lairs, metadata queries and town guidance.
- `Tests/first-native-result.json`: real Unity EditMode 207/214. Seven mutation probes had not been explicitly registered in the separate native test assembly. Registration and invocation assertions fixed that fixture problem. All 95 then-current content/use cases and all 37 art/cave cases passed.
- `Tests/regression-result.json`: broader native sweep, 1928/1930. The two failures were pre-existing planting expectations, reproduced against a pure committed-source archive in `Tests/baseline-38252.xml` and `Tests/baseline-provenance.json`. The updated tests assert refusal without payment, successful planting after owner-link repair, and the current earlier refusal reason. No planting production behavior was changed for them.
- `Tests/final-native-result.json`: final native acceptance **1930/1930**, zero failures/skips, 186.85 seconds. `Tests/native-selected-fixtures.json` lists the 80 selected fixtures. These are selected regression fixtures, not the entire repository test suite.
- `Art/native-import.json` records the 280 imported native meshes/prefabs. Seven Blender sheets show all forms. Source geometry has three passing structural tests. `Tests/objects-append-proof.json` proves all 551 earlier parsed blueprints and their order remain unchanged; exactly 105 new objects were appended.
- Three native world-census cases use seeds 64, 1729 and 729490642, real new-game exploration policy, actual arrival paths/stairs and physical ripe owners. Each finds all 35 new species. This is sampled coverage, not an all-seeds guarantee.

## Native keyboard runs

All runs use isolated saves and restore the previous editor scene. NPC scheduling remains active. Travel shortcuts are disclosed in each report. A `canVerify` field describes route capabilities; only completed PASS entries establish an actual outcome.

| Run | Result |
|---|---|
| `686ef8095d2c487ba99bed010d0e4429` | Source-selection guard rejected both actual Marlroot sites before travel. The harness required 12 hostile-free cells, stricter than ordinary travel. Selection was corrected without changing content, enemies or the seed. |
| `91e607abb7b24e379a59c7653eca3f38` | Eight PASS checks: ordinary actor, generated plant, harvest, physical pickup, Prepare into FireClay, returned-seed planting, learning rain and watering. Compound growth safety guard stopped; cause was not captured. |
| `970f23d7fdec4de69d79c930d0d15e0d` | The same eight checks passed; an unsafe diagnostic dereferenced an absent player cell. Diagnostic made null-safe. No gameplay conclusion from this failed probe. |
| `72856a61409344b797a54033b6a81700` | Eight checks passed. Final diagnostic proves an intact planted crop with eight growth rounds and 32 moisture remaining. An adjacent marlback scrabbler attacked the waiting player for 9, 21 and 7 damage; the route stopped at 3 HP. No invulnerability, enemy deletion or forced growth was used. |
| `1c383cb0539e4c089cf2df1361826b9e` | Independent cave route **5/5**, zero errors: ordinary actor, actual connected cave plant/native model, keyboard harvest and physical pickup, real equipment stowing/drop, and increased actual light-map brightness. One labelled travel shortcut. Source `Overworld.12.4.5@(66,18)` in this seed64 route. |

## Can verify

The automated native tests exercise every new species through harvested-item use and growth, including refusals/counters and saved ownership. Live input confirms the surface harvest → pickup → prepare → plant → rain sequence, actual paid growth beginning, and independent underground harvest/light behavior. The final surface danger screenshot and both cave screenshots were inspected; seven model sheets were reviewed.

## Cannot verify

The final surface Play run did **not** reach sprout, full maturation, second harvest or its F5/F6 checkpoint. Those remain automated lifecycle/save evidence, not a successful live route claim. No claim of unassisted discovery, every world seed, complete all-biome visual review in Play, long-term farming economy, combat balance, standalone performance, offscreen growth or new equipment rigs. Native cave stone reuses existing wall/floor geometry; it is not a new geological art pack.

## Repeating the bounded routes

Unity menus: `Caves Of Ooo / Scenarios / World / Biome Crops Audit` and `Biome Cave Crops Audit`. The surface audit honestly stops if the ordinary actor is endangered. It is not an invulnerable farming demonstration. Existing normal Morrowfast beds provide a place to plant discovered seeds after making the area safe.

Final source/docs whitespace check passes. Unity-generated mesh/library/meta YAML retains the editor’s normal blank-value trailing spaces; those serialized outputs were excluded from the textual whitespace check and were not rewritten after native testing.
