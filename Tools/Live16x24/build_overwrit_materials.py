"""Original Overwrit static sprites, authored as editable integer-cell geometry.

Blender --background --threads 4 --python this file
python3 this-file --verify

Rendering writes only this source packet. Importing the new PNG is a separate
explicit copy. No raster input is cropped, traced, edited or resampled.
"""
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/overwrit-materials'
PALETTE = {
    'ground': 'A7A292', 'scuff': '9E998A', 'scuff-light': 'ACA797',
    'shoot-dark': '465343', 'shoot': '65745A', 'shoot-tip': '99A084',
    'wood-dark': '4A4033', 'wood': '75644B', 'wood-edge': '948064',
    'stone-dark': '62665E', 'stone': '83877B', 'stone-light': '9CA091',
}


class Frame:
    def __init__(self, name, blueprint, description, **state):
        self.name, self.blueprint, self.description, self.state = name, blueprint, description, state
        self.cells = [[None] * 16 for _ in range(24)]

    def rect(self, part, color, x, y, w=1, h=1):
        assert color in PALETTE and 0 <= x < x+w <= 16 and 0 <= y < y+h <= 24
        for yy in range(y, y+h):
            for xx in range(x, x+w):
                self.cells[yy][xx] = (part, color)
        return self

    def path(self, part, color, points):
        for x, y in points:
            self.rect(part, color, x, y)
        return self


def ground(variant):
    f = Frame('scraped-ground-' + str(variant), 'OverwritGround',
              'Smooth pale warm-grey ground; no stones, joins, per-cell border or light emission.',
              variant=variant, opaque=True, baseSelectionWeight=75 if variant == 0 else None)
    f.rect('smooth-ground', 'ground', 0, 0, 16, 24)
    if variant == 1:
        f.rect('rare-shallow-scuff', 'scuff', 3, 15, 3)
    elif variant == 2:
        f.rect('rare-shallow-scuff', 'scuff-light', 9, 6, 2)
        f.rect('rare-shallow-scuff', 'scuff', 10, 7)
    elif variant == 3:
        f.rect('rare-shallow-scuff', 'scuff', 7, 19, 2)
        f.rect('rare-shallow-scuff', 'scuff-light', 6, 18)
    return f


def shoots(variant):
    f = Frame('clipped-young-shoots-' + str(variant), 'OverwritNewGrowth',
              'Three small young shoots share the same flat cut height. Lower leaves vary; no flowers, seed beads or harvest promise.',
              variant=variant, solid=False, clippedTipY=15, stemTipXs=[4, 7, 11] if variant == 0 else [3, 8, 12])
    stems = ([(4, 4, 6), (7, 7, 8), (11, 11, 10)] if variant == 0
             else [(3, 3, 5), (8, 8, 7), (12, 12, 10)])
    for tip_x, upper_x, foot_x in stems:
        f.rect('young-stem', 'shoot-dark', upper_x, 15, 1, 5)
        f.rect('flat-clipped-tip', 'shoot-tip', tip_x, 15)
        f.rect('young-stem', 'shoot', upper_x, 16, 1, 3)
        direction = 1 if foot_x > upper_x else -1 if foot_x < upper_x else 0
        f.path('young-stem', 'shoot-dark', [(upper_x+direction, 20), (foot_x, 21), (foot_x, 22)])
    if variant == 0:
        f.path('low-young-leaf', 'shoot', [(3, 18), (4, 19), (5, 20), (6, 21), (9, 18), (9, 19), (8, 20), (12, 19), (11, 20)])
        f.rect('root-contact', 'shoot-dark', 5, 22, 6)
    else:
        f.path('low-young-leaf', 'shoot', [(5, 18), (4, 19), (5, 20), (6, 21), (6, 19), (7, 20), (10, 18), (11, 19), (10, 20)])
        f.rect('root-contact', 'shoot-dark', 4, 22, 7)
    return f


def bench():
    f = Frame('plain-bench-inward-west', 'OverwritPilgrimBench',
              'Plain one-cell wooden bench in side view. East/right backrest; west/left open seat faces the inward empty field. No resting/service emblem.',
              solid=True, facing='west', backrestSide='east', service=False)
    # Narrow right-side back, open left-side sitting space and two grounded feet.
    f.rect('east-backrest', 'wood-dark', 11, 10, 3, 12)
    f.rect('east-backrest', 'wood', 11, 11, 2, 7)
    f.rect('backrest-top-edge', 'wood-edge', 11, 10, 2)
    f.rect('seat-planks', 'wood-dark', 2, 17, 12, 3)
    f.rect('seat-planks', 'wood', 3, 17, 8, 2)
    f.rect('worn-seat-edge', 'wood-edge', 3, 17, 7)
    f.rect('restrained-grain', 'wood-dark', 5, 18, 3)
    f.rect('west-front-foot', 'wood-dark', 3, 20, 2, 3)
    f.rect('west-front-foot', 'wood', 3, 20, 1, 2)
    f.rect('east-rear-foot', 'wood-dark', 11, 20, 2, 3)
    f.rect('low-stretcher', 'wood-dark', 5, 21, 6)
    return f


