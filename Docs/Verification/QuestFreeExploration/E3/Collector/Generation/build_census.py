from pathlib import Path
import shutil,re
base=Path('/tmp/coo-questfree-implementation/actors/collector-placement')
source=base/'baseline-v4-runner'
for version in (3,4):
 dst=base/f'census-v{version}-runner'
 shutil.copytree(source,dst,ignore=shutil.ignore_patterns('bin','obj','Patched','*.xml','*.log'),dirs_exist_ok=True)
 cs=(dst/'EditModeRunner.csproj').read_text()
 cs=re.sub(r'<ItemGroup><Compile Remove="\$\(RepoRoot\)/Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs".*?</ItemGroup>','',cs,flags=re.S)
 if version==4:
  cs=cs.replace('<Import Project="patched.props"', '<ItemGroup><Compile Remove="$(RepoRoot)/Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs"/><Compile Include="/tmp/coo-questfree-implementation/entry-expansion/SpreadExplorationPlan.cs"/><Compile Remove="$(RepoRoot)/Assets/Scripts/Gameplay/World/WorldTravellers.cs"/><Compile Include="/tmp/coo-questfree-implementation/exchange/WorldTravellers.sample.cs"/></ItemGroup><Import Project="patched.props"')
 (dst/'EditModeRunner.csproj').write_text(cs)
 (dst/'tests.props').write_text('<Project><ItemGroup><Compile Include="/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Gameplay/World/DensityLootCensusTests.cs"/><Compile Include="'+str(base/'OccupiedBankAvailabilityCensus.cs')+'"/></ItemGroup></Project>')
