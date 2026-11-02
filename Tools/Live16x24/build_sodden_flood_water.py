"""Original restrained ripple quads over unchanged approved native bank cells.
Blender --background --threads 4 --python this-file -- --build
Blender editable.blend --background --python this-file -- --inspect
python3 this-file --verify (read-only pixel comparison; no raster generation).
"""
import copy, hashlib, json, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/sodden-flood-water'
BASE = json.loads((ROOT / 'ArtSource/Live16x24/Ground/source-cells.json').read_text())
PALETTE = {k: BASE['palette'][k] for k in ('water', 'shallows', 'bank')}
PALETTE.update({'ripple-low': '175052', 'ripple-light': '235E5E', 'ripple-shade': '0B3F42'})
# Eight original rhythms, including two almost still views. No per-frame motion.
# Tuple: y, x, length, palette. Short interior strokes never cross cell edges.
STROKES = [[], [(9, 3, 4, 'ripple-low')],
           [(6, 2, 4, 'ripple-low'), (7, 4, 5, 'ripple-light'), (9, 7, 3, 'ripple-shade')],
           [(16, 5, 5, 'ripple-low'), (17, 3, 3, 'ripple-light')],
           [(4, 8, 4, 'ripple-low'), (18, 2, 3, 'ripple-shade')],
           [(11, 2, 3, 'ripple-low'), (12, 6, 5, 'ripple-light'), (14, 8, 3, 'ripple-low')],
           [(20, 5, 4, 'ripple-low')],
           [(8, 7, 5, 'ripple-shade'), (9, 4, 4, 'ripple-low'), (11, 3, 3, 'ripple-light')]]

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(name, value): (OUT / name).write_text(json.dumps(value, indent=2) + '\n')
def guard():
    pins = json.loads((OUT / 'protected-inputs.json').read_text())
    for name, expected in pins.items(): assert sha(ROOT / name) == expected, name
    return len(pins)
def frames():
    result = []
    for variant, strokes in enumerate(STROKES):
        for mask in range(16):
            cells = copy.deepcopy(BASE['frames'][mask]['cells'])
            for y, x, length, color in strokes:
                for xx in range(x, x + length):
                    if cells[y][xx][1] == 'water': cells[y][xx] = ['quiet-water-ripple', color]
            result.append({'index': variant * 16 + mask, 'id': f'flood-{variant}-bank-{mask}',
                           'variant': variant, 'dryEdgeMask': mask, 'cells': cells})
    return result
def build():
    import bpy
    sys.path.insert(0, str(ROOT / 'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard(); bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene; scene.name = 'Sodden quiet flood water native2048x24'
    for frame in frames():
        col = model(scene, frame['id'], frame['cells'], PALETTE); scene.collection.children.unlink(col)
        instance(scene, col, frame['id'], frame['index'] * 16, 0)
    camera_for(scene, 2048, 24); scene.render.filepath = str(OUT / 'sodden-flood-water.png')
    bpy.ops.render.render(write_still=True, scene=scene.name); canonicalize_timing(OUT / 'sodden-flood-water.png')
    save('source-cells.json', {'palette': PALETTE, 'frames': frames()})
    save('manifest.json', {'frames': 128, 'size': [2048, 24], 'native_cell': [16, 24],
                          'variants': 8, 'bank_masks': 16, 'scope': 'Optional visible Sodden16.5 permanent ownerless water only',
                          'filter': 'Point', 'compression': 'None', 'mipmaps': False, 'source': 'editable.blend'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'editable.blend')); guard()
def inspect():
    import bpy
    guard()
    for frame in frames(): assert sum(len(o.data.polygons) for o in bpy.data.collections[frame['id']].objects) == 384
    save('reopened-blend.json', {'passed': True, 'editable_quads': 49152, 'semantic_frames': 128})
def verify():
    from PIL import Image
    guard(); data = json.loads((OUT / 'source-cells.json').read_text())
    assert data == {'palette': PALETTE, 'frames': frames()}
    image = Image.open(OUT / 'sodden-flood-water.png').convert('RGBA'); assert image.size == (2048, 24)
    changed = 0
    for frame in frames():
        base = BASE['frames'][frame['dryEdgeMask']]['cells']
        for y, row in enumerate(frame['cells']):
            for x, cell in enumerate(row):
                assert image.getpixel((frame['index'] * 16 + x, y)) == tuple(bytes.fromhex(PALETTE[cell[1]])) + (255,)
                if base[y][x][1] != 'water' or x in (0, 15) or y in (0, 23): assert cell == base[y][x]
                changed += cell != base[y][x]
    save('verification.json', {'passed': True, 'exact_native_pixels': 49152, 'changed_water_pixels': changed,
                              'all_bank_and_border_cells_preserved': True, 'protected_inputs': guard(),
                              'png_sha256': sha(OUT / 'sodden-flood-water.png'), 'blend_sha256': sha(OUT / 'editable.blend'),
                              'actual_play': False})
    print('PASS: 49152 exact native pixels; all original bank/border cells and protected sources unchanged')
if __name__ == '__main__':
    if '--build' in sys.argv: build()
    elif '--inspect' in sys.argv: inspect()
    else: verify()
