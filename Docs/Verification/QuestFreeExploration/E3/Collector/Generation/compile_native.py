from pathlib import Path
import subprocess
root=Path('/Users/steven/caves-of-ooo');p=Path('/tmp/coo-questfree-implementation/actors/collector-placement');base=root/'Library/Bee/artifacts/200b0aE.dag';u=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
owned=['SpreadCollectorPart.cs','SpreadExplorationActorPlacement.cs','PopulationBuilder.cs','SpreadExplorationPopulationPolicy.cs','SpreadExplorationBuilder.cs']
overrides={f.name:p/f.name for f in (root/'Assets/Scripts').rglob('*.cs') if f.name in owned};overrides['SpreadExplorationPlan.cs']=Path('/tmp/coo-questfree-implementation/entry-expansion/SpreadExplorationPlan.cs')
overrides['WorldTravellers.cs']=Path('/tmp/coo-questfree-implementation/exchange/WorldTravellers.sample.cs')
lines=[]
for line in (base/'CavesOfOoo.rsp').read_text().splitlines():
 if line.startswith(('-out:','-refout:','-analyzer:','-additionalfile:')) or (line.strip('"').startswith('Assets/Scripts/') and line.strip('"').endswith('.cs')):continue
 lines.append(line)
lines+=['"'+str(overrides.get(f.name,f))+'"' for f in sorted((root/'Assets/Scripts').rglob('*.cs'))]
lines+=['"'+str(p/'SpreadExplorationActorPlacement.Collector.cs')+'"','-out:"'+str(p/'CavesOfOoo.dll')+'"','-refout:"'+str(p/'CavesOfOoo.ref.dll')+'"']
(p/'runtime.rsp').write_text('\n'.join(lines)+'\n')
for kind in ['runtime','tests']:
 if kind=='tests':
  lines=[]
  for line in (base/'EditModeTests.rsp').read_text().splitlines():
   if line.startswith(('-out:','-refout:','-analyzer:','-additionalfile:')) or line.strip('"').endswith('.cs'):continue
   if line.startswith('-r:') and ('CavesOfOoo.dll' in line or 'CavesOfOoo.ref.dll' in line):line='-r:"'+str(p/'CavesOfOoo.ref.dll')+'"'
   lines.append(line)
  lines+=['"'+str(f)+'"' for f in sorted((root/'Assets/Tests/EditMode').rglob('*.cs'))]
  lines+=['"'+str(f)+'"' for f in sorted(p.glob('*Tests.cs'))]
  lines+=['-out:"'+str(p/'EditModeTests.dll')+'"','-refout:"'+str(p/'EditModeTests.ref.dll')+'"'];(p/'tests.rsp').write_text('\n'.join(lines)+'\n')
 r=subprocess.run([str(u/'NetCoreRuntime/dotnet'),str(u/'DotNetSdkRoslyn/csc.dll'),'@'+str(p/(kind+'.rsp'))],cwd=root,capture_output=True,text=True)
 (p/(kind+'-compile.log')).write_text(r.stdout+r.stderr);print(kind,r.returncode)
 if r.returncode:print(r.stdout+r.stderr);raise SystemExit(r.returncode)
