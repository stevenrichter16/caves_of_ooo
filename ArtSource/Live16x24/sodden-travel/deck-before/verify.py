"""Decode-only source validation. Never authors, imports or edits raster pixels."""
import copy
import hashlib
import json
from pathlib import Path
from PIL import Image

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
PRESERVED_FRAME_INDICES = tuple(range(16)) + (18,19,20,21)
PASS2 = json.loads((OUT/'pass2/source-cells.json').read_text())
PALETTE = {
    'board-shadow':'322E25', 'board-dark':'504B3B', 'board':'726C59',
    'board-worn':'88806B', 'board-grain':'635D4B',
    'mire':'252C1B', 'mire-deep':'1D2519', 'mire-film':'3A3C20',
    'mire-ochre':'5A5226', 'mire-glint':'6F6939',
    'peat-edge':'211F1B', 'peat':'332F25', 'peat-layer':'3D3829',
    'peat-fiber':'504B35', 'peat-moss':'465332',
    'bark-dark':'37413B', 'bark':'68756A', 'bark-light':'8C9380', 'broken-tip':'A6AB90',
}


def contract(source):
    assert source['palette'] == PALETTE
    frames = source['frames']; assert len(frames) == 28, 'Expected 28 frames including six sparse mire variants'
    for index in PRESERVED_FRAME_INDICES:
        assert frames[index] == PASS2['frames'][index], ('Accepted non-mire frame changed',index)
    decoded = []
    for i, frame in enumerate(frames):
        cells = frame['cells']
        assert frame['index'] == i and len(cells) == 24 and all(len(row) == 16 for row in cells)
        assert all(c is None or isinstance(c,list) and len(c)==2 and c[1] in PALETTE for row in cells for c in row)
        pixels = [PALETTE[c[1]] if c else None for row in cells for c in row]
        assert any(pixels) and pixels not in decoded, 'Empty or duplicate frame'
        decoded.append(pixels)
    for mask in range(16):
        frame = frames[mask]; cells = frame['cells']
        assert frame['blueprint'] == 'Duckboard' and frame['neighborMask'] == mask
        assert all(c is None or c[1].startswith('board') for row in cells for c in row)
        for bit, positions in ((1,[(x,0) for x in range(4,12)]), (2,[(15,y) for y in range(8,16)]),
                               (4,[(x,23) for x in range(4,12)]), (8,[(0,y) for y in range(8,16)])):
            assert all(bool(cells[y][x]) == bool(mask & bit) for x,y in positions), 'Board connection missing/invented'
        for y,row in enumerate(cells):
            for x,c in enumerate(row):
                allowed = (4 <= x <= 11 and 8 <= y <= 15 or mask&1 and 4 <= x <= 11 and y <= 11
                           or mask&2 and x >= 8 and 8 <= y <= 15 or mask&4 and 4 <= x <= 11 and y >= 12
                           or mask&8 and x <= 7 and 8 <= y <= 15)
                assert bool(c) == bool(allowed), 'Board footprint drift'
        assert any(c and c[0]=='transverse-plank-joint' for row in cells for c in row)
        assert any(c and c[0]=='weathered-wood-grain' for row in cells for c in row)
    for a in range(16):
        for b in range(16):
            if a&1 and b&4:
                assert frames[a]['cells'][0] == frames[b]['cells'][23], 'North/south board seam'
            if a&2 and b&8:
                assert [row[15] for row in frames[a]['cells']] == [row[0] for row in frames[b]['cells']], 'East/west board seam'
    for index, state, variant in ((16,'filled',0),(17,'depleted-wet-film',0),
                                 (22,'filled',1),(23,'filled',2),(24,'filled',3),
                                 (25,'depleted-wet-film',1),(26,'depleted-wet-film',2),(27,'depleted-wet-film',3)):
        frame = frames[index]; cells = frame['cells']
        assert frame['blueprint']=='MirePool' and frame['stockState']==state and frame['stillWet'] is True and frame['variant']==variant
        assert all(c and c[1].startswith('mire') for row in cells for c in row), 'Mire becomes dry/transparent'
        assert all(c[1] in ('mire','mire-deep','mire-film') for row in cells for c in row), 'Bright repeating mire material'
        reflection=sum(c[1]=='mire-film' for row in cells for c in row)
        marks=sum(c[1]!='mire' for row in cells for c in row)
        assert 1 <= reflection <= (3 if variant==0 else 6), 'Wet reflection too absent/dense'
        assert marks <= (12 if variant==0 else 24), 'Repeated mire pattern is too dense'
        assert any(c[0]=='wet-surface-glint' for row in cells for c in row), 'Wet surface cue absent'
        assert all(c[1]=='mire' for c in cells[0]+cells[23])
        assert all(row[0][1]==row[15][1]=='mire' for row in cells)
    assert sum(a!=b for ra,rb in zip(frames[16]['cells'],frames[17]['cells']) for a,b in zip(ra,rb)) >= 10, 'Mire states indistinguishable'
    for index in (18,19):
        f=frames[index]; cells=f['cells']; opaque=[c for row in cells for c in row if c]
        assert f['blueprint']=='PeatBank' and f['variant']==index-18 and len(opaque)>=300
        assert all(c[1].startswith('peat') for c in opaque)
        assert {'compressed-peat-face','cut-peat-layer','embedded-root-fiber','muted-moss-lip'} <= {c[0] for c in opaque}
        assert all(c for row in cells for c in row), 'Walkable-looking hole or pinched seam in solid peat'
        assert sum(c[1]=='peat-moss' for c in opaque) <= 24, 'Peat replaced by a green hedge'
    for index in (20,21):
        f=frames[index];cells=f['cells']; opaque=[(x,y,c) for y,row in enumerate(cells) for x,c in enumerate(row) if c]
        assert f['blueprint']=='DeadTree' and f['variant']==index-20
        assert 65 <= len(opaque) <= 160 and max(y for x,y,c in opaque)==22 and all(c is None for c in cells[23])
        assert all(c[1].startswith('bark') or c[1]=='broken-tip' for x,y,c in opaque), 'Live canopy/fruit or wrong material'
        assert {'dead-trunk','bare-left-fork','bare-right-fork','root-flare'} <= {c[0] for x,y,c in opaque}
    return True


