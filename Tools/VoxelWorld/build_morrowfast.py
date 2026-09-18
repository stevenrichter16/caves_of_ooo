#!/usr/bin/env python3
"""Explicit, isolated Morrowfast coarse-scenery bake; never closes an editor."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'Village3D'))
from run_common import ROOT, UNITY, MCP, restart_mcp, refuse_unity


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('name')
    args = parser.parse_args()
    if not args.name or Path(args.name).name != args.name or args.name in ('.', '..'):
        parser.error('Name must be one directory component.')
    refuse_unity()
    output = ROOT / 'Docs/Verification/RegionalSituations' / args.name
    output.mkdir(parents=True, exist_ok=False)
    def art_hashes():
        return {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                for folder in ('Assets/Art3D/VoxelWorld', 'Assets/Art3D/Village', 'Assets/Resources/VoxelWorld', 'Assets/Resources/Village3D', 'Assets/Resources/SceneArt/Morrowfast')
                for p in (ROOT / folder).rglob('*') if p.is_file()}
    before = art_hashes()
    (output / 'art-before.json').write_text(json.dumps(before, indent=2) + '\n')
    (output / 'mcp-start.json').write_text(json.dumps(restart_mcp(MCP, args.name), indent=2) + '\n')
    command = [str(UNITY), '-batchmode', '-quit', '-projectPath', str(ROOT),
               '-executeMethod', 'CavesOfOoo.Editor.MorrowfastCoarseVoxelBuilder.BuildFromCommandLine',
               '-morrowfastCoarseReport', str(output / 'mesh-report.json'), '-logFile', str(output / 'unity.log')]
    print('Starting ' + args.name, flush=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    log = (output / 'unity.log').read_text(errors='replace')
    errors = [line for line in log.splitlines() if 'error CS' in line]
    print('C# error lines: ' + str(len(errors)), flush=True)
    after = art_hashes()
    changes = [name for name in sorted(before.keys() | after.keys()) if before.get(name) != after.get(name)]
    allowed = lambda name: name.startswith('Assets/Art3D/VoxelWorld/Morrowfast/') or name in (
        'Assets/Art3D/VoxelWorld/Morrowfast.meta', 'Assets/Resources/VoxelWorld/Library.asset')
    unexpected = [name for name in changes if not allowed(name)]
    report = {}
    if not errors and (output / 'mesh-report.json').exists():
        report = json.loads((output / 'mesh-report.json').read_text())
    receipt = {'unityExit': result.returncode, 'compileErrors': errors, 'changedArt': changes,
               'unexpectedArtChanges': unexpected, 'models': report.get('models'), 'meshes': report.get('meshes'),
               'pass': result.returncode == 0 and not errors and not unexpected and report.get('status') == 'passed'}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({k:v for k,v in receipt.items() if k != 'changedArt'}, indent=2), flush=True)
    return 0 if receipt['pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
