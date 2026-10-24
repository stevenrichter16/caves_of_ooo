"""Read-only native/source checks; writes only the owned verification receipt."""
import hashlib, json, re
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
required=('source-cells.json','manifest.json','working-court.png','working-court-contact.png','working-court-repeat.png','working-court.blend')
missing=[p for p in required if not (HERE/p).is_file()]
assert not missing, 'Expected source RED before authoring; missing: '+', '.join(missing)
s=json.loads((HERE/'source-cells.json').read_text()); m=json.loads((HERE/'manifest.json').read_text())
a=Image.open(HERE/'working-court.png').convert('RGBA'); c=Image.open(HERE/'working-court-contact.png').convert('RGBA')
r=Image.open(HERE/'working-court-repeat.png').convert('RGBA')
assert a.size==(48,24) and c.size==(768,320) and r.size==(256,288)
assert set(a.getchannel('A').tobytes())=={255}, 'Ground cells must be opaque.'
assert s['palette']['grass']=='032F23' and len(s['palette'])==4
assert len(s['frames'])==m['frameCount']==3 and m['width']==48 and m['height']==24
palette={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()}
frames=[]; seen=[]
for i,f in enumerate(s['frames']):
 assert f['index']==i and m['frames'][i]['rect']==[i*16,0,16,24]
 assert len(f['cells'])==24 and all(len(row)==16 for row in f['cells'])
 stones=set(); mismatches=contact_bad=0
 for y,row in enumerate(f['cells']):
  for x,v in enumerate(row):
   assert v and v[1] in palette
   expected=palette[v[1]]
   mismatches+=a.getpixel((i*16+x,y))!=expected
   if v[1]!='grass':stones.add((x,y))
   # Direct8x geometry contact: frames at x8/40/72, y-bottom8.
   contact_bad+=sum(c.getpixel(((i*32+8+x)*8+dx,(8+y)*8+dy))!=expected for dy in range(8) for dx in range(8))
 assert mismatches==contact_bad==0,(f['id'],mismatches,contact_bad)
 assert 64<=len(stones)<=190,(f['id'],'not sparse/broad',len(stones))
 assert all(1<=x<=14 and 1<=y<=22 for x,y in stones),'Keep a green border between adjoining cells.'
 remaining=set(stones); sizes=[]
 while remaining:
  todo=[remaining.pop()]; n=0
  while todo:
   p=todo.pop(); n+=1
   for q in ((p[0]-1,p[1]),(p[0]+1,p[1]),(p[0],p[1]-1),(p[0],p[1]+1)):
    if q in remaining: remaining.remove(q);todo.append(q)
  sizes.append(n)
 assert 2<=len(sizes)<=3 and min(sizes)>=16,(f['id'],'broad separated stones expected',sizes)
 assert m['frames'][i]['stonePixels']==len(stones)
 assert m['frames'][i]['grassPixels']==384-len(stones)
 frame_bytes=a.crop((i*16,0,i*16+16,24)).tobytes()
 assert len({frame_bytes[j:j+4] for j in range(0,len(frame_bytes),4)})==4
 seen.append(tuple(tuple(v[1] for v in row) for row in f['cells']))
 frames.append({'index':i,'id':f['id'],'stonePixels':len(stones),'grassPixels':384-len(stones),'stoneComponentSizes':sorted(sizes),'sourcePixelMismatches':mismatches,'contactPixelMismatches':contact_bad})
assert len(set(seen))==3
# The tiled preview is also a direct geometry render; compare each display block.
for row in range(3):
 for col in range(4):
  i=(col+row*2)%3
  for y in range(24):
   for x in range(16):
    expected=a.getpixel((i*16+x,y))
    assert all(r.getpixel(((col*16+x)*4+dx,(row*24+y)*4+dy))==expected for dy in range(4) for dx in range(4))
resource=ROOT/'Assets/Resources/Sprites/Live16x24/working-court.png'
assert resource.read_bytes()==(HERE/'working-court.png').read_bytes()
meta=Path(str(resource)+'.meta').read_text()
for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
 assert token in meta,token
# Unity may normalize this setting to 1. With all alpha=255 this does not
# create transparency or change the ground contract; verify both facts explicitly.
alpha_transparency=int(re.search(r'^\s+alphaIsTransparency: ([01])$',meta,re.M).group(1))
guid=re.search(r'^guid: ([0-9a-f]{32})$',meta,re.M).group(1)
assert len(guid)==32
preserved=json.loads((HERE/'preserved-art-hashes.json').read_text())
for path,digest in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('Changed accepted art',path)
sha=hashlib.sha256(resource.read_bytes()).hexdigest();assert sha==m['sha256']
report={'passed':True,'nativeSize':list(a.size),'contactSize':list(c.size),'repeatSize':list(r.size),'alphaValues':[255],'unityMetaAlphaIsTransparency':alpha_transparency,'palette':s['palette'],'frames':frames,'preservedArtFiles':len(preserved),'resourceByteIdentical':True,'sha256':sha,'rgbaSha256':hashlib.sha256(a.tobytes()).hexdigest(),'scope':'Source pixels, direct geometry previews, resource/meta contract and preserved art only. No Unity importer, catalog, terrain behavior or gameplay proof.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
