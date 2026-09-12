"""Cell-first rectangular candidates for native gameplay integration.

This adapter complements, rather than changes, the square ``Town`` planner.
One logical cell is one metre. Positions/footprints use X east and Y south;
quarter turns rotate east toward south. Buildings have independently owned
wall cells, accessible open entrances and reserved interior circulation.
The output is a candidate plan: it never installs or replaces Unity content.
"""
from collections import deque
from dataclasses import dataclass
import hashlib
import heapq
import math
import random

from .native_assets import asset_manifest

DEFAULT_CHUNKS=(('Overworld.3.6.0','settlement'),('Overworld.2.6.0','agricultural'),
                ('Overworld.3.7.0','stump_foothills'),('Overworld.4.7.0','grove'))
DIRECTIONS={'north':(0,-1),'south':(0,1),'west':(-1,0),'east':(1,0)}
OPPOSITE={'north':'south','south':'north','west':'east','east':'west'}


@dataclass(frozen=True)
class NativeConfig:
    seed:int=41
    width:int=80
    height:int=25
    water_amount:float=.5
    vegetation_amount:float=.65
    clutter_amount:float=.6
    population_per_building:int=2

    def validate(self):
        for name,low,high in (('seed',-2**63,2**63-1),('width',40,256),('height',20,128),
                              ('population_per_building',0,6)):
            value=getattr(self,name)
            if type(value) is not int or not low<=value<=high:raise ValueError(f'Invalid {name}')
        for name in ('water_amount','vegetation_amount','clutter_amount'):
            value=getattr(self,name)
            if isinstance(value,bool) or not isinstance(value,(int,float)) or not math.isfinite(value) or not 0<=value<=1:
                raise ValueError(f'{name} must be finite between zero and one')
        return self


def _hash(*values):
    return int.from_bytes(hashlib.sha256('|'.join(map(str,values)).encode()).digest()[:8],'big')


def _xy(rows):return {tuple(p) for p in rows}
def _sorted(cells):return [list(p) for p in sorted(cells,key=lambda p:(p[1],p[0]))]
def _rect(x,y,w,h,pad=0):return {(xx,yy) for yy in range(y-pad,y+h+pad) for xx in range(x-pad,x+w+pad)}


def _portals(config,zone_id):
    _,wx,wy,wz=zone_id.split('.');wx,wy,wz=map(int,(wx,wy,wz));result=[]
    for direction,(dx,dy) in DIRECTIONS.items():
        neighbor=f'Overworld.{wx+dx}.{wy+dy}.{wz}'
        axis_size=config.width if dy else config.height
        low=max(3,round(axis_size*.3));high=min(axis_size-4,round(axis_size*.7))
        index=low+_hash(config.seed,'portal',*sorted((zone_id,neighbor)))%(high-low+1)
        cells=[];approach=[]
        for coordinate in range(index-1,index+2):
            for depth in range(3):
                x=(coordinate if dy else depth if dx<0 else config.width-1-depth)
                y=(coordinate if dx else depth if dy<0 else config.height-1-depth)
                approach.append([x,y])
                if depth==0:cells.append([x,y])
        result.append({'direction':direction,'neighborZoneId':neighbor,'cells':cells,'approachCells':approach})
    return result


def _route(start,goal,blocked,width,height,seed,radius=0,preferred=frozenset()):
    """Four-neighbour A* with actual widened-cell clearance, including edges."""
    def clear(p):
        x,y=p
        return 0<=x<width and 0<=y<height and not any(
            (x+dx,y+dy) in blocked for dx in range(-radius,radius+1) for dy in range(-radius,radius+1))
    if not clear(start) or not clear(goal):raise ValueError('Route endpoint lacks native clearance')
    heap=[(0,0,start)];cost={start:0};previous={}
    while heap:
        _,spent,p=heapq.heappop(heap)
        if spent!=cost[p]:continue
        if p==goal:
            result=[p]
            while p in previous:p=previous[p];result.append(p)
            return result[::-1]
        for dx,dy in DIRECTIONS.values():
            n=(p[0]+dx,p[1]+dy)
            if not clear(n):continue
            step=(.8 if n in preferred else 1)+(_hash(seed,*n)%17)/100
            value=spent+step
            if value<cost.get(n,float('inf')):
                cost[n]=value;previous[n]=p
                heapq.heappush(heap,(value+.8*(abs(goal[0]-n[0])+abs(goal[1]-n[1])),value,n))
    raise ValueError('No accessible native route')


