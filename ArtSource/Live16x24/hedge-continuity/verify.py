"""Read native outputs without editing pixels; write this packet's receipt only."""
import hashlib,json,re
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
required=('source-cells.json','manifest.json','hedge-continuity.blend','hedge-continuity.png',
          'hedge-continuity-masks.png','hedge-continuity-native-contacts.png',
          'hedge-continuity-contacts.png','preserved-art-hashes.json')
missing=[p for p in required if not (HERE/p).is_file()]
assert not missing,'Source RED: new connected hedge packet absent: '+', '.join(missing)
s=json.loads((HERE/'source-cells.json').read_text())
m=json.loads((HERE/'manifest.json').read_text())
p={k:tuple(bytes.fromhex(v))+(255,) for k,v in s['palette'].items()}
clear=(0,0,0,0);grass=tuple(bytes.fromhex('032F23'))+(255,)
atlas=Image.open(HERE/'hedge-continuity.png').convert('RGBA')
old=Image.open(ROOT/'Assets/Resources/Sprites/Live16x24/north-vegetation.png').convert('RGBA')
assert atlas.size==(256,24) and len(s['frames'])==16
assert set(atlas.getchannel('A').tobytes())=={0,255}
rgba=[]
for i,f in enumerate(s['frames']):
 assert f['mask']==i and len(f['cells'])==24 and all(len(row)==16 for row in f['cells'])
 pixels=[[p[v[1]] if v else clear for v in row] for row in f['cells']]
 rgba.append(pixels)
 for y in range(24):
  for x in range(16):
   assert atlas.getpixel((i*16+x,y))==pixels[y][x],('Native source mismatch',i,x,y)
   if i==0:assert old.getpixel((16+x,y))==pixels[y][x],('Approved isolated pixels changed',x,y)
 # Every connected derivative is one continuous four-connected mass.
 if i:
  todo={(x,y) for y in range(24) for x in range(16) if pixels[y][x][3]}
  pending=[todo.pop()]
  while pending:
   x,y=pending.pop()
   for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
    if q in todo:todo.remove(q);pending.append(q)
  assert not todo,('Disconnected leaf islands',i)
  assert all(bool(pixels[0][x][3])==bool(i&1 and 3<=x<=12) for x in range(16))
  assert all(bool(pixels[23][x][3])==bool(i&4 and 3<=x<=12) for x in range(16))
  assert all(bool(pixels[y][0][3])==bool(i&8 and 6<=y<=17) for y in range(24))
  assert all(bool(pixels[y][15][3])==bool(i&2 and 6<=y<=17) for y in range(24))
  assert {v[1] for row in f['cells'] for v in row if v} <= {'leaf_dark','leaf_mid','leaf','wood_dark'}
  assert sum(v is not None and v[1]=='leaf' for row in f['cells'] for v in row)<=18
for a in range(1,16):
 for b in range(1,16):
  if a&2 and b&8:assert [r[15] for r in rgba[a]]==[r[0] for r in rgba[b]],('E/W join',a,b)
  if a&4 and b&1:assert rgba[a][23]==rgba[b][0],('S/N join',a,b)
native=Image.open(HERE/'hedge-continuity-native-contacts.png').convert('RGBA')
large=Image.open(HERE/'hedge-continuity-contacts.png').convert('RGBA')
masks=Image.open(HERE/'hedge-continuity-masks.png').convert('RGBA')
assert masks.size==(768,864)
for i in range(16):
 for y in range(24):
  for x in range(16):
   expected=rgba[i][y][x] if rgba[i][y][x][3] else grass
   xx,yy=(i%4)*32+8+x,(i//4)*36+4+y
   assert all(masks.getpixel((xx*6+dx,yy*6+dy))==expected for dx in range(6) for dy in range(6)),('Mask contact',i,x,y)
assert native.size==(192,144) and large.size==(768,576)
placed={(v['x'],v['y']):v['mask'] for v in m['contactCells']}
for cy in range(6):
 for cx in range(12):
  mask=placed.get((cx,cy))
  if mask is not None:
   actual=sum(bit for dx,dy,bit in ((0,-1,1),(1,0,2),(0,1,4),(-1,0,8)) if (cx+dx,cy+dy) in placed)
   assert actual==mask,('Contact mask',cx,cy,mask,actual)
  for y in range(24):
   for x in range(16):
    expected=rgba[mask][y][x] if mask is not None else clear
    if not expected[3]:expected=grass
    xx,yy=cx*16+x,cy*24+y
    assert native.getpixel((xx,yy))==expected,('Native contact',xx,yy)
    assert all(large.getpixel((xx*4+dx,yy*4+dy))==expected for dx in range(4) for dy in range(4)),('Integer contact',xx,yy)
assert all(native.getpixel((7*16+x,y))==grass for y in range(24) for x in range(16)), 'Real one-cell gap closed'
resource=ROOT/'Assets/Resources/Sprites/Live16x24/hedge-continuity.png'
assert resource.read_bytes()==(HERE/'hedge-continuity.png').read_bytes()
meta=Path(str(resource)+'.meta').read_text()
for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
 assert token in meta,token
assert re.search(r'^guid: [0-9a-f]{32}$',meta,re.M)
protected=json.loads((HERE/'preserved-art-hashes.json').read_text())
for path,sha in protected.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Prior source/art changed',path)
report={'passed':True,'nativeSize':[256,24],'frameCount':16,'sourcePixelMismatches':0,'isolatedFramePixelMismatches':0,
        'connectedJoinPixelMismatches':0,'contactPixelMismatches':0,'preservedFiles':len(protected),
        'nativeRGBAHash':hashlib.sha256(atlas.tobytes()).hexdigest(),
        'limits':'Source geometry/native pixels/import metadata only; root owns Unity integration, actual gameplay and performance acceptance.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
