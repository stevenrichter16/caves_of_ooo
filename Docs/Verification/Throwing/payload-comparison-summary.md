# Controlled payload comparison results

**Accepted corrected checkpoint.** W7 ground/order tables use the corrected explicit-stream native run in `explicit-stream-comparisons-green.xml` (28/28 passed). W3 retains the earlier seeded-tonic native receipts from `seeded-comparisons-green.xml`; W3 was not rerun in that 28-case set. The test-only witness preserves explicitly supplied combat/brain RNG streams and seeds otherwise unowned stun saves; production RNG is unchanged. Prior provisional and overwritten-stream checkpoints are retained separately and are not the source of these current W7 tables.

Native receipt files: 12/12. Trials: 13160. Each row summarizes exactly seeds101–120.

These are controlled supplied arenas with actual purchases, commands, finite stock and ordinary AI. They do not establish natural acquisition, general win rates, optimal policies or physical Steam Deck behavior. Level budgets change actual earned-level stats equally within each budget. The same seed does not force identical later RNG draws after different actions.

W3 measures two wounded allied layouts with finite starting healing. W7 measures lane holding, route detours and early retreat separately. W7-order pays the intervening real enemy and terrain turns; an unavailable or blocked follow-up is recorded as refused rather than made legal. Reported item use also includes a lost carried/equipped unit, so refer to custody and commands before treating it as consumed. The CSV actual_focused column counts trials with a paid Focused launch from the actual command receipt; the raw policy flag alone does not establish use.

## Focused healing tradeoff

| Budget/package | Variant/policy | Alive | Ally ΔHP | Enemy ΔHP | Player HP lost | Actions | Crossed | Detour | Used/lost items |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0/bank | mixed/focused-if-owned | 20/20 | -2.5 | 5.5 | 0 | 4 | 0/20 | 0 | 1 |
| 0/bank | two-allies/focused-if-owned | 20/20 | 13.95 | -2.75 | 0 | 4 | 0/20 | 0 | 1 |
| 2/bank | mixed/focused-if-owned | 20/20 | -2.5 | 5.5 | 0 | 4 | 0/20 | 0 | 1 |
| 2/bank | two-allies/focused-if-owned | 20/20 | 13.95 | -2.75 | 0 | 4 | 0/20 | 0 | 1 |
| 2/focused | mixed/focused-if-owned | 20/20 | -1.55 | -2.5 | 0 | 4 | 0/20 | 0 | 1 |
| 2/focused | two-allies/focused-if-owned | 20/20 | -0.05 | -2.65 | 0 | 4 | 0/20 | 0 | 1 |
| 4/bank | mixed/focused-if-owned | 20/20 | -2.5 | 5.5 | 0 | 4 | 0/20 | 0 | 1 |
| 4/bank | two-allies/focused-if-owned | 20/20 | 13.95 | -2.75 | 0 | 4 | 0/20 | 0 | 1 |
| 4/general4 | mixed/focused-if-owned | 20/20 | -1.55 | -2.5 | 0 | 4 | 0/20 | 0 | 1 |
| 4/general4 | two-allies/focused-if-owned | 20/20 | -0.05 | -2.65 | 0 | 4 | 0/20 | 0 | 1 |
| 6/all5-bank | mixed/focused-if-owned | 20/20 | -1.55 | -2.5 | 0 | 4 | 0/20 | 0 | 1 |
| 6/all5-bank | two-allies/focused-if-owned | 20/20 | -0.05 | -2.65 | 0 | 4 | 0/20 | 0 | 1 |
| 6/bank | mixed/focused-if-owned | 20/20 | -2.5 | 5.5 | 0 | 4 | 0/20 | 0 | 1 |
| 6/bank | two-allies/focused-if-owned | 20/20 | 13.95 | -2.75 | 0 | 4 | 0/20 | 0 | 1 |
| 6/focus-water | mixed/focused-if-owned | 20/20 | -1.55 | -2.5 | 0 | 4 | 0/20 | 0 | 1 |
| 6/focus-water | two-allies/focused-if-owned | 20/20 | -0.05 | -2.65 | 0 | 4 | 0/20 | 0 | 1 |

## Ground control with ordinary Broad splash

