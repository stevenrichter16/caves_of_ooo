#!/usr/bin/env python3
"""Morrowfast Village3D deterministic source builder. Run with Blender 5.2+.

blender --background --threads 2 --python build_scene.py -- --output DIR --render
Only writes --output. Input definitions are immutable adjacent JSON snapshots.
Meshes use palette-atlas UVs; node tricks are not required for Unity materials.
"""
import bpy, bmesh, math, random, json, argparse, sys, hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ARGS = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
ap=argparse.ArgumentParser(); ap.add_argument('--output', type=Path, default=Path(__file__).resolve().parent/'build')
ap.add_argument('--render',action='store_true');ap.add_argument('--probe-only',action='store_true');ap.add_argument('--skip-export',action='store_true')
args=ap.parse_args(ARGS); OUT=args.output.resolve(); OUT.mkdir(parents=True,exist_ok=True)
for sub in ('models','textures','renders','reports'): (OUT/sub).mkdir(exist_ok=True)
SOURCE=Path(__file__).resolve().parent
NATIVE=json.loads((SOURCE/'native-definition.json').read_text()); ART=json.loads((SOURCE/'art-definition.json').read_text())
OWNERS={o['id']:o for o in NATIVE['owners']}; ART_OWNERS={o['id']:o for o in ART['owners']}
SEED=9042026; RNG=random.Random(SEED)
# sRGB swatches. Stable index is the atlas contract; colors intentionally muted.
COLORS=[
 ('earth',(0.34,0.37,0.17)),('moss',(0.385,0.415,0.18)),('moss_light',(0.48,0.50,0.21)),('moss_dark',(0.22,0.29,0.10)),
 ('stone',(0.56,0.555,0.44)),('stone_light',(0.68,0.655,0.51)),('stone_dark',(0.42,0.45,0.38)),('stone_warm',(0.62,0.58,0.44)),
 ('wood',(0.37,0.25,0.15)),('wood_light',(0.54,0.38,0.23)),('wood_dark',(0.23,0.16,0.10)),('wood_end',(0.62,0.47,0.29)),
 ('iron',(0.26,0.29,0.27)),('iron_light',(0.45,0.48,0.43)),('rope',(0.68,0.56,0.32)),('soil',(0.24,0.20,0.11)),
 ('leaf',(0.29,0.40,0.09)),('leaf_light',(0.44,0.52,0.13)),('leaf_dark',(0.205,0.29,0.065)),('cream',(0.84,0.80,0.64)),
 ('red',(0.64,0.20,0.13)),('gold',(0.78,0.57,0.20)),('teal',(0.25,0.47,0.43)),('violet',(0.48,0.32,0.48)),
 ('water',(0.09,0.21,0.23)),('water_light',(0.20,0.38,0.37)),('flower_white',(0.87,0.84,0.65)),('flower_pink',(0.76,0.31,0.46)),
 ('hood_teal',(0.14,0.44,0.44)),('hood_gold',(0.68,0.48,0.17)),('hood_violet',(0.43,0.32,0.49)),('hood_olive',(0.39,0.43,0.24)),
 ('skin',(0.69,0.57,0.40)),('face_shadow',(0.12,0.14,0.12)),('leather',(0.30,0.22,0.14)),('steel',(0.61,0.64,0.57)),
 ('cobble',(0.57,0.535,0.40)),('cobble_light',(0.69,0.635,0.49)),('cobble_dark',(0.43,0.435,0.33)),('tile_earth',(0.43,0.37,0.24)),
 ('roof_moss',(0.43,0.44,0.185)),('roof_moss_light',(0.51,0.51,0.235)),('roof_moss_dark',(0.405,0.425,0.185)),('roof_stone',(0.50,0.50,0.31)),
 ('flower_yellow',(0.79,0.67,0.28)),('cabbage',(0.36,0.54,0.17)),('cabbage_light',(0.54,0.66,0.27)),('bread',(0.74,0.55,0.28)),
 ('cloth_shadow',(0.26,0.36,0.31)),('cloth_light',(0.79,0.66,0.33)),('paper',(0.79,0.74,0.57)),('terracotta',(0.53,0.32,0.20)),
 ('book_blue',(0.28,0.40,0.43)),('book_red',(0.48,0.23,0.21)),('book_green',(0.33,0.39,0.20)),('ember',(0.72,0.28,0.08)),
 ('axis_x',(0.85,0.12,0.08)),('axis_y',(0.12,0.72,0.20)),('axis_z',(0.12,0.29,0.85)),('black',(0.06,0.07,0.05)),
 ('grass',(0.43,0.47,0.20)),('grass_light',(0.59,0.59,0.28)),('pebble',(0.55,0.54,0.40)),('rim',(0.75,0.71,0.55))]
CINDEX={name:i for i,(name,_) in enumerate(COLORS)}
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
 if c.name!='Collection':bpy.data.collections.remove(c)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1
SCENE.render.threads_mode='FIXED';SCENE.render.threads=2
# Atlas swatches carry subtle deterministic paint grain rather than flat fills.
# Per-face box-projected UVs use one inset tile, so runtime needs one texture.
ATLAS_SIZE=1024;TILE=ATLAS_SIZE//8
ATLAS=bpy.data.images.new('VillagePalette',width=ATLAS_SIZE,height=ATLAS_SIZE,alpha=False)
def paint_noise(u,v,freq,salt):
 x=u*freq;y=v*freq;ix=math.floor(x);iy=math.floor(y);fx=x-ix;fy=y-iy
 fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
 def cell(a,b):
  q=(a*374761393+b*668265263+salt*1442695041)&0xffffffff
  q=((q^(q>>13))*1274126177)&0xffffffff
  return ((q^(q>>16))&65535)/32767.5-1
 a=cell(ix,iy)*(1-fx)+cell(ix+1,iy)*fx;b=cell(ix,iy+1)*(1-fx)+cell(ix+1,iy+1)*fx
 return a*(1-fy)+b*fy
pixels=[]
paint=random.Random(SEED+401)
for y in range(ATLAS_SIZE):
 for x in range(ATLAS_SIZE):
  name,base=COLORS[(y//TILE)*8+x//TILE];u=(x%TILE)/TILE;v=(y%TILE)/TILE
  grain=paint.uniform(-.045,.045)
  broad=.025*math.sin(u*17+math.cos(v*12))+.016*math.sin(v*31+u*9)
  factor=1+grain+broad
  # Material-local variation is exported in albedo, not a Cycles-only node trick.
  if name in ('stone','stone_light','stone_warm','cobble','cobble_light','cobble_dark','roof_stone'):
   factor+=.10*math.sin(u*8+math.sin(v*7))+.025*math.sin(v*58+u*12)
  if name in ('earth','moss','moss_light','moss_dark','grass'):
   factor=1+grain+.15*paint_noise(u,v,4,17)+.10*paint_noise(u,v,12,31)+.055*paint_noise(u,v,31,61)+.030*paint_noise(u,v,79,93)
   if paint.random()<.055:factor+=paint.uniform(-.18,.22)
  if name in ('wood','wood_light','wood_dark','wood_end'):factor+=.055*math.sin(u*64+math.sin(v*4)*2)
  if name.startswith('roof_') or name in ('moss','earth','grass'):
   factor+=.025*paint_noise(u,v,23,43)
   if paint.random()<.012:factor+=.20
  pixels.extend((*[max(0,min(1,c*factor)) for c in base],1))
ATLAS.pixels=pixels;ATLAS.filepath_raw=str(OUT/'textures/VillagePalette.png');ATLAS.file_format='PNG';ATLAS.save()
MAT=bpy.data.materials.new('VillagePalette');MAT.use_nodes=True
bs=MAT.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88
tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.name='VillagePaletteAtlas';tex.image=ATLAS;tex.interpolation='Linear'
MAT.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
WATER=bpy.data.materials.new('VillageWater');WATER.use_nodes=True
bs=WATER.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.012,.045,.051,1);bs.inputs['Roughness'].default_value=.43;bs.inputs['Metallic'].default_value=.05;bs.inputs['Specular IOR Level'].default_value=.12
MODELS={}; CURRENT=None; PLACEMENTS=[]; STATIC=[]; SERIAL=0

def color_mesh(mesh,color):
 idx=CINDEX[color];uv=mesh.uv_layers.new(name='UVMap') if not mesh.uv_layers else mesh.uv_layers[0]
 mins=[min(v.co[i] for v in mesh.vertices) for i in range(3)];sizes=[max(v.co[i] for v in mesh.vertices)-mins[i] for i in range(3)]
 for poly in mesh.polygons:
  dominant=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3) if i!=dominant]
  for li in poly.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co
   a=(co[axes[0]]-mins[axes[0]])/max(sizes[axes[0]],.0001);b=(co[axes[1]]-mins[axes[1]])/max(sizes[axes[1]],.0001)
   uv.data[li].uv=((idx%8+.06+.88*a)/8,(idx//8+.06+.88*b)/8)
 attr=mesh.color_attributes.new(name='VillageColor',type='BYTE_COLOR',domain='CORNER') if not mesh.color_attributes else mesh.color_attributes[0]
 for q in attr.data:q.color=(*COLORS[idx][1],1)
 mesh.materials.append(MAT)

def newmesh(name,verts,faces,color,smooth=False):
 mesh=bpy.data.meshes.new(name+'Mesh');mesh.from_pydata(verts,[],faces);mesh.update();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob)
 for p in mesh.polygons:p.use_smooth=smooth
 return ob

def box(name,loc,size,color,bevel=.07,rot=0):
 bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
 for v in bm.verts:v.co.x*=size[0];v.co.y*=size[1];v.co.z*=size[2]
 if bevel>0:bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(size)*.3),segments=2,affect='EDGES')
 mesh=bpy.data.meshes.new(name+'Mesh');bm.to_mesh(mesh);bm.free();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob);ob.location=loc;ob.rotation_euler[2]=rot
 # Broad faces remain flat, rounded bevel faces smooth.
 for p in mesh.polygons:p.use_smooth=len(p.vertices)==4 and p.area < min(size)*max(size)*.25
 return ob

def ellipsoid(name,loc,scale,color,sub=1):
 bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=sub,radius=1)
 for v in bm.verts:v.co.x*=scale[0];v.co.y*=scale[1];v.co.z*=scale[2]
 mesh=bpy.data.meshes.new(name+'Mesh');bm.to_mesh(mesh);bm.free();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob);ob.location=loc
 for p in mesh.polygons:p.use_smooth=True
 return ob

def softstone(name,loc,size,color,rng,rot=0):
 # Low, softly bevelled irregular masonry: flattened tops catch light while
 # unequal rounded corners keep the overhead silhouette from looking tiled.
 n=8;outline=[]
 for i in range(n):
  a=(i+.5)*math.tau/n
  outline.append((math.copysign(abs(math.cos(a))**.6,math.cos(a))*rng.uniform(.92,1.04),math.copysign(abs(math.sin(a))**.6,math.sin(a))*rng.uniform(.92,1.04)))
 verts=[]
 for z,r in [(-.5,.72),(-.19,1),(.19,1),(.5,.76)]:
  verts.extend((x*size[0]*.5*r,y*size[1]*.5*r,z*size[2]) for x,y in outline)
 faces=[tuple(reversed(range(n))),tuple(range(3*n,4*n))]
 for j in range(3):
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 ob=newmesh(name,verts,faces,color,True);ob.location=loc;ob.rotation_euler[2]=rot
 ob.data.polygons[0].use_smooth=False;ob.data.polygons[1].use_smooth=False
 return ob

