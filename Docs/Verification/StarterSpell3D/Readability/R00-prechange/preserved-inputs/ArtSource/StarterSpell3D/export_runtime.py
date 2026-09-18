"""Export polished mesh art + sampled motion for the native Unity adapter.

Read-only .blend ingestion. Cone cells are an explicit bounded adaptation:
full-size authored triangles clipped per cell, then the original eased envelope
scales them around that cell. No studio actor, target, crop or stage is exported.
"""
import bpy,json,math,re,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'runtime';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=json.loads((ROOT/'manifest.json').read_text())
C=Matrix(((0,1,0),(0,0,1),(1,0,0)));C4=C.to_4x4();SOURCE=Vector((-2.35,0,0))
meshes=[];materials=[];matids={};checks=[];maxerr=0.0
def need(name,ok,actual):checks.append(dict(name=name,passed=bool(ok),actual=actual))
def matindex(ob):
 mat=ob.data.materials[0]
 if mat.name in matids:return matids[mat.name]
 rgba=mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value
 srgb=[v*12.92 if v<=.0031308 else 1.055*(v**(1/2.4))-.055 for v in rgba[:3]]
 index=len(materials);materials.append(dict(id=mat.name,rgbaSrgb=[round(v,7) for v in srgb]+[1.0]));matids[mat.name]=index;return index
def triangles(ob,world=False):
 me=ob.data;me.calc_loop_triangles();matrix=ob.matrix_world if world else Matrix.Identity(4)
 return [[matrix@me.vertices[index].co for index in tri.vertices] for tri in me.loop_triangles]
def addmesh(name,tris,material):
 vertices=[];normals=[];indices=[]
 for tri in tris:
  a,b,c=[C@v for v in tri];n=(b-a).cross(c-a)
  if n.length<1e-9:continue
  n.normalize();start=len(vertices)//3
  for v in (a,b,c):vertices.extend(round(float(q),7) for q in v);normals.extend(round(float(q),7) for q in n)
  indices.extend([start,start+1,start+2])
 if not indices:return None
 index=len(meshes);meshes.append(dict(id=name,vertices=vertices,normals=normals,triangles=indices,materialIndex=material));return name
def clip_plane(poly,axis,bound,keep_less):
 if not poly:return []
 result=[]
 for i,p in enumerate(poly):
  q=poly[(i+1)%len(poly)];pin=p[axis]<=bound+1e-8 if keep_less else p[axis]>=bound-1e-8;qin=q[axis]<=bound+1e-8 if keep_less else q[axis]>=bound-1e-8
  if pin:result.append(p)
  if pin!=qin:
   t=(bound-p[axis])/(q[axis]-p[axis]);result.append(p+(q-p)*t)
 return result
def clip_cell(tris,anchor):
 output=[]
 for tri in tris:
  poly=list(tri)
  for axis in [0,1]:
   poly=clip_plane(poly,axis,anchor[axis]-.5,False);poly=clip_plane(poly,axis,anchor[axis]+.5,True)
  for i in range(1,len(poly)-1):output.append([poly[0]-anchor,poly[i]-anchor,poly[i+1]-anchor])
 return output
def curve_scale(scene,ob):
 values=[]
 for frame in range(111):scene.frame_set(frame);values.append(max(float(v) for v in ob.scale))
 peak=max(values)
 return [min(1,max(0,v/peak)) if peak else 0 for v in values]
def trs_tracks(scene,ob,anchor):
 global maxerr
 result=[]
 for frame in range(111):
  scene.frame_set(frame);bpy.context.view_layer.update();m=C4@Matrix.Translation(-anchor)@ob.matrix_world@C4.transposed();p,q,s=m.decompose();back=Matrix.LocRotScale(p,q,s);error=max(abs(m[r][c]-back[r][c]) for r in range(4) for c in range(4));maxerr=max(maxerr,error)
  result.append(dict(position=[round(float(v),7) for v in p],rotation=[round(float(v),7) for v in (q.x,q.y,q.z,q.w)],scale=[round(float(v),7) for v in s]))
 # Rotation is underdetermined at zero scale. Carry a nearby visible orientation
 # across those samples so the reveal never invents a spin from identity.
 visible=[i for i,row in enumerate(result) if max(abs(v) for v in row['scale'])>1e-6]
 for i,row in enumerate(result):
  if max(abs(v) for v in row['scale'])<=1e-6 and visible:row['rotation']=result[min(visible,key=lambda k:abs(k-i))]['rotation'][:]
 return result
def piece(name,mesh,role,condition,anchor,poses,forward=0,lateral=0,variant=0,essential=True,adaptation=''):
 return dict(id=name,meshId=mesh,role=role,condition=condition,anchor=anchor,forwardCell=forward,lateralCell=lateral,variant=variant,reducedEssential=essential,nativeAdaptation=adaptation,poses=poses)
