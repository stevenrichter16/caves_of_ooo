#!/usr/bin/env python3
"""Editable starter-magic motion studies. Blender 5.2; no Unity/project writes.

Blender --background --factory-startup --threads 4 --python build_studies.py
  -- --render-stills [--render-loops] [--skip-export]
All keyframes use 100 fps native seconds. Preview playback is deliberately 4x slow.
"""
import bpy, math, random, json, hashlib, sys, argparse
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from normalize_png import remove_density_chunk
SOURCE=ROOT.parent/'Village3D/village_master.blend'
argslist=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
ap=argparse.ArgumentParser();ap.add_argument('--render-stills',action='store_true');ap.add_argument('--render-loops',action='store_true');ap.add_argument('--skip-export',action='store_true');args=ap.parse_args(argslist)
for sub in ('renders','frames','exports'): (ROOT/sub).mkdir(exist_ok=True)
SOURCE_HASH=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
FPS=100; CLEAR=98; END=110; RNG=random.Random(9102607)
PITCH=math.radians(56)
SPECS=[
 dict(id='ember_spit',name='Ember Spit',school='PYROMANCY',tag='PINCH / FLICK',distance=3,travel=True,color='ember',note='One charcoal seed. One body. Four-cell maximum.'),
 dict(id='flaming_hands',name='Flaming Hands',school='PYROMANCY',tag='OPEN / PRESS',distance=1,travel=False,color='flame',note='A serrated palm burst held inside one adjacent cell.'),
 dict(id='jet_blast',name='Jet Blast',school='HYDROMANCY',tag='CUP / UNFOLD',distance=2,travel=True,color='river',note='A heavy folded stream. Short two-cell creature cone.'),
 dict(id='ground_surge',name='Ground Surge',school='GALVANISM',tag='LOWER / STITCH',distance=4,travel=True,color='ochre',note='Four low stitches follow the earth. No sky strike.'),
 dict(id='rime_grip',name='Rime Grip',school='CRYOMANCY',tag='SPREAD / CLOSE',distance=3,travel=False,color='ice',note='Three blunt clamps. The face and shoulders stay visible.'),
 dict(id='calm',name='Calm',school='SPELLCRAFT',tag='SOFTEN / RELEASE',distance=4,travel=True,color='violet',note='An open binding relaxes. Pacified does not mean allied.'),
 dict(id='conjure_rain',name='Conjure Rain',school='LEARNABLE UTILITY',tag='COUNT / TURN',distance=0,travel=False,color='river',note='Learn from the carried book or buy with SP. Empty hands; valid crops only.')]

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for scene in list(bpy.data.scenes)[1:]:bpy.data.scenes.remove(scene)
with bpy.data.libraries.load(str(SOURCE),link=False) as (available,loaded): loaded.collections=['character-teal','character-olive']
ACTOR_SOURCE=loaded.collections[0]
NEUTRAL_SOURCE=loaded.collections[1]
PALETTE={
 'ink':(.118,.125,.110),'earth':(.235,.217,.166),'stone':(.31,.32,.275),'stone_light':(.39,.38,.305),
 'moss':(.285,.345,.155),'moss_light':(.48,.49,.22),'wood':(.45,.324,.195),'wood_light':(.69,.563,.378),
 'ivory':(.98,.90,.68),'charcoal':(.185,.165,.13),'ember':(.91,.34,.075),'flame':(1,.60,.15),
 'river':(.085,.41,.51),'water_light':(.40,.79,.82),'ochre':(.99,.71,.20),
 'ice':(.60,.81,.83),'ice_shadow':(.235,.48,.60),'violet':(.60,.45,.68),'violet_light':(.85,.73,.83),
 'leaf':(.39,.51,.21),'soil':(.18,.14,.105),'book':(.235,.37,.395),
 'ember_core':(1,.73,.24),'foam':(.70,.94,.91),'ice_rim':(.83,.96,.97),'violet_core':(.74,.56,.84)}
MATS={}
def linear_swatch(color):return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in color)
for name,color in PALETTE.items():
 m=bpy.data.materials.new('SpellFolk_'+name);m.use_nodes=True;m.diffuse_color=(*linear_swatch(color),1)
 node=m.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=(*linear_swatch(color),1);node.inputs['Roughness'].default_value=.77
 MATS[name]=m

SC=None; ACTIVE=None; EFFECTS=[]
def link(ob,collection=None):
 (collection or ACTIVE).objects.link(ob);return ob
def empty(name,parent=None,location=(0,0,0)):
 ob=link(bpy.data.objects.new(name,None));ob.parent=parent;ob.location=location;return ob
def mesh(name,verts,faces,mat,parent=None):
 me=bpy.data.meshes.new(name+'_mesh');me.from_pydata(verts,[],faces);me.update();me.materials.append(MATS[mat]);ob=link(bpy.data.objects.new(name,me));ob.parent=parent;return ob
def box(name,location,scale,mat,parent=None,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1);ob=bpy.context.object;ob.name=name
 for coll in list(ob.users_collection):coll.objects.unlink(ob)
 ACTIVE.objects.link(ob);ob.parent=parent;ob.location=location;ob.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);ob.data.materials.append(MATS[mat])
 if bevel:
  mod=ob.modifiers.new('Carved corners','BEVEL');mod.width=bevel;mod.segments=1;ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
 return ob
def ico(name,location,scale,mat,parent=None,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1);ob=bpy.context.object;ob.name=name
 for coll in list(ob.users_collection):coll.objects.unlink(ob)
 ACTIVE.objects.link(ob);ob.parent=parent;ob.location=location;ob.scale=scale;ob.data.materials.append(MATS[mat]);return ob