def fittedstone(name,loc,outline,height,color,flat_top=False):
 n=len(outline);vs=[]
 for z,r in [(-.25,.79),(.07,1),(.56,.69)]:
  vs.extend((x*r,y*r,z*height) for x,y in outline)
 fs=[tuple(reversed(range(n))),tuple(range(n*2,n*3))]
 for ring in range(2):
  for i in range(n):fs.append((ring*n+i,ring*n+(i+1)%n,(ring+1)*n+(i+1)%n,(ring+1)*n+i))
 ob=newmesh(name,vs,fs,color,True);ob.location=loc
 ob.data.polygons[0].use_smooth=False
 if flat_top:ob.data.polygons[1].use_smooth=False
 return ob

def leafshape(name,loc,length,width,angle,color,tilt=.045):
 # Ten smooth facets around a gently rounded leaf; closed volume catches light
 # and avoids black razor-thin card interiors from the overhead camera.
 n=5;vs=[]
 for i in range(n):
  a=i*math.tau/n;vs.append((math.cos(a)*length*.53,math.sin(a)*width*.52,0))
 vs.extend([(0,0,tilt),(0,0,-tilt*.48)])
 fs=[(n,i,(i+1)%n) for i in range(n)]+[(n+1,(i+1)%n,i) for i in range(n)]
 ob=newmesh(name,vs,fs,color,True);ob.location=loc;ob.rotation_euler[2]=angle
 return ob

def cylinder(name,loc,radius,depth,color,vertices=12,radius2=None):
 r2=radius if radius2 is None else radius2; vs=[]
 for z,r in [(-depth/2,radius),(depth/2,r2)]:
  for i in range(vertices):a=i*math.tau/vertices;vs.append((r*math.cos(a),r*math.sin(a),z))
 fs=[tuple(reversed(range(vertices))),tuple(range(vertices,vertices*2))]
 for i in range(vertices):j=(i+1)%vertices;fs.append((i,j,j+vertices,i+vertices))
 ob=newmesh(name,vs,fs,color);ob.location=loc
 for p in ob.data.polygons:p.use_smooth=p.index>1
 return ob

def beam(name,a,b,r,color,vertices=8):
 a=Vector(a);b=Vector(b);ob=cylinder(name,(a+b)*.5,r,(b-a).length,color,vertices)
 ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return ob

def torus(name,loc,r,minor,color,major_segments=16,minor_segments=5):
 vs=[];fs=[]
 for i in range(major_segments):
  a=math.tau*i/major_segments
  for j in range(minor_segments):
   b=math.tau*j/minor_segments;rr=r+minor*math.cos(b);vs.append((rr*math.cos(a),rr*math.sin(a),minor*math.sin(b)))
 for i in range(major_segments):
  for j in range(minor_segments):fs.append((i*minor_segments+j,((i+1)%major_segments)*minor_segments+j,((i+1)%major_segments)*minor_segments+(j+1)%minor_segments,i*minor_segments+(j+1)%minor_segments))
 ob=newmesh(name,vs,fs,color,True);ob.location=loc;return ob

def model(mid,kind='prop',pivot='ground-centre'):
 global CURRENT
 CURRENT=bpy.data.collections.new(mid);MODELS[mid]={'id':mid,'collection':CURRENT,'kind':kind,'pivot':pivot};return CURRENT

def instance(mid,loc=(0,0,0),angle=0,scale=(1,1,1),owner=None,role='static'):
 ob=bpy.data.objects.new(owner or mid+'_instance',None);ob.instance_type='COLLECTION';ob.instance_collection=MODELS[mid]['collection'];SCENE.collection.objects.link(ob)
 ob.location=loc;ob.rotation_euler[2]=angle;ob.scale=scale
 ob['modelId']=mid;ob['ownerId']=owner or '';ob['role']=role
 return ob

def export_collection(mid):
 # Source collections remain editable. FBX contains a named root and explicit
 # mesh children. Root sits at source origin and carries no camera/light helpers.
 coll=MODELS[mid]['collection'];temps=[]
 root=bpy.data.objects.new(mid,None);SCENE.collection.objects.link(root);temps.append(root)
 # Unity FBX importer reverses both horizontal source axes. The export-only
 # parent rotates them once; authoring geometry and manifest stay east/north.
 root.rotation_euler[2]=math.pi
 for source in coll.objects:
  ob=source.copy()
  if source.data:ob.data=source.data
  SCENE.collection.objects.link(ob);ob.parent=root;ob.matrix_parent_inverse=Matrix.Identity(4);temps.append(ob)
 for ob in bpy.context.selected_objects:ob.select_set(False)
 for ob in temps:ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/'models'/f'{mid}.fbx'),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},
  axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1.0,
  bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,
  add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_custom_props=True)
 for ob in temps:bpy.data.objects.remove(ob,do_unlink=True)

def make_probe():
 model('axis_probe','validation')
 box('one_metre_cube',(0,0,.5),(1,1,1),'cream',0)
 beam('X_EAST_RED',(0,0,0),(2,0,0),.08,'axis_x');cylinder('X_end_two_discs',(2,0,.05),.2,.1,'axis_x')
 beam('Y_NORTH_GREEN',(0,0,0),(0,3,0),.08,'axis_y');box('Y_end_square',(0,3,.1),(.4,.4,.2),'axis_y')
 beam('Z_UP_BLUE',(0,0,0),(0,0,4),.08,'axis_z');ellipsoid('Z_end_ball',(0,0,4),(.22,.22,.22),'axis_z')
 export_collection('axis_probe')
 (OUT/'reports/axis-probe.json').write_text(json.dumps({'authoring':'Blender X east, Y north, Z up; metre units','export':{'axis_forward':'Z','axis_up':'Y','bake_space_transform':True,'apply_scale_options':'FBX_SCALE_UNITS','exportRootRotationBlenderZDegrees':180},'cube':{'dimensions':[1,1,1],'groundOrigin':[0,0,0]},'endpointsBlender':{'redX':[2,0,0],'greenY':[0,3,0],'blueZ':[0,0,4]},'expectedUnityWorld':{'redX':[2,0,0],'greenY':[0,0,3],'blueZ':[0,4,0]},'status':'Unity must verify imported hierarchy and axes; this is a probe, not accepted orientation evidence'},indent=2))
make_probe()
if args.probe_only:
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'axis_probe.blend'));sys.exit(0)

def barrel(variant=0,sideways=False):
 rng=random.Random(810+variant);count=12;r=.42;h=.92
 for i in range(count):
  a=math.tau*i/count;vs=[]
  for z,rr in [(0,r*.82),(.15,r),(.46,r*1.06),(.78,r),(.92,r*.82)]:
   for da in [-.47,.47]: t=a+da*math.tau/count;vs.append((rr*math.cos(t),rr*math.sin(t),z))
  fs=[(j*2,j*2+1,j*2+3,j*2+2) for j in range(4)]
  ob=newmesh('Oak_stave_%02d'%i,vs,fs,['wood','wood_light','wood_dark'][(i+variant)%3],True)
 for z,rr in [(.13,r),(.72,r*1.01)]:torus('Iron_hoop',(0,0,z),rr,.037,'iron',12,4)
 cylinder('Barrel_lid',(0,0,h-.025),r*.8,.04,'wood_end',12)
 for x in [-.17,0,.17]:box('Lid_seam',(x,0,h),(.013,.59,.006),'wood_dark',0)
 if variant==1:torus('Rope_coil',(0,0,h+.045),.22,.04,'rope',12,4)
 if variant==2:box('Lid_brace',(0,0,h+.035),(.08,.58,.04),'wood_light',.012,rot=.55)
 if variant==3:cylinder('Iron_bung',(.15,0,h+.02),.065,.035,'iron',8)
 if sideways:
  for ob in CURRENT.objects:ob.matrix_world=Matrix.Translation((0,0,.48))@Matrix.Rotation(math.pi/2,4,'X')@Matrix.Translation((0,0,-h/2))@ob.matrix_world

