"""Export original static grain and source-only review. No shared writes or gameplay objects."""
import bpy,bmesh,json,argparse,sys,os,zlib,struct,hashlib,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=a.output.resolve()
for d in ['models','textures','renders']:(OUT/d).mkdir(parents=True,exist_ok=True)
kit=json.loads(Path(__file__).with_name('kit.json').read_text());bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
rgb=[tuple(int(h[i:i+2],16) for i in (1,3,5)) for h in kit['palette']]
def chunk(t,d):return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',24,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0'+bytes(v for c in rgb for v in c)))+chunk(b'IEND',b'')
(OUT/'textures/SpreadBiomePalette.png').write_bytes(png)
img=bpy.data.images.load(str(OUT/'textures/SpreadBiomePalette.png'));img.colorspace_settings.name='sRGB'
mat=bpy.data.materials.new('ApprovedGladePalette');mat.use_nodes=True;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest';mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color']);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1
model=kit['models'][0];col=bpy.data.collections.new(model['id']);scene.collection.children.link(col)
for k in model['kernels']:
 mesh=bpy.data.meshes.new(k['part']);mesh.from_pydata([(v[0],v[2],v[1]) for v in k['vertices']],[],k['faces']);mesh.update()
 # Unity→Blender basis swaps handedness; recalculate outward normals on the closed piece.
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free();uv=mesh.uv_layers.new(name='UVMap')
 for poly in mesh.polygons:
  poly.use_smooth=False
  for li in poly.loop_indices:uv.data[li].uv=((k['faceColors'][poly.index]+.5)/24,.5)
 mesh.materials.append(mat);ob=bpy.data.objects.new(k['part'],mesh);col.objects.link(ob)
bpy.ops.object.select_all(action='DESELECT')
for ob in col.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=list(col.objects)[0]
path=OUT/'models'/f"{model['id']}.fbx"
bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',bake_anim=False,use_mesh_modifiers=True,add_leaf_bones=False,path_mode='RELATIVE')
row={'id':model['id'],'sourceBlueprint':model['blueprint'],'path':'models/'+path.name,'kind':'entity','rigged':False,'clips':[],'sockets':[],'triangles':sum(sum(len(f)-2 for f in k['faces']) for k in model['kernels']),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'status':'private original source; not native-adopted'}
(OUT/'catalog.json').write_text(json.dumps({'schemaVersion':1,'palette':kit['palette'],'models':[row]},indent=2)+'\n')
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.32,.29,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
floor=bpy.data.materials.new('ReviewOnlyGround');floor.use_nodes=True;floor.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.014,.053,.042,1);floor.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1;bpy.ops.mesh.primitive_plane_add(size=100,location=(0,0,-.002));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('ReviewKey','AREA');li.energy=220;li.size=3;ob=bpy.data.objects.new('ReviewKey',li);scene.collection.objects.link(ob);ob.location=(-1,-2,4);ob.rotation_euler=(Vector((0,0,.1))-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camdata);scene.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=1.1
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
cam.location=(.8,-1.4,1.6);cam.rotation_euler=(Vector((0,0,.045))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'renders/toasted-grain-oblique.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'toasted-grain-source-review.blend'),compress=True)
if a.render:
 bpy.ops.render.render(write_still=True);cam.location=(0,-.05,2);cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'renders/toasted-grain-top.png');bpy.ops.render.render(write_still=True)
print('TOASTED_GRAIN_SOURCE',json.dumps(row))
