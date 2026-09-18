"""Run an isolated named Unity EditMode receipt; never close an open Editor."""
import argparse
import gzip
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path('/Users/steven/caves-of-ooo')
sys.path.insert(0, str(ROOT / 'Tools/Village3D'))
from run_common import restart_mcp, refuse_unity

parser = argparse.ArgumentParser()
parser.add_argument('name')
parser.add_argument('--filter')
args = parser.parse_args()
out = ROOT / 'Docs/Verification/VoxelWorld' / args.name
out.mkdir()
refuse_unity()
mcp = restart_mcp(Path('/Users/steven/unity-mcp/Server'), args.name)
(out / 'mcp-start.json').write_text(json.dumps(mcp, indent=2) + '\n')
cmd = ['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity',
       '-batchmode', '-projectPath', str(ROOT), '-runTests', '-testPlatform', 'EditMode',
       '-testResults', str(out / 'results.xml'), '-logFile', str(out / 'unity.log')]
if args.filter:
    cmd.extend(['-testFilter', args.filter])
(ROOT / 'Temp/UnityLockfile').unlink(missing_ok=True)
print('Starting ' + args.name, flush=True)
result = subprocess.run(cmd, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
log = (out / 'unity.log').read_text(errors='replace')
errors = [line for line in log.splitlines() if 'error CS' in line]
print('C# error lines: ' + str(len(errors)), flush=True)
receipt = {'unityExit': result.returncode, 'compileErrors': errors, 'filter': args.filter}
# Compiler errors invalidate any stale/partial XML; inspect these FIRST.
if not errors and (out / 'results.xml').is_file():
    tree = ET.parse(out / 'results.xml').getroot()
    receipt['results'] = dict(tree.attrib)
    receipt['failures'] = [{'name': test.get('fullname'),
                            'message': test.findtext('failure/message')}
                           for test in tree.iter('test-case') if test.get('result') == 'Failed']
else:
    receipt['resultError'] = 'Compiler errors or missing fresh test results.'
(out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
with gzip.open(out / 'unity.log.gz', 'wb') as target:
    target.write((out / 'unity.log').read_bytes())
print(json.dumps({k: v for k, v in receipt.items() if k != 'failures'}, indent=2), flush=True)
print('Failed cases: ' + str(len(receipt.get('failures', []))), flush=True)
sys.exit(1 if errors or receipt.get('resultError') else result.returncode)
