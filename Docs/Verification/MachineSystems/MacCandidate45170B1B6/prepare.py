from pathlib import Path
import hashlib, json, subprocess, tarfile

repo = Path('/Users/steven/caves-of-ooo')
commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip()
tag = commit[:9] + '-20261122'
bundle = Path('/private/tmp/coo-mac-' + tag + '-export')
project = Path('/Users/steven/coo-verification/mac-' + tag)
output = Path('/Users/steven/coo-verification/builds/mac-' + tag)
assert not bundle.exists() and not project.exists() and not output.exists()
bundle.mkdir(); project.mkdir(); output.mkdir()
roots = ['Assets', 'Packages', 'ProjectSettings', 'ArtSource']
subprocess.run(['git', 'archive', '--format=tar', '-o', str(bundle/'input.tar'), commit, *roots], cwd=repo, check=True)
with tarfile.open(bundle/'input.tar') as archive:
    archive.extractall(project, filter='data')
def pins(root, directories):
    result = {}
    for directory in directories:
        for f in sorted((root/directory).rglob('*')):
            if f.is_file(): result[str(f.relative_to(root))] = hashlib.sha256(f.read_bytes()).hexdigest()
    return result
source = pins(project, roots)
(bundle/'source-pins.json').write_text(json.dumps(source, indent=2)+'\n')
subprocess.run(['cp', '-cR', '/Users/steven/coo-verification/clone3/Library', str(project/'Library')], check=True)
old = Path('/private/tmp/coo-mac-47761ba77-export-20261121/CodexCandidateBuild.cs').read_text()
helper = old.replace('/Users/steven/coo-verification/mac-47761ba77-20261121', str(project))
helper = helper.replace('/Users/steven/coo-verification/builds/mac-47761ba77-20261121', str(output))
helper = helper.replace('/private/tmp/coo-mac-47761ba77-export-20261121', str(bundle))
helper = helper.replace('47761ba77b5a452cefa4c0ffc894f28a43b54b17', commit)
(project/'Assets/Editor/CodexCandidateBuild.cs').write_text(helper)
(bundle/'CodexCandidateBuild.cs').write_text(helper)
external = Path('/Users/steven/unity-mcp/MCPForUnity')
external_pins = {str(f.relative_to(external)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(external.rglob('*')) if f.is_file()}
(bundle/'external-mcp-pins.json').write_text(json.dumps(external_pins,indent=2)+'\n')
preparation = {'commit':commit,'sourcePins':len(source),'copy':str(project),'output':str(output),'warmLibraryFrom':'clone3','bundle':str(bundle)}
(bundle/'preparation.json').write_text(json.dumps(preparation,indent=2)+'\n')
print(json.dumps(preparation), flush=True)
