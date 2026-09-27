import bpy,json,math,argparse,sys
from pathlib import Path
from mathutils import Vector
here=Path(__file__).parent
source=json.loads((here/'worn-source.json').read_text())
parser=argparse.ArgumentParser();parser.add_argument('--repo',type=Path,default=here.parents[1]);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
palette=json.loads((args.repo/'ArtSource/SpreadPortable3D/portable-source.json').read_text())['palette']
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials=[]
for i,color in enumerate(palette):
 m=bpy.data.materials.new('Scoped_'+str(i));m.diffuse_color=(*[int(color[j:j+2],16)/255 for j in (1,3,5)],1);m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=m.diffuse_color;m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8;materials.append(m)
for index,model in enumerate(source['models']):
 ox=(index%4-1.5)*1.7;oy=(index//4-1)*1.9
 for box in model['boxes']:
  c=box['center'];sz=box['size'];bpy.ops.mesh.primitive_cube_add(size=1,location=(ox+c[0],oy+c[2],.75+c[1]));obj=bpy.context.object;obj.dimensions=(sz[0],sz[2],sz[1]);obj.data.materials.append(materials[box['paint']])
 bpy.ops.object.text_add(location=(ox-.64,oy-.7,.035));label=bpy.context.object;label.data.body=model['blueprint'];label.data.size=.14;label.data.extrude=0;label.data.materials.append(materials[17])
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.10));bpy.context.object.data.materials.append(materials[10])
bpy.ops.object.light_add(type='AREA',location=(1,-3,8));bpy.context.object.data.energy=1200;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=8
bpy.ops.object.camera_add(location=(5,-8,10));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=9.0

for ob in bpy.data.objects:
 if ob.type=='FONT':ob.rotation_euler=camera.rotation_euler;ob.data.size=.14
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.world.color=(.30,.30,.30);scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.render.filepath=str(here/'fitted-source-gallery.png');bpy.ops.render.render(write_still=True)