def beam(name,a,b,radius,mat,parent=None,sides=6):
 a,b=Vector(a),Vector(b);axis=b-a;normal=axis.normalized();ref=Vector((0,0,1)) if abs(normal.z)<.9 else Vector((1,0,0));u=normal.cross(ref).normalized()*radius;v=normal.cross(u).normalized()*radius
 vs=[tuple(p+u*math.cos(i*math.tau/sides)+v*math.sin(i*math.tau/sides)) for p in (a,b) for i in range(sides)]
 fs=[tuple(range(sides-1,-1,-1)),tuple(range(sides,2*sides))]+[(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
 return mesh(name,vs,fs,mat,parent)
def path(name,points,radius,mat,parent=None):
 vs=[];fs=[];sides=5
 for i,p in enumerate(points):
  axis=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);normal=axis.normalized();ref=Vector((0,0,1)) if abs(normal.z)<.85 else Vector((1,0,0));u=normal.cross(ref).normalized()*radius;v=normal.cross(u).normalized()*radius
  vs.extend(tuple(Vector(p)+u*math.cos(a*math.tau/sides)+v*math.sin(a*math.tau/sides)) for a in range(sides))
  if i:
   for a in range(sides):fs.append(((i-1)*sides+a,(i-1)*sides+(a+1)%sides,i*sides+(a+1)%sides,i*sides+a))
 fs += [tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+a for a in range(sides))]
 return mesh(name,vs,fs,mat,parent)
def shard(name,location,length,width,mat,parent=None,angle=0):
 # A broad asymmetrical chisel, not a thin unreadable particle.
 ob=mesh(name,[(-length*.5,-width*.45,0),(-length*.25,width*.5,0),(length*.50,width*.12,0),(length*.18,-width*.3,0),(-length*.16,0,width*.32)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],mat,parent)
 ob.location=location;ob.rotation_euler[2]=angle;return ob
def key(ob,frame,location=None,scale=None,rotation=None):
 if location is not None:ob.location=location;ob.keyframe_insert('location',frame=frame)
 if scale is not None:ob.scale=(scale,)*3 if isinstance(scale,(float,int)) else scale;ob.keyframe_insert('scale',frame=frame)
 if rotation is not None:ob.rotation_euler=rotation;ob.keyframe_insert('rotation_euler',frame=frame)
def pulse(ob,start,contact,finish,scale=None):
 # Scale each fragment about its own carved form, never about the distant world origin.
 if ob.type=='MESH' and len(ob.data.vertices):
  centroid=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices)
  # matrix_local may still be stale immediately after assigning a new object's
  # transform. Compose current properties directly, without a dependency-graph race.
  old_position=Matrix.LocRotScale(ob.location.copy(),ob.rotation_euler.copy(),ob.scale.copy()) @ centroid
  ob.data.transform(Matrix.Translation(-centroid));ob.location=old_position
 base=tuple(ob.scale) if scale is None else scale
 reveal=min(8.0,max(6.0,(finish-start)*.28));peak=min(start+reveal,finish-6);hold=max(peak,min(contact,finish-6))
 key(ob,0,scale=0);key(ob,max(0,start-1),scale=0);key(ob,peak,scale=base);key(ob,hold,scale=base);key(ob,finish,scale=0);key(ob,END,scale=0)
 EFFECTS.append(ob);ob['transientEffect']=True;ob['noCollider']=True;ob['startFrame']=start;ob['peakFrame']=peak;ob['holdFrame']=hold;ob['clearFrame']=finish
def stage(spec):
 box('Stage__basalt_plinth',(.15,0,-.25),(6.3,3.15,.38),'ink',bevel=.16)
 for x in range(6):
  for y in range(3):
   ob=box('Stage__one_metre_cell_%d_%d'%(x,y),(x-2.35,y-1,-.04),(.975,.975,.12),'stone' if (x+y)%3 else 'stone_light',bevel=.035)
   for k in range(2):
    ico('Stage__moss_chip',(x-2.72+RNG.random()*.74,y-1.38+RNG.random()*.76,.035),(.055+.05*RNG.random(),.08,.018),'moss' if k else 'moss_light')
 for x,y in [(-2.68,-1.34),(2.84,.98),(-2.58,1.19),(2.54,-1.24)]:
  ico('Stage__edge_stone',(x,y,.06),(.16,.13,.12),'stone_light')
  for j in range(3):shard('Stage__leaf',(x+.08*j,y+.11,.08),.18,.09,'moss_light',angle=j*1.7)
