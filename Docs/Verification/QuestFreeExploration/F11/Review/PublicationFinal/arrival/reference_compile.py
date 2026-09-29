from pathlib import Path
import subprocess,sys
root=Path(__file__).parent;repo=Path('/Users/steven/caves-of-ooo');bee=repo/'Library/Bee/artifacts/200b0aE.dag';unity=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting');csc=[str(unity/'NetCoreRuntime/dotnet'),str(unity/'DotNetSdkRoslyn/csc.dll')];completed={}
overrides={'Assets/Scripts/Gameplay/World/Generation/SpreadExplorationHunt.cs':root/'SpreadExplorationHunt.cs','Assets/Tests/EditMode/Gameplay/World/SpreadHuntArrivalTests.cs':root/'SpreadHuntArrivalTests.cs'}
for assembly in ['CavesOfOoo','EditModeTests']:
 rows=[];replaced=set()
 for row in (bee/(assembly+'.rsp')).read_text().splitlines():
  if row.startswith('-out:'):row='-out:"'+str(root/(assembly+'.dll'))+'"'
  elif row.startswith('-refout:'):continue
  elif row.strip('"')in overrides:
   k=row.strip('"');row='"'+str(overrides[k])+'"';replaced.add(k)
  elif row.startswith('-r:'):
   n=Path(row[3:].strip('"')).name.replace('.ref.dll','.dll')
   if n in completed:row='-r:"'+str(completed[n])+'"'
  rows.append(row)
 for key,p in overrides.items():
  if key not in replaced and ((assembly=='EditModeTests'and key.startswith('Assets/Tests/'))or(assembly=='CavesOfOoo'and key.startswith('Assets/Scripts/'))):rows.append('"'+str(p)+'"')
 rsp=root/(assembly+'-reference.rsp');rsp.write_text('\n'.join(rows)+'\n');run=subprocess.run(csc+['@'+str(rsp)],cwd=repo,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);(root/(assembly+'-reference-compile.log')).write_text(run.stdout);print(assembly,'exit',run.returncode)
 if run.returncode:print('\n'.join(x for x in run.stdout.splitlines()if 'error 'in x));sys.exit(run.returncode)
 completed[assembly+'.dll']=root/(assembly+'.dll')
