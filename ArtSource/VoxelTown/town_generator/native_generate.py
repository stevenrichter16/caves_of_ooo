"""Export native rectangular candidates and the toolkit mesh library.

Pure: python -m town_generator.native_generate --output ...
Blender: blender -b --threads 2 --python-exit-code 1 --python this.py -- --render --output ...
"""
import argparse
import json
from pathlib import Path
import sys
import tempfile
import os

if __package__ in (None,''):sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from town_generator.native_assets import export_assets
from town_generator.native_region import NativeConfig,generate_region


def export_region(path,region):
    """Atomically write one region manifest; unrelated destination files survive."""
    from town_generator.native_region import validate_region
    validate_region(region);path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    raw=(json.dumps(region,sort_keys=True,separators=(',',':'))+'\n').encode()
    with tempfile.NamedTemporaryFile(prefix='.native-region-',dir=path.parent,delete=False) as stream:
        temporary=Path(stream.name)
        try:stream.write(raw)
        except BaseException:temporary.unlink(missing_ok=True);raise
    try:os.replace(temporary,path)
    finally:temporary.unlink(missing_ok=True)


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True);parser.add_argument('--seed',type=int,default=41)
    parser.add_argument('--width',type=int,default=80);parser.add_argument('--height',type=int,default=25)
    parser.add_argument('--render',action='store_true');parser.add_argument('--samples',type=int,default=24)
    args=parser.parse_args(argv)
    if not 1<=args.samples<=4096:parser.error('samples must be between1 and4096')
    # A render request needs Blender before any prior export can be replaced.
    if args.render:import bpy
    region=generate_region(NativeConfig(seed=args.seed,width=args.width,height=args.height))
    output=args.output.resolve();assets=export_assets(output/'assets.json');export_region(output/'region.json',region)
    if args.render:
        from town_generator.native_scene import realize_chunk
        for chunk in region['chunks']:
            scene=realize_chunk(chunk,assets);bpy.context.window.scene=scene;scene.cycles.samples=args.samples
            scene.render.filepath=str(output/(chunk['zoneId']+'.png'))
            if 'FINISHED' not in bpy.ops.render.render(write_still=True,scene=scene.name):raise RuntimeError('Native preview failed')
        if 'FINISHED' not in bpy.ops.wm.save_as_mainfile(filepath=str(output/'native-region.blend'),copy=True):raise RuntimeError('Native blend save failed')
    print('NATIVE REGION '+json.dumps({'seed':args.seed,'chunks':len(region['chunks']),'assets':len(assets['assets']),
          'placements':sum(len(c['placements']) for c in region['chunks']),'rendered':args.render,'output':str(output)}),flush=True)


if __name__=='__main__':main(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else None)
