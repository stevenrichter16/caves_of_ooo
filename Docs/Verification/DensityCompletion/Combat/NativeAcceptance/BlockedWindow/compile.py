from pathlib import Path
import subprocess
p=Path('/tmp/coo-c2-blocked-window'); base=Path('Library/Bee/artifacts/200b0aE.dag'); exe='/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting/NetCoreRuntime/dotnet';csc='/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll'
for kind,asm,replacements in [('runtime','CavesOfOoo',['DensityCombatNativeEvidence.cs','DensityCombatNativePlayer.cs']),('tests','EditModeTests',['DensityCombatNativeEvidenceTests.cs'])]:
 lines=[]
 for line in (base/(asm+'.rsp')).read_text().splitlines():
  if line.startswith(('-out:','-refout:')):continue
  if line.rstrip('"').endswith('.cs'):
   if kind=='tests' or any(line.rstrip('"').endswith('/'+f) for f in replacements):continue
  if kind=='tests' and line.startswith('-r:') and line.rstrip('"').endswith(('/CavesOfOoo.dll','/CavesOfOoo.ref.dll')):line='-r:"'+str(p/'CavesOfOoo.dll')+'"'
  lines.append(line)
 
 if kind=='runtime':
  steam='Assets/Scripts/Gameplay/Effects/SteamContact.cs'
  if Path(steam).exists() and not any(steam in x for x in lines):lines.append(chr(34)+steam+chr(34))
 lines+=['-out:"'+str(p/(asm+'.dll'))+'"','-refout:"'+str(p/(asm+'.ref.dll'))+'"']+['"'+str(p/f)+'"' for f in replacements]
 rsp=p/(kind+'.rsp');rsp.write_text('\n'.join(lines)+'\n');r=subprocess.run([exe,csc,'@'+str(rsp)],capture_output=True,text=True);(p/(kind+'-compile.log')).write_text(r.stdout+r.stderr);print(kind,r.returncode);print(r.stdout+r.stderr if r.returncode else '')
 if r.returncode:break
