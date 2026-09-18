import bpy,sys,json
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1])
for zid in json.loads((root/'catalog.json').read_text())['zones']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'scenes'/zid/'scene.blend'));sc=bpy.context.scene;sc.render.threads_mode='FIXED';sc.render.threads=2;sc.cycles.samples=32;sc.render.filepath=str(root/'scenes'/zid/'overhead.png');bpy.ops.render.render(write_still=True)
 print('FINAL_WIDE_READY',zid,flush=True)
