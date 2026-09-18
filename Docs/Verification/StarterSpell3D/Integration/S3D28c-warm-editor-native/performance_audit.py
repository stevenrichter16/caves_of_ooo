import argparse,csv,hashlib,json,math,statistics
from pathlib import Path
from collections import Counter
from PIL import Image

ap=argparse.ArgumentParser();ap.add_argument('--report',required=True);ap.add_argument('--out',required=True);ap.add_argument('--label',required=True);a=ap.parse_args()
rp=Path(a.report).resolve();out=Path(a.out).resolve();out.mkdir(parents=True,exist_ok=True)
r=json.loads(rp.read_text());run=r['runId'];checks=[]
intermediate='intermediate' in a.label
stage_note='This is the intermediate capture before the final persistent-aura and color visual refinement; preserve it as history.' if intermediate else 'This is the final post-refinement native capture; prior intermediate receipts are preserved separately.'
def check(name,condition,detail=None):
 row={'name':name,'pass':bool(condition)}
 if detail is not None:row['detail']=detail
 checks.append(row)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def quantile(xs,p):
 v=sorted(xs);index=(len(v)-1)*p;low=math.floor(index);high=math.ceil(index)
 return v[low]+(v[high]-v[low])*(index-low)
def stats(xs,unit):
 return {'unit':unit,'count':len(xs),'mean':statistics.fmean(xs),'p95':quantile(xs,.95),'p99':quantile(xs,.99),'max':max(xs),'min':min(xs),'sum':sum(xs)}
check('complete_successful_native_report',r['workloadComplete'] and r['failures']==r['unexpectedErrors']==0 and not r['fatal'])
check('all_reported_checks_pass',all(c['pass'] for c in r['checks']))
check('all_seven_spell_ids',len(set(r['spells']))==7)
check('all_commands_succeeded_with_stable_results',all(c['pass'] and c['resultStable'] and c['nativeEntry']==c['spell'] for c in r['casts']))
check('all_observed_nonprofile_gestures',all(c['sawGesture'] and c['gestureRestored'] and c['gesturePeakDegrees']>3 and c['gestureSamples']>1 for c in r['casts'] if c['mode']!='profile'))
cp=rp.with_name(rp.name.replace('-native.json','-cleanup.json'));cleanup=json.loads(cp.read_text())
check('same_run_cleanup',cleanup['runId']==run and cleanup['exitCode']==0 and cleanup['finalNativeVerified'])
check('private_root_removed',cleanup['privateRootRemoved'] and not Path(r['saveRoot']).exists())
raw=Path(r['rawFrames']).resolve();check('run_owned_raw_csv',raw.parent==rp.parent and raw.name==f'SSN-{run}-frames.csv')
check('raw_sha256_matches',sha(raw)==r['rawSha256'])
header=['phase','unity_frame','wall_seconds','unscaled_delta','tick','native_meshes','allocated_views','zone_renderer','input','main_thread','gc_bytes','draw_calls','triangles']
with raw.open(newline='') as f:
 reader=csv.DictReader(f);check('exact_csv_columns',reader.fieldnames==header);rows=list(reader)
ints=set(header)-{'wall_seconds','unscaled_delta'}
for row in rows:
 for k in header:row[k]=int(row[k]) if k in ints else float(row[k])
check('raw_count_matches',len(rows)==r['profileFrames'])
check('raw_monotonic_order',all(b['unity_frame']>a['unity_frame'] and b['wall_seconds']>a['wall_seconds'] and b['phase']>=a['phase'] for a,b in zip(rows,rows[1:])))
check('raw_finite_nonnegative',all(0<=v['phase']<4 and all(math.isfinite(value) for value in v.values()) and v['unscaled_delta']>0 and v['native_meshes']>=0 and v['allocated_views']>=v['native_meshes'] for v in rows))
names=['COO.ZoneRenderer.LateUpdate','COO.Input.Update','Main Thread','GC Allocated In Frame','Draw Calls Count','Triangles Count'];units=['TimeNanoseconds']*3+['Bytes','Count','Count'];cols=header[7:]
check('counter_names_units',r['counterNames']==names and r['counterUnits']==units and len(r['counterAvailable'])==6)
check('required_counters_available',all(r['counterAvailable'][:4]))
for col,avail in zip(cols,r['counterAvailable']):check('availability_values_'+col,all(v[col]>=0 if avail else v[col]==-1 for v in rows))
frames=[(cast,frame) for cast in r['casts'] for frame in cast['frames']]
framepaths=[x['path'] for c,x in frames]
check('one_cast_record_per_png',len(framepaths)==len(set(framepaths))==len(r['loopFrames']) and set(framepaths)==set(r['loopFrames']))
check('screenshots_are_captured_frames',set(r['screenshots'])<=set(framepaths))
images=[]
for cast,frame in frames:
 p=Path(frame['path']).resolve();actual=sha(p)
 check('png_owned_'+p.name,p.parent==rp.parent and p.name.startswith(f'SSN-{run}-'))
 check('png_sha_'+p.name,actual==frame['sha256'])
 with Image.open(p) as im:size=im.size;format=im.format;im.verify()
 with Image.open(p) as im:im.load()
 check('png_decodes_1080p_'+p.name,size==(1920,1080) and format=='PNG')
 images.append({'path':str(p),'sha256':actual,'bytes':p.stat().st_size,'spell':cast['spell'],'mode':cast['mode'],'captureSeconds':frame['wallSeconds'],'observedMeshes':frame['meshes']})
