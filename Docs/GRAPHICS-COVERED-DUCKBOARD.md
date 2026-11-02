# Covered Duckboard continuity

Status: bounded Editor gameplay accepted; publication and current package delivery pending. Source base: `d6818410f`. This is a presentation correction, not whole-area polish or a new gameplay mechanic. Evidence: `Docs/Verification/GraphicsExecution/CoveredDuckboard/verification.json`.

## Problem and scope

The published Causeway decking disappeared beneath the player, contracted adjacent connections and reverted the exact underfoot reader to `=`. At seed65, ordinary Sodden Causeway19.7, the earned checkpoint at40,15 reproduces all three defects. Reuse approved native16x24 decking and the existing lower decoration tilemap; no new art, geometry, collisions, generation, save schema or lighting engine.

## Requirements and acceptance

| Requirement | Verification / disposition |
| --- | --- |
| One disclosed canonical board remains beneath a real actor/item | Player/Dagger native cases; actual player at40,15 and38,15; bounded accepted |
| Cardinal joins use that same disclosed board | Native neighbor/move/removal checks and matched1x/2x actual captures; accepted |
| Exact authorized reader uses quiet frame0 | Native exact-identity checks; actual underfoot Wood8/8 portrait; accepted |
| Fog/memory, hidden, stale graph, malformed/custodied/removed/destroyed/active-tint/temperature/resource states refuse | Native positive controls and countercases; accepted within tested cases |
| Hidden competitors neither qualify nor erase disclosed boards; disclosed duplicates and solid fixtures refuse | Native duplicate, altered duplicate, hidden Duckboard/PeatBank and solid-fixture cases; accepted |
| Foreground identity, independent floors and hazards remain unchanged | Native wet-ground/heat-mark non-null controls and identity/state comparisons; accepted |
| Existing decoration precedence and legacy architecture remain intact | Source review; Morrowfast regression; legacy branch reused, no new actual toggle claim |
| Movement, travel and reload remain playable | Native input40,15→38,15;19.7↔19.8; F5turn2690 and fresh Continue40,15HP22; accepted with local image difference noted below |
| Narrow paths remain narrow | Actual16.7 earned crossing20,12→20,11 at1x/2x; accepted |
| Performance and release claims stay bounded | Short Editor profile only; R11 open; package delivery pending |

## Implementation and review

`FlatPixelSoddenTravelCatalog.TryBoardOwner` is the shared read-only authority check for masks, covered decoration and board portraits. It verifies current area admission, cell visibility, physical placement and canonical owner semantics. It ignores undisclosed owners and refuses disclosed ambiguity. `CoveredBoard` paints only when a real actor/item covers the board; the selected board remains on its original foreground layer. `FlatPixelGreyboxCatalog` preserves MorrowfastBridge and ReserveCord priority before adding Sodden decoration. Tiles are cached; queries do not allocate art, generate graphs or write state.

Self-review covered identity/duplicate parts, disclosure, graph replacement, movement/removal, resource fallback, independent hazard state, layer precedence and save behavior. Independent source review found no remaining P1/P2; independent visual review accepted only the corrected decking and exact reader. Existing fine timber-grain repetition, unrelated ASCII and black backing remain. No population/shadow acceptance was advanced.

## Source corrections and test receipts

- Native RED job `ee7de5f3ace34eadbd7653ef62bee890`:21 intended failures plus2 missing-resource passes,23 scheduled.
- First regression job `04271b880f8c4bc2afa905b34f98ae0a`:2 failures. Dry wood rejects `Entity.ForceApplyEffect(Electrified)` through the object-status matrix; the tint fixture was unasserted. It now injects an owned active StatusEffectsPart and asserts its override. A third historical covered-reader expectation was intentionally revised to the new contract; hidden/removal negatives remain unchanged.
- Independent fixture review added non-null submitted ground/heat-mark controls, preventing null-to-null comparisons.
- Final `FlatPixelSoddenTravelTests` + `FlatPixelMorrowfastRiverTests`, job `b390a123aa434d9dbe8378c27da342a0`: completion response reported208 passed,0 failed,39.5509279s;211 scheduled. Three pre-existing allocation checks remain uncertified/inconclusive. Later job queries retained progress but returned `result:null` after reload. Saved receipts are terminal status evidence; no claim that complete per-case results were preserved.

## Actual gameplay and limits

BeforeCheckpoint is a native F5 at40,15,turn2560,HP38. Matched1280x800 1x/2x captures show the hole closed and joins preserved. Actual underfoot examine shows duckboard, Wood,8/8 and native frame0. Ordinary travel and return triggered real snake attacks: AfterCheckpoint is40,15,turn2690,HP22 with bleeding; no healing, F12, teleport or gameplay-state correction was used. Fresh Continue preserves that state and continuous decking.

Saved→reloaded differs140/639200 world pixels in450,384–476,400. Cause unproven; no exact whole-world parity claim. Before/after also differs in HUD/log and snake-state pixels. Actual item-covered play was not separately performed; Dagger coverage is native-test evidence. The narrow16.7 control retains slim boards and readable water.

The164.3475s after-only Editor profile includes initial Continue/load, captures and tool scheduling (start11.10,end19.7). Renderer p95.09225ms,max351.617ms; Main Thread max18.121s. Its initial2321x1285 metadata precedes the fixed1280x800 captures. It establishes no causal performance gain or packaged benchmark. R11 remains open.

## Preservation, reproduction and next action

Root used isolated native Play/Finish sessions, preserving normal saves/preferences.75 foreign metadata hashes remain exact;2 root-neutral metadata files remain unstaged. Play stopped, save override null and scene clean. No storage cleanup.

To reproduce: use the existing `Play Live16x24 Greybox` isolated launcher; install BeforeCheckpoint in that isolated root, Continue, capture1x and2x, examine underfoot, walk left twice, travel19.7→19.8→19.7, walk down3, F5 and fresh Continue. Use the earned `packaged-sodden-crossing65` fixture for16.7 narrow control. Never overwrite normal saves.

Publish exact reviewed owned files with these living records. Batch Mac/Linux after this commit, including both d681 deck art and this runtime correction; current fa616 packages exclude both. Then select the largest evidenced environmental gap in another region, considering purposeful chunk composition and restrained shadows. Full V0–V7 completion, whole-area polish, broad population, remaining ASCII, R11 and Linux/Deck execution remain open.
