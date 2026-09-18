"""Non-destructive Blender polish stage: original bundle -> separate candidate.
Run after the ordinary kit builder. Never overwrites the input or touches Assets.
"""
import argparse,ast,bpy,hashlib,json,math,shutil,sys
from pathlib import Path
from mathutils import Matrix,Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from sculpt_normals import blend_corner
REPO=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--kit',choices=['Village3D','SpawnRing3D','MultiCellPilot3D'],required=True);p.add_argument('--source',type=Path);p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
SOURCE=(args.source or REPO/'ArtSource'/args.kit).resolve();OUT=args.output.resolve();assert SOURCE!=OUT and not OUT.exists(),'new separate output required'
OUT.mkdir(parents=True);(OUT/'reports').mkdir();(OUT/'renders').mkdir();shutil.copytree(SOURCE/'models',OUT/'models');shutil.copytree(SOURCE/'textures',OUT/'textures')
manifest='manifest.json' if args.kit=='Village3D' else 'catalog.json';shutil.copy2(SOURCE/manifest,OUT/manifest)
for filename in ['native-definition.json','art-definition.json','layout.json','model-contract.json']:
 if (SOURCE/filename).exists():shutil.copy2(SOURCE/filename,OUT/filename)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
blend={'Village3D':'village_master.blend','SpawnRing3D':'ring_kit.blend','MultiCellPilot3D':'pilot_kit.blend'}[args.kit]
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/blend));SCENE=bpy.context.scene;assert not SCENE.get('coo_model_polish_recipe'),'Start from a fresh builder bundle, not a previously polished master';SCENE.frame_set(0);bpy.context.view_layer.update()
catalog=json.loads((SOURCE/manifest).read_text());MODELS={}
for row in catalog['models']:
 coll=bpy.data.collections.get(row['id']);assert coll is not None,row['id'];MODELS[row['id']]={**row,'collection':coll}
