"""Read-only source/render checks. Never creates, resizes or edits an image."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

OUT = Path(__file__).resolve().parent
FILES = ('opening-ground.png', 'opening-ground.blend', 'opening-ground-contact.png',
         'opening-ground-context.png', 'source-cells.json', 'manifest.json')

def verify():
    missing = [p for p in FILES if not (OUT / p).is_file()]
    assert not missing, 'Missing native source artifacts: ' + ', '.join(missing)
    source = json.loads((OUT / 'source-cells.json').read_text())
    manifest = json.loads((OUT / 'manifest.json').read_text())
    im = Image.open(OUT / 'opening-ground.png').convert('RGBA')
    assert im.size == (336, 24), im.size
    assert len(source['frames']) == len(manifest['frames']) == 21
    palette = source['palette']; frame_hashes = []; counts = []
    for i, frame in enumerate(source['frames']):
        assert frame['index'] == i and len(frame['cells']) == 24
        colors = set(); raw = []
        for y, row in enumerate(frame['cells']):
            assert len(row) == 16
            for x, cell in enumerate(row):
                assert cell and len(cell) == 2, (i,x,y,'ground must cover entire cell')
                expected = tuple(bytes.fromhex(palette[cell[1]])) + (255,)
                actual = im.getpixel((i*16+x,y))
                assert actual == expected, ('source mismatch',i,x,y,expected,actual)
                assert actual[3] == 255, (i,x,y,'not fully opaque')
                colors.add(actual); raw.extend(actual)
        assert 1 <= len(colors) <= 4, (i,len(colors))
        counts.append(len(colors)); frame_hashes.append(hashlib.sha256(bytes(raw)).hexdigest())
        assert manifest['frames'][i]['unityRect'] == [i*16,0,16,24]
    assert len(set(frame_hashes)) == 21, 'Duplicate state/mask frame'
    # The quiet wet interior is identical across every mask. No foam, glint,
    # object, diagonal outside-cell query or height signal is allowed there.
    center = source['frames'][0]['cells']
    for mask in range(16):
        cells = source['frames'][mask]['cells']
        assert source['frames'][mask]['dryEdgeMask'] == mask
        for y in range(3,21):
            for x in range(3,13):
                assert cells[y][x][1] == center[y][x][1] == 'water'
        for bit, samples in ((1,[(x,0) for x in range(3,13)]), (2,[(15,y) for y in range(3,21)]),
                             (4,[(x,23) for x in range(3,13)]), (8,[(0,y) for y in range(3,21)])):
            for x,y in samples:
                assert (cells[y][x][1] == 'bank') == bool(mask&bit), (mask,bit,x,y)
        for y in range(24):
            for x in range(16):
                if cells[y][x][1] != 'water':
                    assert (mask&1 and y<3) or (mask&2 and x>=13) or (mask&4 and y>=21) or (mask&8 and x<3), (mask,x,y)
    # Added dry bits only add their own bounded edge treatment.
    for mask in range(16):
        for bit in (1,2,4,8):
            if mask&bit: continue
            a=source['frames'][mask]['cells']; b=source['frames'][mask|bit]['cells']
            for y in range(24):
                for x in range(16):
                    in_edge = (bit==1 and y<3) or (bit==2 and x>=13) or (bit==4 and y>=21) or (bit==8 and x<3)
                    if not in_edge: assert a[y][x] == b[y][x], ('edge leaked',mask,bit,x,y)
    # Wetness must change palette, not add growth marks or alter furrows.
    dry=source['frames'][19]['cells']; wet=source['frames'][20]['cells']
    for y in range(24):
        for x in range(16):
            assert dry[y][x][0] == wet[y][x][0], ('furrow geometry changed',x,y)
            d=bytes.fromhex(palette[dry[y][x][1]]); w=bytes.fromhex(palette[wet[y][x][1]])
            assert all(wc < dc for wc,dc in zip(w,d)), ('wet not darker',x,y)
    # Direct integer camera-scale contact, independently compared at every pixel.
    contact=Image.open(OUT/'opening-ground-contact.png').convert('RGBA')
    info=manifest['contact']; scale=info['scale']
    assert contact.size==tuple(n*scale for n in info['nativeSize'])
    for placement in info['instances']:
        i=placement['frame']; ox,oy=placement['topLeft']
        for y,row in enumerate(source['frames'][i]['cells']):
            for x,cell in enumerate(row):
                expected=tuple(bytes.fromhex(palette[cell[1]]))+(255,)
                for dy in range(scale):
                    for dx in range(scale):
                        assert contact.getpixel(((ox+x)*scale+dx,(oy+y)*scale+dy))==expected, ('contact mismatch',i,x,y)
    # Compare the repeated/source-layout study separately; no screenshot pixels.
    context=Image.open(OUT/'opening-ground-context.png').convert('RGBA'); info=manifest['context']; scale=info['scale']
    assert context.size==tuple(n*scale for n in info['nativeSize'])
    for placement in info['instances']:
        i=placement['frame']; ox,oy=placement['topLeft']
        for y,row in enumerate(source['frames'][i]['cells']):
            for x,cell in enumerate(row):
                expected=tuple(bytes.fromhex(palette[cell[1]]))+(255,)
                for dy in range(scale):
                    for dx in range(scale):
                        assert context.getpixel(((ox+x)*scale+dx,(oy+y)*scale+dy))==expected, ('context mismatch',i,x,y)
    digest=hashlib.sha256((OUT/'opening-ground.png').read_bytes()).hexdigest()
    assert manifest['sha256']==digest
    return {'passed':True,'nativeSize':[336,24],'frames':21,'opaquePixelsPerFrame':384,
            'paletteColorsPerFrame':counts,'sourcePixelMismatches':0,'contactPixelMismatches':0,
            'contextPixelMismatches':0,'boundedDryEdgeMasks':16,'sameDryWetGeometry':True,'wetPaletteStrictlyDarker':True,
            'sha256':digest,'scope':'Offline source only; no Unity import, owner routing, fog, gameplay or Deck acceptance.'}

if __name__=='__main__':
    ap=argparse.ArgumentParser(); ap.add_argument('--receipt'); args=ap.parse_args()
    try: result=verify()
    except (AssertionError,OSError,KeyError,ValueError) as e: result={'passed':False,'error':str(e),'scope':'Read-only source contract; no image modification.'}
    if args.receipt: Path(args.receipt).write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2)); raise SystemExit(0 if result['passed'] else 1)
