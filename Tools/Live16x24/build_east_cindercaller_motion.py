"""Five original Cindercaller derivatives as editable native integer-cell geometry.

Blender --background --python Tools/Live16x24/build_east_cindercaller_motion.py -- --defer-unity
Always packet-only: no Assets, Unity, Git or raster image editing. Approved idle
geometry is borrowed read-only; the saved concept guides gestures, not pixels.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import camera_for
from build_cindercaller import cindercaller_cells, P, model
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/east-cindercaller-motion'
POSES = ('walk-a', 'walk-b', 'attack', 'hurt', 'spit-cast')
LIMBS = {'bent-hind-leg', 'hind-ankle', 'hind-foot', 'hind-toe',
         'near-shoulder', 'near-wrist', 'near-claw', 'far-forearm', 'far-claw'}
FACE = {'brow', 'blunt-face', 'eye', 'nose'}


def make_frames():
    idle = cindercaller_cells()
    descriptions = (
        'Rearward hind contact and near claw under the fixed mantle; the far foreclaw reaches forward.',
        'Hind foot draws inward and near claw plants ahead while the far foreclaw folds back. No whole-body bob.',
        'Leading foreclaw lifts beside the unchanged blunt face; the other claw braces. No held weapon or hit effect.',
        'Complete cap, mantle and face recoil one cell back and down; braced limbs retain the original foot baseline.',
        'Planted original limbs and fixed mantle; complete face advances one cell forward and up on a connected neck. No projectile or ammo state.',
    )
    frames = []
    for index, (pose, description) in enumerate(zip(POSES, descriptions)):
        cells = copy.deepcopy(idle)
        removed = FACE if pose == 'spit-cast' else LIMBS
        for row in cells:
            for x, value in enumerate(row):
                if value and value[0] in removed:
                    row[x] = None

        def rect(part, color, x, y, w=1, h=1):
            assert color in P and 0 <= x < x+w <= 16 and 0 <= y < y+h <= 23
            for yy in range(y, y+h):
                for xx in range(x, x+w):
                    assert cells[yy][xx] is None or cells[yy][xx][0] in LIMBS, (pose, part, xx, yy, cells[yy][xx])
                    cells[yy][xx] = (part, color)

        if pose == 'walk-a':
            rect('bent-hind-leg', 'tawny', 3, 17, 3, 3)
            rect('hind-ankle', 'tawny', 2, 20, 2, 2)
            rect('hind-foot', 'tawny', 1, 22, 3)
            rect('hind-toe', 'claw', 1, 22)
            rect('near-shoulder', 'tawny', 8, 18, 2, 2)
            rect('near-wrist', 'tawny', 7, 20, 2, 2)
            rect('near-claw', 'claw', 6, 22, 3)
            rect('far-forearm', 'tawny', 13, 19, 2)
            rect('far-forearm', 'tawny', 14, 20)
            rect('far-claw', 'claw', 14, 21, 2)
        elif pose == 'walk-b':
            rect('bent-hind-leg', 'tawny', 5, 17, 2, 3)
            rect('hind-ankle', 'tawny', 5, 20, 2, 2)
            rect('hind-foot', 'tawny', 4, 22, 3)
            rect('hind-toe', 'claw', 4, 22)
            rect('near-shoulder', 'tawny', 8, 18, 2, 2)
            rect('near-wrist', 'tawny', 9, 20, 2, 2)
            rect('near-claw', 'claw', 10, 22, 3)
            rect('far-forearm', 'tawny', 13, 19)
            rect('far-claw', 'claw', 12, 20, 2)
        elif pose == 'attack':
            rect('bent-hind-leg', 'tawny', 3, 17, 3, 3)
            rect('hind-ankle', 'tawny', 2, 20, 2, 2)
            rect('hind-foot', 'tawny', 1, 22, 3)
            rect('hind-toe', 'claw', 1, 22)
            rect('near-shoulder', 'tawny', 8, 18, 2, 2)
            rect('near-wrist', 'tawny', 8, 20, 2, 2)
            rect('near-claw', 'claw', 9, 22, 3)
            rect('far-forearm', 'tawny', 12, 19, 4)
            rect('far-claw', 'claw', 14, 16, 2)
            rect('far-claw', 'claw', 15, 17, 1, 2)
        elif pose == 'hurt':
            torso = [(x, y, value) for y, row in enumerate(cells)
                     for x, value in enumerate(row) if value]
            cells = [[None]*16 for _ in range(24)]
            for x, y, value in torso:
                assert 0 <= x-1 < 16 and y+1 < 23
                cells[y+1][x-1] = value
            rect('bent-hind-leg', 'tawny', 3, 18, 3, 2)
            rect('hind-ankle', 'tawny', 4, 20, 2, 2)
            rect('hind-foot', 'tawny', 4, 22, 3)
            rect('hind-toe', 'claw', 4, 22)
            rect('near-shoulder', 'tawny', 7, 20, 2)
            rect('near-wrist', 'tawny', 8, 21, 2)
            rect('near-claw', 'claw', 9, 22, 3)
            rect('far-forearm', 'tawny', 12, 20)
            rect('far-claw', 'claw', 11, 21, 2)
        else:
            for y, row in enumerate(idle):
                for x, value in enumerate(row):
                    if value and value[0] in FACE:
                        rect(value[0], value[1], x+1, y-1)
            rect('spit-neck', 'tawny', 11, 17)
        frames.append({'index': index, 'pose': pose, 'id': 'cindercaller-'+pose,
                       'blueprint': 'MarlbackCindercaller', 'description': description,
                       'cells': cells})
    return frames


def assert_preserved(preserved):
    for path, digest in preserved.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == digest, path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--defer-unity', action='store_true', help='Explicit request for always-on packet-only export.')
    parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    preserved = json.loads((OUT/'preserved-inputs.json').read_text())
    assert_preserved(preserved)
    frozen = json.loads((ROOT/'ArtSource/Live16x24/cindercaller/source-cells.json').read_text())
    idle = cindercaller_cells()
    assert json.loads(json.dumps(idle)) == frozen['cells'] and P == frozen['palette']
    frames = make_frames()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    native = bpy.context.scene
    native.name = 'East Cindercaller five native motion derivatives'
    collections = []
    for index, frame in enumerate(frames):
        col = model(native, frame['id'], frame['cells'], P)
        native.collection.children.unlink(col)
        for obj in col.objects:
            obj['authoring'] = 'Original native motion derivative from unchanged approved Cindercaller geometry.'
            obj['pose'] = frame['pose']
        instance(native, col, frame['id'], index*16, 0)
        collections.append(col)
    camera_for(native, 80, 24)
    native.render.filepath = str(OUT/'east-cindercaller-motion.png')
    bpy.ops.render.render(write_still=True, scene=native.name)
    idle_col = model(native, 'Borrowed approved Cindercaller idle', idle, P)
    native.collection.children.unlink(idle_col)
    for scale, name in ((1, 'contact-native'), (8, 'contact')):
        scene = bpy.data.scenes.new('Cindercaller borrowed idle and derivatives '+str(scale)+'x')
        camera_for(scene, 152, 40, scale=scale)
        plane(scene, 'Quiet contact background', '032F23', 0, 0, 152, 40, -1)
        for column, col in enumerate([idle_col]+collections):
            instance(scene, col, 'Borrowed idle' if column == 0 else POSES[column-1], 8+24*column, 8)
        scene.render.filepath = str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True, scene=scene.name)
    source = {'schema': 1, 'coordinates': '16x24 top-left origin; y downward; null transparent.',
              'palette': P, 'frames': frames}
    (OUT/'source-cells.json').write_text(json.dumps(source, indent=2)+'\n')
    write_meta(OUT/'east-cindercaller-motion.png.meta')
    rgba = {key: bytes.fromhex(value)+b'\xff' for key, value in P.items()}
    pixels = b''.join(rgba[cell[1]] if cell else b'\0\0\0\0'
                      for y in range(24) for frame in frames for cell in frame['cells'][y])
    manifest = {
        'schema': 1, 'frameWidth': 16, 'frameHeight': 24, 'frameCount': 5, 'width': 80, 'height': 24,
        'blueprint': 'MarlbackCindercaller', 'frameOrder': list(POSES),
        'resource': 'Sprites/Live16x24/east-cindercaller-motion', 'actorPivot': [.5, 0], 'pixelsPerUnit': 16,
        'idleBorrow': {'resource': 'Sprites/Live16x24/cindercaller', 'frame': 0},
        'filter': 'Point', 'wrap': 'Clamp', 'mipmaps': False, 'compression': 'None', 'alpha': 'Binary0/255',
        'palette': P, 'source': 'native.blend', 'editableCells': 'source-cells.json',
        'generator': 'Tools/Live16x24/build_east_cindercaller_motion.py', 'native': 'east-cindercaller-motion.png',
        'importMetadata': 'east-cindercaller-motion.png.meta', 'contacts': ['contact-native.png', 'contact.png'],
        'contactLayout': '152x40 native; top8, columns left8+24*n: borrowed idle, WalkA, WalkB, Attack, Hurt, SpitCast.',
        'unityExportDeferred': True, 'referenceSHA256': preserved,
        'nativeSHA256': hashlib.sha256((OUT/'east-cindercaller-motion.png').read_bytes()).hexdigest(),
        'decodedPixelSHA256': hashlib.sha256(pixels).hexdigest(),
        'limits': 'Canonical East-facing derivatives only. Immutable idle borrowed at runtime. No equipment, fuel, new action or permanent FX promise. Source evidence only; parent owns event admission and actual gameplay acceptance.',
        'frames': [{key: value for key, value in frame.items() if key != 'cells'} for frame in frames],
    }
    (OUT/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'native.blend'))
    assert_preserved(preserved)
    print(json.dumps({'packetOnly': True, 'resourceWritten': False, 'frameCount': 5,
                      'nativeSHA256': manifest['nativeSHA256'], 'decodedPixelSHA256': manifest['decodedPixelSHA256']}))


if __name__ == '__main__':
    main()
