from pathlib import Path
import hashlib,json,subprocess,tarfile,shutil,datetime,gzip,sys,re
repo=Path('/Users/steven/caves-of-ooo');commit=sys.argv[1];assert re.fullmatch('[0-9a-f]{40}',commit)
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip()==commit
tag=commit[:9]+'-20261124';bundle=Path('/private/tmp/coo-mac-'+tag+'-export');project=Path('/Users/steven/coo-verification/mac-'+tag);output=Path('/Users/steven/coo-verification/builds/mac-'+tag)
assert not bundle.exists() and not project.exists() and not output.exists()
assert subprocess.run(['pgrep','-f','Unity.*-projectPath /Users/steven/coo-verification/clone3( |$)'],capture_output=True).returncode==1
assert shutil.disk_usage(repo).free>15_000_000_000
prior=repo/'Docs/Verification/MachineSystems/MacCandidate9A0D55F93';old='9a0d55f93-20261124';oldcommit='9a0d55f9303b76c033af98ee28a0d985663c3624'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
apps=json.loads(gzip.decompress((prior/'all-apps-post-boot.json.gz').read_bytes()));assert len(apps)==25
for a,pins in apps.items():
 root=Path(a);assert {str(p.relative_to(root)):sha(p) for p in sorted(root.rglob('*')) if p.is_file()}==pins,a
batch=repo/'Docs/Verification/MachineSystems/FieldWorkTools20261124'
pins=json.loads((batch/'source-pins.json').read_text());assert all(sha(repo/p)==v and sha(Path('/Users/steven/coo-verification/clone3')/p)==v for p,v in pins.items())
bundle.mkdir();project.mkdir();output.mkdir();(bundle/'prior-apps-before.json').write_text(json.dumps(apps,indent=2)+'\n')
roots=['Assets','Packages','ProjectSettings','ArtSource'];subprocess.run(['git','archive','--format=tar','-o',str(bundle/'input.tar'),commit,*roots],cwd=repo,check=True)
with tarfile.open(bundle/'input.tar') as t:t.extractall(project,filter='data')
pins={str(p.relative_to(project)):sha(p) for r in roots for p in sorted((project/r).rglob('*')) if p.is_file()};(bundle/'source-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
subprocess.run(['cp','-cR','/Users/steven/coo-verification/clone3/Library',str(project/'Library')],check=True)
helper=(repo/'Docs/Verification/MachineSystems/MacCandidate088029E00/CodexCandidateBuild.cs').read_text().replace('088029e00-20261124',tag).replace('088029e00241a9b59e20d2cc0bcbf99dba69ae16',commit)
(project/'Assets/Editor/CodexCandidateBuild.cs').write_text(helper);(bundle/'CodexCandidateBuild.cs').write_text(helper)
external=Path('/Users/steven/unity-mcp/MCPForUnity');(bundle/'external-mcp-pins.json').write_text(json.dumps({str(p.relative_to(external)):sha(p) for p in sorted(external.rglob('*')) if p.is_file()},indent=2)+'\n')
for name in ['verify-inputs.py','boot-existing-save-readonly.py','verify-committed-source.py','window-preflight.swift']:
 (bundle/name).write_text((prior/name).read_text().replace(old,tag))
prep={'commit':commit,'sourcePins':len(pins),'copy':str(project),'output':str(output),'warmLibraryFrom':'idle clone3;15 exact field-tool runtime/test/meta pins; final native verification, no tests repeated for packaging','bundle':str(bundle),'preparedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'freeDiskBytes':shutil.disk_usage(project).free,'priorApps':len(apps),'priorAppFiles':sum(map(len,apps.values()))};(bundle/'preparation.json').write_text(json.dumps(prep,indent=2)+'\n')
shutil.copy2(__file__,bundle/'prepare.py');print(json.dumps(prep),flush=True)

