"""Isolated Blender export of tested original Spread animal cuboids/rigs."""
import bpy,bmesh,math,json,argparse,sys,ast,os,zlib,struct,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT',str(Path(__file__).resolve().parents[2])))
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for name in ('models','textures','renders'):(OUT/name).mkdir(parents=True,exist_ok=True)
kit=json.loads(Path(__file__).with_name('actors.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.fps=24
MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'borrowed_mesh_kit','exec'),globals())
# Source swatches are exact sRGB. PNG writes explicit bytes, independent of any
# Blender scene/color-management setting. The native builder borrows the glade palette.
def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
rgb=[tuple(int(h[i:i+2],16)for i in (1,3,5))for h in kit['palette']]
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',24,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0'+bytes(v for c in rgb for v in c)))+chunk(b'IEND',b'')
(OUT/'textures/SpreadBiomePalette.png').write_bytes(png)
img=bpy.data.images.load(str(OUT/'textures/SpreadBiomePalette.png'));img.colorspace_settings.name='sRGB'
MAT=bpy.data.materials.new('SpreadBiomePalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest';shader=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=1;WATER=MAT
COLORS=[(str(i),tuple(v/255 for v in c))for i,c in enumerate(rgb)];CINDEX={str(i):i for i in range(24)}
def color_mesh(mesh,color):
 idx=int(color);mesh.update();uv=mesh.uv_layers.new(name='UVMap')
 for q in uv.data:q.uv=((idx+.5)/24,.5)
 attr=mesh.color_attributes.new(name='SpreadBiomeColor',type='BYTE_COLOR',domain='CORNER')
 for q in attr.data:q.color=(*COLORS[idx][1],1)
 mesh.materials.append(MAT)
tree=ast.parse((ROOT/'ArtSource/SpawnRing3D/build_ring.py').read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name=='rig_parts');exec(compile(ast.Module(body=[fn],type_ignores=[]),'borrowed_rig_parts','exec'),globals())
def xyz(p):return(p[0],p[2],p[1])
rows=[]
for desc in kit['models']:
 mid=desc['id'];coll=model(mid,'actor','feet-root');weights={}
 for item in desc['boxes']:
  ob=box(item['part'],xyz(item['center']),xyz(item['size']),str(item['color']),bevel=0);weights[ob.name]=item['bone']
 bones=[(b['name'],xyz(b['head']),xyz(b['tail']),b['parent'])for b in desc['bones']]
 rig_parts(mid,bones,weights,desc['rigFamily']);bpy.context.view_layer.update();export_collection(mid)
 rig=next(o for o in coll.objects if o.type=='ARMATURE')
 # Action metadata is read from created native Blender tracks, not source labels.
 clips=[t.strips[0].action.name.split('__')[-1]for t in rig.animation_data.nla_tracks]
 assert clips==['Idle','Walk','Interact','Attack','Hit']
 for ob in coll.objects:
  if ob.type=='MESH':assert len(ob.vertex_groups)==1 and len(ob.modifiers)==1
 verts=[ob.matrix_basis@v.co for ob in coll.objects if ob.type=='MESH'for v in ob.data.vertices]
 lo=[min(v[i]for v in verts)for i in range(3)];hi=[max(v[i]for v in verts)for i in range(3)]
 rows.append({'id':mid,'sourceBlueprint':desc['blueprint'],'path':'models/'+mid+'.fbx','rigFamily':desc['rigFamily'],'rigged':True,'clips':clips,'sockets':[], 'bones':[b.name for b in rig.data.bones],'triangles':sum(len(ob.data.polygons)*2 for ob in coll.objects if ob.type=='MESH'),'boundsBlender':{'min':lo,'max':hi},'sha256':hashlib.sha256((OUT/'models'/f'{mid}.fbx').read_bytes()).hexdigest(),'status':'private-source-not-native-adopted'})
(OUT/'catalog.json').write_text(json.dumps({'schemaVersion':1,'id':'spread-biome-animals','palette':kit['palette'],'models':rows},indent=2)+'\n')
for i,mid in enumerate(MODELS):instance(mid,loc=(i*1.5,0,0))
# Source review uses the same approximate approved viewing inclination but is
# explicitly not Unity lighting or a native animation/appearance acceptance.
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.30,1);SCENE.world.node_tree.nodes['Background'].inputs[1].default_value=.8
floor=bpy.data.materials.new('ReviewGround');floor.use_nodes=True;floor.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.003,.025,.021,1);floor.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('Key','AREA');li.energy=600;li.size=4;ob=bpy.data.objects.new('Key',li);SCENE.collection.objects.link(ob);ob.location=(-2,-3,6);ob.rotation_euler=(Vector((1.5,0,0))-ob.location).to_track_quat('-Z','Y').to_euler()
ca=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO';ca.ortho_scale=4.7;cam.location=(1.5,-6,8);cam.rotation_euler=(Vector((1.5,0,.1))-cam.location).to_track_quat('-Z','Y').to_euler()
SCENE.render.engine='CYCLES';SCENE.cycles.samples=24;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_x=1500;SCENE.render.resolution_y=750;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='Standard';SCENE.view_settings.look='Medium High Contrast';SCENE.render.filepath=str(OUT/'renders/animals-review.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'animals-review.blend'),compress=True)
if args.render:bpy.ops.render.render(write_still=True)
print('SPREAD_ANIMAL_SOURCE',json.dumps(rows))
