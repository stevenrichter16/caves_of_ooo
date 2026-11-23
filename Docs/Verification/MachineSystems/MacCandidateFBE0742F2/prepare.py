from pathlib import Path
import hashlib,json,subprocess,tarfile,shutil,datetime
repo=Path('/Users/steven/caves-of-ooo');commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip();assert commit=='fbe0742f235ba5c92237cee710571ff0ee1d1a7b'
tag=commit[:9]+'-20261122';bundle=Path('/private/tmp/coo-mac-'+tag+'-export');project=Path('/Users/steven/coo-verification/mac-'+tag);output=Path('/Users/steven/coo-verification/builds/mac-'+tag)
assert not bundle.exists() and not project.exists() and not output.exists();assert subprocess.run(['pgrep','-f','Unity.*-projectPath /Users/steven/coo-verification/clone3 '],capture_output=True).returncode==1
bundle.mkdir();project.mkdir();output.mkdir();roots=['Assets','Packages','ProjectSettings','ArtSource'];subprocess.run(['git','archive','--format=tar','-o',str(bundle/'input.tar'),commit,*roots],cwd=repo,check=True)
with tarfile.open(bundle/'input.tar') as t:t.extractall(project,filter='data')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();pins={str(p.relative_to(project)):sha(p) for r in roots for p in sorted((project/r).rglob('*')) if p.is_file()};(bundle/'source-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
subprocess.run(['cp','-cR','/Users/steven/coo-verification/clone3/Library',str(project/'Library')],check=True)
prior=repo/'Docs/Verification/MachineSystems/MacCandidateA9DE3D3D9';helper=(prior/'CodexCandidateBuild.cs').read_text().replace('mac-a9de3d3d9-20261122','mac-'+tag).replace('coo-mac-a9de3d3d9-20261122-export','coo-mac-'+tag+'-export').replace('a9de3d3d9de9c21a917748c0d74c387b17ab7b3d',commit)
(project/'Assets/Editor/CodexCandidateBuild.cs').write_text(helper);(bundle/'CodexCandidateBuild.cs').write_text(helper)
external=Path('/Users/steven/unity-mcp/MCPForUnity');(bundle/'external-mcp-pins.json').write_text(json.dumps({str(p.relative_to(external)):sha(p) for p in sorted(external.rglob('*')) if p.is_file()},indent=2)+'\n')
prep={'commit':commit,'sourcePins':len(pins),'copy':str(project),'output':str(output),'warmLibraryFrom':'idle clone3, final471caseGREEN','bundle':str(bundle),'preparedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'freeDiskBytes':shutil.disk_usage(project).free};(bundle/'preparation.json').write_text(json.dumps(prep,indent=2)+'\n')
for name in ['verify-inputs.py','boot-check.py']:
 s=(prior/name).read_text().replace('mac-a9de3d3d9-20261122','mac-'+tag).replace('coo-mac-a9de3d3d9-20261122','coo-mac-'+tag);(bundle/name).write_text(s)
print(json.dumps(prep),flush=True)