def caster(spec):
 root=empty(spec['id']+'__ActorRoot',location=(-2.35,0,0));root.rotation_euler[2]=math.pi/2;root['nativeCellRoot']=True
 remap={}
 for src in ACTOR_SOURCE.objects:
  ob=src.copy()
  if src.data:ob.data=src.data.copy()
  ob.name=spec['id']+'__'+src.name;link(ob);remap[src]=ob
 for src,ob in remap.items():
  ob.parent=remap.get(src.parent,root);ob.matrix_parent_inverse=src.matrix_parent_inverse.copy() if src.parent else Matrix.Identity(4)
  for mod in ob.modifiers:
   if mod.type=='ARMATURE':mod.object=remap.get(mod.object,mod.object)
  if ob.animation_data:ob.animation_data_clear()
 rig=next(ob for ob in remap.values() if ob.type=='ARMATURE');rig.name=spec['id']+'__CasterRig'
 for b in rig.pose.bones:b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
 # Poses are local bone rotations. Root and both feet remain fixed.
 poses={
 'ember_spit':({'Arm.R':(-1.05,-.2,-.55),'Hand.R':(.2,0,.4),'Spine':(-.04,0,-.10),'Head':(.08,0,-.09)}, {'Arm.R':(-.90,0,.24),'Hand.R':(-.25,0,-.40),'Spine':(.08,0,.13),'Head':(-.07,0,.10)}),
 'flaming_hands':({'Arm.L':(-.72,0,-.22),'Arm.R':(-.72,0,.22),'Hand.L':(0,0,-.4),'Hand.R':(0,0,.4),'Spine':(-.12,0,0)}, {'Arm.L':(-1.05,0,.13),'Arm.R':(-1.05,0,-.13),'Spine':(.12,0,0)}),
 'jet_blast':({'Arm.L':(-.6,0,.25),'Arm.R':(-.65,0,-.4),'Hand.R':(.2,0,.4),'Spine':(-.11,0,-.12)}, {'Arm.R':(-1.23,0,.20),'Arm.L':(-.52,0,-.3),'Spine':(.10,0,.12)}),
 'ground_surge':({'Arm.R':(-.55,0,.12),'Spine':(.2,0,-.13),'Head':(.15,0,0)}, {'Arm.R':(-1.16,0,.21),'Spine':(.37,0,.10),'Head':(.17,0,.08)}),
 'rime_grip':({'Arm.R':(-.86,0,-.32),'Hand.R':(0,0,.56),'Arm.L':(-.35,0,.2)}, {'Arm.R':(-.95,0,-.20),'Hand.R':(.47,0,-.50),'Spine':(-.05,0,.09)}),
 'calm':({'Arm.R':(-.67,0,.18),'Arm.L':(-.2,0,-.1),'Hand.R':(.12,0,-.4),'Head':(-.045,0,.05)}, {'Arm.R':(-.83,0,.32),'Hand.R':(-.21,0,.1),'Spine':(-.065,0,0),'Head':(-.035,0,.08)}),
 'conjure_rain':({'Arm.L':(-.32,0,-.12),'Arm.R':(-.66,0,-.35),'Hand.R':(.15,0,.3),'Head':(.1,0,0)}, {'Arm.L':(-.35,0,-.20),'Arm.R':(-1.05,0,.2),'Hand.R':(.34,0,0),'Head':(-.04,0,0)})}
 gather,release=poses[spec['id']]
 settle={bone:tuple(v*.80 for v in angles) for bone,angles in release.items()};relax={bone:tuple(v*.34 for v in angles) for bone,angles in release.items()}
 for frame,pose in [(0,{}),(12,gather),(22,release),(32,settle),(48,relax),(66,{}),(END,{})]:
  for b in rig.pose.bones:
   b.rotation_euler=pose.get(b.name,(0,0,0));b.keyframe_insert('rotation_euler',frame=frame,group=b.name)
 rig.animation_data.action.name=spec['id']+'__CastNative100fps'
 return root,rig,list(remap.values())
def dummy(spec):
 root=empty('StudyOnly__neutral_target',location=(-2.35+spec['distance'],0,0));root['studyOnly']=True
 if spec['id']=='calm':
  # Pacification has a brain-bearing recipient; scenery cannot be calmed.
  root.name='Calm__sentient_studio_recipient';root.rotation_euler[2]=-math.pi/2;remap={}
  for src in NEUTRAL_SOURCE.objects:
   ob=src.copy()
   if src.data:ob.data=src.data.copy()
   ob.name='Calm__recipient_'+src.name;link(ob);remap[src]=ob
  for src,ob in remap.items():
   ob.parent=remap.get(src.parent,root);ob.matrix_parent_inverse=src.matrix_parent_inverse.copy() if src.parent else Matrix.Identity(4)
   if ob.animation_data:ob.animation_data_clear()
   for mod in ob.modifiers:
    if mod.type=='ARMATURE':mod.object=remap.get(mod.object,mod.object)
   if ob.type=='ARMATURE':
    for bone in ob.pose.bones:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0)
    for frame,value in [(0,.07),(30,.07),(46,-.035),(70,-.035),(END,.07)]:
     ob.pose.bones['Spine'].rotation_euler[0]=value;ob.pose.bones['Spine'].keyframe_insert('rotation_euler',frame=frame)
  return root
 beam('Target__wooden_post',(0,0,.05),(0,0,1.33),.19,'wood',root,sides=8)
 ico('Target__unaffiliated_head',(0,0,1.38),(.235,.23,.27),'wood_light',root,sub=1)
 beam('Target__shoulder',(-.38,0,.95),(.38,0,.95),.115,'wood',root)
 box('Target__neutral_binding',(0,-.205,1.02),(.30,.05,.12),'ivory',root,bevel=.025)
 box('Target__foot',(0,0,.025),(.6,.5,.05),'ink',root,bevel=.045)
 return root
