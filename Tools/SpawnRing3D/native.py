import argparse,subprocess,time,sys,uuid,json,hashlib,gzip
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--profile',choices=['full','low']);p.add_argument('--actions',action='store_true');a=p.parse_args()
root=Path('/Users/steven/caves-of-ooo');stamp=uuid.uuid4().hex;out=root/'Docs/Verification/SpawnRing3D'/('native-launch-'+stamp);out.mkdir()
if subprocess.run(['pgrep','-x','Unity'],stdout=subprocess.DEVNULL).returncode==0:raise SystemExit('Finish the current Unity run first.')
files={}
for folder in ['Assets','ProjectSettings','Packages','ArtSource/SpawnRing3D']:
 for path in (root/folder).rglob('*'):
  if path.is_file() and str(path.relative_to(root)) not in ['Assets/UnityMCP/Log/mcp.log','Assets/UnityMCP/Log/mcpError.log']:
   files[str(path.relative_to(root))]=hashlib.sha256(path.read_bytes()).hexdigest()
(out/'source-before.json').write_text(json.dumps(files,indent=2)+'\n')
(out/'git-status.txt').write_bytes(subprocess.check_output(['git','status','--short'],cwd=root))
with gzip.open(out/'tracked-diff.patch.gz','wb') as f:f.write(subprocess.check_output(['git','diff','--binary'],cwd=root))
subprocess.run(['pkill','-f','[m]cp-for-unity'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);time.sleep(2)
with open(out/'mcp.log','w') as f:subprocess.Popen(['uv','run','mcp-for-unity','--transport','http'],cwd='/Users/steven/unity-mcp/Server',stdout=f,stderr=subprocess.STDOUT,start_new_session=True)
time.sleep(5);(root/'Temp/UnityLockfile').unlink(missing_ok=True)
log=out/'unity.log';cmd=['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-projectPath',str(root),'-logFile',str(log),'-executeMethod','CavesOfOoo.Editor.SpawnRing3DNativeAuditBatch.RunFromCommandLine']
if a.profile:cmd+=['-spawnRing3dProfile',a.profile]
if a.actions:cmd+=['-spawnRing3dActions']
(out/'launch.json').write_text(json.dumps({'id':stamp,'command':cmd,'profile':a.profile,'actions':a.actions},indent=2)+'\n');print(out,flush=True)
started=time.time()
r=subprocess.run(cmd,cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
lines=log.read_text(errors='replace').splitlines();cs=[l for l in lines if 'error CS' in l];print('C# error lines:',len(cs),flush=True)
if cs:print('\n'.join(dict.fromkeys(cs)));raise SystemExit(2)
changes=[]
for rel,sha in files.items():
 path=root/rel
 if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest()!=sha:changes.append(rel)
action_receipts=[]
if a.actions:
 for path in (root/'Docs/Verification/SpawnRing3D').glob('R3D-*-actions.json'):
  if path.stat().st_mtime>=started:
   d=json.loads(path.read_text());native=path.with_name('R3D-'+d['runId']+'-native.json')
   n=json.loads(native.read_text()) if native.exists() else {}
   labels={'harvest-before','harvest-menu','harvest-after','combat-before','combat-action-frame','combat-after'}
   expected={'R3D-'+d['runId']+'-actions-'+label+'.png' for label in labels}
   screenshots=[Path(p) for p in d.get('screenshots',[])]
   valid=d.get('complete') and d.get('cleanup') and d.get('failures')==0 and d.get('runId')==n.get('runId') and d.get('saveRoot')==n.get('saveRoot') and d.get('worldSeed')==729490642 and len(screenshots)==6 and {p.name for p in screenshots}==expected and all(p.exists() and p.stat().st_size>1000 for p in screenshots)
   action_receipts.append({'path':str(path),'valid':bool(valid),'checks':len(d.get('checks',[])),'failures':[c for c in d.get('checks',[]) if not c.get('pass')]})
actions_valid=not a.actions or (len(action_receipts)==1 and action_receipts[0]['valid'])
receipt={'exit':r.returncode,'compileErrors':len(cs),'changedProtectedFiles':changes,'actionsValid':actions_valid,'actionReceipts':action_receipts}
(out/'launch-result.json').write_text(json.dumps(receipt,indent=2)+'\n');print(receipt,flush=True)
for line in lines:
 if '[SpawnRing3D' in line or '[NativeSaveIsolation]' in line:print(line)
sys.exit(r.returncode or (1 if changes or not actions_valid else 0))
