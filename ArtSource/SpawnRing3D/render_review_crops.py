import bpy,sys
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1])
for zid,centre in [('Overworld.2.5.0',(33,12.5)),('Overworld.2.6.0',(37,14)),('Overworld.3.5.0',(50,14))]:
 bpy.ops.wm.open_mainfile(filepath=str(root/'scenes'/zid/'scene.blend'));sc=bpy.context.scene;sc.camera.location=(centre[0],centre[1],90);sc.camera.data.ortho_scale=16;sc.render.resolution_x=1024;sc.render.resolution_y=1024;sc.render.resolution_percentage=100;sc.cycles.samples=32;sc.render.threads_mode='FIXED';sc.render.threads=2;sc.render.filepath=str(root/'renders'/(zid+'-gameplay-crop.png'));bpy.ops.render.render(write_still=True)
