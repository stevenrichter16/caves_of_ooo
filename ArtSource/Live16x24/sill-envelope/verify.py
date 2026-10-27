"""Read-only native source contract; never generates or modifies image pixels."""
import copy, hashlib, json
from pathlib import Path
from PIL import Image
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]

def source_contract(s):
    assert len(s['frames'])==24
    for i,f in enumerate(s['frames']):
        assert f['index']==i and len(f['cells'])==24
        assert all(len(row)==16 for row in f['cells'])
        assert all(c is None or len(c)==2 and c[1] in s['palette'] for row in f['cells'] for c in row)
        assert any(c for row in f['cells'] for c in row)
        if i>=20: assert all(c for row in f['cells'] for c in row),'ground alpha hole'
    for mask in range(16):
        f=s['frames'][mask]; assert f['neighborMask']==mask
        for bit,pts in [(1,[(x,0) for x in range(5,11)]),(2,[(15,y) for y in range(9,16)]),(4,[(x,23) for x in range(5,11)]),(8,[(0,y) for y in range(9,16)])]:
            assert all(bool(f['cells'][y][x])==bool(mask&bit) for x,y in pts),'connection mask mismatch'
    # Opposite edges use the same strand profile; no visible seam at a join.
    for a in range(16):
        for b in range(16):
            if a&1 and b&4:
                assert [s['frames'][a]['cells'][0][x] for x in range(5,11)]==[s['frames'][b]['cells'][23][x] for x in range(5,11)]
            if a&2 and b&8:
                assert [s['frames'][a]['cells'][y][15] for y in range(9,16)]==[s['frames'][b]['cells'][y][0] for y in range(9,16)]
    assert all(s['frames'][17]['cells'][y][x] is None for y in range(24) for x in (7,8)),'NS passage not visibly open'
    assert all(s['frames'][19]['cells'][y][x] is None for y in (10,11) for x in range(16)),'EW passage not visibly open'
    for a,b in [(16,17),(18,19)]:
        assert s['frames'][a]['closed'] and not s['frames'][b]['closed']
        assert s['frames'][a]['quarterTurns']==s['frames'][b]['quarterTurns']
        assert s['frames'][a]['cells']!=s['frames'][b]['cells'],'door state identical'
    return True

def verify():
    required=['sill-envelope.png','sill-envelope.blend','contact.png','manifest.json','source-cells.json','preserved-inputs.json']
    assert all((OUT/n).is_file() for n in required),'Missing native artifacts: '+', '.join(n for n in required if not (OUT/n).is_file())
    s=json.loads((OUT/'source-cells.json').read_text());m=json.loads((OUT/'manifest.json').read_text());source_contract(s)
    image=Image.open(OUT/'sill-envelope.png').convert('RGBA');assert image.size==(384,24)
    hashes=[];counts=[]
    for i,f in enumerate(s['frames']):
        raw=[];colors=set();opaque=0
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                p=image.getpixel((i*16+x,y));expected=tuple(bytes.fromhex(s['palette'][c[1]]))+(255,) if c else (0,0,0,0)
                assert p==expected,('source/render mismatch',i,x,y,p,expected)
                assert p[3] in (0,255);raw.extend(p);colors.add(p)
                opaque+=bool(p[3])
        assert m['frames'][i]['unityRect']==[i*16,0,16,24]
        hashes.append(hashlib.sha256(bytes(raw)).hexdigest());counts.append(opaque)
    assert len(set(hashes))==24,'duplicate frames'
    assert counts[17]<counts[16] and counts[19]<counts[18],'open doorway not more open'
    contact=Image.open(OUT/'contact.png').convert('RGBA');info=m['contact'];scale=info['scale']
    assert contact.size==tuple(n*scale for n in info['nativeSize'])
    bg=tuple(bytes.fromhex(info['background']))+(255,)
    for inst in info['instances']:
        for y,row in enumerate(s['frames'][inst['frame']]['cells']):
            for x,c in enumerate(row):
                expected=tuple(bytes.fromhex(s['palette'][c[1]]))+(255,) if c else bg
                for dy in range(scale):
                    for dx in range(scale):
                        assert contact.getpixel(((inst['topLeft'][0]+x)*scale+dx,(inst['topLeft'][1]+y)*scale+dy))==expected,('contact mismatch',inst['frame'],x,y)
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    for path,digest in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('approved input changed',path)
    digest=hashlib.sha256((OUT/'sill-envelope.png').read_bytes()).hexdigest();assert digest==m['sha256']
    mutations=[]
    for name,change in [
        ('frame-count',lambda t:t['frames'].pop()),
        ('width',lambda t:t['frames'][0]['cells'][0].pop()),
        ('height',lambda t:t['frames'][0]['cells'].pop()),
        ('unknown-palette',lambda t:t['frames'][0]['cells'][10].__setitem__(7,['x','unknown'])),
        ('ground-alpha-hole',lambda t:t['frames'][20]['cells'][0].__setitem__(0,None)),
        ('missing-north-connection',lambda t:t['frames'][1]['cells'][0].__setitem__(5,None)),
        ('invented-north-connection',lambda t:t['frames'][0]['cells'][0].__setitem__(5,['x','vine'])),
        ('unchanged-open-door',lambda t:t['frames'][17].__setitem__('cells',copy.deepcopy(t['frames'][16]['cells']))),
        ('blocked-NS-open-passage',lambda t:t['frames'][17]['cells'][10].__setitem__(7,['x','wood'])),
        ('blocked-EW-open-passage',lambda t:t['frames'][19]['cells'][10].__setitem__(7,['x','wood'])),
    ]:
        candidate=copy.deepcopy(s);change(candidate)
        try:source_contract(candidate)
        except AssertionError:mutations.append(name)
        else:raise AssertionError('counter failed: '+name)
    return {'passed':True,'frames':24,'size':[384,24],'sourcePixelMismatches':0,'contactPixelMismatches':0,'allFramesDistinct':True,'binaryAlpha':True,'opaquePixelsPerFrame':counts,'approvedInputsUnchanged':len(preserved),'mutationCounters':mutations,'sha256':digest,'scope':'Editable source only. No Unity import, runtime mapping, actual scene fullness, gameplay or packaged acceptance.'}

if __name__=='__main__':
    try:result=verify()
    except (AssertionError,OSError,KeyError,ValueError) as e:result={'passed':False,'error':str(e)}
    (OUT/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2));raise SystemExit(0 if result['passed'] else 1)
