#!/usr/bin/env python3
"""Offline asset contract. Deliberately fails before authored artifacts exist."""
import json, pathlib, collections, sys
root=pathlib.Path(__file__).resolve().parent
errors=[]
def require(value,message):
 if not value: errors.append(message)
def read(name):
 p=root/name
 if not p.exists():errors.append('Missing '+name);return {}
 return json.loads(p.read_text())
layout=read('layout.json');catalog=read('catalog.json');geometry=read('reports/geometry.json')
require(layout.get('zoneId')=='Overworld.3.7.0','Must be directly south of spawn')
require((layout.get('width'),layout.get('height'))==(80,25),'Native dimensions must be 80×25')
models={m['id']:m for m in catalog.get('models',[])};ids=set();blocked={}
for p in layout.get('placements',[]):
 require(p['id'] not in ids,'Repeated owner '+p['id']);ids.add(p['id'])
 require(p['modelId'] in models,'Missing model '+p['modelId'])
 cells=[(c['x'],c['y']) for c in p['footprint']]
 require(len(cells)==len(set(cells)) and cells,'Empty/duplicate footprint '+p['id'])
 require(p['cellsRaw']==';'.join(f'{x},{y}' for x,y in cells),'CellsRaw drift '+p['id'])
 for dx,dy in cells:
  x,y=p['x']+dx,p['y']+dy
  require(0<=x<80 and 0<=y<25,'Out-of-zone '+p['id'])
  if p['solid']:
   require((x,y) not in blocked,'Overlap '+p['id']+' with '+blocked.get((x,y),''))
   blocked[(x,y)]=p['id']
   require(x not in (39,40,41),'North/south portal corridor blocked')
 require(p.get('destructible') or p['role'] in ('actor','ground'),'Scenery without destruction '+p['id'])
for mid,m in models.items():
 require((root/m['path']).exists(),'Missing FBX '+mid)
 require(m.get('triangles',0)>0,'No topology '+mid)
 require(m.get('outsideFootprintVertices',1)==0,'Mesh exceeds gameplay footprint '+mid)
 require(m.get('zeroAreaTriangles',1)==0,'Degenerate topology '+mid)
 require(m.get('footprint'),'Missing model footprint '+mid)
require(geometry.get('modelCount')==len(models) and bool(models),'Geometry report incomplete')
# Connectivity and counter-check: every ordinary free cell is reachable, while
# deliberately excluded solid cells are not returned by the same flood.
seen={(40,0)};todo=[(40,0)]
while todo:
 x,y=todo.pop()
 for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
  if 0<=q[0]<80 and 0<=q[1]<25 and q not in blocked and q not in seen:seen.add(q);todo.append(q)
require(len(seen)==2000-len(blocked),'Disconnected open cells')
require(not seen.intersection(blocked),'Flood crosses solid cells')
print(json.dumps({'passed':not errors,'errors':errors,'models':len(models),'placements':len(ids),'solidCells':len(blocked),'connectedFreeCells':len(seen)},indent=2))
sys.exit(1 if errors else 0)
