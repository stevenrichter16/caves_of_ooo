from pathlib import Path
import subprocess, sys
REPO=Path('/Users/steven/caves-of-ooo')
ROOT=Path(__file__).resolve().parent
BEE=REPO/'Library/Bee/artifacts/200b0aE.dag'
UNITY=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
CSC=[str(UNITY/'NetCoreRuntime/dotnet'),str(UNITY/'DotNetSdkRoslyn/csc.dll')]
variant=sys.argv[1] if len(sys.argv)>1 else 'candidate'
OUTPUT=ROOT/('reference-'+variant);OUTPUT.mkdir(exist_ok=True)
OVERRIDES={
 'Assets/Tests/EditMode/Presentation/Rendering/QuestFreePhysicalPursuitNativeModeTests.cs':ROOT/'tests/QuestFreePhysicalPursuitNativeModeTests.cs',
}
if variant=='candidate':
 OVERRIDES.update({
 'Assets/Scripts/Scenarios/Custom/QuestFreeSpreadStateNativePlayer.cs':ROOT/'production/QuestFreeSpreadStateNativePlayer.cs',
 'Assets/Scripts/Scenarios/Custom/QuestFreeSpreadStateNativePlayer.PhysicalPursuit.cs':ROOT/'production/QuestFreeSpreadStateNativePlayer.PhysicalPursuit.cs',
 'Assets/Editor/Scenarios/QuestFreeSpreadStateNativeBatch.cs':ROOT/'production/QuestFreeSpreadStateNativeBatch.cs',
 })

completed={}
for assembly in ['CavesOfOoo','Assembly-CSharp-Editor','EditModeTests']:
    rows=[]; replaced=set()
    for row in (BEE/(assembly+'.rsp')).read_text().splitlines():
        if row.startswith('-out:'): row='-out:"'+str(OUTPUT/(assembly+'.dll'))+'"'
        elif row.startswith('-refout:'): continue
        elif row.strip('"') in OVERRIDES:
            key=row.strip('"'); row='"'+str(OVERRIDES[key])+'"';replaced.add(key)
        elif row.startswith('-r:'):
            ref=row[3:].strip('"'); name=Path(ref).name.replace('.ref.dll','.dll')
            if name in completed: row='-r:"'+str(completed[name])+'"'
        rows.append(row)
    for key,p in OVERRIDES.items():
        kind='EditModeTests' if key.startswith('Assets/Tests/') else 'Assembly-CSharp-Editor' if key.startswith('Assets/Editor/') else 'CavesOfOoo'
        if kind==assembly and key not in replaced: rows.append(chr(34)+str(p)+chr(34))
    response=OUTPUT/(assembly+'-reference.rsp');response.write_text('\n'.join(rows)+'\n')
    run=subprocess.run(CSC+['@'+str(response)],cwd=REPO,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
    (OUTPUT/(assembly+'-reference-compile.log')).write_text(run.stdout)
    print(assembly,'exit',run.returncode)
    if run.returncode:
        print('\n'.join(x for x in run.stdout.splitlines() if 'error ' in x));sys.exit(run.returncode)
    completed[assembly+'.dll']=OUTPUT/(assembly+'.dll')
