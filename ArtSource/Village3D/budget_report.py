#!/usr/bin/env python3
import json,math,sys
from pathlib import Path
root=Path(sys.argv[1]);m=json.loads((root/'manifest.json').read_text());by={x['id']:x for x in m['models']}
triangles=whole=0;count=0;hidden_by_crop=[];visible=[]
for p in m['owners']+m['staticPlacements']:
 q=by[p['modelId']];whole+=q['triangles'];c=q['boundsCenter'];s=q['boundsSize'];sc=p['scale'];pos=p['position'];a=math.radians(p['rotationY']);cs=math.cos(a);sn=math.sin(a);pts=[]
 for xx in [-1,1]:
  for zz in [-1,1]:
   x=(c['x']+xx*s['x']/2)*sc['x'];z=(c['z']+zz*s['z']/2)*sc['z'];pts.append((pos['x']+cs*x+sn*z,pos['z']-sn*x+cs*z))
 pid=p.get('ownerId',p.get('id'))
 if max(x for x,z in pts)<21.25 or min(x for x,z in pts)>58.75 or max(z for x,z in pts)<0 or min(z for x,z in pts)>25:hidden_by_crop.append(pid);continue
 triangles+=q['triangles'];count+=1;visible.append(pid)
result={'status':'Static source AABB estimate only; no Unity performance claim','wholeZonePlacedTriangles':whole,'referenceCropConservativeTriangles':triangles,'referenceCropPlacements':count,'referenceCropXZ':[21.25,58.75,0,25],'excludedByCrop':hidden_by_crop,'includedByCrop':visible,'assumptions':['Counts both roofs and indoor contents even though native visibility usually hides one group.','AABB intersection conservatively counts every triangle in an intersecting model.','Does not predict renderer batching, shadow passes, FOV clipping or actual frame time.','The source crop can exceed the initial300k design target: denser rounded leaf crowns, draped cloth, legible timber details and six real planting soil cells are deliberate fidelity additions. Record the exact estimate; only native profiling can accept runtime cost.'],'attachmentModels':[{'id':a['id'],'triangles':a['triangles'],'pivot':a['pivot']} for a in m['models'] if a['kind']=='equipment']}
(root/'reports/asset-budget.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k not in ('excludedByCrop','includedByCrop','assumptions')}))
