#!/usr/bin/env python3
"""Selected W7 exploratory precision check; reads receipts only."""
import argparse
import gzip
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--stun-seeded", action="store_true",
                    help="Use only after the corrected native sweep; validates its first-20 overlap.")
args = parser.parse_args()
if args.stun_seeded:
    for name, total in (("explicit-stream-comparisons-green.xml", "28"),
                        ("explicit-stun-and-skills-green.xml", "41")):
        attributes = ET.parse(ROOT / name).getroot().attrib
        assert all(attributes.get(k) == v for k, v in
                   {"result": "Passed", "total": total, "passed": total, "failed": "0"}.items()), (name, "terminal gate not passed")


def load(name, historical=False):
    directory = ROOT / "comparisons"
    if historical:
        directory /= "superseded-before-stun-seeding"
    path = directory / (name + ".json")
    if path.exists():
        return json.loads(path.read_text())["trials"]
    path = path.with_suffix(path.suffix + ".gz")
    if path.exists():
        with gzip.open(path, "rt") as stream:
            return json.load(stream)["trials"]
    return []


def wilson(k, n):
    z = 1.959963984540054
    p = k / n
    divisor = 1 + z * z / n
    center = (p + z * z / (2 * n)) / divisor
    half = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / divisor
    return max(0, center - half), min(1, center + half)


def difference(a, b):
    ka = sum(r["common"]["survived"] and not r["crossed"] for r in a)
    kb = sum(r["common"]["survived"] and not r["crossed"] for r in b)
    pa, pb = ka / len(a), kb / len(b)
    la, ua = wilson(ka, len(a))
    lb, ub = wilson(kb, len(b))
    delta = pa - pb
    low = max(-1, delta - math.sqrt((pa - la) ** 2 + (ub - pb) ** 2))
    high = min(1, delta + math.sqrt((ua - pa) ** 2 + (pb - lb) ** 2))
    return ka, kb, delta, low, high


def rows(arm, extension=False, corrected_initial=False):
    budget, package, variant, policy = arm
    name = (f"W7-precision100-{budget}-{package}-{variant}-{policy}" if extension
            else f"W7-budget-{budget}")
    result = [r for r in load(name, historical=not extension and not corrected_initial)
              if (r["common"]["budget"], r["common"]["package"],
                  r["common"]["variant"], r["common"]["policy"]) == arm]
    if result:
        seeds = [r["common"]["seed"] for r in result]
        assert len(seeds) == len(set(seeds)), (arm, "duplicate seed")
        assert set(seeds) == set(range(101, 201 if extension else 121)), (arm, "seed set")
        assert all(r["common"]["spent"] + r["common"]["banked"] == budget for r in result)
    return result


def focused(variant, policy):
    return 2, "focused", variant, policy


PAIRS = [
    ("Fast pursuer: Focused − broad", focused("fast", "focused-if-owned"), focused("fast", "broad-ground")),
    ("Open: Focused − broad", focused("open", "focused-if-owned"), focused("open", "broad-ground")),
    ("Acid immune: Focused − broad", focused("acid-immune", "focused-if-owned"), focused("acid-immune", "broad-ground")),
    ("Open: 2 SP Jet control − 2 SP Focused", (2, "jet", "open", "available-control"), focused("open", "focused-if-owned")),
    ("Open: ordinary direct − broad ground", (0, "bank", "open", "direct"), (0, "bank", "open", "broad-ground")),
]