def _buildings(config,role,rng,portals,plaza):
    roles={'settlement':['meeting_house','trader','workshop','residence','residence','storehouse'],
           'agricultural':['farmhouse','storehouse'],'stump_foothills':['guard_watch'],'grove':['shrine']}[role]
    # Smaller rectangular envelopes request proportionally fewer complete lots.
    if config.width*config.height<2000:roles=roles[:max(1,len(roles)*config.width*config.height//2000)]
    occupied=_rect(*plaza,1,1,3)|{tuple(p) for port in portals for p in port['approachCells']}
    result=[]
    for index,building_role in enumerate(roles):
        dimensions={'meeting_house':(10,12,7,8),'trader':(9,12,5,6),
                    'workshop':(8,10,7,8),'residence':(6,8,6,7),
                    'storehouse':(8,11,5,6),'farmhouse':(8,10,6,8),
                    'guard_watch':(6,7,5,6),'shrine':(7,9,6,7)}[building_role]
        w=rng.randint(*dimensions[:2]);h=rng.randint(*dimensions[2:])
        target={'trader':(config.width*.18,config.height*.55),
                'workshop':(config.width*.77,config.height*.27),
                'farmhouse':(config.width*.65,config.height*.55),
                'storehouse':(config.width*.75,config.height*.7),
                'shrine':(config.width*.30,config.height*.3)}.get(building_role,plaza)
        candidates=[]
        for _ in range(1400):
            x=rng.randint(3,config.width-w-3);y=rng.randint(3,config.height-h-3)
            if occupied.intersection(_rect(x,y,w,h,1)):continue
            score=math.dist((x+w/2,y+h/2),target)+rng.random()*8
            candidates.append((score,x,y))
        if not candidates:raise ValueError('Rectangular settlement has insufficient clear building lots')
        _,x,y=min(candidates);center=(x+w//2,y+h//2)
        sides=[((x+w//2,y),(0,1),2),((x+w//2,y+h-1),(0,-1),0),
               ((x,y+h//2),(1,0),1),((x+w-1,y+h//2),(-1,0),3)]
        entrance,inward,turn=min(sides,key=lambda row:math.dist(row[0],plaza))
        corridor=[];p=entrance
        while p!=center:
            corridor.append(p)
            if p[0]!=center[0]:p=(p[0]+(1 if p[0]<center[0] else -1),p[1])
            else:p=(p[0],p[1]+(1 if p[1]<center[1] else -1))
        corridor.append(center)
        interior=_rect(x+1,y+1,w-2,h-2);walls=_rect(x,y,w,h)-interior-{entrance}
        identifier=f'building-{index:02}-{building_role}'
        row={'id':identifier,'role':building_role,'bounds':[x,y,w,h],
             'rotationQuarterTurns':turn,'wallHeight':2.5,'entrance':list(entrance),
             'entranceCells':[list(entrance)],'interiorCells':_sorted(interior),
             'wallCells':_sorted(walls),'aisleCells':_sorted(corridor),
             'approach':[entrance[0]-inward[0],entrance[1]-inward[1]]}
        result.append(row);occupied|=_rect(x,y,w,h,1)
        # Reserve the actual approach so the next lot cannot plug this opening.
        occupied|={tuple(row['approach'])}
    return result


def _chunk_attempt(config,zone_id,role,assets,attempt):
    rng=random.Random(_hash(config.seed,zone_id,'native-layout',attempt));w,h=config.width,config.height
    portals=_portals(config,zone_id)
    plaza=(round(w*.5+rng.uniform(-w*.045,w*.045)),round(h*.5+rng.uniform(-1,1)))
    buildings=_buildings(config,role,rng,portals,plaza)
    lots={p for b in buildings for p in _rect(*b['bounds'])};walls={tuple(p) for b in buildings for p in b['wallCells']}
    routes=set();paths=[];centre_lines=set();zone_ids={row[0] for row in DEFAULT_CHUNKS}
    for i,portal in enumerate(portals):
        radius=1 if portal['neighborZoneId'] in zone_ids else 0
        start=tuple(portal['cells'][1]);points=_route(start,plaza,lots,w,h,_hash(config.seed,zone_id,i),radius,centre_lines)
        for x,y in points:
            routes|={(x+dx,y+dy) for dx in range(-radius,radius+1) for dy in range(-radius,radius+1)
                     if 0<=x+dx<w and 0<=y+dy<h and (x+dx,y+dy) not in lots}
        centre_lines.update(points)
        routes|=_xy(portal['approachCells'])
        paths.append({'id':f'entry-{i}','purpose':'entrance-to-plaza','width':2*radius+1,'points':[list(p) for p in points]})
    for b in buildings:
        points=_route(tuple(b['approach']),plaza,lots,w,h,_hash(config.seed,zone_id,b['id']),0,routes)
        routes.update(points);routes|=_xy(b['aisleCells'])
        paths.append({'id':b['id']+'-path','purpose':b['role']+'-to-plaza','width':1,
                      'points':[b['entrance']]+[list(p) for p in points]})
    interiors={tuple(p) for b in buildings for p in b['interiorCells']}
    wet=set();terrain=[];dry_lots=lots|routes;shore=set()
    water_center=(w*.16,h*.26);rx=max(2,w*.12*config.water_amount);ry=max(2,h*.27*config.water_amount)
    for y in range(h):
        for x in range(w):
            normalized=math.sqrt(((x-water_center[0])/rx)**2+((y-water_center[1])/ry)**2)
            water=config.water_amount>0 and normalized<1
            water=water and (x,y) not in dry_lots
            if water:wet.add((x,y))
            shore_distance=(normalized-1)*min(rx,ry)
            if config.water_amount and 0<=shore_distance<4 and (x,y) not in dry_lots:shore.add((x,y))
            material='earth' if role=='stump_foothills' else 'oasis_soil' if role in ('grove','agricultural') else 'sand'
            if (x,y) in shore:material='oasis_soil'
            if (x,y) in routes:material='worn_ground'
            if (x,y) in interiors:material='dark_floor'
            terrain.append({'x':x,'y':y,'material':material,'water':bool(water),'interior':(x,y) in interiors})
    placements=[];reserved=set();non_wall_count=0

    def place(asset,x,y,semantic,category,turn=0,allowed=None,optional=False,home=None):
        nonlocal non_wall_count
        recipe=assets[asset];cells=[]
        for dx,dy in recipe['nativeFootprintCells']:
            for _ in range(turn):dx,dy=-dy,dx
            cells.append([dx,dy])
        absolute={(x+dx,y+dy) for dx,dy in cells}
        if not all(0<=xx<w and 0<=yy<h for xx,yy in absolute):return False
        if absolute.intersection(reserved|routes|wet):return False
        if allowed is not None and not absolute<=allowed:return False
        if category not in ('wall','furniture','npc') and absolute.intersection(lots):return False
        identifier=f'{zone_id}/{category}-{non_wall_count:04}-{asset}';non_wall_count+=1
        family=asset.split('_')[0]
        row={'id':identifier,'asset':recipe['id'],'sourceAsset':asset,'role':semantic,'category':category,
             'x':x,'y':y,'rotationQuarterTurns':turn,'cells':cells,'uniformScale':1,
             'solid':category not in ('vegetation','crop') or family in ('tree','palm','rock'),
             'opaque':family in ('wall','tree','palm','ruin'),
             'destructible':True,'optional':optional}
        if home:row['homeId']=home
        placements.append(row);reserved.update(absolute);return True

    for b in buildings:
        bx,by,bw,bh=b['bounds']
        for x,y in b['wallCells']:
            turn=1 if x in (bx,bx+bw-1) and y not in (by,by+bh-1) else 0
            if not place('wall',x,y,'wall','wall',turn=turn,allowed=walls):raise ValueError('Wall installation collision')
            placements[-1]['id']=f"{zone_id}/{b['id']}/wall-{x}-{y}"
        allowed=_xy(b['interiorCells']);candidates=list(allowed-routes);rng.shuffle(candidates)
        furniture={'residence':['bed','crate','stool'],'trader':['table','crate'],
                   'workshop':['workbench','furnace','crate'],'meeting_house':['table','stool'],
                   'storehouse':['crate','barrel','shelf'],'farmhouse':['bed','barrel','stool'],
                   'guard_watch':['crate','stool'],'shrine':['firepit','stool']}[b['role']]
        for asset in furniture:
            for x,y in candidates:
                if place(asset,x,y,b['role'],'furniture',allowed=allowed,home=b['id']):break
            else:raise ValueError(f'No complete {asset} in {b["id"]}')
        for index in range(config.population_per_building):
            asset=('npc','npc_1','npc_2')[index%3]
            for x,y in candidates:
                if place(asset,x,y,b['role'],'npc',allowed=allowed,home=b['id']):break
            else:raise ValueError(f'No resident space in {b["id"]}')

    for b in buildings:
        if b['role']!='trader':continue
        ex,ey=b['entrance'];points=[(math.dist((x,y),(ex,ey)),x,y) for y in range(2,h-2) for x in range(2,w-2)]
        for distance,x,y in sorted(points):
            if distance<2:continue
            if place('market_stall',x,y,'outdoor-market','prop',home=b['id']):break
        else:raise ValueError('No accessible exterior trading area')

    # A communal water fixture belongs to the plaza's activity edge, not the
    # path centre. Search the rule-defined annulus rather than one demo point.
    points=[(math.dist((x,y),plaza),x,y) for y in range(2,h-2) for x in range(2,w-2)]
    for distance,x,y in sorted(points):
        if distance<3:continue
        if place('well',x,y,'water-source','prop'):break
    # Full native assets can overhang several cells; reservations use their
    # complete footprint instead of silently compressing them into one owner.
    farms=[]
    if role=='agricultural':
        for b in buildings:
            if b['role']!='farmhouse':continue
            bx,by,bw,bh=b['bounds'];candidates=[]
            for y in range(2,h-6):
                for x in range(2,w-10):
                    cells=_rect(x,y,9,5)
                    if cells.intersection(lots|routes|reserved|wet):continue
                    candidates.append((math.dist((x+4,y+2),(bx+bw/2,by+bh/2)),x,y))
            if not candidates:raise ValueError('No accessible agricultural field')
            _,x,y=min(candidates);farm={'id':b['id']+'-field','buildingId':b['id'],'bounds':[x,y,9,5],'cells':[]}
            for cell in terrain:
                if x<=cell['x']<x+9 and y<=cell['y']<y+5:cell['material']='field_soil'
            for yy in range(y,y+5,2):
                for xx in range(x,x+9,2):
                    if place('crop',xx,yy,'harvestable-crop','crop'):farm['cells'].append([xx,yy])
            farms.append(farm)
    dressing=random.Random(_hash(config.seed,zone_id,'native-dressing'))
    options=[(x,y) for y in range(1,h-1) for x in range(1,w-1)];dressing.shuffle(options)
    for x,y in options:
        activity=any(max(bx-x,0,x-(bx+bw-1))+max(by-y,0,y-(by+bh-1))<=2
                     for bx,by,bw,bh in (b['bounds'] for b in buildings))
        if config.clutter_amount and activity and dressing.random()<config.clutter_amount*.09:
            place(dressing.choice(('crate','crate_1','crate_2','crate_3','barrel','barrel_1','barrel_2','barrel_3','pot')),
                  x,y,'activity-clutter','prop',optional=True)
        # Coherent influence patches and a lush shoreline replace equal random
        # scatter. Settlement centres remain cleared; wilderness grows denser.
        patch=(math.sin((x+config.seed%11)*.25)+math.cos((y-config.seed%7)*.45)+2)/4
        density=.85 if (x,y) in shore else (.22 if role=='grove' else .05)*(.2+patch*1.6)
        if config.vegetation_amount and dressing.random()<config.vegetation_amount*density:
            near_water=any((x+dx,y+dy) in wet for dx in range(-2,3) for dy in range(-2,3))
            choices=('reeds','reeds','scrub') if near_water else ('scrub','scrub','palm') if (x,y) in shore else ('tree','scrub','scrub','rock') if role=='grove' else ('palm','scrub','scrub','rock')
            family=dressing.choice(choices);variant=dressing.randrange(3)
            asset=family+('_'+str(variant) if variant else '')
            place(asset,x,y,'native-vegetation','vegetation')
        if role in ('stump_foothills','grove') and dressing.random()<.009:
            place('ruin',x,y,'ancient-remnant','prop')
    return {'zoneId':zone_id,'role':role,'width':w,'height':h,'portals':portals,
            'anchors':{'plaza':list(plaza),'water':list(water_center)},'routes':_sorted(routes),
            'paths':paths,'terrain':terrain,'buildings':buildings,'farms':farms,'placements':placements}


def _chunk(config,zone_id,role,assets):
    # Greedy irregular lots can paint themselves into a corner. Retry the
    # deterministic placement stream, retaining the requested archetype count;
    # never repair one seed with hand coordinates or silently drop a building.
    failure=None
    for attempt in range(32):
        try:return _chunk_attempt(config,zone_id,role,assets,attempt)
        except ValueError as error:failure=error
    raise ValueError(f'No feasible rectangular plan for {zone_id}: {failure}')


def generate_region(config=None):
    """Generate four connected rectangular candidates; no global RNG or bpy."""
    config=(config or NativeConfig()).validate()
    assets={a['sourceAsset']:a for a in asset_manifest()['assets']}
    chunks=[_chunk(config,zone_id,role,assets) for zone_id,role in DEFAULT_CHUNKS]
    result={'schemaVersion':1,'seed':config.seed,'zoneWidth':config.width,'zoneHeight':config.height,
            'cellSize':1,'voxelSize':.25,'axes':'Native X east, Y south; Unity X east, Y height, Z north',
            'rotationUnits':'clockwise quarter turns in native XY','candidateOnly':True,'chunks':chunks}
    validate_region(result);return result


def _validate_region(region):
    """Reject incomplete, colliding or inaccessible native ownership exports."""
    w,h=region['zoneWidth'],region['zoneHeight'];chunks=region['chunks'];index={c['zoneId']:c for c in chunks}
    NativeConfig(seed=region['seed'],width=w,height=h).validate()
    if region['schemaVersion']!=1 or region['voxelSize']!=.25 or region['cellSize']!=1:raise ValueError('Unsupported region units/schema')
    assets={a['id']:a for a in asset_manifest()['assets']}
    if len(index)!=len(chunks) or len(chunks)!=4:raise ValueError('Expected four distinct chunks')
    all_ids=set();shared_graph={k:set() for k in index}
    for c in chunks:
        if (c['width'],c['height'])!=(w,h):raise ValueError('Inconsistent chunk dimensions')
        ground={(t['x'],t['y']):t for t in c['terrain']}
        if len(c['terrain'])!=w*h or set(ground)!=_rect(0,0,w,h):raise ValueError('Incomplete or duplicate terrain')
        blocked=set();owners=set();routes=_xy(c['routes'])
        if len(routes)!=len(c['routes']) or any(type(x) is not int or type(y) is not int or not (0<=x<w and 0<=y<h) for x,y in routes):
            raise ValueError('Invalid native route cell')
        for p in c['placements']:
            if p['asset'] not in assets:raise ValueError('Unknown native visual recipe')
            recipe=assets[p['asset']]
            if p['sourceAsset']!=recipe['sourceAsset']:raise ValueError('Asset identity mismatch')
            if type(p['rotationQuarterTurns']) is not int or p['rotationQuarterTurns'] not in range(4):raise ValueError('Invalid rotation')
            if type(p['x']) is not int or type(p['y']) is not int:raise ValueError('Anchor must be a native integer cell')
            if p['uniformScale']!=1:raise ValueError('Candidate ownership requires native-size geometry')
            if any(type(p[key]) is not bool for key in ('solid','opaque','destructible')):raise ValueError('Invalid physical flags')
            footprint=set()
            for dx,dy in recipe['nativeFootprintCells']:
                for _ in range(p['rotationQuarterTurns']):dx,dy=-dy,dx
                footprint.add((dx,dy))
            if _xy(p['cells'])!=footprint:raise ValueError('Native footprint disagrees with full-size recipe')
            if p['id'] in all_ids:raise ValueError('Duplicate owner ID')
            all_ids.add(p['id']);cells={(p['x']+dx,p['y']+dy) for dx,dy in p['cells']}
            if not cells or len(cells)!=len(p['cells']) or any(not (0<=x<w and 0<=y<h) for x,y in cells):raise ValueError('Invalid or crossing footprint')
            if p['solid']:
                if cells.intersection(blocked|routes):raise ValueError('Solid owner blocks another owner or route')
                blocked.update(cells)
            if p['role']=='wall':
                if p['cells']!=[[0,0]] or not p['destructible'] or not p['solid'] or not p['opaque']:
                    raise ValueError('Walls need individual solid opaque destructible cell owners')
                owners.add((p['x'],p['y']))
        expected={tuple(p) for b in c['buildings'] for p in b['wallCells']}
        if owners!=expected:raise ValueError('Missing or extra structural wall owners')
        start=tuple(c['anchors']['plaza'])
        if len(start)!=2 or any(type(n) is not int for n in start) or start not in ground or start in blocked or ground[start]['water']:
            raise ValueError('Plaza must be an open dry native cell')
        seen={start};queue=deque((start,))
        while queue:
            x,y=queue.popleft()
            for dx,dy in DIRECTIONS.values():
                n=(x+dx,y+dy)
                if 0<=n[0]<w and 0<=n[1]<h and n not in seen and n not in blocked:
                    seen.add(n);queue.append(n)
        for b in c['buildings']:
            x,y,bw,bh=b['bounds'];interior=_rect(x+1,y+1,bw-2,bh-2)
            perimeter=_rect(x,y,bw,bh)-interior
            if not _xy(b['entranceCells'])<=perimeter or _xy(b['wallCells'])!=perimeter-_xy(b['entranceCells']):
                raise ValueError('Building perimeter differs from its owned walls/openings')
            if _xy(b['interiorCells'])!=interior:raise ValueError('Building interior differs from its bounds')
            if not _xy(b['entranceCells'])<=seen or not _xy(b['aisleCells'])<=seen:raise ValueError('Inaccessible native entrance/interior')
            if any(not ground[tuple(p)]['interior'] for p in b['interiorCells']):raise ValueError('Lost interior semantics')
        for p in c['portals']:
            direction=p['direction'];dx,dy=DIRECTIONS[direction]
            _,wx,wy,wz=c['zoneId'].split('.');wx,wy,wz=map(int,(wx,wy,wz))
            if p['neighborZoneId']!=f'Overworld.{wx+dx}.{wy+dy}.{wz}':raise ValueError('Portal references wrong world neighbor')
            if len(p['cells'])!=3 or len(_xy(p['cells']))!=3:raise ValueError('Portal width/identity mismatch')
            axis=0 if dy else 1;coordinates=[xy[axis] for xy in p['cells']]
            if coordinates!=list(range(coordinates[0],coordinates[0]+3)):raise ValueError('Portal is not contiguous')
            for x,y in p['cells']:
                edge=(y==0 if dy<0 else y==h-1 if dy>0 else x==0 if dx<0 else x==w-1)
                if not edge or (x,y) not in seen:raise ValueError('Invalid or inaccessible edge portal')
            if not _xy(p['approachCells'])<=routes or _xy(p['approachCells']).intersection(blocked):raise ValueError('Portal approach blocked')
            neighbor=p['neighborZoneId']
            if neighbor in index:
                pair=next((q for q in index[neighbor]['portals'] if q['neighborZoneId']==c['zoneId']),None)
                if pair is None or pair['direction']!=OPPOSITE[direction] or coordinates!=[xy[axis] for xy in pair['cells']]:raise ValueError('Nonreciprocal seam')
                shared_graph[c['zoneId']].add(neighbor)
    seen={chunks[0]['zoneId']};queue=list(seen)
    while queue:
        for neighbor in shared_graph[queue.pop()]-seen:seen.add(neighbor);queue.append(neighbor)
    if len(seen)!=len(chunks):raise ValueError('Disconnected region')
    return True


def validate_region(region):
    """Validate the complete native candidate or raise a readable ValueError."""
    try:return _validate_region(region)
    except (KeyError,TypeError,IndexError,AttributeError) as error:
        raise ValueError(f'Malformed native region: {error}') from error
