# Voxel native acceptance — verification sweep

Status: harness implemented; root-run U11 native acceptance passed, and U15 final regression independently matches the U00 failure baseline. No Unity process was launched by the harness author. Full-suite status remains 11,678 passed / 32 preexisting failures out of 11,710; it is not an all-green suite.

This is CoO-original presentation integration. It makes no Qud parity claim.

| Premise | Verified API / correction |
| --- | --- |
| The spawn is the global world-map starting constant. | False. Ordinary fresh `GameBootstrap` starts Morrowfast `Overworld.3.6.0` at `(40,23)`; the Sill world-map constant remains separate. |
| South is a generic fen. | False. `OverworldZoneManager` installs `MultiCellPilotRuntime` at `Overworld.3.7.0` before generic biome dispatch. The native audit expects 149 canonical pilot owners and 2,000 ground entities. |
| Four connected chunks need new world generation. | False for this integration. The selected native strip is `(2,6)–(3,6)–(3,7)–(4,7)`, with existing gameplay retained. |
| Town and ring share identical view lookup APIs. | False. `Village3DPresenter.TryGetOwnerView` takes a stable authored owner ID (player is `$player`); `SpawnRing3DPresenter.TryGetEntityView` takes an exact live entity. |
| An available actor root proves an animated visible actor. | False. A hidden guard can still have a root. The fixture moves the existing player near the guard and requires native render visibility before checking its controller/attachments. A separate actual hotbar cast measures evaluated bone motion. |
| Traversing a seam at arbitrary coordinates proves aligned travel. | False. Native arrival can search for another passable cell. The audit reads both existing borders, selects an already clear exact lane, explicitly fixture-positions the existing player, then uses an ordinary key and verifies exact aligned arrival and turn advancement. |
| Successful support lookup proves all source meshes were replaced. | False. Each current-zone capture requires positive applied count and zero missing source meshes. Full reveal is prohibited; player visibility and partial native FOV are required. |
| A static scene capture proves destruction. | False. The inherited pilot workload independently measures an imported nonanchor mesh contact, routes real structure damage, destroys a different owner, checks its entire physical body and actual view disappear, and checks repeat destruction is idempotent. |
| One body means one cell contact. | False. The native ridge has ten physical cells and an empty anchor hole. The new pulse gate repeats its physical contact list, checks the real cast-local snapshot emits its owner once, applies one damage pulse, and excludes the owner as the countercheck. |
| A successful save call proves reload persistence. | False. The native workload uses F5, captures owners/tiles, counter-mutates HP and removes the pipe, then uses F6 and compares exact snapshots and replacement references. No migration requirement is introduced. |

The new scenario files preserve the existing `MultiCellPilotNativeAudit` mechanics and profile workload as explicit copies, then add a `Region` partial. Existing scenario files remain unchanged. This intentionally duplicates test infrastructure to avoid changing the previously shipped native acceptance gate during presentation integration.

The editor wrapper uses the existing `NativeSaveIsolation` and retains the original runner's scene setup, GameView dimensions, input-settings and display-preference restoration checks. All captures use unique `VWN-<run-id>` names. The Python wrapper never launches Unity on import/default invocation; execution requires `--execute`, refuses an existing Editor, restarts MCP before launch, snapshots source hashes, checks compiler errors before accepting reports, and archives only this new run's artifacts.

## Observable acceptance and limits

Can verify: four current-zone mesh replacement counts, seven native 1920×1080 GameView captures, all six directed seam crossings with exact player identity/arrival, native quest-owner references and starter plantable terrain, a visible guard's real item attachment/controller, evaluated starter cast bones/native particle count, physical nonanchor selection/reach, damage deduplication at the shared query, destruction/view disposal, whole-body movement/blocking/hauling, current save/load and cache revisit, raw frame counters and teardown restoration.

Cannot verify automatically: visual quality, how readable every creature feels at all zoom levels, all-spell damage semantics, all-world content coverage, long-session stability, subjective input comfort, or sustained performance in all four biomes. The preserved 80-second profile is four 20-second south-pilot phases (pristine/restored × idle/walk). Other chunks receive native captures and counters, not an invented four-biome performance claim. Normal AI, HP, FOV, RNG and terrain consequences remain live; unexpected gameplay death or input failure is a failed gate, never repaired by suppressing the simulation.

Implementation log:

- Added 20 reflection contract tests before the root adapter existed. Root recorded `U02-contract-red`: zero compiler errors; all 20 integration cases and 15 independently authored mesh cases failed as intended.
- Added 53 dedicated receipt adversarial cases, preserving a complete positive control per rejection test. Root launched the missing-native-runner RED while scenario sources stayed outside `Assets`.
- Python launcher compiled and its dry-run invocation printed `launch: false`; no Unity was started.
- Root U11 completed 44 distinct native checks, four supported chunks with positive replacement counts and zero missing meshes, six exact seam crossings, and seven native captures. `U11-native-first/independent-inspection.json` independently checks source/artifact hashes, save snapshots and all 23,894 raw profile samples against their summaries. The 80-second workload remains limited to the south pilot. It includes 395–684 ms engine-frame stalls; historical pilot captures also contain comparable stalls, so evidence integrity is not a frame-budget pass or a controlled performance improvement claim.
- Root U15 completed 11,710 cases: 11,678 passed, 32 failed, zero skipped/inconclusive and zero C# compiler errors. All 167 new voxel cases pass. The 32 failing full names and their exact messages match U00; every retained test name has the same result. The net increase is 168 cases because the former single equipment case became a passing supported-region case and a passing outside-region countercase. Independent accounting and evidence hashes are in `U15-final-full-regression/independent-comparison.json`.
