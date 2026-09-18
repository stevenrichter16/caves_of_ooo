#!/usr/bin/env python3
"""Read-only independent source/PNG checks for the existing21 assembled GPU cases."""
import argparse,hashlib,json
from pathlib import Path
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('report',type=Path);p.add_argument('inputs',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
assert not a.output.exists(),'Use fresh immutable audit output'
r=json.loads(a.report.read_text());frozen=json.loads(a.inputs.read_text());checks=[];images=0

def check(name,value): checks.append({'name':name,'passed':bool(value)})
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
expected=frozen['ArtSource/StarterSpell3D/runtime/starter_spell_library.json']['sha256']
check('run and report source matches frozen imported art',r['status']=='PASS' and not r['error'] and len(r['cases'])==21 and r['sourceSha256']==expected and (r['width'],r['height'])==(512,512))
for c in r['cases']:
 check(c['id']+':paired',c['passed'] and not c['error'] and c['positivePixels']>0 and c['negativePixels']==0)
 rgb={}
 for f in c['frames']:
  path=Path(f['path']);data=None
  with Image.open(path) as im: im.load();valid=im.size==(512,512) and im.format=='PNG';data=im.convert('RGB').tobytes()
  rgb[f['name']]=data;images+=1
  check(c['id']+':'+f['name'],valid and path.parent.resolve()==a.report.parent.resolve() and sha(path)==f['sha256'])
 if c['id'].endswith(':color'):
  unchanged=c['maximumEmissionControlError']==0 and all(max(abs(u[k]-u['initialEmission']) for k in ('retainedEmission','freshEmission','counterEmission'))<=1e-7 for u in c['colorUploads'])
  check(c['id']+':unchanged-emission',unchanged and bool(c['colorUploads']))
  check(c['id']+':independent-linear-color',rgb['native_upload']==rgb['single_linear_reference'] and rgb['native_upload']!=rgb['double_linear_counter'] and c['meanNativeReferenceDifference']==0 and c['doubleLinearDifferentPixels']>0)
for name,want in frozen.items(): check('preserved:'+name,Path(name).is_file() and sha(name)==want['sha256'])
result={'status':'PASS' if all(c['passed'] for c in checks) else 'FAIL','runId':r['runId'],'sourceSha256':r['sourceSha256'],'reportSha256':sha(a.report),'inputManifestSha256':sha(a.inputs),'cases':len(r['cases']),'images':images,'assertions':len(checks),'checks':checks}
a.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k!='checks'},indent=2));raise SystemExit(0 if result['status']=='PASS' else 1)
