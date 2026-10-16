#!/usr/bin/env python3
"""Summarize actual finite command receipts; do not turn partial/retreat into success."""
import collections
import csv
import gzip
import json
from pathlib import Path
import statistics
import sys

ROOT = Path(__file__).resolve().parent
WORLDS = ("W1", "W2", "W4", "W4-reversal", "W5", "W6")

def load(name):
    p = ROOT / "comparisons" / (name + ".json")
    baseline=ROOT/'comparisons'/'baseline-measured-plus2'/(name+'.json.gz')
    if name.startswith('W1-') and baseline.exists():
        with gzip.open(baseline,'rt') as f:return json.load(f)['trials']
    if p.exists():
        return json.loads(p.read_text())["trials"]
    p = p.with_suffix(p.suffix + ".gz")
    if p.exists():
        with gzip.open(p, "rt") as f:
            return json.load(f)["trials"]
    return []

def mean(rows, field):
    return round(statistics.mean(r[field] for r in rows), 3)

def summarize(rows):
    counts = collections.Counter(r["outcome"] for r in rows)
    return dict(n=len(rows), survived=sum(r["survived"] for r in rows),
                objective=sum(counts[x] for x in ("objective-and-weapon", "portable-custody-and-extraction")),
                objective_weapon_lost=counts["objective-weapon-lost"], death=counts["death"],
                early_retreat=counts["early-retreat"], hp_loss=mean(rows,"hpLoss"),
                actions=mean(rows,"actions"), enemy_turns=mean(rows,"enemyTurns"),
                outcomes="; ".join(f"{k}:{v}" for k,v in sorted(counts.items())))

all_rows = []
data = {}
for world in WORLDS:
    for budget in (0,2,4,6):
        trials = load(f"{world}-{budget}")
        data[world,budget] = trials
        groups = collections.defaultdict(list)
        for row in trials:
            groups[row["variant"],row["package"],row["policy"]].append(row)
        for (variant,package,policy), rows in sorted(groups.items()):
            all_rows.append(dict(world=world,budget=budget,variant=variant,package=package,policy=policy,**summarize(rows)))
if all_rows:
    with (ROOT/"physical-comparison-summary.csv").open("w",newline="") as out:
        writer=csv.DictWriter(out,fieldnames=list(all_rows[0]));writer.writeheader();writer.writerows(all_rows)

evidence_status = "**Evidence status:** W1 is the preserved historical **+2 Measured baseline**, loaded explicitly from `comparisons/baseline-measured-plus2/`. It is not acceptance evidence for the current +4 revision; read [the separate revision report](measured-revision-summary.md) and [retention decision](measured-revision-1.md). " + ("W6 uses the corrected fallback-stun stream receipts; their terminal execution and replay checks are recorded separately in the precision report." if "--stun-seeded" in sys.argv else "W6's corrected fallback-stun rerun is pending; its displayed historical counts must not be used for final current attribution.")
lines=["# Physical throwing and finite expedition comparisons", "", evidence_status, "",
       "These are controlled native Unity EditMode trials of the real registry, purchases, commands, scheduler, creature AI, effects, inventory and terrain boundaries. They are not naturally earned progression, rendered play, human enjoyment, or Steam Deck performance. Read the terminal test XML separately; a generated receipt is not a claim that every other test passed.", "",
       "The arena, enemy coordinates, 0/4/8 AV experiments and explicitly listed supplementary supplies are controlled fixture inputs. Each package at a given budget receives the same starting build, levels, gear and declared supply. Banked points count. Twenty seeds are frozen for each variant. Differing actions and retries consume different production RNG draws; do not interpret individual paired outcomes as identical luck after divergence. Gas poison duration also uses its existing TestRng seam with a separate per-trial seed and restoration. Self-use tonics use a separate test-only seeded stream injected through the existing InventoryAction Random parameter, preserving real commands/consumption. The initial W6 receipts before that correction were confounded by wall-clock-seeded healing and must not support skill attribution.", "",
       "W1 uses skill-aware policies: melee alternatives keep their blade, while the declared ordinary-throw baseline and Throwing owners can spend it at range. W6 deliberately uses two common finite-stock policies; it is a stock/consequence comparison, not an optimized policy for every purchased active. In particular its Driving and Focused purchases are not themselves activated. Do not call differences from those unused buttons their causal benefit.", "",
       "## Aggregate receipts", ""]
for world in WORLDS:
    lines.extend([f"### {world}", "", "| SP | Package | Policy | N | Alive | Objective + weapon | Lost weapon objective | Early retreat | Mean HP loss | Mean actions |", "|---:|---|---|---:|---:|---:|---:|---:|---:|---:|"])
    for budget in (0,2,4,6):
        groups=collections.defaultdict(list)
        for r in data[world,budget]:groups[r["package"],r["policy"]].append(r)
        for (pkg,pol),rows in sorted(groups.items()):
            s=summarize(rows)
            lines.append(f"| {budget} | {pkg} | {pol} | {s['n']} | {s['survived']} | {s['objective']} | {s['objective_weapon_lost']} | {s['early_retreat']} | {s['hp_loss']:.2f} | {s['actions']:.2f} |")
    lines.append("")
lines += ["## Interpretation boundaries", "",
          "- W2 validates actual universal floor/cover/owner intent and committed commands, not an equipment-retrieval objective. Its zero objective column is therefore not failure.",
          "- W4 observes short-term force/entry and withdrawal. All these W4 arms had zero player HP loss; no survival improvement follows from that fixture. Root-only banked arms isolate Driving from the root's accuracy retry. Brace and wall cases should be compared against that control, not against an untrained thrower. Dense poison gas is an explicit arena hazard, not a guaranteed world opportunity. The paired W4-reversal holds the same gas lane with cloud at the enemy's current position: ordinary root throws defeat 15/20 after withdrawal versus Driving 4/20 because an unnecessary push can rescue the enemy from its imminent dose. The dry control is 4/20 for both. This is a harmful positioning reversal, not evidence of player deaths caused by pushing.",
          "- W5 has distinct objectives: ground rethrow and abandonment intentionally do not secure portable custody. Recovery's one-action saving must be weighed against its two-node cost, unchanged entry harm and a free dog's actual ownership. A free dog can remove the player's recovery opportunity.",
          "- W1 and W6 record death, early retreat, timeouts and weapon loss separately. Lower mean action count can mean an earlier death; read it with survival and objective success. Lower raw HP loss across different SP budgets is confounded by their different earned levels/max HP, so compare within a budget.",
          "- A purchased package with no eligible use in a fixed script is not evidence that the skill is useless generally. No broad superiority, universal safety, merchant replenishment or natural acquisition claim follows from these small controlled ensembles.", ""]
(ROOT/"physical-comparison-summary.md").write_text("\n".join(lines))
print(f"Wrote {len(all_rows)} grouped observations from {sum(map(len,data.values()))} finite command trials")
