from pathlib import Path
import subprocess, sys
REPO=Path('/Users/steven/caves-of-ooo')
ROOT=Path(__file__).resolve().parent
BASE=ROOT.parent/'mechanics-stage2'
STAGE3=ROOT.parent/'mechanics-stage3'
STAGE4=ROOT.parent/'mechanics-stage4'
BEE=REPO/'Library/Bee/artifacts/200b0aE.dag'
UNITY=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
CSC=[str(UNITY/'NetCoreRuntime/dotnet'),str(UNITY/'DotNetSdkRoslyn/csc.dll')]
OVERRIDES={
 'Assets/Tests/EditMode/Gameplay/AI/SpreadHuntPhaseValidationTests.cs':ROOT/'SpreadHuntPhaseValidationTests.cs',
 'Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs':STAGE4/'native-count/SpreadHuntAllocationTests.cs',
 'Assets/Scripts/Gameplay/AI/Goals/WanderRandomlyGoal.cs':STAGE3/'WanderRandomlyGoal.cs',
 'Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs':STAGE3/'SpreadHuntLiveGapTests.cs',
 'Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs':ROOT/'SpreadPredatorPart.cs',
 'Assets/Scripts/Gameplay/AI/SpreadGrazerPart.cs':STAGE4/'SpreadGrazerPart.cs',
 'Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs':BASE/'BoredGoal.cs',
 'Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs':BASE/'SpreadPredatorStage1Tests.cs',
 'Assets/Scripts/Gameplay/Entities/CorpsePart.cs':BASE/'CorpsePart.cs',
 'Assets/Scripts/Gameplay/World/Generation/SpreadExplorationReadout.cs':BASE/'SpreadExplorationReadout.cs',
 'Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs':BASE/'SpreadPredatorFeedingTests.cs',
 'Assets/Tests/EditMode/Gameplay/World/FurrowstalkerContentTests.cs':BASE/'FurrowstalkerContentTests.cs',
}
completed={}
for assembly in ['CavesOfOoo','EditModeTests']:
    rows=[]; replaced=set()
    for row in (BEE/(assembly+'.rsp')).read_text().splitlines():
        if row.startswith('-out:'): row='-out:"'+str(ROOT/(assembly+'.dll'))+'"'
        elif row.startswith('-refout:'): continue
        elif row.strip('"') in OVERRIDES:
            key=row.strip('"'); row='"'+str(OVERRIDES[key])+'"';replaced.add(key)
        elif row.startswith('-r:'):
            ref=row[3:].strip('"'); name=Path(ref).name.replace('.ref.dll','.dll')
            if name in completed: row='-r:"'+str(completed[name])+'"'
        rows.append(row)
    for key,p in OVERRIDES.items():
            if ((assembly=='EditModeTests' and key.startswith('Assets/Tests/')) or (assembly=='CavesOfOoo' and key.startswith('Assets/Scripts/'))) and key not in replaced: rows.append('"'+str(p)+'"')
    response=ROOT/(assembly+'-reference.rsp');response.write_text('\n'.join(rows)+'\n')
    run=subprocess.run(CSC+['@'+str(response)],cwd=REPO,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
    (ROOT/(assembly+'-reference-compile.log')).write_text(run.stdout)
    print(assembly,'exit',run.returncode)
    if run.returncode:
        print('\n'.join(x for x in run.stdout.splitlines() if 'error ' in x));sys.exit(run.returncode)
    completed[assembly+'.dll']=ROOT/(assembly+'.dll')
