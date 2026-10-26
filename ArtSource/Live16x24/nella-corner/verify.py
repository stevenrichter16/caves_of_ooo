"""Read native/source geometry without editing any raster or shared asset."""
import hashlib
import json
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
required = ('editable.blend', 'source-cells.json', 'manifest.json', 'native.png',
            'contact-native.png', 'contact-6x.png', 'corner-native.png', 'corner-4x.png')
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, 'Source RED: Nella corner missing ' + ', '.join(missing)
source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source['palette'].items()}
native = Image.open(HERE / 'native.png').convert('RGBA')
assert native.size == (272, 24)
assert set(native.getchannel('A').tobytes()) == {0, 255}
assert len(source['frames']) == 17
frames = source['frames']
stats = []
for i, frame in enumerate(frames):
    assert frame['index'] == i
    cells = frame['cells']; assert len(cells) == 24 and all(len(row) == 16 for row in cells)
    occupied = set(); colors = set()
    for y, row in enumerate(cells):
        for x, cell in enumerate(row):
            expected = palette[cell[1]] if cell else (0, 0, 0, 0)
            assert native.getpixel((16 * i + x, y)) == expected, ('native/source mismatch', i, x, y)
            if cell: occupied.add((x, y)); colors.add(cell[1])
    assert 3 <= len(colors) <= 5
    if i < 16:
        # All pipe geometry must be one connected metal silhouette.
        pending = set(occupied); frontier = [pending.pop()]
        while frontier:
            x, y = frontier.pop()
            for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if q in pending: pending.remove(q); frontier.append(q)
        assert not pending, ('disconnected pipe', i)
        if i:
            assert {x for x in range(16) if cells[0][x]} == (set(range(6,10)) if i & 1 else set()), ('north extent', i)
            assert {y for y in range(24) if cells[y][15]} == (set(range(10,14)) if i & 2 else set()), ('east extent', i)
            assert {x for x in range(16) if cells[23][x]} == (set(range(6,10)) if i & 4 else set()), ('south extent', i)
            assert {y for y in range(24) if cells[y][0]} == (set(range(10,14)) if i & 8 else set()), ('west extent', i)
    else:
        assert 35 <= len(occupied) <= 80, 'Peat must retain ample transparent water gaps.'
        assert min(y for x,y in occupied) >= 12 and max(y for x,y in occupied) <= 22
        assert min(x for x,y in occupied) >= 1 and max(x for x,y in occupied) <= 14
        assert not any('water' in c for c in colors), 'Real ground, not this foreground, owns water.'
    stats.append({'frame': i, 'opaquePixels': len(occupied), 'colors': len(colors)})

def edge(i, side):
    cells = frames[i]['cells']
    values = cells[0] if side == 1 else cells[23] if side == 4 else [row[15 if side == 2 else 0] for row in cells]
    return [palette[v[1]] if v else (0,0,0,0) for v in values]
joins = 0
for a in range(1,16):
    for b in range(1,16):
        if a & 4 and b & 1: assert edge(a,4) == edge(b,1); joins += 1
        if a & 2 and b & 8: assert edge(a,2) == edge(b,8); joins += 1

# Mask0 preserves the approved East elbow's original native pixels exactly.
east = Image.open(ROOT / 'Assets/Resources/Sprites/Live16x24/east-kitchen.png').convert('RGBA')
for y in range(24):
    for x in range(16): assert native.getpixel((x,y)) == east.getpixel((15*16+x,y)), ('isolated elbow changed', x,y)

background = tuple(bytes.fromhex('032F23')) + (255,)
for scale, name in ((1,'contact-native.png'),(6,'contact-6x.png')):
    contact = Image.open(HERE/name).convert('RGBA')
    assert contact.size == (176*scale,168*scale)
    for i, frame in enumerate(frames):
        left, top = manifest['contactPlacements'][i]['topLeft']
        for y,row in enumerate(frame['cells']):
            for x,cell in enumerate(row):
                expected = palette[cell[1]] if cell else background
                assert all(contact.getpixel(((left+x)*scale+dx,(top+y)*scale+dy)) == expected
                           for dx in range(scale) for dy in range(scale)), ('direct-contact mismatch', name,i,x,y)
resource = ROOT / 'Assets/Resources/Sprites/Live16x24/nella-corner.png'
assert resource.read_bytes() == (HERE/'native.png').read_bytes()
meta = (resource.with_suffix('.png.meta')).read_text()
for contract in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','spritePixelsToUnits: 16','textureCompression: 0','nPOTScale: 0'):
    assert contract in meta, contract
for path, digest in manifest['preservedApprovedFiles'].items():
    assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == digest, ('approved asset changed', path)
digest = hashlib.sha256((HERE/'native.png').read_bytes()).hexdigest()
assert digest == manifest['nativeSHA256']
report = {'passed': True, 'nativeSize': [272,24], 'frameCount':17, 'alphaValues':[0,255],
          'sourcePixelMismatches':0,'contactPixelMismatches':0,'matchedCardinalSeams':joins,
          'isolatedEastElbowUnchanged':True,'resourceBytesMatch':True,'frames':stats,'nativeSHA256':digest,
          'limits':'Source and direct Blender render proof only; no Unity importer, live topology, game-state or gameplay acceptance.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
