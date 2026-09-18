"""Render saved source at detail and retained gameplay pixel scale.

No source/Assets mutation. Output SHA and exact native sample time are recorded.
The small proof uses 33.38 square-pixel samples/metre; it is not a Unity capture.
"""
import bpy,argparse,hashlib,json,math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT))
from normalize_png import remove_density_chunk
ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);ap.add_argument('--only',nargs='*');ap.add_argument('--loops',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'));manifest=json.loads((ROOT/'manifest.json').read_text());receipt=dict(sourceBlendSha256=hashlib.sha256((ROOT/'starter_spells.blend').read_bytes()).hexdigest(),pixelScale=33.38,cameraPitchDegrees=56,nativeFps=100,proofBoundary='Blender studio at native pixel density; not actual Unity gameplay',images=[])
for spec in manifest['studies']:
 if args.only and spec['id'] not in args.only:continue
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;original=scene.camera.data.ortho_scale
 samples=[('contact',spec['contactFrame']),('peak',spec['contactFrame']+8)]
 for label,frame in samples:
  scene.frame_set(math.floor(frame),subframe=frame%1)
  for view in ('detail','native_scale'):
   if view=='detail':scene.render.resolution_x=1100;scene.render.resolution_y=620;scene.camera.data.ortho_scale=original;scene.cycles.samples=20
   else:scene.render.resolution_x=420;scene.render.resolution_y=260;scene.camera.data.ortho_scale=420/33.38;scene.cycles.samples=24
   path=out/(spec['id']+'-'+label+'-'+view+'.png');scene.render.filepath=str(path);bpy.ops.render.render(write_still=True);remove_density_chunk(path)
   receipt['images'].append(dict(spell=spec['id'],phase=label,nativeFrame=frame,nativeSeconds=frame/100,view=view,path=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print('READABILITY_STILL_READY '+str(path),flush=True)
 if args.loops:
  folder=out/'frames'/spec['id'];folder.mkdir(parents=True,exist_ok=True);scene.render.resolution_x=840;scene.render.resolution_y=474;scene.camera.data.ortho_scale=original;scene.cycles.samples=12
  for index,frame in enumerate(range(0,111,3)):
   scene.frame_set(frame);path=folder/('%04d.png'%index);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True);remove_density_chunk(path)
   receipt['images'].append(dict(spell=spec['id'],phase='motion',nativeFrame=frame,nativeSeconds=frame/100,view='detail',path=str(path.relative_to(out)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
  print('READABILITY_LOOP_READY '+spec['id'],flush=True)
 scene.camera.data.ortho_scale=original
(out/'render-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
