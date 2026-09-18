#!/usr/bin/env python3
"""Independently recompute native profile percentiles/counts from retained gzip CSV."""
import csv,gzip,hashlib,json,math,pathlib,statistics,sys
def validate_walk_steps(phase,steps,index):
    """Accept exact native moves and explicitly identified unsuccessful attempts."""
    errors=[]
    def need(ok,message):
        if not ok:errors.append(f'phase{index} '+message)
    counts={'moved':0,'target_rejected':0,'input_without_movement':0};consecutive_unmoved=0;previous=None
    integers=['phase','targetX','targetY','beforeX','beforeY','afterX','afterY','beforeTick','afterTick','beforeHp','afterHp']
    flags=['pass','inputIssued','targetAvailable','targetExcluded']
    for n,s in enumerate(steps):
        valid=isinstance(s,dict) and all(type(s.get(k)) is int for k in integers) and all(type(s.get(k)) is bool for k in flags)
        need(valid,f'attempt{n} missing/invalid adaptive schema')
        if not valid:continue
        outcome=s.get('outcome');need(outcome in counts,f'attempt{n} unknown/incomplete outcome')
        if outcome not in counts:continue
        counts[outcome]+=1
        need(s.get('zoneId')==phase['zoneId'] and s['phase']==index,f'attempt{n} identity')
        seconds=s.get('seconds');need(type(seconds) in (int,float) and phase['startSeconds']<=seconds<=phase['startSeconds']+phase['seconds'],f'attempt{n} outside phase')
        need(s.get('inputState')=='Normal' and s['beforeHp']>0 and s['afterHp']>0,f'attempt{n} unhealthy/state')
        before=(s['beforeX'],s['beforeY']);after=(s['afterX'],s['afterY']);target=(s['targetX'],s['targetY'])
        delta=(target[0]-before[0],target[1]-before[1]);keys={(1,0):'D',(-1,0):'A',(0,1):'S',(0,-1):'W'}
        need(delta in keys and s.get('key')==keys.get(delta),f'attempt{n} key/target is not cardinal')
        need(0<target[0]<79 and 0<target[1]<24 and 0<before[0]<79 and 0<before[1]<24,f'attempt{n} crosses native border')
        if previous is None:
            need(before==(phase['startX'],phase['startY']) and s['beforeTick']==phase['startTick'],f'attempt{n} start receipt')
        else:
            need(before==(previous['afterX'],previous['afterY']) and s['beforeTick']==previous['afterTick'],f'attempt{n} discontinuous native path')
            need(type(seconds) in (int,float) and type(previous.get('seconds')) in (int,float) and seconds>=previous['seconds'],f'attempt{n} time moved backwards')
        previous=s
        if outcome=='target_rejected':
            need(not s['inputIssued'] and not s['pass'] and (not s['targetAvailable'] or s['targetExcluded']),f'attempt{n} false preflight rejection')
            need(after==before and s['afterTick']==s['beforeTick'] and s['afterHp']==s['beforeHp'],f'attempt{n} no-input record changed native state')
        else:
            need(s['inputIssued'] and s['targetAvailable'] and not s['targetExcluded'],f'attempt{n} invalid issued-input preflight')
            if outcome=='moved':
                need(s['pass'] and after==target and s['afterTick']>s['beforeTick'],f'attempt{n} false successful step')
                consecutive_unmoved=0
            else:
                consecutive_unmoved+=1
                need(not s['pass'] and after==before and s['afterTick']>=s['beforeTick'],f'attempt{n} false unmoved-input receipt')
                need(consecutive_unmoved<4,f'attempt{n} exceeded four-unmoved abort bound')
    need(counts['moved']==phase.get('successfulSteps') and counts['moved']>=2,'native successful step count mismatch')
    need(counts['target_rejected']==phase.get('rejectedTargets'),'rejected-target count mismatch')
    need(counts['input_without_movement']==phase.get('unmovedInputs'),'unmoved-input count mismatch')
    need(counts['moved']+counts['input_without_movement']==phase.get('inputAttempts'),'issued-input count mismatch')
    if previous is not None:
        need((previous['afterX'],previous['afterY'])==(phase['endX'],phase['endY']) and previous['afterTick']==phase['endTick'],'final step/phase endpoint mismatch')
    return errors

p=pathlib.Path(sys.argv[1]).resolve();report=json.loads(p.read_text());errors=[]
def need(ok,message):
    if not ok:errors.append(message)
raw=pathlib.Path(report['rawFrames']);need(raw.is_file(),'raw CSV missing')
need(hashlib.sha256(raw.read_bytes()).hexdigest()==report['rawSha256'],'raw gzip SHA256 mismatch')
with gzip.open(raw,'rt',newline='') as f:
    reader=csv.DictReader(f);headers=reader.fieldnames;rows=list(reader)
