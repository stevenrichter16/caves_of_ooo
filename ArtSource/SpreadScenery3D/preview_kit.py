"""Offline geometry review only. Does not replace native imported-asset gates."""
import bpy,json,sys,argparse
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--traps-only',action='store_true');p.add_argument('--cord-only',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True)
kit=json.loads(Path(__file__).with_name('kit.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials=[]
for i,c in enumerate(kit['palette']):
 rgb=[int(c[j:j+2],16)/255 for j in [1,3,5]];rgb=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb]
 m=bpy.data.materials.new('Glade-'+str(i));m.diffuse_color=(*rgb,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1;materials.append(m)
v0=[(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)]
f0=[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
models={m['id']:m for m in kit['models']}
ids=['spread-scenery-'+bp+suffix+'-0' for bp in ['spiketrap','beartrap','firetrap','pressureplate'] for suffix in ['', '-jammed']] if a.traps_only else [m['id'] for m in kit['models'] if m['id'].endswith('-0')]
if a.cord_only:ids=['spread-scenery-knotflaxsnare-'+str(v) for v in range(2)]
for n,model in enumerate(models[id] for id in ids):
 verts=[];faces=[];colors=[]
 for b in model['boxes']:
  off=len(verts);c,s=b['center'],b['size'];verts.extend((c[0]+v[0]*s[0],c[2]+v[1]*s[2],c[1]+v[2]*s[1]) for v in v0);faces.extend(tuple(off+j for j in f) for f in f0);colors.extend([b['color']]*6)
 mesh=bpy.data.meshes.new(model['id']);mesh.from_pydata(verts,[],faces);mesh.update()
 for m in materials:mesh.materials.append(m)
 for f,c in zip(mesh.polygons,colors):f.material_index=c
 ob=bpy.data.objects.new(model['id'],mesh);bpy.context.scene.collection.objects.link(ob);x=(n%2-.5)*2.3 if a.traps_only else (n%7-3)*1.9;y=(1.5-n//2)*1.7 if a.traps_only else (1.5-n//7)*2.3;ob.location=(x,y,0)
 if a.cord_only:x=(n-.5)*1.5;y=0;ob.location=(x,y,0)
 bpy.ops.mesh.primitive_plane_add(size=1.32,location=(x,y,-.026));bpy.context.object.data.materials.append(materials[1])
 name=model['id'].removeprefix('spread-scenery-') if a.cord_only else model['id'].removeprefix('spread-scenery-').removesuffix('-0');font=bpy.data.curves.new('label','FONT');font.body=name;font.size=.14;font.align_x='CENTER';font.extrude=0
 label=bpy.data.objects.new('label-'+name,font);bpy.context.scene.collection.objects.link(label);label.location=(x,y-.91,.015);font.materials.append(materials[17])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.4,.47,.42,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.9
lamp=bpy.data.objects.new('reviewlight',bpy.data.lights.new('reviewlight','AREA'));scene.collection.objects.link(lamp);lamp.location=(-5,-5,12);lamp.data.energy=2400;lamp.data.size=6
camera=bpy.data.objects.new('camera',bpy.data.cameras.new('camera'));scene.collection.objects.link(camera);camera.location=(0,-8,14) if a.traps_only else (0,-9,15);camera.rotation_euler=(Vector((0,-.3,.05) if a.cord_only else (0,0,.1) if a.traps_only else (0,.2,.2))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.1 if a.cord_only else 8.1 if a.traps_only else 14.4;scene.camera=camera
scene.render.resolution_x=1400 if a.traps_only or a.cord_only else 2800;scene.render.resolution_y=850 if a.cord_only else 1800 if a.traps_only else 1950;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.view_settings.exposure=0
scene.render.film_transparent=False;scene.world.color=(.06,.08,.06);scene.render.image_settings.file_format='PNG';scene.render.filepath=str(a.output/('cord-snare-gallery.png' if a.cord_only else 'trap-state-gallery.png' if a.traps_only else 'scenery-gallery.png'));bpy.ops.render.render(write_still=True)