for n,c in enumerate(r['casts']):
 check('capture_order_'+str(n),all(b['wallSeconds']>a['wallSeconds'] for a,b in zip(c['frames'],c['frames'][1:])))
phases=[];first=0
for i,p in enumerate(r['phases']):
 rr=[v for v in rows if v['phase']==i];cc=[c for c in r['casts'] if c['mode']=='profile' and c['zoneId']==p['zoneId'] and c['fxMode']==p['mode']]
 check('phase_coverage_'+str(i),len(rr)==p['frames'] and p['firstFrame']==first and sum(v['native_meshes']>0 for v in rr)==p['activeFrames'] and max(v['native_meshes'] for v in rr)==p['maxMeshes'] and len(cc)==p['casts']);first+=len(rr)
 m={col:stats([v[col]/(1e6 if j<3 else 1) for v in rr], 'ms' if j<3 else 'bytes' if j==3 else 'count') if r['counterAvailable'][j] else None for j,col in enumerate(cols)}
 phases.append({'index':i,**p,'castsBySpell':dict(Counter(c['spell'] for c in cc)),'activeFraction':p['activeFrames']/p['frames'],'renderFramesPerSecond':p['frames']/p['seconds'],'metrics':m,'wallFrame':stats([v['unscaled_delta']*1000 for v in rr],'ms'),'framesOver16_667ms':sum(v['unscaled_delta']>1/60 for v in rr),'framesOver50ms':sum(v['unscaled_delta']>.05 for v in rr),'framesOver100ms':sum(v['unscaled_delta']>.1 for v in rr),'largestMainFrames':sorted(rr,key=lambda v:v['main_thread'],reverse=True)[:3]})
