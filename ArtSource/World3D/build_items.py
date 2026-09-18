"""Tangible item-form studies, separate from runtime integration and equipment rigs."""
import bpy,bmesh,math,random,json,sys,argparse,shutil
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent));from item_designs import designs
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for d in ('models','textures','renders'):(OUT/d).mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'mesh_kit.py','exec'),globals());CINDEX={n:i for i,(n,c) in enumerate(COLORS)}
shutil.copy2(ROOT/'ArtSource/SpawnRing3D/textures/SpawnRingPalette.png',OUT/'textures/ItemPalette.png');img=bpy.data.images.load(str(OUT/'textures/ItemPalette.png'));MAT=bpy.data.materials.new('ItemPalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;shader=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=.75;WATER=MAT
entries=[r for r in designs(json.loads((Path(__file__).parent/'coverage.json').read_text())['blueprints']) if r['form']]
COL={'fire':'ember','ice':'water_light','acid':'leaf_light','storm':'gold','water':'teal','blood':'red','mind':'violet','life':'leaf','plain':'book_blue'}

def blade(length,width,color='steel',serrated=False):
 # Convex diamond-section blade with a true tip, visible edge and central ridge.
 vs=[(-width,0,.04),(0,0,.085),(width,0,.04),(0,0,.025),(-width,.65*length,.04),(0,.65*length,.085),(width,.65*length,.04),(0,.65*length,.025),(0,length,.045)]
 fs=[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,8),(5,6,8),(6,7,8),(7,4,8),(3,2,1,0)];ob=newmesh('Forged_blade',vs,fs,color)
 if serrated:
  for i in range(5):ellipsoid('Edge_notch',(-width-.01,(i+.5)*length*.12,.045),(.022,.03,.025),'iron',1)
 return ob

def book_motif(elem):
 c=COL[elem]
 if elem in ('water','ice'):
  for j in range(3):beam('Water_sigil',(-.13,-.09+j*.075,.162),(.13,-.06+j*.075,.162),.012,c,6)
 elif elem=='fire':
  leafshape('Flame_sigil',(0,0,.164),.27,.12,math.pi/2,c,.014);leafshape('Small_flame',(.075,-.03,.167),.15,.08,math.pi/3,'gold',.008)
 elif elem=='storm':
  pts=[(-.06,.14,.163),(.06,.025,.163),(-.025,.025,.163),(.08,-.14,.163)]
  for a,b in zip(pts,pts[1:]):beam('Lightning_sigil',a,b,.014,c,5)
 elif elem=='life':
  beam('Stem',(0,-.13,.162),(0,.13,.162),.013,'gold',5)
  for j in [-1,1]:leafshape('Leaf_sigil',(j*.07,.035,.166),.16,.07,j*.6,c,.01)
 else:
  torus('Embossed_seal',(0,0,.162),.095,.013,c,12,4);box('Title_plate',(0,.18,.16),(.21,.07,.012),'paper',.002)

