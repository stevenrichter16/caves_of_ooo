"""Diagnostic fine/coarse pairs at identical uniform native-owner envelopes.

Run in a background Blender process with --threads 2. This creates a temporary
scene and a diagnostic PNG; it does not replace a town/candidate .blend file.
"""
import json
import math
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from mathutils import Vector
from town_generator.materials import make_palette
from town_generator.native_assets import asset_manifest
from town_generator.native_coarse import COARSE_KEYS

root=Path(__file__).resolve().parents[1]
scene=bpy.data.scenes.new('NativeCoarseDiagnostic')
bpy.context.window.scene=scene
palette=make_palette('native-coarse-diagnostic')
fine={a['sourceAsset']:a for a in asset_manifest()['assets']}
coarse={a['sourceAsset']:a for a in asset_manifest('native_coarse')['assets']}
keys=sorted(COARSE_KEYS)
for index,key in enumerate(keys):
    column=index%4;row=index//4
    base=Vector((column*7,-row*6,0))
    # Both examples fit one physical owner envelope, preserving height/shape
    # uniformly; differing recipe extents never receive per-axis distortion.
    target=[v*min(1.,2.7/max(fine[key]['bounds']['size'])) for v in fine[key]['bounds']['size']]
    for name,asset,x in (('fine',fine[key],0),('coarse',coarse[key],3.3)):
        mesh=bpy.data.meshes.new(key+'-'+name)
        mesh.from_pydata(asset['vertices'],[],asset['quads']);mesh.update()
        for material in palette:mesh.materials.append(material)
        for polygon,material in zip(mesh.polygons,asset['quadMaterials']):polygon.material_index=material
        obj=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(obj)
        obj.location=base+Vector((x,0,0))
        scale=min(target[a]/asset['bounds']['size'][a] for a in range(3));obj.scale=(scale,)*3
        assert not mesh.validate(),key+' mesh invalid'
    font=bpy.data.curves.new(key+'-label','FONT');font.body=key.replace('_',' ')+'   '+str(sum(r[3] for r in fine[key]['runs']))+' > '+str(sum(r[3] for r in coarse[key]['runs']))
    font.size=.34;font.align_x='CENTER'
    label=bpy.data.objects.new(font.name,font);scene.collection.objects.link(label)
    label.location=base+Vector((1.65,-1.5,.01));font.materials.append(palette[0])

world=bpy.data.worlds.new('CoarseDiagnosticWorld');world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.12,.14,.17,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8;scene.world=world
sun_data=bpy.data.lights.new('CoarseDiagnosticSun','SUN');sun_data.energy=2.5;sun_data.angle=.3
sun=bpy.data.objects.new(sun_data.name,sun_data);scene.collection.objects.link(sun);sun.rotation_euler=(.3,-.4,-.4)
plane_mesh=bpy.data.meshes.new('DiagnosticFloor');plane_mesh.from_pydata([(-5,-32,-.025),(28,-32,-.025),(28,6,-.025),(-5,6,-.025)],[],[(0,1,2,3)])
plane=bpy.data.objects.new('DiagnosticFloor',plane_mesh);scene.collection.objects.link(plane)
floor=bpy.data.materials.new('DiagnosticNeutralGround');floor.diffuse_color=(.18,.2,.22,1);plane_mesh.materials.append(floor)
camera_data=bpy.data.cameras.new('CoarseDiagnosticCamera');camera_data.type='ORTHO';camera_data.ortho_scale=32
camera=bpy.data.objects.new(camera_data.name,camera_data);scene.collection.objects.link(camera)
target=Vector((12,-12,0));camera.location=target+Vector((0,-15,40))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.resolution_x=2100;scene.render.resolution_y=2300;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
output=root/'Output/native-region/coarse-comparison.png';output.parent.mkdir(parents=True,exist_ok=True)
scene.render.filepath=str(output)
bpy.ops.render.render(write_still=True,scene=scene.name)
print(json.dumps({'status':'passed','pairs':len(keys),'path':str(output),'note':'fine left, coarse right; same uniform owner envelopes'},sort_keys=True))
