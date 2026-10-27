"""Decode-only Cudgel source checks; writes only this packet's verification receipt.

The optional --check-unity gate is for root after copying/importing the packet.
Default source checks neither read runtime art nor require a Unity refresh.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--check-unity', action='store_true')
args = parser.parse_args()
required = ('native.blend', 'native.png', 'native.png.meta', 'contact-native.png',
            'contact.png', 'source-cells.json', 'manifest.json', 'preserved-inputs.json')
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, 'Source RED: native Cudgel packet missing: ' + ', '.join(missing)

source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
expected_hex = {'outline': '342E25', 'shadow': '5F4830',
                'wood': '80603B', 'light': '9B7848'}
assert source['palette'] == expected_hex, 'Exactly the four approved wood colors'
palette = {name: tuple(bytes.fromhex(value)) + (255,) for name, value in expected_hex.items()}
assert len(source['frames']) == 1
frame = source['frames'][0]
assert frame['blueprint'] == 'Cudgel' and frame['state'] == 'identity'
cells = frame['cells']
assert len(cells) == 24 and all(len(row) == 16 for row in cells)
native = Image.open(HERE / 'native.png').convert('RGBA')
contact_native = Image.open(HERE / 'contact-native.png').convert('RGBA')
contact = Image.open(HERE / 'contact.png').convert('RGBA')
assert native.size == (16, 24)
assert contact_native.size == (32, 32) and contact.size == (256, 256)
assert set(native.getchannel('A').tobytes()) == {0, 255}
assert set(contact_native.getchannel('A').tobytes()) == {255}
assert set(contact.getchannel('A').tobytes()) == {255}
ground = tuple(bytes.fromhex('032F23')) + (255,)
occupied, used, parts = set(), set(), {}
for y, row in enumerate(cells):
    for x, cell in enumerate(row):
        expected = palette[cell[1]] if cell else (0, 0, 0, 0)
        assert native.getpixel((x, y)) == expected, ('native/source mismatch', x, y)
        shown = expected if expected[3] else ground
        assert contact_native.getpixel((8+x, 4+y)) == shown, ('1x contact mismatch', x, y)
        assert all(contact.getpixel(((8+x)*8+dx, (4+y)*8+dy)) == shown
                   for dx in range(8) for dy in range(8)), ('8x contact mismatch', x, y)
        if cell:
            occupied.add((x, y)); used.add(cell[1])
            parts.setdefault(cell[0], set()).add((x, y))
assert used == set(palette), 'Four discrete flat wood masses, no imported shading'
assert set(parts) == {'striking-head', 'tapered-shoulder', 'smooth-grip'}
assert all(1 <= x <= 14 and 2 <= y <= 22 for x, y in occupied)
xs, ys = zip(*occupied)
assert 18 <= max(ys)-min(ys)+1 <= 20, 'Shorter than the approved 22-pixel LongSword'
assert 60 <= len(occupied) <= 100, 'Readable blunt club without a solid oversized icon'
assert sum(x for x, y in parts['striking-head']) / len(parts['striking-head']) > sum(x for x, y in parts['smooth-grip']) / len(parts['smooth-grip'])
assert max(y for x, y in parts['striking-head']) < min(y for x, y in parts['smooth-grip'])
row_widths = {y: max(x for x, yy in occupied if yy == y)-min(x for x, yy in occupied if yy == y)+1 for y in set(ys)}
head_rows = {y for x, y in parts['striking-head']}
grip_rows = {y for x, y in parts['smooth-grip']}
assert 5 <= max(row_widths[y] for y in head_rows) <= 6, 'Wider blunt striking end'
assert all(row_widths[y] <= 3 for y in grip_rows), 'Narrow hand-smoothed grip'
assert 2 <= row_widths[min(ys)] <= 4, 'Blunt tip, never a point or detached glint'
pending = set(occupied)
frontier = [pending.pop()]
while frontier:
    x, y = frontier.pop()
    for q in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
        if q in pending:
            pending.remove(q); frontier.append(q)
assert not pending, 'One connected wooden implement, no detached debris or effect'

digest = hashlib.sha256((HERE / 'native.png').read_bytes()).hexdigest()
pixel_digest = hashlib.sha256(native.tobytes()).hexdigest()
assert manifest['nativeSHA256'] == digest and manifest['decodedPixelSHA256'] == pixel_digest
assert manifest['frameCount'] == 1 and manifest['resource'] == 'Sprites/Live16x24/cudgel'
assert manifest['blueprint'] == 'Cudgel' and manifest['state'] == 'identity'
assert manifest['footprintCells'] == [1, 1] and manifest['pivot'] == [.5, .5]
assert manifest['pixelsPerUnit'] == 16 and manifest['unityExportDeferred'] is True
assert manifest['palette'] == expected_hex
meta = (HERE / 'native.png.meta').read_text()
for token in ('enableMipMap: 0', 'filterMode: 0', 'wrapU: 1', 'wrapV: 1', 'wrapW: 1',
              'nPOTScale: 0', 'textureCompression: 0', 'spritePixelsToUnits: 16',
              'spritePivot: {x: 0.5, y: 0.5}'):
    assert token in meta, token
assert re.search(r'^guid: [0-9a-f]{32}$', meta, re.M)
preserved = json.loads((HERE / 'preserved-inputs.json').read_text())
for path, sha in preserved.items():
    assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == sha, ('Reference or accepted source changed', path)
assert manifest['referenceSHA256'] == preserved
if args.check_unity:
    from metadata_contract import assert_authored_subset, authored_contract_counts, parse_metadata
    resource = ROOT / 'Assets/Resources/Sprites/Live16x24/cudgel.png'
    assert resource.is_file() and resource.read_bytes() == (HERE / 'native.png').read_bytes()
    authored_metadata = parse_metadata(meta)
    imported_metadata = parse_metadata(Path(str(resource)+'.meta').read_text())
    assert_authored_subset(authored_metadata, imported_metadata)
report = {'passed': True, 'nativeSize': [16, 24], 'frameCount': 1,
          'opaquePixels': len(occupied), 'paletteSize': len(used), 'alphaValues': [0, 255],
          'boundsTopLeftExclusive': [min(xs), min(ys), max(xs)+1, max(ys)+1],
          'parts': {name: len(points) for name, points in parts.items()},
          'sourcePixelMismatches': 0, 'contactPixelMismatches': 0,
          'packetImportMetadataValid': True, 'preservedInputCount': len(preserved),
          'nativeSHA256': digest, 'decodedPixelSHA256': pixel_digest,
          'runtimeCopyChecked': args.check_unity,
          'limits': 'Native geometry/render/packet checks only. No Unity write, import, runtime admission, live gameplay, or physical Deck acceptance.'}
if args.check_unity:
    report['runtimeMetadataAuthoredContract'] = authored_contract_counts(authored_metadata)
    report['runtimeMetadataComparison'] = 'Every authored value and structure retained; additional mapping defaults and YAML whitespace allowed.'
(HERE / 'verification.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
