#!/usr/bin/env python3
"""Compare one linked Measured bonus revision to preserved unchanged-world receipts."""
import collections
import gzip
import json
import math
import re
import statistics
from pathlib import Path

ROOT=Path(__file__).resolve().parent

def load(name,baseline=False):
    folder=ROOT/'comparisons'
    if baseline:folder=folder/'baseline-measured-plus2'
    p=folder/(name+'.json')
    if p.exists():return json.loads(p.read_text())['trials']
    p=p.with_suffix('.json.gz')
    if not p.exists():return []
    with gzip.open(p,'rt') as f:return json.load(f)['trials']

def summary(rows):
    alive=[r for r in rows if r['survived']]
    objective=[r for r in rows if r['outcome']=='objective-and-weapon']
    return dict(n=len(rows),alive=len(alive),obj=len(objective),hp=statistics.mean(r['hp'] for r in alive) if alive else 0,
        actions=statistics.mean(r['actions'] for r in objective) if objective else 0,
        timeout=sum(r['outcome']=='partial-or-timeout' for r in rows),
        supplies=collections.Counter(r['finalSupply'] for r in objective))

def wilson(successes,n):
    p=successes/n;z=1.959963984540054;den=1+z*z/n
    center=(p+z*z/(2*n))/den
    half=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/den
    return center-half,center+half

def risk_difference(a,b):
    pa=a['obj']/a['n'];pb=b['obj']/b['n'];la,ua=wilson(a['obj'],a['n']);lb,ub=wilson(b['obj'],b['n'])
    d=pa-pb
    return d,d-math.hypot(pa-la,ub-pb),d+math.hypot(ua-pa,pb-lb)

def key(r):return r['variant'],r['package'],r['policy'],r['seed']

def comparable(r):
    r=dict(r);r['afterActions']=[re.sub(r'(player=-?\d+,-?\d+)/-?\d+(?= )',r'\1',x) for x in r['afterActions']]
    return r

lines=['# Measured revision 1: unchanged-world comparison','',
'One gameplay parameter changed: paid Measured preparation adds +4 penetration and cap instead of +2. All scenarios, actor builds, finite supplies, costs, policies and seeds remain fixed. This report reads explicit `-measured-r1` outputs against the frozen +2 baseline; it never substitutes a later ordinary-method receipt for the original. The supplemental 100-seed set includes the first20, not 120 independent observations.','',
'Objective means enemy defeated, original weapon in portable custody, and extraction reached. Survivor-only HP is a selected-population descriptive statistic, not a causal estimate; action means below include successful objectives only so faster death is not counted as improvement. The original source/sink and natural progression limits still apply.','',
'## Full frozen W1 at 20 seeds','',
'| SP | Condition | Package / policy | Original +2 objective/alive | Revision +4 objective/alive | Survivor HP original→revision | Successful actions original→revision |','|---:|---|---|---:|---:|---|---|']
controls=[];selected_controls=[];selected_summaries={};selected_rows={};available=0
for budget in (0,2,4,6):
    before=load(f'W1-{budget}',True);after=load(f'W1-{budget}-measured-r1')
    if not after:continue
    available+=len(after);old={key(r):r for r in before};assert len(after)==len(before)
    assert set(old)=={key(r) for r in after}
    groups=collections.defaultdict(list)
    for r in after:groups[(r['variant'],r['package'],r['policy'])].append(r)
    for group,rs in sorted(groups.items()):
        bs=[old[key(r)] for r in rs];a=summary(bs);b=summary(rs)
        v,p,policy=group
        # Purchase receipts are real; keep non-owners as replay controls.
        owns=any('Throwing_MeasuredThrow' in str(r['purchases']) for r in rs)
        diffs=sum(comparable(r)!=comparable(old[key(r)]) for r in rs)
        if not owns:controls.append((budget,v,p,policy,len(rs),diffs))
        lines.append(f'| {budget} | {v} | {p} / {policy} | {a["obj"]}/{a["alive"]} | {b["obj"]}/{b["alive"]} | {a["hp"]:.2f}→{b["hp"]:.2f} | {a["actions"]:.2f}→{b["actions"]:.2f} |')
lines+=['','## Selected 100-seed comparisons','',
'| Condition | Package | Original objective/alive | Revision objective/alive | Survivor HP original→revision | Successful actions original→revision |',
'|---|---|---:|---:|---|---|']
before=load('W1-2-precision100',True);after=load('W1-2-precision100-measured-r1')
if after:
    old={key(r):r for r in before};assert set(old)=={key(r) for r in after};available+=len(after)
    groups=collections.defaultdict(list)
    for r in after:groups[(r['variant'],r['package'],r['policy'])].append(r)
    for (v,p,policy),rs in sorted(groups.items()):
        a=summary([old[key(r)] for r in rs]);b=summary(rs)
        selected_summaries[(v,p)]=b;selected_rows[(v,p)]=rs
        if not any('Throwing_MeasuredThrow' in str(r['purchases']) for r in rs):
            selected_controls.append((v,p,len(rs),sum(comparable(r)!=comparable(old[key(r)]) for r in rs)))
        lines.append(f'| {v} | {p} | {a["obj"]}/{a["alive"]} | {b["obj"]}/{b["alive"]} | {a["hp"]:.2f}→{b["hp"]:.2f} | {a["actions"]:.2f}→{b["actions"]:.2f} |')
