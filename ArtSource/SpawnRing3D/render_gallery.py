import bpy,math,json,sys
from mathutils import Vector
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);bpy.ops.wm.open_mainfile(filepath=str(root/'ring_kit.blend'))
c=json.load(open(root/'catalog.json'));scene=bpy.context.scene
models=[m for m in c['models'] if m['rigged']]
legend=[]
for i,m in enumerate(models):
 co=bpy.data.collections.get(m['id']);assert co is not None,m['id']
 ob=bpy.data.objects.new(m['id']+'_preview',None);scene.collection.objects.link(ob);ob.instance_type='COLLECTION';ob.instance_collection=co;ob.location=((i%7)*2.2,(i//7)*2.5,0);legend.append({'row':i//7,'column':i%7,'modelId':m['id'],'rigFamily':m['rigFamily']})
mat=bpy.data.materials.new('Gallery_neutral_ground');mat.diffuse_color=(.24,.25,.20,1)
bpy.ops.mesh.primitive_plane_add(size=2,location=(6.6,2.5,-.045));floor=bpy.context.object;floor.scale=(9.4,5.8,1);floor.data.materials.append(mat)
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.40,.435,.46,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
li=bpy.data.lights.new('Sun','SUN');li.energy=2.35;li.color=(1,.88,.71);li.angle=math.radians(24);lo=bpy.data.objects.new('Sun',li);scene.collection.objects.link(lo);lo.rotation_euler=(.4,-.3,-.4)
ca=bpy.data.cameras.new('Gallery_camera');ob=bpy.data.objects.new('Gallery_camera',ca);scene.collection.objects.link(ob);scene.camera=ob;ca.type='ORTHO';ca.ortho_scale=16.8
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.resolution_x=1680;scene.render.resolution_y=840;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=.25
for angle in ['overhead','threequarter']:
 ob.location=(6.6,2.5,20) if angle=='overhead' else (6.6,-9.5,15);ob.rotation_euler=(Vector((6.6,2.5,.4))-ob.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(root/'renders'/('creatures-'+angle+'.png'));bpy.ops.render.render(write_still=True)
(root/'renders/creatures-legend.json').write_text(json.dumps(legend,indent=2))