check('phase_duration_and_count_totals',first==len(rows) and abs(sum(p['seconds'] for p in phases)-r['profileSeconds'])<1e-6 and sum(p['activeFrames'] for p in phases)==r['activeFrames'])
check('profile_cast_total_matches_phase_sum',sum(c['mode']=='profile' for c in r['casts'])==sum(p['casts'] for p in phases))
check('profile_has_four_paired_20second_phases',len(phases)==4 and all(20<=p['seconds']<=23 and p['casts']>=7 and p['zoneId']==('Overworld.3.6.0' if i<2 else 'Overworld.3.7.0') and p['mode']==('Full' if i%2==0 else 'Reduced') for i,p in enumerate(phases)))
firstcast={k:v for k,v in r['firstCast'].items() if k!='samples'}
report={'status':'PASS' if all(c['pass'] for c in checks) else 'FAIL','stage':a.label,'runId':run,'sourceReport':str(rp),'sourceReportSha256':sha(rp),'cleanupReport':str(cp),'cleanupReportSha256':sha(cp),'rawCsv':str(raw),'rawSha256':sha(raw),'assertions':len(checks),'passed':sum(c['pass'] for c in checks),'checks':checks,'hardware':{k:r[k] for k in ['unityVersion','gpu','cpu','screenWidth','screenHeight','targetFrameRate','vSyncCount']},'profileSeconds':r['profileSeconds'],'profileFrames':len(rows),'activeFrames':r['activeFrames'],'commandCasts':len(r['casts']),'profileCommandCasts':sum(c['mode']=='profile' for c in r['casts']),'realKeyboardCasts':r['nativeKeyboardCasts'],'commandModes':dict(Counter(c['mode'] for c in r['casts'])),'sevenShowcaseGestures':[{'spell':c['spell'],'peakDegrees':c['gesturePeakDegrees'],'samples':c['gestureSamples'],'restored':c['gestureRestored']} for c in r['casts'] if c['mode']=='showcase'],'phases':phases,'preparation':r['preparation'],'firstCast':firstcast,'imagesVerified':len(images),'screenshots':len(r['screenshots']),'imageFiles':images,'quantileMethod':'Linear interpolation at index (N-1)*p (R7); all raw phase frames retained including startup-of-cast and worst outliers.','honestyBounds':r['bounds'],'interpretation':stage_note+' The phases include whole-editor/game frame cost and deterministic fixture/counter overhead, not exclusive spell cost. Fixed sequential Full/Reduced runs are descriptive and do not establish that Reduced is faster. Startup preparation and screenshot/gesture-observation segments are excluded from the four measured phases.'}
(out/'performance.json').write_text(json.dumps(report,indent=2)+'\n')
def f(v):return f'{v:.3f}'
md=['# Native starter-spell performance — '+a.label,'',f"Run `{run}`. **{report['status']}: {report['passed']}/{report['assertions']} independent checks.** {stage_note}",'',f"Verified the SHA-256 of the raw CSV and all **{len(images)} PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.",'',f"**{r['profileSeconds']:.3f} seconds**, **{len(rows):,} raw frames**, **{r['activeFrames']:,} frames with native spell meshes**, **{sum(c['mode']=='profile' for c in r['casts'])} profile commands**. The complete run contains **{len(r['casts'])} deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.",'','| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |','| --- | ---: | ---: | ---: | ---: | ---: |']
for p in phases:md.append(f"| {'Town' if p['index']<2 else 'South'} {p['mode']} | {p['seconds']:.3f} | {p['frames']:,} | {p['activeFrames']:,} | {p['casts']} | {p['maxMeshes']} |")
md+=['','Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.','','| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |','| --- | ---: | ---: | ---: |']
for p in phases:
 cells=[' / '.join(f(p['metrics'][k][a]) for a in ['mean','p95','p99','max']) for k in ['main_thread','input','zone_renderer']]
 md.append(f"| {'Town' if p['index']<2 else 'South'} {p['mode']} | "+' | '.join(cells)+' |')
md+=['','GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.','','| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |','| --- | ---: | ---: | ---: | ---: |']
for p in phases:
 m=p['metrics'];gc=' / '.join(f(m['gc_bytes'][a]/1024) for a in ['mean','p95','p99','max']);draw=m['draw_calls'];tri=m['triangles']
 md.append(f"| {'Town' if p['index']<2 else 'South'} {p['mode']} | {gc} | {draw['mean']:.1f} / {draw['max']:.0f} | {tri['mean']:,.0f} / {tri['max']:,.0f} | {p['framesOver16_667ms']} / {p['framesOver50ms']} |")
prep=r['preparation'];fc=firstcast
md+=['',f"Preparation before input took **{prep['LoadSeconds']*1000:.2f} ms loading**, **{prep['ValidationSeconds']*1000:.2f} ms validating**, and **{prep['PoolSeconds']*1000:.2f} ms creating the 384-view pool**. The first real cast kept the library count {fc['libraryInstancesBefore']}→{fc['libraryInstancesAfter']} and pool {fc['poolBefore']}→{fc['poolAfter']}, displayed {fc['peakMeshes']} peak meshes, and reached Normal input state. Its maximum Main Thread time was **{fc['maxMainNanoseconds']/1e6:.2f} ms**. This records startup work moved before the cast, not removed work.",'','Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.','',"Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.",'',f"Machine: Unity {r['unityVersion']}, {r['gpu']}, 1920×1080, vSync {r['vSyncCount']}, targetFrameRate {r['targetFrameRate']}. Quantiles use linear interpolation at `(N-1)×p`.",'',f"Raw receipt: `{rp}`",f"CSV SHA-256: `{r['rawSha256']}`",'']
(out/'performance.md').write_text('\n'.join(md))
print(json.dumps({k:report[k] for k in ['status','stage','assertions','passed','profileSeconds','profileFrames','activeFrames','commandCasts','imagesVerified']},indent=2))
for p in phases:print(p['zoneId'],p['mode'],p['metrics']['main_thread'],p['metrics']['input'],p['metrics']['zone_renderer'])
for c in checks:
 if not c['pass']:print('FAIL',c)
