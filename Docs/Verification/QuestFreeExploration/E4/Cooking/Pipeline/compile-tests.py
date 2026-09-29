from pathlib import Path
import subprocess
r=Path('/Users/steven/caves-of-ooo');p=Path('/tmp/coo-questfree-implementation/cooking-pipeline');b=r/'Library/Bee/artifacts/200b0aE.dag';u=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
lines=[s for s in (b/'EditModeTests.rsp').read_text().splitlines() if not s.startswith(('-out:','-refout:','-analyzer:','-additionalfile:')) and 'SpreadCookingPipelineTests.cs' not in s]
lines+=['"'+str(p/'SpreadCookingPipelineTests.cs')+'"','-out:"'+str(p/'EditModeTests.dll')+'"','-refout:"'+str(p/'EditModeTests.ref.dll')+'"']
(p/'EditModeTests.rsp').write_text('\n'.join(lines)+'\n')
v=subprocess.run([str(u/'NetCoreRuntime/dotnet'),str(u/'DotNetSdkRoslyn/csc.dll'),'@'+str(p/'EditModeTests.rsp')],cwd=r,capture_output=True,text=True)
(p/'native-reference-compile.log').write_text(v.stdout+v.stderr);print(v.returncode);print('\n'.join(s for s in (v.stdout+v.stderr).splitlines() if 'error ' in s));raise SystemExit(v.returncode)
