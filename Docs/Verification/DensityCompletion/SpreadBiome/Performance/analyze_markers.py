#!/usr/bin/env python3
"""Read native recorder receipts without treating latest-completed samples as same-frame."""
import argparse,csv,gzip,hashlib,json,math,statistics
from pathlib import Path

def stats(values):
    values=sorted(values)
    if not values:return {'count':0}
    def quantile(p):
        k=(len(values)-1)*p;lo=int(k);hi=min(lo+1,len(values)-1)
        return values[lo]+(values[hi]-values[lo])*(k-lo)
    return dict(count=len(values),mean=statistics.fmean(values),p50=quantile(.5),p95=quantile(.95),max=values[-1])

def analyze(folder):
    raw=folder/'profile-markers.csv.gz';meta=json.loads((folder/'profile-markers.json').read_text())
    with gzip.open(raw,'rt') as f:rows=list(csv.DictReader(f))
    descriptions={x['name']:x for x in meta['markers']}
    def sample(row,name):
        value=row.get(name)
        if value in ('',None):return None
        value=float(value)
        return value/1e6 if descriptions.get(name,{}).get('units')=='TimeNanoseconds' else value
    completed=[r for r in rows[2:] if (sample(r,'COO.ZoneRenderer.RenderZone') or 0)>0]
    clean=[r for r in rows[2:] if sample(r,'COO.ZoneRenderer.RenderZone')==0]
    lag=[]
    for offset in range(-2,3):
        delta=[]
        for i in range(2,len(rows)):
            j=i+offset
            if j<2 or j>=len(rows):continue
            v=sample(rows[j],'COO.ZoneRenderer.LateUpdate')
            if v is not None:delta.append(abs(float(rows[i]['rendererSnapshotMs'])-v))
        lag.append(dict(recorderRowMinusSnapshotRow=offset,absoluteDifferenceMs=stats(delta)))
    names=[n for n in descriptions if n.startswith('COO.')]
    bygroup={}
    for name,group in [('allAfterFirstTwo',rows[2:]),('completedRedrawMarkerPositive',completed),('completedRedrawMarkerZero',clean)]:
        bygroup[name]={n:stats([v for r in group if (v:=sample(r,n)) is not None]) for n in names}
    return dict(runId=meta['runId'],rawSha256=hashlib.sha256(raw.read_bytes()).hexdigest(),sampleRows=len(rows),
        scope='Editor-only native route. First two observations excluded. Latest-completed profiler values may lag current renderer snapshot. Redraw groups use their own completed RenderZone marker; no same-row dirty attribution. Nested values overlap and are not additive. No RenderCell measurements when detailed cell profiling is disabled.',
        lagComparison=lag,groups=bygroup,settingsBefore=meta['before'],settingsAfter=meta['after'])

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--output',type=Path);a=p.parse_args()
    text=json.dumps(analyze(a.folder),indent=2)+'\n'
    if a.output:a.output.write_text(text)
    else:print(text,end='')
