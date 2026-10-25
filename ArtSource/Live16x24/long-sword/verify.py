"""Read-only image validation; writes only this source packet's JSON receipt."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
required = ('long-sword.png', 'long-sword-contact.png', 'long-sword-parts.png',
            'long-sword.blend', 'source-cells.json', 'manifest.json')
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, 'Expected pre-authoring RED: missing ' + ', '.join(missing)
source = json.loads((HERE / 'source-cells.json').read_text())
manifest = json.loads((HERE / 'manifest.json').read_text())
native = Image.open(HERE / 'long-sword.png').convert('RGBA')
contact = Image.open(HERE / 'long-sword-contact.png').convert('RGBA')
parts_image = Image.open(HERE / 'long-sword-parts.png').convert('RGBA')
assert native.size == (16, 24)
assert contact.size == (768, 320) and parts_image.size == (640, 320)
assert set(native.getchannel('A').tobytes()) == {0, 255}
assert native.getbbox() == (2, 1, 14, 23), native.getbbox()
assert len(source['cells']) == 24 and all(len(row) == 16 for row in source['cells'])
assert set(source['palette'].values()) == {'342E25', '5F4830', '3C5148', '526657', '7D8E71', 'B5A47E'}
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source['palette'].items()}
bg = tuple(bytes.fromhex('032F23')) + (255,)
colors, names, opaque = set(), set(), set()
for y, row in enumerate(source['cells']):
    for x, value in enumerate(row):
        expected = palette[value[1]] if value else (0, 0, 0, 0)
        actual = native.getpixel((x, y))
        assert actual == expected or actual[3] == expected[3] == 0, ('native', x, y, actual, expected)
        if value:
            colors.add(actual); names.add(value[0]); opaque.add((x, y))
        displayed = expected if expected[3] else bg
        for dx in range(8):
            for dy in range(8):
                assert contact.getpixel(((40+x)*8+dx, (8+y)*8+dy)) == displayed, ('contact',x,y)
        for part, origin in (('blade', 8), ('crossguard', 32), ('grip', 56)):
            selected = value and (value[0].startswith(part) or (part == 'grip' and value[0].startswith('pommel')))
            part_pixel = expected if selected else bg
            for dx in range(8):
                for dy in range(8):
                    assert parts_image.getpixel(((origin+x)*8+dx, (8+y)*8+dy)) == part_pixel, ('parts',part,x,y)
assert len(colors) == 6 and 60 <= len(opaque) <= 110, (len(colors), len(opaque))
assert {'blade-face', 'blade-edge', 'blade-spine', 'crossguard', 'grip-leather', 'pommel'} <= names
assert all(not value for value in source['cells'][0] + source['cells'][23])
assert not any(word in name for name in names for word in ('flame','magic','glow','spark'))
# Approved comparison items are re-rendered from their editable cells, not copied raster pixels.
items = Image.open(ROOT/'ArtSource/Live16x24/items/opening-items.png').convert('RGBA')
for index, origin in ((9, 8), (7, 72)):
    for y in range(24):
        for x in range(16):
            px = items.getpixel((16*index+x,y)); expected = px if px[3] else bg
            for dx in range(8):
                for dy in range(8):
                    assert contact.getpixel(((origin+x)*8+dx,(8+y)*8+dy)) == expected, ('comparison',index,x,y)
resource = ROOT/'Assets/Resources/Sprites/Live16x24/long-sword.png'
assert resource.read_bytes() == (HERE/'long-sword.png').read_bytes()
meta = Path(str(resource)+'.meta').read_text()
assert re.search(r'^guid: [0-9a-f]{32}$', meta, re.M)
for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0',
              'textureCompression: 0','spritePixelsToUnits: 16','spritePivot: {x: 0.5, y: 0.5}'):
    assert token in meta, token
preserved = json.loads((HERE/'preserved-art-git-hashes.json').read_text())
for path, sha in preserved.items():
    assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == sha, ('Changed approved asset',path)
sha = hashlib.sha256(resource.read_bytes()).hexdigest()
assert manifest['sha256'] == sha and manifest['blueprint'] == 'LongSword'
assert manifest['resource'] == 'Sprites/Live16x24/long-sword'
report = dict(passed=True, size=list(native.size), bounds=list(native.getbbox()),
              opaquePixels=len(opaque), opaqueColors=len(colors), alpha=[0,255],
              sourcePixelMismatches=0, contactPixelMismatches=0, partsPixelMismatches=0,
              preservedAssetFiles=len(preserved), sha256=sha,
              scope='Native source pixels, editable geometry contract, import metadata and preservation only; not Unity/runtime/gameplay acceptance.')
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
