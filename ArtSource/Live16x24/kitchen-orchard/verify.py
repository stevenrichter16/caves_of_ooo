"""Read-only PNG/source checks. Writes only this packet's verification receipt."""
import hashlib,json,re
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
required=('source-cells.json','manifest.json','kitchen-orchard.blend','orchard-hive.png','working-court-v2.png','kitchen-orchard-contact.png','working-court-v2-repeat.png','preserved-art-hashes.json')
missing=[name for name in required if not (HERE/name).is_file()]
assert not missing,'Source RED: native packet not authored: '+', '.join(missing)
s=json.loads((HERE/'source-cells.json').read_text());m=json.loads((HERE/'manifest.json').read_text())
assert len(s['frames'])==4 and len(m['frames'])==4
palette={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()}
hive=Image.open(HERE/'orchard-hive.png').convert('RGBA');floor=Image.open(HERE/'working-court-v2.png').convert('RGBA')
contact=Image.open(HERE/'kitchen-orchard-contact.png').convert('RGBA');repeat=Image.open(HERE/'working-court-v2-repeat.png').convert('RGBA')
assert hive.size==(16,24) and floor.size==(48,24)
assert contact.size==(1024,352) and repeat.size==(256,288)
assert set(hive.getchannel('A').tobytes())=={0,255} and set(floor.getchannel('A').tobytes())=={255}
frames=[]
def components(points):
 pending=set(points);sizes=[]
 while pending:
  todo=[pending.pop()];n=0
  while todo:
   x,y=todo.pop();n+=1
   for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
    if q in pending:pending.remove(q);todo.append(q)
  sizes.append(n)
 return sorted(sizes)
for i,f in enumerate(s['frames']):
 assert len(f['cells'])==24 and all(len(row)==16 for row in f['cells'])
 img=hive if i==0 else floor;offset=0 if i==0 else (i-1)*16
 marked=set();used=set()
 for y,row in enumerate(f['cells']):
  for x,v in enumerate(row):
   expected=palette[v[1]] if v else (0,0,0,0)
   got=img.getpixel((offset+x,y))
   assert got==expected,(f['id'],x,y,got,expected)
   if v and (i==0 or v[1]!='grass'):marked.add((x,y));used.add(v[1])
   display=expected if expected[3] else palette['grass']
   assert all(contact.getpixel(((i*32+8+x)*8+dx,(12+y)*8+dy))==display for dx in range(8) for dy in range(8)),('Contact mismatch',f['id'],x,y)
 assert all(1<=x<=14 and 1<=y<=22 for x,y in marked),'Green/transparent border protects adjacent cells'
 sizes=components(marked)
 if i==0:
  assert 140<=len(marked)<=210 and len(sizes)==1
  assert {'ink','ochre','ochre_dark','linen'}==used
  assert all(f['cells'][y][x][0]=='entrance' for y in range(18,22) for x in range(6,9))
  assert not any('bee' in v[0] for row in f['cells'] for v in row if v),'No unsupported bee animation/activity'
 else:
  assert 120<=len(marked)<=230 and 1<=len(sizes)<=2 and min(sizes)>=24,(f['id'],sizes)
  assert used<= {'stone_olive','stone_grey','worn_surface'}
 frames.append({'id':f['id'],'occupiedOrStonePixels':len(marked),'componentSizes':sizes,'pixelMismatches':0,'contactMismatches':0})
for row in range(3):
 for col in range(4):
  variant=(col+row*2)%3
  for y in range(24):
   for x in range(16):
    expect=floor.getpixel((variant*16+x,y))
    assert all(repeat.getpixel(((col*16+x)*4+dx,(row*24+y)*4+dy))==expect for dx in range(4) for dy in range(4))
for name in ('orchard-hive','working-court-v2'):
 resource=ROOT/f'Assets/Resources/Sprites/Live16x24/{name}.png'
 assert resource.read_bytes()==(HERE/f'{name}.png').read_bytes()
 meta=Path(str(resource)+'.meta').read_text()
 for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
  assert token in meta,(name,token)
 assert re.search(r'^guid: [0-9a-f]{32}$',meta,re.M)
 assert hashlib.sha256(resource.read_bytes()).hexdigest()==m['resources'][name]['sha256']
protected=json.loads((HERE/'preserved-art-hashes.json').read_text())
for path,sha in protected.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Accepted art changed',path)
report={'passed':True,'frames':frames,'preservedSourceAndArtFiles':len(protected),'nativeSizes':{'hive':[16,24],'floor':[48,24]},'alphaValues':{'hive':[0,255],'floor':[255]},'sourceAndResourceBytesMatch':True,'limits':'Native source pixels, direct geometry previews and import metadata only. Runtime selection, palette in lighting, physical activity/removal and gameplay acceptance remain root-owned.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
