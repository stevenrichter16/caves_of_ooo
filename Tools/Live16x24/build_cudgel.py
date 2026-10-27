"""Original native Cudgel: one editable integer-cell wooden item.

Blender --background --python Tools/Live16x24/build_cudgel.py -- --defer-unity
All outputs stay in ArtSource/Live16x24/cudgel. Unity export is always deferred;
root copies the final PNG and metadata after stopping its active Play session.
No raster is opened, sampled, traced, resized or edited. References are hashed
only. Native and both contacts are independent direct geometry renders.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import camera_for
from build_cindercaller import model
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/cudgel'
PALETTE = {'outline': '342E25', 'shadow': '5F4830',
           'wood': '80603B', 'light': '9B7848'}


def make_cells():
    cells = [[None] * 16 for _ in range(24)]

    def dot(part, color, x, y):
        assert color in PALETTE and 0 <= x < 16 and 0 <= y < 24
        cells[y][x] = [part, color]

    def span(part, y, x, width):
        # A connected flat face, not gradients, wood grain, or baked 3D light.
        for xx in range(x, x+width):
            dot(part, 'shadow', xx, y)
        dot(part, 'outline', x+width-1, y)
        for xx in range(x, x+width-2):
            dot(part, 'wood', xx, y)

    # Unequal blunt shoulders distinguish a worked wooden club from a pointed
    # stake or a metal mace. Deliberately wider than the smooth grip below.
    for y, x, width in ((3,10,3), (4,9,5), (5,9,6), (6,8,6),
                        (7,8,6), (8,7,6), (9,7,5)):
        span('striking-head', y, x, width)
    dot('striking-head', 'shadow', 10, 3)
    dot('striking-head', 'wood', 11, 3)
    for x, y in ((10,4), (11,4), (9,5), (9,6), (8,7), (8,8)):
        dot('striking-head', 'light', x, y)

    # One continuous tapered shoulder, with no blade, collar, strap or rivets.
    for y, x, width in ((10,6,5), (11,6,4), (12,5,4), (13,5,3)):
        span('tapered-shoulder', y, x, width)
    for x, y in ((7,10), (6,11)):
        dot('tapered-shoulder', 'light', x, y)

    # Two-row diagonal steps make a narrow, unwrapped hand-smoothed grip. The
    # compact square butt closes the silhouette without an invented metal cap.
    for y, x, width in ((14,4,3), (15,4,3), (16,3,3), (17,3,3),
                        (18,2,3), (19,2,3), (20,2,2), (21,2,1)):
        span('smooth-grip', y, x, width)
    dot('smooth-grip', 'wood', 2, 20)
    return cells


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--defer-unity', action='store_true',
                        help='Explicitly confirm packet-only export (also the safe default).')
    parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    OUT.mkdir(parents=True, exist_ok=True)
    preserved = json.loads((OUT / 'preserved-inputs.json').read_text())
    for path, digest in preserved.items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    native = bpy.context.scene
    native.name = 'Cudgel original neutral16x24'
    cells = make_cells()
    collection = model(native, 'Cudgel / original wooden item', cells, PALETTE)
    for obj in collection.objects:
        obj['authoring'] = 'Original named integer cells; neutral wooden Cudgel; direct flat native render.'
    camera_for(native, 16, 24)
    native.render.filepath = str(OUT / 'native.png')
    bpy.ops.render.render(write_still=True, scene=native.name)
    for scale, name in ((1, 'contact-native'), (8, 'contact')):
        contact = bpy.data.scenes.new('Cudgel on approved ground '+str(scale)+'x')
        camera_for(contact, 32, 32, scale=scale)
        plane(contact, 'Approved quiet Spread ground', '032F23', 0, 0, 32, 32, -1)
        instance(contact, collection, 'One neutral single-cell club', 8, 4)
        contact.render.filepath = str(OUT / (name+'.png'))
        bpy.ops.render.render(write_still=True, scene=contact.name)
    source = {'schema': 1, 'coordinates': '16x24 top-left origin, y downward; null transparent.',
              'palette': PALETTE, 'frames': [{'index': 0, 'id': 'cudgel-neutral',
              'blueprint': 'Cudgel', 'state': 'identity', 'cells': cells}]}
    (OUT / 'source-cells.json').write_text(json.dumps(source, indent=2)+'\n')
    # This is source-packet metadata, not a write beneath Assets. It remains
    # stable on rebuild; root later copies these exact bytes at integration.
    write_meta(OUT / 'native.png.meta')
    points = [(x, y) for y, row in enumerate(cells) for x, cell in enumerate(row) if cell]
    # Compute expected decoded bytes directly from the integer-cell contract;
    # the independent verifier compares them against the actual rendered PNG.
    rgba = {name: bytes.fromhex(value)+b'\xff' for name, value in PALETTE.items()}
    pixels = b''.join(rgba[cell[1]] if cell else b'\0\0\0\0' for row in cells for cell in row)
    manifest = {
        'schema': 1, 'frameWidth': 16, 'frameHeight': 24, 'frameCount': 1,
        'blueprint': 'Cudgel', 'state': 'identity', 'footprintCells': [1, 1],
        'pivot': [.5, .5], 'pixelsPerUnit': 16, 'alpha': 'Binary0/255',
        'filter': 'Point', 'wrap': 'Clamp', 'mipmaps': False, 'compression': 'None',
        'palette': PALETTE, 'source': 'native.blend', 'editableCells': 'source-cells.json',
        'generator': 'Tools/Live16x24/build_cudgel.py', 'native': 'native.png',
        'importMetadata': 'native.png.meta', 'contacts': ['contact-native.png', 'contact.png'],
        'resource': 'Sprites/Live16x24/cudgel', 'unityExportDeferred': True,
        'boundsTopLeftExclusive': [min(x for x, y in points), min(y for x, y in points),
                                   max(x for x, y in points)+1, max(y for x, y in points)+1],
        'opaquePixels': len(points), 'nativeSHA256': hashlib.sha256((OUT / 'native.png').read_bytes()).hexdigest(),
        'decodedPixelSHA256': hashlib.sha256(pixels).hexdigest(),
        'referenceSHA256': preserved,
        'parts': ['striking-head', 'tapered-shoulder', 'smooth-grip'],
        'sourceEvidence': {'red': 'source-red.txt', 'green': 'source-green.txt', 'verification': 'verification.json'},
        'paletteProvenance': 'Four explicit warm colors from the accepted incidental-equipment source; no sampled raster pixels.',
        'rejected': ['concept resizing', 'tracing', 'soft shading', 'invented UI', 'metal',
                     'spikes', 'wrapping', 'wood-grain noise', 'damage', 'repair', 'glow', 'animation'],
        'limits': 'One neutral exact Cudgel identity. Source packet only: root owns Assets copy/import, runtime admission, custody, visibility and actual gameplay acceptance.',
    }
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'native.blend'))
    print(json.dumps({'packetOnly': True, 'resourceWritten': False,
                      'opaquePixels': len(points), 'nativeSHA256': manifest['nativeSHA256'],
                      'decodedPixelSHA256': manifest['decodedPixelSHA256']}))


if __name__ == '__main__':
    main()
