from pathlib import Path
import subprocess,json
r=Path('/Users/steven/caves-of-ooo');p=Path('/tmp/coo-questfree-implementation/e4/cooking-art/production');b=r/'Library/Bee/artifacts/200b0aE.dag';u=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
def run(assembly,replacements,refs={}):
 lines=[]; replacements=dict(replacements)
 for line in (b/(assembly+'.rsp')).read_text().splitlines():
  if line.startswith(('-out:','-refout:','-analyzer:','-additionalfile:')):continue
  for before,after in refs.items():line=line.replace(before,after)
  q=line.strip('"')
  if q in replacements:line='"'+str(replacements.pop(q))+'"'
  lines.append(line)
 for f in replacements.values():lines.append('"'+str(f)+'"')
 lines+=['-out:"'+str(p/(assembly+'.dll'))+'"','-refout:"'+str(p/(assembly+'.ref.dll'))+'"']
 out=p/(assembly+'.rsp');out.write_text('\n'.join(lines)+'\n')
 c=subprocess.run([str(u/'NetCoreRuntime/dotnet'),str(u/'DotNetSdkRoslyn/csc.dll'),'@'+str(out)],cwd=r,capture_output=True,text=True);(p/(assembly+'.log')).write_text(c.stdout+c.stderr);print(assembly,c.returncode,flush=True)
 if c.returncode:print('\n'.join(l for l in (c.stdout+c.stderr).splitlines()if 'error CS'in l));raise SystemExit(c.returncode)
runtime={f'Assets/Scripts/Presentation/Rendering/{f.name}':f for f in p.glob('*.cs')if f.name!='SpreadCookingBuilder.cs'}
run('CavesOfOoo',runtime)
refs={'Library/Bee/artifacts/200b0aE.dag/CavesOfOoo.ref.dll':str(p/'CavesOfOoo.ref.dll')}
run('Assembly-CSharp-Editor',{'Assets/Editor/Art/SpreadCookingBuilder.cs':p/'SpreadCookingBuilder.cs'},refs)
run('EditModeTests',{},refs)
