"""Read-only source/render checks; writes only this packet's result receipt."""
import hashlib, json
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
required = ('editable.blend', 'source-cells.json', 'manifest.json', 'native.png', 'contact.png')
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, 'Source RED: native shrine packet missing: ' + ', '.join(missing)
source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source['palette'].items()}
native = Image.open(HERE / 'native.png').convert('RGBA')
contact = Image.open(HERE / 'contact.png').convert('RGBA')
assert native.size == (16, 24) and contact.size == (256, 256)
assert set(native.getchannel('A').tobytes()) == {0, 255}
cells = source['frames'][0]['cells']
assert len(source['frames']) == 1 and len(cells) == 24 and all(len(row) == 16 for row in cells)
occupied, token, used = set(), set(), set()
grass = tuple(bytes.fromhex('032F23')) + (255,)
for y, row in enumerate(cells):
    for x, cell in enumerate(row):
        expected = palette[cell[1]] if cell else (0, 0, 0, 0)
        assert native.getpixel((x, y)) == expected, ('native/source mismatch', x, y)
        if cell:
            occupied.add((x, y)); used.add(cell[1])
            if cell[0] == 'carved-token': token.add((x, y))
        display = expected if expected[3] else grass
        assert all(contact.getpixel(((8 + x) * 8 + dx, (4 + y) * 8 + dy)) == display
                   for dx in range(8) for dy in range(8)), ('direct-contact mismatch', x, y)
assert used == set(palette)
assert min(x for x, y in occupied) >= 1 and max(x for x, y in occupied) <= 14
assert min(y for x, y in occupied) >= 10 and max(y for x, y in occupied) == 22
assert 12 <= max(x for x, y in occupied) - min(x for x, y in occupied) + 1 <= 14
assert 10 <= max(y for x, y in occupied) - min(y for x, y in occupied) + 1 <= 13
assert token == {(7, 15), (8, 15), (7, 16), (8, 16)}
pending = set(occupied); frontier = [pending.pop()]
while frontier:
    x, y = frontier.pop()
    for q in ((x-1,y), (x+1,y), (x,y-1), (x,y+1)):
        if q in pending: pending.remove(q); frontier.append(q)
assert not pending, 'Shrine should be one connected stone/recess silhouette, not loose coins or debris'
digest = hashlib.sha256((HERE / 'native.png').read_bytes()).hexdigest()
assert digest == manifest['nativeSHA256']
assert manifest['blueprint'] == 'RiverShrine' and manifest['stateCount'] == 1
assert manifest['footprint'] == [1, 1] and manifest['baselineRow'] == 22
report = {'passed': True, 'nativeSize': [16,24], 'alphaValues': [0,255],
          'opaquePixels': len(occupied), 'tokenPixels': len(token), 'connectedSilhouettes': 1,
          'sourcePixelMismatches': 0, 'contactPixelMismatches': 0, 'nativeSHA256': digest,
          'limits': 'Source geometry and direct Blender renders only. Runtime import, owner admission, in-game readability, and gameplay remain root-owned.'}
(HERE / 'verification.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))
