from pathlib import Path
import subprocess, sys
REPO=Path('/Users/steven/caves-of-ooo')
ROOT=Path(__file__).resolve().parent
BEE=REPO/'Library/Bee/artifacts/200b0aE.dag'
UNITY=Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/Resources/Scripting')
CSC=[str(UNITY/'NetCoreRuntime/dotnet'),str(UNITY/'DotNetSdkRoslyn/csc.dll')]
PHASE=sys.argv[1]
OUT=ROOT/PHASE/'compile'
OUT.mkdir(exist_ok=True)
OVERRIDES={str(p.relative_to(REPO)): ROOT/PHASE/p.name for p in (REPO/'Assets/Tests/EditMode/Gameplay/World').glob('SpreadHunt*.cs')}
completed={}
for assembly in ['CavesOfOoo','EditModeTests']:
    rows=[]; replaced=set()
    for row in (BEE/(assembly+'.rsp')).read_text().splitlines():
        if row.startswith('-out:'): row='-out:"'+str(OUT/(assembly+'.dll'))+'"'
        elif row.startswith('-refout:'): continue
        elif row.strip('"') in OVERRIDES:
            key=row.strip('"'); row='"'+str(OVERRIDES[key])+'"';replaced.add(key)
        elif row.startswith('-r:'):
            ref=row[3:].strip('"'); name=Path(ref).name.replace('.ref.dll','.dll')
            if name in completed: row='-r:"'+str(completed[name])+'"'
        rows.append(row)
    if assembly=='EditModeTests':
        for key,p in OVERRIDES.items():
            if key.startswith('Assets/Tests/') and key not in replaced: rows.append('"'+str(p)+'"')
    response=OUT/(assembly+'-reference.rsp');response.write_text('\n'.join(rows)+'\n')
    run=subprocess.run(CSC+['@'+str(response)],cwd=REPO,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
    (OUT/(assembly+'-reference-compile.log')).write_text(run.stdout)
    print(assembly,'exit',run.returncode)
    if run.returncode:
        print('\n'.join(x for x in run.stdout.splitlines() if 'error ' in x));sys.exit(run.returncode)
    completed[assembly+'.dll']=OUT/(assembly+'.dll')
