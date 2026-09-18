"""Render slow-review source frames from the saved editable Blender study."""
import bpy, json, sys, argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from normalize_png import remove_density_chunk
ap=argparse.ArgumentParser();ap.add_argument('--only',nargs='*');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=json.loads((ROOT/'manifest.json').read_text())
for spec in manifest['studies']:
 if args.only and spec['id'] not in args.only:continue
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
 scene.render.resolution_x=840;scene.render.resolution_y=474;scene.cycles.samples=12
 folder=ROOT/'frames'/spec['id'];folder.mkdir(exist_ok=True)
 for index,frame in enumerate(range(0,111,3)):
  scene.render.filepath=str(folder/('%04d.png'%index))
  scene.frame_set(frame);bpy.ops.render.render(write_still=True);remove_density_chunk(scene.render.filepath)
 print('LOOP_FRAMES_READY '+spec['id'],flush=True)
print('SELECTED_ANIMATIONS_RENDERED '+json.dumps(args.only or [s['id'] for s in manifest['studies']]),flush=True)
