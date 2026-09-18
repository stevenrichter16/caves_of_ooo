"""First missing-anatomy batch: real, rigged volumes; isolated candidate output."""
import bpy,bmesh,math,random,json,argparse,sys,ast,shutil
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for d in ('models','textures','renders'): (OUT/d).mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.fps=24
MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'mesh_kit.py','exec'),globals())
# New palette preserves accepted first64 swatches and adds three study colors.
COLORS[56]=('indigo',(.15,.19,.34));COLORS[57]=('lichen',(.43,.48,.27));COLORS[58]=('amber',(.88,.54,.16));CINDEX={n:i for i,(n,c) in enumerate(COLORS)}
shutil.copy2(ROOT/'ArtSource/SpawnRing3D/textures/SpawnRingPalette.png',OUT/'textures/WorldPalette.png')
img=bpy.data.images.load(str(OUT/'textures/WorldPalette.png'));pix=list(img.pixels[:]);w,h=img.size
for idx in (56,57,58):
 for y in range((idx//16)*h//8,(idx//16+1)*h//8):
  for x in range((idx%16)*w//16,(idx%16+1)*w//16):
   j=(y*w+x)*4;v=.97+.03*math.sin(x*.31)*math.sin(y*.23);pix[j:j+4]=[c*v for c in COLORS[idx][1]]+[1]
img.pixels.foreach_set(pix);img.filepath_raw=str(OUT/'textures/WorldPalette.png');img.file_format='PNG';img.save()
MAT=bpy.data.materials.new('WorldPalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;shader=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=.82;WATER=MAT
# Reuse the verified rig/export functions without executing the ring world build.
tree=ast.parse((ROOT/'ArtSource/SpawnRing3D/build_ring.py').read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='rig_parts');exec(compile(ast.Module(body=[fn],type_ignores=[]),'ring_rig_parts','exec'),globals())

def endemic(bp):
 mid='world-'+bp;coll=model(mid,'actor','feet-root');weights={};rng=random.Random(bp)
 def bind(ob,bone='Body'):weights[ob.name]=bone;return ob
 bones=[('Root',(0,0,0),(0,0,.06),None),('Body',(0,0,.08),(0,0,.35),'Root'),('Head',(0,-.13,.16),(0,-.32,.17),'Body')]
 frog=bp=='SummitSinger';gecko=bp=='PrickleBrowGecko';body='wood' if frog else 'indigo' if gecko else 'lichen';light='wood_light' if frog else 'amber' if gecko else 'moss_light'
 bind(ellipsoid('Compact_trunk' if frog else 'Flattened_lizard_trunk',(0,.03,.18),(.17 if frog else .14,.22,.135 if frog else .075),body,3))
 bind(ellipsoid('Warm_throat',(0,-.155,.10),(.105,.11,.065),light,2),'Head')
 bind(ellipsoid('Broad_short_head' if frog else 'Wedge_head',(0,-.195,.18),(.16 if frog else .11,.12,.095 if frog else .065),body,2),'Head')
 for sign,side in [(-1,'L'),(1,'R')]:
  eye=(sign*(.12 if frog else .09),-.235,.255 if frog else .225)
  bind(ellipsoid('Raised_eye_brow',eye,(.052,.048,.036),light,2),'Head');bind(ellipsoid('Gold_iris',(eye[0],eye[1]-.02,eye[2]+.017),(.029,.018,.026),'amber',2),'Head')
  bind(ellipsoid('Vertical_pupil',(eye[0],eye[1]-.033,eye[2]+.02),(.007,.007,.023),'black',2),'Head')
  bind(ellipsoid('Eye_catchlight',(eye[0]-.008,eye[1]-.037,eye[2]+.029),(.008,.006,.009),'cream',1),'Head')
  if gecko:
   a=(eye[0]+sign*.012,eye[1]+.01,eye[2]+.035);b=(eye[0]+sign*.07,eye[1]+.075,eye[2]+.135)
   bind(beam('Long_supraciliary_spine',a,b,.012,'cream',6),'Head')
  for rear,yy in [(False,-.115),(True,.18)]:
   tag=('LegRear.' if rear else 'LegFront.')+side;hip=(sign*.1,yy,.14);elbow=(sign*(.265 if rear else .23),yy+(.08 if rear else .055),.08);foot=(sign*.23,yy-.12,.025)
   bones.append((tag,hip,foot,'Body'))
   bind(ellipsoid('Folded_thigh' if rear and frog else 'Shoulder',hip,(.087 if rear and frog else .049,.10 if rear and frog else .055,.07),body,2),tag)
   bind(beam('Upper_limb',hip,elbow,.046 if rear and frog else .022,body,8),tag);bind(beam('Sharp_elbow_forelimb',elbow,foot,.023,light,7),tag)
   for j in range(3 if frog else 5):
    spread=(j-(1 if frog else 2))*.027;tip=(foot[0]+sign*(.04+abs(spread)*.35),foot[1]-.065+spread,.018)
    bind(beam('Toe',foot,tip,.009,light,5),tag)
    if gecko:bind(ellipsoid('Adhesive_toe_pad',tip,(.017,.022,.012),'cream',1),tag)
 if frog:
  # Raised, low-contrast W follows the dorsal surface: no floating icon/card.
  pts=[(-.12,.10,.264),(-.065,-.015,.298),(0,.10,.307),(.065,-.015,.298),(.12,.10,.264)]
  for a,b in zip(pts,pts[1:]):bind(beam('Diagnostic_W_dorsal_fold',a,b,.014,'wood_dark',6))
  for j in range(18):
   a=j*2.399;r=.115*math.sqrt((j+.5)/18);x=math.cos(a)*r;y=.03+math.sin(a)*r*1.3;z=.18+.134*math.sqrt(max(0,1-(x/.17)**2-((y-.03)/.22)**2))
   bind(ellipsoid('Dorsal_granule',(x,y,z),(.012,.014,.009),'wood_light' if j%3==0 else 'wood_dark',1))
 else:
  points=[(math.sin(i*.48)*.09,.21+i*.038,.15-i*.012) for i in range(8)];verts=[];faces=[];groups={}
  for i,pt in enumerate(points):
   name='Tail.'+str(i);bones.append((name,pt,tuple(Vector(pt)+Vector((.001,.065,0))),'Body' if i==0 else 'Tail.'+str(i-1)))
   rr=.065*(1-i/8)**1.1;groups[name]=[]
   for j in range(8):
    a=j*math.tau/8;groups[name].append(len(verts));verts.append((pt[0]+rr*math.cos(a),pt[1],pt[2]+rr*.58*math.sin(a)))
  faces=[tuple(reversed(range(8))),tuple(range(56,64))]
  for i in range(7):
   for j in range(8):faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
  tail=newmesh('Continuous_tapered_tail',verts,faces,body,True);weights[tail.name]=groups
  for j in range(22):
   a=j*2.399;r=.11*math.sqrt((j+.5)/22);x=math.cos(a)*r;y=.04+math.sin(a)*r*1.55;z=.18+.077*math.sqrt(max(0,1-(x/.14)**2-((y-.03)/.22)**2))
   bind(ellipsoid('Lichen_mottle' if not gecko else 'Lace_scale',(x,y,z),(.016 if not gecko else .009,.023 if not gecko else .012,.004),'moss_dark' if j%2 else 'moss_light' if not gecko else 'cream',1))
 rig_parts(mid,bones,weights,'frog' if frog else 'lizard');MODELS[mid]['blueprint']=bp
 # Rig rest state and exporter require an updated dependency graph.
 bpy.context.view_layer.update();export_collection(mid)

for bp in ('SummitSinger','BrocchiniaSentinel','PrickleBrowGecko'):endemic(bp)
rows=[]
for mid,d in MODELS.items():
 verts=[ob.matrix_basis@v.co for ob in d['collection'].objects if ob.type=='MESH' for v in ob.data.vertices];lo=[min(v[i] for v in verts) for i in range(3)];hi=[max(v[i] for v in verts) for i in range(3)]
 tri=0
 for ob in d['collection'].objects:
  if ob.type=='MESH':ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles)
 rows.append({'id':mid,'blueprint':d['blueprint'],'path':'models/'+mid+'.fbx','rigFamily':d['rigFamily'],'clips':d['clips'],'sockets':[], 'triangles':tri,'boundsBlender':{'min':lo,'max':hi},'status':'candidate-not-integrated'})
(OUT/'catalog.json').write_text(json.dumps({'schemaVersion':1,'models':rows},indent=2)+'\n')
# Collection instances keep editable assets separate from presentation staging.
for i,mid in enumerate(MODELS):instance(mid,loc=(i*1.5,0,0))
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.39,.43,1);SCENE.world.node_tree.nodes['Background'].inputs[1].default_value=.7
floor=bpy.data.materials.new('ReviewGround');floor.diffuse_color=(.15,.17,.15,1);bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('Key','AREA');li.energy=450;li.shape='DISK';li.size=5;ob=bpy.data.objects.new('Key',li);SCENE.collection.objects.link(ob);ob.location=(-1,-3,5);ob.rotation_euler=(Vector((1,0,0))-ob.location).to_track_quat('-Z','Y').to_euler()
ca=bpy.data.cameras.new('Review56');cam=bpy.data.objects.new('Review56',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO';ca.ortho_scale=4.7;cam.location=(1.5,-7*math.cos(math.radians(56)),7*math.sin(math.radians(56)));cam.rotation_euler=(Vector((1.5,0,0))-cam.location).to_track_quat('-Z','Y').to_euler()
SCENE.render.engine='CYCLES';SCENE.cycles.samples=40;SCENE.cycles.use_denoising=True;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_x=1500;SCENE.render.resolution_y=650;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='AgX';SCENE.render.filepath=str(OUT/'renders/endemic-review.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'endemics.blend'))
if args.render:bpy.ops.render.render(write_still=True)
print('ENDEMIC CANDIDATES',json.dumps(rows))
