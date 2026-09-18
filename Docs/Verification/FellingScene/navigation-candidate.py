import json
from collections import deque
from pathlib import Path
root=Path(__file__).resolve().parents[3]
l=json.load(open(root/'ArtSource/FellingSite/layout.json'))
m=json.load(open(root/'ArtSource/FellingSite/Components/build/manifest.json'))
cliff=[[300,0],[1536,0],[1536,330],[1460,359],[1430,347],[1380,380],[1280,340],[1240,273],[1140,270],[1100,370],[1000,410],[950,437],[820,417],[766,438],[680,416],[628,428],[587,399],[544,341],[478,300],[405,260],[355,210]]
water=[[30,250],[95,300],[115,370],[140,440],[75,480],[47,550],[65,650],[48,680],[56,750],[90,820],[117,879],[145,950],[197,1024],[288,1024],[260,968],[248,920],[210,891],[196,826],[168,804],[130,750],[126,682],[131,635],[145,570],[157,525],[201,486],[204,436],[173,415],[172,380],[148,352],[144,320],[102,293],[89,254]]
polys=[dict(b) for b in l['blockers']]
polys[0]['polygon']=cliff
polys[1]['polygon']=water
polys.append({'id':'northwest-cliff','kind':'cliff','blocksSight':True,'polygon':[[96,0],[302,0],[355,210],[405,260],[478,300],[544,341],[537,393],[502,433],[448,443],[397,378],[348,324],[280,282],[202,276],[126,252]],'note':'Fixed left cliff shoulder; west ledge remains below its toe.'})
polys.append({'id':'northeast-root-toe','kind':'root','blocksSight':False,'polygon':[[1215,343],[1255,342],[1284,414],[1315,459],[1385,537],[1374,570],[1323,552],[1244,464]],'note':'Low eastern outer-root footprint, independent of the tall visual silhouette.'})
polys.append({'id':'east-root-branch','kind':'root','blocksSight':False,'polygon':[[1139,539],[1189,563],[1256,624],[1311,660],[1302,693],[1258,683],[1193,642],[1151,610]],'note':'Low root branch extends east from the main buttress.'})

def inside(x,y,p):
 c=False
 for i,(xi,yi) in enumerate(p):
  xj,yj=p[i-1]
  if (yi>y)!=(yj>y) and x<(xj-xi)*(y-yi)/(yj-yi)+xi:c=not c
 return c
obstacles={b['id']:{(x,y) for x in range(16,64) for y in range(25) if inside((x-16+.5)*32,224+(y+.5)*32,b['polygon'])} for b in polys}
solid=set().union(*obstacles.values())
explicitBlocked={(48,5)}
explicitWalkable={tuple(c['footprintCells'][0]) for c in m['components'] if c['mutable'] and tuple(c['footprintCells'][0]) in solid}
solid=(solid|explicitBlocked)-explicitWalkable
rock={tuple(c['footprintCells'][0]) for c in m['components'] if c['mutable'] and c['kind'] in ('loose-boulder','loose-rock-cluster','standing-bank-stone')}
# Plants do not block movement; rocks have physical single-cell footprints.
open_cells={(x,y) for x in range(80) for y in range(25)}-solid-rock
D=[(-1,0),(1,0),(0,-1),(0,1)]
seen={tuple(l['spawn'])};q=deque(seen)
while q:
 p=q.popleft()
 for dx,dy in D:
  n=p[0]+dx,p[1]+dy
  if n in open_cells and n not in seen:seen.add(n);q.append(n)
issues=[]
for c in m['components']:
 if c['mutable']:
  x,y=c['footprintCells'][0]
  adj=[(x+dx,y+dy) for dx,dy in D if (x+dx,y+dy) in seen]
  if not adj:issues.append((c['id'],(x,y),[k for k,v in obstacles.items() if (x,y) in v]))
print('counts', {k:len(v) for k,v in obstacles.items()}, 'rocks',len(rock),'total blocked',len(solid|rock),'open',len(open_cells),'reachable',len(seen),'artReach',sum(16<=x<64 for x,y in seen))
print('unreachable mutable',issues)
print('landmarks',[(s['id'],tuple(s['cell']) in seen) for s in l['sterilePositions']+[l['seventh']]])
print('edge cells',{'north':sum(y==0 for x,y in seen),'south':sum(y==24 for x,y in seen),'west':sum(x==0 for x,y in seen),'east':sum(x==79 for x,y in seen)})
print('kinds',sorted(set(c['kind'] for c in m['components'] if c['mutable'])))
if __name__=='__main__':
 (Path(__file__).parent/'navigation-candidate.json').write_text(json.dumps({'schemaVersion':1,'coordinateConvention':'source top-left pixels; grid x=floor(px/32)+16, y=floor((py-224)/32)','polygons':polys,'blockedCells':sorted([list(p) for p in solid]),'walkableCellCount':len(open_cells),'reachableCellCount':len(seen),'unreachableMutableAnchors':issues},indent=2)+'\n')
