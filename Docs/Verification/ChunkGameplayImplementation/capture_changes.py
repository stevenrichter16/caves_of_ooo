"""Review only this task's changes against its preserved starting workspace.
Does not stage, reset or commit anything, including pre-existing untracked code.
"""
from pathlib import Path
import difflib
import hashlib
import json
import subprocess

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
snapshot=json.loads((OUT/'prechange.json').read_text())
BACKUP=Path(snapshot['backup'])
HEAD=snapshot['head']
changes={}

def add(name,before,after):
    if before!=after:changes[name]=(before,after)

for name in snapshot['paths']:
    p=ROOT/name;old=BACKUP/name
    if old.is_file():add(name,old.read_bytes(),p.read_bytes() if p.is_file() else b'')
for folder in ('Assets/Scripts','Assets/Editor','Assets/Tests'):
    for p in (ROOT/folder).rglob('*.cs'):
        name=str(p.relative_to(ROOT))
        if name not in snapshot['paths']:
            add(name,b'',p.read_bytes())
            meta=Path(str(p)+'.meta')
            if meta.exists():add(str(meta.relative_to(ROOT)),b'',meta.read_bytes())
art=json.loads((ROOT/'Docs/Verification/VoxelWorld/CG08-stubble-build/receipt.json').read_text())
art_before=json.loads((ROOT/'Docs/Verification/VoxelWorld/CG08-stubble-build/assets-before.json').read_text())
for name in art['new']:add(name,b'',(ROOT/name).read_bytes())
for name in art['changedExisting']:
    before=subprocess.check_output(['git','show',HEAD+':'+name],cwd=ROOT)
    assert hashlib.sha256(before).hexdigest()==art_before[name], 'Pre-existing art edits require their original bytes: '+name
    add(name,before,(ROOT/name).read_bytes())
for name in ('Docs/CHUNK-GAMEPLAY-AUDIT.md','Docs/CHUNK-GAMEPLAY-IMPLEMENTATION.md',
             'Docs/CANONICAL-VILLAGE-QUESTS.md','Docs/REGIONAL-GUIDANCE.md',
             'Docs/CHUNK-GAMEPLAY-NATIVE-AUDIT.md',
             'Docs/BIOME-AFFORDANCES.md','Docs/MORROWFAST-MERCHANT-QUEST-CUES.md',
             'Tools/ChunkGameplay/run_native.py'):
    p=ROOT/name
    if not p.is_file():continue
    old=subprocess.run(['git','show',HEAD+':'+name],cwd=ROOT,capture_output=True)
    add(name,old.stdout if old.returncode==0 else b'',p.read_bytes())
patch=[];rows=[]
for name,(before,after) in sorted(changes.items()):
    row={'path':name,'beforeSha256':hashlib.sha256(before).hexdigest() if before else None,'afterSha256':hashlib.sha256(after).hexdigest(),'newAtTaskStart':not bool(before)}
    tracked=subprocess.run(['git','cat-file','-e',HEAD+':'+name],cwd=ROOT,capture_output=True).returncode==0
    row['existedInStartingCommit']=tracked
    row['preExistingUntrackedModified']=bool(before) and not tracked
    try:
        lines=difflib.unified_diff(before.decode().splitlines(True),after.decode().splitlines(True),fromfile='a/'+name if before else '/dev/null',tofile='b/'+name)
        patch.append('diff --git a/'+name+' b/'+name+'\n')
        for line in lines:
            patch.append(line if line.endswith('\n') else line+'\n\\ No newline at end of file\n')
    except UnicodeDecodeError:row['binaryNotInTextPatch']=True
    rows.append(row)
(OUT/'changes.patch').write_text(''.join(patch))
(OUT/'changes.json').write_text(json.dumps({'baseCommit':HEAD,'baseIsPreservedWorkingTree':True,'files':rows,'note':'Not a HEAD-only patch. Existing untracked production was preserved; no files staged or committed.'},indent=2)+'\n')
print(json.dumps({'changedFiles':len(rows),'preExistingUntrackedModified':[r['path'] for r in rows if r['preExistingUntrackedModified']],'binary':[r['path'] for r in rows if r.get('binaryNotInTextPatch')]}))
