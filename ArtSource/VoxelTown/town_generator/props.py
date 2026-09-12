"""Role-specific furniture fitted to actual voxels, with reserved activity space."""
import math,random
from .buildings import local_to_world
from .spatial import (Clearance,Reservations,make_placement,inside_room,interior_candidates,
                      placement_bounds,in_field_approach)
from .terrain import ground_height

# Preferred positions express room function; fitting uses actual asset geometry.
PROFILES={
 'residence':[('bed',-.55,.45),('table',.5,.45),('stool',.55,-.15),('crate',-.55,-.45),('lantern',.55,.70)],
 'workshop':[('workbench',-.50,.48),('furnace',.5,.48),('shelf',-.55,-.1),('crate',.55,-.15),('barrel',.55,-.52),('lantern',-.5,.70)],
 'trader':[('shelf',-.5,.5),('barrel',.5,.5),('table',.50,-.10),('crate',-.55,-.15),('banner',0,.73)],
 'farmhouse':[('bed',-.50,.48),('table',.5,.4),('barrel',-.5,-.4),('crate',.5,-.35)],
 'meeting_house':[('table',0,.5),('stool',-.6,.4),('stool',.6,.4),('banner',0,.73),('torch',-.65,-.45),('torch',.65,-.45)],
 'shrine':[('firepit',0,.35),('torch',-.60,-.3),('torch',.60,-.3),('banner',0,.75)],
 'storehouse':[('shelf',-.55,.5),('shelf',.55,.5),('barrel',-.5,0),('crate',.5,0),('crate',-.5,-.45),('barrel',.5,-.45)],
 'bathhouse':[('well',0,.4),('stool',-.6,-.3),('stool',.6,-.3),('pot',-.6,.5)],
 'animal_enclosure':[('trough',-.55,.5),('hay',.55,.4)],
 'guard_watch':[('stairs',-.45,.3),('shelf',.55,.4),('torch',.55,-.4),('banner',0,.7)]}


