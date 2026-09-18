"""Create72 cell-aligned wood/stone building pieces and an editable assembly."""
import bpy,bmesh,math,random,json,sys,argparse,shutil
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent));from contract import validate
ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);ap.add_argument('--render',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for p in ('models','textures','renders'): (OUT/p).mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'mesh_kit.py','exec'),globals());CINDEX={n:i for i,(n,c) in enumerate(COLORS)}
shutil.copy2(ROOT/'ArtSource/SpawnRing3D/textures/SpawnRingPalette.png',OUT/'textures/BuildingPalette.png');img=bpy.data.images.load(str(OUT/'textures/BuildingPalette.png'))
MAT=bpy.data.materials.new('BuildingPalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;bsdf=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.88;WATER=MAT
FAMILIES=['timber-post','timber-beam','plank-wall','window-wall','timber-doorframe','plank-floor','timber-brace','stone-block','masonry-wall','stone-corner','stone-doorway','stone-floor','foundation','wooden-stair','stone-stair','roof-slope','roof-ridge','roof-end']

def nail(x,y,z):ellipsoid('Forged_peg',(x,y,z),(.019,.016,.019),'iron',1)
def grain(x,y,z,length,vertical=False,variant=0):
 for j in range(2):
  r=(j-.5)*.035
  if vertical:beam('Hand_cut_grain',(x+r,y,z-length*.38),(x+r+.012*math.sin(variant+j),y,z+length*.38),.006,'wood_dark',5)
  else:beam('Hand_cut_grain',(x-length*.38,y,z+r),(x+length*.38,y,z+r+.009*math.sin(variant+j)),.006,'wood_dark',5)

def masonry(rng,variant,corner=False,door=False,block=False,foundation=False):
 height=.24 if foundation else .48 if block else 1.2;courses=2 if foundation or block else 5;course=height/courses
 for level in range(courses):
  if corner:
   for x,y,sx,sy in [(-.34,0,.30,.98),(.14,.34,.66,.30)]:softstone('Corner_quoin',(x,y,(level+.5)*course),(sx,sy,course*.97),['stone','stone_warm','stone_light'][(level+variant)%3],rng)
  elif door and level<courses-1:
   for side in [-1,1]:softstone('Door_jamb',(side*.385,0,(level+.5)*course),(.23,.42,course*.97),'stone_warm' if (level+variant)%2 else 'stone',rng)
  else:
   n=2 if (level+variant)%2==0 else 3;unit=1/n
   for j in range(n):
    x=-.5+(j+.5)*unit
    ob=softstone('Cut_stone',(x,0,(level+.5)*course),(unit*.97,.96 if block or foundation else .42,course*.97),['stone','stone_warm','stone_light','stone_dark'][(j+level+variant)%4],rng)
    if not foundation:
     beam('Stone_chisel_mark',(x-unit*.12,-(.489 if block else .217),(level+.45)*course),(x+unit*.08,-(.489 if block else .217),(level+.56)*course),.006,'stone_dark',5)

for family in FAMILIES:
 for variant in range(4):
  mid=family+'-'+str(variant);model(mid,'block');rng=random.Random(mid);wood=['wood','wood_light','wood','wood_end'][variant];height=1.2
  if family=='timber-post':
   box('Square_post',(0,0,.6),(.22,.22,1.2),wood,.025)
   for z in [.11,1.08]:box('Forged_collar',(0,0,z),(.245,.245,.065),'iron',.01)
   grain(0,-.117,.6,1.05,True,variant)
  elif family=='timber-beam':
   box('Mortised_beam',(0,0,.12),(1,.23,.24),wood,.023)
   for x in [-.37,.37]:nail(x,-.124,.12)
   grain(0,-.125,.12,.9,False,variant)
  elif family in ('plank-wall','window-wall','timber-doorframe'):
   for x in [-.43,.43]:box('Upright',(x,0,.6),(.14,.22,1.2),'wood_dark',.02);grain(x,-.116,.6,1,True,variant)
   for z in [.08,1.12]:box('Crossrail',(0,0,z),(1,.235,.16),wood,.018)
   if family!='timber-doorframe':
    for j in range(5):
     x=-.30+j*.15
     if family=='window-wall' and j in (1,2,3):
      for z,h in [(.26,.30),(.99,.17)]:box('Short_infill',(x,.016,z),(.144,.115,h),wood if j%2 else 'wood_light',.01)
     else:box('Split_plank',(x,.016,.6),(.144,.115,.93),wood if j%2 else 'wood_light',.014);grain(x,-.049,.6,.8,True,variant+j)
    if family=='window-wall':
     box('Window_sill',(0,-.02,.44),(.58,.29,.085),'wood_end',.012)
     for x in [-.22,.22]:box('Window_trim',(x,-.056,.66),(.055,.1,.47),'wood_dark',.007)
    else:
     beam('Diagonal_brace',(-.33,-.094,.20),(.33,-.094,1.0),.037,'wood_dark',4)
   for x in [-.43,.43]:
    for z in [.08,1.12]:nail(x,-.128,z)
  elif family=='plank-floor':
   for j in range(5):
    x=-.4+j*.2;box('Floorboard',(x,0,.045),(.194,1,.09),['wood','wood_light','wood_end','wood_dark'][(j+variant)%4],.012)
    for y in [-.39,.39]:nail(x,y,.084)
  elif family=='timber-brace':
   box('Foot',(0,0,.055),(1,.16,.11),'wood_dark',.015);beam('Diagonal_timber',(-.43,0,.13),(.43,0,1.12),.065,wood,4)
   nail(-.36+variant*.015,-.04,.15+variant*.02)
  elif family in ('stone-block','masonry-wall','stone-corner','stone-doorway','foundation'):
   masonry(rng,variant,corner=family=='stone-corner',door=family=='stone-doorway',block=family=='stone-block',foundation=family=='foundation')
  elif family=='stone-floor':
   for x in [-.25,.25]:
    for y in [-.25,.25]:softstone('Flagstone',(x,y,.06),(.475,.475,.12),['stone','stone_warm','stone_light'][(variant+(x>0)+(y>0))%3],rng)
  elif family in ('wooden-stair','stone-stair'):
   for i in range(6):
    h=(i+1)*.20;y=-.5+(i+.5)/6
    box('Stair_tread',(0,y,h/2),(.96,1/6-.002,h),wood if family=='wooden-stair' else ['stone','stone_warm','stone_light','stone_dark'][(i+variant)%4],.015)
    if family=='wooden-stair':
     box('Tread_nose',(0,y-1/12+.018,h-.018),(.98,.038,.036),'wood_light',.005)
     for x in [-.35+variant*.025,.34-variant*.02]:ellipsoid('Tread_peg',(x,y,h+.008),(.015,.018,.008),'iron',1)
    else:
     x=-.22+variant*.12;beam('Worn_tread_chisel',(x-.10,y-.03,h+.005),(x+.08,y+.015,h+.005),.005,'stone_dark',5)
  else:
   # Modular ridge runs along Y. Roofs are lifted to wall height by placement.
   def roof_half(sign):
    vs=[(0,-.5,.53),(sign*.5,-.5,.08),(sign*.5,.5,.08),(0,.5,.53),(0,-.5,.48),(sign*.5,-.5,.03),(sign*.5,.5,.03),(0,.5,.48)]
    newmesh('Roof_deck',vs,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'wood_dark')
    for i in range(4):
     x=sign*(i+.5)*.125;z=.53-abs(x)*.9
     for j in range(4):
      y=-.375+j*.25;ob=box('Overlapping_shingle',(x,y,z+.02),(.147,.24,.025),rng.choice(['roof_moss','roof_moss','roof_moss_light','roof_moss_dark']),.006);ob.rotation_euler[1]=sign*math.atan(.9)
   if family=='roof-slope':
    roof_half(1)
    # Extend this half into one full cell while retaining the42-degree pitch.
    for ob in CURRENT.objects:
     if ob.type=='MESH':
      mat=ob.matrix_basis;inv=mat.inverted()
      for vertex in ob.data.vertices:
       p=mat@vertex.co;p.x=2*p.x-.5;p.z=2*p.z-.03;vertex.co=inv@p
   else:
    roof_half(-1);roof_half(1)
    beam('Ridge_cap',(0,-.49,.555),(0,.49,.555),.032,'wood_end',8)
    if family=='roof-end':
     newmesh('Gable_infill',[(-.47,-.455,.055),(.47,-.455,.055),(0,-.455,.48),(-.47,-.40,.055),(.47,-.40,.055),(0,-.40,.48)],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],wood)
  # Clamp stochastic stone extremities to exact cell seams without welding.
  for ob in CURRENT.objects:
   if ob.type!='MESH':continue
   mat=ob.matrix_basis;inv=mat.inverted()
   for v in ob.data.vertices:
    p=mat@v.co;p.x=max(-.5,min(.5,p.x));p.y=max(-.5,min(.5,p.y));p.z=max(0,p.z);v.co=inv@p
   ob.data.update()
  bpy.context.view_layer.update();export_collection(mid)

