from pathlib import Path
import hashlib,json,sys
root=Path('/Users/steven/caves-of-ooo'); here=Path(__file__).resolve().parent
assert len(sys.argv)==3,'usage: record-generated.py actual-import.json output-manifest.json'
report=json.loads(Path(sys.argv[1]).read_text()); assert report['status']=='success' and report['models']==1,report
h=lambda f:hashlib.sha256(Path(f).read_bytes()).hexdigest()
borrowed=json.loads((here/'borrowed-before.json').read_text())
changed=[row['path'] for row in borrowed['files'] if not (root/row['path']).is_file() or h(root/row['path'])!=row['sha256']]
assert not changed,('borrowed inputs changed',changed)
expected=json.loads((here/'expected-generated-files.json').read_text())['files']; rows=[]
for row in expected:
 p=root/row['path']; assert p.is_file(),('missing owned output',row['path'])
 rows.append(dict(path=row['path'],sha256=h(p),beforeSha256=row['beforeSha256']))
allowed={row['path'] for row in rows}
actual={str(p.relative_to(root))for folder in ['Assets/Art3D/SpreadCooking','Assets/Resources/SpreadCooking3D']for p in (root/folder).rglob('*')if p.is_file()}
assert actual<=allowed,('unexpected output',sorted(actual-allowed))
assert set(report['assets'])=={x for x in allowed if not x.endswith('.meta')},('report output set',report['assets'])
output=Path(sys.argv[2]);output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(dict(scope='Exact successful one-form prepared-grain import; no foreign assets adopted',report=str(Path(sys.argv[1]).resolve()),reportSha256=h(sys.argv[1]),model='spread-toasted-emberwheat',borrowedCount=len(borrowed['files']),borrowedUnchanged=True,files=rows),indent=2)+'\n')
print(str(output),h(output),len(rows))
