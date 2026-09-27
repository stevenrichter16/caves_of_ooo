from pathlib import Path
import subprocess
root=Path('/Users/steven/caves-of-ooo');work=Path('/tmp/coo-first-hour-e2');bee=root/'Library/Bee/artifacts/200b0aE.dag'
runtime=[str(p.relative_to(work/'candidate')) for p in (work/'candidate/Assets/Scripts').rglob('*.cs') if p.name in ['AIAmbushPart.cs','DormantGoal.cs']]
for asm in ['CavesOfOoo','EditModeTests']:
 lines=[]
 for s in (bee/(asm+'.rsp')).read_text().splitlines():
  if s.startswith('-out:'):s='-out:"'+str(work/(asm+'.dll'))+'"'
  elif s.startswith('-refout:'):s='-refout:"'+str(work/(asm+'.ref.dll'))+'"'
  elif s.startswith('-r:') and 'CavesOfOoo.ref.dll' in s:s='-r:"'+str(work/'CavesOfOoo.ref.dll')+'"'
  elif s.startswith('-analyzer:') or s.startswith('/analyzer:'):continue
  if s.strip('"') in runtime:s='"'+str(work/'candidate'/s.strip('"'))+'"'
  lines.append(s)
 if asm=='EditModeTests':
  for name in ['FirstHourAmbushSaveTests','FirstHourAmbushSaveAdversarialTests','FirstHourAmbushLegacyBytes']:
   lines.append('"'+str(work/'candidate/Assets/Tests/EditMode/Gameplay/AI'/(name+'.cs'))+'"')
 rsp=work/(asm+'.rsp');rsp.write_text('\n'.join(lines)+'\n')
 p=subprocess.run(['dotnet','/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll','@'+str(rsp)],cwd=root,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
 (work/(asm+'-native-compile.log')).write_text(p.stdout)
 print(asm,p.returncode,'\n'.join(s for s in p.stdout.splitlines() if 'error ' in s)[:6000],flush=True)
 if p.returncode:break
