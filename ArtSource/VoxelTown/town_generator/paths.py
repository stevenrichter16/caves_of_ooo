"""Destination-led paths with widened, obstacle-aware clearance."""
import heapq
import itertools
import math
from .buildings import building_bounds, entrance_world, entrance_approach
from .model import Path


def point_in_water(point,water,padding=0):
    return ((point[0]-water.center[0])/(water.radii[0]+padding))**2+((point[1]-water.center[1])/(water.radii[1]+padding))**2 < 1


def _simplify(points):
    result=[]
    for p in points:
        if result and math.dist(result[-1],p)<1e-7: continue
        while len(result)>=2:
            a,b=result[-2:]
            if abs((b[0]-a[0])*(p[1]-b[1])-(b[1]-a[1])*(p[0]-b[0]))>1e-7: break
            if (b[0]-a[0])*(p[0]-b[0])+(b[1]-a[1])*(p[1]-b[1])<0: break
            result.pop()
        result.append(tuple(p))
    return result


def route_path(start,end,buildings,waters,town_size,width,seed,irregularity,preferred=None):
    """Return world XY points or reject; width is protected, not just the centerline.

    Endpoints must lie outside building footprints. Building-door connectors are
    authored separately along their actual outward normals by generate_paths.
    Eight-neighbor A* prohibits diagonal corner cutting. Its coordinate noise is
    deterministic and never consumes the process-global random generator.
    """
    if isinstance(width,bool) or not isinstance(width,(int,float)) or not math.isfinite(width) or width <= 0:
        raise ValueError('Path width must be finite and greater than zero')
    step=.5
    extent=int(town_size/2/step)-2
    clearance=width/2+.26
    boxes=[building_bounds(b,clearance) for b in buildings]
    cache={}
    preferred=preferred or set()

    def blocked(node):
        if node in cache: return cache[node]
        x,y=node[0]*step,node[1]*step
        value=(abs(node[0])>extent or abs(node[1])>extent or
               any(a<x<c and b<y<d for a,b,c,d in boxes) or
               any(point_in_water((x,y),w,clearance) for w in waters))
        cache[node]=value
        return value

    def nearest(point):
        root=(round(point[0]/step),round(point[1]/step))
        if not blocked(root): return root
        # The public API fails closed when a destination itself is obstructed.
        raise ValueError(f'Path endpoint {point} has insufficient clearance for width {width}')

    source=nearest(start); target=nearest(end)
    queue=[]; serial=itertools.count(); scores={source:0}; previous={}
    heapq.heappush(queue,(0,next(serial),source))
    closed=set()
    while queue:
        _,_,node=heapq.heappop(queue)
        if node in closed: continue
        if node==target:
            nodes=[node]
            while node!=source:
                node=previous[node]; nodes.append(node)
            nodes.reverse()
            return _simplify([tuple(start)]+[(x*step,y*step) for x,y in nodes]+[tuple(end)])
        closed.add(node)
        for dx,dy in ((1,0),(0,1),(-1,0),(0,-1),(1,1),(-1,1),(-1,-1),(1,-1)):
            nxt=(node[0]+dx,node[1]+dy)
            if nxt in closed or blocked(nxt): continue
            if dx and dy and (blocked((node[0]+dx,node[1])) or blocked((node[0],node[1]+dy))): continue
            noise=((nxt[0]*73856093)^(nxt[1]*19349663)^(seed*83492791))&1023
            cost=math.hypot(dx,dy)*(1+irregularity*.55*noise/1023)
            if nxt in preferred: cost*=.76
            new_score=scores[node]+cost
            if new_score>=scores.get(nxt,float('inf')): continue
            scores[nxt]=new_score; previous[nxt]=node
            heuristic=math.dist(nxt,target)*.76
            heapq.heappush(queue,(new_score+heuristic,next(serial),nxt))
    raise ValueError(f'No accessible path between {start} and {end} at width {width}')


def generate_paths(config,buildings,waters,anchors):
    paths=[]; preferred=set()

    def connect(start,end,width,purpose,source_building=None,target_building=None):
        outer_start=entrance_approach(source_building) if source_building else start
        outer_end=entrance_approach(target_building) if target_building else end
        points=route_path(outer_start,outer_end,buildings,waters,config.town_size,width,
                          config.seed,config.path_irregularity,preferred)
        if source_building: points.insert(0,start)
        if target_building: points.append(end)
        path=Path(f'path-{len(paths):03d}',_simplify(points),width,purpose)
        paths.append(path)
        for a,b in zip(points,points[1:]):
            count=max(1,math.ceil(math.dist(a,b)/.5))
            for i in range(count+1):
                preferred.add((round((a[0]+(b[0]-a[0])*i/count)/.5),round((a[1]+(b[1]-a[1])*i/count)/.5)))

    connect(anchors['main_entrance'],anchors['market'],2.25,'entrance-market')
    connect(anchors['market'],anchors['plaza'],2.25,'market-plaza')
    connect(anchors['plaza'],anchors['secondary_entrance'],1.75,'plaza-secondary')
    connect(anchors['plaza'],anchors['water'],1.5,'plaza-water')
    for b in buildings:
        if b.role=='residence': anchor,purpose='water','home-water'
        elif b.role=='farmhouse': anchor,purpose='water','farm-water'
        elif b.role=='trader': anchor,purpose='market','trader-market'
        else: anchor,purpose='plaza',b.role+'-plaza'
        connect(entrance_world(b),anchors[anchor],1.0,purpose,source_building=b)
    farms=[b for b in buildings if b.role=='farmhouse']
    stores=[b for b in buildings if b.role=='storehouse']
    for b in buildings:
        destinations=farms if b.role=='residence' else stores if b.role in ('workshop','farmhouse') else []
        if destinations:
            target=min(destinations,key=lambda other:math.dist(b.position,other.position))
            purpose='home-farm' if b.role=='residence' else b.role+'-storage'
            connect(entrance_world(b),entrance_world(target),1,purpose,b,target)
    return paths
