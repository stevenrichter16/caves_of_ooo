import contextlib,io,runpy,sys,pathlib,json,hashlib
sys.path.insert(0,'/tmp/codex-ring-felling-review')
from scan_crossings import projected_crossings
source=pathlib.Path('/tmp/codex-spawn-ring-refinement/final'); baseline=pathlib.Path('/Users/steven/caves-of-ooo/ArtSource/SpawnRing3D')
catalog=json.loads((source/'catalog.json').read_text());old=json.loads(pathlib.Path('/tmp/codex-spawn-ring-art/hero-correction/catalog.json').read_text());priorHashes=json.loads(pathlib.Path('/tmp/codex-ring-felling-review/corrected-triangle-scan.json').read_text())['allModelHashes']; oldBy={m['id']:m for m in old['models']};rows=[];changes=[];caps=[]
for model in catalog['models']:
 path=source/model['path']; raw=path.read_bytes();changed=hashlib.sha256(raw).hexdigest()!=priorHashes[model['id']]
 if changed:changes.append(model['id'])
 sys.argv=['p',str(path)]
 with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path('/tmp/codex-ring-quartz-review/inspect_fbx.py')
 tri=sum(m['fanTriangles'] for m in ns['meshes']);bad=sum(m['degenerateFanTriangles'] for m in ns['meshes']); nfaces=sum(m['polygons'] for m in ns['meshes'])
 rows.append({'id':model['id'],'sha256':hashlib.sha256(raw).hexdigest(),'changed':changed,'triangles':tri,'catalogTriangles':model['triangles'],'priorCatalogTriangles':oldBy[model['id']]['triangles'],'degenerateTriangles':bad,'polygons':nfaces})
 if not changed:continue
 for n in ns['all_nodes'](ns['roots']):
  if n['name']!='Geometry' or n['props'][-1]!='Mesh':continue
  ch={c['name']:c for c in n['children']};v=ch['Vertices']['props'][0];verts=list(zip(v[::3],v[1::3],v[2::3]));face=[];idx=0
  for vi in ch['PolygonVertexIndex']['props'][0]:
   face.append(-vi-1 if vi<0 else vi)
   if vi<0:
    if len(face)>3:
     pts=[verts[k] for k in face];normals=[]
     for i in range(len(face)-2):
      for j in range(i+1,len(face)-1):
       for k in range(j+1,len(face)):
        a,b,c=pts[i],pts[j],pts[k];u=[b[d]-a[d] for d in range(3)];w=[c[d]-a[d] for d in range(3)]
        normal=(u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]);normals.append((sum(t*t for t in normal),normal,i,j,k))
     best=max(normals);first=normals[0];fan=max(n for n in normals if n[2]==0)
     drops={name:max(range(3),key=lambda k:abs(row[1][k])) for name,row in [('first3',first),('largestTriangle',best),('largestFan',fan)]}
     crossed={name:projected_crossings(pts,drop) for name,drop in drops.items()}
     normal=best[1];length=sum(t*t for t in normal)**.5
     plane=max(abs(sum((pt[d]-pts[best[2]][d])*normal[d] for d in range(3)))/length for pt in pts) if length else None
     caps.append({'id':model['id'],'polygon':idx,'vertices':len(face),'planeDeviation':plane,'crossings':crossed})
    face=[];idx+=1
expected=json.loads(pathlib.Path('/tmp/codex-spawn-ring-refinement/reports/source-delta.json').read_text())['changedSourceModels']
summary={'models':len(rows),'changed':changes,'unchanged':len(rows)-len(changes),'exactExpectedChangeSet':set(changes)==set(expected),'zeroAreaTriangles':sum(r['degenerateTriangles'] for r in rows),'countMismatches':[r['id'] for r in rows if r['triangles']!=r['catalogTriangles']],'triangleIncreases':[r['id'] for r in rows if r['triangles']>r['priorCatalogTriangles']],'sourceTriangles':sum(r['triangles'] for r in rows),'priorTriangles':sum(m['triangles'] for m in old['models']),'crossedPolygons':[r for r in caps if any(r['crossings'].values())],'catalogSha256':hashlib.sha256((source/'catalog.json').read_bytes()).hexdigest()}
pathlib.Path('/tmp/codex-ring-refinement-review/raw-topology.json').write_text(json.dumps({'summary':summary,'models':rows,'changedNonTriangles':caps},indent=2)+'\n')
print(json.dumps(summary,indent=2))
