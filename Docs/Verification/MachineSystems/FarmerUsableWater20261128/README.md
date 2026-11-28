# Dovrin visits usable water in Sill

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple M5.

The ordinary seed 65 Sill arrival at turn 980 contained Dovrin Ternfold
and the cracked-ring well. The ring still admitted water under the
existing source check. The matched gameplay copies set the exact well's
stock to zero and placed one authored covered cistern with one unit to
the player's east. A test-only reset let the existing farmer evaluate
idle behavior immediately, with his visit chance set to 100% for both
copies. The player used normal wait, save, Continue, look and draw water
inputs. The authored cadence remains 5% per idle tick.

| Trial | Observed result |
| --- | --- |
| Earlier runtime, turn 990 | Dovrin moved one step toward the empty well's edge at (41,13), despite a stocked cistern at (43,17). |
| New routine, turn 990 | The same resident and first step selected the exact stocked cistern as his destination. |
| Arrival, turn 1100 | Dovrin stood on the adjacent bank at (44,16), with 20 HP. The cistern still held its one unit. |
| Fresh Continue, turn 1010 | His route pointed to the exact loaded, grounded cistern. The source, stock and positions matched the saved trip. |
| The traveler drew one unit | The cistern reached zero through its existing free draw action. On the next ordinary wait at turn 1020 Dovrin turned east, away from the cistern, and returned to his normal goal. His HP stayed 20 and another draw was no longer offered. |

Source admission uses the existing filling and drinking checks. An
optional trip only takes reachable steps with no shared gas or terrain
hazard penalty. The goal stays in its original zone and checks the exact
source each turn. The trip has a 50-turn budget, including interruptions.
The resident's walk leaves the water for the player's ordinary draw action.

Eight changed cases failed on the prior runtime, with 45 controls
passing. All 274 focused cases passed on the final runtime, including
follow and stay orders, broken and empty sources, poison at the only
passage, arrival, and depletion during travel. The final Editor replay
matched the earlier first step and arrival exactly. Independent source
and gameplay reviewers checked the rule, fixture and receipts. All
880 ordinary Quick files and 87 unrelated paths matched their initial
hashes. This slice has no wider NPC exposure or hardware performance result.

- [Dovrin beside the cistern](01-arrival.png)
- [The last draw remains available](02-draw.png)
- [The draw vanishes after depletion](03-empty.png)
