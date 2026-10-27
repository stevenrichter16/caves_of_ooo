"""Decode-only Beating source contract. Does not create or modify image pixels."""
import copy
import hashlib
import json
from pathlib import Path
from PIL import Image
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
OWNERS=['Sand']*4+['SaltCrust']*4+['DuneCrest']*4+['SandstoneFloor']*4+['SandstoneWall']*2+['Rock']*2+['BrinePool']*8+['PaleSaltVein','Bones','Saltbriar','Saltbriar','RoadStone','RoadStone']
OPAQUE=set(range(18))|set(range(20,28))|{32,33}
PRESERVED_INDICES=tuple(i for i in range(32) if i not in range(8,12))

def contract(s):
 p=s['palette'];fs=s['frames'];assert len(fs)==34,'Expected34 frames including warm packed road'
 assert all(len(v)==6 and all(c in '0123456789ABCDEF' for c in v) for v in p.values())
 seen=[]
 for i,f in enumerate(fs):
  c=f['cells'];assert f['index']==i and f['blueprint']==OWNERS[i]
  assert len(c)==24 and all(len(r)==16 for r in c),'Native cell shape'
  assert all(v is None or isinstance(v,list) and len(v)==2 and v[1] in p for r in c for v in r),'Unknown palette/cell'
  pixels=[p[v[1]] if v else None for r in c for v in r];assert any(pixels) and pixels not in seen,'Empty/duplicate frame';seen.append(pixels)
  if i in OPAQUE:assert all(pixels),'Opaque terrain/liquid becomes a hole'
  else:assert any(v is None for v in pixels) and all(v is None for v in c[23]),'Transparent prop baseline'
 for i in range(4):
  cells=fs[i]['cells'];colors=[c[1] for r in cells for c in r]
  assert set(colors)<=set(('sand','sand-ripple','sand-shadow')) and colors.count('sand')>=327,'Sand becomes granular carpet'
  assert all(c[1]=='sand' for c in cells[0]+cells[23]) and all(r[0][1]==r[15][1]=='sand' for r in cells),'Sand framed seams'
 for i in range(4,8):
  cells=fs[i]['cells'];colors=[c[1] for r in cells for c in r]
  assert set(colors)<=set(('salt','salt-crack','salt-light')) and colors.count('salt')>=300,'Salt overtextured'
  assert 3<=colors.count('salt-crack')<=45,'Salt crack budget'
 for i in range(8,12):
  colors=[c[1] for r in fs[i]['cells'] for c in r]
  assert set(colors)<=set(('dune','dune-lee','dune-edge','dune-strata')) and colors.count('dune')>=355,'Dune overtextured or foreign material'
  assert 1<=colors.count('dune-lee')<=18 and colors.count('dune-edge')<=6,'Quiet dune lee/edge budget'
  cells=fs[i]['cells']
  assert all(c[1]=='dune' for c in cells[0]+cells[23]) and all(r[0][1]==r[15][1]=='dune' for r in cells),'Joined dune stripe seam'
 for i in range(12,16):
  colors=[c[1] for r in fs[i]['cells'] for c in r]
  assert set(colors)<=set(('floor','floor-join','floor-worn')) and colors.count('floor')>=320,'Floor noisy/framed'
 for i in range(20,28):
  f=fs[i];colors=[c[1] for r in f['cells'] for c in r]
  expected='filled' if i<24 else 'depleted-wet-film'
  assert f['stockState']==expected and f['stillWet'] is True,'False liquid safety state'
  allowed=('brine','brine-deep','brine-reflection') if i<24 else ('film','film-deep','film-reflection')
  assert set(colors)<=set(allowed),'Dry or wrong liquid material'
  assert colors.count(allowed[0])>=355 and 1<=colors.count(allowed[2])<=7,'Brine carpet or missing wet glint'
  assert all(c[1]==allowed[0] for c in f['cells'][0]+f['cells'][23]) and all(r[0][1]==r[15][1]==allowed[0] for r in f['cells']),'Liquid border seam'
 for i in (18,19,28,29,30,31):
  f=fs[i];cs=[c for r in f['cells'] for c in r if c]
  assert 25<=len(cs)<=160,'Prop density'
 for i in (30,31):
  parts={c[0] for r in fs[i]['cells'] for c in r if c}
  assert {'salt-rimed-leaf','branch-stem','root-crown'}<=parts,'Briar becomes dead brush'
 for i in (32,33):
  cells=fs[i]['cells'];colors=[c[1] for r in cells for c in r]
  assert set(colors)<=set(('road','road-grain','road-dust','road-dark')) and colors.count('road')>=350,'Road becomes grass, brick or granular carpet'
  assert all(c[1]=='road' for c in cells[0]+cells[23]) and all(r[0][1]==r[15][1]=='road' for r in cells),'Road framed seams'
 return True

