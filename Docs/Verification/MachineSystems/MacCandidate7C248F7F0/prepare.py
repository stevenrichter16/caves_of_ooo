from pathlib import Path
import hashlib,json,subprocess,tarfile,shutil,datetime
repo=Path('/Users/steven/caves-of-ooo');commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip();assert commit=='7c248f7f06d437d91fb3abbb2f7f2678da5c652a'
tag=commit[:9]+'-20261123';bundle=Path('/private/tmp/coo-mac-'+tag+'-export');project=Path('/Users/steven/coo-verification/mac-'+tag);output=Path('/Users/steven/coo-verification/builds/mac-'+tag)
assert not bundle.exists() and not project.exists() and not output.exists();assert subprocess.run(['pgrep','-f','Unity.*-projectPath /Users/steven/coo-verification/clone3 '],capture_output=True).returncode==1
bundle.mkdir();project.mkdir();output.mkdir();roots=['Assets','Packages','ProjectSettings','ArtSource'];subprocess.run(['git','archive','--format=tar','-o',str(bundle/'input.tar'),commit,*roots],cwd=repo,check=True)
with tarfile.open(bundle/'input.tar') as t:t.extractall(project,filter='data')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();pins={str(p.relative_to(project)):sha(p) for r in roots for p in sorted((project/r).rglob('*')) if p.is_file()};(bundle/'source-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
subprocess.run(['cp','-cR','/Users/steven/coo-verification/clone3/Library',str(project/'Library')],check=True)
prior=repo/'Docs/Verification/MachineSystems/MacCandidate73FB59DA4';helper=(prior/'CodexCandidateBuild.cs').read_text().replace('73fb59da4-20261123',tag).replace('73fb59da42649243bf68d7b4beee4cb0b6558b9a',commit)
(project/'Assets/Editor/CodexCandidateBuild.cs').write_text(helper);(bundle/'CodexCandidateBuild.cs').write_text(helper)
external=Path('/Users/steven/unity-mcp/MCPForUnity');(bundle/'external-mcp-pins.json').write_text(json.dumps({str(p.relative_to(external)):sha(p) for p in sorted(external.rglob('*')) if p.is_file()},indent=2)+'\n')
prep={'commit':commit,'sourcePins':len(pins),'copy':str(project),'output':str(output),'warmLibraryFrom':'idle clone3 after Cinderhold smith native200 total/199pass/0fail/1Explicit utilityskip; build must import exact committed source','bundle':str(bundle),'preparedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'freeDiskBytes':shutil.disk_usage(project).free};(bundle/'preparation.json').write_text(json.dumps(prep,indent=2)+'\n')
for name in ['verify-inputs.py','boot-check.py']:
 s=(prior/('boot-check-retry.py' if name=='boot-check.py' else name)).read_text().replace('73fb59da4-20261123',tag);(bundle/name).write_text(s)
shutil.copy2(__file__,bundle/'prepare.py');print(json.dumps(prep),flush=True)
