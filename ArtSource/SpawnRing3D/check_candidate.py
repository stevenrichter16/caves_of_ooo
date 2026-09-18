import json,hashlib,sys
from pathlib import Path
base=Path('/tmp/codex-spawn-ring-art/final');candidate=Path(sys.argv[1]);out=Path(sys.argv[2]);bc=json.loads((base/'catalog.json').read_text());cc=json.loads((candidate/'catalog.json').read_text());bm={m['id']:m for m in bc['models']};cm={m['id']:m for m in cc['models']}
assert set(bm)==set(cm)
assert {k:v for k,v in bc.items() if k!='models'}=={k:v for k,v in cc.items() if k!='models'}
rows=[]
for mid,m in cm.items():
 assert m['triangles']<=bm[mid]['triangles'],mid
 if bm[mid]!=m:rows.append({'id':mid,'beforeTriangles':bm[mid]['triangles'],'afterTriangles':m['triangles'],'changedFields':[k for k in m if m[k]!=bm[mid][k]]})
zones=[]
for zid in bc['zones']:
 a=json.loads((base/'scenes'/zid/'manifest.json').read_text());b=json.loads((candidate/'scenes'/zid/'manifest.json').read_text());assert a['placements']==b['placements'],zid
 assert {k:v for k,v in a.items() if k!='placedTriangles'}=={k:v for k,v in b.items() if k!='placedTriangles'},zid
 assert b['placedTriangles']<=a['placedTriangles']
 zones.append({'id':zid,'placementsIdentical':True,'placementCount':len(b['placements']),'beforeTriangles':a['placedTriangles'],'afterTriangles':b['placedTriangles']})
out.write_text(json.dumps({'baselineCatalogSha256':hashlib.sha256((base/'catalog.json').read_bytes()).hexdigest(),'candidateCatalogSha256':hashlib.sha256((candidate/'catalog.json').read_bytes()).hexdigest(),'modelIdsAndNonModelCatalogFieldsIdentical':True,'allNativeSceneManifestFieldsExceptTriangleTotalsIdentical':True,'beforeUniqueTriangles':sum(m['triangles'] for m in bm.values()),'afterUniqueTriangles':sum(m['triangles'] for m in cm.values()),'modelChanges':rows,'zones':zones},indent=2)+'\n')
print('PASS all218 model ceilings, exact catalog contracts, all8 native scene placements and other manifest fields')
