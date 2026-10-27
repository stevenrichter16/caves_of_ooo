"""Original native BrinePool identity: editable planar integer-cell geometry.

Blender --background --python Tools/Live16x24/build_kitchen_brine.py
No raster is opened, sampled, traced, cropped, resized or edited. References
are hashed for provenance only. Native PNG and both contacts are direct renders.
"""
import hashlib
import json
import shutil
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_quad, camera_for, emission_material
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/kitchen-brine'
RESOURCE = ROOT / 'Assets/Resources/Sprites/Live16x24/kitchen-brine.png'
PALETTE = {
    'mud': '233C32', 'silt': '526657', 'salt': '8D9B78',
    'deep_teal': '063638', 'brine': '0C4344', 'quiet_teal': '184B45',
}
REFERENCES = (
    'ArtSource/Live16x24/kitchen-brine/concept.png',
    'ArtSource/Live16x24/kitchen-brine/imagegen-prompt.txt',
    'ArtSource/Live16x24/kitchen-brine/references.md',
    'Docs/Verification/Live16x24/kitchen-brine-02-before-world-2x.jpg',
    'ArtSource/Live16x24/east-kitchen/source-cells.json',
    'ArtSource/Live16x24/east-kitchen/east-kitchen-contact.png',
)


def make_cells():
    cells = [[None] * 16 for _ in range(24)]

    def rect(part, color, x, y, width, height=1):
        assert color in PALETTE and 0 <= x < x+width <= 16 and 0 <= y < y+height <= 24
        for yy in range(y, y+height):
            for xx in range(x, x+width): cells[yy][xx] = [part, color]

    # Unequal shoulders and low margins: a ground puddle, not a circular vessel.
    for y, x, width in ((10,8,3), (11,5,7), (12,4,9), (13,3,11), (14,2,12),
                        (15,1,13), (16,1,14), (17,2,13), (18,2,12), (19,3,10),
                        (20,4,9), (21,5,7), (22,7,3)):
        rect('irregular-mud-edge', 'mud', x, y, width)
    for y, x, width in ((11,7,4), (12,5,7), (13,4,9), (14,3,10), (15,2,11),
                        (16,2,12), (17,3,11), (18,3,10), (19,4,8), (20,5,7),
                        (21,6,5)):
        rect('flat-brine-interior', 'brine', x, y, width)
    # One irregular dark pocket, without a repeated ripple or directional light.
    rect('quiet-water-pocket', 'deep_teal', 3, 16, 3)
    rect('quiet-water-pocket', 'deep_teal', 4, 17, 3)
    rect('quiet-water-pocket', 'deep_teal', 5, 18, 2)
    # Interrupted mineral deposits, never an all-around bright raised rim.
    for x, y, width in ((8,10,3), (5,11,2), (4,12,1), (3,13,1),
                        (13,14,1), (13,15,1), (14,16,1), (13,18,1),
                        (5,21,2), (7,22,3)):
        rect('mineral-silt', 'silt', x, y, width)
    for x, y, width in ((8,10,2), (5,11,1), (4,12,1), (13,14,1),
                        (14,16,1), (13,18,1), (5,21,1), (8,22,2)):
        rect('salt-crust', 'salt', x, y, width)
    rect('quiet-water-mark', 'quiet_teal', 7, 14, 3)
    return cells


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    cells = make_cells()
    materials = {name: emission_material(name+' #'+value, value) for name, value in PALETTE.items()}
    collection = bpy.data.collections.new('BrinePool original neutral material identity')
    groups = {}
    for y, row in enumerate(cells):
        for x, cell in enumerate(row):
            if not cell: continue
            verts, faces = groups.setdefault(tuple(cell), ([], []))
            add_quad(verts, faces, x, 23-y, x+1, 24-y)
    for (part, color), (verts, faces) in sorted(groups.items()):
        mesh = bpy.data.meshes.new(part)
        mesh.from_pydata(verts, [], faces); mesh.materials.append(materials[color]); mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj['paletteHex'] = PALETTE[color]
        obj['authoring'] = 'Original named native integer cells; flat unlit quads.'
        collection.objects.link(obj)
    native = bpy.context.scene; native.name = 'BrinePool native16x24'
    native.collection.children.link(collection); camera_for(native, 16, 24)
    native.render.filepath = str(OUT / 'native.png')
    bpy.ops.render.render(write_still=True, scene=native.name)
    for scale, name in ((1, 'contact-native'), (8, 'contact')):
        contact = bpy.data.scenes.new('BrinePool geometry on approved ground '+str(scale)+'x')
        camera_for(contact, 32, 32, scale=scale)
        plane(contact, 'Approved quiet preview ground', '032F23', 0, 0, 32, 32, -1)
        instance(contact, collection, 'One single-cell brine identity', 8, 4)
        contact.render.filepath = str(OUT / (name+'.png'))
        bpy.ops.render.render(write_still=True, scene=contact.name)
    source = {'schema': 1, 'coordinates': '16x24 top-left origin, y downward; null transparent.',
              'palette': PALETTE, 'frames': [{'index': 0, 'id': 'brine-pool-neutral',
              'blueprint': 'BrinePool', 'state': 'identity', 'cells': cells}]}
    (OUT / 'source-cells.json').write_text(json.dumps(source, indent=2)+'\n')
    manifest = {
        'schema': 1, 'frameWidth': 16, 'frameHeight': 24, 'frameCount': 1,
        'blueprint': 'BrinePool', 'state': 'identity', 'footprintCells': [1, 1],
        'baselineRow': 22, 'pivot': [.5, .5], 'pixelsPerUnit': 16,
        'alpha': 'Binary0/255', 'filter': 'Point', 'wrap': 'Clamp', 'mipmaps': False,
        'compression': 'None', 'source': 'native.blend', 'editableCells': 'source-cells.json',
        'generator': 'Tools/Live16x24/build_kitchen_brine.py', 'native': 'native.png',
        'contacts': ['contact-native.png', 'contact.png'],
        'resource': 'Sprites/Live16x24/kitchen-brine',
        'nativeSHA256': hashlib.sha256((OUT / 'native.png').read_bytes()).hexdigest(),
        'referenceSHA256': {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in REFERENCES},
        'parts': ['asymmetric dark mud edge', 'flat dark teal interior',
                  'interrupted muted salt deposits', 'one quiet water mark'],
        'sourceEvidence': {'red': 'source-red.txt', 'green': 'source-green.txt', 'verification': 'verification.json'},
        'paletteProvenance': 'Water0C4344/184B45 from approved Ground; mud233C32, silt526657 and salt8D9B78 from approved stone/shrine grammar. Deep063638 is one original low-contrast water pocket.',
        'rejected': ['uniform raised rim', 'clay bowl', 'noisy highlights', 'soft alpha',
                     '3D lighting', 'volume states', 'ice', 'charge', 'flow', 'steam', 'bubbles'],
        'limits': 'One static material identity only; no contents quantity, potability, available liquid, freeze or charge promise. Runtime admission, hazard overlays, owner disclosure, Unity import and actual gameplay acceptance remain root-owned.',
    }
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'native.blend'))
    shutil.copyfile(OUT / 'native.png', RESOURCE)
    write_meta(Path(str(RESOURCE)+'.meta'))
    print('Wrote single-cell BrinePool native source:', manifest['nativeSHA256'])


if __name__ == '__main__': main()
