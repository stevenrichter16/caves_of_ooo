#!/usr/bin/env python3
"""Explicit native GameView audit launcher. Import and default use never launch Unity."""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity')
ENTRY = 'CavesOfOoo.Editor.VoxelWorldNativeAuditBatch.RunFromCommandLine'
LIVE = ROOT / 'Docs/Verification/VoxelWorld'


def write(path, value):
    with path.open('x') as stream:
        json.dump(value, stream, indent=2, sort_keys=True)
        stream.write('\n')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('name', help='New receipt directory name under Docs/Verification/VoxelWorld')
    parser.add_argument('--execute', action='store_true', help='Actually restart MCP and launch a native Unity Editor')
    args = parser.parse_args(argv)
    if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_.-]{0,90}', args.name) or args.name in {'.', '..'}:
        parser.error('Name must be one safe directory component.')
    command = [str(UNITY), '-projectPath', str(ROOT), '-executeMethod', ENTRY,
               '-logFile', str(LIVE / args.name / 'unity.log')]
    if not args.execute:
        print(json.dumps({'launch': False, 'command': command,
                          'scope': 'Native rendered Editor, private save root, seven real GameView captures and 80-second pilot profile.'}, indent=2))
        return 0

    sys.path.insert(0, str(ROOT / 'Tools/Village3D'))
    from run_common import refuse_unity, restart_mcp, sources
    refuse_unity()
    output = LIVE / args.name
    output.mkdir(parents=True, exist_ok=False)
    before_outputs = {p.name for p in LIVE.glob('VWN-*')}
    source_before = sources(ROOT)
    write(output / 'sources-before.json', source_before)
    write(output / 'command.json', command)
    write(output / 'mcp-start.json', restart_mcp(Path('/Users/steven/unity-mcp/Server'), args.name))
    refuse_unity()
    (ROOT / 'Temp/UnityLockfile').unlink(missing_ok=True)
    print('Starting native voxel acceptance: ' + args.name, flush=True)
    process = subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    timed_out = False
    try:
        code = process.wait(timeout=1100)
    except subprocess.TimeoutExpired:
        # Signal only the exact process this invocation owns, never another Editor.
        timed_out = True
        process.terminate()
        try:
            code = process.wait(timeout=45)
        except subprocess.TimeoutExpired:
            process.kill()
            code = process.wait(timeout=10)

    log = (output / 'unity.log').read_text(errors='replace') if (output / 'unity.log').is_file() else ''
    errors = [line for line in log.splitlines() if 'error CS' in line]
    print('C# error lines: ' + str(len(errors)), flush=True)
    receipt = {'unityExit': code, 'timeout': timed_out, 'compileErrors': errors, 'entry': ENTRY,
               'bounds': 'Native report counters require human inspection of attached screenshots for visual quality. Profile covers the pilot, not all four chunks.'}
    try:
        if errors:
            raise RuntimeError('Compiler errors invalidate all native evidence.')
        native = [p for p in LIVE.glob('VWN-*-native.json') if p.name not in before_outputs]
        if len(native) != 1:
            raise RuntimeError('Expected one unique new native report; found ' + str(len(native)))
        report = json.loads(native[0].read_text())
        run_id = report.get('runId', '')
        if not re.fullmatch(r'[a-f0-9]{32}', run_id) or native[0].name != f'VWN-{run_id}-native.json':
            raise RuntimeError('Native run ID does not match the newly created report.')
        cleanup_path = LIVE / f'VWN-{run_id}-cleanup.json'
        if cleanup_path.name in before_outputs:
            raise RuntimeError('Cleanup report predates this invocation.')
        cleanup = json.loads(cleanup_path.read_text())
        if cleanup.get('runId') != run_id or not cleanup.get('finalNativeVerified') or cleanup.get('exitCode') != 0:
            raise RuntimeError('Native editor did not validate the complete run and restoration.')
        if report.get('failures') != 0 or report.get('unexpectedErrors') != 0 or not report.get('workloadComplete'):
            raise RuntimeError('Native acceptance reported incomplete or failed work.')
        archive = output / 'artifacts'
        archive.mkdir()
        artifacts = []
        for path in sorted(LIVE.glob(f'VWN-{run_id}-*')):
            if path.is_symlink() or not path.is_file() or path.name in before_outputs:
                raise RuntimeError('Unexpected new native artifact ownership: ' + str(path))
            data = path.read_bytes()
            (archive / path.name).write_bytes(data)
            artifacts.append({'path': str(path), 'archive': str(archive / path.name),
                              'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
        receipt.update({'runId': run_id, 'nativeReport': str(native[0]), 'cleanupReport': str(cleanup_path),
                        'artifacts': artifacts, 'checks': len(report.get('checks', [])),
                        'chunks': report.get('region'), 'borders': report.get('borders'), 'validated': True})
    except Exception as error:
        receipt.update({'validated': False, 'evidenceError': str(error)})
    source_after = sources(ROOT)
    write(output / 'sources-after.json', source_after)
    receipt['changedSources'] = [name for name in sorted(source_before.keys() | source_after.keys())
                                 if source_before.get(name) != source_after.get(name)]
    receipt['sourceFrozen'] = not receipt['changedSources']
    receipt['pass'] = bool(code == 0 and not timed_out and not errors and receipt.get('validated') and receipt['sourceFrozen'])
    write(output / 'receipt.json', receipt)
    if (output / 'unity.log').is_file():
        with gzip.open(output / 'unity.log.gz', 'wb') as target:
            target.write((output / 'unity.log').read_bytes())
    print(json.dumps(receipt, indent=2), flush=True)
    return 0 if receipt['pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
