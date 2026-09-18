#!/usr/bin/env python3
"""Read-only Ember revision source snapshot. Writes verification artifacts only."""
import collections,copy,datetime,hashlib,json,os,shutil,subprocess
from pathlib import Path
ROOT=Path('/Users/steven/caves-of-ooo')
OUT=ROOT/'Docs/Verification/StarterSpell3D/EmberRevision/E00-prechange'
assert not OUT.exists(),'Use a fresh immutable baseline directory'
OUT.mkdir(parents=True)

def sha_bytes(data):return hashlib.sha256(data).hexdigest()
def digest(path):return sha_bytes(path.read_bytes())
def stable_hash(path):
 a=path.stat();data=path.read_bytes();b=path.stat()
 return {'sha256':sha_bytes(data),'bytes':len(data)},(a.st_size,a.st_mtime_ns,b.st_size,b.st_mtime_ns)
def dump(path,data):path.write_text(json.dumps(data,indent=2,sort_keys=True,allow_nan=False)+'\n')
roots=['Assets','ProjectSettings','Packages','ArtSource/StarterSpell3D']
paths=set()
for directory in roots:
 for p in (ROOT/directory).rglob('*'):
  if p.is_file() and '__pycache__' not in p.parts:paths.add(p.relative_to(ROOT).as_posix())
patterns=['*.cs','*.py','*.shader','*.hlsl','*.compute','*.asmdef','*.asmref','*.sh','*.js','*.ts','*.csproj','*.sln']
cmd=['rg','--files']
for pattern in patterns:cmd+=['-g',pattern]
paths.update(subprocess.check_output(cmd,cwd=ROOT,text=True).splitlines())
references=['CLAUDE.md','Docs/PERF-FOUNDATION.md','Docs/STARTER-SPELL-3D-DESIGN.md','Docs/SPELL-3D-INTEGRATION.md','Docs/SPELL-3D-READABILITY.md',
 'Docs/Verification/StarterSpell3D/Readability/R25b-final-preservation/report.json',
 'Docs/Verification/StarterSpell3D/Readability/R25b-final-preservation/source-hashes-current.json',
 'Docs/Verification/StarterSpell3D/Readability/R24-final-full/receipt.json',
 'Docs/Verification/StarterSpell3D/Readability/R24-final-full/baseline-comparison.json',
 'Docs/Verification/StarterSpell3D/Readability/R24-final-full/guid-audit.json',
 'Docs/Verification/StarterSpell3D/Readability/R22b-hitch-boundary-targeted/receipt.json',
 'Docs/Verification/StarterSpell3D/Readability/R23-final-visual-acceptance.json',
 'Docs/Verification/StarterSpell3D/Readability/Art/59-continuity-import-freeze.json']
paths.update(references)
records={};stats={};unstable=[]
for name in sorted(paths):
 p=ROOT/name
 if p.is_symlink():
  data=os.readlink(p).encode();records[name]={'sha256':sha_bytes(data),'bytes':len(data),'symlink':True};continue
 records[name],stats[name]=stable_hash(p)
 if stats[name][:2]!=stats[name][2:]:unstable.append(name)
for name,state in stats.items():
 s=(ROOT/name).stat()
 if (s.st_size,s.st_mtime_ns)!=state[2:] and name not in unstable:unstable.append(name)
dump(OUT/'source-hashes-before.json',records)
code={p:v for p,v in records.items() if Path(p).suffix in ['.cs','.py','.shader','.hlsl','.compute','.asmdef','.asmref','.sh','.js','.ts','.csproj','.sln']}
dump(OUT/'code-camera-test-hashes.json',code)
accepted_path=ROOT/'Docs/Verification/StarterSpell3D/Readability/R25b-final-preservation/source-hashes-current.json'
accepted=json.loads(accepted_path.read_text());drift=[]
for name,want in accepted.items():
 current=records.get(name)
 if current is None or current['sha256']!=want['sha256']:drift.append({'path':name,'acceptedSha256':want['sha256'],'current':current})
dump(OUT/'accepted-state-comparison.json',{'acceptedManifestSha256':digest(accepted_path),'checked':len(accepted),'unchanged':len(accepted)-len(drift),'drift':drift,'sourceNotRestored':True})
# Copy exact authoring/runtime/current implementation inputs; preserve other six FBX bytes too.
critical=set(['ArtSource/StarterSpell3D/starter_spells.blend','ArtSource/StarterSpell3D/runtime/starter_spell_library.json','ArtSource/StarterSpell3D/manifest.json',
 'ArtSource/StarterSpell3D/build_studies.py','ArtSource/StarterSpell3D/export_runtime.py',
 'Assets/Resources/SpellFx3D/Library.asset',
 'Assets/Scripts/Presentation/Rendering/NativeSpellFxRenderer.cs','Assets/Scripts/Presentation/Rendering/NativeSpellFxLibrary.cs',
 'Assets/Scripts/Presentation/Rendering/WorldFxCoordinator.cs','Assets/Editor/Art/NativeSpellFxAssetBuilder.cs',
 'ProjectSettings/ProjectSettings.asset','ProjectSettings/QualitySettings.asset','Library/LastSceneManagerSetup.txt'])
