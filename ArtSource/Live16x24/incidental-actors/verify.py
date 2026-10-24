"""Read-only art/source validation; writes only its owned verification receipt."""
import hashlib,json,re
from pathlib import Path
from PIL import Image

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
REQUIRED=('farmer.png','farmer-contact.png','incidental-actors.blend','source-cells.json','manifest.json')
missing=[name for name in REQUIRED if not (HERE/name).is_file()]
assert not missing,'Expected source RED before authoring; missing: '+', '.join(missing)
source=json.loads((HERE/'source-cells.json').read_text())
manifest=json.loads((HERE/'manifest.json').read_text())
farmer=Image.open(HERE/'farmer.png').convert('RGBA')
contact=Image.open(HERE/'farmer-contact.png').convert('RGBA')
assert farmer.size==(16,24) and contact.size==(512,320)
assert set(farmer.getchannel('A').tobytes())=={0,255}
assert farmer.getbbox()[3]==23,'Feet end at native row22; final row is empty.'
assert 2<=farmer.getbbox()[0] and farmer.getbbox()[2]<=14
assert 3<=len(source['palette'])<=8
palette={k:tuple(bytes.fromhex(v))+(255,) for k,v in source['palette'].items()}
cells=source['cells'];assert len(cells)==24 and all(len(row)==16 for row in cells)
opaque=set();colors=set();bg=tuple(bytes.fromhex('032F23'))+(255,)
for y,row in enumerate(cells):
 for x,value in enumerate(row):
  expected=palette[value[1]] if value else (0,0,0,0)
  pixel=farmer.getpixel((x,y))
  assert pixel==expected or expected[3]==pixel[3]==0,(x,y,pixel,expected)
  if value:opaque.add((x,y));colors.add(pixel)
  displayed=expected if expected[3] else bg
  assert all(contact.getpixel(((24+x)*8+dx,(8+y)*8+dy))==displayed for dx in range(8) for dy in range(8)),('contact mismatch',x,y)
assert 90<=len(opaque)<=220,'Civilian silhouette should remain bounded and airy.'
assert 3<=len(colors)<=8
feet=[x for x in range(16) if (x,22) in opaque]
assert feet and any(x not in feet for x in range(min(feet),max(feet)+1)),'Separated feet required.'
assert all('hoe' not in str(v).lower() and 'bundle' not in str(v).lower() and 'sleep' not in str(v).lower() for row in cells for v in row if v)
resource=ROOT/'Assets/Resources/Sprites/Live16x24/farmer.png'
grazer=ROOT/'Assets/Resources/Sprites/Live16x24/reedback-grazer.png'
approved=ROOT/'Assets/Art/ReedbackGrazer16x24/03-ridge.png'
assert resource.read_bytes()==(HERE/'farmer.png').read_bytes()
approved_sha='29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad'
assert hashlib.sha256(approved.read_bytes()).hexdigest()==approved_sha
assert grazer.read_bytes()==approved.read_bytes(),'Grazer must be the exact approved source bytes.'
assert Image.open(grazer).size==(16,24)
for path in (resource,grazer):
 meta=Path(str(path)+'.meta').read_text()
 assert re.search(r'^guid: [0-9a-f]{32}$',meta,re.M)
 for token in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','wrapW: 1','nPOTScale: 0','textureCompression: 0','spritePixelsToUnits: 16'):
  assert token in meta,(path,token)
preserved=json.loads((HERE/'preserved-art-hashes.json').read_text())
for path,sha in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Changed accepted art',path)
villager=Image.open(ROOT/'ArtSource/Live16x24/cast/opening-cast.png').convert('RGBA').crop((0,0,16,24))
cook=Image.open(ROOT/'Assets/Art/Cook16x24/cook-16x24.png').convert('RGBA')
assert farmer.tobytes() not in (villager.tobytes(),cook.tobytes()),'Farmer must not alias another civilian.'
sha=hashlib.sha256(resource.read_bytes()).hexdigest()
assert manifest['farmer']['sha256']==sha and manifest['grazer']['sha256']==approved_sha
report={'passed':True,'farmerSize':list(farmer.size),'farmerBounds':list(farmer.getbbox()),'opaquePixels':len(opaque),'opaqueColors':len(colors),'alpha':[0,255],'contactSize':list(contact.size),'sourcePixelMismatches':0,'contactPixelMismatches':0,'farmerSha256':sha,'grazerExactApprovedSha256':approved_sha,'preservedArtFiles':len(preserved),'scope':'Source art, exact approved copy and authored import metadata only; no Unity, actor selection, state, motion, save/load or gameplay claim.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