| Budget/package | Variant/policy | Alive | Ally ΔHP | Enemy ΔHP | Player HP lost | Actions | Crossed | Detour | Used/lost items |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0/bank | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 14.8 | 9 | 20/20 | 0 | 1.05 |
| 0/bank | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.6 | 9 | 20/20 | 3 | 1 |
| 0/bank | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 0/bank | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 0/bank | no-spare/broad-ground | 20/20 | 0 | -8.95 | 14.8 | 9 | 20/20 | 0 | 0.05 |
| 0/bank | open/broad-ground | 20/20 | 0 | -6.55 | 9.6 | 9 | 20/20 | 2 | 1 |
| 0/bank | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 2/bank | acid-immune/broad-ground | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 2/bank | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 2/bank | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 2/bank | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 2/bank | no-spare/broad-ground | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 2/bank | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 2/bank | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 2/focused | acid-immune/broad-ground | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 2/focused | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 2/focused | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 2/focused | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 2/focused | no-spare/broad-ground | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 2/focused | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 2/focused | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 4/bank | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 4/bank | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 4/bank | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 4/bank | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 4/bank | no-spare/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 4/bank | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 4/bank | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 4/general4 | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 4/general4 | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 4/general4 | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 4/general4 | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 4/general4 | no-spare/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 4/general4 | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 4/general4 | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/all5-bank | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/all5-bank | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 6/all5-bank | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 6/all5-bank | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 6/all5-bank | no-spare/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/all5-bank | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 6/all5-bank | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/bank | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/bank | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 6/bank | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 6/bank | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 6/bank | no-spare/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/bank | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 6/bank | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/focus-water | acid-immune/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/focus-water | bypass/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 6/focus-water | choke/broad-ground | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 6/focus-water | fast/broad-ground | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 6/focus-water | no-spare/broad-ground | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/focus-water | open/broad-ground | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 6/focus-water | ranged/broad-ground | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |

## Ground control with Focused when purchased

| Budget/package | Variant/policy | Alive | Ally ΔHP | Enemy ΔHP | Player HP lost | Actions | Crossed | Detour | Used/lost items |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0/bank | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 14.8 | 9 | 20/20 | 0 | 1.05 |
| 0/bank | bypass/focused-if-owned | 20/20 | 0 | -6.55 | 9.6 | 9 | 20/20 | 3 | 1 |
| 0/bank | choke/focused-if-owned | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 0/bank | fast/focused-if-owned | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 0/bank | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 14.8 | 9 | 20/20 | 0 | 0.05 |
| 0/bank | open/focused-if-owned | 20/20 | 0 | -6.55 | 9.6 | 9 | 20/20 | 2 | 1 |
| 0/bank | ranged/focused-if-owned | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 2/bank | acid-immune/focused-if-owned | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 2/bank | bypass/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 2/bank | choke/focused-if-owned | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 2/bank | fast/focused-if-owned | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 2/bank | no-spare/focused-if-owned | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 2/bank | open/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 2/bank | ranged/focused-if-owned | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 2/focused | acid-immune/focused-if-owned | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 2/focused | bypass/focused-if-owned | 20/20 | 0 | -3 | 5.35 | 9 | 20/20 | 3 | 1 |
| 2/focused | choke/focused-if-owned | 19/20 | 0 | -11.55 | 12.9 | 9 | 20/20 | 0 | 1 |
| 2/focused | fast/focused-if-owned | 20/20 | 0 | -6.9 | 14.65 | 9 | 20/20 | 0 | 1 |
| 2/focused | no-spare/focused-if-owned | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 2/focused | open/focused-if-owned | 19/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 1 | 1.05 |
| 2/focused | ranged/focused-if-owned | 20/20 | 0 | 0 | 18.9 | 8 | 20/20 | 1 | 1 |
| 4/bank | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 4/bank | bypass/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 4/bank | choke/focused-if-owned | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 4/bank | fast/focused-if-owned | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 4/bank | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 4/bank | open/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 4/bank | ranged/focused-if-owned | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 4/general4 | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 4/general4 | bypass/focused-if-owned | 20/20 | 0 | -3 | 5.35 | 9 | 20/20 | 3 | 1 |
| 4/general4 | choke/focused-if-owned | 20/20 | 0 | -11.55 | 12.9 | 9 | 20/20 | 0 | 1 |
| 4/general4 | fast/focused-if-owned | 20/20 | 0 | -6.9 | 14.65 | 9 | 20/20 | 0 | 1 |
| 4/general4 | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 4/general4 | open/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 1 | 1.05 |
| 4/general4 | ranged/focused-if-owned | 20/20 | 0 | 0 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/all5-bank | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/all5-bank | bypass/focused-if-owned | 20/20 | 0 | -3 | 5.35 | 9 | 20/20 | 3 | 1 |
| 6/all5-bank | choke/focused-if-owned | 20/20 | 0 | -11.55 | 12.9 | 9 | 20/20 | 0 | 1 |
| 6/all5-bank | fast/focused-if-owned | 20/20 | 0 | -6.9 | 14.65 | 9 | 20/20 | 0 | 1 |
| 6/all5-bank | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/all5-bank | open/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 1 | 1.05 |
| 6/all5-bank | ranged/focused-if-owned | 20/20 | 0 | 0 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/bank | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/bank | bypass/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 3 | 1 |
| 6/bank | choke/focused-if-owned | 20/20 | 0 | -13.75 | 6.85 | 9 | 20/20 | 0 | 1 |
| 6/bank | fast/focused-if-owned | 20/20 | 0 | -8 | 0 | 9 | 0/20 | 0 | 1 |
| 6/bank | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/bank | open/focused-if-owned | 20/20 | 0 | -6.55 | 9.85 | 9 | 20/20 | 2 | 1 |
| 6/bank | ranged/focused-if-owned | 20/20 | 0 | -3 | 18.9 | 8 | 20/20 | 1 | 1 |
| 6/focus-water | acid-immune/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 1.05 |
| 6/focus-water | bypass/focused-if-owned | 20/20 | 0 | -3 | 5.35 | 9 | 20/20 | 3 | 1 |
| 6/focus-water | choke/focused-if-owned | 20/20 | 0 | -11.55 | 12.9 | 9 | 20/20 | 0 | 1 |
| 6/focus-water | fast/focused-if-owned | 20/20 | 0 | -6.9 | 14.65 | 9 | 20/20 | 0 | 1 |
| 6/focus-water | no-spare/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 0 | 0.05 |
| 6/focus-water | open/focused-if-owned | 20/20 | 0 | -8.95 | 15.55 | 9 | 20/20 | 1 | 1.05 |
| 6/focus-water | ranged/focused-if-owned | 20/20 | 0 | 0 | 18.9 | 8 | 20/20 | 1 | 1 |