need(len(rows)==report['frames'],'frame count mismatch')
need([int(r['sample']) for r in rows]==list(range(len(rows))),'sample indices not complete/in order')
need(all(int(b['unity_frame'])>int(a['unity_frame']) for a,b in zip(rows,rows[1:])),'duplicate or backward Unity frame observations')
need(all(int(b['phase_id'])>=int(a['phase_id']) and float(b['seconds'])>=float(a['seconds']) for a,b in zip(rows,rows[1:])),'phase/time ordering changed')
phases=report['phases'];need(len(phases)==24,'24 declared bind/idle/walk phases required')
expected_zones={f'Overworld.{x}.{y}.0' for y in range(5,8) for x in range(2,5) if (x,y)!=(3,6)}
need({p['zoneId'] for p in phases}==expected_zones,'exact eight-zone profile required')
need([p['kind'] for p in phases]==['first_binding','idle','walk']*8,'phase order changed')
need(report['steadySeconds']>=80 and report['steadySeconds']<=90,'steady duration outside80–90s')
need(abs(sum(x['seconds'] for x in phases if x['kind']!='first_binding')-report['steadySeconds'])<1e-5,'steady duration sum mismatch')
need(report['valid'] and report['complete'] and not report['overflow'] and report['badResolutionFrames']==0,'runtime workload invalid')
need(report['screenWidth']==1920 and report['screenHeight']==1080 and report['worldSeed']==729490642,'native resolution/seed mismatch')
need(len(report['steps'])==sum(p.get('inputAttempts',0)+p.get('rejectedTargets',0) for p in phases if p['kind']=='walk'),'orphan or omitted profile attempt receipts')
computed=[]
for i,phase in enumerate(phases):
    frames=[r for r in rows if int(r['phase_id'])==i]
    need(len(frames)==phase['frames'],f'phase{i} frame count mismatch')
    need(all(r['zone']==phase['zoneId'] and r['kind']==phase['kind'] for r in frames),f'phase{i} raw identity mismatch')
    if phase['kind']=='idle':need(phase['startTick']==phase['endTick'] and phase['startX']==phase['endX'] and phase['startY']==phase['endY'],f'phase{i} idle control drift')
    if phase['kind']=='walk':
        steps=[s for s in report['steps'] if s['zoneId']==phase['zoneId']]
        errors.extend(validate_walk_steps(phase,steps,i))
    phase_metrics=[m for m in report['metrics'] if m['phase']==i]
    need(len(phase_metrics)==len(headers[10:])+1 and {m['name'] for m in phase_metrics}==set(headers[10:])|{'Engine frame duration'},f'phase{i} metric coverage mismatch')
    for original in phase_metrics:
        col='engine_frame_ms' if original['name']=='Engine frame duration' else original['name']
        values=sorted(float(r[col]) for r in frames if r[col]!='NA')
        row={'phase':i,'zone':phase['zoneId'],'kind':phase['kind'],'name':original['name'],'unit':original['unit'],
             'count':len(values),'missing':len(frames)-len(values),'positive':sum(v>0 for v in values)}
        for key in ['count','missing','positive']:need(row[key]==original[key],f'{i}/{col}/{key} mismatch')
        if original['available']:
            need(bool(values),f'{i}/{col} available without samples')
            if values:
                row.update(mean=statistics.mean(values),max=max(values),p95=values[math.ceil(.95*len(values))-1],p99=values[math.ceil(.99*len(values))-1])
                for key in ['mean','max','p95','p99']:
                    need(math.isclose(row[key],original[key],rel_tol=1e-6,abs_tol=1e-6),f'{i}/{col}/{key} mismatch')
        else:row['available']=False
        computed.append(row)
run=report['runId'];native_path=p.parent/f'R3D-{run}-native.json';cleanup_path=p.parent/f'R3D-{run}-cleanup.json'
if native_path.is_file() and cleanup_path.is_file():
    native=json.loads(native_path.read_text());cleanup=json.loads(cleanup_path.read_text())
    need(native['runId']==run and native['saveRoot']==report['saveRoot'] and native['failures']==0 and native['unexpectedErrors']==0 and native['workloadComplete'],'native workload receipt mismatch/failure')
    for key in ['shutdownObserved','shutdownRootHeld','shutdownSavingUnregistered','displayPreferencesRestored','inputSettingsRestored']:need(native.get(key) is True,'native teardown failed: '+key)
    need(cleanup['runId']==run and cleanup['privateRoot']==report['saveRoot'] and cleanup['exitCode']==0 and cleanup['totalUnexpectedErrors']==0,'final cleanup identity/errors')
    for key in ['privateRootRemoved','scenesRestored','gameViewRestored','finalNativeVerified','seedSettingRestored','inheritedSaveRootRestored','lastGamePreferenceRestored']:need(cleanup.get(key) is True,'cleanup failed: '+key)
    need(not pathlib.Path(report['saveRoot']).exists(),'private save root still exists')
else:errors.append('Final native/cleanup receipt missing; profile is not yet accepted')
out={'status':'PASS' if not errors else 'FAIL','runId':run,'mode':report['mode'],'sourceReport':str(p),'rawSha256':report['rawSha256'],'frames':len(rows),'errors':errors,'recomputedMetrics':computed,
     'bounds':'Recomputes recorded counters and validates this run’s native/cleanup receipts. Does not validate image quality, metric availability on another device, or production-build performance. Interpret per-zone/per-phase values; pooled frame weighting would favor faster regions.'}
output=p.with_name(p.stem+'-recomputed.json');output.write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({'status':out['status'],'report':str(output),'frames':len(rows),'errors':errors},indent=2))
sys.exit(bool(errors))