critical.update(p for p in records if p.startswith('ArtSource/StarterSpell3D/exports/') and p.endswith('.fbx'))
critical.update(p for p in records if ('Camera' in p or 'Projection' in p or 'NativeZone3DRenderSurface' in p) and p.endswith('.cs'))
critical.update(references)
copied={}
for name in sorted(critical):
 src=ROOT/name;target=OUT/'preserved-inputs'/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,target)
 assert digest(src)==digest(target),name
 copied[name]={'sha256':digest(target),'bytes':target.stat().st_size}
dump(OUT/'copied-inputs.json',copied)
# Resolve material indexes into actual material content. Preserve piece order and all
# poses; omit only whole-library source hash and unrelated spells/global row order.
library=json.loads((ROOT/'ArtSource/StarterSpell3D/runtime/starter_spell_library.json').read_text());meshes={m['id']:m for m in library['meshes']}
normalized=OUT/'normalized-spell-exports';normalized.mkdir();spell_receipts={}
for study in library['studies']:
 payload={'normalizationVersion':1,'globals':{k:library[k] for k in ['schemaVersion','fps','frameCount','releaseFrame','coordinateConvention']},'study':copy.deepcopy(study),'meshes':[]}
 used={p['meshId'] for p in study['pieces']}
 for mid in sorted(used):
  mesh=copy.deepcopy(meshes[mid]);index=mesh.pop('materialIndex');mesh['resolvedMaterial']=copy.deepcopy(library['materials'][index]);payload['meshes'].append(mesh)
 canonical=json.dumps(payload,sort_keys=True,separators=(',',':'),allow_nan=False).encode();target=normalized/(study['id']+'.json');dump(target,payload)
 spell_receipts[study['id']]={'semanticSha256':sha_bytes(canonical),'fileSha256':digest(target),'meshCount':len(used),'pieceCount':len(study['pieces']),'path':target.relative_to(OUT).as_posix(),'preserveForRevision':study['id']!='ember_spit'}
dump(OUT/'normalized-spell-exports.json',{'normalization':'All global clock/coordinate fields, complete study including ordered pieces/111poses, referenced mesh geometry/normals/colors/triangles and resolved full material data. Excludes whole-library sourceBlendSha256 and unrelated global list ordering only. No floating precision reduction. Six non-Ember semantic hashes are preservation gates.','spells':spell_receipts})
processes=[]
for line in subprocess.check_output(['ps','-axo','pid=,comm='],text=True).splitlines():
 parts=line.strip().split(None,1)
 if len(parts)!=2:continue
 pid,comm=parts
 if any(x in comm.lower() for x in ['/unity.app/contents/macos/unity','/blender.app/contents/macos/blender','ffmpeg','ffprobe']) or Path(comm).name.lower()=='blender':processes.append({'pid':int(pid),'executable':comm})
status=subprocess.check_output(['git','status','--porcelain=v1'],cwd=ROOT,text=True);(OUT/'git-status-before.txt').write_text(status)
summary={'status':'PASS' if not unstable else 'UNSTABLE_SOURCE','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'branch':subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip(),'sourceRoots':roots,'extraCodeGlobs':patterns,'fileCount':len(records),'bytes':sum(v['bytes'] for v in records.values()),'sourceManifestSha256':digest(OUT/'source-hashes-before.json'),'codeCameraTestFiles':len(code),'copiedInputs':len(copied),'unstableReads':unstable,'acceptedStateDrift':drift,'processesObserved':processes,'gitStatusLines':len(status.splitlines()),'baselineTests':{'total':11338,'passed':11307,'knownFailures':31,'focused':449,'freshRunPerformed':False},'mutations':'Verification artifact creation only. No Unity/Blender/ffmpeg launch, no source/asset/settings/save modification, no Editor quit or Play change.','honesty':'Existing ordinary Unity Editor remains open and was separately inspected read-only. No active verification/render process is claimed absent merely because a source hash is stable. Prior R24/R22b results are retained baseline evidence, not rerun E00 tests.'}
dump(OUT/'snapshot.json',summary)
print(json.dumps({k:summary[k] for k in ['status','fileCount','bytes','codeCameraTestFiles','copiedInputs','unstableReads','acceptedStateDrift','processesObserved']},indent=2))
