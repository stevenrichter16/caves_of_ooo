"""Read-only installed-FBX contact sheets: no Unity assets or source scene edits."""
import bpy,sys,json,math,hashlib
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]);out=Path(sys.argv[sys.argv.index('--')+2]);out.mkdir(parents=True,exist_ok=True)
catalog=json.loads((root/'Assets/Art3D/BuildingBlocks/catalog.json').read_text())['models']
families=list(dict.fromkeys(r['family'] for r in catalog));records=[]
for sheet in range(3):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
 scene.render.threads_mode='FIXED';scene.render.threads=2
 scene.render.resolution_x=1900;scene.render.resolution_y=1800;scene.render.resolution_percentage=100
 scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast' if 'Medium High Contrast' in [x.identifier for x in scene.bl_rna.properties] else 'None'
 world=bpy.data.worlds.new('Review World');scene.world=world;world.use_nodes=True
 world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.40,.42,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7
 palette=bpy.data.materials.new('Installed Building Palette');palette.use_nodes=True
 nodes=palette.node_tree.nodes;tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'Assets/Art3D/BuildingBlocks/Textures/BuildingPalette.png'));tex.interpolation='Closest'
 bsdf=nodes.get('Principled BSDF');palette.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.88
 ground=bpy.data.materials.new('Neutral review surface');ground.diffuse_color=(.14,.16,.17,1)
 bpy.ops.mesh.primitive_plane_add(size=100,location=(0,0,-.03));bpy.context.object.data.materials.append(ground)
 camdata=bpy.data.cameras.new('Contact camera');cam=bpy.data.objects.new('Contact camera',camdata);scene.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=13.4
 target=Vector((1.1,4.8,.30));cam.location=target+Vector((0,18*math.cos(math.radians(56)),18*math.sin(math.radians(56))));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 textmat=bpy.data.materials.new('Label');textmat.use_nodes=True
 labelbsdf=textmat.node_tree.nodes.get('Principled BSDF');labelbsdf.inputs['Base Color'].default_value=(.012,.016,.020,1);labelbsdf.inputs['Roughness'].default_value=1
 def text(label,loc,size=.19):
  data=bpy.data.curves.new(label,'FONT');data.body=label;data.size=size;data.align_x='CENTER';data.align_y='CENTER'
  ob=bpy.data.objects.new(label,data);scene.collection.objects.link(ob);ob.location=loc;ob.rotation_euler=cam.rotation_euler;ob.data.materials.append(textmat)
 text('IMPORTED BUILDING BLOCKS — '+str(sheet+1)+' / 3',(1.2,-1.8,.45),.27)
 for v in range(4):text('variant '+str(v),(v*1.7,-.85,.25),.2)
 for row,family in enumerate(families[sheet*6:sheet*6+6]):
  y=row*2;text(family,(-2.15,y,.55),.19)
  for variant in range(4):
   mid=family+'-'+str(variant);path=root/'Assets/Art3D/BuildingBlocks/Models'/f'{mid}.fbx'
   previous=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False);created=[ob for ob in bpy.data.objects if ob not in previous]
   for ob in created:
    if ob.parent not in created:ob.location+=Vector((variant*1.7,y,0))
    if ob.type=='MESH':ob.data.materials.clear();ob.data.materials.append(palette)
   records.append({'id':mid,'path':str(path.relative_to(root)),'fbxSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'sheet':sheet+1})
 key=bpy.data.lights.new('Broad review key','AREA');key.energy=1700;key.size=9;light=bpy.data.objects.new('Broad review key',key);scene.collection.objects.link(light);light.location=(-3,8,15);light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(out/f'installed-variants-{sheet+1}.png');bpy.ops.render.render(write_still=True)
(out/'contact-sheet-manifest.json').write_text(json.dumps({'source':'installed FBXs; unchanged UVs and installed atlas; neutral review light, not gameplay','models':records},indent=2)+'\n')
print('REVIEWED EXPORTS',len(records))
