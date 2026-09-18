#!/usr/bin/env python3
"""External immutable-source/protected-asset/GUID evidence; no Unity execution."""
import hashlib, json, pathlib, re, subprocess, sys

def digest(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()

def files_under(root):
    return {str(p.relative_to(root)):digest(p) for p in sorted(root.rglob('*')) if p.is_file()}

def snapshot(repo,source):
    repo,source=pathlib.Path(repo).resolve(),pathlib.Path(source).resolve()
    files={}
    for folder in ['Assets','ProjectSettings','Packages']:
        for rel,value in files_under(repo/folder).items():files[folder+'/'+rel]=value
    guids={}
    for name in files:
        if name.endswith('.meta'):
            match=re.search(r'^guid:\s*([a-f0-9]{32})\s*$',(repo/name).read_text(),re.M)
            if match:guids[name]=match.group(1)
    return {'repo':str(repo),'source':str(source),'files':files,'guids':guids,'sourceFiles':files_under(source),
            'gitStatus':subprocess.check_output(['git','status','--porcelain=v1','--untracked-files=all'],cwd=repo,text=True)}

def owned(name):
    return name in ['Assets/Art3D/SpawnRing.meta','Assets/Resources/SpawnRing3D.meta'] or name.startswith(('Assets/Art3D/SpawnRing/','Assets/Resources/SpawnRing3D/'))

# Independent MCP plugin writes these two logs. Their metas and every other
# UnityMCP file remain protected; retain log hashes/deltas without failing import.
EXTERNAL_WRITER_LOGS={'Assets/UnityMCP/Log/mcp.log','Assets/UnityMCP/Log/mcpError.log'}

def check(before,after,folder,exit_code):
    errors=[]
    def need(condition, message):
        if not condition:errors.append(message)
    need(exit_code==0,f'Unity exited {exit_code}')
    changed=[p for p in sorted(set(before['files'])|set(after['files'])) if before['files'].get(p)!=after['files'].get(p)]
    protected=[p for p in changed if not owned(p) and p not in EXTERNAL_WRITER_LOGS]
    external_logs=[p for p in changed if p in EXTERNAL_WRITER_LOGS]
    need(not protected,'Non-owned project files changed: '+repr(protected))
    need(before['sourceFiles']==after['sourceFiles'],'Completed source export changed during import')
    churn=[p for p,g in before['guids'].items() if after['guids'].get(p)!=g]
    need(not churn,'Existing source/project meta GUIDs changed or disappeared: '+repr(churn))
    guid_paths={}
    for p,g in after['guids'].items():guid_paths.setdefault(g,[]).append(p)
    collisions={g:p for g,p in guid_paths.items() if len(p)>1}
    # All collisions are retained, and a clean baseline is a required precondition.
    need(not collisions,'GUID collision(s) in Assets: '+repr(collisions))
    imported=scene=None
    try:imported=json.loads((folder/'import.json').read_text())
    except Exception as e:errors.append('Missing/unreadable importer report: '+repr(e))
    try:scene=json.loads((folder/'scenes.json').read_text())
    except Exception as e:errors.append('Missing/unreadable scene report: '+repr(e))
    for label,r in [('import',imported),('scene',scene)]:
        if r is not None:need(bool(re.fullmatch('[a-f0-9]{32}',r.get('runId',''))),label+' runId is not a fresh-form GUID')
    if imported is not None:
        need(imported.get('status')=='passed-import-validation','Importer validation did not pass')
        need(imported.get('sourceRoot')==before['source'],'Importer source root does not match captured source')
        need(imported.get('catalogSha256')==before['sourceFiles'].get('catalog.json'),'Importer catalog hash mismatch')
        need(imported.get('modelCount')==218 and len(imported.get('models',[]))==218,'Expected218 measured imported models')
        need(imported.get('blueprintCount')==73 and imported.get('fellingOwnerCount')==55,'Expected73 blueprints/55 exact native owners')
        need(imported.get('borrowedAssetsUnchanged') is True,'Importer direct borrowed-asset receipt did not pass')
    if scene is not None:
        need(scene.get('status')=='PASS','Scene preservation receipt failed')
        need(pathlib.Path(scene.get('importerReport','')).resolve()==(folder/'import.json').resolve(),'Scene receipt references another import')
        for key in ['cleanControlVerified','dirtyControlVerified','importScenesPreserved','originalScenesPreserved','ownedScenesClosed']:
            need(scene.get(key) is True,'Scene control failed: '+key)
        need(not scene.get('unexpectedLogs'),'Import emitted error/assert/exception logs')
    report={'status':'PASS' if not errors else 'FAIL','scope':'Explicit import and file/scene preservation, not rendered gameplay',
            'errors':errors,'changedOwnedFiles':[p for p in changed if owned(p)],'changedProtectedFiles':protected,'externalWriterLogChanges':external_logs,
            'existingGuidChurn':churn,'guidCollisions':collisions,'sourceFilesUnchanged':before['sourceFiles']==after['sourceFiles'],
            'protectedAssetsUnchanged':not protected,'importRunId':(imported or {}).get('runId'),'sceneRunId':(scene or {}).get('runId')}
    (folder/'receipt.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'status':report['status'],'receipt':str(folder/'receipt.json'),'errors':errors},indent=2))
    return bool(errors)

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='snapshot':
        pathlib.Path(sys.argv[4]).write_text(json.dumps(snapshot(sys.argv[2],sys.argv[3]),indent=2)+'\n')
    elif mode=='preflight':
        before=json.loads(pathlib.Path(sys.argv[2]).read_text());paths={}
        for p,g in before['guids'].items():paths.setdefault(g,[]).append(p)
        collisions={g:p for g,p in paths.items() if len(p)>1}
        if collisions:raise SystemExit('Pre-existing GUID collisions need review before import: '+repr(collisions))
    elif mode=='check':
        folder=pathlib.Path(sys.argv[2]).resolve()
        sys.exit(check(json.loads((folder/'before.json').read_text()),json.loads((folder/'after.json').read_text()),folder,int(sys.argv[3])))
    else:raise SystemExit('snapshot <repo> <source> <output> OR check <run-directory> <Unity-exit>')
