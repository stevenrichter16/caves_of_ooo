"""Optional Blender review of the exact cuboid source; writes only --output."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
kit=json.loads((Path(__file__).parent/'kit.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials=[]
for i,hexcolor in enumerate(kit['palette']):
 rgb=[int(hexcolor[j:j+2],16)/255 for j in [1,3,5]]
 # Blender diffuse values are linear; recipe colors are native sRGB swatches.
 linear=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb]
 m=bpy.data.materials.new('GladeSwatch'+str(i));m.diffuse_color=(*linear,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*linear,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1;materials.append(m)
verts0=[(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)]
faces0=[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
families=['paving','road','tree','hedge','vine-wall','stubble','grain','flowers','dry-brush','rock']
for model in kit['models']:
 verts=[];faces=[];matids=[]
 for box in model['boxes']:
  offset=len(verts);c=box['center'];s=box['size'];verts.extend((c['x']+v[0]*s['x'],c['z']+v[1]*s['z'],c['y']+v[2]*s['y']) for v in verts0);faces.extend(tuple(offset+j for j in f) for f in faces0);matids.extend([box['color']]*6)
 mesh=bpy.data.meshes.new(model['id']);mesh.from_pydata(verts,[],faces);mesh.update()
 for material in materials:mesh.materials.append(material)
 for face,index in zip(mesh.polygons,matids):face.material_index=index
 ob=bpy.data.objects.new(model['id'],mesh);bpy.context.scene.collection.objects.link(ob);ob.location=((families.index(model['family'])-4.5)*1.55,(1.5-model['variant'])*2.1,0)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.45,.5,.47,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
lamp=bpy.data.objects.new('Soft short-shadow light',bpy.data.lights.new('Soft short-shadow light','AREA'));scene.collection.objects.link(lamp);lamp.location=(-5,-6,12);lamp.data.energy=2400;lamp.data.size=4
camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'));scene.collection.objects.link(camera);camera.location=(0,-9,14);camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=17.5;scene.camera=camera
scene.render.resolution_x=2100;scene.render.resolution_y=1300;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast';scene.view_settings.exposure=0
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/'kit-gallery.png');bpy.ops.wm.save_as_mainfile(filepath=str(out/'kit-review.blend'),compress=True);bpy.ops.render.render(write_still=True)
