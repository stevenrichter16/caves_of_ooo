from pathlib import Path
import json,hashlib,subprocess,tarfile
b=Path('/private/tmp/coo-mac-238096389-20261124-export');repo=Path('/Users/steven/caves-of-ooo');prep=json.loads((b/'preparation.json').read_text());commit=prep['commit']
assert subprocess.check_output(['git','rev-parse','--show-object-format'],cwd=repo,text=True).strip()=='sha1'
records=subprocess.check_output(['git','ls-tree','-rz',commit,'--','Assets','Packages','ProjectSettings','ArtSource'],cwd=repo)
git={}
for line in records.split(b'\0'):
 if not line:continue
 head,path=line.split(b'\t',1);mode,kind,oid=head.split();assert kind==b'blob' and mode in [b'100644',b'100755'];git[path.decode()]=oid.decode()
pins=json.loads((b/'source-pins.json').read_text());assert set(git)==set(pins)
seen=set()
with tarfile.open(b/'input.tar') as archive:
 for member in archive:
  if not member.isfile():continue
  data=archive.extractfile(member).read();name=member.name
  assert hashlib.sha256(data).hexdigest()==pins[name]
  assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==git[name]
  seen.add(name)
assert seen==set(pins)
r={'commit':commit,'gitBlobAndSHA256Verified':len(seen),'sourcePins':len(pins),'exact':True};(b/'git-source-proof.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