lines+=['','## Remaining health and survival at the bounded endpoint','',
'These predeclared secondary outcomes do not turn a partial/timeout into completed extraction. Remaining HP assigns zero to deaths rather than dropping them from the average. Shared-survivor differences compare the same world-seed subset alive in both arms, but different commands consume different later random draws; they are descriptive, not identical-luck causal estimates. Each row contains 100 seeds and the first 20 are already included.', '',
'| Condition | Measured/root alive | Alive only with Measured / only with root | Mean remaining HP, Measured/root (death=0) | Shared survivors | Shared-survivor mean HP difference |',
'|---|---:|---:|---|---:|---:|']
for variant in sorted({v for v,p in selected_rows}):
    a={r['seed']:r for r in selected_rows.get((variant,'measured'),[])}
    b={r['seed']:r for r in selected_rows.get((variant,'root'),[])}
    if not a or not b:continue
    assert set(a)==set(b)
    shared=[k for k in a if a[k]['survived'] and b[k]['survived']]
    alive_a=sum(r['survived'] for r in a.values());alive_b=sum(r['survived'] for r in b.values())
    only_a=sum(a[k]['survived'] and not b[k]['survived'] for k in a)
    only_b=sum(b[k]['survived'] and not a[k]['survived'] for k in a)
    hp_a=statistics.mean(max(0,r['hp']) if r['survived'] else 0 for r in a.values())
    hp_b=statistics.mean(max(0,r['hp']) if r['survived'] else 0 for r in b.values())
    hp_shared=statistics.mean(a[k]['hp']-b[k]['hp'] for k in shared) if shared else 0
    lines.append(f'| {variant} | {alive_a}/{alive_b} | {only_a}/{only_b} | {hp_a:.2f}/{hp_b:.2f} | {len(shared)} | {hp_shared:+.2f} |')
lines+=['','## Selected revision precision and ordinary alternatives','',
'These are the same exploratory Wilson/Newcombe risk-difference intervals specified before the one 100-seed expansion. Fixed seeds are not a random gameplay-population sample, the arms are treated independently, and no multiplicity correction or formal significance claim is made. A wide interval remains unresolved; there is no new seed expansion. The intervals compare current +4 Measured with unchanged same-budget arms, not +4 versus +2.', '',
'| Condition | Comparison | Objective difference | 95% interval | Width |',
'|---|---|---:|---|---:|']
for variant in sorted({v for v,p in selected_summaries}):
    measured=selected_summaries.get((variant,'measured'))
    if not measured:continue
    for other in ('root','puncture'):
        comparator=selected_summaries.get((variant,other))
        if not comparator:continue
        d,lo,hi=risk_difference(measured,comparator)
        lines.append(f'| {variant} | Measured − {other} | {100*d:+.1f} pp | [{100*lo:.1f}, {100*hi:.1f}] | {100*(hi-lo):.1f} pp |')
lines+=['','## Non-owner replay control','',
f'{sum(x[4] for x in controls)} full-matrix trials did not purchase Measured; {sum(x[5] for x in controls)} differ from the baseline after normalizing only the additive intermediate player-HP snapshot token. The selected 100-seed set independently checks {sum(x[2] for x in selected_controls)} non-owner replays, with {sum(x[3] for x in selected_controls)} differences. These sets overlap their first 20 seeds and are not independent trial totals. Any nonzero count requires inspection before causal attribution. The baseline lacked that intermediate HP field, so only its originally recorded fields can be compared.','',
'## Retention decision','',
'The generated tables are evidence, not an automatic retention vote. Record the explicit armored/scarce benefit and ordinary-condition reversal decision in `measured-revision-1.md`; do not silently treat a passing execution test or improved low-armor result as acceptance.','']
if any(x[5] for x in controls) or any(x[3] for x in selected_controls):
    lines[2:2]=['**REJECTED for causal before/after attribution:** non-owner replay controls differ. Resolve and rerun the comparison harness before using these tables to retain or cut Measured.', '']
(ROOT/'measured-revision-summary.md').write_text('\n'.join(lines))
print(f'Read {available} explicit revision trials; full-matrix non-owner differences {sum(x[5] for x in controls)} / {sum(x[4] for x in controls)}; selected100 differences {sum(x[3] for x in selected_controls)} / {sum(x[2] for x in selected_controls)}')
