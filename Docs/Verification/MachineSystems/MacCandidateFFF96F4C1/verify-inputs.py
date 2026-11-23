from pathlib import Path
import hashlib, json, tarfile, subprocess

bundle = Path('/private/tmp/coo-mac-fff96f4c1-20261123-export')
prep = json.loads((bundle/'preparation.json').read_text())
project = Path(prep['copy']); app = Path(prep['output'])/'CavesOfOoo.app'
report = json.loads((bundle/'build-report.json').read_text())
assert report['result']=='Succeeded' and report['errors']==0 and report['commit']==prep['commit']
def pins(root, directories):
    out = {}
    for directory in directories:
        for p in sorted((root/directory).rglob('*')):
            if p.is_file(): out[str(p.relative_to(root))] = hashlib.sha256(p.read_bytes()).hexdigest()
    return out
before = json.loads((bundle/'source-pins.json').read_text())
after = pins(project,['Assets','Packages','ProjectSettings','ArtSource'])
(bundle/'post-build-input-pins.json').write_text(json.dumps(after,indent=2)+'\n')
changed = {p:{'before':before[p],'after':after[p]} for p in before.keys() & after.keys() if before[p]!=after[p]}
missing = sorted(before.keys()-after.keys()); added=sorted(after.keys()-before.keys())
runtime_changed=[p for p in changed if p.startswith('Assets/Scripts/') and p.endswith('.cs')]
assert not runtime_changed and not missing, (runtime_changed, missing)
delta={'changed':changed,'missing':missing,'added':added,'runtimeCSharpChanged':runtime_changed}
(bundle/'input-deltas.json').write_text(json.dumps(delta,indent=2)+'\n')
with tarfile.open(bundle/'effective-input-deltas.tar.gz','w:gz') as overlay:
    for name in sorted(set(changed)|set(added)):overlay.add(project/name,arcname=name,recursive=False)
player = {str(f.relative_to(app)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(app.rglob('*')) if f.is_file()}
(bundle/'player-pins.json').write_text(json.dumps(player,indent=2)+'\n')
external=Path('/Users/steven/unity-mcp/MCPForUnity')
external_after={str(f.relative_to(external)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(external.rglob('*')) if f.is_file()}
external_before=json.loads((bundle/'external-mcp-pins.json').read_text())
assert external_after==external_before,'External MCP package changed during build'
architecture=subprocess.check_output(['lipo','-archs',str(app/'Contents/MacOS/caves-of-ooo')],text=True).strip()
assert set(architecture.split())=={'x86_64','arm64'}
result={'commit':prep['commit'],'sourcePins':len(before),'effectivePins':len(after),'changed':len(changed),'missing':len(missing),'added':len(added),'externalPins':len(external_before),'playerPins':len(player),'architecture':architecture,'runtimeCSharpChanged':runtime_changed}
(bundle/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result))
