"""Saved-mesh seam samples using real native cell-anchor spacing.

These checks sample named grid borders/tube endpoints, not whole-effect AABBs.
They establish geometric lap and missing-owner breaks; native pixels remain the
final test of whether the flowing silhouette is visually convincing.
"""
import bpy, hashlib, json, math, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
SOURCE=Vector((-2.35,0,0))
path=ROOT/'starter_spells.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
manifest=json.loads((ROOT/'manifest.json').read_text())
checks=[]
def need(name,ok,actual):checks.append(dict(name=name,passed=bool(ok),actual=actual))
def scene_for(sid):
 spec=next(s for s in manifest['studies'] if s['id']==sid)
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
 return spec,scene
def sample(ob,index,degrees,forward,lateral=0):
 world=ob.matrix_world@ob.data.vertices[index].co
 local=world-SOURCE-Vector((forward,lateral,0))
 step=math.sqrt(2) if degrees%90 else 1
 if ob.get('visualBounds','CellSurface')=='CellSurface':
  local.x/=step;local.y/=step
 local+=Vector((step*forward,step*lateral,0))
 a=math.radians(degrees)
 # Real rotated grid positions; projected comparisons below use its actual
 # forward/lateral basis. The diagonal is not merely a rotated studio board.
 return Vector((local.x*math.cos(a)-local.y*math.sin(a),local.x*math.sin(a)+local.y*math.cos(a),local.z))
def axis(degrees,lateral=False):
 a=math.radians(degrees)+(math.pi/2 if lateral else 0)
 return Vector((math.cos(a),math.sin(a),0))
def center_ring(ob,first,degrees,forward):
 start=0 if first else len(ob.data.vertices)-6
 return sum((sample(ob,start+k,degrees,forward) for k in range(6)),Vector())/6

spec,scene=scene_for('jet_blast')
for frame in [spec['contactFrame']+8,spec['contactFrame']+14,spec['contactFrame']+21]:
 scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
 for degrees in range(0,360,45):
  gaps=[]
  for left,right in [(-1,0),(0,1)]:
   a=bpy.data.objects[f'RD_Jet_cell_2_{left}_connected_fold'];b=bpy.data.objects[f'RD_Jet_cell_2_{right}_connected_fold']
   # Matching u rows of the actual authored top surface; each side owns the
   # pieces separately. A negative signed gap is a physical lateral lap.
   for row in [3,6,9,12]:
    pa=sample(a,row*6+5,degrees,2,left);pb=sample(b,row*6,degrees,2,right)
    gaps.append((pb-pa).dot(axis(degrees,True)))
  need(f'Jet internal lateral lap at frame{frame:g} direction{degrees}',max(gaps)<-.025,dict(maxSignedGap=max(gaps),sampledBorderGaps=gaps))
  a=bpy.data.objects['RD_Jet_cell_1_0_connected_fold'];b=bpy.data.objects['RD_Jet_cell_2_0_connected_fold']
  gaps=[(sample(b,c,degrees,2)-sample(a,12*6+c,degrees,1)).dot(axis(degrees)) for c in [2,3]]
  need(f'Jet near/far lap at frame{frame:g} direction{degrees}',max(gaps)<-.025,dict(sampledForwardGaps=gaps))

spec,scene=scene_for('ground_surge')
for frame in [spec['contactFrame'],spec['contactFrame']+8,spec['contactFrame']+21]:
 scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
 for degrees in range(0,360,45):
  obs=[bpy.data.objects.get(f'RD_Surge_cell_{n}_air_stitch') or bpy.data.objects[f'RD_Surge_cell_{n}_root_0'] for n in range(1,5)]
  starts=[center_ring(ob,True,degrees,n+1) for n,ob in enumerate(obs)]
  ends=[center_ring(ob,False,degrees,n+1) for n,ob in enumerate(obs)]
  gaps=[(starts[n+1]-ends[n]).dot(axis(degrees)) for n in range(3)]
  need(f'Surge adjacent elevated stitch at frame{frame:g} direction{degrees}',max(gaps)<-.025,dict(signedEndpointGaps=gaps))
  missing=(starts[2]-ends[0]).dot(axis(degrees))
  need(f'Surge removed middle owner leaves a break at frame{frame:g} direction{degrees}',missing>.25,dict(clearForwardGap=missing))
  step=math.sqrt(2) if degrees%90 else 1
  forward=[ends[n].dot(axis(degrees))-step*(n+1) for n in range(4)]
  need(f'Surge first/last/truncated endpoints do not add forward reach at frame{frame:g} direction{degrees}',max(forward)<=.50001,dict(localForwardEnds=forward))
  leading=[max(sample(ob,i,degrees,n+1).dot(axis(degrees))-step*(n+1) for i in range(len(ob.data.vertices))) for n,ob in enumerate(obs)]
  # Includes actual cap vertices, not only the endpoint's centreline. The
  # diagonal retained root leads by about .35m at full size.
  need(f'Surge complete leading cap stays behind diagonal root at frame{frame:g} direction{degrees}',max(leading)<=.35,dict(fullVertexForwardExtents=leading))

