"""Decode-only steam-mark source check; optional semantic runtime-copy check."""
import argparse
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--check-unity', action='store_true')
args = parser.parse_args()
required = ('native.blend', 'native.png', 'native.png.meta', 'contact-native.png',
            'contact.png', 'source-cells.json', 'manifest.json', 'preserved-inputs.json')
missing = [name for name in required if not (HERE/name).is_file()]
assert not missing, 'Source RED: steam-mark artifacts missing: '+', '.join(missing)
source = json.loads((HERE/'source-cells.json').read_text())
manifest = json.loads((HERE/'manifest.json').read_text())
expected_palette = {'muted': '3C5148', 'body': '526657', 'edge': '7D8E71'}
assert source['palette'] == expected_palette and len(source['frames']) == 1
frame = source['frames'][0]
assert frame['id'] == 'steam-tile-mark' and frame['ownerKind'] == 'tile-state'
assert frame['cloud'] == 'steam' and 'blueprint' not in frame
cells = frame['cells']
assert len(cells) == 24 and all(len(row) == 16 for row in cells)
rgba = {name: tuple(bytes.fromhex(value))+(255,) for name, value in expected_palette.items()}
native = Image.open(HERE/'native.png').convert('RGBA')
small = Image.open(HERE/'contact-native.png').convert('RGBA')
contact = Image.open(HERE/'contact.png').convert('RGBA')
assert native.size == (16,24) and small.size == (32,32) and contact.size == (256,256)
assert set(native.getchannel('A').tobytes()) == {0,255}
assert set(small.getchannel('A').tobytes()) == set(contact.getchannel('A').tobytes()) == {255}
approved = json.loads((ROOT/'ArtSource/Live16x24/east-kitchen/source-cells.json').read_text())
grate = approved['frames'][16]
assert grate['id'] == 'neutral-vent' and grate['blueprint'] == 'SteamVent'
grate_rgba = {name: tuple(bytes.fromhex(value))+(255,) for name,value in approved['palette'].items()}
ground = tuple(bytes.fromhex('032F23'))+(255,)
occupied, used, parts = set(), set(), set()
for y,row in enumerate(cells):
    for x,cell in enumerate(row):
        expected = rgba[cell[1]] if cell else (0,0,0,0)
        assert native.getpixel((x,y)) == expected, ('native/source',x,y)
        original = grate['cells'][y][x]
        assert not (cell and original), 'Steam cannot repaint the approved grille'
        shown = expected if cell else grate_rgba[original[1]] if original else ground
        assert small.getpixel((8+x,4+y)) == shown, ('native contact',x,y)
        assert all(contact.getpixel(((8+x)*8+dx,(4+y)*8+dy)) == shown
                   for dx in range(8) for dy in range(8)), ('8x contact',x,y)
        if cell:
            occupied.add((x,y)); used.add(cell[1]); parts.add(cell[0])
assert used == set(expected_palette) and parts == {'upper-tail','middle-curl','lower-thread'}
xs,ys = zip(*occupied)
bounds = [min(xs),min(ys),max(xs)+1,max(ys)+1]
assert 6 <= max(xs)-min(xs)+1 <= 8 and 10 <= max(ys)-min(ys)+1 <= 12
assert all(4 <= x <= 11 and 2 <= y <= 13 for x,y in occupied)
assert all(cell is None for row in cells[15:] for cell in row), 'Grate and bottom eight pixels remain clear'
assert 16 <= len(occupied) <= 28 and all(sum(c is not None for c in row) <= 3 for row in cells)
assert len(occupied) < (max(xs)-min(xs)+1)*(max(ys)-min(ys)+1)/3, 'Mostly negative space, no smoke blanket'
pending=set(occupied); frontier=[pending.pop()]
while frontier:
    x,y=frontier.pop()
    for point in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
        if point in pending: pending.remove(point); frontier.append(point)
assert not pending, 'One sparse connected rising wisp'
digest = hashlib.sha256((HERE/'native.png').read_bytes()).hexdigest()
pixel_digest = hashlib.sha256(native.tobytes()).hexdigest()
assert manifest['nativeSHA256'] == digest and manifest['decodedPixelSHA256'] == pixel_digest
assert manifest['boundsTopLeftExclusive'] == bounds and manifest['opaquePixels'] == len(occupied)
assert manifest['frameCount'] == 1 and manifest['resource'] == 'Sprites/Live16x24/east-steam'
assert manifest['ownerKind'] == 'tile-state' and manifest['cloud'] == 'steam'
assert manifest['pixelsPerUnit'] == 16 and manifest['pivot'] == [.5,.5]
assert manifest['unityExportDeferred'] is True
preserved = json.loads((HERE/'preserved-inputs.json').read_text())
for path,digest_before in preserved.items():
    assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == digest_before, ('Preserved input changed',path)
assert manifest['referenceSHA256'] == preserved
# Reuse the reviewed Cudgel comparator; do not duplicate a metadata framework.
sys.path.insert(0,str(HERE.parent/'cudgel'))
from metadata_contract import parse_metadata, assert_authored_subset, authored_contract_counts
metadata = parse_metadata((HERE/'native.png.meta').read_text())
assert len(metadata['guid']) == 32 and all(c in '0123456789abcdef' for c in metadata['guid'])
assert_authored_subset({'fileFormatVersion':2, 'TextureImporter':{
    'mipmaps':{'enableMipMap':0}, 'textureSettings':{'filterMode':0,'wrapU':1,'wrapV':1,'wrapW':1},
    'nPOTScale':0,'spritePixelsToUnits':16,'spritePivot':{'x':.5,'y':.5},
    'spriteSheet':{'sprites':[],'outline':[],'physicsShape':[],'vertices':[]},
    'platformSettings':[{'textureCompression':0}]}},metadata)
if args.check_unity:
    resource=ROOT/'Assets/Resources/Sprites/Live16x24/east-steam.png'
    assert resource.read_bytes() == (HERE/'native.png').read_bytes()
    assert_authored_subset(metadata,parse_metadata(Path(str(resource)+'.meta').read_text()))
report={'passed':True,'nativeSize':[16,24],'frameCount':1,'opaquePixels':len(occupied),
        'paletteSize':len(used),'alphaValues':[0,255],'boundsTopLeftExclusive':bounds,
        'bottomNineRowsTransparent':True,'approvedGrateOverlapPixels':0,
        'sourcePixelMismatches':0,'contactPixelMismatches':0,'preservedInputCount':len(preserved),
        'nativeSHA256':digest,'decodedPixelSHA256':pixel_digest,
        'authoredMetadataContract':authored_contract_counts(metadata),
        'runtimeCopyChecked':args.check_unity,
        'limits':'Source geometry/render/metadata checks only; root owns actual tile-state admission, priority, visibility, editor import and gameplay acceptance.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
