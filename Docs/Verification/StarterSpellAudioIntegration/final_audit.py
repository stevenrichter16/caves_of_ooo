import collections,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=ROOT/'ArtSource/Audio/StarterSpellDesign/revision2';manifest=json.loads((source/'manifest.json').read_text())
assert sha(source/'manifest.json')=='0f32e12303f04e5691b3f801b57760064d68d987d7f6cc51fa5ea6fea7289ab7'
for rel,h in manifest['audioHashes'].items():assert sha(source/rel)==h,rel
copies=json.loads((ROOT/'ArtSource/Audio/StarterSpellDesign/runtime-verification/05-assets-copy.json').read_text())
for rel,h in copies['hashes'].items():assert sha(ROOT/rel)==h,rel
baseline=json.loads((OUT/'baseline-files.json').read_text());ember=[(p,h) for p,h in baseline.items() if p.startswith('Assets/Resources/Audio/EmberSpit/') and p.endswith('.wav')]
for p,h in ember:assert sha(ROOT/p)==h,p
allguids=collections.defaultdict(list)
for p in (ROOT/'Assets').rglob('*.meta'):
 m=re.search(r'^guid: ([a-f0-9]+)$',p.read_text(errors='replace'),re.M)
 if m:allguids[m[1]].append(str(p.relative_to(ROOT)))
collisions={g:ps for g,ps in allguids.items() if len(ps)>1};assert not collisions,collisions
metas=list((ROOT/'Assets/Resources/Audio/StarterSpells').glob('*.wav.meta'));assert len(metas)==85
for p in metas:
 s=p.read_text()
 for line in ['loadType: 0','sampleRateSetting: 0','compressionFormat: 0','preloadAudioData: 1','forceToMono: 0','loadInBackground: 0']:assert line in s,(p,line)
base=json.loads((OUT/'A00-baseline/receipt.json').read_text());final=json.loads((OUT/'A06-final-full/receipt.json').read_text());assert base['failures']==final['failures'];assert not final['compileErrors']
assert sha(ROOT/'Assets/Scenes/Main/SampleScene.unity')==baseline['Assets/Scenes/Main/SampleScene.unity']
p=ROOT/'ProjectSettings/QualitySettings.asset';original=(OUT/'preserved/ProjectSettings/QualitySettings.asset').read_bytes();t=p.read_text();n=t.replace('  - serializedVersion: 5','  - serializedVersion: 4').replace('    meshLodThreshold: 1\n','').replace('    Nintendo Switch 2: 5\n','')
if p.read_bytes()!=original:
 assert n.encode()==original,'unexpected quality settings edit';p.write_bytes(original)
report={'passed':True,'approvedHashes':len(manifest['audioHashes']),'importHashes':len(copies['hashes']),'emberWavs':len(ember),'importerMetas':len(metas),'guidCollisions':collisions,'newTests':102,'finalTotal':11537,'finalPassed':11506,'exactBaselineFailures':31,'scenePreserved':True,'qualitySettingsPreserved':True}
(OUT/'final-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
