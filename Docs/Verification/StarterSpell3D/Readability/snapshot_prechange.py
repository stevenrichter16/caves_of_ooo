import datetime,hashlib,json,shutil,subprocess
from pathlib import Path
root=Path('/Users/steven/caves-of-ooo');out=root/'Docs/Verification/StarterSpell3D/Readability';out.mkdir(exist_ok=True)
pre=out/'R00-prechange';pre.mkdir()
roots=['Assets','ProjectSettings','Packages','ArtSource/StarterSpell3D']
files=[]
for name in roots:
 for p in sorted((root/name).rglob('*')):
  if p.is_file() and '__pycache__' not in p.parts:files.append(p)
for name in ['CLAUDE.md','Docs/SPELL-3D-INTEGRATION.md','Docs/STARTER-SPELL-3D-DESIGN.md','Docs/PERF-FOUNDATION.md','Tools/Village3D/run_common.py','Docs/Verification/StarterSpell3D/Integration/run_editmode.py','Docs/Verification/StarterSpell3D/Integration/run_native.py']:
 p=root/name
 if p.is_file():files.append(p)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
manifest={};unstable=[]
for p in files:
 before=p.stat();h=sha(p);after=p.stat();name=str(p.relative_to(root))
 manifest[name]={'sha256':h,'bytes':after.st_size}
 if before.st_size!=after.st_size or before.st_mtime_ns!=after.st_mtime_ns:unstable.append(name)
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip();branch=subprocess.check_output(['git','branch','--show-current'],cwd=root,text=True).strip()
(pre/'source-hashes-before.json').write_text(json.dumps(manifest,indent=2)+'\n')
(pre/'git-status-before.txt').write_text(subprocess.check_output(['git','status','--short'],cwd=root,text=True))
backup=pre/'preserved-inputs';backup.mkdir();copied=[]
# Preserve exact bytes for canonical inputs likely to be edited and Unity's automatic
# settings rewrite. Existing environment/town/ring art is tracked in the full hash set.
for name in manifest:
 if name.startswith(('ProjectSettings/','Packages/','Assets/Resources/SpellFx3D/','Assets/Art3D/SpellFx3D/','Assets/Scripts/Presentation/Rendering/NativeSpell','Assets/Editor/Art/NativeSpell','Assets/Editor/Scenarios/StarterSpell3D','Assets/Scripts/Scenarios/Custom/StarterSpell3D')) or name in ['ArtSource/StarterSpell3D/starter_spells.blend','ArtSource/StarterSpell3D/build_studies.py','ArtSource/StarterSpell3D/export_runtime.py','ArtSource/StarterSpell3D/runtime/starter_spell_library.json','ArtSource/StarterSpell3D/manifest.json']:
  dest=backup/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/name,dest)
  if sha(dest)!=manifest[name]['sha256']:raise RuntimeError('Copy mismatch '+name)
  copied.append(name)
receipt={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'head':head,'branch':branch,'roots':roots,'files':len(manifest),'totalBytes':sum(x['bytes'] for x in manifest.values()),'unstableDuringHash':unstable,'manifestSha256':sha(pre/'source-hashes-before.json'),'preservedCopies':copied,'scope':'All source/assets/settings/package inputs hashed before readability edits. Canonical spell authoring/runtime input and settings bytes preserved selectively. No user save files, camera values or working source were altered.'}
(pre/'snapshot.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:v for k,v in receipt.items() if k!='preservedCopies'},indent=2));print('Copied',len(copied),'inputs')
if unstable:raise RuntimeError('Concurrent source mutation during hash')