def essential(name,sid):
 if sid=='rime_grip' and name=='Rime__drifting_ice_chip_0':return True
 if any(word in name for word in ('curved_flame_wake','round_ember_finish','soft_fold_foam','far_side_foam','settling_splash','moving_warm','pale_blunt_rim','drifting_ice','soft_inner','gentle_unfurl','soft_bead','cushioned')):return False
 if sid=='conjure_rain':return any(name.endswith('_'+str(i)) for i in (0,4,8)) if 'short_stroke' in name else name.endswith('_0')
 if 'impact_split' in name:return name.endswith('_0') or name.endswith('_3')
 return True
library=dict(schemaVersion=1,fps=100,frameCount=111,releaseFrame=22,sourceBlendSha256=hashlib.sha256((ROOT/'starter_spells.blend').read_bytes()).hexdigest(),coordinateConvention='author X -> Unity Z; author Y -> Unity X; author Z -> Unity Y; indices preserve geometric normal winding; Unity front-face acceptance required',materials=materials,meshes=meshes,studies=[])
for spec in manifest['studies']:
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;sid=spec['id'];distance=spec['distance'];target=SOURCE+Vector((distance,0,0));effects=sorted([o for o in scene.objects if o.type=='MESH' and o.get('transientEffect')],key=lambda o:o.name);pieces=[]
 entry=dict(id=sid,contactFrame=spec['contactFrame'],clearFrame=spec['clearFrame'],authoredDistanceCells=distance,authoredMuzzleForward=.36,projectileProgress=[],pieces=pieces)
 carrier=next((o for o in scene.objects if o.name in ('Ember__travel_seed','Calm__travel_open_loop')),None)
 if carrier:
  for frame in range(111):scene.frame_set(frame);bpy.context.view_layer.update();entry['projectileProgress'].append(round((carrier.matrix_world.translation.x-SOURCE.x)/distance,7))
 else:entry['projectileProgress']=[0.0]*111
 for ob in effects:
  name=ob.name;mat=matindex(ob);env=curve_scale(scene,ob);peakframe=ob.get('peakFrame',spec['contactFrame']+7)
  if sid in ('flaming_hands','jet_blast'):
   cells=[(1,0)] if sid=='flaming_hands' else [(1,0),(2,-1),(2,0),(2,1)]
   scene.frame_set(math.floor(peakframe),subframe=peakframe%1);bpy.context.view_layer.update();full=triangles(ob,True)
   for forward,lateral in cells:
    anchor=SOURCE+Vector((forward,lateral,0));clipped=clip_cell(full,anchor);mesh=addmesh(name+'__cell_%s_%s'%(forward,lateral),clipped,mat)
    if not mesh:continue
    poses=[dict(position=[0,0,0],rotation=[0,0,0,1],scale=[round(value,7)]*3) for value in env]
    pieces.append(piece(mesh,mesh,'ConeCell','Always','Cell',poses,forward,lateral,essential=essential(name,sid),adaptation='Full-size authored triangles clipped to native cell; original eased scalar envelope centered inside that cell.'))
   continue
  if sid=='conjure_rain':
   crop=re.search(r'crop_(\d)_',name)
   if not crop or int(crop.group(1))!=0:continue
   anchor=SOURCE+Vector((1,-1,0));role='CropCell';condition='Always';anchorkey='Cell';forward=0;lateral=0;variant=0
  elif sid=='ground_surge':
   match=re.search(r'Surge__cell_(\d)_',name)
   n=int(match.group(1)) if match else int(name.rsplit('_',1)[1])+1 if name.startswith('Surge__stone_tongue_') else 4
   anchor=SOURCE+Vector((n,0,0));role='GroundCell';condition='Always';anchorkey='Cell';forward=n;lateral=0;variant=n-1
  elif sid=='ember_spit':
   head=any(parent.name=='Ember__travel_seed' for parent in [ob.parent,ob.parent.parent if ob.parent else None] if parent)
   role='ProjectileHead' if head else 'ProjectileTrail' if 'broken_soot' in name else 'TargetImpact';anchor=SOURCE if role!='TargetImpact' else target;condition='Always';anchorkey='ProjectileCarrier' if head else 'Source' if role=='ProjectileTrail' else 'Target';forward=lateral=variant=0
  elif sid=='calm':
   head=ob.parent and ob.parent.name=='Calm__travel_open_loop';role='ProjectileHead' if head else 'TargetImpact';condition='Always' if head else 'PacifiedApplied';anchor=SOURCE if head else target;anchorkey='ProjectileCarrier' if head else 'Target';forward=lateral=variant=0
  elif sid=='rime_grip' and name=='Rime__thin_cold_thread':
   scene.frame_set(math.floor(peakframe),subframe=peakframe%1);bpy.context.view_layer.update();full=triangles(ob,True)
   for n in range(1,4):
    anchor=SOURCE+Vector((n,0,0));clipped=clip_cell(full,anchor);mesh=addmesh(name+'__path_segment_'+str(n),clipped,mat)
    if not mesh:continue
    poses=[dict(position=[0,0,n],rotation=[0,0,0,1],scale=[round(value,7)]*3) for value in env]
    pieces.append(piece(mesh,mesh,'ProjectileTrail','Always','Source',poses,essential=False,adaptation='Short cell-bounded segments of the original thin thread; each center follows copied path progress.'))
   continue
  else:
   anchor=target;role='TargetImpact';condition='Always' if 'drifting_ice_chip' in name else 'FrozenApplied';anchorkey='Target';forward=lateral=variant=0
  mesh=addmesh(name,triangles(ob),mat);poses=trs_tracks(scene,ob,anchor)
  pieces.append(piece(name,mesh,role,condition,anchorkey,poses,forward,lateral,variant,essential(name,sid)))
 # A native-only water reaction derives flat plates from actual authored Rime chips.
 # This is a transient reaction variant, never a persistent Frozen-water state.
 if sid=='rime_grip':
  chip=next(o for o in effects if o.name.startswith('Rime__drifting_ice_chip_0'));base=triangles(chip);mat=matindex(chip)
  for n,(x,y,a) in enumerate([(-.18,-.13,.25),(.17,.09,2.1),(-.03,.22,4.3)]):
   rot=Matrix.Rotation(a,3,'Z');tris=[[rot@Vector((v.x*2.0,v.y*2.0,v.z*.35))+Vector((x,y,.045)) for v in tri] for tri in base];mesh=addmesh('Rime__freeze_water_plate_'+str(n),tris,mat);poses=[]
   for frame in range(111):
    t=(frame-22)/8 if frame<30 else 1 if frame<40 else (60-frame)/20;u=min(1,max(0,t));u=u*u*(3-2*u);poses.append(dict(position=[0,0,0],rotation=[0,0,0,1],scale=[round(u,7)]*3))
   pieces.append(piece(mesh,mesh,'ReactionCell','FreezeWater','Cell',poses,essential=True,adaptation='Low plate derivative of authored Rime chip; scaled2x across ground and0.35x in height; visible only for recorded freeze_water reaction.'))
 library['studies'].append(entry)
