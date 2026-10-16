#!/usr/bin/env python3
"""A3-D12 selected exploratory precision gate. No trial generation or Unity writes."""
import gzip
import json
import math
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent

def load(name,initial=False):
    if initial and name=="W6-6":
        frozen=ROOT/"comparisons/superseded-before-stun-seeding/W6-6.json.gz"
        if frozen.exists():
            with gzip.open(frozen,"rt") as f:return json.load(f)["trials"]
    p=ROOT/'comparisons'/(name+'.json')
    baseline=ROOT/'comparisons'/'baseline-measured-plus2'/(name+'.json.gz')
    if name.startswith('W1-') and baseline.exists():
        with gzip.open(baseline,'rt') as f:return json.load(f)['trials']
    if p.exists(): return json.loads(p.read_text())['trials']
    p=p.with_suffix(p.suffix+'.gz')
    if p.exists():
        with gzip.open(p,'rt') as f:return json.load(f)['trials']
    return []

def wilson(k,n):
    z=1.959963984540054;p=k/n;d=1+z*z/n
    center=(p+z*z/(2*n))/d
    half=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/d
    return max(0,center-half),min(1,center+half)

def difference(a,b,success):
    ka=sum(success(r) for r in a);kb=sum(success(r) for r in b)
    pa=ka/len(a);pb=kb/len(b);la,ua=wilson(ka,len(a));lb,ub=wilson(kb,len(b))
    d=pa-pb
    low=max(-1,d-math.sqrt((pa-la)**2+(ub-pb)**2))
    high=min(1,d+math.sqrt((ua-pa)**2+(pb-lb)**2))
    return ka,kb,d,low,high

pairs=[]
for v in ('Viper/AV0/daggers2','MarlbackScrabbler/AV0/daggers2','Viper/AV4/daggers1','MarlbackScrabbler/AV4/daggers1'):
    for a,b in [('root','bank'),('measured','root'),('measured','puncture')]:
        pairs.append(('W1',2,v,a,b,'objective-and-weapon'))
for v in ('gas','dry','wall','brace'):pairs.append(('W4',2,v,'driving','root','enemy-defeated-then-withdraw'))
for v in ('gas-at-current-position','dry-control'):pairs.append(('W4-reversal',2,v,'driving','root','enemy-defeated-then-withdraw'))
for v in ('quiet','pursuit','acid','free-dog','busy-dog'):pairs.append(('W5',2,v,'recovery','bank','portable-custody-and-extraction'))
for b in ('bank','water-acrobatics'):pairs.append(('W6',6,'outbound-return','all5-water',b,'objective-and-weapon'))

def rows(world,budget,variant,package,extension=False):
    data=load(f'{world}-{budget}'+('-precision100' if extension else ''),initial=not extension)
    policy='ordinary' if world=='W1' and package=='bank' else 'owned-opportunity' if world=='W1' else 'secure' if world=='W5' else 'spend-for-distance' if world=='W6' else None
    result=[r for r in data if r['variant']==variant and r['package']==package and (policy is None or r['policy']==policy)]
    if result:
        seeds=[r['seed'] for r in result]
        assert len(seeds)==len(set(seeds)),(world,variant,package,'duplicate seed')
        assert set(seeds)==set(range(101,201 if extension else 121)),(world,variant,package,'unexpected seed set')
    return result

