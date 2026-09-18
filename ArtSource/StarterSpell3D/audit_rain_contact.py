"""Measure actual first Rain geometry at exact contact; reject a subpixel reveal.

This source-space gate complements the unchanged actual Unity GPU readback.
The probe's512px/7cell raster must get >=1px width and>=3px projected height.
"""
from pathlib import Path
import bpy,sys,json,math,hashlib
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
spec=next(s for s in json.loads((ROOT/'manifest.json').read_text())['studies'] if s['id']=='conjure_rain');scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
rows=[]
def measure(ob):
 scene.frame_set(22);bpy.context.view_layer.update();pts=[ob.matrix_world@v.co for v in ob.data.vertices]
 width=(max(v.x for v in pts)-min(v.x for v in pts))*512/7
 height=(max(v.z for v in pts)-min(v.z for v in pts))*math.cos(math.radians(56))*512/7
 return dict(name=ob.name,widthPixels=width,projectedHeightPixels=height,passed=width>=1 and height>=3)
for i in range(3):rows.append(measure(bpy.data.objects['Rain__crop_%d_short_stroke_0'%i]))
# Actual keyed mesh scale mutation. Never save the damaged in-memory source.
ob=bpy.data.objects['Rain__crop_0_short_stroke_0'];ob.keyframe_insert('scale',frame=22);ob.scale=(.0354947,)*3;ob.keyframe_insert('scale',frame=22)
control=measure(ob);rows.append(dict(name='Original subpixel-sized actual contact mutation rejected',passed=not control['passed'],measured=control))
report=dict(status='GREEN' if all(r['passed'] for r in rows) else 'RED',sourceBlendSha256=hashlib.sha256((ROOT/'starter_spells.blend').read_bytes()).hexdigest(),assertions=len(rows),passed=sum(r['passed'] for r in rows),checks=rows)
out=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else ROOT/'rain-contact-audit.json';out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if report['status']!='GREEN':raise SystemExit(1)