def projected_triangles(ob,degrees,forward,lateral):
 ob.data.calc_loop_triangles();f=axis(degrees);l=axis(degrees,True);result=[]
 for tri in ob.data.loop_triangles:
  ps=[sample(ob,i,degrees,forward,lateral) for i in tri.vertices]
  result.append([(p.dot(f),p.dot(l)) for p in ps])
 return result
def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def inside(p,t):
 ds=[cross(t[i],t[(i+1)%3],p) for i in range(3)]
 return min(ds)>=-1e-6 or max(ds)<=1e-6
def missing_area(side,degrees,put_back=False):
 removed=bpy.data.objects[f'RD_Jet_cell_2_{side}_connected_fold'];triangles=projected_triangles(removed,degrees,2,side)
 remaining=[]
 for forward,lateral in [(1,0),(2,-1),(2,0),(2,1)]:
  if forward==2 and lateral==side and not put_back:continue
  remaining+=projected_triangles(bpy.data.objects[f'RD_Jet_cell_{forward}_{lateral}_connected_fold'],degrees,forward,lateral)
 lost=total=0
 for tri in triangles:
  area=abs(cross(*tri))*.5
  if area<1e-8:continue
  total+=area;center=tuple(sum(v[k] for v in tri)/3 for k in range(2))
  if not any(inside(center,t) for t in remaining):lost+=area
 return lost/total
spec,scene=scene_for('jet_blast');scene.frame_set(math.ceil(spec['contactFrame'])+14);bpy.context.view_layer.update()
for degrees in range(0,360,45):
 for side in [-1,1]:
  fraction=missing_area(side,degrees)
  need(f'Jet removed side{side} leaves its own substantial open area direction{degrees}',fraction>.25,dict(projectedTriangleCentroidAreaLost=fraction,minimum=.25))
need('Repopulating the missing Jet owner is rejected by its open-area gate',missing_area(-1,45,True)<.001,missing_area(-1,45,True))

# Genuine in-memory mesh counterchecks. These never save the delivered file.
ob=bpy.data.objects['RD_Jet_cell_2_-1_connected_fold'];old=[v.co.copy() for v in ob.data.vertices]
for v in ob.data.vertices:v.co.y-=1.0
bpy.context.view_layer.update();other=bpy.data.objects['RD_Jet_cell_2_0_connected_fold']
damaged_gap=(sample(other,6*6,45,2,0)-sample(ob,6*6+5,45,2,-1)).dot(axis(45,True))
need('Separating actual Jet border geometry is rejected',damaged_gap>0,damaged_gap)
for v,p in zip(ob.data.vertices,old):v.co=p
spec,scene=scene_for('ground_surge');scene.frame_set(math.ceil(spec['contactFrame'])+14);bpy.context.view_layer.update()
bridge=bpy.data.objects.get('RD_Surge_cell_4_air_stitch')
if bridge:
 old=[v.co.copy() for v in bridge.data.vertices]
 for v in list(bridge.data.vertices)[-6:]:v.co.x+=1
 bpy.context.view_layer.update();leading=max(sample(bridge,i,45,4).dot(axis(45))-4*math.sqrt(2) for i in range(len(bridge.data.vertices)))
 need('Actual forward-extending bridge cap is rejected',leading>.35,leading)
 for v,p in zip(bridge.data.vertices,old):v.co=p

# Full normalized exported pieces prove unchanged lower contacts and five
# unrelated spells. Material identity replaces global table ordinals.
data=json.loads((ROOT/'runtime/starter_spell_library.json').read_text())
baseline=json.loads((ROOT/'revisions/readability-candidate2/runtime/starter_spell_library.json').read_text())
def normalized(data,sid,surface_only=False):
 study=next(s for s in data['studies'] if s['id']==sid);meshes={m['id']:m for m in data['meshes']};result=[]
 for p in study['pieces']:
  if surface_only and p.get('visualBounds')!='CellSurface':continue
  m=dict(meshes[p['meshId']]);m['material']=data['materials'][m.pop('materialIndex')]
  result.append(dict(piece=p,mesh=m))
 return result
for sid in ['ember_spit','flaming_hands','rime_grip','calm','conjure_rain']:
 need(sid+' exported per-spell content unchanged',normalized(data,sid)==normalized(baseline,sid),len(normalized(data,sid)))
for sid in ['jet_blast','ground_surge']:
 need(sid+' exact CellSurface geometry and poses unchanged',normalized(data,sid,True)==normalized(baseline,sid,True),len(normalized(data,sid,True)))
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',sourceBlendSha256=hashlib.sha256(path.read_bytes()).hexdigest(),assertions=len(checks),passed=sum(c['passed'] for c in checks),checks=checks,boundary='Actual saved-mesh border/end samples; visual continuity and depth/fog still need native pixels')
out=Path(sys.argv[sys.argv.index('--')+1]);out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='checks'},indent=2))
if report['status']!='GREEN':raise SystemExit(1)
