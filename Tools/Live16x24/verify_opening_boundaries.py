"""Read-only native geometry/raster contract; never modifies an image."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/boundaries'
FILES = ('opening-boundaries.png', 'opening-boundaries.blend', 'opening-boundaries-contact.png',
         'opening-boundaries-context.png', 'source-cells.json', 'manifest.json')

def verify():
    missing = [name for name in FILES if not (OUT / name).is_file()]
    assert not missing, 'Missing native source artifacts: ' + ', '.join(missing)
    source = json.loads((OUT / 'source-cells.json').read_text())
    manifest = json.loads((OUT / 'manifest.json').read_text())
    im = Image.open(OUT / 'opening-boundaries.png').convert('RGBA')
    assert im.size == (256, 72), im.size
    assert len(source['frames']) == len(manifest['frames']) == 48
    palette = source['palette']; errors = []; occupied = []
    for i, frame in enumerate(source['frames']):
        assert frame['index'] == i and frame['mask'] == i % 16 and frame['material'] == i // 16
        pixels = im.crop((i % 16 * 16, i // 16 * 24, i % 16 * 16 + 16, i // 16 * 24 + 24))
        ink = set(); colors = set()
        for y in range(24):
            for x in range(16):
                cell = frame['cells'][y][x]
                expected = (0, 0, 0, 0) if cell is None else tuple(bytes.fromhex(palette[cell[1]])) + (255,)
                actual = pixels.getpixel((x, y))
                if actual != expected: errors.append([i, x, y, list(expected), list(actual)])
                assert actual[3] in (0, 255), (i, x, y, actual)
                if actual[3]: ink.add((x, y)); colors.add(actual)
        assert ink and len(colors) == 3, (i, len(ink), len(colors))
        left,right,top,bottom = (5,11,9,15) if frame['material']==0 else (4,12,8,16)
        for flag, edge, port in (
            (1, {(x, 0) for x in range(16)}, {(x, 0) for x in range(left, right)}),
            (2, {(15, y) for y in range(24)}, {(15, y) for y in range(top, bottom)}),
            (4, {(x, 23) for x in range(16)}, {(x, 23) for x in range(left, right)}),
            (8, {(0, y) for y in range(24)}, {(0, y) for y in range(top, bottom)})):
            assert ink & edge == (port if frame['mask'] & flag else set()), (i, flag)
        reached = {next(iter(ink))}; todo = list(reached)
        while todo:
            x,y = todo.pop()
            for pt in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if pt in ink and pt not in reached: reached.add(pt); todo.append(pt)
        assert reached == ink, ('disconnected mass', i)
        occupied.append(len(ink))
        assert manifest['frames'][i]['unityRect'] == [i % 16 * 16, (2 - i // 16) * 24, 16, 24]
    # Every enabled port in one material must match any complementary port,
    # including color. Different materials deliberately share a six-pixel
    # opaque overlap; their palette/height change is not a transparent gap.
    for material in range(3):
        frames=source['frames'][material*16:(material+1)*16]
        for bit,opposite in ((1,4),(2,8)):
            def edge(cells, flag):
                if flag==1: return cells[0]
                if flag==4: return cells[23]
                if flag==2: return [row[15] for row in cells]
                return [row[0] for row in cells]
            def colors(cells,flag): return [None if p is None else palette[p[1]] for p in edge(cells,flag)]
            for a in frames:
                if not a['mask']&bit: continue
                for b in frames:
                    if b['mask']&opposite: assert colors(a['cells'],bit)==colors(b['cells'],opposite),('port color seam',a['index'],b['index'])
    assert not errors, ('source pixel mismatches', errors[:8], len(errors))
    # Verify each contact frame from its exact geometry position, excluding labels.
    contact = Image.open(OUT / 'opening-boundaries-contact.png').convert('RGBA')
    scale = manifest['contact']['scale']; bg = tuple(bytes.fromhex(manifest['contact']['background'])) + (255,)
    assert contact.size == tuple(v * scale for v in manifest['contact']['nativeSize'])
    for placement in manifest['contact']['instances']:
        i = placement['frame']; ox, oy = placement['topLeft']
        for y, row in enumerate(source['frames'][i]['cells']):
            for x, cell in enumerate(row):
                expected = bg if cell is None else tuple(bytes.fromhex(palette[cell[1]])) + (255,)
                for dy in range(scale):
                    for dx in range(scale):
                        assert contact.getpixel(((ox+x)*scale+dx, (oy+y)*scale+dy)) == expected, ('contact mismatch',i,x,y)
    return {'passed': True, 'frames':48, 'nativeSize':[256,72], 'pixelMismatches':0,
            'alpha':'binary', 'paletteColorsPerFrame':3, 'allMassesConnected':True,
            'cardinalPortsMatched':True, 'contactGeometryMatches':True,
            'occupiedPixels':occupied, 'sha256':hashlib.sha256((OUT / FILES[0]).read_bytes()).hexdigest(),
            'scope':'Offline source verification only; no import, runtime routing or gameplay acceptance.'}

if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--receipt'); args=parser.parse_args()
    try: result=verify()
    except (AssertionError, OSError, KeyError, ValueError) as e:
        result={'passed':False,'error':str(e),'scope':'Offline source contract; no image modification.'}
    if args.receipt: Path(args.receipt).write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2)); raise SystemExit(0 if result['passed'] else 1)
