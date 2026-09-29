from pathlib import Path
p=Path(__file__).parent;r=Path('/Users/steven/caves-of-ooo');rows=['<Project><ItemGroup>','<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/SpreadHaulingSourceTests.cs"/>','<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/DensityLootCensusTests.cs"/>','<Compile Include="'+str(p)+'/*Tests.cs"/>']
rows+=['<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/Spread*.cs" Exclude="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/SpreadHaulingSourceTests.cs;$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/SpreadComposition*.cs;$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/SpreadDiscoveryCensusTests.cs;$(RepoRoot)/Assets/Tests/EditMode/Gameplay/World/SpreadRareEncounterLifecycleTests.cs"/>','<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/Gameplay/AI/Spread*.cs"/>']
rows.append('<Compile Include="$(RepoRoot)/Assets/Tests/EditMode/TestSupport/PartRoundTripHelper.cs"/>')
for f in (p/'production/Assets').rglob('*.cs'):
 rel=f.relative_to(p/'production')
 if f.name=='OverworldZoneManager.cs':
  (p/'runner/Patched/OverworldZoneManager.cs').write_text(f.read_text().replace('zoneID.GetHashCode()','CooRun.StableHash.Of(zoneID)'));continue
 rows+=['<Compile Remove="$(RepoRoot)/'+str(rel)+'"/>','<Compile Include="'+str(f)+'"/>']
for name,rel in [('SpreadGrazerPart.cs','Assets/Scripts/Gameplay/AI/SpreadGrazerPart.cs'),('BoredGoal.cs','Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs'),('SpreadPredatorPart.cs',None),('CorpsePart.cs','Assets/Scripts/Gameplay/Entities/CorpsePart.cs')]:
 src=Path('/Users/steven/.codex/scratch/caves-of-ooo/f11/private/mechanics-stage2')/name
 if rel: rows.append('<Compile Remove="$(RepoRoot)/'+rel+'"/>')
 rows.append('<Compile Include="'+str(src)+'"/>')
rows+=['</ItemGroup></Project>'];(p/'runner/tests.props').write_text('\n'.join(rows))