def crate(variant=0):
 for z in [.1,.32,.54]:
  for y in [-.37,.37]:box('Side_plank',(0,y,z),(.82,.09,.19),'wood_light' if variant%2 else 'wood',.025)
  for x in [-.37,.37]:box('End_plank',(x,0,z),(.09,.66,.19),'wood',.025)
 for x in [-.37,.37]:
  for y in [-.37,.37]:box('Corner',(x,y,.35),(.105,.105,.75),'wood_dark',.026)
 box('Crate_floor',(0,0,.06),(.7,.7,.09),'wood_dark',.02)
 if variant%2==0:
  for i in range(4):box('Lid_plank',(-.3+i*.2,0,.69),(.185,.82,.09),'wood_light',.014)
  beam('Cross_brace',(-.32,-.32,.76),(.32,.32,.76),.04,'wood_dark',5)
 else:
  for i in range(6):beam('Stored_kindling',((i%3)*.15-.15,(i//3)*.2-.1,.1),((i%3)*.13-.1,(i//3)*.19-.1,.78+(i%2)*.14),.045,'wood_end',5)

def bucket(variant=0):
 cylinder('Bucket_body',(0,0,.27),.24,.5,'wood_light',12,radius2=.32)
 cylinder('Bucket_inside',(0,0,.528),.265,.012,'water' if variant%2 else 'wood_dark',16)
 torus('Upper_band',(0,0,.49),.315,.025,'iron',16,4);torus('Lower_band',(0,0,.07),.25,.024,'iron',12,4)
 for s in [-1,1]:beam('Handle_post',(s*.28,0,.43),(s*.27,0,.83),.025,'iron',6)
 beam('Handle_grip',(-.27,0,.83),(.27,0,.83),.035,'wood_end',8)

def planter(variant=0):
 width=1.8 if variant!=3 else 1.0;length=.86 if variant!=3 else 1.0
 box('Loam',(0,0,.15),(width,length,.22),'soil',.1)
 for y in [-length/2,length/2]:box('Bed_rail',(0,y,.26),(width+.15,.13,.3),'wood',.045)
 for x in [-width/2,width/2]:box('Bed_end',(x,0,.26),(.13,length+.15,.3),'wood_light',.04)
 for x in [-width/2,width/2]:
  for y in [-length/2,length/2]:cylinder('Bed_post',(x,y,.25),.10,.58,'wood_end',8)
 for i in range(6 if variant!=3 else 4):
  x=(i%3-1)*.47 if variant!=3 else (i%2-.5)*.38;y=(i//3-.5)*.35 if variant!=3 else (i//2-.5)*.38
  if variant==2:
   for k in range(4):a=k*math.tau/4;ellipsoid('Cabbage_leaf',(x+.08*math.cos(a),y+.08*math.sin(a),.43),(.19,.16,.17),'cabbage_light' if k%2 else 'cabbage',1)
  else:
   ellipsoid('Plant_leaves',(x,y,.38),(.20,.16,.13),'leaf',1)
   for k in range(3):
    a=k*2.1;z=.48+(.03 if k==1 else 0)
    ellipsoid('Fruit_or_flower',(x+.1*math.cos(a),y+.09*math.sin(a),z),(.078,.078,.085),'red' if variant==0 else ('flower_yellow' if variant==1 else 'flower_pink'),1)

def roundpot(variant=0):
 cylinder('Pot',(0,0,.23),.26,.43,'terracotta',12,radius2=.35);torus('Pot_rim',(0,0,.45),.33,.045,'wood_end',16,5)
 cylinder('Pot_soil',(0,0,.45),.285,.014,'soil',12)
 for i in range(5):
  a=i*math.tau/5;ellipsoid('Herb_leaf',(.15*math.cos(a),.15*math.sin(a),.61),(.13,.11,.18),'leaf_light' if i%2 else 'leaf',1)
  if i%2==0:ellipsoid('Bloom',(.16*math.cos(a),.16*math.sin(a),.78),(.08,.08,.07),'flower_pink' if variant%2 else 'flower_white',1)

def rope_coil():
 for r in [.14,.22,.30]:torus('Woven_rope',(0,0,.055),r,.035,'rope',18,5)
 beam('Loose_end',(.32,0,.05),(.49,.22,.05),.03,'rope',6)

def table(variant=0):
 width=1.35 if variant!=2 else 1.7;depth=.78
 for i in range(4):box('Table_top_plank',((i-1.5)*width/4,0,.78),(width/4-.018,depth,.12),'wood_light',.025)
 for x in [-width*.38,width*.38]:
  for y in [-depth*.33,depth*.33]:box('Table_leg',(x,y,.37),(.13,.13,.74),'wood',.024)
 beam('Low_stretcher',(-width*.39,0,.26),(width*.39,0,.26),.07,'wood_dark',6)
 if variant==1:
  box('Folded_paper',(-.28,.10,.865),(.38,.26,.018),'paper',.006,rot=.14)
  cylinder('Inkpot',(.25,.08,.92),.085,.15,'iron',10);beam('Quill',(.26,.08,.97),(.33,.11,1.23),.017,'cream',5)
 elif variant==2:
  for i in range(3):box('Work_tools',(-.45+i*.32,.05,.88),(.21,.10,.035),'steel',.01,rot=.3+i*.45)
  rope_ob=torus('Workbench_cord',(.42,0,.9),.15,.025,'rope',12,4)
 else:
  cylinder('Serving_plate',(0,0,.87),.21,.025,'cream',14);ellipsoid('Loaf',(0,0,.94),(.16,.11,.08),'bread',1)

def stool():
 cylinder('Stool_seat',(0,0,.51),.27,.1,'wood_light',12)
 for a in [0,math.tau/3,2*math.tau/3]:beam('Stool_leg',(.22*math.cos(a),.22*math.sin(a),.04),(.18*math.cos(a),.18*math.sin(a),.5),.05,'wood',7)

def bed(variant=0):
 box('Bed_frame',(0,0,.24),(.8,1.7,.20),'wood',.08)
 for x in [-.37,.37]:
  for y in [-.8,.8]:box('Bed_post',(x,y,.31),(.12,.12,.62),'wood_dark',.025)
 box('Mattress',(0,0,.41),(.72,1.56,.20),'cream',.14)
 box('Wool_blanket',(0,-.24,.535),(.76,1.04,.07),'teal' if variant==0 else 'violet',.09)
 box('Pillow',(0,.52,.56),(.57,.36,.18),'paper',.12)


def bookshelf():
 for x in [-.62,.62]:box('Shelf_upright',(x,0,.85),(.12,.32,1.7),'wood',.035)
 for z in [.15,.69,1.23,1.65]:box('Shelf_board',(0,0,z),(1.35,.36,.08),'wood_light',.02)
 for shelf in range(3):
  for i in range(7):box('Ledger_%d_%d'%(shelf,i),(-.49+i*.16,0,.19+shelf*.53+.21),(.12,.26,.39+(i%2)*.04),['book_red','book_blue','book_green','paper'][(i+shelf)%4],.01,rot=0)

def bench():
 for x in [-.58,.58]:box('Bench_support',(x,0,.26),(.16,.4,.52),'wood',.025)
 for y in [-.10,.10]:box('Bench_seat',(0,y,.56),(1.5,.18,.12),'wood_light',.025)
 box('Bench_back',(0,.23,.85),(1.5,.10,.32),'wood',.04)

def hearth(variant=0):
 for x in [-.48,.48]:box('Hearth_cheek',(x,0,.42),(.28,.70,.8),'stone_dark',.1)
 box('Hearth_back',(0,.28,.45),(.85,.25,.85),'stone',.1)
 box('Hearth_arch',(0,-.03,.90),(1.23,.82,.27),'stone_light',.14)
 box('Fire_bed',(0,-.13,.1),(.72,.63,.14),'face_shadow',.04)
 for i in range(3):beam('Charred_log',(-.28,-.22+i*.16,.2),(.27,-.10+i*.11,.2),.065,'wood_dark',7)
 for x in [-.17,.04,.21]:ellipsoid('Glowing_coal',(x,-.14,.22),(.10,.12,.045),'ember',1)
 if variant: cylinder('Cooking_pot',(0,0,1.09),.32,.27,'iron',16,radius2=.38);torus('Pot_lip',(0,0,1.23),.38,.035,'iron_light')


def oven():
 ellipsoid('Turf_oven_dome',(0,0,.72),(1.1,1.08,.84),'roof_moss',2)
 for i in range(9):
  a=math.pi*i/8;ellipsoid('Mouth_arch_stone',(.56*math.cos(a),-.88,.15+.61*math.sin(a)),(.24,.22,.20),'stone_light' if i%2 else 'stone',1)
 box('Dark_oven_mouth',(0,-.96,.33),(.82,.10,.58),'face_shadow',.25)
 box('Oven_hearth',(0,-1.03,.08),(1.24,.6,.18),'stone_dark',.07)
 for i in range(14):
  a=i*2.4;r=.9*math.sqrt((i+.5)/14);ellipsoid('Oven_moss',(.8*r*math.cos(a),.75*r*math.sin(a),.9+.35*(1-r)),(.24,.20,.10),'moss_light' if i%3==0 else 'moss',1)


def well():
 rng=random.Random(SEED+813);n=18;r=1.65
 cylinder('Water',(0,0,.22),1.415,.025,'water',48)
 ob=CURRENT.objects[-1];ob.data.materials.clear();ob.data.materials.append(WATER);ob['keepSeparate']=True
 cylinder('Well_dark_bottom',(0,0,.02),1.72,.12,'face_shadow',40)
 # The inner lining remains visibly deep below the uneven worn lip.
 for course in range(2):
  for i in range(n):
   a=math.tau*(i+(course%2)*.5)/n
   softstone('Ring_stone_%d_%02d'%(course,i),(r*math.cos(a),r*math.sin(a),.24+course*.35),(.57,.43,.39),['stone','stone_warm','stone_dark'][(i+course)%3],rng,rot=a+math.pi/2+rng.uniform(-.06,.06))
 for i in range(n):
  a=math.tau*i/n+rng.uniform(-.01,.01);rr=r+rng.uniform(-.026,.026)
  softstone('Worn_coping_%02d'%i,(rr*math.cos(a),rr*math.sin(a),.96+rng.uniform(-.04,.04)),(.59+rng.uniform(-.04,.02),.47+rng.uniform(-.03,.03),.29),rng.choice(['stone_light','stone_light','stone','stone_warm']),rng,rot=a+math.pi/2+rng.uniform(-.09,.09))
  if i%3==1:
   leafshape('Rim_crevice_moss',(rr*math.cos(a),rr*math.sin(a),1.12),.21,.065,a,'moss',.006)
 # Asymmetric broken arcs survive final image size; no refraction or hidden-scene reflection.
 for i in range(17):
  cx=rng.uniform(-.85,.85);cy=rng.uniform(-.85,.85);rad=rng.uniform(.12,.32);start=rng.uniform(0,math.tau)
  if math.hypot(cx,cy)+rad>1.36:continue
  points=[]
  for j in range(6):
   a=start+j*.22;points.append((cx+rad*math.cos(a),cy+rad*.72*math.sin(a),.244))
  vs=[]
  for j,q in enumerate(points):
   a=start+j*.22;w=.010*math.sin((j+.4)/6.8*math.pi)+.005
   vs.extend([(q[0]-w*math.cos(a),q[1]-w*math.sin(a),q[2]),(q[0]+w*math.cos(a),q[1]+w*math.sin(a),q[2])])
  newmesh('Broken_water_curve_%02d'%i,vs,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(5)],'water_light' if i%4 else 'water')


def fence(variant=0):
 length=2.4
 for x in [-length/2,length/2]:
  cylinder('Fence_post',(x,0,.49),.18,.98,'wood',12,radius2=.16)
  cylinder('Post_endgrain',(x,0,1.004),.177,.045,'wood_end',12)
  torus('Cap_weathered_ring',(x,0,1.028),.128,.014,'wood_light',12,4)
  torus('Post_binding',(x,0,.81),.161,.019,'rope',12,4)
 for z in [.34,.68]:
  y=.065 if z<.5 else -.045
  if variant==2 and z==.68:beam('Broken_rail',(-length/2,y,z),(.2,y-.035,z-.18),.093,'wood_light',8)
  else:
   beam('Fence_rail_a',(-length/2,y,z),(0,y+.055*(variant-1),z-.038),.092,'wood_light',8)
   beam('Fence_rail_b',(0,y+.055*(variant-1),z-.038),(length/2,y,z+(variant-1)*.025),.092,'wood',8)
 if variant==3:beam('Diagonal_brace',(-1.05,.10,.23),(.95,.10,.81),.057,'wood_dark',7)

def gate():
 for x in [-1.8,1.8]:
  cylinder('Gate_post',(x,0,1.15),.25,2.3,'wood',12,radius2=.21);cylinder('Gate_cap',(x,0,2.32),.26,.15,'wood_end',12)
  for z in [.62,1.8]:torus('Gate_binding',(x,0,z),.25,.045,'rope',12,4)
 beam('Oath_crossbar',(-1.8,0,2.02),(1.8,0,2.02),.16,'wood_light',12)
 for x in [-.7,0,.7]:
  beam('Token_cord',(x,0,2.01),(x,-.025,1.55),.015,'rope',5)
  box('Oath_token',(x,-.025,1.53),(.17,.06,.24),'cream' if x else 'teal',.06)


def stall(variant=0):
 w=2.9;d=2.6;rng=random.Random(SEED+221+variant)
 for x in [-w/2,w/2]:
  for y in [-d/2,d/2]:
   cylinder('Stall_post',(x,y,.95),.105,1.9,'wood',10);ellipsoid('Post_cap',(x,y,1.96),(.14,.14,.115),'wood_end',2)
 for x in [-w/2,w/2]:beam('Canopy_side',(x,-d/2,1.8),(x,d/2,1.8),.065,'wood_light',8)
 def cloth_point(u,v):
  x=(u-.5)*w*(1-.11*math.sin(math.pi*v));y=-.77+v*(d/2+.77)
  y+=(.07 if v<.5 else -.10)*math.sin(math.pi*u)
  sag=.36*math.sin(math.pi*u)*math.sin(math.pi*v)
  # Broad corner tension fans and a low scalloped front hem survive topdown lighting.
  folds=(.042*math.sin((u-v)*math.pi*3)+.031*math.cos((u+v)*math.pi*4))*math.sin(math.pi*u)*math.sin(math.pi*v)
  return (x,y,1.88-sag+folds)
 for side in [0,1]:
  vs=[];fs=[];steps=14
  for j in range(steps+1):
   for i in range(steps+1):vs.append(cloth_point(side*.5+i*.5/steps,j/steps))
  for j in range(steps):
   for i in range(steps):a=j*(steps+1)+i;fs.append((a,a+1,a+steps+2,a+steps+1))
  newmesh('Draped_canopy_panel_%d'%side,vs,fs,('teal' if side==0 else 'gold') if variant==0 else ('violet' if side==0 else 'hood_violet'),True)
 for edge in ['left','right','front','back','seam']:
  points=[]
  for i in range(15):
   t=i/14;u,v=(0,t) if edge=='left' else (1,t) if edge=='right' else (t,0) if edge=='front' else (t,1) if edge=='back' else (.5,t)
   q=cloth_point(u,v);points.append((q[0],q[1],q[2]+.012))
  for i in range(14):beam('Cloth_hem_'+edge,points[i],points[i+1],.012,'cloth_light' if variant==0 else 'violet',5)
 # Stitches and raised diagonal crease lines imply gathered cloth, not stretched board.
 for u0,v0 in [(0,0),(1,0),(0,1),(1,1)]:
  for j in range(5):
   t=.08+j*.075;q=cloth_point(u0+(.5-u0)*t,v0+(.5-v0)*t);q2=cloth_point(u0+(.5-u0)*(t+.026),v0+(.5-v0)*(t+.026))
   beam('Corner_stitch',(q[0],q[1],q[2]+.018),(q2[0],q2[1],q2[2]+.018),.012,'rope',5)
 for x in [-w/2,w/2]:
  for y in [-d/2,d/2]:beam('Cloth_tie',(x,y,1.88),(x*.97,y*.98,1.51),.019,'rope',5)
 for i in range(7):box('Counter_plank',(-1.23+i*.41,-1.34,.79),(.395,.73,.11),'wood_light' if i%2 else 'wood',.025)
 for x in [-1.10,1.10]:box('Counter_leg',(x,-1.34,.38),(.16,.16,.76),'wood',.025)
 for k,x in enumerate([-.84,0,.84]):
  cylinder('Wares_basket_%d'%k,(x,-1.38,.91),.31,.26,'wood',12,radius2=.39)
  cylinder('Basket_dark_inside',(x,-1.38,1.043),.34,.015,'wood_dark',12)
  torus('Woven_basket_rim',(x,-1.38,1.055),.375,.028,'rope',12,4)
  for i in range(7):
   a=i*2.4;rr=.22*math.sqrt((i+.4)/7);sz=rng.uniform(.09,.14)
   ellipsoid('Wares_%d_%d'%(k,i),(x+rr*math.cos(a),-1.38+rr*math.sin(a),1.10),(sz,sz*.83,sz*.80),('red' if k==0 else 'cabbage_light' if k==1 else 'bread') if variant else ['teal','iron','cream'][k],2 if i%3==0 else 1)


def cart():
 for x in [-.7,.7]:
  wheel=torus('Cart_wheel',(x,0,.55),.49,.085,'wood_dark',16,5);wheel.rotation_euler[1]=math.pi/2
  for a in [0,math.pi/3,2*math.pi/3]:beam('Wheel_spoke',(x,.40*math.cos(a),.55+.40*math.sin(a)),(x,-.4*math.cos(a),.55-.4*math.sin(a)),.035,'wood_light',6)
 box('Cart_bed',(0,0,.6),(1.3,1.6,.16),'wood',.07)
 for i in range(5):box('Cart_plank',(-.49+i*.245,0,.7),(.23,1.45,.1),'wood_light',.02)
 for x in [-.67,.67]:box('Cart_side',(x,0,.96),(.1,1.64,.6),'wood',.05)
 for x in [-.44,.44]:beam('Cart_handle',(x,-.65,.72),(x,-2.0,.6),.065,'wood_light',8)
 for i in range(3):box('Cart_board_load',(-.33+i*.26,.10,.89),(.21,1.4,.21),'wood_end' if i%2 else 'wood',.03,rot=.07*(i-1))


def shrub(variant=0,tree=False):
 rng=random.Random(3100+variant+(100 if tree else 0))
 radius=1.43 if tree else .73;top=1.32 if tree else .50
 if tree:
  cylinder('Low_trunk',(0,0,.49),.17,.98,'wood',10)
  for k in range(3):
   a=k*2.4+variant;beam('Branch',(0,0,.45),(.7*math.cos(a),.6*math.sin(a),.98),.065,'wood_dark',7)
 # Asymmetric overlapping dark masses anchor hundreds of individually readable leaves.
 lobes=[]
 for j in range(5 if tree else 4):
  a=j*2.4+variant*.61;rr=radius*(.39 if j else .08);x=rr*math.cos(a);y=rr*math.sin(a)
  scale=radius*rng.uniform(.43,.60);z=top+rng.uniform(-.16,.12)
  lobes.append((x,y,z,scale));ellipsoid('Dark_foliage_core_%d'%j,(x,y,z),(scale,scale*.90,scale*.45),'leaf',2)
 n=390 if tree else 150
 for i in range(n):
  x0,y0,z0,r=lobes[i%len(lobes)];a=rng.uniform(0,math.tau);rr=r*math.sqrt(rng.random())*1.09
  x=x0+rr*math.cos(a);y=y0+rr*math.sin(a);z=z0+r*.44*math.sqrt(max(0,1-min(1,rr/r)**2))+rng.uniform(.04,.07)
  length=rng.uniform(.24,.37) if tree else rng.uniform(.14,.25)
  col=rng.choice(['leaf','leaf','leaf_light','leaf'])
  if x-y<-.25 and i%3:col='leaf_light'
  leafshape('Layered_leaf_%03d'%i,(x,y,z),length,length*rng.uniform(.48,.7),a+rng.uniform(-.7,.7),col,length*.18)
 if variant==3:
  for i in range(13):
   a=i*2.4;rr=radius*.72*math.sqrt((i+.5)/13);x=rr*math.cos(a);y=rr*math.sin(a)
   ellipsoid('Shrub_bloom',(x,y,top+.23),(.075,.065,.050),'flower_pink',1)


def rockcluster(variant=0):
 rng=random.Random(1500+variant)
 for i in range(3+(variant%2)):
  a=i*2.4;r=.33*math.sqrt(i);sz=rng.uniform(.22,.46)
  ob=ellipsoid('River_rock_%d'%i,(r*math.cos(a),r*math.sin(a),sz*.42),(sz,sz*.84,sz*.69),['stone','stone_dark','stone_light'][(i+variant)%3],1);ob.rotation_euler[2]=a
  if i%2==0:ellipsoid('Rock_moss',(.08+r*math.cos(a),r*math.sin(a),sz*.91),(.19,.15,.037),'moss',1)


def grasspatch(variant=0):
 rng=random.Random(4300+variant)
 for i in range(8):
  a=i*2.4;r=.43*math.sqrt((i+.5)/12);x=r*math.cos(a);y=r*math.sin(a);h=rng.uniform(.035,.10)
  newmesh('Grass_blade_%02d'%i,[(x-.027,y,0),(x+.027,y,0),(x+.055,y+.02,h)],[(0,1,2)],'grass_light' if i%3==0 else 'grass')
 if variant in (1,3):
  for i in range(12):
   a=i*2.4;rr=.46*math.sqrt((i+.5)/12);x=rr*math.cos(a);y=rr*math.sin(a);r=rng.uniform(.045,.078)
   verts=[(x,y,.10)]
   for k in range(8):
    ang=k*math.tau/8;rad=r if k%2==0 else r*.35;verts.append((x+rad*math.cos(ang),y+rad*math.sin(ang),.105))
   newmesh('Daisy_%d'%i,verts,[(0,k+1,(k+1)%8+1) for k in range(8)],'flower_white' if variant==1 else 'flower_yellow')
 for i in range(5):
  a=i*2.4;x=.24*math.cos(a);y=.24*math.sin(a)
  newmesh('Broad_ground_leaf_%d'%i,[(x,y,.02),(x+.07*math.cos(a+1),y+.07*math.sin(a+1),.035),(x+.145*math.cos(a),y+.145*math.sin(a),.026),(x+.07*math.cos(a-1),y+.07*math.sin(a-1),.035)],[(0,1,2,3)],'grass')


def bridge():
 for i in range(9):box('Bridge_board',(-1.35+i*.3375,0,.18),(.32,1.3,.13),'wood_light' if i%2 else 'wood',.035)
 for y in [-.64,.64]:beam('Bridge_stringer',(-1.55,y,.07),(1.55,y,.07),.09,'wood_dark',8)
 for x in [-1.35,1.35]:
  for y in [-.69,.69]:cylinder('Bridge_post',(x,y,.48),.085,.9,'wood',8)
 for y in [-.69,.69]:beam('Bridge_rope',(-1.35,y,.81),(1.35,y,.81),.025,'rope',7)

# Editable reusable prop families. These are independent collection assets,
# not a baked single scene mesh; instances share model IDs in the manifest.
for v in range(4):
 model('barrel-%d'%v);barrel(v)
 model('crate-%d'%v);crate(v)
 model('planter-%d'%v);planter(v)
 model('fence-%d'%v);fence(v)
 model('shrub-%d'%v,'vegetation');shrub(v)
 model('tree-%d'%v,'vegetation');shrub(v,True)
 model('rocks-%d'%v,'decoration');rockcluster(v)
 model('grass-%d'%v,'decoration');grasspatch(v)
model('barrel-side');barrel(0,True)
for v in range(2):model('bucket-%d'%v);bucket(v)
model('herb-pot');roundpot(1)
model('bowl');cylinder('Low_bowl',(0,0,.13),.3,.23,'wood_end',16,radius2=.4);cylinder('Bowl_contents',(0,0,.25),.34,.02,'cream',16)
model('rope-coil');rope_coil()
for v in range(3):model('table-%d'%v);table(v)
model('stool');stool()
for v in range(2):model('bed-%d'%v);bed(v)
model('bookshelf');bookshelf()
model('bench');bench()
for v in range(2):model('hearth-%d'%v);hearth(v)
model('bread-oven','hero');oven()
model('central-well','hero');well()
model('oath-arch','hero');gate()
for v in range(2):model('market-stall-%d'%v,'hero');stall(v)
model('handcart');cart()
model('footbridge');bridge()
model('chopping-block');cylinder('Block',(0,0,.3),.4,.6,'wood',12);cylinder('Cut_face',(0,0,.615),.4,.04,'wood_end',12)
model('tortoise','creature');ellipsoid('Shell',(0,0,.38),(.42,.52,.28),'hood_gold',2)
for x in [-.3,.3]:
 for y in [-.3,.3]:ellipsoid('Foot',(x,y,.13),(.12,.16,.1),'leather',1)
ellipsoid('Head',(0,-.56,.19),(.16,.2,.14),'skin',1)
model('frog','creature');ellipsoid('Frog_body',(0,0,.16),(.3,.3,.17),'leaf_light',1)
for x in [-.2,.2]:ellipsoid('Haunch',(x,.1,.14),(.17,.2,.13),'leaf',1);ellipsoid('Eye',(x,-.2,.29),(.065,.07,.07),'gold',1)

# Five native rooms, separate immutable shells and removable roofs. Doorway
# centres follow the actual native door owner, not guessed screenshot pixels.
for bi,building in enumerate(NATIVE['buildings']):
 bid=building['id'];inside=building['interior'];xs=[p['x'] for p in inside];ys=[p['y'] for p in inside]
 xmin=min(xs)-.46;xmax=max(xs)+1.46;north=25-min(ys)+.46;south=25-max(ys)-1.46
 owner=OWNERS[bid+'-shell'];ax=owner['anchorX']+.5;ay=25-owner['anchorY']-.5
 door=OWNERS[building['doorId']];dx=door['anchorX']+.5
 def rel(x,y,z):return(x-ax,y-ay,z)
 model(bid+'-shell','building-shell','native-owner-anchor')
 box('Room_floor',rel((xmin+xmax)/2,(north+south)/2,.045),(xmax-xmin-.32,north-south-.32,.09),'wood_dark',.05)
 # Broad interior plank strips: authored interior remains visible after roof cutaway.
 count=math.ceil((xmax-xmin-.7)/.5)
 for i in range(count):
  x=xmin+.35+(i+.5)*(xmax-xmin-.7)/count
  box('Interior_plank_%02d'%i,rel(x,(north+south)/2,.115),((xmax-xmin-.7)/count-.018,north-south-.62,.08),'wood' if (i+bi)%3 else 'wood_light',.018)
 for course in range(3):
  z=.30+course*.46
  for side,y in [('North',north-.12),('South',south+.12)]:
   count=math.ceil((xmax-xmin)/.72)
   for i in range(count):
    x=xmin+(i+.5)*(xmax-xmin)/count
    if side=='South' and abs(x-dx)<.68:continue
    color=['stone','stone_light','stone_warm','stone_dark'][(i+course+bi)%4]
    box(side+'_stone_%d_%02d'%(course,i),rel(x,y,z),((xmax-xmin)/count-.035,.51,.44),color,.105)
  for side,x in [('West',xmin+.12),('East',xmax-.12)]:
   count=math.ceil((north-south-.55)/.7)
   for i in range(count):
    y=south+.29+(i+.5)*(north-south-.58)/count
    box(side+'_stone_%d_%02d'%(course,i),rel(x,y,z),(.51,(north-south-.58)/count-.035,.44),['stone','stone_light','stone_dark'][(i+course+bi)%3],.10)
 for sign in [-1,1]:box('Door_jamb',rel(dx+sign*.62,south+.10,.82),(.23,.61,1.55),'wood_dark',.05)
 box('Door_lintel',rel(dx,south+.10,1.63),(1.53,.65,.25),'wood_light',.06)
 box('Threshold',rel(dx,south-.18,.1),(1.10,.75,.15),'stone_light',.06)
 model(bid+'-roof','roof','native-owner-anchor')
 cx=(xmin+xmax)/2;cy=(north+south)/2;w=xmax-xmin+.14;h=north-south+.14
 box('Roof_moss_bed',rel(cx,cy,1.76),(w,h,.28),'roof_moss_dark',.22)
 rng=random.Random(SEED+bi*121)
 # Broad low masonry plates retain a stone top, with broken staggered joints.
 ny=max(5,round(h/.49))
 for j in range(ny):
  y=cy-h/2+(j+.5)*h/ny+rng.uniform(-.063,.063)
  left=cx-w/2+.045;right=cx+w/2-.045;x=left;i=0
  while x<right-.10:
   sw=min(right-x,rng.uniform(.30,.86) if i else rng.uniform(.22,.62));sh=h/ny*rng.uniform(.86,1.035)
   col=rng.choice(['roof_moss','roof_moss','roof_moss_light','roof_stone','roof_moss_light'])
   # Rounded rectangular footprint, not a heap of radially shaped pebbles.
   hw=sw*.51;hh=sh*.52;c=min(hw,hh)*rng.uniform(.23,.35)
   outline=[(-hw+c,-hh),(hw-c,-hh),(hw,-hh+c),(hw,hh-c),(hw-c,hh),(-hw+c,hh),(-hw,hh-c),(-hw,-hh+c)]
   outline=[(xx*rng.uniform(.96,1.035),yy*rng.uniform(.96,1.035)) for xx,yy in outline]
   ob=fittedstone('Worn_roof_slab_%02d_%02d'%(i,j),rel(x+sw/2,y,1.965+rng.uniform(-.009,.009)),outline,.092+rng.uniform(-.010,.018),col,True)
   ob.rotation_euler.z=rng.uniform(-.065,.065)
   x+=sw*.995;i+=1
 # Pale rounded parapet makes the reference's clean house silhouette.
 for y in [south-.02,north+.02]:
  count=round(w/.5)
  for i in range(count):softstone('Roof_edge',rel(xmin+(i+.5)*(xmax-xmin)/count,y+rng.uniform(-.025,.025),1.98+rng.uniform(-.035,.035)),((xmax-xmin)/count*rng.uniform(.96,1.13),rng.uniform(.45,.53),rng.uniform(.29,.37)),rng.choice(['stone_light','stone','stone_light','stone_warm']),rng,rot=rng.uniform(-.10,.10))
 for x in [xmin-.02,xmax+.02]:
  count=round(h/.5)
  for i in range(count):softstone('Roof_edge',rel(x+rng.uniform(-.025,.025),south+(i+.5)*(north-south)/count,1.98+rng.uniform(-.035,.035)),(rng.uniform(.45,.53),(north-south)/count*rng.uniform(.96,1.13),rng.uniform(.29,.37)),rng.choice(['stone_light','stone','stone_light','stone_warm']),rng,rot=rng.uniform(-.10,.10))
 if bi in (0,4):
  chx=cx+.45;chy=cy+.25
  cylinder('Chimney_void',rel(chx,chy,2.04),.30,.1,'face_shadow',20)
  torus('Chimney_coping',rel(chx,chy,2.17),.31,.105,'stone_light',20,6)
  torus('Chimney_base',rel(chx,chy,2.04),.36,.08,'stone',20,5)
 elif bi==1:
  torus('Roof_vent_rim',rel(cx+w*.25,cy+h*.17,2.04),.31,.055,'wood',16,5)
  for a in [0,math.pi/3,2*math.pi/3]:beam('Vent_spoke',rel(cx+w*.25+.27*math.cos(a),cy+h*.17+.27*math.sin(a),2.05),rel(cx+w*.25-.27*math.cos(a),cy+h*.17-.27*math.sin(a),2.05),.025,'wood_end',5)
 # Native inn roof gains the reference's two low stone dividers, under the same removable roof owner.
 if bi==1:
  divider_y=south+h*.50
  for i in range(max(2,round((w-1)/.43))):
   xx=xmin+.48+i*.43
   softstone('Inn_roof_divider_horizontal',rel(xx,divider_y,2.0),(.45,.26,.20),'stone' if i%3 else 'roof_stone',rng,rot=rng.uniform(-.07,.07))
  for i in range(max(2,round((h*.50-.6)/.42))):
   yy=south+.45+i*.42
   softstone('Inn_roof_divider_vertical',rel(cx+.4,yy,2.0),(.27,.45,.20),'stone',rng,rot=rng.uniform(-.07,.07))
 for i in range(19):
  x=rng.uniform(xmin+.45,xmax-.45);y=rng.uniform(south+.45,north-.45);length=rng.uniform(.13,.31);ang=rng.uniform(0,math.tau)
  pts=[rel(x,y,2.015)]
  for k in range(9):
   a=k*math.tau/9;rr=rng.uniform(.65,1.15);xx=length*math.cos(a)*rr;yy=.065*math.sin(a)*rr
   pts.append(rel(x+xx*math.cos(ang)-yy*math.sin(ang),y+xx*math.sin(ang)+yy*math.cos(ang),2.018))
  newmesh('Thin_joint_moss_%02d'%i,pts,[(0,k+1,(k+1)%9+1) for k in range(9)],'roof_moss_light' if i%3 else 'moss')

model('oak-door','door','left-hinge-ground')
for i in range(5):box('Door_plank_%d'%i,(.09+i*.18,0,.83),(.176,.14,1.66),'wood' if i%2 else 'wood_light',.026)
for z in [.32,1.27]:box('Door_iron_strap',(.42,-.081,z),(.82,.032,.075),'iron',.012)
for z in [.22,1.38]:cylinder('Hinge',(0,0,z),.055,.25,'iron',8)
torus('Door_ring_handle',(.73,-.14,.86),.075,.014,'iron_light',12,4).rotation_euler[0]=math.pi/2

# Character base: exaggerated hood/head for legibility from directly above.
# Rigid component weighting is intentional; five short generic clips animate
# the skeleton without root translation or a second movement simulation.
BONE_DEFS=[('Root',(0,0,0),(0,0,.25),None),('Spine',(0,0,.42),(0,0,1.1),'Root'),
 ('Head',(0,0,1.1),(0,0,1.65),'Spine'),('Arm.L',(-.31,0,.98),(-.45,0,.55),'Spine'),
 ('Arm.R',(.31,0,.98),(.45,0,.55),'Spine'),('Leg.L',(-.16,0,.50),(-.16,0,.08),'Root'),
 ('Leg.R',(.16,0,.50),(.16,0,.08),'Root'),('Hand.L',(-.45,0,.55),(-.45,0,.42),'Arm.L'),
 ('Hand.R',(.45,0,.55),(.45,0,.42),'Arm.R')]

def character(color,mid):
 coll=model(mid,'character','feet-root');parts={}
 def part(ob,bone):parts[ob.name]=bone;return ob
 part(cylinder('Coat',(0,0,.69),.39,.83,color,16,radius2=.28),'Spine')
 part(torus('Coat_hem',(0,0,.285),.39,.035,'leather',16,4),'Spine')
 part(torus('Leather_belt',(0,0,.69),.35,.035,'leather',16,4),'Spine')
 part(box('Belt_buckle',(0,-.35,.70),(.10,.05,.10),'gold',.014),'Spine')
 part(ellipsoid('Hood',(0,0,1.32),(.38,.34,.39),color,3),'Head')
 part(ellipsoid('Hood_point',(0,.21,1.55),(.17,.24,.14),color,1),'Head')
 part(ellipsoid('Hood_dark_opening',(0,-.291,1.31),(.225,.045,.23),'face_shadow',2),'Head')
 part(ellipsoid('Face',(0,-.32,1.25),(.135,.035,.12),'skin',1),'Head')
 for s,label in [(-1,'L'),(1,'R')]:
  part(beam('Sleeve.'+label,(s*.25,0,1.02),(s*.44,0,.58),.14,color,10),'Arm.'+label)
  part(ellipsoid('Hand.'+label,(s*.46,0,.51),(.115,.12,.13),'skin',1),'Hand.'+label)
  part(cylinder('Trouser.'+label,(s*.17,0,.27),.13,.30,'leather',10),'Leg.'+label)
  part(ellipsoid('Boot.'+label,(s*.17,-.075,.09),(.14,.22,.11),'wood_dark',1),'Leg.'+label)
 arm=bpy.data.armatures.new(mid+'_Skeleton');rig=bpy.data.objects.new('VillageRig',arm);SCENE.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for name,head,tail,parent in BONE_DEFS:
  b=arm.edit_bones.new(name);b.head=head;b.tail=tail
  if parent:b.parent=arm.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
 for collection in list(rig.users_collection):collection.objects.unlink(rig)
 coll.objects.link(rig)
 for ob in list(coll.objects):
  if ob.type!='MESH':continue
  group=ob.vertex_groups.new(name=parts[ob.name]);group.add(list(range(len(ob.data.vertices))),1,'REPLACE')
  mod=ob.modifiers.new('VillageRig','ARMATURE');mod.object=rig;ob.parent=rig
 for name,bone,loc in [('Equipment.Head','Head',(0,0,1.68)),('Equipment.Hand.L','Hand.L',(-.46,-.02,.46)),('Equipment.Hand.R','Hand.R',(.46,-.02,.46)),('Equipment.Back','Spine',(0,.27,.95))]:
  sock=bpy.data.objects.new(mid+'__'+name,None);sock['socketName']=name;coll.objects.link(sock);sock.parent=rig;sock.parent_type='BONE';sock.parent_bone=bone
  # Bone parenting translates from bone tail, so world bind-space matrix is explicit.
  sock.matrix_parent_inverse=(rig.matrix_world @ rig.pose.bones[bone].matrix @ Matrix.Translation((0,rig.data.bones[bone].length,0))).inverted();sock.matrix_basis=Matrix.Translation(loc);sock['socketBone']=bone
 rig.animation_data_create()
 for clip in ['Idle','Walk','Interact','Attack','Hit']:
  for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
  action=bpy.data.actions.new(mid+'__'+clip);rig.animation_data.action=action
  length=48 if clip=='Idle' else 24
  for frame in [0,length//4,length//2,3*length//4,length]:
   phase=frame/length*math.tau
   for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
   if clip=='Idle':rig.pose.bones['Head'].rotation_euler[1]=.025*math.sin(phase);rig.pose.bones['Spine'].rotation_euler[0]=.025*math.sin(phase)
   elif clip=='Walk':
    for side,sign in [('L',1),('R',-1)]:rig.pose.bones['Leg.'+side].rotation_euler[0]=.36*sign*math.sin(phase);rig.pose.bones['Arm.'+side].rotation_euler[0]=-.28*sign*math.sin(phase)
   elif clip=='Interact':rig.pose.bones['Spine'].rotation_euler[0]=-.16*math.sin(frame/length*math.pi);rig.pose.bones['Arm.R'].rotation_euler[0]=-.65*math.sin(frame/length*math.pi)
   elif clip=='Attack':rig.pose.bones['Arm.R'].rotation_euler[0]=-1.15*math.sin(frame/length*math.pi);rig.pose.bones['Spine'].rotation_euler[2]=.23*math.sin(phase)
   elif clip=='Hit':rig.pose.bones['Spine'].rotation_euler[0]=.24*math.sin(frame/length*math.pi);rig.pose.bones['Head'].rotation_euler[0]=-.14*math.sin(frame/length*math.pi)
   for p in rig.pose.bones:p.keyframe_insert(data_path='rotation_euler',frame=frame,group=p.name)
  track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,0,action);strip.name=clip;track.mute=True
 rig.animation_data.action=None
 for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
 MODELS[mid]['rigged']=True;MODELS[mid]['clips']=['Idle','Walk','Interact','Attack','Hit'];MODELS[mid]['sockets']=['Equipment.Head','Equipment.Hand.L','Equipment.Hand.R','Equipment.Back']
for color in ['teal','gold','violet','olive']:character('hood_'+color,'character-'+color)

# Small sockets can receive these shared equipment meshes through native loadout.
model('equipment-blade','equipment','grip')
box('Sword_blade',(0,0,.44),(.09,.045,.72),'steel',.014);box('Crossguard',(0,0,.08),(.32,.08,.055),'gold',.012);cylinder('Leather_grip',(0,0,-.065),.04,.23,'leather',8)
model('equipment-staff','equipment','grip')
cylinder('Staff_shaft',(0,0,.18),.035,1.55,'wood',10);ellipsoid('Staff_knotted_head',(0,0,.99),(.10,.10,.16),'wood_light',1)
for z in [-.07,.015,.10]:torus('Staff_grip_binding',(0,0,z),.037,.013,'rope',8,4)
model('equipment-shield','equipment','grip')
cylinder('Buckler',(0,0,0),.29,.065,'wood',12);torus('Steel_rim',(0,0,.04),.29,.025,'iron',12,4);ellipsoid('Boss',(0,0,.075),(.09,.09,.055),'iron_light',1)

model('equipment-club','equipment','grip')
cylinder('Club_grip',(0,0,.05),.055,.54,'wood',10);ellipsoid('Club_head',(0,0,.46),(.14,.13,.24),'wood_light',2)
for z in [.31,.50]:torus('Club_iron_band',(0,0,z),.13,.020,'iron',10,4)
model('equipment-pack','equipment','back-attachment')
box('Leather_pack',(0,.07,-.18),(.48,.23,.57),'leather',.065);box('Pack_flap',(0,.19,.03),(.46,.045,.19),'wood_light',.045)
for x in [-.15,.15]:box('Pack_strap',(x,.21,-.14),(.047,.032,.45),'wood_dark',.009)
box('Pack_buckle',(0,.221,-.045),(.085,.025,.075),'gold',.009)
model('equipment-helmet','equipment','head-attachment')
ellipsoid('Iron_cap',(0,0,-.055),(.39,.355,.22),'iron_light',2);torus('Helmet_rim',(0,0,-.09),.35,.038,'iron',12,4)
box('Helmet_crown',(0,0,.105),(.065,.51,.045),'steel',.016)

def v3(x,y,z):return {'x':round(x,6),'y':round(y,6),'z':round(z,6)}
def unity(blender):return v3(blender[0],blender[2],blender[1])

def place_owner(owner,mid,offset=(0,0,0),angle=0,scale=(1,1,1)):
 ax=owner['anchorX']+.5;ay=25-owner['anchorY']-.5
 pos=(ax+offset[0],ay+offset[1],offset[2]); ob=instance(mid,pos,angle,scale,owner['id'],owner['kind'])
 PLACEMENTS.append({'ownerId':owner['id'],'modelId':mid,'kind':owner['kind'],'roomId':owner['roomId'] or '',
  'anchorX':owner['anchorX'],'anchorY':owner['anchorY'],'position':unity(pos),'visualOffset':unity(offset),
  'rotationY':-math.degrees(angle),'scale':unity(scale),'visibleWhen':ART_OWNERS[owner['id']].get('visibleWhen') or 'owner-visible',
  'mutable':owner['mutable'],'footprint':owner['footprint']})
 return ob

npc_colors={'north-guard-west':'gold','north-guard-east':'violet','western-shopkeeper':'gold','east-robed-resident':'violet','southwest-craftsperson':'teal','southern-food-vendor':'violet'}
for index,o in enumerate(NATIVE['owners']):
 oid=o['id'];kind=o['kind'];mid=None;off=(0,0,0);scale=(1,1,1);angle=0
 if kind in ('building-shell','roof'):mid=oid
 elif kind=='door':mid='oak-door';off=(-.45,0,0)
 elif kind=='npc':mid='character-'+npc_colors.get(oid,'olive')
 elif kind=='cistern':mid='central-well';off=(.5,2,0)
 elif kind=='gate':mid='oath-arch';off=(0,1,0)
 elif kind=='shop-stall':
  mid='market-stall-'+('0' if oid=='provisioners-stall' else '1')
  off=(-.5,2.5,0) if oid=='provisioners-stall' else (.5,2,0);scale=(1.06,1.10,1)
 elif kind=='bridge':mid='footbridge'
 elif kind=='cart':mid='handcart';off=(-.5,1.5,0);angle=.3
 elif kind=='oven':mid='bread-oven';off=(.5,1,0)
 elif kind=='planter':mid='herb-pot' if 'herb-pot' in oid else ('planter-1' if oid=='north-flower-planter' else 'planter-0')
 elif kind=='bowl':mid='bowl'
 elif kind in ('salvage','rope'):mid='rope-coil'
 elif kind=='creature':mid='frog' if 'frog' in oid else 'tortoise'
 elif kind=='bench':mid='bench'
 elif kind=='bed':mid='bed-'+str(index%2)
 elif kind=='bookshelf':mid='bookshelf';angle=math.pi/2 if 'west' in oid else 0
 elif kind=='stool':mid='stool'
 elif kind in ('table','workbench'):mid='table-'+('2' if kind=='workbench' else '1' if 'desk' in oid else '0')
 elif kind=='hearth':mid='hearth-0'
 elif kind=='workstation':mid='hearth-1' if 'stove' in oid else 'chopping-block' if 'chopping' in oid else 'table-2'
 elif kind=='container':
  if 'sideways' in oid:mid='barrel-side';angle=.4
  elif 'bucket' in oid:mid='bucket-'+str(index%2)
  elif 'crate' in oid or 'chest' in oid:mid='crate-'+str(index%4)
  elif 'basket' in oid:mid='bucket-0'
  else:mid='barrel-'+str(index%4)
 if mid is None:raise ValueError('Unmapped native owner '+oid+' '+kind)
 if o['roomId'] and kind not in ('building-shell','roof','door'):
  if kind=='bed':scale=(1,.53,.82)
  elif kind in ('table','workbench'):scale=(.53 if kind=='workbench' else .66,1,.85)
  elif kind=='bench':scale=(.59,1,.88)
  elif kind=='bookshelf':scale=(.65,1,.90)
  elif kind=='hearth':scale=(.72,.9,.9)
 place_owner(o,mid,off,angle,scale)
# Preview player only: this is NOT one of the persistent71 native owners.
player=instance('character-teal',(39.6,1.75,0),owner='SHOWCASE-player',role='showcase-only')

# Model source stamping retains reusable input meshes in editable collections,
# and export later consolidates copies into a cheap patch mesh.
def stamp(mid,loc=(0,0,0),angle=0,scale=(1,1,1)):
 transform=Matrix.Translation(loc)@Matrix.Rotation(angle,4,'Z')@Matrix.Diagonal((*scale,1))
 for source in MODELS[mid]['collection'].objects:
  if source.type!='MESH':continue
  ob=source.copy();ob.data=source.data;ob.parent=None;ob.matrix_world=transform@source.matrix_world;CURRENT.objects.link(ob)

for v in range(4):
 model('ground-patch-%d'%v,'terrain')
 box('Ground',(0,0,-.09),(10,5,.16),'earth',0)
 rng=random.Random(SEED+600+v)
 for i in range(24):
  # Low broad faceted color patches interrupt the base without tile seams.
  x=rng.uniform(-4.9,4.9);y=rng.uniform(-2.4,2.4);r=rng.uniform(.30,.66)
  verts=[(x,y,-.002)]
  for k in range(10):
   a=k*math.tau/10;rr=r*rng.uniform(.7,1.25);verts.append((x+rr*math.cos(a),y+rr*.7*math.sin(a),-.002))
  newmesh('Flat_ground_moss_%02d'%i,verts,[(0,k+1,(k+1)%10+1) for k in range(10)],['moss','earth','earth'][i%3])
 # Hundreds of tiny ground flecks form an exportable texture in geometry.
 for i in range(260):
  x=rng.uniform(-4.98,4.98);y=rng.uniform(-2.48,2.48);r=rng.uniform(.012,.037);a=rng.uniform(0,math.tau)
  verts=[(x+r*math.cos(a+k*math.tau/3),y+r*math.sin(a+k*math.tau/3),.001) for k in range(3)]
  newmesh('Ground_grain_%03d'%i,verts,[(0,1,2)],['grass','grass_light','moss','pebble','earth'][i%5])

rooms=[]
for b in NATIVE['buildings']:
 xs=[p['x'] for p in b['interior']];ys=[p['y'] for p in b['interior']]
 rooms.append((min(xs)-.7,max(xs)+1.7,25-max(ys)-1.7,25-min(ys)+.7))
watercells={(c['x'],c['y']) for c in NATIVE['cells'] if c['water']}
solidcells={(c['x'],c['y']) for c in NATIVE['cells'] if c['solid']}
# Authorized new-game farming verge. Native runtime owns Plantable and crops;
# this is bare soil only, retained in the ordinary terrain-detail mesh.
STARTER_GARDEN={(x,y) for x in (41,42) for y in (21,22,23)}

def blocked(x,y,margin=.12):
 for x0,x1,y0,y1 in rooms:
  if x0-margin<x<x1+margin and y0-margin<y<y1+margin:return True
 for p in PLACEMENTS:
  if p['kind'] in ('roof','building-shell','door','npc','creature'):continue
  if (x-(p['anchorX']+.5))**2+(y-(25-p['anchorY']-.5))**2<.55:return True
 if (x-41)**2+(y-12.5)**2<2.1**2:return True
 return False

def lane_x(y):return 40+.4*math.sin(y*.44)
def segment_distance(x,y,a,b):
 p=Vector((x,y));aa=Vector(a);bb=Vector(b);delta=bb-aa;t=max(0,min(1,(p-aa).dot(delta)/delta.length_squared));return(p-aa-t*delta).length
branches=[((21.25,10.2),(37.7,10.8)),((40,16.8),(49.5,17.5)),((40,5.1),(48.8,3.4)),((31.5,3.5),(39.7,3.5)),((34.5,18.5),(39.8,18.5))]
def pathness(x,y):
 r=math.hypot(x-41,y-12.5)
 if abs(r-2.85)<.69:return True
 if abs(x-lane_x(y))<.83 and r>2.05:return True
 return any(segment_distance(x,y,a,b)<.64 for a,b in branches)

# All paving candidates share a world-space neighborhood, so patch borders do
# not restart a lattice. A deterministic variable-spacing dart set is clipped
# into softly rounded fitted polygons, then allocated to the existing patches.
def clip_polygon(poly,nx,ny,limit):
 out=[]
 for i,a in enumerate(poly):
  b=poly[(i+1)%len(poly)];da=a[0]*nx+a[1]*ny-limit;db=b[0]*nx+b[1]*ny-limit
  if da<=0:out.append(a)
  if (da<0)!=(db<0):
   t=da/(da-db);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
 return out
pave_rng=random.Random(SEED+6060);pave_grid={};pave_points=[];pave_step=.45
for attempt in range(53000):
 x=pave_rng.uniform(20.8,59.2);y=pave_rng.uniform(-.4,25.4);rad=pave_rng.uniform(.095,.165)
 key=(math.floor(x/pave_step),math.floor(y/pave_step));near=[]
 for xx in range(key[0]-1,key[0]+2):
  for yy in range(key[1]-1,key[1]+2):near.extend(pave_grid.get((xx,yy),[]))
 if any((x-a[0])**2+(y-a[1])**2<((rad+a[2])*.87)**2 for a in near):continue
 pt=(x,y,rad);pave_points.append(pt);pave_grid.setdefault(key,[]).append(pt)
PAVING={}
for x,y,rad in pave_points:
 edge_jitter=.04*math.sin(x*4.9+y*2.1)+.055*math.sin(y*5.3-x*3)
 if (int(x),int(25-y)) in STARTER_GARDEN:continue
 if x<21.25 or x>58.75 or y<0 or y>=25 or not pathness(x+edge_jitter,y+edge_jitter) or blocked(x,y,.03) or (int(x),int(25-y)) in watercells:continue
 key=(math.floor(x/pave_step),math.floor(y/pave_step));poly=[(-.38,-.38),(.38,-.38),(.38,.38),(-.38,.38)]
 neighbors=[]
 for xx in range(key[0]-2,key[0]+3):
  for yy in range(key[1]-2,key[1]+3):neighbors.extend(pave_grid.get((xx,yy),[]))
 neighbors.sort(key=lambda p:(x-p[0])**2+(y-p[1])**2)
 for qx,qy,qr in neighbors:
  dx=qx-x;dy=qy-y
  if abs(dx)+abs(dy)<1e-7:continue
  poly=clip_polygon(poly,dx,dy,(dx*dx+dy*dy)*.5)
  if not poly:break
 if len(poly)<3:continue
 radii=[];n=8;phase=pave_rng.uniform(0,math.tau)
 for i in range(n):
  a=phase+i*math.tau/n;dx=math.cos(a);dy=math.sin(a);radius=.6
  for j,pa in enumerate(poly):
   pb=poly[(j+1)%len(poly)];nx=pb[1]-pa[1];ny=pa[0]-pb[0];denom=nx*dx+ny*dy
   if denom>1e-8:radius=min(radius,(nx*pa[0]+ny*pa[1])/denom)
  radii.append(radius)
 outline=[]
 for i in range(n):
  a=phase+i*math.tau/n;r=(radii[i]*.80+radii[(i-1)%n]*.10+radii[(i+1)%n]*.10)*.92
  outline.append((r*math.cos(a),r*math.sin(a)))
 PAVING.setdefault((int(x)//10,int(y)//5),[]).append((x,y,outline,pave_rng.choice(['cobble','cobble','cobble_light','stone_warm','cobble_dark'])))

# Native creek cell mask is rendered honestly; its source-image ambiguity is
# recorded in the layout ledger rather than replacing simulation coordinates.
for gy in range(5):
 for gx in range(8):
  cx=gx*10+5;cy=gy*5+2.5;mid='ground-patch-'+str((gx+2*gy)%4)
  ob=instance(mid,(cx,cy,0),owner='ground-%02d-%02d'%(gx,gy),role='terrain')
  STATIC.append({'id':ob.name,'modelId':mid,'position':unity((cx,cy,0)),'rotationY':0,'scale':v3(1,1,1),'role':'terrain'})
  decoid='detail-patch-%02d-%02d'%(gx,gy);model(decoid,'terrain-detail')
  rng=random.Random(SEED+gx*983+gy*7211)
  # Fitted stones are shared-world candidates, allocated without patch-edge seams.
  for i,(x,y,outline,col) in enumerate(PAVING.get((gx,gy),[])):
   fittedstone('Fitted_path_pebble_%04d'%i,(x-cx,y-cy,.038),outline,rng.uniform(.095,.15),col)


  # Sparse fringe outside art region; rich, deliberately grouped vegetation within.
  n=95 if 20<=cx<=60 else 16
  for i in range(n):
   x=rng.uniform(gx*10+.2,(gx+1)*10-.2);y=rng.uniform(gy*5+.2,(gy+1)*5-.2)
   if blocked(x,y,.18) or pathness(x,y) or (int(x),int(25-y)) in watercells or (int(x),int(25-y)) in STARTER_GARDEN:continue
   # Native solid cells are not cleared or replaced; scenery is decoration only.
   if i%19==0 and (x<27 or x>54 or y<1 or y>24):stamp('rocks-'+str(i%4),(x-cx,y-cy,0),rng.uniform(0,math.tau),(.8,.8,.8))
   elif i%31==0 and (x<27 or x>54):stamp('shrub-'+str(i%4),(x-cx,y-cy,0),rng.uniform(0,math.tau),(.7,.7,.7))
   else:
    stamp('grass-'+str(i%4),(x-cx,y-cy,0),rng.uniform(0,math.tau),(rng.uniform(.5,.95),rng.uniform(.5,.95),.8))
    if i%5==0:ellipsoid('Scattered_stone',(x-cx+.18,y-cy,.06),(.095,.075,.058),'pebble',1)
  for native_x,native_y in sorted(STARTER_GARDEN):
   x=native_x+.5;y=25-native_y-.5
   if not (gx*10<=x<(gx+1)*10 and gy*5<=y<(gy+1)*5):continue
   # Each inset patch exposes the exact plantable centre, with no fake planted crop.
   softstone('Plantable_soil_%d_%d'%(native_x,native_y),(x-cx,y-cy,.008),(.87,.88,.035),'soil',rng,rot=rng.uniform(-.014,.014))
   for row in [-.23,0,.23]:
    pts=[(x-cx-.34,y-cy+row-.025,.033),(x-cx+.34,y-cy+row-.02,.033),(x-cx+.33,y-cy+row+.027,.038),(x-cx-.32,y-cy+row+.021,.037)]
    newmesh('Prepared_earth_furrow',pts,[(0,1,2,3)],'tile_earth')
   for k in range(9):
    a=k*2.4;rr=.32*math.sqrt((k+.5)/9)
    ellipsoid('Small_soil_clod',(x-cx+rr*math.cos(a),y-cy+rr*math.sin(a),.034),(.027,.025,.012),'tile_earth' if k%3 else 'soil',1)
  for fi,(fx,fy,flowercol) in enumerate([(26.4,12.8,'flower_pink'),(57.0,6.4,'flower_pink'),(46.0,1.1,'flower_pink'),(22.6,9.9,'flower_yellow'),(55.5,20.2,'flower_yellow'),(28.8,22.0,'flower_white')]):
   if not (gx*10<=fx<(gx+1)*10 and gy*5<=fy<(gy+1)*5):continue
   for k in range(24):
    a=k*2.4;rr=.60*math.sqrt((k+.5)/24);x=fx+rr*math.cos(a);y=fy+rr*.7*math.sin(a)
    if blocked(x,y,.08) or pathness(x,y):continue
    leafshape('Flower_group_leaf',(x-cx,y-cy,.055),.18,.065,a,'leaf',.025)
    ellipsoid('Flower_group_bloom',(x-cx,y-cy,.17),(.065,.058,.045),flowercol,1)
  for native_x,native_y in sorted(watercells):
   x=native_x+.5;y=25-native_y-.5
   if gx*10<=x<(gx+1)*10 and gy*5<=y<(gy+1)*5:
    ob=newmesh('Water_cell',[(x-cx-.5,y-cy-.5,.015),(x-cx+.5,y-cy-.5,.015),(x-cx+.5,y-cy+.5,.015),(x-cx-.5,y-cy+.5,.015)],[(0,1,2,3)],'water')
    ob.data.materials.clear();ob.data.materials.append(WATER);ob['keepSeparate']=True
    if (native_x+native_y)%3==0:beam('Creek_ripple',(x-cx-.22,y-cy,.025),(x-cx+.12,y-cy+.05,.025),.01,'water_light',4)
    for ex,ey in [(-1,0),(1,0),(0,-1),(0,1)]:
     if (native_x+ex,native_y-ey) in watercells:continue
     ellipsoid('Mossy_bank',(x-cx+ex*.48,y-cy+ey*.48,.028),(.37 if ex==0 else .19,.37 if ey==0 else .19,.04),'moss_dark',1)
     if (native_x+native_y)%2==0:ellipsoid('Shore_rock',(x-cx+ex*.40+.06,y-cy+ey*.40,.09),(.18,.15,.10),'stone_dark',1)
  if not CURRENT.objects:continue
  ob=instance(decoid,(cx,cy,0),owner=decoid,role='terrain-detail')
  STATIC.append({'id':decoid,'modelId':decoid,'position':unity((cx,cy,0)),'rotationY':0,'scale':v3(1,1,1),'role':'terrain-detail'})

# Reference composition accents. These do not spawn native owners or block paths.
# Separate clusters deliberately have3–4 variants rather than identical wallpaper.
ACCENTS=[('tree-0',22.6,21.3,1.15),('tree-1',23.1,3.7,1.05),('tree-2',56.7,3.9,.95),('tree-3',56.3,15.6,1.1),
 ('tree-1',54.8,20.6,.8),('shrub-0',24.1,23.9,.9),('shrub-2',57.1,23.1,1.0),('shrub-3',57.2,7.2,.9),
 ('rocks-0',24.0,24.1,1.3),('rocks-1',56.5,24.3,1.2),('rocks-2',23.1,18.8,1.2),('rocks-3',58.0,19.1,1.2),
 ('rocks-2',25.2,1.9,1.15),('rocks-1',56.1,1.2,1.1),('planter-0',25.9,17.2,1.0),('planter-2',26.0,6.6,1.0),
 ('planter-0',31.0,1.3,1.0),('planter-1',55.0,15.7,1.0)]
for i,(mid,x,y,s) in enumerate(ACCENTS):
 ob=instance(mid,(x,y,0),angle=math.pi/2 if mid.startswith('planter') and i%2==0 else 0,scale=(s,s,s),owner='accent-%02d'%i)
 STATIC.append({'id':ob.name,'modelId':mid,'position':unity((x,y,0)),'rotationY':-90 if mid.startswith('planter') and i%2==0 else 0,'scale':v3(s,s,s),'role':'decoration'})
# Fences follow boundary fragments; the arrival and exit lanes remain open.
FENCES=[(30.0,24.0,0),(32.4,24.0,0),(34.8,24.0,0),(44.2,23.2,-.45),(46.4,22.1,-.45),
 (54.9,22.3,.1),(56.1,10.1,-.75),(57.0,8.0,math.pi/2),(57.0,5.6,math.pi/2),
 (47.0,.70,0),(49.4,.7,0),(51.8,.7,0),(27.0,4.5,0)]
for i,(x,y,a) in enumerate(FENCES):
 mid='fence-'+str(i%4);ob=instance(mid,(x,y,0),a,owner='fence-placement-%02d'%i)
 STATIC.append({'id':ob.name,'modelId':mid,'position':unity((x,y,0)),'rotationY':-math.degrees(a),'scale':v3(1,1,1),'role':'decoration'})

# Export one geometry mesh per static model, with water a separate group. Original
# source pieces are never joined, preserving the fully editable Blender kit.
def export_collection(mid):
 # Direct static assembly reads evaluated world transforms, including newly placed parts.
 bpy.context.view_layer.update()
 info=MODELS[mid];coll=info['collection'];temps=[];remap={}
 root=bpy.data.objects.new(mid,None);SCENE.collection.objects.link(root);temps.append(root)
 # Unity FBX importer reverses both horizontal source axes. The export-only
 # parent rotates them once; authoring geometry and manifest stay east/north.
 root.rotation_euler[2]=math.pi
 if not info.get('rigged'):
  # Direct assembly avoids Blender's O(piece-count) context/depsgraph work for
  # each join operation. This preserves every source vertex, face, UV and tint.
  for water in [False,True]:
   group=[o for o in coll.objects if o.type=='MESH' and bool(o.get('keepSeparate',False))==water]
   if not group:continue
   verts=[];faces=[];uvs=[];colors=[];smooth=[];normals=[]
   for ob in group:
    mesh=ob.data;offset=len(verts);mat=ob.matrix_world
    verts.extend(tuple(mat@v.co) for v in mesh.vertices)
    faces.extend(tuple(offset+i for i in f.vertices) for f in mesh.polygons)
    smooth.extend(f.use_smooth for f in mesh.polygons)
    # Sculpted corner normals must survive static assembly and nonuniform scale.
    normal_matrix=mat.to_3x3().inverted().transposed()
    normals.extend(tuple((normal_matrix@n.vector).normalized()) for n in mesh.corner_normals)
    uvs.extend(tuple(t.uv) for t in mesh.uv_layers[0].data)
    colors.extend(tuple(t.color) for t in mesh.color_attributes[0].data)
   name=mid+('__Water' if water else '__Geometry');mesh=bpy.data.meshes.new(name+'Mesh');mesh.from_pydata(verts,[],faces);mesh.update()
   mesh.materials.append(WATER if water else MAT);layer=mesh.uv_layers.new(name='UVMap');layer.data.foreach_set('uv',[v for uv in uvs for v in uv])
   attr=mesh.color_attributes.new(name='VillageColor',type='BYTE_COLOR',domain='CORNER');attr.data.foreach_set('color',[v for color in colors for v in color])
   for f,value in zip(mesh.polygons,smooth):f.use_smooth=value
   mesh.normals_split_custom_set(normals)
   ob=bpy.data.objects.new(name,mesh);SCENE.collection.objects.link(ob);ob.parent=root;temps.append(ob)
  for ob in bpy.context.selected_objects:ob.select_set(False)
  for ob in temps:ob.select_set(True)
  bpy.context.view_layer.objects.active=root
  bpy.ops.export_scene.fbx(filepath=str(OUT/'models'/f'{mid}.fbx'),use_selection=True,object_types={'EMPTY','MESH'},
   axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1.0,
   bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,
   bake_anim=False,path_mode='RELATIVE',use_custom_props=True)
  for ob in temps:bpy.data.objects.remove(ob,do_unlink=True)
  return
 for source in coll.objects:
  ob=source.copy()
  if source.data:ob.data=source.data.copy() if source.type in ('MESH','ARMATURE') else source.data
  SCENE.collection.objects.link(ob);temps.append(ob);remap[source]=ob
  if ob.type=='ARMATURE':ob.name=mid+'__Rig'
  if ob.get('socketName'):ob.name=ob['socketName']
 for source,ob in remap.items():
  ob.parent=remap.get(source.parent,root)
  ob.matrix_parent_inverse=source.matrix_parent_inverse.copy() if source.parent else Matrix.Identity(4)
  for mod in ob.modifiers:
   if mod.type=='ARMATURE':mod.object=remap.get(mod.object,mod.object)
 rigged=bool(info.get('rigged'))
 if rigged:
  rig=next(o for o in temps if o.type=='ARMATURE')
  for track in rig.animation_data.nla_tracks:track.mute=False
 for water in [False,True]:
  group=[o for o in temps if o.type=='MESH' and bool(o.get('keepSeparate',False))==water]
  if not group:continue
  for ob in bpy.context.selected_objects:ob.select_set(False)
  for ob in group:ob.select_set(True)
  bpy.context.view_layer.objects.active=group[0]
  if len(group)>1:bpy.ops.object.join()
  joined=group[0];joined.name=mid+('__Water' if water else '__Geometry')
  # Joined duplicate material slots are canonicalized by Blender, preserving the
  # palette atlas; only one palette slot is needed per joined geometry.
  if len(joined.data.materials)>1 and all(m==joined.data.materials[0] for m in joined.data.materials):
   mat=joined.data.materials[0];joined.data.materials.clear();joined.data.materials.append(mat)
   for p in joined.data.polygons:p.material_index=0
  temps=[o for o in temps if o==joined or o not in group]
 for ob in bpy.context.selected_objects:ob.select_set(False)
 for ob in temps:ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/'models'/f'{mid}.fbx'),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},
  axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1.0,
  bake_space_transform=not rigged,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,
  bake_anim=rigged,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=rigged,bake_anim_use_all_actions=False,
  bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,bake_anim_step=1,path_mode='RELATIVE',use_custom_props=True)
 # Object joining invalidates references to removed children; the filtered list
 # above contains only living objects after each join.
 for ob in temps:
  if ob.name in bpy.data.objects:bpy.data.objects.remove(ob,do_unlink=True)

SCENE.render.fps=24
# The first model must measure its placed pieces, not stale identity matrices.
bpy.context.view_layer.update()
model_rows=[]
for mid,info in MODELS.items():
 # The standalone probe retains named markers for the accepted Unity tests.
 # It is diagnostic evidence, not a runtime library model.
 if info['kind']=='validation':continue
 coll=info['collection'];points=[];triangles=0;meshcount=0
 for ob in coll.objects:
  if ob.type!='MESH':continue
  meshcount+=1;ob.data.calc_loop_triangles();triangles+=len(ob.data.loop_triangles)
  for vertex in ob.data.vertices:points.append(ob.matrix_world@vertex.co)
 if not points:continue
 mins=[min(p[i] for p in points) for i in range(3)];maxs=[max(p[i] for p in points) for i in range(3)]
 if not all(math.isfinite(v) for v in mins+maxs):raise ValueError('Invalid model bounds '+mid)
 centre=[(a+b)/2 for a,b in zip(mins,maxs)];size=[b-a for a,b in zip(mins,maxs)]
 model_rows.append({'id':mid,'path':'models/'+mid+'.fbx','kind':info['kind'],'pivot':info['pivot'],'triangles':triangles,
  'boundsCenter':unity(centre),'boundsSize':unity(size),'rigged':bool(info.get('rigged')),
  'clips':info.get('clips',[]),'sockets':info.get('sockets',[])})
 if not args.skip_export:export_collection(mid)

manifest={'schemaVersion':1,'id':'morrowfast-village3d','artSeed':SEED,'coordinates':'Unity X east, Y height, Z north; one unit per cell',
 'zoneWidth':80,'zoneHeight':25,'artOriginX':21.25,'artWidth':37.5,'paletteTexture':'textures/VillagePalette.png',
 'models':model_rows,'owners':PLACEMENTS,'staticPlacements':STATIC,
 'starterGardenCells':[{'x':x,'y':y} for x,y in sorted(STARTER_GARDEN)],
 'buildings':[{'id':b['id'],'shellOwnerId':b['id']+'-shell','roofOwnerId':b['roofId'],'doorOwnerId':b['doorId']} for b in NATIVE['buildings']],
 'materials':[{'id':'VillagePalette','baseColorTexture':'textures/VillagePalette.png','roughness':.88,'metallic':0,'opaque':True},
 {'id':'VillageWater','baseColorTexture':'','baseColor':{'r':.09,'g':.21,'b':.23,'a':1},'roughness':.43,'metallic':.05,'opaque':True}]}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
assert len(PLACEMENTS)==71 and len({p['ownerId'] for p in PLACEMENTS})==71
assert {p['ownerId'] for p in PLACEMENTS}==set(OWNERS)
assert len(manifest['buildings'])==5
models_by={m['id']:m for m in model_rows}
placed_triangles=sum(models_by[p['modelId']]['triangles'] for p in PLACEMENTS+STATIC)
report={'status':'Blender source/export validation; Unity import/runtime unverified','artSeed':SEED,'nativeOwners':len(PLACEMENTS),
 'buildings':5,'models':len(model_rows),'staticPlacements':len(STATIC),'wholeZonePlacedTriangles':placed_triangles,
 'paletteSwatches':64,'rigClips':['Idle','Walk','Interact','Attack','Hit'],'sourceDefinitionSha256':hashlib.sha256((SOURCE/'native-definition.json').read_bytes()).hexdigest(),
 'boundsValidated':True,'ownerIdsComplete':True,'groundCoverage':[0,0,80,25],'exportPreset':'static bake_space_transform=True; rig=False, +Z forward/Y up, export-only Z rotation 180 degrees',
 'knownLimits':['Unity must validate axes, materials, rig clips and sockets before acceptance','Native footprint/room layout takes priority over image ambiguities','Palette color/AO appearance requires actual Unity review','Procedural source has authored geometry and lighting, no photoreal texture bake','Showcase player is decorative preview only and omitted from owner manifest']}
(OUT/'reports/validation.json').write_text(json.dumps(report,indent=2)+'\n')

# Studio scene uses a straight-down orthographic camera and soft warm key light.
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes.get('Background').inputs[0].default_value=(.37,.40,.45,1);SCENE.world.node_tree.nodes.get('Background').inputs[1].default_value=.85
camdata=bpy.data.cameras.new('Village_Overhead');cam=bpy.data.objects.new('Village_Overhead',camdata);SCENE.collection.objects.link(cam)
cam.location=(40,12.5,50);cam.rotation_euler=(0,0,0);camdata.type='ORTHO';camdata.ortho_scale=37.5;SCENE.camera=cam
sun_data=bpy.data.lights.new('Warm_sun','SUN');sun_data.energy=2.05;sun_data.color=(1.0,.88,.69);sun_data.angle=math.radians(25);sun=bpy.data.objects.new('Warm_sun',sun_data);SCENE.collection.objects.link(sun);sun.rotation_euler=(math.radians(25),math.radians(-20),math.radians(-25))
area_data=bpy.data.lights.new('Soft_fill','AREA');area_data.energy=1000;area_data.shape='DISK';area_data.size=18
area=bpy.data.objects.new('Soft_fill',area_data);SCENE.collection.objects.link(area);area.location=(38,18,20)
SCENE.render.engine='CYCLES';SCENE.cycles.device='CPU';SCENE.cycles.samples=32;SCENE.cycles.use_denoising=True
SCENE.render.resolution_x=1200;SCENE.render.resolution_y=800;SCENE.render.resolution_percentage=100
SCENE.view_settings.view_transform='AgX';SCENE.view_settings.look='AgX - Medium High Contrast';SCENE.view_settings.exposure=.05
SCENE.render.image_settings.file_format='PNG';SCENE.render.film_transparent=False
SCENE.render.filepath=str(OUT/'renders/village_overhead.png')
# Source-kit collections are nested under an excluded view layer collection for
# discovery in Blender. Instances retain links to their editable originals.
library=bpy.data.collections.new('ASSET_LIBRARY_EDITABLE');SCENE.collection.children.link(library)
for info in MODELS.values():library.children.link(info['collection'])
SCENE.view_layers[0].layer_collection.children[library.name].exclude=True
ATLAS.pack();ATLAS.filepath='//textures/VillagePalette.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'village_master.blend'))
if args.render:
 bpy.ops.render.render(write_still=True)
 # One cutaway image proves authored room contents exist, without claiming Unity state bindings.
 for ob in SCENE.objects:
  if ob.get('role')=='roof':ob.hide_render=True
 SCENE.render.filepath=str(OUT/'renders/village_cutaway.png');bpy.ops.render.render(write_still=True)
 for ob in SCENE.objects:
  if ob.get('role')=='roof':ob.hide_render=False
print('VILLAGE3D_BUILD_COMPLETE',json.dumps(report))
