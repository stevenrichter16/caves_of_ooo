#!/usr/bin/env python3
"""Run one explicit, isolated ground-sprite A/B side; never modifies renderer source."""
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
ENTRY = 'CavesOfOoo.Editor.EquipmentGroundSpriteNativeAuditBatch.RunFromCommandLine'


def write(path, value):
    with path.open('x') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('name')
    parser.add_argument('--mode', required=True, choices=('before', 'after'))
    parser.add_argument('--execute', action='store_true')
    args = parser.parse_args(argv)
    if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_.-]{0,90}', args.name) or args.name in ('.', '..'):
        parser.error('Name must be one safe directory component.')
    output = ROOT / 'Docs/Verification/ReleaseR1/GroundSprites' / args.name
    command = [str(UNITY), '-projectPath', str(ROOT), '-executeMethod', ENTRY,
               '--equipment-ground-mode', args.mode, '--equipment-ground-output', str(output),
               '-logFile', str(output / 'unity.log')]
    if not args.execute:
        print(json.dumps({'launch': False, 'command': command,
                          'scope': 'Synthetic native renderer workload; caller prepares and preserves the selected renderer source.'}, indent=2))
        return 0
    sys.path.insert(0, str(ROOT / 'Tools/Village3D'))
    from run_common import refuse_unity, restart_mcp, sources
    refuse_unity()
    output.mkdir(parents=True, exist_ok=False)
    before = sources(ROOT)
    write(output / 'command.json', command)
    (output / 'sources-before.json.gz').write_bytes(gzip.compress(json.dumps(before, sort_keys=True).encode(), mtime=0))
    write(output / 'mcp-start.json', restart_mcp(Path('/Users/steven/unity-mcp/Server'), args.name))
    refuse_unity()
    print('Starting ground-sprite native ' + args.mode + ': ' + args.name, flush=True)
    process = subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    timed_out = False
    try:
        code = process.wait(timeout=600)
    except subprocess.TimeoutExpired:
        timed_out = True
        process.terminate()  # Only this launcher's child, never another Editor.
        try:
            code = process.wait(timeout=30)
        except subprocess.TimeoutExpired:
            process.kill()
            code = process.wait(timeout=10)
    log = (output / 'unity.log').read_text(errors='replace') if (output / 'unity.log').is_file() else ''
    compile_errors = [line for line in log.splitlines() if 'error CS' in line]
    print('C# error lines: ' + str(len(compile_errors)), flush=True)
    exceptions = [line for line in log.splitlines() if re.match(r'^(?:[\w.]+\.)?\w*Exception:', line)]
    receipt = {'unityExit': code, 'mode': args.mode, 'timeout': timed_out,
               'compileErrors': compile_errors, 'unhandledLogExceptions': exceptions}
    try:
        if compile_errors or exceptions or timed_out or code != 0:
            raise RuntimeError('Compilation, exception, timeout or exit failure invalidates acceptance.')
        reports = list(output.glob('EGSN-*-native.json'))
        if len(reports) != 1:
            raise RuntimeError('Expected exactly one new report in the owned output directory.')
        report = json.loads(reports[0].read_text())
        run_id = report.get('runId', '')
        stem = 'EGSN-' + args.mode + '-' + run_id
        if not re.fullmatch(r'[a-f0-9]{32}', run_id) or reports[0].name != stem + '-native.json':
            raise RuntimeError('Report stamp/mode mismatch.')
        cleanup = json.loads((output / (stem + '-cleanup.json')).read_text())
        if (cleanup.get('runId') != run_id or cleanup.get('mode') != args.mode
                or cleanup.get('exitCode') != 0 or not cleanup.get('reportVerified')
                or not cleanup.get('validatorCounterchecks') or not cleanup.get('artifactsVerified')
                or not cleanup.get('privateRootRemoved')):
            raise RuntimeError('Native validation, negative controls or private cleanup failed.')
        if report.get('failures') != 0 or report.get('unexpectedErrors') != 0:
            raise RuntimeError('Native checks failed.')
        artifacts = []
        for path in sorted(output.glob(stem + '-*')):
            if path.is_symlink() or not path.is_file():
                raise RuntimeError('Unexpected artifact ownership.')
            data = path.read_bytes()
            artifacts.append({'name': path.name, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
        receipt.update({'runId': run_id, 'validated': True, 'checks': len(report.get('checks', [])),
                        'artifacts': artifacts, 'phases': report.get('phases'), 'bounds': report.get('bounds')})
    except Exception as error:
        receipt.update({'validated': False, 'evidenceError': str(error)})
    after = sources(ROOT)
    (output / 'sources-after.json.gz').write_bytes(gzip.compress(json.dumps(after, sort_keys=True).encode(), mtime=0))
    receipt['changedSources'] = [p for p in sorted(before.keys() | after.keys()) if before.get(p) != after.get(p)]
    receipt['sourceFrozen'] = not receipt['changedSources']
    receipt['pass'] = bool(receipt.get('validated') and receipt['sourceFrozen'])
    write(output / 'receipt.json', receipt)
    if (output / 'unity.log').is_file():
        (output / 'unity.log.gz').write_bytes(gzip.compress((output / 'unity.log').read_bytes(), mtime=0))
    print(json.dumps({k: v for k, v in receipt.items() if k not in ('artifacts', 'phases')}, indent=2), flush=True)
    return 0 if receipt['pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
