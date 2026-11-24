from pathlib import Path
import json,subprocess,hashlib,tarfile,shutil
r=Path('/Users/steven/caves-of-ooo');b=Path('/private/tmp/coo-mac-152b2cdcf-20261124-export');prior=r/'Docs/Verification/MachineSystems/MacCandidate9A0D55F93'
for n in ['verify-committed-source.py','verify-inputs.py']:
 subprocess.run(['python3',str(b/n)],check=True)
prep=json.loads((b/'preparation.json').read_text());project=Path(prep['copy']);post=json.loads((b/'post-build-input-pins.json').read_text());delta=json.loads((b/'input-deltas.json').read_text());names=set(delta['changed'])|set(delta['added']);seen=set()
with tarfile.open(b/'effective-input-deltas.tar.gz') as t:
 for m in t:
  assert m.isfile() and m.name in names and m.name not in seen
  body=t.extractfile(m).read();assert hashlib.sha256(body).hexdigest()==post[m.name];seen.add(m.name)
assert seen==names
(b/'overlay-verification.json').write_text(json.dumps({'exact':True,'changedOrAddedBodies':len(names),'effectiveSourcePins':len(post),'missing':0},indent=2)+'\n')
current=json.loads((b/'build-report.json').read_text());previous=json.loads((prior/'build-report.json').read_text());warnings=lambda j:sorted(x['content'] for x in j['messages'] if x['type']=='Warning')
assert warnings(current)==warnings(previous)
(b/'warning-comparison.json').write_text(json.dumps({'prior':'MacCandidate9A0D55F93','sameFiveWarnings':True,'warnings':warnings(current)},indent=2)+'\n')
args=json.loads((prior/'connections-arguments.json').read_text());oldapp=args[2];args=[x.replace('9a0d55f93-20261124','152b2cdcf-20261124') if i<3 else x for i,x in enumerate(args)];args.append(oldapp)
(b/'connections-arguments.json').write_text(json.dumps(args,indent=2)+'\n')
result=subprocess.run(args,capture_output=True,text=True);(b/'connections-verification.txt').write_text(result.stdout+result.stderr)
print((result.stdout+result.stderr)[-1600:]);assert result.returncode==0
print(json.dumps({k:v for k,v in current.items() if k!='messages'}))