def plan_props(town):
 rows=[];rng=random.Random(f'{town.config.seed}:props');q=town.config.voxel_size
 clear=Clearance(town);reserved=Reservations(q)
 def row_for(asset,x,y,z=0,rotation=0,owner='town',role='functional'):
  return make_placement(asset,x,y,z,rotation,q,id=f'prop-{len(rows):04d}',owner=owner,role=role)
 def accept(row):
  rows.append(row);reserved.add(row)
 def outdoor(asset,x,y,rotation=0,owner='town',role='activity-clutter',allow_farm=False,allow_path=False):
  row=row_for(asset,x,y,ground_height(x,y,town),rotation,owner,role)
  a,b,c,d=placement_bounds(row,q);cx,cy=(a+c)/2,(b+d)/2
  radius=math.hypot(c-a,d-b)/2
  if not allow_path and not clear.free(cx,cy,radius,allow_farm=allow_farm):return False
  if allow_path and (abs(cx)+radius>town.config.town_size/2 or abs(cy)+radius>town.config.town_size/2 or clear.in_building(cx,cy,radius)):return False
  if reserved.collides(row):return False
  accept(row);return True

 # Essential infrastructure claims space before optional activity clutter.
 wx,wy=town.anchors['water']
 if not outdoor('well',wx,wy,owner='town-water',role='water-source',allow_path=True):
  raise ValueError('Primary well does not fit its reserved water gathering space')

 for b in town.buildings:
  for asset,nx,ny in PROFILES[b.role]:
   preferred=(nx*(b.width/2-1.15),ny*(b.depth/2-1.15));placed=False
   for x,y in interior_candidates(b,preferred,q):
    row=row_for(asset,x,y,q,b.rotation,b.id)
    if inside_room(row,b,q) and not reserved.collides(row):
     accept(row);placed=True;break
   if not placed:raise ValueError(f'No accessible interior placement for {asset} in {b.id}')

  if b.role=='trader':
   yard_start=len(rows)
   for sign in (-1,1):
    # A small semantic search around each shop wing finds a real trading yard.
    for along in (.5,-1,2):
     for extra in (2.25,3.25,4.25):
      x,y=local_to_world(b,(sign*(b.width/2+extra),-b.depth/2+along))
      if outdoor('market_stall',x,y,b.rotation,b.id,'trading-yard'):break
     else:continue
     break
   # Narrow side alleys may be committed to routes. Search the shop's front
   # apron and then its back yard before giving up on this essential identity.
   if len(rows)==yard_start:
    found=False
    for direction in (-1,1):
     for distance in (b.depth/2+3,b.depth/2+4.5,b.depth/2+6):
      for side in (-b.width*.5,b.width*.5,-b.width,b.width):
       x,y=local_to_world(b,(side,direction*distance))
       if outdoor('market_stall',x,y,b.rotation,b.id,'trading-yard'):
        found=True;break
      if found:break
     if found:break
    if not found:
     # Larger districts can pack both immediate wings and aprons. Their traders
     # use the nearest free district-market site, still linked by semantic owner.
     sites=[]
     for ix in range(-14,15):
      for iy in range(-14,15):
       lx,ly=ix*1.5,iy*1.5
       sites.append((lx*lx+ly*ly,lx,ly))
     for _,lx,ly in sorted(sites):
      x,y=local_to_world(b,(lx,ly))
      if outdoor('market_stall',x,y,b.rotation,b.id,'trading-yard'):
       found=True;break
    if not found:raise ValueError(f'No usable outdoor trading yard for {b.id}')

  if b.role=='farmhouse':
   farm=next((f for f in town.farms if f.building_id==b.id),None)
   if farm:
    # Equipment is reserved before the crop pass, which leaves its cells clear.
    outdoor('irrigation',farm.center[0]-farm.width/2+1,farm.center[1]+farm.depth*.25,0,b.id,'farm-water',True)
    outdoor('crate',farm.center[0]+farm.width/2-1,farm.center[1]-farm.depth/2+1,0,b.id,'harvest',True)
    # Short repeated fence pieces remain individually described in the export.
    # Gate and route gaps follow access requirements rather than decorative cuts.
    for side in (-1,1):
     for i in range(int(farm.width)):
      x=farm.center[0]+i-(int(farm.width)-1)/2;y=farm.center[1]+side*farm.depth/2
      if not in_field_approach(x,y,farm,b,.5):outdoor('fence',x,y,0,b.id,'farm-boundary',True)
     for i in range(int(farm.depth)):
      x=farm.center[0]+side*farm.width/2;y=farm.center[1]+i-(int(farm.depth)-1)/2
      if not in_field_approach(x,y,farm,b,.5):outdoor('fence',x,y,math.pi/2,b.id,'farm-boundary',True)

  for i in range(round(town.config.clutter_amount*5)):
   asset=rng.choice(['crate','barrel','pot','sign'])
   for attempt in range(8):
    lx=(-1 if i%2 else 1)*(b.width/2+1+rng.uniform(0,1.5));ly=rng.uniform(-b.depth*.4,b.depth*.4)
    x,y=local_to_world(b,(lx,ly))
    if outdoor(asset,x,y,b.rotation,b.id):break

 # Water is a functional destination; a route may meet its rim. Other dressing
 # still observes street clearance. Well bounds were reserved by layout anchors.
 for x,y in [(wx-2.5,wy+1.5),(wx+2.5,wy+1.5),(wx+2.5,wy-1.5)]:
  outdoor('pot' if x<wx else 'stool',x,y,owner='town-water',role='water-gathering')
 px,py=town.anchors['plaza']
 for dx,dy in ((0,3),(3,0),(-3,0),(0,-3),(3,3),(-3,3)):
  if outdoor('firepit',px+dx,py+dy,owner='plaza',role='communal-hearth'):break
 return rows