def verify():
    required=('sodden-travel.png','sodden-travel.blend','source-cells.json','manifest.json','contact-native.png','contact.png','preserved-inputs.json')
    missing=[name for name in required if not (OUT/name).is_file()]
    assert not missing, 'Source RED: missing '+', '.join(missing)
    source=json.loads((OUT/'source-cells.json').read_text()); manifest=json.loads((OUT/'manifest.json').read_text());contract(source)
    native=Image.open(OUT/'sodden-travel.png').convert('RGBA');assert native.size==(448,24)
    previous=Image.open(OUT/'pass2/sodden-travel.png').convert('RGBA')
    for index in PRESERVED_FRAME_INDICES:
        assert all(native.getpixel((index*16+x,y))==previous.getpixel((index*16+x,y)) for y in range(24) for x in range(16)), ('Accepted native pixels changed',index)
    assert set(native.getchannel('A').tobytes())=={0,255}
    counts=[]
    for index,frame in enumerate(source['frames']):
        count=0
        for y,row in enumerate(frame['cells']):
            for x,c in enumerate(row):
                expected=tuple(bytes.fromhex(PALETTE[c[1]]))+(255,) if c else (0,0,0,0)
                assert native.getpixel((index*16+x,y))==expected, ('Native/source mismatch',index,x,y)
                count+=bool(c)
        assert manifest['frames'][index]['unityRect']==[index*16,0,16,24];counts.append(count)
    assert [manifest[k] for k in ('width','height','frameCount','frameWidth','frameHeight')]==[448,24,28,16,24]
    for filename,scale in (('contact-native.png',1),('contact.png',6)):
        contact=Image.open(OUT/filename).convert('RGBA');m=manifest['contact']
        assert contact.size==tuple(n*scale for n in m['nativeSize'])
        bg=tuple(bytes.fromhex(m['background']))+(255,)
        for placement in m['instances']:
            for y,row in enumerate(source['frames'][placement['frame']]['cells']):
                for x,c in enumerate(row):
                    expected=tuple(bytes.fromhex(PALETTE[c[1]]))+(255,) if c else bg
                    for dy in range(scale):
                        for dx in range(scale):
                            assert contact.getpixel(((placement['topLeft'][0]+x)*scale+dx,(placement['topLeft'][1]+y)*scale+dy))==expected, ('Contact/source mismatch',filename,placement,x,y)
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    for path,before in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==before, ('Preserved input changed',path)
    historical=json.loads((OUT/'pass2/preserved-pass2.json').read_text())
    for name,before in historical.items():assert hashlib.sha256((OUT/'pass2'/name).read_bytes()).hexdigest()==before, ('Pass2 source altered',name)
    mutations=[]
    for name,change in (
        ('lost-frame',lambda s:s['frames'].pop()),
        ('duplicate-state',lambda s:s['frames'][17].__setitem__('cells',copy.deepcopy(s['frames'][16]['cells']))),
        ('invented-north-link',lambda s:s['frames'][0]['cells'][0].__setitem__(4,['board-join','board'])),
        ('missing-east-link',lambda s:s['frames'][2]['cells'][8].__setitem__(15,None)),
        ('board-seam-mismatch',lambda s:s['frames'][1]['cells'][0].__setitem__(5,['board-join','board-worn'])),
        ('depleted-dry-hole',lambda s:s['frames'][17]['cells'][12].__setitem__(8,None)),
        ('false-safe-depleted',lambda s:s['frames'][17].__setitem__('stillWet',False)),
        ('variant-bright-mound',lambda s:s['frames'][22]['cells'][8].__setitem__(8,['oxidized-slick','mire-ochre'])),
        ('dense-film-carpet',lambda s:s['frames'][25]['cells'][10].__setitem__(slice(1,15),[['wet-surface-glint','mire-film']]*14)),
        ('duplicated-mire-variant',lambda s:s['frames'][24].__setitem__('cells',copy.deepcopy(s['frames'][22]['cells']))),
        ('changed-accepted-board',lambda s:s['frames'][0]['cells'][10].__setitem__(6,['wood','board-worn'])),
        ('changed-accepted-peat',lambda s:s['frames'][18]['cells'][10].__setitem__(6,['compressed-peat-face','peat-layer'])),
        ('changed-accepted-tree',lambda s:s['frames'][20]['cells'][20].__setitem__(7,['dead-trunk','bark-light'])),
        ('cutbank-gap',lambda s:s['frames'][18]['cells'][12].__setitem__(8,None)),
        ('tree-live-leaf',lambda s:s['frames'][20]['cells'][4].__setitem__(8,['leaf','peat-moss'])),
        ('unknown-color',lambda s:s['frames'][0]['cells'][10].__setitem__(7,['wood','unknown'])),
    ):
        changed=copy.deepcopy(source);change(changed)
        try:contract(changed)
        except AssertionError:mutations.append(name)
        else:raise AssertionError('Countercheck accepted '+name)
    sha=hashlib.sha256((OUT/'sodden-travel.png').read_bytes()).hexdigest();assert sha==manifest['sha256']
    return {'passed':True,'size':[448,24],'frames':28,'allFramesDistinct':True,'binaryAlpha':True,
            'opaquePixelsPerFrame':counts,'sourcePixelMismatches':0,'contactPixelMismatches':0,
            'acceptedNonMireFramesPreservedExactly':list(PRESERVED_FRAME_INDICES),'pass2FilesPreservedExactly':len(historical),
            'mireFrames':{str(i):{'subduedReflectionPixels':sum(c[1]=='mire-film' for row in source['frames'][i]['cells'] for c in row),
                                'allMarkedPixels':sum(c[1]!='mire' for row in source['frames'][i]['cells'] for c in row)} for i in (16,17,22,23,24,25,26,27)},
            'preservedInputsUnchanged':len(preserved),'counterchecksRejected':mutations,'sha256':sha,
            'decodedPixelSHA256':hashlib.sha256(native.tobytes()).hexdigest(),
            'scope':'Editable source and pixel contract only; no Unity import, runtime/state mapping, scene fullness, actual play or device acceptance.'}


if __name__=='__main__':
    try:report=verify()
    except (AssertionError,OSError,KeyError,ValueError) as exc:report={'passed':False,'error':str(exc)}
    (OUT/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2));raise SystemExit(0 if report['passed'] else 1)
