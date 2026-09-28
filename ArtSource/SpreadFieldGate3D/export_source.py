"""Private static source export/review, borrowing approved mesh helpers read-only."""
import bpy,bmesh,math,json,argparse,sys,os,zlib,struct,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for name in ('models','textures','renders'):(OUT/name).mkdir(parents=True,exist_ok=True)
kit=json.loads(Path(__file__).with_name('kit.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.fps=24
MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'borrowed_mesh_kit','exec'),globals())
def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
rgb=[tuple(int(h[i:i+2],16) for i in (1,3,5)) for h in kit['palette']]
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',24,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0'+bytes(v for c in rgb for v in c)))+chunk(b'IEND',b'')
(OUT/'textures/SpreadBiomePalette.png').write_bytes(png)
img=bpy.data.images.load(str(OUT/'textures/SpreadBiomePalette.png'));img.colorspace_settings.name='sRGB'
MAT=bpy.data.materials.new('BorrowedApprovedPalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest';shader=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=1
COLORS=[(str(i),tuple(v/255 for v in c)) for i,c in enumerate(rgb)];CINDEX={str(i):i for i in range(24)}
def color_mesh(mesh,color):
 idx=int(color);mesh.update();uv=mesh.uv_layers.new(name='UVMap')
 for q in uv.data:q.uv=((idx+.5)/24,.5)
 attr=mesh.color_attributes.new(name='ApprovedSourceColor',type='BYTE_COLOR',domain='CORNER')
 for q in attr.data:q.color=(*COLORS[idx][1],1)
 mesh.materials.append(MAT)
def xyz(p):return(p[0],p[2],p[1])
rows=[]
for desc in kit['models']:
 mid=desc['id'];coll=model(mid,'entity','ground-centre')
 for item in desc['boxes']:
  ob=box(item['part'],xyz(item['center']),xyz(item['size']),str(item['color']),bevel=0)
  for face in ob.data.polygons:face.use_smooth=False
 bpy.context.view_layer.update();export_collection(mid)
 verts=[ob.matrix_basis@v.co for ob in coll.objects if ob.type=='MESH' for v in ob.data.vertices]
 lo=[min(v[i] for v in verts) for i in range(3)];hi=[max(v[i] for v in verts) for i in range(3)]
 rows.append(dict(id=mid,sourceBlueprint=desc['blueprint'],path='models/'+mid+'.fbx',kind='entity',rigged=False,clips=[],sockets=[],triangles=sum(len(ob.data.polygons)*2 for ob in coll.objects if ob.type=='MESH'),boundsBlender=dict(min=lo,max=hi),sha256=hashlib.sha256((OUT/'models'/f'{mid}.fbx').read_bytes()).hexdigest(),status='private-source-not-native-adopted'))
(OUT/'catalog.json').write_text(json.dumps(dict(schemaVersion=1,id=kit['id'],palette=kit['palette'],models=rows),indent=2)+'\n')
# Review instances only. Context never enters the two exported collections.
for i,mid in enumerate(['spread-field-gate-closed','spread-field-gate-open']):instance(mid,loc=((i-.5)*2.6,0,0))
context=model('review-only-existing-hedge','context','ground-centre')
hedge=next(m for m in json.loads((ROOT/'ArtSource/SpreadEnvironment3D/kit.json').read_text())['models'] if m['id']=='spread-environment-hedge-0')
for i,item in enumerate(hedge['boxes']):
 c=item['center'];s=item['size'];box('review-hedge-'+str(i),(c['x'],c['z']-.25,c['y']),(s['x'],s['z'],s['y']),str(item['color']),bevel=0)
hedges=[]
for centre in [-1.3,1.3]:
 for sign in [-1,1]:hedges.append(instance('review-only-existing-hedge',loc=(centre+sign*.96,0,0)))
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.30,1);SCENE.world.node_tree.nodes['Background'].inputs[1].default_value=.8
floor=bpy.data.materials.new('ReviewGroundOnly');floor.use_nodes=True;floor.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.003,.025,.021,1);floor.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('ReviewKey','AREA');li.energy=550;li.size=4;ob=bpy.data.objects.new('ReviewKey',li);SCENE.collection.objects.link(ob);ob.location=(-2,-3,6);ob.rotation_euler=(Vector((0,0,.3))-ob.location).to_track_quat('-Z','Y').to_euler()
ca=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO';ca.ortho_scale=5.5
SCENE.render.engine='CYCLES';SCENE.cycles.samples=24;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_x=2000;SCENE.render.resolution_y=1000;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='Standard';SCENE.view_settings.look='Medium High Contrast'
def pose(name,loc,target):
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();SCENE.render.filepath=str(OUT/'renders'/name)
pose('field-gate-front-context.png',(0,-7,7),(0,0,.28))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'field-gate-source-review.blend'),compress=True)
if args.render:
 bpy.ops.render.render(write_still=True)
 pose('field-gate-oblique-context.png',(3.2,-7,6),(0,0,.28));bpy.ops.render.render(write_still=True)
 for ob in hedges:ob.hide_render=True
 ca.ortho_scale=4.2;pose('field-gate-two-states.png',(0,-7,5),(0,0,.34));bpy.ops.render.render(write_still=True)
print('FIELD_GATE_ORIGINAL_SOURCE',json.dumps(rows))