def effect_study(spec):
 global EFFECTS
 EFFECTS=[]; sid=spec['id'];dist=spec['distance']; target=Vector((-2.35+dist,0,.0));cast=Vector((-1.99,0,1.02));contact=22+(dist*2.5 if spec['travel'] else 0);finish=contact+20;clear=finish+18
 root=empty(sid+'__EffectRoot');root['transientEffectRoot']=True;root['nativeCellPivot']='cast actor ground centre; contacts relative to resolved native cell'
 aftermath=empty(sid+'__AftermathRoot',root);aftermath['runtimeSpawnCondition']='recorded outcome only; study shows success branch'
 def eico(n,p,s,m,parent=root):return ico(sid+'__'+n,p,s,m,parent)
 if sid=='ember_spit':
  seed=empty('Ember__travel_seed',root);ob=eico('charcoal_seed',(0,0,0),(.25,.15,.16),'charcoal',seed);pulse(ob,13,contact,finish)
  ob=eico('warm_carved_core',(.09,-.075,.10),(.19,.105,.12),'ember_core',seed);pulse(ob,14,contact+4,finish)
  for side in [-1,1]:
   ob=path('Ember__curved_flame_wake_'+str(side),[(.05,side*.12,.07),(-.24,side*.20,.14),(-.55,side*.15,.19),(-.76,side*.06,.13)],.043,'ember' if side<0 else 'flame',seed);pulse(ob,15,contact+3,finish)
  for j,(a,b,m) in enumerate([(.16,.24,'flame'),(.02,.15,'ember'),(-.11,.13,'ivory')]):
   ob=shard('Ember__flame_chisel_%d'%j,(-.05-j*.12,a,.02+j*.06),.50+j*.09,b,m,seed,angle=math.pi+.15*j);pulse(ob,14,contact,finish)
  key(seed,12,location=cast);key(seed,22,location=cast);key(seed,contact,location=target+Vector((0,0,.8)));key(seed,finish,location=target+Vector((.08,0,.7)))
  for j in range(5):
   ob=eico('broken_soot_%d'%j,tuple(cast),(.08,.065,.08),'charcoal');pulse(ob,22+j,contact+5,finish+4)
   key(ob,22+j,location=cast+Vector((-.13*j,0,0)));key(ob,contact+4,location=target+Vector((-.4-j*.16,.05*math.sin(j),.85-j*.06)))
  for j in range(7):
   a=j*math.tau/7;ob=shard('Ember__impact_split_%d'%j,(0,0,0),.29,.12,'flame' if j%2 else 'ember',aftermath,a);pulse(ob,contact,contact+7,clear)
   key(ob,contact,location=target+Vector((0,0,.76)));key(ob,contact+12,location=target+Vector((.55*math.cos(a),.40*math.sin(a),.46+.12*(j%3))))
  for j in range(4):
   a=j*math.tau/4+.35;ob=eico('round_ember_finish_%d'%j,(0,0,0),(.075,.055,.065),'ember_core',aftermath);pulse(ob,contact+2,contact+13,clear)
   key(ob,contact+2,location=target+Vector((.12*math.cos(a),.12*math.sin(a),.61)));key(ob,contact+15,location=target+Vector((.41*math.cos(a),.38*math.sin(a),.28)))
 elif sid=='flaming_hands':
  # Entire release mesh is contained in selected adjacent cell: local X +/- .45, Y +/- .43.
  for side in [-1,1]:
   fan=empty('Flame__folded_palm_'+str(side),root,tuple(target))
   for j in range(4):
    y=side*(.05+j*.095);h=.52+.16*math.sin(j*1.3);ob=mesh('Flame__serrated_fan_%s_%s'%(side,j),[(-.40,y,.24),(.43,y*.55,.26),(.28,y*.74,h),(-.18,y,.44),(-.08,y+.045*side,.26)],[(0,1,2),(0,2,3),(0,4,1),(1,4,2),(2,4,3),(3,4,0)],'flame' if j%2 else 'ember',fan);pulse(ob,16,34,clear)
   ob=path('Flame__ivory_palm_edge_'+str(side),[(-.32,side*.03,.28),(-.05,side*.12,.50),(.23,side*.23,.55),(.37,side*.34,.34)],.028,'ivory',fan);pulse(ob,20,31,finish)
   ob=path('Flame__warm_inner_fold_'+str(side),[(-.31,side*.04,.32),(-.12,side*.14,.45),(.12,side*.20,.47),(.27,side*.24,.35)],.047,'ember_core',fan);pulse(ob,19,33,finish+8)
   ob=path('Flame__cushioned_contact_curl_'+str(side),[(.13,side*.30,.24),(.27,side*.35,.31),(.37,side*.26,.42),(.27,side*.17,.48)],.035,'flame',fan);pulse(ob,22,35,clear)
 elif sid=='jet_blast':
  # Elevated folded flow suggests a short cone; no blanket ground puddle geometry.
  for j in range(5):
   x=-1.8+j*.34;w=.12+j*.075;z=.87-j*.055
   ob=mesh('Jet__folded_river_strip_%d'%j,[(x,-w,z-.035),(x+.49,-w-.08,z-.15),(x+.49,w+.08,z+.035),(x,w,z+.13),(x+.12,0,z+.20)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,3,2,1)],'river' if j%2 else 'water_light',root);pulse(ob,22+j*1.2,contact+6,finish+4)
   if j in (1,3):
    ob=path('Jet__soft_fold_foam_%d'%j,[(x+.04,-w*.70,z+.11),(x+.20,0,z+.21),(x+.36,w*.72,z+.09)],.047,'foam',root);pulse(ob,24+j,contact+10,finish+7)
  lip=path('Jet__ivory_wave_lip',[(target.x-.34,-.56,.55),(target.x-.05,-.4,.76),(target.x+.06,0,.87),(target.x-.05,.4,.76),(target.x-.34,.56,.55)],.065,'ivory',root);pulse(lip,contact-1,contact+8,finish)
  for side in [-1,1]:
   ob=mesh('Jet__far_side_fold_'+str(side),[(target.x-.80,side*.18,.69),(target.x-.18,side*.73,.44),(target.x+.24,side*1.22,.25),(target.x-.23,side*1.27,.58),(target.x-.56,side*.57,.79)],[(0,1,4),(1,2,3,4),(4,3,2,1,0)],'river',root);pulse(ob,24,contact+8,finish+3)
   ob=path('Jet__far_side_lip_'+str(side),[(target.x-.19,side*.72,.53),(target.x+.06,side*.9,.61),(target.x+.13,side*1.16,.49),(target.x-.09,side*1.28,.37)],.045,'water_light',root);pulse(ob,26,contact+10,finish+4)
   for j in range(2):
    ob=eico('far_side_foam_%s_%s'%(side,j),(target.x+.02-j*.11,side*(.85+j*.22),.51-j*.06),(.075,.09,.055),'foam');pulse(ob,27+j,contact+12,finish+7)
  for j in range(3):
   ob=eico('heavy_droplet_%d'%j,(0,0,0),(.12,.115,.18),'water_light');pulse(ob,22+j,contact+8,clear)
   key(ob,22+j,location=cast+Vector((0,(j-1)*.1,0)));key(ob,contact+8,location=target+Vector((.15,(j-1)*.49,.48)));key(ob,clear,location=target+Vector((.38,(j-1)*.62,.08)))
  for j in range(5):
   a=.4+j*1.2;ob=shard('Jet__broken_splash_%d'%j,tuple(target+Vector((.34*math.cos(a),.42*math.sin(a),.1))),.3,.17,'river' if j%2 else 'water_light',aftermath,a);pulse(ob,contact,contact+12,clear)
  for side in [-1,1]:
   ob=path('Jet__settling_splash_crown_'+str(side),[(target.x-.21,side*.18,.20),(target.x+.03,side*.36,.29),(target.x+.25,side*.28,.19)],.040,'foam',aftermath);pulse(ob,contact+2,contact+13,clear)
 elif sid=='ground_surge':
  for cell in range(4):
   x=-1.35+cell
   for side in [-1,1]:
    pts=[(x-.43,side*.16,.11),(x-.17,side*.08,.17),(x-.01,side*.23,.12),(x+.16,side*.1,.20),(x+.43,side*.16,.13)]
    ob=path('Surge__cell_%d_angular_stitch_%s'%(cell+1,side),pts,.052,'ochre' if side<0 else 'ivory',root);pulse(ob,22+cell*2.5,contact+6,finish+8)
   ob=shard('Surge__stone_tongue_%d'%cell,(x,0,.08),.61,.28,'ochre',root);pulse(ob,22+cell*2.5,contact+5,finish)
   ob=path('Surge__cell_%d_moving_warm_highlight'%(cell+1),[(x-.15,-.035,.18),(x,0,.24),(x+.15,.035,.18)],.050,'ember_core',root);pulse(ob,24+cell*2.5,contact+10,finish+10)
   start=24+cell*2.5;base=ob.location.copy();key(ob,start,location=base+Vector((-.10,0,0)));key(ob,start+12,location=base+Vector((.10,0,0)))
  front=path('Surge__low_hook_front',[(target.x-.1,-.4,.12),(target.x+.16,-.17,.27),(target.x-.01,0,.38),(target.x+.16,.17,.27),(target.x-.1,.4,.12)],.065,'ivory',root);pulse(front,contact-1,contact+9,clear)
 elif sid=='rime_grip':
  ob=path('Rime__thin_cold_thread',[tuple(cast),(-1.1,.03,.77),(target.x-.42,0,.37)],.024,'ice_shadow',root);pulse(ob,15,contact+5,finish)
  for j,a in enumerate([-.3,1.9,4.0]):
   jaw=empty('Rime__clamp_pivot_%d'%j,aftermath,tuple(target));jaw.rotation_euler[2]=a
   ob=mesh('Rime__blunt_split_clamp_%d'%j,[(.39,-.15,.04),(.56,-.18,.12),(.54,-.17,.62),(.36,-.13,.68),(.31,-.1,.42),(.39,.13,.04),(.56,.17,.12),(.54,.15,.62),(.36,.11,.68),(.31,.09,.42)],[(0,1,2,3,4),(9,8,7,6,5),(0,5,6,1),(1,6,7,2),(2,7,8,3),(3,8,9,4),(4,9,5,0)],'ice',jaw);pulse(ob,18,contact+18,clear)
   seam=path('Rime__dark_carved_seam_%d'%j,[(.55,-.01,.17),(.42,-.01,.31),(.47,-.01,.43),(.39,-.01,.60)],.025,'ice_shadow',jaw);pulse(seam,18,contact+18,clear)
   rim=path('Rime__pale_blunt_rim_%d'%j,[(.55,-.15,.15),(.54,-.14,.53),(.45,-.11,.65),(.36,-.09,.59)],.032,'ice_rim',jaw);pulse(rim,20,contact+18,clear)
   key(jaw,18,location=target+Vector((.24*math.cos(a),.24*math.sin(a),0)));key(jaw,30,location=target);key(jaw,clear,location=target+Vector((.1*math.cos(a),.1*math.sin(a),0)))
   chip=shard('Rime__drifting_ice_chip_%d'%j,tuple(target),.16,.105,'ice_rim',aftermath,a);pulse(chip,contact+1,contact+14,clear)
   key(chip,contact+1,location=target+Vector((.31*math.cos(a),.31*math.sin(a),.53)));key(chip,clear,location=target+Vector((.55*math.cos(a+.2),.55*math.sin(a+.2),.19)))
 elif sid=='calm':
  loop=empty('Calm__travel_open_loop',root)
  pts=[(math.sin(a)*.38,0,math.cos(a)*.40) for a in [(.35+i*5.15/17) for i in range(18)]]
  ob=path('Calm__ivory_open_binding',pts,.047,'ivory',loop);pulse(ob,15,contact+8,finish)
  ob=path('Calm__violet_return',[(p[0]*1.17,.035,p[2]*1.13) for p in pts[3:14]],.043,'violet_core',loop);pulse(ob,16,contact+9,finish+4)
  ob=path('Calm__soft_inner_crescent',[(p[0]*.84,-.025,p[2]*.84) for p in pts[5:15]],.028,'violet_light',loop);pulse(ob,18,contact+10,finish+6)
  key(loop,15,location=cast);key(loop,22,location=cast);key(loop,contact,location=target+Vector((-.08,0,.96)));key(loop,finish,location=target+Vector((.08,0,1.03)))
  for j in range(2):
   ob=path('Calm__relaxing_angular_crossing_%d'%j,[(-.28,-.32,.54),(.04,0,.72),(-.25,.32,.55)],.047,'violet_light' if j else 'ivory',aftermath);ob.location=target;ob.rotation_euler[2]=j*math.pi; pulse(ob,contact,contact+10,clear)
   key(ob,contact,rotation=(0,0,j*math.pi+.6));key(ob,contact+15,rotation=(0,0,j*math.pi));key(ob,clear,rotation=(0,0,j*math.pi-.3))
  for side in [-1,1]:
   ob=path('Calm__gentle_unfurl_arc_'+str(side),[(target.x-.28,side*.29,.47),(target.x-.07,side*.38,.53),(target.x+.17,side*.37,.49),(target.x+.29,side*.26,.40)],.035,'violet_core' if side<0 else 'ivory',aftermath);pulse(ob,contact+3,contact+16,clear)
   base=ob.location.copy();key(ob,contact+3,location=base);key(ob,clear,location=base+Vector((0,side*.05,-.06)))
 elif sid=='conjure_rain':
  # Empty-handed utility. A learning source is not a casting equipment requirement.
  beds=[(-1.35,-1),(-.35,1),(.65,0)]
  for index,(x,y) in enumerate(beds):
   box('Crop__planter_%d'%index,(x,y,.06),(.72,.72,.13),'wood',bevel=.025)
   box('Crop__soil_%d'%index,(x,y,.135),(.61,.61,.04),'soil')
   leafroot=empty('Crop__leaf_dip_pivot_%d'%index,location=(x,y,.16))
   for j in range(5):
    a=j*math.tau/5;shard('Crop__leaf_%d_%d'%(index,j),(.1*math.cos(a),.1*math.sin(a),.12),.37,.19,'leaf' if j%2 else 'moss_light',leafroot,a)
   for f,a in [(0,0),(22,0),(32,.045),(48,0),(END,0)]:key(leafroot,f,rotation=(a,0,0))
   for j in range(9):
    px=x+(j%3-1)*.19;py=y+(j//3-1)*.19
    ob=beam('Rain__crop_%d_short_stroke_%d'%(index,j),(0,0,-.10),(0,0,.10),.018,'water_light' if j%2 else 'ivory',root,sides=4)
    # A defining drop reveals during charge; its fall still starts at exact contact.
    start=22+j*1.05; pulse(ob,15 if j==0 else start,start+12,finish+8)
    for f,z in [(start,1.02),(start+21,.22)]:key(ob,f,location=(px,py,z))
    if j in (0,4,8):
     bead=eico('crop_%d_soft_bead_%d'%(index,j),(0,0,0),(.034,.033,.052),'foam');pulse(bead,start+2,start+13,finish+8)
     key(bead,start+2,location=(px,py,.93));key(bead,start+21,location=(px,py,.22))
   for j in range(3):
    a=j*2.1;pts=[(x+.16*math.cos(t),y+.16*math.sin(t),.22) for t in [a+k*.20 for k in range(7)]]
    ob=path('Rain__crop_%d_bead_arc_%d'%(index,j),pts,.020,'water_light' if j%2 else 'foam',aftermath);pulse(ob,29+j,41,clear)
 spec.update(contactFrame=contact,impactEndFrame=finish,clearFrame=clear)
 return root,aftermath,list(EFFECTS)

def lighting(scene):
 scene.world=bpy.data.worlds.new(scene.name+'_World');scene.world.use_nodes=True;bg=scene.world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.045,.035,.025,1);bg.inputs[1].default_value=.35
 for name,loc,energy,color,size in [('Large warm key',(-3,-4,8),1450,(1,.85,.66),7),('Cool sky bounce',(4,3,6),1150,(.63,.82,1),6),('Soft front fill',(0,-5,3),260,(1,.95,.81),5)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.color=color;data.shape='DISK';data.size=size;ob=link(bpy.data.objects.new(name,data));ob.location=loc;ob.rotation_euler=(Vector((0,0,.3))-ob.location).to_track_quat('-Z','Y').to_euler()
 camera=bpy.data.cameras.new(scene.name+'_Camera');ob=link(bpy.data.objects.new(scene.name+'_Camera',camera));focus=Vector((.10,0,.45));ob.location=focus+Vector((0,-math.cos(PITCH)*11,math.sin(PITCH)*11));ob.rotation_euler=(focus-ob.location).to_track_quat('-Z','Y').to_euler();camera.type='ORTHO';camera.sensor_fit='HORIZONTAL';camera.ortho_scale=7.4;scene.camera=ob
 scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
 scene.render.resolution_x=1100;scene.render.resolution_y=620;scene.render.pixel_aspect_x=1/math.sin(PITCH);scene.render.pixel_aspect_y=1;scene.render.resolution_percentage=100;scene.render.film_transparent=False;scene.render.image_settings.file_format='PNG';scene.render.fps=FPS
 scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=.1
 scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.frame_start=0;scene.frame_end=END

STUDIES=[]
for index,spec in enumerate(SPECS):
 SC=bpy.data.scenes.new('%02d — %s'%(index+1,spec['name']));bpy.context.window.scene=SC
 ACTIVE=bpy.data.collections.new(spec['id']+'__DesignStudy');SC.collection.children.link(ACTIVE)
 stage(spec);ar,rig,actorobjs=caster(spec)
 if spec['id']!='conjure_rain':dummy(spec)
 er,af,fx=effect_study(spec);lighting(SC)
 for name,frame in [('Gather',0),('Cast end',12),('Release',22),('Contact',spec['contactFrame']),('Impact end',spec['impactEndFrame']),('Clear',spec['clearFrame']),('Review loop reset',END)]:SC.timeline_markers.new(name,frame=round(frame))
 SC['nativeTiming']='100 fps; cast .12 s + charge .10 s; travel .025 s per recorded cell; impact .20 s; aftermath .18 s'
 SC['previewCamera']='56 degrees down; one metre per cell; studio light only'
 SC['phaseFrameExactJson']=json.dumps({k:spec[k] for k in ('contactFrame','impactEndFrame','clearFrame')})
 SC['gameplayBoundary']='DESIGN STUDY: no damage, targeting, status, occupancy, simulation, or Unity integration'
 STUDIES.append(dict(spec=spec,scene=SC,actorRoot=ar,rig=rig,actorObjects=actorobjs,effectRoot=er,aftermathRoot=af,effects=fx,collection=ACTIVE))

def descendants(root):return [root]+[ob for ob in root.children_recursive]
def export(study):
 spec=study['spec'];scene=study['scene'];bpy.context.window.scene=scene
 bpy.ops.object.select_all(action='DESELECT');objects=descendants(study['actorRoot'])+descendants(study['effectRoot'])
 for ob in objects:ob.select_set(True)
 bpy.context.view_layer.objects.active=study['actorRoot']
 bpy.ops.export_scene.fbx(filepath=str(ROOT/'exports'/(spec['id']+'.fbx')),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1,bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,bake_anim_step=1,path_mode='COPY',embed_textures=True,use_custom_props=True)

def audit(study):
 s=study['scene'];bpy.context.window.scene=s;spec=study['spec'];root=study['actorRoot'];rig=study['rig'];fx=study['effects'];sample=[0,12,22,spec['contactFrame'],spec['contactFrame']+9,spec['clearFrame']+2,END]
 positions=[];poses=[];transforms=[];visible=[]
 for frame in sample:
  s.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update();positions.append(tuple(root.matrix_world.translation));poses.append([float(v) for b in rig.pose.bones for v in b.rotation_euler]);transforms.append([float(v) for ob in fx for row in ob.matrix_world for v in row]);visible.append(sum(max(abs(v) for v in ob.matrix_world.to_scale())>.002 for ob in fx))
 mats=set(mat for ob in descendants(study['effectRoot']) if ob.type=='MESH' for mat in ob.data.materials)
 alphas=[mat.node_tree.nodes.get('Principled BSDF').inputs['Alpha'].default_value for mat in mats]
 result=dict(id=spec['id'],actorRootDrift=max((Vector(p)-Vector(positions[0])).length for p in positions),gesturePoseDelta=max(abs(a-b) for p in poses for a,b in zip(p,poses[0])),animatedMeshCount=len(fx),effectTransformDelta=max(abs(a-b) for p in transforms for a,b in zip(p,transforms[0])),effectVisibleAtContact=visible[3],effectVisibleAfterClear=visible[-2],colliderCount=sum('collider' in ob.name.lower() and not ob.get('noCollider') for ob in descendants(study['effectRoot'])),materialAlphaMinimum=min(alphas),materialAnimatedCount=sum(bool(m.animation_data or m.node_tree.animation_data) for m in mats),exportExists=(ROOT/'exports'/(spec['id']+'.fbx')).is_file(),roundtripBoneCount=0,roundtripAnimatedObjectCount=0,roundtripMeshCount=0,roundtripMotionDelta=0)
 return result

if not args.skip_export:
 for study in STUDIES:export(study)

auditrows=[audit(study) for study in STUDIES]
# Read exported files into a genuinely empty scene. Never infer FBX readiness from exporter success.
if not args.skip_export:
 for study,row in zip(STUDIES,auditrows):
  imp=bpy.data.scenes.new('TEMP_FBX_READBACK');bpy.context.window.scene=imp;imp.render.fps=FPS
  before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(ROOT/'exports'/(study['spec']['id']+'.fbx')),use_anim=True)
  imported=list(set(bpy.data.objects)-before);row['roundtripBoneCount']=sum(len(ob.data.bones) for ob in imported if ob.type=='ARMATURE');row['roundtripAnimatedObjectCount']=sum(bool(ob.animation_data and ob.animation_data.action) for ob in imported);row['roundtripMeshCount']=sum(ob.type=='MESH' for ob in imported)
  states=[];effectstates=[];rootpositions=[];importedfx=[ob for ob in imported if ob.type=='MESH' and ob.get('transientEffect')];importedroots=[ob for ob in imported if ob.get('nativeCellRoot')]
  for frame in [1,23,45,END]:
   imp.frame_set(frame);bpy.context.view_layer.update();states.append([float(v) for ob in sorted(imported,key=lambda o:o.name) if ob.type=='MESH' for matrixrow in ob.matrix_world for v in matrixrow]);effectstates.append([float(v) for ob in sorted(importedfx,key=lambda o:o.name) for matrixrow in ob.matrix_world for v in matrixrow]);rootpositions.append([tuple(ob.matrix_world.translation) for ob in importedroots])
  row['roundtripMotionDelta']=max(abs(a-b) for state in states for a,b in zip(state,states[0]))
  row['roundtripEffectMotionDelta']=max((abs(a-b) for state in effectstates for a,b in zip(state,effectstates[0])),default=0)
  row['roundtripEffectCount']=len(importedfx);row['roundtripEffectVisibleAfterClear']=sum(max(abs(v) for v in ob.matrix_world.to_scale())>.002 for ob in importedfx)
  row['roundtripRootDrift']=max(((Vector(a)-Vector(b)).length for state in rootpositions for a,b in zip(state,rootpositions[0])),default=999)
  for ob in imported:bpy.data.objects.remove(ob,do_unlink=True)
  bpy.context.window.scene=study['scene'];bpy.data.scenes.remove(imp)

