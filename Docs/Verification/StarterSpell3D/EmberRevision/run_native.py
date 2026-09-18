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
parser.add_argument('kind', choices=['gpu', 'readability-gpu', 'game'])
parser.add_argument('--ember-motion', action='store_true')
args = parser.parse_args()
if args.ember_motion and args.kind != 'game':
    parser.error('--ember-motion requires the actual game audit')
out = BASE / args.name
out.mkdir()
refuse_unity()
(out / 'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'), args.name), indent=2) + '\n')
# The existing game launcher retains its Integration-owned fixed output contract.
native_dir = ROOT / 'Docs/Verification/StarterSpell3D/Integration/NativeAudit'
before = set(native_dir.glob('*-native.json'))
entry = {'gpu': 'CavesOfOoo.Editor.NativeSpellFxGpuProbe.RunFromCommandLine',
         'readability-gpu': 'CavesOfOoo.Editor.NativeSpellReadabilityGpuProbe.RunFromCommandLine',
         'game': 'CavesOfOoo.Editor.StarterSpell3DNativeAuditBatch.RunFromCommandLine'}[args.kind]
cmd = ['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity', '-projectPath', str(ROOT), '-executeMethod', entry, '-logFile', str(out / 'unity.log')]
if args.kind in ('gpu', 'readability-gpu'):
    flag = '-spellReadabilityGpuReport' if args.kind == 'readability-gpu' else '-spellFxGpuReport'
    cmd.extend([flag, str(out / 'gpu.json'), '-quit'])
if args.ember_motion:
    cmd.append('-emberMotionCapture')
print('Starting native ' + args.name, flush=True)
result = subprocess.run(cmd, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
log = (out / 'unity.log').read_text(errors='replace')
errors = [line for line in log.splitlines() if 'error CS' in line]
print('C# error lines: ' + str(len(errors)), flush=True)
receipt = {'unityExit': result.returncode, 'compileErrors': errors, 'kind': args.kind, 'emberMotionRequested': args.ember_motion, 'passed': False}
if not errors:
    if args.kind in ('gpu', 'readability-gpu') and (out / 'gpu.json').is_file():
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
            if args.ember_motion:
                motion_path = path.with_name(path.name.replace('-native.json', '-ember-motion.json'))
                motion = json.loads(motion_path.read_text()) if motion_path.is_file() else {}
                receipt['emberMotionReportPath'] = str(motion_path)
                receipt['emberMotionPassed'] = bool(motion.get('passed') and motion.get('released') and motion.get('pendingAtRelease') == 0 and cleanup.get('emberMotionVerified'))
                receipt['passed'] = receipt['passed'] and receipt['emberMotionPassed']
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
