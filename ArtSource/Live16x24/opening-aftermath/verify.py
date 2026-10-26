"""Read-only raster/source verification. Does not edit images."""
import hashlib,json,subprocess
from pathlib import Path
from PIL import Image
D=Path(__file__).resolve().parent; R=D.parents[2]
s=json.loads((D/'source-cells.json').read_text());m=json.loads((D/'manifest.json').read_text())
a=Image.open(D/'opening-aftermath.png').convert('RGBA'); c=Image.open(D/'opening-aftermath-contact.png').convert('RGBA')
assert a.size==(48,24) and c.size==(1440,672)
assert set(a.getchannel('A').tobytes())=={0,255}
p={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()}
checks=[]
for i,f in enumerate(s['frames']):
    occupied=[]; colors=set(); nb=cb=0
    for y,row in enumerate(f['cells']):
        assert len(row)==16
        for x,v in enumerate(row):
            got=a.getpixel((i*16+x,y)); want=p[v[1]] if v else (0,0,0,0)
            nb += got!=want if v else got[3]!=0
            if v:occupied.append((x,y)); colors.add(got)
            background=(3,47,35,255)
            left=(i*40+12+x)*12;top=(14+y)*12
            cb+=sum(c.getpixel((left+sx,top+sy))!=(want if v else background) for sx in range(12) for sy in range(12))
    assert nb==cb==0,(i,nb,cb)
    assert 3<=len(colors)<=6 and len(occupied)==m['frames'][i]['occupiedPixels']
    assert all(1<=x<=14 and y<=22 for x,y in occupied)
    assert len(f['cells'])==24 and all(v is None for v in f['cells'][23])
    checks.append({'id':f['id'],'occupied':len(occupied),'colors':len(colors),'nativeMismatch':nb,'contactMismatch':cb})
assert min(y for y,row in enumerate(s['frames'][0]['cells']) for v in row if v)>=15
assert min(y for y,row in enumerate(s['frames'][1]['cells']) for v in row if v)>=16
preserved=[]
for rel in ['Assets/Resources/Sprites/Live16x24/north-works.png','ArtSource/Live16x24/north-works/source-cells.json','Assets/Resources/Sprites/Live16x24/opening-items.png']:
    b=(R/rel).read_bytes();assert b==subprocess.check_output(['git','show','HEAD:'+rel],cwd=R)
    preserved.append({'path':rel,'sha256':hashlib.sha256(b).hexdigest(),'matchesHEAD':True})
report={'passed':True,'frames':checks,'rgbaSha256':hashlib.sha256(a.tobytes()).hexdigest(),'sha256':hashlib.sha256((D/'opening-aftermath.png').read_bytes()).hexdigest(),'preserved':preserved,'scope':'Native/source/contact only. Gameplay and physical Deck are separate gates.'}
(D/'verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