lines = [
    "# Selected W7 payload precision check", "",
    "Selection frozen on 2026-10-16 before the extension, after inspecting the original 20-seed matrix. This is the bounded A3-D12 refinement, not an originally preregistered statistical analysis. The initial matrix had not satisfied the precision gate.", "",
    "Primary success is `common.survived && !crossed`: the player survives and the enemy is never observed at X <= 6 after any of the existing eight follow-up actions. This is the original hold-lane policy and sampled observation horizon, not an extraction objective or a guarantee that the enemy never entered the line between observations. Early retreat is excluded. Survival, damage, detour and remaining supplies are secondary descriptive observations. A fast death cannot count as holding the lane.", "",
    "These five pairs use nine original arms with their unchanged purchased packages, geometry, actual commands, finite supplies and real AI turns. Seeds 101–200 replace the selected 101–120 ensembles; they are never pooled as 120 observations. The 2 SP Jet-versus-Focused pair compares two whole purchase-and-action policies, not an isolated passive effect. The other Focused pairs hold the purchased package fixed and compare delivery modes. The 0 SP pair establishes an ordinary throwing alternative.", "",
    "Intervals use the same 95% Wilson/Newcombe independent risk-difference recipe as `analyze_precision_gate.py`. Different actions can consume different random draws even with the same starting seed. Fixed seeds are exploratory game tests, not samples from a defined gameplay population; intervals are precision heuristics. There is no multiplicity correction or significance claim. Width over 20 percentage points triggers the one 100-seed extension; width still over 20 at 100 remains unresolved without further expansion or balance tuning.", "",
    "W3 retains continuous ally/enemy/player HP comparisons and the adjacent-ally versus allied-group reversal. No new binary W3 success condition was invented to obtain a precision result. Those 20-seed means remain descriptive. These controlled runs cannot establish natural acquisition, renewable supply pacing, or naturally earned use of Focused Delivery.", "",
    ("Accepted corrected checkpoint: `explicit-stream-comparisons-green.xml` passed all 28 affected cases. The first Jet/available-control extension changed two original-seed outcomes because electrical-contact stun saves used a wall-clock RNG. The first harness correction then overwrote explicitly supplied combat/brain streams and was rejected for causal attribution. The final witness preserves those streams and seeds only otherwise unowned enemy stun saves; production RNG is unchanged. The original selection counts below remain archived pre-correction observations. The 100-seed columns use the corrected native run, and the reader verifies full-record agreement for every selected arm's first 20 seeds against the corrected full matrix. Earlier W7/W6 checkpoints remain losslessly archived."
     if args.stun_seeded else
     "Reproducibility review: the first Jet/available-control extension changed two original-seed outcomes because real electrical-contact stun saves used a wall-clock RNG. Its interval is provisional until the test-only existing StunnedEffect.Rng correction and corrected native rerun complete. The other eight arms reproduced their checked original 20-seed behavioral fields. Pre-correction W7/W6 receipts remain losslessly archived; native fixture success alone is not a causal-evidence claim."), "",
    "| Pair (A − B) | Exact A arm: budget/package/variant/policy | Exact B arm | Initial successes A/B | Initial Δ | Initial interval / width | Expand? | 100-seed successes A/B | 100-seed interval / width |",
    "|---|---|---|---:|---:|---|---|---:|---|",
]
completed = 0
for title, arm_a, arm_b in PAIRS:
    a, b = rows(arm_a), rows(arm_b)
    assert a and b, (title, "missing initial evidence")
    ka, kb, delta, low, high = difference(a, b)
    ea, eb = rows(arm_a, True), rows(arm_b, True)
    if args.stun_seeded:
        for arm, extended in ((arm_a, ea), (arm_b, eb)):
            baseline = {r["common"]["seed"]: r for r in rows(arm, corrected_initial=True)}
            assert len(baseline) == 20 and len(extended) == 100, (arm, "incomplete corrected evidence")
            for receipt in extended:
                if receipt["common"]["seed"] <= 120:
                    assert receipt == baseline[receipt["common"]["seed"]], (arm, receipt["common"]["seed"], "corrected overlap differs")
    final_count = final_interval = "Pending"
    if ea and eb:
        xa, xb, xd, xl, xh = difference(ea, eb)
        final_count = f"{xa}/{len(ea)} vs {xb}/{len(eb)}"
        final_interval = f"[{xl * 100:.1f}, {xh * 100:.1f}] / {(xh - xl) * 100:.1f} pp"
        completed += 1
    lines.append(f"| {title} | {'/'.join(map(str, arm_a))} | {'/'.join(map(str, arm_b))} | "
                 f"{ka}/{len(a)} vs {kb}/{len(b)} | {delta * 100:+.1f} pp | "
                 f"[{low * 100:.1f}, {high * 100:.1f}] / {(high - low) * 100:.1f} pp | "
                 f"{'Yes' if high - low > .20 else 'No'} | {final_count} | {final_interval} |")
lines += ["", f"Extension pairs with complete receipts: {completed}/{len(PAIRS)}. "
          "The current 20-seed matrix is in payload-comparison-summary.md; the initial selection counts above remain explicitly historical. This supplement replaces only the selected binary comparisons.", "",
          "The arm named Jet/available-control actually casts Drench Lob and throws one finite LightningTonic in all 100 receipts; it makes zero Jet Blast casts. Its result supports that existing wet-plus-lightning alternative, not an isolated benefit from purchasing Jet Blast. Focused ground delivery loses to Broad against the fast pursuer and does not hold the lane in the open or acid-immune conditions. Focused's separate W3 benefit is selective healing around a hostile, with reduced value when both adjacent recipients are allies; those continuous HP observations remain descriptive.", "",
          "Regenerate after the corrected native sweep with `python3 Docs/Verification/Throwing/analyze_payload_precision.py --stun-seeded`. "
          "That mode requires exact 100-seed coverage and full-record agreement for the 20 overlapping corrected seeds. "
          "Running without the flag deliberately labels the remaining stochastic comparison provisional.", ""]
(ROOT / "payload-precision-gate.md").write_text("\n".join(lines))
print(f"Wrote {len(PAIRS)} selected W7 pairs; {completed} have complete extension receipts.")
