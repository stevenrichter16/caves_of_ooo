"""Sumphold-only palette variant of approved, editable connected Duckboards.

Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify
Original cells, alpha, joins and all shared Sodden assets remain unchanged.
"""
import copy, hashlib, json, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/sumphold-boards'
BASE = ROOT / 'ArtSource/Live16x24/sodden-travel/source-cells.json'
COLORS = {'board-shadow':'322E25', 'board-dark':'504B3B', 'board':'887A61',
          'board-worn':'A08C6E', 'board-grain':'635D4B'}
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def data():
    original = json.loads(BASE.read_text())
    result = copy.deepcopy(original)
    result['palette'] = COLORS
    result['frames'] = result['frames'][:16]
    assert all(f['blueprint']=='Duckboard' and f['neighborMask']==i for i,f in enumerate(result['frames']))
    return result
def guard():
    for name, digest in json.loads((OUT/'protected-inputs.json').read_text()).items():
        assert sha(ROOT/name)==digest, name
def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard(); d=data(); bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene; scene.name='Sumphold connected boards native256x24'
    for f in d['frames']:
        col=model(scene,f['id'],f['cells'],d['palette'])
        scene.collection.children.unlink(col); instance(scene,col,f['id'],f['index']*16,0)
    camera_for(scene,256,24); scene.render.filepath=str(OUT/'sumphold-boards.png')
    bpy.ops.render.render(write_still=True,scene=scene.name)
    canonicalize_timing(OUT/'sumphold-boards.png')
    (OUT/'source-cells.json').write_text(json.dumps(d,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sumphold-boards.blend'))
    guard()
def verify():
    from PIL import Image
    guard(); d=data(); saved=json.loads((OUT/'source-cells.json').read_text()); assert saved==d
    original=Image.open(ROOT/'Assets/Resources/Sprites/Live16x24/sodden-travel.png').convert('RGBA')
    current=Image.open(OUT/'sumphold-boards.png').convert('RGBA'); assert current.size==(256,24)
    changed=0
    for f in d['frames']:
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                q=(f['index']*16+x,y); value=current.getpixel(q); old=original.getpixel(q)
                expected=tuple(bytes.fromhex(d['palette'][c[1]]))+(255,) if c else (0,0,0,0)
                assert value==expected,(q,value,expected)
                assert value[3]==old[3],('Alpha changed',q)
                if not c or c[1] not in ('board','board-worn'): assert value==old,('Unapproved palette change',q)
                changed+=value!=old
    result={'passed':True,'frames':16,'pixels':6144,'changed_pixels':changed,'all_alpha_and_geometry_exact':True,
            'changed_palette_keys':['board','board-worn'],'protected_shared_inputs_exact':True,
            'native_png_sha256':sha(OUT/'sumphold-boards.png'),'actual_play':False}
    (OUT/'verification.json').write_text(json.dumps(result,indent=2)+'\n'); print(json.dumps(result))
if __name__=='__main__':
    if '--build' in sys.argv: build()
    else: verify()