for row in entries:
 n=row['blueprint'];f=row['form'];e=row['element'];c=COL[e];rng=random.Random(n);mid='item-'+n;model(mid,'item')
 if f in ('knife','sword','polearm','axe','hammer','club','shard','blade-component'):
  if f in ('knife','sword','blade-component','shard'):
   length=.37 if f=='knife' else .64 if f=='sword' else .45;width=.04 if 'Stiletto' in n else .055 if f=='knife' else .095 if n in ('Greatsword','Claymore') else .07
   blade(length,width,'water_light' if e=='ice' else 'steel',n in ('Sporeblade','SerratedEdgeComponent'))
   if f not in ('blade-component','shard'):
    box('Crossguard',(0,-.03,.047),(.27 if f=='sword' else .17,.04,.055),'iron',.01)
    beam('Wrapped_grip',(0,-.06,.05),(0,-.26,.05),.03,'leather',8);ellipsoid('Pommel',(0,-.27,.05),(.05,.045,.046),'gold' if e!='plain' else 'iron',1)
    for j in range(4):box('Grip_binding',(0,-.08-j*.044,.07),(.064,.013,.012),'wood_dark',.002)
   if e!='plain':
    for j in range(3):ellipsoid('Inlaid_element',(0,.10+j*.13,.088),(.014,.029,.012),c,1)
  elif f=='polearm':
   beam('Long_shaft',(0,-.43,.055),(0,.29,.055),.026,'wood',8);ob=blade(.20,.055,'water_light' if e=='ice' else 'steel');ob.location.y=.26
   for j in range(3):box('Head_lashing',(0,.24-j*.025,.067),(.062,.012,.024),'rope',.003)
   if e!='plain':leafshape('Element_inlay',(0,.34,.091),.10,.026,math.pi/2,c,.007)
  elif f=='axe':
   beam('Axe_haft',(0,-.38,.055),(0,.30,.055),.037,'wood',8)
   vs=[(-.025,.13,.04),(-.25,.07,.04),(-.27,.36,.04),(-.025,.30,.04),(-.025,.13,.10),(-.25,.07,.07),(-.27,.36,.07),(-.025,.30,.10)]
   newmesh('Broad_cleaver_head',vs,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'steel')
   if n=='Battleaxe':
    box('Rear_pick',(.1,.22,.06),(.19,.13,.055),'iron',.025)
  else:
   beam('Weighted_haft',(0,-.36,.055),(0,.25,.055),.033,'wood_dark',8)
   if f=='hammer':
    box('Hammer_head',(0,.23,.09),(.39,.20,.17),'stone' if n=='DissolutionMaul' else 'iron',.035)
    for x in [-.15,.15]:box('Striking_face',(x,.23,.095),(.06,.215,.185),c if e!='plain' else 'steel',.015)
   elif n=='OldWorldPipe':
    torus('Pipe_flange',(0,.20,.065),.085,.025,'iron',12,4)
   else:
    ellipsoid('Club_crown',(0,.22,.08),(.10,.18,.08),'cream' if n=='ChoirSpine' else 'wood',2)
    for j in range(5):ellipsoid('Raised_flange',(math.sin(j*1.2)*.10,.13+j*.035,.09),(.035,.045,.045),'iron' if n=='Mace' else 'wood_light',1)
 elif f in ('tonic','vial','grenade'):
  r=.13 if f=='vial' else .18;body=c if f=='tonic' else 'iron' if f=='grenade' else 'water'
  ellipsoid('Bottle_belly',(0,0,.19),(r,r,.18),body,3);cylinder('Bottle_neck',(0,0,.375),r*.42,.14,body,12);torus('Bottle_lip',(0,0,.43),r*.44,.018,'cream',12,4);cylinder('Cork',(0,0,.454),r*.35,.07,'wood_end',10)
  box('Parchment_label',(0,-r*.92,.21),(.16,.026,.14),'paper',.009)
  if f=='grenade':
   torus('Safety_ring',(0,0,.50),.057,.012,'iron_light',12,4);box('Primer',(0,-.18,.23),(.095,.035,.06),c,.006)
  else:
   for j in range(1+sum(n.encode())%3):box('Dose_mark',((j-1)*.035,-r-.008,.215),(.012,.009,.06),c,.001)
 elif f=='book':
  box('Paper_block',(0,0,.078),(.36,.50,.12),'paper',.012)
  for z in [.017,.147]:box('Bound_cover',(0,0,z),(.405,.55,.035),c,.009)
  box('Rounded_spine',(-.20,0,.085),(.06,.54,.145),'leather',.025)
  for y in [-.18,.18]:box('Spine_band',(-.21,y,.095),(.065,.038,.15),'gold',.007)
  for x in [-.16,.16]:
   for y in [-.22,.22]:box('Corner_cap',(x,y,.169),(.065,.065,.015),'iron_light',.006)
  book_motif(e)
 elif f=='shield':
  cylinder('Buckler_face',(0,0,.065),.27,.10,'wood' if n=='Buckler' else 'iron',16);torus('Bound_rim',(0,0,.09),.264,.025,'steel',20,5);ellipsoid('Central_boss',(0,0,.14),(.10,.10,.09),'iron_light',2)
 elif f=='helmet':
  ellipsoid('Cap_dome',(0,0,.18),(.23,.23,.17),'leather' if n=='LeatherCap' else 'iron',3);torus('Helmet_rim',(0,0,.07),.23,.018,'wood_dark' if n=='LeatherCap' else 'steel',20,5)
  if n=='IronHelmet':box('Nasal_guard',(0,-.233,.12),(.044,.025,.19),'steel',.007)
 elif f in ('boots','gloves'):
  for x in [-.13,.13]:
   ellipsoid('Boot_toe' if f=='boots' else 'Mitten_fingers',(x,-.10,.07),(.10,.16,.065),'leather',2);cylinder('Cuff',(x,.035,.16),.085,.24,'leather',10)
   for j in range(3):box('Lacing',(x,-.05,.14+j*.037),(.105,.012,.012),'rope',.003)
   if n=='IronshodBoots':box('Iron_toe_cap',(x,-.19,.085),(.16,.09,.04),'iron',.013)
 elif f in ('coat','cloak'):
  box('Folded_garment',(0,0,.075),(.40,.52,.14),'iron' if n in ('ChainMail','PlateArmor') else 'leather' if f=='coat' else 'book_green',.04)
  for x in [-.22,.22]:box('Folded_sleeve',(x,.08,.10),(.13,.28,.1),'iron' if n=='PlateArmor' else 'leather',.03)
  if n=='ChainMail':
   for i in range(5):
    for j in range(6):torus('Mail_link',((i-2)*.067,(j-2.5)*.07,.15),.026,.006,'steel',8,4)
  elif n=='PlateArmor':
   for j in range(3):box('Overlapping_plate',(0,-.13+j*.13,.16),(.35,.115,.032),'steel',.016)
  else:
   beam('Seam',(-.13,-.2,.15),(-.13,.2,.15),.008,'rope',5);torus('Clasp',(.1,.13,.15),.032,.009,'gold',10,4)
 elif f in ('crystal','shard'):
  for j in range(5):
   a=j*2.399;r=.12 if j else 0;ob=cylinder('Mineral_prism',(math.cos(a)*r,math.sin(a)*r,.10+j*.012),.06,.20+j*.024,'cream' if n=='Tepuibone' else 'stone_light' if e=='plain' else c,6,radius2=.015);ob.rotation_euler[1]=math.sin(a)*.25
 elif f in ('fruit','seed','sac'):
  count=5 if n=='WildBerries' or f=='seed' else 1
  for j in range(count):
   a=j*2.399;size=.065 if count>1 else .20;ellipsoid('Fruit_body' if f=='fruit' else 'Organic_body',(math.cos(a)*.11 if count>1 else 0,math.sin(a)*.11 if count>1 else 0,size*.8),(size,size*.85,size*.8),'terracotta' if n=='RoastedStarapple' else 'red' if f=='fruit' else c,2)
  if f=='fruit':beam('Stem',(0,0,.29),(.02,0,.39),.017,'wood',6);leafshape('Fruit_leaf',(.065,0,.33),.14,.06,.2,'leaf',.01)
 elif f in ('herb','root','sheaf'):
  for j in range(5):
   x=(j-2)*.065;beam('Root_or_stem',(x,-.22,.03),(x*.3,.19,.08),.025 if f=='root' else .01,'terracotta' if n=='CandyCarrot' else 'wood_end' if f=='root' else 'leaf',7)
   for sign in [-1,1]:leafshape('Herb_leaf',(x+sign*.05,.08+j*.015,.09),.17,.06,sign*.65,c if e!='plain' else 'leaf',.016)
  torus('Twine_binding',(0,-.10,.04),.09,.01,'rope',12,4)
 elif f=='meat':
  for j in range(3):ellipsoid('Meat_strip',((j-1)*.10,0,.06),(.065,.23,.055),'red' if n=='RawMeat' else 'wood_dark' if n=='DriedMeat' else 'terracotta',2)
 elif f=='mushroom':
  cylinder('Stem',(0,0,.13),.06,.25,'cream',10);ellipsoid('Cap',(0,0,.28),(.21,.19,.09),'terracotta',2)
 elif f=='honeycomb':
  for j in range(7):a=j*math.tau/6;r=.12 if j<6 else 0;torus('Wax_cell',(math.cos(a)*r,math.sin(a)*r,.04),.069,.018,'gold',6,4)
 elif f=='bone':
  beam('Bone_shaft',(0,-.23,.05),(0,.23,.05),.04,'cream',8)
  for y in [-.23,.23]:
   for x in [-.04,.04]:ellipsoid('Bone_knuckle',(x,y,.06),(.06,.055,.05),'cream',1)
 elif f=='coin':
  for j in range(4):cylinder('Coin',(j*.035-.05,j*.015,.012+j*.018),.13,.018,'gold',16)
 elif f=='torch':
  beam('Torch_haft',(0,-.33,.05),(0,.24,.05),.036,'wood',8);ellipsoid('Pitch_wrapping',(0,.22,.07),(.08,.13,.07),'wood_dark',2)
 elif f=='doll':
  ellipsoid('Woven_head',(0,.18,.07),(.085,.085,.07),'rope',2);beam('Doll_body',(0,-.05,.07),(0,.11,.07),.065,'rope',8)
  for s in [-1,1]:beam('Doll_arm',(0,.07,.07),(s*.19,.025,.055),.025,'rope',7);beam('Doll_leg',(s*.035,-.05,.06),(s*.09,-.23,.04),.03,'rope',7)
 elif f=='haft-component':beam('Raw_haft',(0,-.36,.06),(.025,.36,.06),.04,'wood_light',8)
 elif f=='binding':
  for j in range(3):torus('Leather_coil',(0,0,.02+j*.024),.11+j*.02,.017,'leather',16,5)
 else:raise ValueError((n,f))
 # Centre long flat weapons inside their actual one-cell ground silhouette.
 allv=[ob.matrix_basis@v.co for ob in CURRENT.objects if ob.type=='MESH' for v in ob.data.vertices];low=min(v.y for v in allv);high=max(v.y for v in allv);shift=-(low+high)/2
 for ob in CURRENT.objects:
  if ob.type=='MESH':ob.location.y+=shift
 bpy.context.view_layer.update();export_collection(mid);row['id']=mid;row['path']='models/'+mid+'.fbx'
 allv=[ob.matrix_basis@v.co for ob in CURRENT.objects if ob.type=='MESH' for v in ob.data.vertices];row['bounds']={'min':[min(v[i] for v in allv) for i in range(3)],'max':[max(v[i] for v in allv) for i in range(3)]}
 row['triangles']=sum(len(ob.data.loop_triangles) for ob in CURRENT.objects if ob.type=='MESH');row['status']='candidate-not-integrated'