for study in STUDIES:study['scene'].frame_set(round(study['spec']['contactFrame']+7))
bpy.context.window.scene=STUDIES[0]['scene']
# Drop empty startup scene; keep appended rig data unlinked as an explicit immutable reference.
for scene in list(bpy.data.scenes):
 if not len(scene.objects):bpy.data.scenes.remove(scene)
for image in bpy.data.images:
 if image.source=='FILE' and not image.packed_file:
  try:image.pack()
  except Exception:pass
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=dict(schemaVersion=1,purpose='Blender design studies; not integrated gameplay assets',source=str(SOURCE.relative_to(ROOT.parents[1])),sourceSha256=SOURCE_HASH,cameraPitchDegrees=56,cameraSensorFit='HORIZONTAL',cameraPixelAspect=[1/math.sin(PITCH),1],outputPresentation='Display rendered PNG and composed videos at square pixels; the projection already compensates ground foreshortening.',unitsPerCell=1,blenderCoordinates='Z up; X is cast direction; FBX -Z forward / Y up',exportReadiness='Blender-roundtrip design assemblies only. Study actor offsets are retained. Native Unity axis, socket, origin extraction and clip import validation pending; existing production export convention differs.',nativeFps=FPS,previewPlayback='sample every third native frame, play at 8 fps (4.167x slower than native)',timingSeconds=dict(cast=.12,charge=.10,travelPerCell=.025,impact=.20,aftermath=.18),rootMotion=False,opaqueOnly=True,noColliders=True,unrenderedOutcomeBranches=['miss / immune / denied status','lethal contact and break-away','blocked push vs successful displacement','wet fire suppression','Rime water-only fallback and dry refusal','Calm already peaceful / failed pacification','Rain no valid crops'],studies=[])
STENCILS={'ember_spit':[[3,0]],'flaming_hands':[[1,0]],'jet_blast':[[1,0],[2,-1],[2,0],[2,1]],'ground_surge':[[1,0],[2,0],[3,0],[4,0]],'rime_grip':[[3,0]],'calm':[[4,0]],'conjure_rain':[[1,-1],[2,1],[3,0]]}
for study in STUDIES:
 s=study['spec'];manifest['studies'].append(dict(**s,isStartingActive=s['id']!='conjure_rain',scene=study['scene'].name,casterAction=study['rig'].animation_data.action.name,actorRoot=study['actorRoot'].name,effectRoot=study['effectRoot'].name,aftermathRoot=study['aftermathRoot'].name,heroFrame=25 if s['id'] in ('ember_spit','calm') else round(s['contactFrame']+7),heroPhase='travel' if s['id'] in ('ember_spit','calm') else 'impact',nativeCellOffsetsShown=STENCILS[s['id']],animatedMeshes=[ob.name for ob in study['effects']],materials=sorted({m.name for ob in descendants(study['effectRoot']) if ob.type=='MESH' for m in ob.data.materials}),export='exports/'+s['id']+'.fbx',notes='Transient success study. Persistent status/ground aftermath must be spawned separately from actual outcome records.'))
