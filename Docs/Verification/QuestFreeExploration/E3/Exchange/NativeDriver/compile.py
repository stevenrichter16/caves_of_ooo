from pathlib import Path
import subprocess,sys
r=Path('/Users/steven/caves-of-ooo');p=Path('/tmp/coo-questfree-implementation/exchange/native');d=p/'production';b=r/'Library/Bee/artifacts/200b0aE.dag';u=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
production='--production' in sys.argv

def run(asm,replacements={},extra=[],refs={}):
 lines=[]
 for line in (b/(asm+'.rsp')).read_text().splitlines():
  if line.startswith(('-out:','-refout:','-analyzer:','-additionalfile:')):continue
  for a,v in refs.items():line=line.replace(a,v)
  if line.strip('"') in replacements:line='"'+str(replacements[line.strip('"')])+'"'
  lines.append(line)
 lines += ['"'+str(x)+'"' for x in extra]
 lines += ['-out:"'+str(p/(asm+'.dll'))+'"','-refout:"'+str(p/(asm+'.ref.dll'))+'"']
 rsp=p/(asm+'.rsp');rsp.write_text('\n'.join(lines)+'\n')
 v=subprocess.run([str(u/'NetCoreRuntime/dotnet'),str(u/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],cwd=r,capture_output=True,text=True)
 name=asm+('-candidate' if production else '-test-only')+'.log';(p/name).write_text(v.stdout+v.stderr);print(asm,v.returncode)
 if v.returncode:print('\n'.join(x for x in (v.stdout+v.stderr).splitlines() if 'error ' in x));raise SystemExit(v.returncode)
refs={}
if production:
 run('CavesOfOoo',{'Assets/Scripts/Scenarios/Custom/QuestFreeSpreadStateNativePlayer.cs':d/'QuestFreeSpreadStateNativePlayer.cs'},[d/'QuestFreeSpreadStateNativePlayer.Exchange.cs'])
 refs={'Library/Bee/artifacts/200b0aE.dag/CavesOfOoo.ref.dll':str(p/'CavesOfOoo.ref.dll')}
 run('Assembly-CSharp-Editor',{'Assets/Editor/Scenarios/QuestFreeSpreadStateNativeBatch.cs':d/'QuestFreeSpreadStateNativeBatch.cs'},[],refs)
run('EditModeTests',{},[p/'QuestFreeExchangeNativeModeTests.cs'],refs)
