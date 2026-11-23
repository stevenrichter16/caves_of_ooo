#!/bin/bash
# Uses the repository's canonical idle-clone sync and native runner.
. /Users/steven/caves-of-ooo/Tools/CloneVerification/env.sh
run_name=machine_full_a9de3d3d9_20261122
audit_root=/private/tmp/coo-full-a9de3d3d9-20261122
[ ! -e "$COO_RESULTS/$run_name.xml" ] && [ ! -e "$COO_RESULTS/$run_name.log" ] && [ ! -e "$audit_root" ] || exit 8
clone_path=$(coo_need_clone clone2) || exit 4
mkdir "$audit_root" || exit 8
coo_ensure_mcp
coo_sync "$clone_path" || exit 9
python3 - <<'PY'
from pathlib import Path
import subprocess,hashlib,json,datetime
repo=Path('/Users/steven/caves-of-ooo');clone=Path('/Users/steven/coo-verification/clone2');out=Path('/private/tmp/coo-full-a9de3d3d9-20261122');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip();assert head=='7602dc783dcde98d153638f2ecb83abe13aa61dd'
corrected=json.loads((repo/'Docs/Verification/MachineSystems/WellFlasks20261122/source-pins.json').read_text());assert all(sha(repo/k)==v and sha(clone/k)==v for k,v in corrected.items())
code={}
for p in sorted((repo/'Assets').rglob('*')):
 if p.is_file() and p.suffix in {'.cs','.asmdef','.asmref'}:
  rel=str(p.relative_to(repo));assert (clone/rel).is_file() and sha(p)==sha(clone/rel),rel;code[rel]=sha(p)
for k in ['Assets/Resources/Content/Blueprints/Objects.json','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectSettings.asset']:
 assert sha(repo/k)==sha(clone/k),k;code[k]=sha(repo/k)
(out/'code-test-config-pins.json').write_text(json.dumps(code,indent=2)+'\n')
baseline={s:sha(Path(s)) for s in ['/Users/steven/coo-verification/results/fullrun_h.xml','/Users/steven/coo-verification/baselines/full_s9.xml']}
record={'status':'running','startedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'documentationHead':head,'runtimeCommit':'a9de3d3d9de9c21a917748c0d74c387b17ab7b3d','clone':str(clone),'name':'machine_full_a9de3d3d9_20261122','filter':'CavesOfOoo','codeTestConfigPins':len(code),'sixCorrectedPinsMatch':True,'workingTreePreservesForeignChanges':True,'notCleanTreeExport':True,'baselinePins':baseline,'noConcurrentNativePlayOrBatch':True,'results':'/Users/steven/coo-verification/results/machine_full_a9de3d3d9_20261122.xml','log':'/Users/steven/coo-verification/results/machine_full_a9de3d3d9_20261122.log'}
(out/'progress.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record),flush=True)
PY
[ $? -eq 0 ] || exit 10
coo_unity_run "$clone_path" CavesOfOoo "$run_name"
python3 - <<'PY'
from pathlib import Path
import json,datetime,xml.etree.ElementTree as ET
p=Path('/private/tmp/coo-full-a9de3d3d9-20261122/progress.json');r=json.loads(p.read_text());x=Path(r['results']);log=Path(r['log']);r['finishedUTC']=datetime.datetime.now(datetime.timezone.utc).isoformat();r['compilerErrorLines']=log.read_text(errors='replace').count('error CS') if log.exists() else None
if x.exists():
 root=ET.parse(x).getroot();r['status']='completed_requires_assessment';r['counts']={k:root.get(k) for k in ['total','passed','failed','skipped','inconclusive']}
else:r['status']='failed_no_result_xml'
p.write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r),flush=True)
PY
