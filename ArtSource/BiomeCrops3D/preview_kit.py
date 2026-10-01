"""Seven source review pages, retaining editable Blender scenes. No game edits."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
kit=json.loads((Path(__file__).parent/'kit.json').read_text());models={m['id']:m for m in kit['models']};materials=[]
for i,color in enumerate(kit['palette']):
 rgb=[int(color[j:j+2],16)/255 for j in [1,3,5]];rgb=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb]
 m=bpy.data.materials.new('Native swatch '+str(i));m.diffuse_color=(*rgb,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1;materials.append(m)
v0=[(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)];f0=[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
def label(body,x,y,size=.15):
 c=bpy.data.curves.new('Label','FONT');c.body=body;c.size=size;c.align_x='CENTER';o=bpy.data.objects.new(body,c);bpy.context.scene.collection.objects.link(o);o.location=(x,y,.01);c.materials.append(materials[17])
for biome in ['Spread','Sodden','Beating','Grovelands','Overwrit','Stump','Cave']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for row,s in enumerate([s for s in kit['species'] if s['biome']==biome]):
  suffixes=['0-dry','0-wet','1-dry','1-wet','2-dry','2-wet','seed','harvest'];label(s['name'],-5.28,3.0-row*1.45,.14)
  for col,suffix in enumerate(suffixes):
   model=models['biome-crop-'+s['stem']+'-'+suffix];verts=[];faces=[];matids=[]
   for b in model['boxes']:
    off=len(verts);c=b['center'];z=b['size'];verts.extend((c['x']+v[0]*z['x'],c['z']+v[1]*z['z'],c['y']+v[2]*z['y']) for v in v0);faces.extend(tuple(off+i for i in f) for f in f0);matids.extend([b['color']]*6)
   mesh=bpy.data.meshes.new(model['id']);mesh.from_pydata(verts,[],faces);mesh.update()
   for m in materials:mesh.materials.append(m)
   for f,c in zip(mesh.polygons,matids):f.material_index=c
   ob=bpy.data.objects.new(model['id'],mesh);bpy.context.scene.collection.objects.link(ob);ob.location=((col-3.5)*1.17,3.1-row*1.45,0)
   if row==0:label(suffix,ob.location.x,4.04,.13)
 label(biome+' | original botanical forms',0,4.63,.23)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.threads_mode='FIXED';scene.render.threads=2
 scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.012,.03,.025,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
 light=bpy.data.objects.new('Soft short shadow',bpy.data.lights.new('Soft short shadow','AREA'));scene.collection.objects.link(light);light.location=(-5,-4,10);light.data.energy=2600;light.data.size=5
 camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'));scene.collection.objects.link(camera);camera.location=(-.45,-10,18);camera.rotation_euler=(Vector((-.45,.65,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=12.5;scene.camera=camera
 scene.render.resolution_x=1800;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/(biome.lower()+'.png'));bpy.ops.wm.save_as_mainfile(filepath=str(out/(biome.lower()+'.blend')),compress=True);bpy.ops.render.render(write_still=True)