def marker():
    f = Frame('short-blank-stone', 'OverwritWaymarker',
              'Short plain stone standing in the ground. A cool broad blank face, no letters, arrow, symbol or service. Not yet naturally observed.',
              solid=True, naturallyObserved=False, wreckage='Rubble')
    for y, x, width in [(10, 7, 3), (11, 5, 6), (12, 4, 8), (13, 4, 8),
                         (14, 4, 8), (15, 4, 8), (16, 4, 8), (17, 4, 8),
                         (18, 4, 8), (19, 4, 8), (20, 3, 10), (21, 3, 10), (22, 4, 8)]:
        f.rect('short-stone-body', 'stone-dark', x, y, width)
    f.rect('blank-stone-face', 'stone', 5, 12, 6, 9)
    f.rect('blank-stone-face', 'stone', 6, 11, 4)
    f.rect('soft-top-edge', 'stone-light', 7, 11, 3)
    f.rect('soft-left-edge', 'stone-light', 5, 12, 1, 5)
    return f


def frames():
    return [ground(i) for i in range(4)] + [shoots(0), shoots(1), bench(), marker()]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def preserved_guard():
    for rel, expected in json.loads((OUT / 'preserved-inputs.json').read_text()).items():
        assert sha(ROOT / rel) == expected, ('Protected source changed', rel)


