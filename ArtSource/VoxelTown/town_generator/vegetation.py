"""Influence-led vegetation with separate, reserved agricultural activity space."""
import random,math
from .terrain import sample_fields,in_water,ground_height,water_distance
from .spatial import Clearance,Reservations,make_placement,placement_bounds,in_field_approach
from .props import plan_props
from .population import plan_population
from .buildings import local_to_world


def plan_vegetation(town):
 c=town.config;rng=random.Random(f'{c.seed}:vegetation');q=c.voxel_size
 clear=Clearance(town);rows=[]
 reserved=Reservations(q,plan_props(town)+plan_population(town))
 def put(asset,x,y,role):
  row=make_placement(asset,x,y,ground_height(x,y,town),rng.randrange(4)*math.pi/2,q,
                     id=f'vegetation-{len(rows):05d}',role=role)
  a,b,d,e=placement_bounds(row,q);cx,cy=(a+d)/2,(b+e)/2;radius=math.hypot(d-a,e-b)/2
  half=c.town_size/2
  if a < -half or b < -half or d > half or e > half:return False
  if clear.in_building(cx,cy,radius) or clear.near_path(cx,cy,radius):return False
  if role!='cultivated' and clear.in_farm(cx,cy,radius):return False
  if role!='wetland' and any(water_distance(cx,cy,w)<radius for w in town.water_bodies):return False
  if reserved.collides(row):return False
  rows.append(row);reserved.add(row);return True

 # Reserve useful shade before the small-plant scatter consumes these clearings.
 # Courtyard corners, fields and the inhabited bank receive coherent tree groups;
 # the open desert retains the sparse independent wilderness pass below.
 canopy_rng=random.Random(f'{c.seed}:canopy');shade_sites=[]
 for b in town.buildings:
  for sx,sy in ((-1,1),(1,1),(-1,-1),(1,-1)):
   shade_sites.append(local_to_world(b,(sx*(b.width/2+3.0),sy*(b.depth/2+2.75))))
 for f in town.farms:
  shade_sites.extend((f.center[0]+sx*(f.width/2+2.5),f.center[1]+sy*(f.depth/2+2.5))
                     for sx,sy in ((-1,-1),(1,1),(-1,1),(1,-1)))
 for w in town.water_bodies:
  for i in range(16):
   y=w.center[1]+(i/15*2-1)*w.radii[1]*.88
   x=w.center[0]+w.radii[0]*math.sqrt(max(0,1-((y-w.center[1])/w.radii[1])**2))+3
   shade_sites.append((x,y))
 canopy_rng.shuffle(shade_sites)
 goal=round(c.vegetation_amount*(14+1.5*len(town.buildings)))
 count=0
 for x,y in shade_sites:
  if count>=goal:break
  for attempt in range(5):
   dx,dy=canopy_rng.uniform(-1.5,1.5),canopy_rng.uniform(-1.5,1.5)
   if put('palm' if canopy_rng.random()<.78 else 'tree',x+dx,y+dy,'oasis-canopy'):
    count+=1;break

 step=1.25;half=c.town_size/2
 for iy in range(int(c.town_size/step)):
  for ix in range(int(c.town_size/step)):
   x=-half+(ix+.5)*step+rng.uniform(-.3,.3);y=-half+(iy+.5)*step+rng.uniform(-.3,.3)
   f=sample_fields(x,y,town)
   if clear.in_building(x,y,.5) or clear.near_path(x,y,.5) or clear.in_farm(x,y,.5):continue
   inside=in_water(x,y,town);shore=min((abs(water_distance(x,y,w)) for w in town.water_bodies),default=100)
   if shore<2.8 and rng.random()<c.vegetation_amount*.78:
    put('reeds'+(['','_1','_2'][rng.randrange(3)]),x,y,'wetland');continue
   if inside:continue
   if rng.random()<f['vegetation_influence']*.55:
    if rng.random()<.085 and shore<12:put('palm' if rng.random()<.65 else 'tree',x,y,'oasis-canopy')
    else:put('scrub'+(['','_1','_2'][rng.randrange(3)]),x,y,'understory')
   elif rng.random()<.014:put('rock'+(['','_1','_2'][rng.randrange(3)]),x,y,'wilderness')

 # Crop rows keep organized planting, while exact reservations leave equipment
 # and footpaths accessible instead of laying crops through irrigation blocks.
 for farm in town.farms:
  owner=next(b for b in town.buildings if b.id==farm.building_id)
  for ix in range(max(1,int(farm.width)-1)):
   for iy in range(max(1,int(farm.depth)-1)):
    x=farm.center[0]-farm.width/2+1+ix;y=farm.center[1]-farm.depth/2+1+iy
    if in_field_approach(x,y,farm,owner,.75):continue
    put('crop'+(['','_1','_2'][(ix+iy)%3]),x,y,'cultivated')

 rx,ry=town.anchors['ruin']
 for i in range(round(c.ruin_amount*(8+c.town_age*20))):
  x=rx+rng.gauss(0,c.town_size*.06);y=ry+rng.gauss(0,c.town_size*.06)
  if abs(x)>half-2 or abs(y)>half-2 or not clear.free(x,y,1.5):continue
  put('ruin' if i%3 else 'rock_2',x,y,'ancient-remnant')
 return rows
