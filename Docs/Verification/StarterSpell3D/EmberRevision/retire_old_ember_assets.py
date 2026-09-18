#!/usr/bin/env python3
"""Archive/remove only E00-identical, Ember-exclusive mesh assets absent final export.
No wildcard approval, shared assets, unknown files, or referenced assets are removed.
"""
import argparse, collections, hashlib, json, re, shutil, subprocess, sys
from pathlib import Path
ROOT=Path('/Users/steven/caves-of-ooo');BASE=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('out');parser.add_argument('--apply',action='store_true');args=parser.parse_args()
out=BASE/args.out;out.mkdir()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def dump(path,data):path.write_text(json.dumps(data,indent=2,sort_keys=True)+'\n')
baseline=json.loads((BASE/'E00-prechange/source-hashes-before.json').read_text())
old=collections.defaultdict(set)
for file in (BASE/'E00-prechange/normalized-spell-exports').glob('*.json'):
 data=json.loads(file.read_text())
 for piece in data['study']['pieces']:old[piece['meshId']].add(data['study']['id'])
source=ROOT/'ArtSource/StarterSpell3D/runtime/starter_spell_library.json';source_sha=sha(source);current=json.loads(source.read_text());final={m['id'] for m in current['meshes']}
retired=[];shared=[]
for mid,owners in sorted(old.items()):
 if 'ember_spit' not in owners or mid in final:continue
 if owners!={'ember_spit'}:shared.append({'id':mid,'owners':sorted(owners)});continue
 assert re.fullmatch('[A-Za-z0-9_-]+',mid),mid
 asset=Path('Assets/Art3D/SpellFx/Meshes')/(mid+'.asset');files=[]
 for name in [asset,Path(str(asset)+'.meta')]:
  rel=name.as_posix();assert rel in baseline,('absent E00',rel)
  assert sha(ROOT/name)==baseline[rel]['sha256'],('changed since E00',rel)
  files.append({'path':rel,'sha256':sha(ROOT/name),'bytes':(ROOT/name).stat().st_size})
 guid=re.search(r'^guid: ([a-f0-9]{32})$',(ROOT/Path(str(asset)+'.meta')).read_text(),re.M)
 assert guid,asset
 retired.append({'id':mid,'owners':['ember_spit'],'guid':guid[1],'files':files})
references={}
for row in retired:
 p=subprocess.run(['rg','-l','-F',row['guid'],'Assets','-g','!*.meta'],cwd=ROOT,text=True,capture_output=True)
 assert p.returncode in [0,1],p.stderr
 found=p.stdout.splitlines()
 # The prior library reference is expected in a read-only pre-import plan only.
 unexpected=[x for x in found if args.apply or x!='Assets/Resources/SpellFx3D/Library.asset']
 if unexpected:references[row['id']]=unexpected
report={'stage':'archive-and-remove' if args.apply else 'read-only-plan','runtimeExportSha256':source_sha,'retiredMeshCount':len(retired),'retiredFileCount':2*len(retired),'sharedRetained':shared,'referencesBlockingRemoval':references,'assets':retired,'status':'BLOCKED' if references else 'READY'}
dump(out/'receipt.json',report)
assert not references,references
if args.apply:
 sys.path.insert(0,str(ROOT/'Tools/Village3D'));from run_common import refuse_unity
 refuse_unity()
 assert sha(source)==source_sha,'Export changed during plan'
 for row in retired:
  for f in row['files']:
   src=ROOT/f['path'];dst=out/'archive'/f['path'];dst.parent.mkdir(parents=True,exist_ok=True)
   shutil.copyfile(src,dst);assert sha(dst)==f['sha256'] and sha(src)==f['sha256']
 for row in retired:
  for f in row['files']:
   src=ROOT/f['path'];assert sha(src)==f['sha256'];src.unlink()
 report['status']='PASS';report['archivedByteExact']=True;report['removedAll']=all(not(ROOT/f['path']).exists() for row in retired for f in row['files']);dump(out/'receipt.json',report)
print(json.dumps({k:report[k] for k in ['stage','status','retiredMeshCount','retiredFileCount','sharedRetained','referencesBlockingRemoval']},indent=2))
