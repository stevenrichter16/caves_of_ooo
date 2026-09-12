"""Residents and livestock occupy usable reserved space, including crowd overflow."""
import math
from .buildings import local_to_world,entrance_world
from .spatial import (Clearance,Reservations,make_placement,inside_room,
                      interior_candidates,placement_bounds)
from .terrain import ground_height
from .props import plan_props


def plan_population(town):
 q=town.config.voxel_size;rows=[];clear=Clearance(town)
 # Derive prerequisites from immutable logical town data, never caller-populated
 # town.props/npcs lists. Repeated calls/stage order produce the same result.
 reserved=Reservations(q,plan_props(town))

 def candidate(asset,identifier,b,x,y,z):
  return make_placement(asset,x,y,z,b.rotation,q,id=identifier,home=b.id,
                        role='livestock' if asset.startswith('livestock') else b.role)

 def exterior_points(b):
  # Front is always local -Y, regardless of quarter-turn building rotation.
  for depth in range(2,13):
   for side in (0,-1,1,-2,2,-3,3,-4,4):
    yield local_to_world(b,(side*1.5,-b.depth/2-depth*1.25))
  # At large populations, allocate otherwise free town space nearest this home.
  half=town.config.town_size/2-1.5;points=[]
  for iy in range(math.ceil(-half/1.5),math.floor(half/1.5)+1):
   for ix in range(math.ceil(-half/1.5),math.floor(half/1.5)+1):
    x,y=ix*1.5,iy*1.5
    points.append(((x-b.position[0])**2+(y-b.position[1])**2,x,y))
  for _,x,y in sorted(points):yield x,y

 activity_cache={}
 def activity_points(b,asset):
  key=(b.id,asset)
  if key in activity_cache:return activity_cache[key]
  entry=entrance_world(b);points=[]
  # The route belongs to this destination if its actual doorway is an endpoint.
  # Side positions lie beyond the traveled width, not on the path centerline.
  useful=town.anchors['water'] if b.role=='residence' else b.position
  if b.role=='farmhouse':
   field=next((f for f in town.farms if f.building_id==b.id),None)
   if field:useful=field.center
  probe=candidate(asset,'probe',b,0,0,0)
  a,c,d,e=placement_bounds(probe,q);radius=math.hypot(d-a,e-c)/2
  for path in town.paths:
   if not any(math.dist(entry,p)<1e-6 for p in (path.points[0],path.points[-1])):continue
   for a,end in zip(path.points,path.points[1:]):
    dx,dy=end[0]-a[0],end[1]-a[1];length=math.hypot(dx,dy)
    if length<1e-6:continue
    for fraction in (.2,.5,.8):
     px,py=a[0]+dx*fraction,a[1]+dy*fraction
     if math.dist((px,py),b.position)>max(12,town.config.town_size*.2):continue
     for extra in (.5,1.0,1.5):
      offset=path.width/2+radius+extra
      for sign in (-1,1):
       x,y=px-sign*dy/length*offset,py+sign*dx/length*offset
       score=math.dist((x,y),b.position)*.65+math.dist((x,y),useful)*.35
       points.append((score,x,y))
  result=[(x,y) for _,x,y in sorted(points)]
  activity_cache[key]=result
  return result

 def try_outdoor(asset,identifier,b,points):
  for x,y in points:
   row=candidate(asset,identifier,b,x,y,ground_height(x,y,town))
   a,c,d,e=placement_bounds(row,q);cx,cy=(a+d)/2,(c+e)/2
   radius=math.hypot(d-a,e-c)/2
   if clear.free(cx,cy,radius) and not reserved.collides(row):
    row['activity']='outdoor'
    row['activity_context']={'residence':'water-errand','trader':'market-work','workshop':'workyard','farmhouse':'field-work'}.get(b.role,'courtyard')
    reserved.add(row);rows.append(row);return True
  return False

 def place(asset,identifier,b,preferred,prefer_outdoors=False):
  livestock=asset.startswith('livestock')
  if prefer_outdoors and try_outdoor(asset,identifier,b,activity_points(b,asset)):return
  for x,y in interior_candidates(b,preferred,q):
   row=candidate(asset,identifier,b,x,y,q)
   if inside_room(row,b,q) and not reserved.collides(row):
    row['activity']='enclosure' if livestock else 'indoors'
    reserved.add(row);rows.append(row);return
  if not livestock and try_outdoor(asset,identifier,b,exterior_points(b)):return
  raise ValueError(f'No accessible population placement for {identifier}; reduce population or enlarge town_size')

 human_index=0
 for b in town.buildings:
  for i,identifier in enumerate(b.occupants):
   asset=('npc','npc_1','npc_2')[human_index%3]
   # One member of each trio takes an outdoor activity. Rotating the selected
   # trio member avoids making every outdoor resident the same clothing variant.
   prefer_outdoors=human_index%3==(human_index//3)%3
   human_index+=1
   preferred=((-1 if i%2 else 1)*b.width*.20,b.depth*.10)
   place(asset,identifier,b,preferred,prefer_outdoors)
  if b.role=='animal_enclosure':
   for i in range(2):
    place('livestock',b.id+'-animal-'+str(i),b,((-1 if i else 1)*1.5,-.5))
 return rows
