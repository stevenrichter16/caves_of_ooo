"""Archetype semantics, footprint transforms and clearance-aware lot placement."""
import math
from .config import snap
from .model import Building, Room, FarmPlot
from .districts import ROLE_DISTRICT

PROFILES={'residence':'domestic','workshop':'crafting','trader':'merchandise',
          'meeting_house':'community','shrine':'ritual','storehouse':'storage',
          'farmhouse':'agricultural','bathhouse':'bathing','animal_enclosure':'livestock','guard_watch':'guarding'}


def local_to_world(building, xy):
    c,s=round(math.cos(building.rotation)),round(math.sin(building.rotation))
    return (building.position[0]+c*xy[0]-s*xy[1],building.position[1]+s*xy[0]+c*xy[1])


def entrance_world(building,index=0):
    return local_to_world(building,building.entrances[index])


def entrance_approach(building,distance=2.0):
    x,y=building.entrances[0]
    if abs(y) >= building.depth/2-1e-6:
        y+=math.copysign(distance,y)
    else:
        x+=math.copysign(distance,x)
    return local_to_world(building,(x,y))


def building_bounds(building,padding=0):
    quarter=round(building.rotation/(math.pi/2))%2
    width,depth=(building.depth,building.width) if quarter else (building.width,building.depth)
    x,y=building.position
    return (x-width/2-padding,y-depth/2-padding,x+width/2+padding,y+depth/2+padding)


def bounds_overlap(a,b,padding=0):
    return not (a[2]+padding <= b[0] or b[2]+padding <= a[0] or a[3]+padding <= b[1] or b[3]+padding <= a[1])


def rect_hits_water(bounds,water,padding=0):
    """Closest point catches edge-crossing ellipses that corner-only tests miss."""
    x=min(max(water.center[0],bounds[0]),bounds[2])
    y=min(max(water.center[1],bounds[1]),bounds[3])
    return ((x-water.center[0])/(water.radii[0]+padding))**2+((y-water.center[1])/(water.radii[1]+padding))**2 <= 1


def farm_bounds(farm,padding=0):
    x,y=farm.center
    return (x-farm.width/2-padding,y-farm.depth/2-padding,x+farm.width/2+padding,y+farm.depth/2+padding)


def _rotation_to(position,target):
    # Local front points -Y; four directions keep block export cell-aligned.
    dx,dy=target[0]-position[0],target[1]-position[1]
    if abs(dx)>abs(dy):
        return math.pi/2 if dx>0 else 3*math.pi/2
    return 0 if dy<0 else math.pi


def _room(building_id,role,width,depth):
    room_role={'residence':'living','workshop':'workroom','trader':'shop','meeting_house':'gathering',
               'shrine':'sanctum','storehouse':'storage','farmhouse':'farm_kitchen','bathhouse':'bath',
               'animal_enclosure':'pen','guard_watch':'watch' }[role]
    return [Room(building_id+'-room',room_role,(0,0),width-1,depth-1)]