def verify():
    """Decode-only artifact validation; never modifies a pixel, source or Unity."""
    from PIL import Image
    preserved_guard()
    source = json.loads((OUT / 'source-cells.json').read_text())
    manifest = json.loads((OUT / 'manifest.json').read_text())
    with Image.open(OUT / 'overwrit-materials.png') as png:
        assert png.size == (128, 24) and png.mode == 'RGBA'
        rgba = png.tobytes()
    assert len(source['frames']) == 8 and source['palette'] == PALETTE
    assert sha(OUT / 'overwrit-materials.png') == manifest['sha256']
    native_frames = []
    for i, frame in enumerate(source['frames']):
        assert frame['index'] == i
        expected = frames()[i]
        assert frame['cells'] == json.loads(json.dumps(expected.cells))
        decoded = bytearray()
        for y, row in enumerate(frame['cells']):
            for x, cell in enumerate(row):
                p = 4*(y*128+i*16+x)
                target = bytes.fromhex(PALETTE[cell[1]]) + b'\xff' if cell else b'\0\0\0\0'
                assert rgba[p:p+4] == target, (i, x, y, list(rgba[p:p+4]), list(target))
                decoded.extend(target)
        native_frames.append(bytes(decoded))
    assert len(set(native_frames)) == 8, 'All essential variants must differ.'
    assert all(c[1] == 'ground' for row in source['frames'][0]['cells'] for c in row)
    for i in (4, 5):
        cells = source['frames'][i]['cells']
        tips = [(x, y) for y, row in enumerate(cells) for x, c in enumerate(row) if c and c[0] == 'flat-clipped-tip']
        assert len(tips) == 3 and {y for x, y in tips} == {15}
        assert all(c is None for row in cells[:15] for c in row)
    assert all(c is None for f in source['frames'][4:] for c in f['cells'][23])
    for name, size in [('contact-native.png', (208, 216)), ('contact.png', (1664, 1728))]:
        with Image.open(OUT / name) as img:
            assert img.size == size
    result = {'passed': True, 'frameCount': 8, 'size': [128, 24], 'sha256': manifest['sha256'],
              'decodedRgbaSha256': hashlib.sha256(rgba).hexdigest(),
              'pixelMatchCount': 3072, 'preservedInputCount': len(json.loads((OUT/'preserved-inputs.json').read_text())),
              'scope': 'Decode-only source artifact validation; no Unity/native gameplay tests, import or gameplay acceptance.'}
    (OUT / 'verification.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result))


def build():
    import bpy
    sys.path.insert(0, str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for, add_label, emission_material
    from build_cindercaller import model
    from build_north_plot import instance, plane
    from build_east_kitchen import write_meta
    preserved_guard()
    fs = frames()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    native = bpy.context.scene
    native.name = 'Overwrit original native 128x24'
    cols = []
    for i, f in enumerate(fs):
        col = model(native, f'{i:02d} {f.name}', f.cells, PALETTE)
        native.collection.children.unlink(col)
        for obj in col.objects:
            obj['authoring'] = 'Original Overwrit named integer-cell geometry; no raster input.'
        instance(native, col, str(i), i*16, 0)
        cols.append(col)
    camera_for(native, 128, 24)
    native.render.filepath = str(OUT/'overwrit-materials.png')
    bpy.ops.render.render(write_still=True, scene=native.name)
    approved = json.loads((ROOT/'ArtSource/Live16x24/base/source-cells.json').read_text())
    player = model(native, 'Unchanged approved player comparison only', approved['frames'][0]['cells'], approved['palette'])
    native.collection.children.unlink(player)
    width, height = 208, 216
    placements = []
    for i in range(8):
        placements.append({'frame': i, 'topLeft': [16+(i%4)*48, 16+(i//4)*44]})
    # Exact 48-cell source study: 36 blank cells, four of each rare variant.
    study = [0]*48
    for i, variant in zip((1, 8, 13, 17, 20, 23, 26, 30, 35, 38, 42, 46), (1, 2, 3)*4):
        study[i] = variant
    for scale, name in ((1, 'contact-native'), (8, 'contact')):
        scene = bpy.data.scenes.new('Overwrit direct source contact '+str(scale)+'x')
        camera_for(scene, width, height, scale)
        plane(scene, 'Contact-only surround', '26322B', 0, 0, width, height, -3)
        label = emission_material('Contact label', 'C9C39A')
        add_label(scene, 'OVERWRIT / ORIGINAL STATIC SOURCE / 16x24', width/2, height-7, label, 2.1)
        for p in placements:
            x, y = p['topLeft']
            if p['frame'] >= 4:
                plane(scene, 'Pale context '+str(p['frame']), PALETTE['ground'], x, height-y-24, 16, 24, -1)
            instance(scene, cols[p['frame']], str(p), x, height-y-24)
            add_label(scene, ['0 blank', '1 scuff', '2 scuff', '3 scuff', '4 clipped', '5 clipped', '6 west bench', '7 blank stone'][p['frame']], x+8, height-y-30, label, 1.8)
        add_label(scene, '75% BLANK / APPROVED PLAYER UNCHANGED / SOURCE STUDY ONLY', width/2, height-101, label, 1.8)
        for n, fr in enumerate(study):
            instance(scene, cols[fr], 'Study ground '+str(n), 8+(n%12)*16, height-107-(n//12)*24-24, -1)
        instance(scene, player, 'Unchanged approved player', 120, 13)
        for n, fr, x, y in [(0, 4, 168, 37), (1, 5, 184, 61), (2, 4, 184, 13), (3, 6, 136, 13)]:
            instance(scene, cols[fr], 'Study owner '+str(n), x, y)
        scene.render.filepath = str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True, scene=scene.name)
    source = {'schema': 1, 'coordinateSystem': 'Top-left x right/y down; original named integer-cell geometry. Null means transparent.',
              'palette': PALETTE, 'frames': [{'index': i, 'id': f.name, 'blueprint': f.blueprint, 'state': f.state, 'cells': f.cells} for i, f in enumerate(fs)]}
    (OUT/'source-cells.json').write_text(json.dumps(source, indent=2)+'\n')
    manifest = {'schema': 1, 'artifact': 'overwrit-materials.png', 'resource': 'Sprites/Live16x24/overwrit-materials',
                'width': 128, 'height': 24, 'frameCount': 8, 'frameWidth': 16, 'frameHeight': 24,
                'pixelsPerUnit': 16, 'pivot': [.5, .5], 'filterMode': 'Point', 'wrapMode': 'Clamp',
                'mipmaps': False, 'compression': 'None', 'source': 'overwrit-materials.blend',
                'generator': 'Tools/Live16x24/build_overwrit_materials.py', 'palette': PALETTE,
                'unityExportDeferred': True, 'scope': 'Four exact Overwrit owners, eight static native cells. Marker has not been naturally observed. No animation or generation change. Actual gameplay acceptance remains root-owned.',
                'references': ['references.json', 'concepts/critique-and-parts.md', 'concepts/overwrit-v2.png', 'Docs/Verification/Live16x24/overwrit-native-preflight.md'],
                'contact': {'nativeSize': [width, height], 'scales': [1, 8], 'instances': placements, 'blankGroundWeight': .75, 'approvedComparisonOnly': 'ArtSource/Live16x24/base/source-cells.json frame0'},
                'sha256': sha(OUT/'overwrit-materials.png'),
                'frames': [{'index': i, 'id': f.name, 'blueprint': f.blueprint, 'description': f.description, 'state': f.state, 'unityRect': [i*16, 0, 16, 24]} for i, f in enumerate(fs)]}
    (OUT/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    write_meta(OUT/'overwrit-materials.png.meta')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'overwrit-materials.blend'))
    preserved_guard()
    print(json.dumps({'packetOnly': True, 'frameCount': 8, 'size': [128, 24], 'sha256': manifest['sha256']}))


if __name__ == '__main__':
    if '--verify' in sys.argv:
        verify()
    else:
        build()
