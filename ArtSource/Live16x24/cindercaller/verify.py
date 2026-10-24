"""Read-only native source verification; writes only this directory's receipt."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
required = ('cindercaller.png', 'cindercaller-contact.png', 'cindercaller.blend',
            'source-cells.json', 'manifest.json')
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, 'Expected pre-authoring RED: missing ' + ', '.join(missing)
source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
native = Image.open(HERE / 'cindercaller.png').convert('RGBA')
contact = Image.open(HERE / 'cindercaller-contact.png').convert('RGBA')
assert native.size == (16, 24) and contact.size == (768, 320)
assert set(native.getchannel('A').tobytes()) == {0, 255}
assert native.getbbox()[3] == 23 and 4 <= native.getbbox()[1] <= 7
assert 2 <= native.getbbox()[0] and native.getbbox()[2] <= 15
cells = source['cells']
assert len(cells) == 24 and all(len(row) == 16 for row in cells)
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source['palette'].items()}
assert set(source['palette'].values()) == {'342E25','3C5148','526657','7D8E71','A97542','BCA576','9B442B'}
bg = tuple(bytes.fromhex('032F23')) + (255,)
opaque, colors, parts = set(), set(), set()
for y, row in enumerate(cells):
    for x, value in enumerate(row):
        expected = palette[value[1]] if value else (0, 0, 0, 0)
        actual = native.getpixel((x, y))
        assert actual == expected or expected[3] == actual[3] == 0, (x, y, actual, expected)
        if value:
            opaque.add((x,y)); colors.add(actual); parts.add(value[0])
        displayed = expected if expected[3] else bg
        assert all(contact.getpixel(((40+x)*8+dx, (8+y)*8+dy)) == displayed
                   for dx in range(8) for dy in range(8)), ('contact', x, y)
assert 90 <= len(opaque) <= 175 and len(colors) == 7
assert {'narrow-cap','charred-mantle-edge','blunt-face','hind-foot','near-claw','far-claw'} <= parts
assert not any(token in part for part in parts for token in ('weapon','cudgel','fire','moss','pouch','glow'))
assert all((x,22) not in opaque for x in (7,8,9)), 'Leg/claw negative space must remain open.'
assert all((x,5) not in opaque for x in (0,1,2,3,4,5,6,10,11,12,13,14,15)), 'Narrow cap crown.'
resource = ROOT / 'Assets/Resources/Sprites/Live16x24/cindercaller.png'
assert resource.read_bytes() == (HERE/'cindercaller.png').read_bytes()
meta = Path(str(resource)+'.meta').read_text()
assert re.search(r'^guid: [0-9a-f]{32}$', meta, re.M)
for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
    assert token in meta, token
preserved = json.loads((HERE/'preserved-art-hashes.json').read_text())
for path, sha in preserved.items():
    assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == sha, ('Changed approved art', path)
cast = Image.open(ROOT/'ArtSource/Live16x24/cast/opening-cast.png').convert('RGBA')
for index in (2,3):
    old = cast.crop((index*16,0,(index+1)*16,24))
    assert native.tobytes() != old.tobytes()
    assert native.getchannel('A').tobytes() != old.getchannel('A').tobytes(), 'Must not be a species recolor.'
    origin = 8 if index == 2 else 72
    for y in range(24):
        for x in range(16):
            pixel = old.getpixel((x,y))
            expected = pixel if pixel[3] else bg
            assert all(contact.getpixel(((origin+x)*8+dx,(8+y)*8+dy)) == expected
                       for dx in range(8) for dy in range(8)), ('approved contact changed',index,x,y)
sha = hashlib.sha256(resource.read_bytes()).hexdigest()
assert manifest['sha256'] == sha and manifest['blueprint'] == 'MarlbackCindercaller'
report = dict(passed=True, size=list(native.size), bounds=list(native.getbbox()),
              opaquePixels=len(opaque), opaqueColors=len(colors), alpha=[0,255],
              sourcePixelMismatches=0, contactPixelMismatches=0,
              preservedArtFiles=len(preserved), sha256=sha,
              scope='Source art, import metadata and preservation only; no Unity, gameplay, visibility, motion or device claim.')
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