need('All seven entries exported',len(library['studies'])==7,len(library['studies']))
need('Every sampled matrix can be faithfully represented by TRS',maxerr<2e-5,maxerr)
need('No actor/stage/target/crop meshes in runtime library',all(not any(token in m['id'].lower() for token in ('stage__','target__','casterrig','character-teal','planter','crop__leaf')) for m in meshes),len(meshes))
for entry in library['studies']:
 need(entry['id']+' has named mesh pieces and111 samples',bool(entry['pieces']) and all(len(p['poses'])==111 for p in entry['pieces']),len(entry['pieces']))
 need(entry['id']+' clears every runtime piece',all(max(abs(v) for v in p['poses'][-1]['scale'])<.002 for p in entry['pieces']),len(entry['pieces']))
 if entry['id'] in ('flaming_hands','jet_blast'):
  expected={(1,0)} if entry['id']=='flaming_hands' else {(1,0),(2,-1),(2,0),(2,1)};actual={(p['forwardCell'],p['lateralCell']) for p in entry['pieces']};need(entry['id']+' preserves captured-cell stencil',actual==expected,sorted(actual))
  for p in entry['pieces']:
   m=next(m for m in meshes if m['id']==p['meshId']);vs=m['vertices'];need(p['id']+' remains inside its cell at every envelope scale',all(abs(vs[i])<=.50001 and abs(vs[i+2])<=.50001 for i in range(0,len(vs),3)) and all(0<=v<=1.00001 for pose in p['poses'] for v in pose['scale']),len(vs)//3)
 if entry['id']=='conjure_rain':need('Only one crop cluster exported',all('crop_0_' in p['id'] for p in entry['pieces']),[p['id'] for p in entry['pieces']])
for mat in materials:need(mat['id']+' is opaque',mat['rgbaSrgb'][3]==1,mat['rgbaSrgb'])
(OUT/'starter_spell_library.json').write_text(json.dumps(library,separators=(',',':'))+'\n')
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',sourceBlendSha256=library['sourceBlendSha256'],assertions=len(checks),passed=sum(c['passed'] for c in checks),meshCount=len(meshes),pieceCount=sum(len(s['pieces']) for s in library['studies']),triangleCount=sum(len(m['triangles'])//3 for m in meshes),maxTrsRecompositionError=maxerr,checks=checks)
(OUT/'export-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='checks'},indent=2))
if report['status']!='GREEN':raise SystemExit(1)
