"""Read-only raster/source/import-contract verification; only writes verification.json.
Run before native authoring for missing-output RED, then after direct Blender export.
"""
import hashlib,json,re
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
EXPECTED=('TinkersForge','Oven','Bed','Chair','ConnectedBatchPan','ConnectedBatchPan','ConnectedKitchenEscrow','ConnectedKitchenPickup','ClaspbeanCrop','ClaspbeanCrop','ClaspbeanCrop','ClaspbeanPulp','ClaspbeanSeed','FieldMeal','CharmFlowers','CopperPipe','SteamVent','MillStone')
REQUIRED=('source-cells.json','manifest.json','east-kitchen.png','east-kitchen-contact.png','east-kitchen.blend')
missing=[x for x in REQUIRED if not (HERE/x).is_file()]
assert not missing,'Expected source RED before authoring; missing: '+', '.join(missing)
s=json.loads((HERE/'source-cells.json').read_text());m=json.loads((HERE/'manifest.json').read_text())
a=Image.open(HERE/'east-kitchen.png').convert('RGBA');c=Image.open(HERE/'east-kitchen-contact.png').convert('RGBA')
assert a.size==(288,24) and c.size==(1536,1024)
assert tuple(f['blueprint'] for f in s['frames'])==EXPECTED
assert len(m['frames'])==18 and m['frameCount']==18
assert m['pixelsPerUnit']==16 and m['tilePivotNormalized']==[.5,.5]
assert m['filterMode']=='Point' and m['wrapMode']=='Clamp' and not m['mipmaps'] and m['compression']=='None'
assert set(a.getchannel('A').tobytes())=={0,255}
palette={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()}
checks=[];masks=[];bg=(3,47,35,255)
for i,f in enumerate(s['frames']):
 e=m['frames'][i];assert f['index']==i and e['rect']==[i*16,0,16,24]
 assert len(f['cells'])==24 and all(len(row)==16 for row in f['cells'])
 pts=set();colors=set();native_bad=contact_bad=0
 for y,row in enumerate(f['cells']):
  for x,v in enumerate(row):
   actual=a.getpixel((i*16+x,y))
   if v:
    expected=palette[v[1]];pts.add((x,y));colors.add(expected);native_bad+=actual!=expected
   else:expected=bg;native_bad+=actual[3]!=0
   left=(i%6*32+8+x)*8;top=(10+i//6*36+y)*8
   contact_bad+=sum(c.getpixel((left+sx,top+sy))!=expected for sy in range(8) for sx in range(8))
 assert native_bad==contact_bad==0,(f['id'],native_bad,contact_bad)
 assert pts and all(1<=x<=14 and 0<=y<=22 for x,y in pts)
 assert max(y for x,y in pts)==22 and all(v is None for v in f['cells'][23])
 assert 3<=len(colors)<=6,(f['id'],len(colors))
 bounds=[min(x for x,y in pts),min(y for x,y in pts),max(x for x,y in pts)+1,23]
 assert e['alphaBoundsTopLeft']==bounds
 assert e['occupiedPixels']==len(pts)
 assert set(e['palette'])=={''.join(f'{n:02X}' for n in v[:3]) for v in colors}
 remaining=set(pts);components=0
 while remaining:
  todo=[remaining.pop()];components+=1
  while todo:
   x,y=todo.pop()
   for n in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
    if n in remaining:remaining.remove(n);todo.append(n)
 assert components<= (3 if i==14 else 1),(f['id'],'unintended detached pixels',components)
 masks.append(tuple(sorted((x-bounds[0],y-bounds[1]) for x,y in pts)))
 checks.append({'index':i,'id':f['id'],'blueprint':EXPECTED[i],'colors':len(colors),'opaquePixels':len(pts),'bounds':bounds,'components':components,'sourcePixelMismatches':native_bad,'contactPixelMismatches':contact_bad})
assert s['frames'][4]['cells']!=s['frames'][5]['cells'],'Repair must change native pixels.'
assert masks[4]==masks[5],'Repair changes clay integrity, not the footprint.'
assert len(set(masks))>=16,'All separate roles need silhouette separation; only real state pairs may share outlines.'
assert checks[2]['bounds'][3]-checks[2]['bounds'][1]>checks[2]['bounds'][2]-checks[2]['bounds'][0],'Cot must be vertical.'
assert len({masks[i] for i in (8,9,10)})==3,'Three real crop stages must differ.'
assert checks[11]['bounds'][3]-checks[11]['bounds'][1]<=8 and checks[12]['bounds'][3]-checks[12]['bounds'][1]<=9
for i in (0,1,4,5,6,7,16):
 parts={v[0] for row in s['frames'][i]['cells'] for v in row if v}
 assert not any(any(t in p for t in ('flame','steam','ready','working','food','contents','smoke','glow')) for p in parts),(i,parts)
resource=ROOT/'Assets/Resources/Sprites/Live16x24/east-kitchen.png'
cook_source=ROOT/'Assets/Art/Cook16x24/cook-16x24.png';cook=resource.with_name('east-kitchen-cook.png')
assert resource.read_bytes()==(HERE/'east-kitchen.png').read_bytes()
assert cook.read_bytes()==cook_source.read_bytes() and Image.open(cook).size==(16,24)
for p in (resource,cook):
 meta=Path(str(p)+'.meta').read_text()
 for line in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
  assert line in meta,(p,line)
 assert 'textureCompression: 1' not in meta and 'textureCompression: 2' not in meta
 guid=re.search(r'^guid: (\w+)$',meta,re.M).group(1)
 conflicts=[]
 for old in (ROOT/'Assets').rglob('*.meta'):
  if old!=Path(str(p)+'.meta') and re.search(r'^guid: '+guid+r'$',old.read_text(errors='ignore'),re.M):conflicts.append(str(old))
 assert not conflicts,('duplicate GUID',guid,conflicts)
sha=hashlib.sha256((HERE/'east-kitchen.png').read_bytes()).hexdigest();assert m['sha256']==sha
prior=[]
for group in ('anchors','states','motion','items','cellar','north-vegetation','north-works','north-plot','plot-products','north-works-products'):
 folder=HERE.parent/group;accepted=json.loads((folder/'manifest.json').read_text());p=folder/accepted['artifact'];h=hashlib.sha256(p.read_bytes()).hexdigest();assert h==accepted['sha256'];prior.append({'path':str(p.relative_to(ROOT)),'sha256':h})
report={'passed':True,'nativeSize':list(a.size),'contactSize':list(c.size),'frames':checks,'distinctNormalizedOutlines':len(set(masks)),'alphaValues':[0,255],'sha256':sha,'rgbaSha256':hashlib.sha256(a.tobytes()).hexdigest(),'cookByteIdentical':True,'cookSha256':hashlib.sha256(cook.read_bytes()).hexdigest(),'priorRasters':prior,'scope':'Static native/source/contact and resource-byte/meta contract only. No Unity import, owner routing, state transitions or ordinary gameplay acceptance.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