(OUT/'catalog.json').write_text(json.dumps({'schemaVersion':1,'models':entries},indent=2)+'\n')
for i,row in enumerate(entries):instance(row['id'],loc=((i%15)*1.1,(i//15)*1.1,0))
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes['Background'].inputs[0].default_value=(.4,.43,.45,1);SCENE.world.node_tree.nodes['Background'].inputs[1].default_value=.7
li=bpy.data.lights.new('Key','AREA');li.energy=2100;li.size=10;ob=bpy.data.objects.new('Key',li);SCENE.collection.objects.link(ob);ob.location=(6,-4,12);ob.rotation_euler=(Vector((7,5,0))-ob.location).to_track_quat('-Z','Y').to_euler()
floor=bpy.data.materials.new('GalleryGround');floor.diffuse_color=(.17,.19,.17,1);bpy.ops.mesh.primitive_plane_add(size=100,location=(0,0,-.025));bpy.context.object.data.materials.append(floor)
ca=bpy.data.cameras.new('Item56');cam=bpy.data.objects.new('Item56',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO';ca.ortho_scale=18.0;cam.location=(7.7,5-18*math.cos(math.radians(56)),18*math.sin(math.radians(56)));cam.rotation_euler=(Vector((7.7,5,0))-cam.location).to_track_quat('-Z','Y').to_euler()
SCENE.render.engine='CYCLES';SCENE.cycles.samples=32;SCENE.cycles.use_denoising=True;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_x=2100;SCENE.render.resolution_y=1300;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='AgX';SCENE.render.filepath=str(OUT/'renders/items.png');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'items.blend'))
if args.render:bpy.ops.render.render(write_still=True)
print('ITEM STUDIES',len(entries))