def verify():
 needed=['beating-materials.png','beating-materials.blend','manifest.json','source-cells.json','contact-native.png','contact.png','preserved-inputs.json']
 missing=[n for n in needed if not (OUT/n).is_file()];assert not missing,'Source RED: missing '+', '.join(missing)
 s=json.loads((OUT/'source-cells.json').read_text());m=json.loads((OUT/'manifest.json').read_text());contract(s)
 im=Image.open(OUT/'beating-materials.png').convert('RGBA');assert im.size==(544,24) and set(im.getchannel('A').tobytes())=={0,255}
 assert [m[k] for k in ('width','height','frameCount','frameWidth','frameHeight')]==[544,24,34,16,24]
 first=json.loads((OUT/'revisions/first-trial/source-cells.json').read_text())
 old=Image.open(OUT/'revisions/first-trial/beating-materials.png').convert('RGBA')
 for i in PRESERVED_INDICES:
  assert s['frames'][i]==first['frames'][i],('Unchanged frame cells altered',i)
  assert all(im.getpixel((i*16+x,y))==old.getpixel((i*16+x,y)) for y in range(24) for x in range(16)),('Unchanged native frame altered',i)
 history=json.loads((OUT/'revisions/first-trial/preserved-first-trial.json').read_text())
 for n,sha in history.items():assert hashlib.sha256((OUT/'revisions/first-trial'/n).read_bytes()).hexdigest()==sha,('First trial revision changed',n)
 counts=[]
 for i,f in enumerate(s['frames']):
  counts.append(sum(bool(c) for r in f['cells'] for c in r))
  for y,row in enumerate(f['cells']):
   for x,c in enumerate(row):
    expected=tuple(bytes.fromhex(s['palette'][c[1]]))+(255,) if c else (0,0,0,0)
    assert im.getpixel((i*16+x,y))==expected,('Source pixel mismatch',i,x,y)
  assert m['frames'][i]['unityRect']==[i*16,0,16,24]
 for filename,scale in [('contact-native.png',1),('contact.png',4)]:
  contact=Image.open(OUT/filename).convert('RGBA');cm=m['contact'];assert contact.size==tuple(n*scale for n in cm['nativeSize'])
  bg=tuple(bytes.fromhex(cm['background']))+(255,)
  for placement in cm['instances']:
   for y,row in enumerate(s['frames'][placement['frame']]['cells']):
    for x,c in enumerate(row):
     expected=tuple(bytes.fromhex(s['palette'][c[1]]))+(255,) if c else bg
     for yy in range(scale):
      for xx in range(scale):assert contact.getpixel(((placement['topLeft'][0]+x)*scale+xx,(placement['topLeft'][1]+y)*scale+yy))==expected,('Contact mismatch',filename,placement,x,y)
 preserved=json.loads((OUT/'preserved-inputs.json').read_text())
 for p,sha in preserved.items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha,('Preserved input changed',p)
 counters=[]
 for name,mutate in [
  ('lost-frame',lambda t:t['frames'].pop()),
  ('duplicate-frame',lambda t:t['frames'][1].__setitem__('cells',copy.deepcopy(t['frames'][0]['cells']))),
  ('opaque-dune-hole',lambda t:t['frames'][8]['cells'][12].__setitem__(8,None)),
  ('dry-depleted-brine',lambda t:t['frames'][24].__setitem__('stillWet',False)),
  ('false-stock-state',lambda t:t['frames'][20].__setitem__('stockState','depleted-wet-film')),
  ('liquid-bright-carpet',lambda t:t['frames'][20]['cells'][10].__setitem__(slice(2,14),[['wet-level-glint','brine-reflection']]*12)),
  ('sand-granular-carpet',lambda t:[t['frames'][0]['cells'][y].__setitem__(slice(1,15),[['sand-ripple','sand-ripple']]*14) for y in range(4,12)]),
  ('opaque-prop-bottom',lambda t:t['frames'][18]['cells'][23].__setitem__(8,['rock-face','rock'])),
  ('wrong-owner',lambda t:t['frames'][28].__setitem__('blueprint','Rock')),
  ('joined-dune-band',lambda t:t['frames'][8]['cells'][12].__setitem__(slice(0,16),[['lee','dune-lee']]*16)),
  ('road-green-grass',lambda t:t['frames'][32]['cells'][12].__setitem__(8,['grass','briar'])),
  ('unknown-palette',lambda t:t['frames'][0]['cells'][12].__setitem__(8,['sand','missing'])),
 ]:
  changed=copy.deepcopy(s);mutate(changed)
  try:contract(changed)
  except AssertionError:counters.append(name)
  else:raise AssertionError('Counter accepted '+name)
 sha=hashlib.sha256((OUT/'beating-materials.png').read_bytes()).hexdigest();assert m['sha256']==sha
 return {'passed':True,'size':[544,24],'frames':34,'allFramesDistinct':True,'binaryAlpha':True,'opaquePixelsPerFrame':counts,'sourcePixelMismatches':0,'contactPixelMismatches':0,'preservedInputsUnchanged':len(preserved),'unchangedPriorFrames':list(PRESERVED_INDICES),'firstTrialFilesPreserved':len(history),'counterchecksRejected':counters,'sha256':sha,'decodedPixelSHA256':hashlib.sha256(im.tobytes()).hexdigest(),'limits':'Source geometry and decode-only pixels only; no Assets import, Unity tests, runtime ownership, whole-scene visual acceptance, gameplay or hardware performance proof.'}

if __name__=='__main__':
 try:r=verify()
 except (AssertionError,OSError,KeyError,ValueError) as e:r={'passed':False,'error':str(e)}
 (OUT/'verification.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r,indent=2));raise SystemExit(0 if r['passed'] else 1)
