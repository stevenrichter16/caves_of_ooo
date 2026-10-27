"""Decode-only native art checks. Writes only this packet's verification receipt."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
RESOURCE = ROOT / 'Assets/Resources/Sprites/Live16x24/kitchen-brine.png'
REQUIRED = ('native.blend', 'native.png', 'contact.png', 'contact-native.png',
            'source-cells.json', 'manifest.json')
missing = [name for name in REQUIRED if not (HERE / name).is_file()]
assert not missing, 'Source RED: native brine packet missing: ' + ', '.join(missing)

source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source['palette'].items()}
assert len(palette) == 6 and len(set(palette.values())) == 6
assert len(source['frames']) == 1
frame = source['frames'][0]
assert frame['blueprint'] == 'BrinePool' and frame['state'] == 'identity'
cells = frame['cells']
assert len(cells) == 24 and all(len(row) == 16 for row in cells)
native = Image.open(HERE / 'native.png').convert('RGBA')
contact = Image.open(HERE / 'contact.png').convert('RGBA')
contact_native = Image.open(HERE / 'contact-native.png').convert('RGBA')
assert native.size == (16, 24)
assert contact.size == (256, 256) and contact_native.size == (32, 32)
assert set(native.getchannel('A').tobytes()) == {0, 255}
assert set(contact.getchannel('A').tobytes()) == {255}

occupied, salt, reflection, used = set(), set(), set(), set()
ground = tuple(bytes.fromhex('032F23')) + (255,)
for y, row in enumerate(cells):
    for x, cell in enumerate(row):
        expected = palette[cell[1]] if cell else (0, 0, 0, 0)
        assert native.getpixel((x, y)) == expected, ('native/source mismatch', x, y)
        display = expected if expected[3] else ground
        assert contact_native.getpixel((8+x, 4+y)) == display, ('1x contact mismatch', x, y)
        assert all(contact.getpixel(((8+x)*8+dx, (4+y)*8+dy)) == display
                   for dx in range(8) for dy in range(8)), ('8x contact mismatch', x, y)
        if cell:
            occupied.add((x, y)); used.add(cell[1])
            if cell[0] == 'salt-crust': salt.add((x, y))
            if cell[0] == 'quiet-water-mark': reflection.add((x, y))
assert used == set(palette)
assert all(1 <= x <= 14 and 10 <= y <= 22 for x, y in occupied)
assert max(y for x, y in occupied) == 22
assert 110 <= len(occupied) <= 160
assert 6 <= len(salt) <= 16, 'Salt is a few quiet deposits, not a white continuous rim'
assert 2 <= len(reflection) <= 4 and len({y for x, y in reflection}) == 1
assert len({x for x, y in reflection}) == max(x for x, y in reflection)-min(x for x, y in reflection)+1
pending = set(occupied)
frontier = [pending.pop()]
while frontier:
    x, y = frontier.pop()
    for q in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
        if q in pending: pending.remove(q); frontier.append(q)
assert not pending, 'One real pool identity, no detached salt props or debris'
assert not all(((15-x, y) in occupied) for x, y in occupied), 'Natural asymmetric boundary'
assert not all(((x, 32-y) in occupied) for x, y in occupied), 'No symmetrical constructed bowl'

digest = hashlib.sha256((HERE / 'native.png').read_bytes()).hexdigest()
assert manifest['nativeSHA256'] == digest
assert manifest['frameCount'] == 1 and manifest['resource'] == 'Sprites/Live16x24/kitchen-brine'
assert manifest['baselineRow'] == 22 and manifest['footprintCells'] == [1, 1]
assert RESOURCE.is_file() and RESOURCE.read_bytes() == (HERE / 'native.png').read_bytes()
meta = Path(str(RESOURCE)+'.meta').read_text()
for token in ('enableMipMap: 0', 'filterMode: 0', 'wrapU: 1', 'wrapV: 1', 'wrapW: 1',
              'nPOTScale: 0', 'textureCompression: 0', 'spritePixelsToUnits: 16'):
    assert token in meta, token
assert re.search(r'^guid: [0-9a-f]{32}$', meta, re.M)
for path, sha in manifest['referenceSHA256'].items():
    assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == sha, ('Reference changed', path)
report = {'passed': True, 'nativeSize': [16, 24], 'frameCount': 1,
          'opaquePixels': len(occupied), 'paletteSize': len(used), 'alphaValues': [0, 255],
          'saltPixels': len(salt), 'quietReflectionPixels': len(reflection),
          'sourcePixelMismatches': 0, 'contactPixelMismatches': 0,
          'resourceBytesMatchSource': True, 'importMetadataValid': True,
          'nativeSHA256': digest,
          'limits': 'Native geometry/render/resource source checks only. Root owns runtime admission, reaction overlays, actual Unity import and gameplay acceptance.'}
(HERE / 'verification.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
