"""Smooth environmental influence fields and quantized terrain planning (no bpy)."""
import math
from .buildings import building_bounds, farm_bounds

def xy(value):
 if hasattr(value,'position'):value=value.position
 if isinstance(value,dict):value=value.get('position',value.get('center',value))
 return float(value[0]),float(value[1])
def anchor(town,key,fallback=(0,0)):return xy(town.anchors.get(key,fallback))
def clamp(x):return max(0.0,min(1.0,x))
def distance(a,b):return math.hypot(a[0]-b[0],a[1]-b[1])
def water_distance(x,y,water):
 cx,cy=xy(water.center);rx,ry=water.radii
 return (math.hypot((x-cx)/rx,(y-cy)/ry)-1)*min(rx,ry)
def in_water(x,y,town):return any(water_distance(x,y,w)<0 for w in town.water_bodies)
def sample_fields(x,y,town):
 c=town.config;water=max((math.exp(-max(0,water_distance(x,y,w))/7) for w in town.water_bodies),default=0)
 plaza=anchor(town,'plaza');settlement=math.exp(-(distance((x,y),plaza)/(c.town_size*.28))**2)
 farming=max((math.exp(-((x-f.center[0])**2+(y-f.center[1])**2)/max(9,f.width*f.depth)) for f in town.farms),default=0)
 ruins=math.exp(-(distance((x,y),anchor(town,'ruin',(c.town_size*.3,c.town_size*.3)))/(c.town_size*.13))**2)*c.ruin_amount
 desert=clamp(.9-water*.8-settlement*.20)
 vegetation=clamp(c.vegetation_amount*(.10+.90*water)*(1-.45*settlement))
 return dict(water_influence=clamp(water),settlement_influence=clamp(settlement),agriculture_influence=clamp(farming),desert_influence=desert,ruin_influence=clamp(ruins),vegetation_influence=vegetation)

def ground_material(x,y,town):
 f=sample_fields(x,y,town)
 # Low-frequency patches make the wetland/desert boundary gradual. Sparse
 # near-identical sand shades articulate terrain without a noisy texture.
 drift=.11*math.sin(x*.31+y*.14)+.08*math.sin(y*.39-x*.13)
 if in_water(x,y,town):return 'earth'
 if any(a<=x<=c and b<=y<=d for a,b,c,d in (farm_bounds(v) for v in town.farms)):return 'field_soil'
 if f['water_influence']+drift>.67:return 'oasis_soil'
 if f['water_influence']+drift>.39 and f['settlement_influence']>.25:return 'sand_shade'
 if f['settlement_influence']+drift>.67:return 'sand_shade'
 return 'sand_shade' if drift>.135 else 'sand'

def ground_height(x,y,town):
 if in_water(x,y,town):return -.5
 # Every functional lot and route remains level, including peripheral farms.
 if any(a <= x <= c and b <= y <= d for a,b,c,d in [building_bounds(v,2) for v in town.buildings] + [farm_bounds(v,1) for v in town.farms]):return 0
 from .spatial import segment_distance
 if any(segment_distance(x,y,a,b)<=p.width/2+.75 for p in town.paths for a,b in zip(p.points,p.points[1:])):return 0
 # Central settlement is level; dunes rise only beyond the inhabited envelope.
 plaza=anchor(town,'plaza');r=distance((x,y),plaza)/town.config.town_size
 if r<.38:return 0
 h=max(0,math.sin(x*.16+y*.06)+math.cos(y*.11-x*.08)-.3)*min(1,(r-.38)*8)*.5
 q=town.config.voxel_size;return round(h/q)*q

def ground_cells(town,step=1):
 if not math.isfinite(step) or step<=0:raise ValueError('terrain step must be positive and finite')
 size=town.config.town_size;half=size/2;count=math.ceil(size/step)
 for iy in range(count):
  y0=-half+iy*step;y1=min(half,y0+step);y=(y0+y1)/2
  for ix in range(count):
   x0=-half+ix*step;x1=min(half,x0+step);x=(x0+x1)/2;top=ground_height(x,y,town)
   yield dict(center=(x,y,top-.25),size=(x1-x0,y1-y0,.5),material=ground_material(x,y,town))
