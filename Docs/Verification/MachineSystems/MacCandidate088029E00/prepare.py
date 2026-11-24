from pathlib import Path
import hashlib,json,subprocess,tarfile,shutil,datetime,gzip
repo=Path('/Users/steven/caves-of-ooo');commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip();assert commit=='088029e00241a9b59e20d2cc0bcbf99dba69ae16'
tag=commit[:9]+'-20261124';bundle=Path('/private/tmp/coo-mac-'+tag+'-export');project=Path('/Users/steven/coo-verification/mac-'+tag);output=Path('/Users/steven/coo-verification/builds/mac-'+tag)
assert not bundle.exists() and not project.exists() and not output.exists();assert subprocess.run(['pgrep','-f','Unity.*-projectPath /Users/steven/coo-verification/clone3( |$)'],capture_output=True).returncode==1
assert shutil.disk_usage(repo).free>15_000_000_000
prior=repo/'Docs/Verification/MachineSystems/MacCandidate6325062A1';old='6325062a1-20261124';oldcommit='6325062a1cb3d9f2a1065fec5458f1d8b9b8959d'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
apps=json.loads(gzip.decompress((prior/'all-apps-post-boot.json.gz').read_bytes()))
assert len(apps)==20
for a,pins in apps.items():
 root=Path(a);current={str(p.relative_to(root)):sha(p) for p in sorted(root.rglob('*')) if p.is_file()};assert current==pins, 'Prior app changed: '+a
bundle.mkdir();project.mkdir();output.mkdir();(bundle/'prior-apps-before.json').write_text(json.dumps(apps,indent=2)+'\n')
roots=['Assets','Packages','ProjectSettings','ArtSource'];subprocess.run(['git','archive','--format=tar','-o',str(bundle/'input.tar'),commit,*roots],cwd=repo,check=True)
with tarfile.open(bundle/'input.tar') as t:t.extractall(project,filter='data')
pins={str(p.relative_to(project)):sha(p) for r in roots for p in sorted((project/r).rglob('*')) if p.is_file()};(bundle/'source-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
subprocess.run(['cp','-cR','/Users/steven/coo-verification/clone3/Library',str(project/'Library')],check=True)
helper=(prior/'CodexCandidateBuild.cs').read_text().replace(old,tag).replace(oldcommit,commit)
(project/'Assets/Editor/CodexCandidateBuild.cs').write_text(helper);(bundle/'CodexCandidateBuild.cs').write_text(helper)
external=Path('/Users/steven/unity-mcp/MCPForUnity');(bundle/'external-mcp-pins.json').write_text(json.dumps({str(p.relative_to(external)):sha(p) for p in sorted(external.rglob('*')) if p.is_file()},indent=2)+'\n')
for name in ['verify-inputs.py','boot-existing-save-readonly.py']:
 s=(prior/name).read_text().replace(old,tag)
 if name.startswith('boot-'):s=s.replace(tag+'-native-retry',tag+'-native')
 (bundle/name).write_text(s)
prep={'commit':commit,'sourcePins':len(pins),'copy':str(project),'output':str(output),'warmLibraryFrom':'idle clone3 after waterskin_menu_green_20261124_01:213total/212pass/1utilityskip/0CS; no tests repeated for packaging','bundle':str(bundle),'preparedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'freeDiskBytes':shutil.disk_usage(project).free,'priorApps':len(apps),'priorAppFiles':sum(len(v) for v in apps.values())};(bundle/'preparation.json').write_text(json.dumps(prep,indent=2)+'\n')
shutil.copy2(__file__,bundle/'prepare.py');print(json.dumps(prep),flush=True)
