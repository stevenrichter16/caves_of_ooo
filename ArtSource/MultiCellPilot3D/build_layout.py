#!/usr/bin/env python3
"""Deterministic authored chunk contract, independent of Blender or Unity."""
from pathlib import Path
import json,random
ROOT=Path(__file__).resolve().parent
rect=lambda w,h:[(x,y) for y in range(h) for x in range(w)]
ne=[(2,0),(3,0),(1,1),(2,1),(3,1),(0,2),(1,2),(2,2),(0,3),(1,3)]
se=[(3-x,y) for x,y in ne]
FAMILIES={
 'PilotRidgeN':('GrainRidge',rect(2,4),True,True,'scenery'),
 'PilotRidgeNE':('GrainRidge',ne,True,True,'scenery'),
 'PilotRidgeSE':('GrainRidge',se,True,True,'scenery'),
 'PilotRidgeE':('GrainRidge',rect(4,2),True,True,'scenery'),
 'PilotBoulder':('Rock',rect(2,2),True,False,'scenery'),
 'PilotRock':('Rock',rect(1,1),True,False,'scenery'),
 'PilotTar':('TarSeep',rect(2,3),False,False,'hazard'),
 'PilotPipe':('CopperPipe',rect(3,1),True,False,'movable'),
 'PilotRuinWall':('TepuiWall',rect(1,1),True,True,'scenery'),
 'PilotTepuibone':('TepuiboneVein',rect(1,1),False,False,'scenery'),
 'PilotSteamVent':('SteamVent',rect(1,1),False,False,'hazard'),
 'PilotGround':('TepuiStone',rect(1,1),False,False,'ground'),
 'PilotMawToad':('MawToad',rect(2,2),True,False,'actor'),
 'PilotWardline':('Wardline',rect(1,1),True,False,'actor'),
 'PilotHermit':('CaveHermit',rect(1,1),True,False,'actor'),
}
MODELS=[]
for family,(bp,cells,solid,opaque,role) in FAMILIES.items():
 for variant in range(1 if role=='actor' else 4):
  MODELS.append(dict(id=family if role=='actor' else family+'_'+str(variant),family=family,variant=variant,blueprint=bp,footprint=[dict(x=x,y=y) for x,y in cells],cellsRaw=';'.join(f'{x},{y}' for x,y in cells),solid=solid,opaque=opaque,destructible=role not in ('actor','ground'),role=role))
lookup={m['id']:m for m in MODELS};placements=[];claimed={};reserved={(x,y) for x in (39,40,41) for y in range(25)}
def put(family,x,y,v=0,name=None):
 mid=family if FAMILIES[family][-1]=='actor' else family+'_'+str(v%4);m=lookup[mid]
 cells={(x+c['x'],y+c['y']) for c in m['footprint']}
 if any(q[0]<0 or q[0]>=80 or q[1]<0 or q[1]>=25 for q in cells):raise ValueError(('Bounds',family,x,y))
 if m['solid'] and (cells & (claimed.keys()|reserved)):
  print('Skipped overlap',family,x,y,cells & (claimed.keys()|reserved));return False
 p={k:m[k] for k in ('blueprint','footprint','cellsRaw','solid','opaque','destructible','role')};p.update(id=name or f'{family}-{x}-{y}',modelId=mid,x=x,y=y)
 placements.append(p)
 if m['solid']:
  for q in cells:claimed[q]=p['id']
 return True
for i,(x,y) in enumerate([(6,2),(3,6),(0,14),(0,20),(18,1),(15,5),(12,11),(9,15),(5,21)]):assert put('PilotRidgeNE',x,y,i)
for i,(x,y) in enumerate([(35,1),(36,5),(37,11),(37,15),(36,21),(47,8),(48,12)]):assert put('PilotRidgeN',x,y,i)
for i,(x,y) in enumerate([(47,0),(49,4),(51,8),(54,14),(57,18),(60,1),(64,5),(68,9),(72,13),(76,17)]):assert put('PilotRidgeSE',x,y,i)
for i,(x,y) in enumerate([(42,6),(67,15),(50,20),(60,23)]):assert put('PilotRidgeE',x,y,i)
# Small open eastern doorway; no decorative mesh blocks the hermit's exit.
for i,(x,y) in enumerate([(x,10) for x in range(6,11)]+[(x,14) for x in range(6,11)]+[(6,y) for y in range(11,14)]+[(10,11),(10,13)]):assert put('PilotRuinWall',x,y,i)
assert put('PilotHermit',8,12,name='ridge-hermit')
assert put('PilotMawToad',10,2,name='large-maw-toad')
assert put('PilotWardline',28,5,name='ridge-wardline')
for x,y,v in [(12,1,0),(25,19,1)]:assert put('PilotTar',x,y,v)
for x,y,v in [(67,4,0),(71,5,1),(17,12,2),(18,12,3),(19,12,0),(20,12,1),(21,12,2),(22,12,3)]:
 if x>30:assert put('PilotSteamVent',x,y,v)
 else:assert put('PilotTepuibone',x,y,v)
assert put('PilotPipe',68,3,0,name='movable-copper-pipe')
assert put('PilotPipe',70,7,2,name='buried-copper-pipe')
for i,(x,y) in enumerate([(20,2),(23,6),(26,10),(31,8),(31,14),(42,16),(44,21),(62,9),(71,20),(75,2)]):
 put('PilotBoulder',x,y,i)
# Sparse rubble clusters near ridges, plus occasional isolated stones. Every
# cluster is a native removable rock owner, never a collision-less big prop.
rng=random.Random(9102601)
anchors=[(5,3),(2,8),(4,18),(10,20),(14,17),(18,6),(23,2),(33,2),(36,9),(33,13),(37,20),(48,2),(51,6),(53,12),(57,16),(61,21),(64,4),(70,9),(73,15),(78,22)]
for i in range(155):
 a=anchors[i%len(anchors)];x=a[0]+rng.randrange(-3,4);y=a[1]+rng.randrange(-2,3)
 if not (0<=x<80 and 0<=y<25):continue
 # Ruin entrance and large creature ingress remain intentionally clean.
 if 5<=x<=11 and 10<=y<=14:continue
 if y in (9,18) or x in (39,40,41):continue
 if any(abs(x-q[0])+abs(y-q[1])<=0 for q in claimed):continue
 put('PilotRock',x,y,i)
(ROOT/'layout.json').write_text(json.dumps(dict(schemaVersion=1,zoneId='Overworld.3.7.0',width=80,height=25,coordinates='Cell y increases south; models pivot at anchor-cell centre; fixed orientation',groundModels=[f'PilotGround_{v}' for v in range(4)],placements=placements),indent=2)+'\n')
(ROOT/'model-contract.json').write_text(json.dumps(dict(schemaVersion=1,models=MODELS),indent=2)+'\n')
print(f'{len(placements)} authored owners, {len(claimed)} solid cells, {len(MODELS)} model variants')