(ROOT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
(ROOT/'asset-audit.json').write_text(json.dumps(dict(spellIds=[s['id'] for s in SPECS],cameraPitchDegrees=56,unitsPerCell=1,sourceShaBefore=SOURCE_HASH,sourceShaAfter=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),studies=auditrows),indent=2)+'\n')
if args.render_stills:
 for study in STUDIES:
  scene=study['scene'];bpy.context.window.scene=scene;hero=25 if study['spec']['id'] in ('ember_spit','calm') else study['spec']['contactFrame']+7;scene.frame_set(round(hero));scene.render.filepath=str(ROOT/'renders'/(study['spec']['id']+'.png'));bpy.ops.render.render(write_still=True)
  remove_density_chunk(scene.render.filepath)
  print('STILL_READY '+scene.render.filepath,flush=True)
if args.render_loops:
 for study in STUDIES:
  scene=study['scene'];bpy.context.window.scene=scene;scene.render.resolution_x=840;scene.render.resolution_y=474;scene.cycles.samples=12
  folder=ROOT/'frames'/study['spec']['id'];folder.mkdir(exist_ok=True)
  for i,frame in enumerate(range(0,END+1,3)):
   scene.frame_set(frame);scene.render.filepath=str(folder/('%04d.png'%i));bpy.ops.render.render(write_still=True);remove_density_chunk(scene.render.filepath)
  print('LOOP_FRAMES_READY '+str(folder),flush=True)
print('DESIGN_BUILD_COMPLETE',flush=True)
