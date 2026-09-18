"""Fresh explicit native-spell art import; refuses every existing Unity Editor."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path('/Users/steven/caves-of-ooo')
BASE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'Tools/Village3D'))
from run_common import restart_mcp, refuse_unity

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

parser = argparse.ArgumentParser()
parser.add_argument('name')
parser.add_argument('--candidate', required=True)
args = parser.parse_args()
out = BASE / args.name
out.mkdir()
refuse_unity()
candidate_path = Path(args.candidate).resolve()
candidate = json.loads(candidate_path.read_text())
checks = {name: {'expected': value, 'actual': digest(ROOT/name)} for name,value in candidate['files'].items()}
verified = all(v['expected'] == v['actual'] for v in checks.values())
(out/'source-before.json').write_text(json.dumps({'candidatePath':str(candidate_path),'candidateSha256':digest(candidate_path),'passed':verified,'files':checks},indent=2)+'\n')
if not verified:
    raise RuntimeError('Frozen import candidate hashes no longer match; import refused.')
(out/'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),args.name),indent=2)+'\n')
cmd = ['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity',
       '-batchmode','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.NativeSpellFxAssetBuilder.RunFromCommandLine',
       '-spellFxReport',str(out/'import.json'),'-logFile',str(out/'unity.log'),'-quit']
print('Starting explicit import '+args.name,flush=True)
result = subprocess.run(cmd,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
log = (out/'unity.log').read_text(errors='replace')
errors = [line for line in log.splitlines() if 'error CS' in line]
print('C# error lines: '+str(len(errors)),flush=True)
receipt = {'unityExit':result.returncode,'compileErrors':errors,'kind':'import','passed':False}
if not errors and (out/'import.json').is_file():
    report = json.loads((out/'import.json').read_text())
    after = {p: digest(ROOT/p) for p in candidate['files']}
    same = all(after[p]==v for p,v in candidate['files'].items())
    receipt.update(reportPath=str(out/'import.json'),status=report['status'],sourcePreserved=same,
                   meshes=report['meshes'],triangles=report['triangles'],pieces=report['pieces'],error=report.get('error'))
    receipt['passed'] = report['status']=='PASS' and result.returncode==0 and same and report['sourceBlendSha256']==candidate['sourceBlendSha256'] and report['runtimeJsonSha256']==candidate['files']['ArtSource/StarterSpell3D/runtime/starter_spell_library.json']
    (out/'source-after.json').write_text(json.dumps(after,indent=2)+'\n')
else:
    receipt['error']='Compiler errors or missing fresh import report.'
(out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
with gzip.open(out/'unity.log.gz','wb') as stream:stream.write((out/'unity.log').read_bytes())
print(json.dumps(receipt,indent=2),flush=True)
sys.exit(0 if receipt['passed'] else 1)