## Order comparisons

| Budget/package | Variant/policy | Alive | Ally ΔHP | Enemy ΔHP | Player HP lost | Actions | Crossed | Detour | Used/lost items |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0/bank | acid-push/effect-first | 20/20 | 0 | -12.7 | 0 | 6 | 5/20 | 0 | 2 |
| 0/bank | acid-push/place-first | 20/20 | 0 | -6.3 | 0 | 6 | 17/20 | 0 | 2 |
| 0/bank | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 0/bank | water-charge/place-first | 20/20 | 0 | -12 | 0 | 6 | 5/20 | 0 | 2 |
| 0/bank | water-push/effect-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 0/bank | water-push/place-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 2/bank | acid-push/effect-first | 20/20 | 0 | -12.7 | 0 | 6 | 5/20 | 0 | 2 |
| 2/bank | acid-push/place-first | 20/20 | 0 | -6.3 | 0 | 6 | 17/20 | 0 | 2 |
| 2/bank | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 2/bank | water-charge/place-first | 20/20 | 0 | -12 | 0 | 6 | 5/20 | 0 | 2 |
| 2/bank | water-push/effect-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 2/bank | water-push/place-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 2/focused | acid-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 2/focused | acid-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 2/focused | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 2/focused | water-charge/place-first | 20/20 | 0 | -4 | 0 | 6 | 20/20 | 0 | 2 |
| 2/focused | water-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 2/focused | water-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 4/bank | acid-push/effect-first | 20/20 | 0 | -12.7 | 0 | 6 | 5/20 | 0 | 2 |
| 4/bank | acid-push/place-first | 20/20 | 0 | -6.3 | 0 | 6 | 17/20 | 0 | 2 |
| 4/bank | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 4/bank | water-charge/place-first | 20/20 | 0 | -12 | 0 | 6 | 5/20 | 0 | 2 |
| 4/bank | water-push/effect-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 4/bank | water-push/place-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 4/general4 | acid-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 4/general4 | acid-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 4/general4 | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 4/general4 | water-charge/place-first | 20/20 | 0 | -4 | 0 | 6 | 20/20 | 0 | 2 |
| 4/general4 | water-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 4/general4 | water-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 6/all5-bank | acid-push/effect-first | 20/20 | 0 | -7.35 | 0.2 | 6 | 16/20 | 0 | 2 |
| 6/all5-bank | acid-push/place-first | 20/20 | 0 | -7.35 | 0.2 | 6 | 16/20 | 0 | 2 |
| 6/all5-bank | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 6/all5-bank | water-charge/place-first | 20/20 | 0 | -4 | 0 | 6 | 20/20 | 0 | 2 |
| 6/all5-bank | water-push/effect-first | 20/20 | 0 | -7.35 | 0.2 | 6 | 16/20 | 0 | 2 |
| 6/all5-bank | water-push/place-first | 20/20 | 0 | -7.35 | 0.2 | 6 | 16/20 | 0 | 2 |
| 6/bank | acid-push/effect-first | 20/20 | 0 | -12.7 | 0 | 6 | 5/20 | 0 | 2 |
| 6/bank | acid-push/place-first | 20/20 | 0 | -6.3 | 0 | 6 | 17/20 | 0 | 2 |
| 6/bank | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 6/bank | water-charge/place-first | 20/20 | 0 | -12 | 0 | 6 | 5/20 | 0 | 2 |
| 6/bank | water-push/effect-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 6/bank | water-push/place-first | 20/20 | 0 | -7.25 | 3 | 6 | 17/20 | 0 | 2 |
| 6/focus-water | acid-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 6/focus-water | acid-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 6/focus-water | water-charge/effect-first | 20/20 | 0 | -3 | 5.35 | 6 | 20/20 | 0 | 2 |
| 6/focus-water | water-charge/place-first | 20/20 | 0 | -4 | 0 | 6 | 20/20 | 0 | 2 |
| 6/focus-water | water-push/effect-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
| 6/focus-water | water-push/place-first | 20/20 | 0 | -8.2 | 3 | 6 | 16/20 | 0 | 2 |
