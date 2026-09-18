"""Explicit fresh native Editor receipts. No existing Editor is terminated."""
import argparse
import gzip
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path('/Users/steven/caves-of-ooo')
BASE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'Tools/Village3D'))
from run_common import restart_mcp, refuse_unity

parser = argparse.ArgumentParser()
parser.add_argument('name')
parser.add_argument('kind', choices=['gpu', 'game'])
args = parser.parse_args()
out = BASE / args.name
out.mkdir()
refuse_unity()
(out / 'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'), args.name), indent=2) + '\n')
native_dir = BASE / 'NativeAudit'
before = set(native_dir.glob('*-native.json'))
entry = 'CavesOfOoo.Editor.NativeSpellFxGpuProbe.RunFromCommandLine' if args.kind == 'gpu' else 'CavesOfOoo.Editor.StarterSpell3DNativeAuditBatch.RunFromCommandLine'
cmd = ['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity', '-projectPath', str(ROOT), '-executeMethod', entry, '-logFile', str(out / 'unity.log')]
if args.kind == 'gpu':
    cmd.extend(['-spellFxGpuReport', str(out / 'gpu.json'), '-quit'])
print('Starting native ' + args.name, flush=True)
result = subprocess.run(cmd, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
log = (out / 'unity.log').read_text(errors='replace')
errors = [line for line in log.splitlines() if 'error CS' in line]
print('C# error lines: ' + str(len(errors)), flush=True)
receipt = {'unityExit': result.returncode, 'compileErrors': errors, 'kind': args.kind, 'passed': False}
if not errors:
    if args.kind == 'gpu' and (out / 'gpu.json').is_file():
        report = json.loads((out / 'gpu.json').read_text())
        receipt.update(reportPath=str(out / 'gpu.json'), status=report['status'], failures=[c for c in report.get('cases', []) if not c['passed']])
        receipt['passed'] = report['status'] == 'PASS' and result.returncode == 0
    elif args.kind == 'game':
        new = set(native_dir.glob('*-native.json')) - before
        if len(new) == 1:
            path = new.pop()
            report = json.loads(path.read_text())
            cleanup_path = path.with_name(path.name.replace('-native.json', '-cleanup.json'))
            cleanup = json.loads(cleanup_path.read_text()) if cleanup_path.is_file() else {}
            receipt.update(reportPath=str(path), cleanupPath=str(cleanup_path), failures=report.get('failures'), fatal=report.get('fatal'), workloadComplete=report.get('workloadComplete'), cleanup=cleanup)
            receipt['passed'] = result.returncode == 0 and report.get('failures') == 0 and report.get('workloadComplete') and cleanup.get('exitCode') == 0 and cleanup.get('runId') == report.get('runId')
        else:
            receipt['error'] = 'Expected exactly one fresh native report; found ' + str(len(new))
(out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
with gzip.open(out / 'unity.log.gz', 'wb') as target:
    target.write((out / 'unity.log').read_bytes())
display = dict(receipt)
if isinstance(display.get('failures'), list):
    display['failures'] = [{'id': row.get('id'), 'error': row.get('error')} for row in display['failures']]
print(json.dumps(display, indent=2), flush=True)
sys.exit(0 if receipt['passed'] else 1)
