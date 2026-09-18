"""Actual sampled scale envelopes; rejects abrupt/malformed in-memory animations."""
import bpy,json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=json.loads((ROOT/'manifest.json').read_text());checks=[];rows=[]
def need(name,ok,actual):checks.append(dict(name=name,passed=bool(ok),actual=actual))
def samples(scene,ob):
 values=[]
 for frame in range(0,111):
  scene.frame_set(frame);values.append(max(float(v) for v in ob.scale))
 peak=max(values)
 return values,[v/peak if peak else 0 for v in values]
def valid(values):
 peak=max(values);normalized=[v/peak if peak else 0 for v in values]
 maxstep=max(abs(b-a) for a,b in zip(normalized,normalized[1:]))
 return min(values)>=-1e-6 and maxstep<=.30+1e-6 and values[-1]<.002
for spec in manifest['studies']:
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
 effects=[o for o in scene.objects if o.type=='MESH' and o.get('transientEffect')]
 worst=0;bad=[]
 for ob in effects:
  values,normalized=samples(scene,ob);step=max(abs(b-a) for a,b in zip(normalized,normalized[1:]));worst=max(worst,step)
  if not valid(values):bad.append(dict(name=ob.name,maxNormalizedStepPer10ms=step,minScale=min(values),endScale=values[-1]))
 need(spec['id']+' has bounded non-popping effect envelopes',len(effects)>0 and not bad,dict(meshes=len(effects),worstStepPer10ms=worst,failures=bad))
 rows.append(dict(id=spec['id'],meshes=len(effects),worstStep=worst,failures=bad))
# Mutate a real independent mesh action only in memory, never the source file.
spec=manifest['studies'][1];scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
ob=next(o for o in scene.objects if o.type=='MESH' and o.get('transientEffect'))
baseline,_=samples(scene,ob)
if all(c['passed'] for c in checks):need('Unmodified reveal control passes before mutation',valid(baseline),max(baseline))
ob.animation_data_clear()
for frame,scale in [(0,0),(20,0),(21,1),(45,1),(60,0),(110,0)]:ob.scale=(scale,)*3;ob.keyframe_insert('scale',frame=frame)
mutated,_=samples(scene,ob);need('Actual one-frame pop is rejected',not valid(mutated),dict(maxJump=max(abs(b-a) for a,b in zip(mutated,mutated[1:]))))
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',assertions=len(checks),passed=sum(c['passed'] for c in checks),sourceSha256=hashlib.sha256((ROOT/'starter_spells.blend').read_bytes()).hexdigest(),checks=checks)
out=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else ROOT/'reveal-motion-audit.json';out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='checks'},indent=2))
if report['status']!='GREEN':raise SystemExit(1)
