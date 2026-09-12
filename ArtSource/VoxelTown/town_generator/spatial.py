"""Shared finite-size clearance and exact voxel reservations for pure planners."""
import math
from functools import lru_cache
from .buildings import building_bounds, farm_bounds, local_to_world
from .terrain import water_distance
from .assets import build_recipes
from .config import snap


def rect_contains(rect,x,y,pad=0):
 return rect[0]-pad<=x<=rect[2]+pad and rect[1]-pad<=y<=rect[3]+pad


def segment_distance(x,y,a,b):
 dx=b[0]-a[0];dy=b[1]-a[1];den=dx*dx+dy*dy
 t=0 if den==0 else max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/den))
 return math.hypot(x-a[0]-t*dx,y-a[1]-t*dy)


class Clearance:
 def __init__(self,town):
  self.town=town
  self.buildings=[(b.id,building_bounds(b)) for b in town.buildings]
  self.farms=[farm_bounds(f) for f in town.farms]
  self.bins={};self.step=4
  for path in town.paths:
   for a,b in zip(path.points,path.points[1:]):
    r=path.width/2
    for iy in range(math.floor((min(a[1],b[1])-r-2)/4),math.floor((max(a[1],b[1])+r+2)/4)+1):
     for ix in range(math.floor((min(a[0],b[0])-r-2)/4),math.floor((max(a[0],b[0])+r+2)/4)+1):
      self.bins.setdefault((ix,iy),[]).append((a,b,r))

 def near_path(self,x,y,pad=.5):
  # Existing bins include 2m of padding; expand query bins for larger objects.
  bx,by=math.floor(x/4),math.floor(y/4)
  reach=math.ceil(max(0,pad-2)/4)
  for iy in range(by-reach,by+reach+1):
   for ix in range(bx-reach,bx+reach+1):
    if any(segment_distance(x,y,a,b)<r+pad for a,b,r in self.bins.get((ix,iy),())):return True
  return False

 def in_building(self,x,y,pad=0,ignore=None):
  return any(identifier!=ignore and rect_contains(rect,x,y,pad) for identifier,rect in self.buildings)

 def in_farm(self,x,y,pad=0):
  return any(rect_contains(rect,x,y,pad) for rect in self.farms)

 def free(self,x,y,radius=.5,allow_farm=False,ignore_building=None):
  half=self.town.config.town_size/2
  return not (abs(x)+radius>half or abs(y)+radius>half or
              any(water_distance(x,y,w)<radius for w in self.town.water_bodies) or
              self.in_building(x,y,radius,ignore_building) or self.near_path(x,y,radius) or
              (not allow_farm and self.in_farm(x,y,radius)))


@lru_cache(maxsize=4)
def asset_recipes(q=.25):
 """Internal immutable-use cache; clients receive only derived placement data."""
 return build_recipes(q)


@lru_cache(maxsize=512)
def asset_cells(asset,turn,q=.25):
 result=[]
 for x,y,z in asset_recipes(q)[asset].cells:
  for _ in range(turn%4):x,y=-y-1,x
  result.append((x,y,z))
 return tuple(result)


@lru_cache(maxsize=512)
def asset_bounds(asset,turn,q=.25):
 cells=asset_cells(asset,turn,q)
 return (min(c[0] for c in cells)*q,min(c[1] for c in cells)*q,
         (max(c[0] for c in cells)+1)*q,(max(c[1] for c in cells)+1)*q)


def placement_bounds(row,q=.25):
 turn=round(row.get('rotation',0)/(math.pi/2))%4
 a,b,c,d=asset_bounds(row['asset'],turn,q);x,y,_=row['position']
 return (a+x,b+y,c+x,d+y)


def placement_cells(row,q=.25):
 turn=round(row.get('rotation',0)/(math.pi/2))%4
 tx,ty,tz=(round(v/q) for v in row['position'])
 return ((x+tx,y+ty,z+tz) for x,y,z in asset_cells(row['asset'],turn,q))


class Reservations:
 """Independent generated entities may never claim the same solid voxel."""
 def __init__(self,q=.25,rows=()):
  self.q=q;self.cells=set()
  for row in rows:self.add(row)
 def collides(self,row):
  return any(cell in self.cells for cell in placement_cells(row,self.q))
 def add(self,row):
  self.cells.update(placement_cells(row,self.q))


def inside_room(row,building,q=.25,keep_aisle=True):
 """Protect wall volume and a 1m doorway-to-room aisle using full asset bounds."""
 world=placement_bounds(row,q);room=building_bounds(building,-.5)
 if world[0]<room[0]-1e-6 or world[1]<room[1]-1e-6 or world[2]>room[2]+1e-6 or world[3]>room[3]+1e-6:return False
 if building.role in ('meeting_house','shrine'):
  # Civic/religious shells use .75m corner piers rather than .5m flat walls.
  for sx,sy in ((-1,-1),(-1,1),(1,-1),(1,1)):
   a=local_to_world(building,(sx*building.width/2,sy*building.depth/2))
   b=local_to_world(building,(sx*(building.width/2-.75),sy*(building.depth/2-.75)))
   pier=(min(a[0],b[0]),min(a[1],b[1]),max(a[0],b[0]),max(a[1],b[1]))
   if not (world[2]<=pier[0] or pier[2]<=world[0] or world[3]<=pier[1] or pier[3]<=world[1]):return False
 if keep_aisle:
  # Transform the local approach rectangle; quarter turns keep it axis-aligned.
  a=local_to_world(building,(-.5,-building.depth/2));b=local_to_world(building,(.5,0))
  aisle=(min(a[0],b[0]),min(a[1],b[1]),max(a[0],b[0]),max(a[1],b[1]))
  if not (world[2]<=aisle[0] or aisle[2]<=world[0] or world[3]<=aisle[1] or aisle[3]<=world[1]):return False
 return True


def interior_candidates(building,preferred=(0,0),q=.25):
 """Try the desired activity location, then deterministic nearby usable positions."""
 points=[]
 for ix in range(math.ceil((-building.width/2+.5)/q),math.floor((building.width/2-.5)/q)+1):
  for iy in range(math.ceil((-building.depth/2+.5)/q),math.floor((building.depth/2-.5)/q)+1):
   x,y=ix*q,iy*q
   points.append(((x-preferred[0])**2+(y-preferred[1])**2,x,y))
 for _,x,y in sorted(points):yield local_to_world(building,(x,y))


def make_placement(asset,x,y,z,rotation,q,**fields):
 return dict(asset=asset,position=(snap(x,q),snap(y,q),snap(z,q)),rotation=rotation,scale=(1,1,1),**fields)


def farm_entrance(farm,building):
 """House-facing field gate and outward axis; field coordinates are world-aligned."""
 dx,dy=building.position[0]-farm.center[0],building.position[1]-farm.center[1]
 if abs(dx)>abs(dy):
  sign=1 if dx>0 else -1
  return ((farm.center[0]+sign*farm.width/2,farm.center[1]),(sign,0))
 sign=1 if dy>0 else -1
 return ((farm.center[0],farm.center[1]+sign*farm.depth/2),(0,sign))


def in_field_approach(x,y,farm,building,pad=0):
 gate,normal=farm_entrance(farm,building)
 along=(x-gate[0])*normal[0]+(y-gate[1])*normal[1]
 sideways=abs((x-gate[0])*normal[1]-(y-gate[1])*normal[0])
 return -2-pad < along < .5+pad and sideways < .75+pad
