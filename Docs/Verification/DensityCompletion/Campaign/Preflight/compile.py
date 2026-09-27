from pathlib import Path
import subprocess,json
root=Path('/tmp/coo-c12-campaign-native');base=Path('Library/Bee/artifacts/200b0aE.dag')
exe='/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting/NetCoreRuntime/dotnet'
csc='/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll'
for kind,src,out,adds in [
 ('runtime','CavesOfOoo','CavesOfOoo',['DensityCampaignNativeEvidence.cs','DensityCampaignNativePlayer.cs']),
 ('editor','Assembly-CSharp-Editor','Assembly-CSharp-Editor',['DensityCampaignNativeBatch.cs']),
 ('tests','EditModeTests','EditModeTests',['DensityCampaignNativeEvidenceTests.cs','NativeDensitySceneRestorationTests.cs'])]:
 lines=(base/(src+'.rsp')).read_text().splitlines();lines=[l for l in lines if not l.startswith(('-out:','-refout:'))]
 if kind=='tests':lines=[l for l in lines if not l.rstrip('"').endswith('.cs')]
 for i,l in enumerate(lines):
  if l.startswith('-r:') and l.rstrip('"').endswith(('/CavesOfOoo.dll','/CavesOfOoo.ref.dll')): lines[i]='-r:"'+str(root/'CavesOfOoo.dll')+'"'
  if kind=='tests' and l.startswith('-r:') and l.rstrip('"').endswith(('/Assembly-CSharp-Editor.dll','/Assembly-CSharp-Editor.ref.dll')): lines[i]='-r:"'+str(root/'Assembly-CSharp-Editor.dll')+'"'
 lines+=['-out:"'+str(root/(out+'.dll'))+'"','-refout:"'+str(root/(out+'.ref.dll'))+'"']+['"'+str(root/p)+'"' for p in adds]
 rsp=root/(kind+'.rsp');rsp.write_text('\n'.join(lines)+'\n')
 result=subprocess.run([exe,csc,'@'+str(rsp)],capture_output=True,text=True)
 (root/(kind+'-compile.log')).write_text(result.stdout+result.stderr)
 print(kind,result.returncode,(result.stdout+result.stderr).count('error CS'))
 if result.returncode: print(result.stdout+result.stderr);break