prefix={'Village3D':'Village','SpawnRing3D':'SpawnRing','MultiCellPilot3D':'Pilot'}[args.kit]
MAT=bpy.data.materials.get(prefix+'Palette');WATER=bpy.data.materials.get('PilotTar' if args.kit=='MultiCellPilot3D' else prefix+'Water');assert MAT is not None and WATER is not None,'Explicit palette/water material contract'
exportpath=REPO/('ArtSource/Village3D/build_scene.py' if args.kit=='Village3D' else 'ArtSource/SpawnRing3D/mesh_kit.py')
nodes=[n for n in ast.parse(exportpath.read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='export_collection'];exec(compile(ast.Module(body=[nodes[-1]],type_ignores=[]),str(exportpath),'exec'),globals())
# All geometry-bearing fields: a pure shading pass must preserve this signature.
def structural(mesh):
 mesh.calc_loop_triangles()
 data=[[[float(x) for x in v.co] for v in mesh.vertices],[list(p.vertices) for p in mesh.polygons],[[list(t.uv) for t in uv.data] for uv in mesh.uv_layers],[[[g.group,g.weight] for g in v.groups] for v in mesh.vertices]]
 return hashlib.sha256(json.dumps(data,separators=(',',':')).encode()).hexdigest()
def gallery(stage):
 ids={
 'Village3D':['character-teal','character-gold','equipment-blade','equipment-shield','equipment-pack','equipment-helmet','barrel-0','crate-0','central-well','market-stall-0','rocks-0','shrub-0'],
 'SpawnRing3D':['ring-grain-ridge-0','ring-rock-0','ring-player','ring-helmwood-frog','ring-sari-snake','ring-sky-sari','ring-mosshulk','ring-yellowfoot-wayfarer','ring-hollow-log-0','ring-copper-pipe','ring-steam-vent','ring-shambler'],
 'MultiCellPilot3D':['PilotRidgeN_0','PilotRidgeE_1','PilotRidgeNE_2','PilotRidgeSE_3','PilotBoulder_0','PilotRock_1','PilotTar_0','PilotPipe_0','PilotSteamVent_0','PilotMawToad','PilotWardline','PilotHermit']}[args.kit]
 # Names are checked against the actual catalog; selection is written to report.
 chosen=[i for i in ids if i in MODELS]
 if len(chosen)<12:
  for i,row in MODELS.items():
   if i not in chosen and row.get('kind') not in ('terrain-detail','tile','tile-overlay'):
    chosen.append(i)
    if len(chosen)==12:break
 preview=bpy.data.scenes.new('Polish_'+stage);preview.render.engine='CYCLES';preview.cycles.samples=16;preview.render.threads_mode='FIXED';preview.render.threads=2
 preview.render.resolution_x=1600;preview.render.resolution_y=1150;preview.render.resolution_percentage=100
 preview.world=bpy.data.worlds.new('Studio_'+stage);preview.world.use_nodes=True;preview.world.node_tree.nodes['Background'].inputs[0].default_value=(.42,.45,.48,1);preview.world.node_tree.nodes['Background'].inputs[1].default_value=.45
 for index,mid in enumerate(chosen):
  row=MODELS[mid];coll=row['collection'];size=row['boundsSize'];centre=row['boundsCenter'];scale=min(2.55/max(size['x'],size['z'],.001),2.2/max(size['y'],.001))
  o=bpy.data.objects.new(mid+'__gallery',None);o.instance_type='COLLECTION';o.instance_collection=coll;preview.collection.objects.link(o);o.scale=(scale,)*3
  x=(index%4-1.5)*3.6;y=(1-index//4)*4.1;o.location=(x-centre['x']*scale,y-centre['z']*scale,-(centre['y']-size['y']/2)*scale+.04)
  font=bpy.data.curves.new(mid+'Label','FONT');font.body=mid; font.align_x='CENTER';font.size=.16;ink=bpy.data.materials.get('LabelInk') or bpy.data.materials.new('LabelInk');ink.diffuse_color=(.015,.02,.017,1);font.materials.append(ink);label=bpy.data.objects.new(mid+'Label',font);preview.collection.objects.link(label);label.location=(x,y-1.5,.045)
 camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));preview.collection.objects.link(camera);camera.location=(0,-15,22.23);camera.rotation_euler=(Vector((0,0,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=15.8;preview.camera=camera
 lamp=bpy.data.objects.new('Key',bpy.data.lights.new('Key','AREA'));preview.collection.objects.link(lamp);lamp.location=(-5,-5,12);lamp.data.energy=2200;lamp.data.shape='DISK';lamp.data.size=5
 # Backdrop is presentation only, never an exported model.
 mesh=bpy.data.meshes.new('Backdrop');mesh.from_pydata([(-12,-12,0),(12,-12,0),(12,12,0),(-12,12,0)],[],[(0,1,2,3)]);floor=bpy.data.objects.new('Backdrop',mesh);preview.collection.objects.link(floor)
 m=bpy.data.materials.new('Backdrop');m.diffuse_color=(.18,.20,.19,1);mesh.materials.append(m)
 preview.view_settings.view_transform='AgX';preview.render.filepath=str(OUT/'renders'/(stage+'.png'));bpy.ops.render.render(write_still=True,scene=preview.name)
 bpy.data.scenes.remove(preview);return chosen
selection=gallery('before') if args.render else []
ledger=[]
for mid,row in MODELS.items():
 changed=0;corners=0;max_angle=0;objects=[]
 # Tile surfaces retain their original normals. No cosmetic terrain swell.
 terrain=row.get('kind') in ('terrain-detail','tile','tile-overlay') or mid.startswith('ground-')
 for ob in row['collection'].objects:
  if ob.type!='MESH':continue
  if ob.data.users>1:ob.data=ob.data.copy()
  mesh=ob.data;signature=structural(mesh);mesh.update();old=[tuple(n.vector) for n in mesh.corner_normals]
  # Fine leaf, fibre and feather geometry already has intentional highlight planes.
  detail=any(token in ob.name.lower() for token in ('leaf','scrub','fibre','grain','feather','glint','lichen','flower','scale_diamond'))
  if terrain or detail or ob.get('keepSeparate',False) or len(mesh.polygons)<2:continue
  adjacent=[[] for _ in mesh.vertices]
  for face in mesh.polygons:
   for vi in face.vertices:adjacent[vi].append(face.index)
  normals=[];rounded=any(f.use_smooth for f in mesh.polygons)
  # Rounded organic pieces retain smooth shading. Flat chips retain 28% of
  # their authored plane normal, and only shallow <=35° seams blend.
  for face in mesh.polygons:
   for li in face.loop_indices:
    if face.normal.length<1e-12:normals.append(old[li]);continue
    if rounded and not face.use_smooth:normals.append(tuple(face.normal));continue
    candidates=[mesh.polygons[i] for i in adjacent[mesh.loops[li].vertex_index]]
    if rounded:candidates=[f for f in candidates if f.use_smooth]
    target=blend_corner(tuple(face.normal),[(tuple(f.normal),f.area) for f in candidates],80 if rounded else 35,1 if rounded else .72)
    # Existing organic curvature is the artist's baseline; area weighting is
    # only a restrained correction, never a replacement by broad flat caps.
    normals.append(blend_corner(old[li],[(target,1)],90,.20) if rounded else target)
  delta=[math.degrees(math.acos(max(-1,min(1,sum(a*b for a,b in zip(n,b)))))) for n,b in zip(normals,old)]
  nchanged=sum(d>.1 for d in delta)
  if nchanged:
   for f in mesh.polygons:f.use_smooth=True
   mesh.normals_split_custom_set(normals);changed+=1;corners+=nchanged;max_angle=max(max_angle,max(delta));objects.append(ob.name)
  assert structural(mesh)==signature,(mid,ob.name,'geometry changed')
 if changed:export_collection(mid)
 ledger.append({'id':mid,'changedMeshes':changed,'changedCorners':corners,'maxNormalDegrees':max_angle,'objects':objects,'geometryPreserved':True,'beforeFbxSha256':sha(SOURCE/row['path']),'afterFbxSha256':sha(OUT/row['path'])})
 print('POLISH',mid,changed,corners,flush=True)
if args.render:gallery('after')
# Packed textures travel with the editable source; exported palette files remain identical.
SCENE['coo_model_polish_recipe']='sculpt-normal-v2'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/blend))
report={'kit':args.kit,'source':str(SOURCE),'sourceBlendSha256':sha(SOURCE/blend),'models':ledger,'changedModels':sum(r['changedMeshes']>0 for r in ledger),'gallery':selection,'geometryPreserved':True,'recipe':'sculpt-normal-v2','angle':35,'planeRetention':.28}
(OUT/'reports/polish.json').write_text(json.dumps(report,indent=2)+'\n');print('POLISH_COMPLETE',args.kit,report['changedModels'],len(ledger),flush=True)
