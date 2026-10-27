"""Decode-only native harvest source checks; never creates or edits raster pixels."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image
sys.dont_write_bytecode = True
OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
OWNERS = ['PaleSalt', 'Bone', 'SaltbriarSprig', 'ChoirIron', 'Honeycomb', 'GroveRed', 'Tepuibone', 'KnotflaxCord']
PARTS = [
 {'short-crystal-face', 'broken-salt-base'}, {'worn-shaft', 'broken-end', 'porous-cut'},
 {'cut-stem', 'salt-rimed-leaf'}, {'dark-ore-face', 'red-fracture'},
 {'wax-face', 'cell-hollow', 'torn-edge'}, {'raw-growth-body', 'unpurified-fold', 'torn-stem'},
 {'stone-cut-face', 'closed-wood-grain'}, {'twisted-coil', 'crossing-knot', 'loose-tail'}]
ALLOWED = [set(x.split()) for x in [
 'mineral mineral-edge mineral-shadow', 'bone bone-light mineral-shadow',
 'briar briar-leaf briar-salt briar-shadow', 'stone-dark stone rust stone-light',
 'ochre ochre-dark grain cream', 'rust rust-light ink leather',
 'stone-cut mineral bone-light mineral-shadow', 'linen cream leather']]
PALETTE = {'mineral':'C6BDA0','mineral-edge':'E0D6B5','mineral-shadow':'8D826C',
 'bone':'AFA688','bone-light':'D2C7A6','briar':'6C705E','briar-leaf':'8E9480','briar-salt':'B9B79C','briar-shadow':'535449',
 'stone-dark':'3C5148','stone':'526657','rust':'9B442B','stone-light':'7D8E71',
 'ochre':'C99538','ochre-dark':'9C6B32','grain':'D8A446','cream':'E4D0A0',
 'rust-light':'B85834','ink':'342E25','leather':'5F4830','stone-cut':'B29B73','linen':'B5A47E'}

def holes(cells):
 empty={(x,y) for y,row in enumerate(cells) for x,c in enumerate(row) if c is None}
 outside={p for p in empty if p[0] in (0,15) or p[1] in (0,23)}
 todo=list(outside)
 while todo:
  x,y=todo.pop()
  for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
   if p in empty and p not in outside: outside.add(p);todo.append(p)
 return empty-outside

def contract(source):
 assert source['palette']==PALETTE,'Exact approved-family palette'
 fs=source['frames'];assert len(fs)==8,'Eight product frames required'
 outlines=[];measure=[]
 for i,f in enumerate(fs):
  assert f['index']==i and f['blueprint']==OWNERS[i],'Exact frame/owner order'
  cells=f['cells'];assert len(cells)==24 and all(len(r)==16 for r in cells),'Native cell grid'
  assert all(c is None or isinstance(c,list) and len(c)==2 and isinstance(c[0],str) and c[1] in PALETTE for r in cells for c in r),'Known named palette cell'
  points={(x,y) for y,row in enumerate(cells) for x,c in enumerate(row) if c}
  assert 20<=len(points)<=105,'Compact carried product density'
  xs,ys=zip(*points);x0,y0,x1,y1=min(xs),min(ys),max(xs)+1,max(ys)+1
  assert 2<=x0<x1<=14 and 8<=y0<y1==23,'Clear margins and row22 baseline'
  assert 5<=x1-x0<=12 and 5<=y1-y0<=15,'Native loose item size'
  normalized=frozenset((x-x0,y-y0) for x,y in points)
  assert normalized not in outlines,'Distinct normalized outline';outlines.append(normalized)
  pending=set(points);todo=[pending.pop()]
  while todo:
   x,y=todo.pop()
   for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
    if p in pending:pending.remove(p);todo.append(p)
  assert not pending,'One connected portable identity'
  colors={c[1] for r in cells for c in r if c};parts={c[0] for r in cells for c in r if c}
  assert 3<=len(colors)<=4 and colors<=ALLOWED[i],'Bounded family colors'
  assert PARTS[i]<=parts,'Required material parts'
  if i==0:assert y1-y0<=9,'Salt is a short broken product, not tall vein'
  if i==1:assert y1-y0>=10 and sum(c is not None and c[0]=='porous-cut' for r in cells for c in r)<=3,'Bone shaft and bounded porous end'
  if i==4:assert 3<=sum(c is not None and c[0]=='cell-hollow' for r in cells for c in r)<=8,'Visible bounded wax hollows'
  if i==6:
   assert x1-x0>=y1-y0,'Tepuibone is a squat stone, not animal shaft'
   assert all(x0<x<x1-1 and y0<y<y1-1 for y,row in enumerate(cells) for x,c in enumerate(row) if c and c[0]=='closed-wood-grain'),'Stone grain enclosed inside mass'
  if i==7:assert 2<=len(holes(cells))<=12,'Cord retains a clear bounded loop'
  measure.append({'blueprint':OWNERS[i],'opaquePixels':len(points),'bounds':[x0,y0,x1,y1],'colors':len(colors),'holes':len(holes(cells))})
 return measure

def verify(check_unity=False):
 needed=['world-harvest-products.png','world-harvest-products.png.meta','world-harvest-products.blend','source-cells.json','manifest.json','contact-native.png','contact.png','preserved-inputs.json']
 missing=[n for n in needed if not (OUT/n).is_file()];assert not missing,'Source RED: missing '+', '.join(missing)
 source=json.loads((OUT/'source-cells.json').read_text());manifest=json.loads((OUT/'manifest.json').read_text());measure=contract(source)
 im=Image.open(OUT/'world-harvest-products.png').convert('RGBA')
 assert im.size==(128,24) and set(im.getchannel('A').tobytes())=={0,255},'Native geometry/alpha'
 assert [manifest[k] for k in ('width','height','frameCount','frameWidth','frameHeight')]==[128,24,8,16,24]
 assert manifest['resource']=='Sprites/Live16x24/world-harvest-products' and manifest['unityExportDeferred'] is True
 rgba={k:tuple(bytes.fromhex(v))+(255,) for k,v in PALETTE.items()}
 for i,f in enumerate(source['frames']):
  assert manifest['frames'][i]['blueprint']==OWNERS[i] and manifest['frames'][i]['unityRect']==[16*i,0,16,24]
  for y,row in enumerate(f['cells']):
   for x,c in enumerate(row):assert im.getpixel((i*16+x,y))==(rgba[c[1]] if c else (0,0,0,0)),('Source/native pixel mismatch',i,x,y)
 for filename,scale in [('contact-native.png',1),('contact.png',8)]:
  contact=Image.open(OUT/filename).convert('RGBA');cm=manifest['contact'];assert contact.size==tuple(n*scale for n in cm['nativeSize'])
  for p in cm['instances']:
   bg=tuple(bytes.fromhex(p['background']))+(255,)
   for y,row in enumerate(source['frames'][p['frame']]['cells']):
    for x,c in enumerate(row):
     wanted=rgba[c[1]] if c else bg
     for dy in range(scale):
      for dx in range(scale):assert contact.getpixel(((p['topLeft'][0]+x)*scale+dx,(p['topLeft'][1]+y)*scale+dy))==wanted,('Direct contact mismatch',filename,p,x,y)
 preserved=json.loads((OUT/'preserved-inputs.json').read_text())
 for path,sha in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Approved input changed',path)
 sys.path.insert(0,str(OUT.parent/'cudgel'))
 from metadata_contract import parse_metadata,assert_authored_subset
 meta=parse_metadata((OUT/'world-harvest-products.png.meta').read_text())
 assert len(meta['guid'])==32 and all(c in '0123456789abcdef' for c in meta['guid'])
 required={'fileFormatVersion':2,'TextureImporter':{'mipmaps':{'enableMipMap':0,'sRGBTexture':1},'textureSettings':{'filterMode':0,'wrapU':1,'wrapV':1,'wrapW':1},'nPOTScale':0,'spriteMode':1,'spriteMeshType':0,'spritePixelsToUnits':16,'spritePivot':{'x':.5,'y':.5},'spriteSheet':{'sprites':[],'outline':[],'physicsShape':[],'vertices':[]},'platformSettings':[{'textureCompression':0}]}}
 assert_authored_subset(required,meta)
 counters=[]
 def replace_part(s,index,part):
  for row in s['frames'][index]['cells']:
   for c in row:
    if c and c[0]==part:c[0]='unrecognized-replacement'
 def fill_loop(s):
  c=s['frames'][7]['cells']
  for x,y in holes(c):c[y][x]=['twisted-coil','linen']
 for name,mutate in [
  ('missing-frame',lambda s:s['frames'].pop()),
  ('wrong-owner',lambda s:s['frames'][1].__setitem__('blueprint','Tepuibone')),
  ('duplicate-outline',lambda s:s['frames'][1].__setitem__('cells',copy.deepcopy(s['frames'][0]['cells']))),
  ('opaque-bottom',lambda s:s['frames'][0]['cells'][23].__setitem__(8,['short-crystal-face','mineral'])),
  ('unknown-palette',lambda s:s['frames'][0]['cells'][20].__setitem__(8,['short-crystal-face','unknown'])),
  ('wrong-grid-width',lambda s:s['frames'][0]['cells'][0].append(None)),
  ('missing-bone-cut',lambda s:replace_part(s,1,'porous-cut')),
  ('cord-loop-filled',fill_loop),
  ('missing-wax-hollows',lambda s:replace_part(s,4,'cell-hollow')),
  ('missing-stone-grain',lambda s:replace_part(s,6,'closed-wood-grain')),
  ('salt-neon-palette',lambda s:s['palette'].__setitem__('mineral','FFFFFF')),
  ('purified-growth-claim',lambda s:replace_part(s,5,'unpurified-fold'))]:
  changed=copy.deepcopy(source);mutate(changed)
  try:contract(changed)
  except AssertionError:counters.append(name)
  else:raise AssertionError('Counter accepted '+name)
 changed=copy.deepcopy(meta);changed['TextureImporter']['textureSettings']['filterMode']=1
 try:assert_authored_subset(required,changed)
 except AssertionError:counters.append('metadata-filter-changed')
 else:raise AssertionError('Metadata counter accepted')
 if check_unity:
  target=ROOT/'Assets/Resources/Sprites/Live16x24/world-harvest-products.png'
  assert target.read_bytes()==(OUT/'world-harvest-products.png').read_bytes()
  assert_authored_subset(meta,parse_metadata(Path(str(target)+'.meta').read_text()))
 sha=hashlib.sha256((OUT/'world-harvest-products.png').read_bytes()).hexdigest();assert manifest['sha256']==sha
 return {'passed':True,'size':[128,24],'frames':8,'measurements':measure,'allNormalizedOutlinesDistinct':True,'binaryAlpha':True,'sourcePixelMismatches':0,'contactPixelMismatches':0,'preservedInputsUnchanged':len(preserved),'counterchecksRejected':counters,'nativeSHA256':sha,'decodedPixelSHA256':hashlib.sha256(im.tobytes()).hexdigest(),'runtimeCopyChecked':check_unity,'limits':'Source-only geometry, decoded pixels and authored metadata. No native Unity tests, gameplay, inventory custody, status presentation, all-biome journeys or performance acceptance.'}

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--check-unity',action='store_true');args=parser.parse_args()
 try:r=verify(args.check_unity)
 except (AssertionError,OSError,KeyError,ValueError) as e:r={'passed':False,'error':str(e)}
 (OUT/'verification.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r,indent=2));raise SystemExit(0 if r['passed'] else 1)