def place_buildings(config,anchors,districts,waters,rng):
    buildings=[]; farms=[]
    district_map={d.role:d for d in districts}
    roles=list(config.archetypes)
    # Cycle once to guarantee requested archetypes; additional homes carry population.
    sequence=[roles[i%len(roles)] for i in range(config.building_count)]
    if len(roles)>1:
        rng.shuffle(sequence)
    # Water-dependent and attached-field uses claim scarce shore lots before
    # less constrained houses, shops and civic buildings occupy those sites.
    sequence.sort(key=lambda role: 0 if role in ('farmhouse','animal_enclosure') else 1 if role=='bathhouse' else 2)
    for i,role in enumerate(sequence):
        district=district_map[ROLE_DISTRICT[role]]
        identifier=f'building-{i:03d}-{role}'
        base={'residence':(6.5,7),'workshop':(8,7),'trader':(7,7),
              'meeting_house':(9,9),'shrine':(6.5,7),'storehouse':(8,8),
              'farmhouse':(6.5,6.5),'bathhouse':(8,8),'animal_enclosure':(9,8),
              'guard_watch':(5.5,6)}[role]
        compact=min(1.0,config.town_size/64)
        minimum=math.ceil(5/(config.voxel_size*2))*config.voxel_size*2
        width=max(minimum,snap((base[0]+rng.uniform(-1,1)*config.building_irregularity)*compact,config.voxel_size*2))
        depth=max(minimum,snap((base[1]+rng.uniform(-1,1)*config.building_irregularity)*compact,config.voxel_size*2))
        height=max(2.5,snap(2.5+config.wealth*.75+rng.uniform(-.25,.25),config.voxel_size))
        if role=='animal_enclosure': height=snap(1.5,config.voxel_size)
        if role=='guard_watch': height=snap(3.75,config.voxel_size)
        material='sandstone' if rng.random() < .7 else 'earth'
        # Each household/workplace seeks its own point within a shared district.
        # Minimizing every lot against one identical center produced aligned stacks.
        wander=config.town_size*.10*config.building_irregularity
        ideal=tuple(v+rng.uniform(-wander,wander) for v in district.center)
        best=None; best_score=float('inf')
        # All attempts are bounded; impossible settings reject instead of silently dropping buildings.
        for attempt in range(1400):
            spread=config.town_size*(.075+.08*(1-config.density))
            if attempt < 700:
                position=tuple(snap(rng.gauss(v,spread),config.voxel_size) for v in ideal)
            else:
                position=(snap(rng.uniform(-.27,.42)*config.town_size,config.voxel_size),
                          snap(rng.uniform(-.40,.40)*config.town_size,config.voxel_size))
            target=anchors['market'] if role=='trader' else anchors['plaza']
            rotation=_rotation_to(position,target)
            b=Building(identifier,role,position,rotation,width,depth,height,[(0,-depth/2)],
                       _room(identifier,role,width,depth),material,PROFILES[role],[])
            bounds=building_bounds(b)
            extra=[]
            farm=None
            if role=='farmhouse' and config.agriculture_amount>0:
                fw=snap(5+config.agriculture_amount*5,config.voxel_size*2)
                fd=snap(5+config.agriculture_amount*5,config.voxel_size*2)
                # A side garden leaves the front door and its approach free.
                farm_center=local_to_world(b,(width/2+fw/2+snap(1.25,config.voxel_size),0))
                farm=FarmPlot(identifier+'-field',identifier,farm_center,fw,fd)
                extra.append(farm_bounds(farm))
            all_bounds=[bounds]+extra
            margin=config.town_size/2-2.5
            if any(r[0]<-margin or r[1]<-margin or r[2]>margin or r[3]>margin for r in all_bounds): continue
            if any(rect_hits_water(r,w,padding=1.5) for r in all_bounds for w in waters): continue
            separation=3.0+.75*(1-config.density)
            if any(bounds_overlap(r,building_bounds(old),separation) for r in all_bounds for old in buildings): continue
            if any(bounds_overlap(r,farm_bounds(old),2) for r in all_bounds for old in farms): continue
            # Plazas/wells/path destinations remain unoccupied by lot geometry.
            if any(r[0]-3 < p[0] < r[2]+3 and r[1]-3 < p[1] < r[3]+3
                   for r in all_bounds for key,p in anchors.items() if key not in ('farming','ruin')): continue
            approach=entrance_approach(b,2.25)
            if any(building_bounds(old,1)[0] < approach[0] < building_bounds(old,1)[2]
                   and building_bounds(old,1)[1]<approach[1]<building_bounds(old,1)[3] for old in buildings): continue
            if any(r[0]-1<entrance_approach(old,2.25)[0]<r[2]+1 and r[1]-1<entrance_approach(old,2.25)[1]<r[3]+1
                   for r in all_bounds for old in buildings): continue
            score=math.dist(position,ideal)+.15*math.dist(position,district.center)
            for old in buildings:
                if math.dist(position,old.position)<config.town_size*.3:
                    score+=config.building_irregularity*sum(max(0,1.75-abs(position[a]-old.position[a])) for a in (0,1))
            if role=='trader': score+=.5*math.dist(position,anchors['main_entrance'])
            if role=='farmhouse': score+=.5*abs(position[0]-anchors['farming'][0])
            score+=rng.random()*config.building_irregularity*2
            if score<best_score:
                best_score=score; best=(b,farm)
        if best is None:
            raise ValueError(f'Cannot place {role} {i+1}/{config.building_count}; enlarge town_size or reduce building_count')
        buildings.append(best[0])
        if best[1]: farms.append(best[1])
    # Stable distinct identities; weighted homes avoid making every workplace a dormitory.
    slots=[]
    for b in buildings:
        slots.extend([b]*(3 if b.role in ('residence','farmhouse') else 1))
    for i in range(config.population):
        slots[i%len(slots)].occupants.append(f'npc-{i:04d}')
    return buildings,farms
