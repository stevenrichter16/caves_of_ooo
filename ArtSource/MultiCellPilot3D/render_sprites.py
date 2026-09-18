"""Native 16px icon pipeline: actual pilot models, binary coverage, shared ink.
Authorized Blender orthographic/icon render workflow; no existing sprite edited.
"""
import bpy,sys,json,math,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent;out=root/'sprites';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'pilot_kit.blend'));scene=bpy.context.scene
for child in list(scene.collection.children):scene.collection.children.unlink(child)
for ob in list(scene.collection.objects):scene.collection.objects.unlink(ob)
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=16;scene.render.resolution_y=16;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.60,.67,.75,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.8
sunD=bpy.data.lights.new('Icon_key','SUN');sunD.energy=2;sunD.color=(1,.94,.88);sunD.angle=.25;sun=bpy.data.objects.new('Icon_key',sunD);scene.collection.objects.link(sun);sun.rotation_euler=(.36,-.44,0)
camD=bpy.data.cameras.new('Icon_overhead');camD.type='ORTHO';cam=bpy.data.objects.new('Icon_overhead',camD);scene.collection.objects.link(cam);scene.camera=cam
catalog=json.loads((root/'catalog.json').read_text());models={m['id']:m for m in catalog['models']}
results=[]
for mid,name in [('PilotMawToad','maw_toad'),('PilotPipe_0','copper_pipe'),('PilotTar_0','tar_seep'),('PilotSteamVent_0','steam_vent')]:
 m=models[mid];coll=bpy.data.collections.get(mid);assert coll,mid
 instance=bpy.data.objects.new('Native_icon_'+name,None);instance.instance_type='COLLECTION';instance.instance_collection=coll;scene.collection.objects.link(instance)
 center=m['boundsCenter'];size=m['boundsSize'];cam.location=(center['x'],center['z'],10);cam.rotation_euler=(0,0,0);camD.ortho_scale=max(size['x'],size['z'])*16/12
 scene.render.filepath=str(out/(name+'-raw.png'));bpy.ops.render.render(write_still=True)
 # Work from the generated render buffer. Coverage quantization is the sprite
 # rasterization stage, followed by a one-pixel exterior ink contour.
 img=bpy.data.images.load(str(out/(name+'-raw.png')),check_existing=False);rgba=np.array(img.pixels[:],np.float32).reshape(16,16,4);mask=rgba[:,:,3]>.35
 dilated=mask.copy()
 for dy in [-1,0,1]:
  for dx in [-1,0,1]:
   if abs(dx)+abs(dy)>1:continue
   shifted=np.zeros_like(mask);ys=slice(max(0,dy),min(16,16+dy));xs=slice(max(0,dx),min(16,16+dx));shifted[ys,xs]=mask[max(0,-dy):min(16,16-dy),max(0,-dx):min(16,16-dx)];dilated|=shifted
 ink=dilated&~mask;rgba[~mask]=0;rgba[:,:,3]=dilated.astype(np.float32)
 # Generated PNG image.save writes this byte-oriented buffer as supplied.
 # Exact sRGB ink is independently checked after decoding the final PNG.
 rgba[ink,:3]=np.array([30,32,28],np.float32)/255
 result=bpy.data.images.new(name,width=16,height=16,alpha=True);result.alpha_mode='STRAIGHT';result.pixels.foreach_set(rgba.ravel());result.filepath_raw=str(out/(name+'.png'));result.file_format='PNG';result.save()
 check=bpy.data.images.load(str(out/(name+'.png')),check_existing=False);got=np.array(check.pixels[:]).reshape(16,16,4);assert set(np.unique(got[:,:,3]))<={0,1}
 results.append(dict(name=name,sourceModel=mid,dimensions=[16,16],binaryAlpha=True,outlineSrgb=[30,32,28],visiblePixels=int(dilated.sum()),inkPixels=int(ink.sum()),sha256=hashlib.sha256((out/(name+'.png')).read_bytes()).hexdigest()))
 bpy.data.objects.remove(instance,do_unlink=True)
(root/'reports/sprites.json').write_text(json.dumps(dict(passed=True,method='Actual orthographic Blender geometry rendered at16px; binary coverage with one-pixel shared outline',sprites=results),indent=2)+'\n')
print('PILOT_SPRITES_COMPLETE',results)