rows=[]
for mid,d in MODELS.items():
 verts=[];tri=zero=0
 for ob in d['collection'].objects:
  if ob.type!='MESH':continue
  verts.extend(ob.matrix_basis@v.co for v in ob.data.vertices);ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles)
  for t in ob.data.loop_triangles:
   a,b,c=[ob.data.vertices[i].co for i in t.vertices];zero+=int((b-a).cross(c-a).length<1e-10)
 lo=[min(v[i] for v in verts) for i in range(3)];hi=[max(v[i] for v in verts) for i in range(3)];family,variant=mid.rsplit('-',1)
 rows.append({'id':mid,'family':family,'variant':int(variant),'path':'models/'+mid+'.fbx','pivot':'bottom-centre','bounds':{'min':lo,'max':hi},'triangles':tri,'zeroAreaTriangles':zero,'material':'stone' if family.startswith(('stone','masonry','foundation')) else 'wood','cellSize':1})
errors=validate(rows);(OUT/'catalog.json').write_text(json.dumps({'schemaVersion':1,'models':rows,'snap':{'grid':1,'wallHeight':1.2,'quarterTurns':True},'validationErrors':errors},indent=2)+'\n');assert not errors,errors
# All assets on a separate readable grid; then one cell-by-cell cutaway house.
for i,mid in enumerate(MODELS):instance(mid,loc=((i%12)*1.55,(i//12)*1.8,0))
assembly=[]
def place(fam,x,y,z=0,rotation=0):
 variant=(x*7+y*11)%4;mid=fam+'-'+str(variant);oid='block-'+str(len(assembly));instance(mid,loc=(x+22,y+1,z),angle=rotation,owner=oid);assembly.append({'owner':oid,'model':mid,'cell':[x,y],'height':z,'quarterTurns':round(rotation/(math.pi/2))})
for x in range(5):
 for y in range(5):
  place('foundation',x,y);place('plank-floor',x,y,.24)
  if y==4:place('window-wall' if x in (1,3) else 'plank-wall',x,y,.33)
  elif x in (0,4):place('plank-wall',x,y,.33,math.pi/2)
  elif y==0 and x in (1,3):place('timber-post',x,y,.33)
  # Rear half roof only: cutaway preview does not imply automatic roof logic.
  if y>=3:
   if x==2:place('roof-end' if y==4 else 'roof-ridge',x,y,1.53+1.8)
   else:place('roof-slope',x,y,1.53+(.9 if x in (1,3) else 0),math.pi if x<2 else 0)
(OUT/'assembly.json').write_text(json.dumps({'status':'art-only-cutaway','blocks':assembly},indent=2)+'\n')
world=SCENE.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.38,.42,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
floor=bpy.data.materials.new('Review_ground');floor.diffuse_color=(.16,.18,.15,1);bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.05));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('Warm_key','AREA');li.energy=2200;li.size=10;ob=bpy.data.objects.new('Warm_key',li);SCENE.collection.objects.link(ob);ob.location=(7,-6,12);ob.rotation_euler=(Vector((10,3,0))-ob.location).to_track_quat('-Z','Y').to_euler()
fill=bpy.data.lights.new('House_key','AREA');fill.energy=1500;fill.size=7;fo=bpy.data.objects.new('House_key',fill);SCENE.collection.objects.link(fo);fo.location=(23,-4,9);fo.rotation_euler=(Vector((24,3,0))-fo.location).to_track_quat('-Z','Y').to_euler()
ca=bpy.data.cameras.new('Review56');cam=bpy.data.objects.new('Review56',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO';SCENE.render.engine='CYCLES';SCENE.cycles.samples=32;SCENE.cycles.use_denoising=True;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_x=1800;SCENE.render.resolution_y=1050;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='AgX'
def aim(center,scale):
 ca.ortho_scale=scale;cam.location=Vector(center)+Vector((0,-16*math.cos(math.radians(56)),16*math.sin(math.radians(56))));cam.rotation_euler=(Vector(center)-cam.location).to_track_quat('-Z','Y').to_euler()
aim((8.5,4.5,.3),20);SCENE.render.filepath=str(OUT/'renders/block-kit.png');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'building-blocks.blend'))
if args.render:
 bpy.ops.render.render(write_still=True);aim((24,3,1.1),11.2);SCENE.render.filepath=str(OUT/'renders/block-house.png');bpy.ops.render.render(write_still=True)
print('BUILDING KIT',len(rows),'models',sum(r['triangles'] for r in rows),'triangles')
