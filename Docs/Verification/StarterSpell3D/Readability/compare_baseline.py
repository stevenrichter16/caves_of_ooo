"""Compare a fresh full EditMode receipt with the exact prior baseline names/messages."""
import argparse,collections,hashlib,json,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path('/Users/steven/caves-of-ooo')
ap=argparse.ArgumentParser();ap.add_argument('run');ap.add_argument('--baseline',default=str(ROOT/'Docs/Verification/StarterSpell3D/Integration/S3D29-final-full'));args=ap.parse_args()
out=Path(args.run).resolve();baseline=Path(args.baseline).resolve()
def load(path):
 receipt=json.loads((path/'receipt.json').read_text())
 if receipt.get('compileErrors') or receipt.get('resultError'):raise RuntimeError('Invalid compiler/result receipt: '+str(path))
 xml=path/'results.xml';tree=ET.parse(xml).getroot();cases=list(tree.iter('test-case'));dupes=sorted(n for n,c in collections.Counter(t.get('fullname') for t in cases).items() if c>1)
 return {'xml':str(xml),'sha256':hashlib.sha256(xml.read_bytes()).hexdigest(),**{k:int(tree.get(k)) for k in ['total','passed','failed','skipped']},'compileErrorLines':len(receipt['compileErrors'])},{t.get('fullname'):t for t in cases},dupes
bs,bt,bd=load(baseline);cs,ct,cd=load(out);bf={n for n,t in bt.items() if t.get('result')=='Failed'};cf={n for n,t in ct.items() if t.get('result')=='Failed'}
messages=[{'fullname':n,'baselineMessage':bt[n].findtext('failure/message'),'currentMessage':ct[n].findtext('failure/message'),'exactlyEqual':bt[n].findtext('failure/message')==ct[n].findtext('failure/message')} for n in sorted(bf&cf)];added=sorted(set(ct)-set(bt));removed=sorted(set(bt)-set(ct))
p={'status':'PASS' if bf==cf and all(t['exactlyEqual'] for t in messages) and not(bd or cd or removed) and all(ct[n].get('result')=='Passed' for n in added) else 'DIFFERENCES','baseline':bs,'current':cs,'newFailures':sorted(cf-bf),'resolvedBaselineFailures':sorted(bf-cf),'persistentFailureNames':sorted(bf&cf),'messagesExactlyEqual':all(t['exactlyEqual'] for t in messages),'persistentFailures':messages,'addedTests':added,'addedAllPass':all(ct[n].get('result')=='Passed' for n in added),'removedTests':removed,'duplicateFullnames':{'baseline':bd,'current':cd},'scope':'Exact NUnit fullname and unnormalized message comparison. Known baseline failures remain failures, not ignored or converted to passes.'}
with (out/'baseline-comparison.json').open('x') as f:json.dump(p,f,indent=2);f.write('\n')
md=['# Full-suite baseline comparison','',f'**{p["status"]}** — prior **{bs["passed"]:,}/{bs["total"]:,}**; current **{cs["passed"]:,}/{cs["total"]:,}**.','',f'Persistent failures: {len(bf&cf)}. New failures: {len(cf-bf)}. Resolved baseline failures: {len(bf-cf)}. Exact messages match: {p["messagesExactlyEqual"]}.','',f'Added cases: {len(added)}; all pass: {p["addedAllPass"]}. Removed cases: {len(removed)}. Skips: {cs["skipped"]}. Compile errors: {cs["compileErrorLines"]}.','', 'Exact fullnames/messages and both XML SHA-256 values are retained in `baseline-comparison.json`. Existing failures remain explicit.','']
with (out/'baseline-comparison.md').open('x') as f:f.write('\n'.join(md))
print(json.dumps({k:p[k] for k in ['status','baseline','current','newFailures','resolvedBaselineFailures','messagesExactlyEqual','addedTests','removedTests']},indent=2))