lines=['# A3-D12 selected precision check','',
'**Evidence versions:** W1 remains the historical +2 Measured baseline; the current +4 decision and intervals are separate in `measured-revision-1.md` / `measured-revision-summary.md`. W4/W5 mechanics are unchanged. W6 initial columns preserve the original gate-selection history, while its final 100-seed columns use current +4 gameplay and corrected fallback-stun seeding. They must not be read as a causal before/after tuning comparison.', '',
'Audit 3 declared a single 20→100 seed expansion when a primary success-difference interval is wider than 20 percentage points. The initial matrix used 20 seeds and did not calculate this gate; it was neither satisfied nor superseded. This supplemental selection and interval recipe were recorded before running the extension, after observing the initial matrix. They are an explicit bounded refinement, not an originally preregistered statistical analysis.','',
'We use 95% Wilson score intervals for each binary arm and the Newcombe square-root combination for their risk difference. The formula treats arms independently: matching a world seed does not guarantee identical subsequent random draws after different actions. These fixed seed ensembles are exploratory game tests, not random samples from a defined population; the interval is a conservative precision heuristic, not a formal confidence claim about all gameplay. There is no multiplicity correction or significance threshold. A favorable/unfavorable interval does not itself authorize further seed expansion or balance tuning; any separately approved bounded balance revision must preserve and distinguish these baseline receipts.','',
'Only the exact pairs below receive this inference/precision check. All other initial matrix groups remain descriptive 20-seed observations. The selected extension reuses the existing real command policies, packages, geometry, levels and finite supplies with seeds 101–200. It includes the original 20 seeds, so analysis replaces those selected groups and never pools them as 120 observations. Production RNG is not modified by this analysis. The W6 final receipt version includes the separately approved +4 balance revision; its original gate-selection counts remain historical. A width above 20 points at 100 remains explicitly unresolved; the predeclared one expansion does not promise that every interval will become narrow.','',
('W6 initial selection counts remain frozen in the pre-stun-correction archive. The corrected canonical first 20 records now match the current 100-seed receipt exactly; the original gate decision is not silently recalculated after the harness correction and +4 revision. `explicit-stream-comparisons-green.xml` records the corrected 28-case execution.' if '--stun-seeded' in sys.argv else 'W6 initial selection counts remain frozen in the pre-stun-correction archive. Its final columns are withheld until --stun-seeded validates the current canonical first 20 records. The original gate decision is not silently recalculated.'), '',
'The physical extension completed in `precision100-physical-green.xml` (5/5 cases; 4,100 actual trials). Across the displayed baseline/current evidence versions, only 8 of the 25 selected intervals reach width at most 20 points; the other 17 remain explicitly imprecise. No extra seeds are added to force narrower results. `precision-overlap.md` verifies the repeated original seed outcomes before tuning.', '',
'W1 primary success means objective plus the weapon retained; survival and actions are secondary, and a faster death is not a benefit. W4 primary success means the enemy was defeated before withdrawal; these fixtures do not show a player-survival benefit. W5 means portable custody plus extraction; its one-action saving and dog displacement are separate observed mechanics. W6 means the finite expedition objective plus the weapon; inactive purchased buttons receive no causal credit. Focused/W7 outcome selection is recorded separately by the payload reviewer before its extension.','',
'| World / variant | A − B | Initial successes A/B | Initial Δ | Initial 95% interval | Width | Expand? | 100-seed successes A/B | 100-seed interval / width |',
'|---|---|---:|---:|---|---:|---|---:|---|']
for world,budget,v,a,b,outcome in pairs:
    aa=rows(world,budget,v,a);bb=rows(world,budget,v,b)
    assert aa and bb,(world,v,a,b)
    succ=lambda r:r['outcome']==outcome
    ka,kb,d,lo,hi=difference(aa,bb,succ)
    ea=rows(world,budget,v,a,True);eb=rows(world,budget,v,b,True)
    extended='Pending';interval='Pending'
    if ea and eb and (world!='W6' or '--stun-seeded' in sys.argv):
        xa,xb,xd,xlo,xhi=difference(ea,eb,succ)
        extended=f'{xa}/{len(ea)} vs {xb}/{len(eb)}';interval=f'[{xlo*100:.1f}, {xhi*100:.1f}] / {(xhi-xlo)*100:.1f} pp'
    if world=='W6' and '--stun-seeded' in sys.argv and ea and eb:
        canonical={ (r['variant'],r['package'],r['policy'],r['seed']):r for r in load('W6-6') }
        for r in ea+eb:
            if r['seed']<=120:assert r==canonical[(r['variant'],r['package'],r['policy'],r['seed'])], 'Corrected W6 overlap mismatch'
    lines.append(f'| {world} {v} | {a} − {b} | {ka}/{len(aa)} vs {kb}/{len(bb)} | {d*100:+.1f} pp | [{lo*100:.1f}, {hi*100:.1f}] | {(hi-lo)*100:.1f} pp | {"Yes" if hi-lo>.20 else "No"} | {extended} | {interval} |')
lines+=['','## Retention and reversal limits','',
'- **Sure Grip:** real eligible retry and pre-second-point natural use are observed; W1 compares it against ordinary throws at matched cost, including armor/scarcity conditions. This does not establish universal superiority to keeping the weapon or saving the point. A separate high-Agility cheap-dip matrix was not run.',
'- **Measured Throw:** the +2 baseline here failed to establish its armored/scarce objective role. Revision 1 is retained at +4 for the separately recorded AV4 scarce-projectile endpoint survival/remaining-health benefit, with a low-armor Puncture reversal. Its objective differences against root-only remain wide and imprecise; partial living endpoints are not successful extraction. See `measured-revision-1.md` for the explicit benefit/reversal/no-dominance decision.',
'- **Driving Throw:** destination gas provides an actual benefit; pushing an enemy out of existing gas is an actual harmful reversal. Dry/wall/brace preserve no-benefit paid cooldown controls. Player survival was identical in these fixtures.',
'- **Recovery:** actual carried/equipped origins and exact-unit custody have correctness controls. Ordinary pickup can finish the same objective; Recovery saves a paid action in quiet/pursuit/acid/busy-dog cases, but cannot retrieve a free dog\'s current item and does not remove entry damage. Binary success alone understates this tactical timing comparison.',
'- **Focused Delivery:** payload reviewer separately reconciles adjacent-ally sparing against loss of useful broad exposure, and W7 ground-control outcomes. Natural use of this earned child remains unverified.',
'- **Whole package:** W6 intentionally compares shared finite policies, not optimized use of every active skill. Its result cannot establish the value of inactive Driving/Focused purchases. Naturally earned 4/6 SP and renewable supply pacing remain separate unverified gates.','']
(ROOT/'precision-gate.md').write_text('\n'.join(lines))
print(f'Wrote {len(pairs)} selected primary comparisons; selected100 includes the original20 and is not an independent sample.')
