from pathlib import Path
import sys
p=Path(__file__).parent;r=Path('/Users/steven/caves-of-ooo');d=p/'production';rows=['<Project><ItemGroup>','<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/DensityLootCensusTests.cs"/>','<Compile Include="'+str(p)+'/*Tests.cs"/>']
if '--production' in sys.argv:
 for f in (d/'Assets').rglob('*.cs'):
  rel=f.relative_to(d)
  if rel.as_posix().endswith('/OverworldZoneManager.cs'):
   target=p/'runner/Patched/OverworldZoneManager.cs';target.write_text(f.read_text().replace('zoneID.GetHashCode()','CooRun.StableHash.Of(zoneID)'));continue
  rows+=['<Compile Remove="$(RepoRoot)/'+str(rel)+'"/>','<Compile Include="'+str(f)+'"/>']
if '--neighbors' in sys.argv:
 for name in (p/'neighbors.txt').read_text().splitlines():
  path='Assets/Tests/EditMode/Gameplay/World/'+name+'.cs'
  if not (d/path).exists():rows.append('<Compile Include="$(RepoRoot)/'+path+'"/>')
rows+=['</ItemGroup></Project>'];(p/'runner/tests.props').write_text('\n'.join(rows))
