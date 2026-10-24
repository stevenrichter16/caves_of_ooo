"""Read-only native state/source verification; no image editing."""
import hashlib,json,re
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
required=('kitchen-service.png','kitchen-service-contact.png','kitchen-service.blend','source-cells.json','manifest.json')
missing=[x for x in required if not (HERE/x).exists()]
assert not missing,'Expected source RED before authoring; missing: '+', '.join(missing)
s=json.loads((HERE/'source-cells.json').read_text())
original=json.loads((ROOT/'ArtSource/Live16x24/east-kitchen/source-cells.json').read_text())
manifest=json.loads((HERE/'manifest.json').read_text())
a=Image.open(HERE/'kitchen-service.png').convert('RGBA');contact=Image.open(HERE/'kitchen-service-contact.png').convert('RGBA')
assert a.size==(32,24) and contact.size==(1024,320)
assert set(a.getchannel('A').tobytes())=={0,255}
assert len(s['frames'])==2 and s['palette']==original['palette']
palette={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()};bg=tuple(bytes.fromhex('032F23'))+(255,)
frame_reports=[]
for i,base_index in enumerate((5,7)):
 f=s['frames'][i];base=original['frames'][base_index]['cells']
 assert f['baseSourceFrame']==base_index and f['baseCells']==base
 assert all(len(grid)==24 and all(len(row)==16 for row in grid) for grid in (f['baseCells'],f['overlayCells'],f['cells']))
 count=0;parts=set();colors=set();opaque=[]
 for y in range(24):
  for x in range(16):
   overlay=f['overlayCells'][y][x];value=overlay or base[y][x]
   assert f['cells'][y][x]==value,('base altered outside overlay',i,x,y)
   if overlay:count+=1;parts.add(overlay[0])
   expected=palette[value[1]] if value else (0,0,0,0);pixel=a.getpixel((i*16+x,y))
   assert pixel==expected or expected[3]==pixel[3]==0,(i,x,y,pixel,expected)
   if value:colors.add(expected);opaque.append((x,y))
 assert 3<=len(colors)<=7
 assert min(y for x,y in opaque)==13 and max(y for x,y in opaque)==22
 assert all(f['cells'][y][x]==base[y][x] for x,y in ((1,17),(14,17),(5,22),(10,22))), 'Handles and baseline must remain approved.'
 if i==0:
  assert all(p.startswith('cloth-') for p in parts)
  assert all(f['overlayCells'][y][x] for y in (16,17) for x in range(4,12)),'Working cloth covers original empty cavity.'
  assert all(f['cells'][y][x]==base[y][x] for x,y in ((8,19),(7,20),(7,21),(8,22))),'Retain repaired clay seam.'
 else:
  meal=original['frames'][13]['cells']
  for y in range(24):
   for x in range(16):
    expected=meal[y+3][x] if y+3<24 else None
    assert f['overlayCells'][y][x]==expected,('approved meal must be translated exactly, not repainted',x,y)
  assert count==52
 frame_reports.append({'index':i,'id':f['id'],'baseSourceFrame':base_index,'overlayPixels':count,'opaquePixels':len(opaque),'opaqueColors':len(colors),'sourcePixelMismatches':0})
# Four direct8x comparison cells: original repaired, working, original board, ready.
display=[original['frames'][5]['cells'],s['frames'][0]['cells'],original['frames'][7]['cells'],s['frames'][1]['cells']]
for i,cells in enumerate(display):
 for y,row in enumerate(cells):
  for x,value in enumerate(row):
   expected=palette[value[1]] if value else bg
   assert all(contact.getpixel(((i*32+8+x)*8+dx,(8+y)*8+dy))==expected for dx in range(8) for dy in range(8)),('contact mismatch',i,x,y)
resource=ROOT/'Assets/Resources/Sprites/Live16x24/kitchen-service.png'
assert resource.read_bytes()==(HERE/'kitchen-service.png').read_bytes()
meta=Path(str(resource)+'.meta').read_text();assert re.search(r'^guid: [0-9a-f]{32}$',meta,re.M)
for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
 assert token in meta,token
preserved=json.loads((HERE/'preserved-source-hashes.json').read_text())
for path,sha in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Changed original',path)
sha=hashlib.sha256(resource.read_bytes()).hexdigest();assert manifest['sha256']==sha
report={'passed':True,'atlasSize':list(a.size),'contactSize':list(contact.size),'frames':frame_reports,'sourcePixelMismatches':0,'contactPixelMismatches':0,'preservedSourceFiles':len(preserved),'resourceByteIdentical':True,'sha256':sha,'scope':'Source/base/overlay pixels and metadata only. Runtime state/contents/fog/reader selection and ordinary gameplay remain separate.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
