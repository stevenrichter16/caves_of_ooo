"""Independent source mesh audit, including triangle interiors and winding."""
import bpy,json,sys
from pathlib import Path
from collections import defaultdict
root=Path(__file__).resolve().parent;catalog=json.loads((root/'catalog.json').read_text());bpy.ops.wm.open_mainfile(filepath=str(root/'pilot_kit.blend'));rows=[]
for m in catalog['models']:
 outside=0;wind=0;samples=0;zero=0;coll=bpy.data.collections.get(m['id']);cells=[(c['x'],c['y']) for c in m['footprint']]
 def inside(p):return any(abs(p.x-x)<=.50005 and abs(p.y+y)<=.50005 for x,y in cells)
 for ob in coll.objects:
  if ob.type!='MESH':continue
  mesh=ob.data;edgeOrders=defaultdict(list)
  for poly in mesh.polygons:
   ids=list(poly.vertices)
   for a,b in zip(ids,ids[1:]+ids[:1]):edgeOrders[tuple(sorted((a,b)))].append((a,b))
  wind+=sum(1 for pair in edgeOrders.values() if len(pair)==2 and pair[0]==pair[1])
  mesh.calc_loop_triangles()
  for t in mesh.loop_triangles:
   a,b,c=[ob.matrix_basis@mesh.vertices[i].co for i in t.vertices]
   zero+=(b-a).cross(c-a).length<1e-10
   for u,v in [(1/3,1/3),(.1,.1),(.1,.8),(.8,.1),(.5,.05),(.05,.5),(.45,.5)]:
    p=a*u+b*v+c*(1-u-v);samples+=1;outside+=not inside(p)
 rows.append(dict(id=m['id'],triangleSamples=samples,outsideTriangleSamples=outside,inconsistentWindingEdges=wind,zeroAreaTriangles=zero))
failed=[r for r in rows if r['outsideTriangleSamples'] or r['inconsistentWindingEdges'] or r['zeroAreaTriangles']]
(root/'reports/source-interior-winding.json').write_text(json.dumps(dict(passed=not failed,failures=failed,models=rows),indent=2)+'\n');print('SOURCE_AUDIT',len(failed),failed)
sys.exit(1 if failed else 0)
