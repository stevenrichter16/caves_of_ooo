# Selected W7 payload precision check

Selection frozen on 2026-10-16 before the extension, after inspecting the original 20-seed matrix. This is the bounded A3-D12 refinement, not an originally preregistered statistical analysis. The initial matrix had not satisfied the precision gate.

Primary success is `common.survived && !crossed`: the player survives and the enemy is never observed at X <= 6 after any of the existing eight follow-up actions. This is the original hold-lane policy and sampled observation horizon, not an extraction objective or a guarantee that the enemy never entered the line between observations. Early retreat is excluded. Survival, damage, detour and remaining supplies are secondary descriptive observations. A fast death cannot count as holding the lane.

These five pairs use nine original arms with their unchanged purchased packages, geometry, actual commands, finite supplies and real AI turns. Seeds 101–200 replace the selected 101–120 ensembles; they are never pooled as 120 observations. The 2 SP Jet-versus-Focused pair compares two whole purchase-and-action policies, not an isolated passive effect. The other Focused pairs hold the purchased package fixed and compare delivery modes. The 0 SP pair establishes an ordinary throwing alternative.

Intervals use the same 95% Wilson/Newcombe independent risk-difference recipe as `analyze_precision_gate.py`. Different actions can consume different random draws even with the same starting seed. Fixed seeds are exploratory game tests, not samples from a defined gameplay population; intervals are precision heuristics. There is no multiplicity correction or significance claim. Width over 20 percentage points triggers the one 100-seed extension; width still over 20 at 100 remains unresolved without further expansion or balance tuning.

W3 retains continuous ally/enemy/player HP comparisons and the adjacent-ally versus allied-group reversal. No new binary W3 success condition was invented to obtain a precision result. Those 20-seed means remain descriptive. These controlled runs cannot establish natural acquisition, renewable supply pacing, or naturally earned use of Focused Delivery.

Accepted corrected checkpoint: `explicit-stream-comparisons-green.xml` passed all 28 affected cases. The first Jet/available-control extension changed two original-seed outcomes because electrical-contact stun saves used a wall-clock RNG. The first harness correction then overwrote explicitly supplied combat/brain streams and was rejected for causal attribution. The final witness preserves those streams and seeds only otherwise unowned enemy stun saves; production RNG is unchanged. The original selection counts below remain archived pre-correction observations. The 100-seed columns use the corrected native run, and the reader verifies full-record agreement for every selected arm's first 20 seeds against the corrected full matrix. Earlier W7/W6 checkpoints remain losslessly archived.

| Pair (A − B) | Exact A arm: budget/package/variant/policy | Exact B arm | Initial successes A/B | Initial Δ | Initial interval / width | Expand? | 100-seed successes A/B | 100-seed interval / width |
|---|---|---|---:|---:|---|---|---:|---|
| Fast pursuer: Focused − broad | 2/focused/fast/focused-if-owned | 2/focused/fast/broad-ground | 0/20 vs 20/20 | -100.0 pp | [-100.0, -77.2] / 22.8 pp | Yes | 0/100 vs 100/100 | [-100.0, -94.8] / 5.2 pp |
| Open: Focused − broad | 2/focused/open/focused-if-owned | 2/focused/open/broad-ground | 0/20 vs 0/20 | +0.0 pp | [-16.1, 16.1] / 32.2 pp | Yes | 0/100 vs 0/100 | [-3.7, 3.7] / 7.4 pp |
| Acid immune: Focused − broad | 2/focused/acid-immune/focused-if-owned | 2/focused/acid-immune/broad-ground | 0/20 vs 0/20 | +0.0 pp | [-16.1, 16.1] / 32.2 pp | Yes | 0/100 vs 0/100 | [-3.7, 3.7] / 7.4 pp |
| Open: 2 SP Jet control − 2 SP Focused | 2/jet/open/available-control | 2/focused/open/focused-if-owned | 18/20 vs 0/20 | +90.0 pp | [64.2, 97.2] / 33.0 pp | Yes | 78/100 vs 0/100 | [68.2, 85.0] / 16.8 pp |
| Open: ordinary direct − broad ground | 0/bank/open/direct | 0/bank/open/broad-ground | 20/20 vs 0/20 | +100.0 pp | [77.2, 100.0] / 22.8 pp | Yes | 100/100 vs 0/100 | [94.8, 100.0] / 5.2 pp |

Extension pairs with complete receipts: 5/5. The current 20-seed matrix is in payload-comparison-summary.md; the initial selection counts above remain explicitly historical. This supplement replaces only the selected binary comparisons.

The arm named Jet/available-control actually casts Drench Lob and throws one finite LightningTonic in all 100 receipts; it makes zero Jet Blast casts. Its result supports that existing wet-plus-lightning alternative, not an isolated benefit from purchasing Jet Blast. Focused ground delivery loses to Broad against the fast pursuer and does not hold the lane in the open or acid-immune conditions. Focused's separate W3 benefit is selective healing around a hostile, with reduced value when both adjacent recipients are allies; those continuous HP observations remain descriptive.

Regenerate after the corrected native sweep with `python3 Docs/Verification/Throwing/analyze_payload_precision.py --stun-seeded`. That mode requires exact 100-seed coverage and full-record agreement for the 20 overlapping corrected seeds. Running without the flag deliberately labels the remaining stochastic comparison provisional.
