"""Independent saved-.blend stencil audit. Run before/after geometry corrections."""
import bpy, json, re, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=json.loads((ROOT/'manifest.json').read_text());checks=[]
def need(name,condition,actual):checks.append(dict(name=name,passed=bool(condition),actual=actual))
def bounds(ob):
 points=[ob.matrix_world@v.co for v in ob.data.vertices]
 return [[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)]
for spec in manifest['studies']:
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;scene.frame_set(round(spec['contactFrame']+5));bpy.context.view_layer.update()
 objects=list(scene.objects);caster=next(o for o in objects if o.get('nativeCellRoot'));cx,cy,_=caster.matrix_world.translation
 if spec['id']=='ground_surge':
  stitches=[o for o in objects if re.match(r'Surge__cell_\d_angular_stitch_',o.name)]
  need('Ground Surge has two angular stitches in each of four cells',len(stitches)==8,len(stitches))
  for ob in stitches:
   cell=int(re.search(r'cell_(\d)',ob.name).group(1));bb=bounds(ob);center=cx+cell
   need(ob.name+' stays inside actual native cell',bb[0][0]>=center-.5-1e-5 and bb[0][1]<=center+.5+1e-5 and bb[1][0]>=cy-.5 and bb[1][1]<=cy+.5,dict(bounds=bb,cellCenter=[center,cy]))
  for ob in [o for o in objects if o.name.startswith('Surge__stone_tongue_')]:
   cell=int(ob.name.rsplit('_',1)[1])+1;bb=bounds(ob);center=cx+cell
   need(ob.name+' scales at its own native cell',bb[0][0]>=center-.5-1e-5 and bb[0][1]<=center+.5+1e-5,dict(bounds=bb,cellCenter=[center,cy]))
 if spec['id']=='jet_blast':
  lips=[o for o in objects if o.name.startswith('Jet__far_side_lip_')]
  need('Jet Blast has two separate far-side impact lips',len(lips)==2,len(lips))
  for ob in lips:
   side=-1 if '_-1' in ob.name else 1;bb=bounds(ob)
   need(ob.name+' is in far-row side cell',bb[0][0]>=cx+1.5-1e-5 and bb[0][1]<=cx+2.5+1e-5 and bb[1][0]>=side-.5 and bb[1][1]<=side+.5,dict(bounds=bb,nativeOffset=[2,side]))
 if spec['id']=='flaming_hands':
  fx=[o for o in objects if o.type=='MESH' and o.get('transientEffect') and o.get('visualBounds','CellSurface')=='CellSurface']
  need('Flaming Hands has nonempty transient geometry',bool(fx),len(fx))
  for ob in fx:
   bb=bounds(ob);need(ob.name+' stays in selected adjacent cell',bb[0][0]>=cx+.5-1e-5 and bb[0][1]<=cx+1.5+1e-5 and bb[1][0]>=cy-.5 and bb[1][1]<=cy+.5,bb)
 if spec['id']=='rime_grip':
  clamps=[o for o in objects if o.name.startswith('Rime__blunt_split_clamp_')]
  need('Rime has exactly three visible blunt clamps',len(clamps)==3,len(clamps))
  need('Rime clamps leave head and shoulders clear',all(bounds(o)[2][1]<.75 for o in clamps),[bounds(o)[2] for o in clamps])
 if spec['id']=='calm':
  loop=next(o for o in objects if o.name=='Calm__travel_open_loop');recipient=next(o for o in objects if o.name=='Calm__sentient_studio_recipient')
  for frame in [32,39]:
   scene.frame_set(frame);bpy.context.view_layer.update();pos=tuple(loop.matrix_world.translation);target=tuple(recipient.matrix_world.translation)
   need('Calm loop reaches recipient at native frame '+str(frame),abs(pos[0]-target[0])<.13 and abs(pos[1]-target[1])<1e-5,dict(loop=pos,recipient=target))
  for ob in [o for o in objects if o.name.startswith('Calm__relaxing_angular_crossing_')]:
   bb=bounds(ob);center=[sum(axis)/2 for axis in bb]
   need(ob.name+' relaxes beside recipient, not scene origin',abs(center[0]-target[0])<.5 and abs(center[1]-target[1])<.5,dict(bounds=bb,recipient=target))
 if spec['id']=='conjure_rain':
  beds=[o for o in objects if o.name.startswith('Crop__planter_')];actual=[]
  for ob in beds:
   x,y,_=ob.matrix_world.translation;offset=(x-cx,y-cy);actual.append(list(offset))
   need(ob.name+' is on an integer native cell within radius three',all(abs(v-round(v))<1e-5 and abs(v)<=3+1e-5 for v in offset),list(offset))
  need('Rain shows exactly three valid crop cells',len(beds)==3,actual)
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',assertions=len(checks),passed=sum(c['passed'] for c in checks),checks=checks)
out=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else ROOT/'geometry-audit.json';out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(status=report['status'],assertions=report['assertions'],passed=report['passed'],report=str(out)),indent=2))
if report['status']!='GREEN':raise SystemExit(1)
