"""Original RiverShrine integer-cell geometry. Run with Blender --background --python.

No raster sampling, editing, tracing or resizing. Runtime transfer remains root-owned.
"""
import hashlib
import json
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_quad, camera_for, emission_material
from build_north_plot import instance, plane

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/river-shrine'
PALETTE = {'recess': '233C32', 'dark_stone': '3C5148', 'stone': '526657',
           'worn_edge': '8D9B78', 'token': 'C99538'}
CONCEPT = 'ArtSource/Live16x24/river-shrine/concepts/shrine-scene-reference.png'


def make_cells():
    cells = [[None for _ in range(16)] for _ in range(24)]
    def rect(part, color, x, y, w, h):
        assert color in PALETTE and 0 <= x < x+w <= 16 and 0 <= y < y+h <= 24
        for yy in range(y, y+h):
            for xx in range(x, x+w): cells[yy][xx] = (part, color)
    for dy, (x, width) in enumerate(((6,4), (5,6), (4,8), (4,9), (3,10), (3,10),
                                   (2,12), (1,14), (1,14), (1,14), (2,12), (2,11), (4,8))):
        rect('worn-shrine-stone', 'stone' if dy < 7 else 'dark_stone', x, 10+dy, width, 1)
    # Few large planes of worn material, not all-around bevel lighting.
    rect('back-stone-wear', 'worn_edge', 6, 10, 3, 1)
    rect('left-back-stone', 'stone', 5, 11, 2, 3)
    rect('right-back-break', 'dark_stone', 10, 12, 2, 2)
    rect('right-shoulder', 'dark_stone', 11, 14, 2, 3)
    rect('left-worn-stone', 'worn_edge', 3, 16, 2, 1)
    rect('left-worn-stone', 'worn_edge', 2, 17, 1, 1)
    rect('left-flat-stone', 'stone', 2, 18, 3, 2)
    rect('left-base-break', 'recess', 2, 20, 2, 1)
    rect('right-flat-stone', 'stone', 12, 17, 2, 3)
    rect('right-worn-stone', 'worn_edge', 12, 17, 1, 1)
    rect('low-front-stone', 'stone', 5, 19, 6, 2)
    rect('low-worn-cut', 'worn_edge', 5, 19, 4, 1)
    rect('low-base', 'dark_stone', 4, 21, 7, 1)
    rect('low-base-break', 'recess', 4, 22, 8, 1)
    # A single shallow carved recess, with one fixed token/inlay. Not inventory.
    rect('carved-recess-rim', 'dark_stone', 5, 14, 6, 5)
    rect('carved-recess', 'recess', 6, 15, 4, 4)
    rect('carved-token', 'token', 7, 15, 2, 2)
    return cells


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    cells = make_cells()
    materials = {name: emission_material(name+' #'+hex_value, hex_value) for name, hex_value in PALETTE.items()}
    collection = bpy.data.collections.new('RiverShrine neutral single-cell identity')
    groups = {}
    for y, row in enumerate(cells):
        for x, cell in enumerate(row):
            if cell is None: continue
            vertices, faces = groups.setdefault(cell, ([], []))
            add_quad(vertices, faces, x, 23-y, x+1, 24-y)
    for (part, color), (vertices, faces) in sorted(groups.items()):
        mesh = bpy.data.meshes.new(part)
        mesh.from_pydata(vertices, [], faces); mesh.materials.append(materials[color]); mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj['paletteHex'] = PALETTE[color]
        obj['authoring'] = 'Original named planar integer cells; no sampled raster'
        collection.objects.link(obj)
    native = bpy.context.scene; native.name = 'RiverShrine native16x24'
    native.collection.children.link(collection); camera_for(native, 16, 24)
    native.render.filepath = str(OUT / 'native.png')
    bpy.ops.render.render(write_still=True, scene=native.name)
    contact = bpy.data.scenes.new('RiverShrine direct8x on approved ground')
    camera_for(contact, 32, 32, scale=8)
    plane(contact, 'Approved dark green preview ground', '032F23', 0, 0, 32, 32, -1)
    instance(contact, collection, 'One authored shrine', 8, 4)
    contact.render.filepath = str(OUT / 'contact.png')
    bpy.ops.render.render(write_still=True, scene=contact.name)
    source = {'schema': 1, 'coordinates': '16x24 top-left origin; y increases down; null transparent.',
              'palette': PALETTE, 'frames': [{'index': 0, 'id': 'river-shrine-neutral', 'blueprint': 'RiverShrine', 'cells': cells}]}
    (OUT / 'source-cells.json').write_text(json.dumps(source, indent=2)+'\n')
    manifest = {'schema': 1, 'blueprint': 'RiverShrine', 'part': 'SanctuaryPart', 'stateCount': 1,
                'frameWidth': 16, 'frameHeight': 24, 'footprint': [1,1], 'baselineRow': 22,
                'pivot': [.5,.5], 'pixelsPerUnit': 16, 'alpha': 'binary0/255', 'filter': 'Point', 'wrap': 'Clamp', 'mipmaps': False,
                'source': 'editable.blend', 'editableCells': 'source-cells.json',
                'generator': 'Tools/Live16x24/build_river_shrine.py', 'native': 'native.png', 'contact': 'contact.png',
                'proposedResource': 'Sprites/Live16x24/river-shrine', 'runtimeTransferOwner': 'root',
                'nativeSHA256': hashlib.sha256((OUT / 'native.png').read_bytes()).hexdigest(),
                'concept': CONCEPT, 'conceptPrompt': 'ArtSource/Live16x24/river-shrine/concepts/prompt.txt',
                'greyboxCapture': 'Docs/Verification/Live16x24/current-scene-11-shrine-before.png',
                'parts': ['worn-stone silhouette', 'one recessed panel', 'one fixed muted ochre carved token'],
                'mapping': 'Stable non-solid nonportable current RiverShrine owner with SanctuaryPart; donation changes actor only, not shrine art.',
                'rejected': ['water contents', 'glow', 'loose coins', 'inventory quantities', 'donation animation', 'soft alpha', 'simulated 3D lighting'],
                'limits': 'Native source only. Root owns runtime file/import, resolver tests, actual game integration and visual acceptance.'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'editable.blend'))
    print('Native RiverShrine transfer:', OUT / 'native.png')


if __name__ == '__main__': main()
