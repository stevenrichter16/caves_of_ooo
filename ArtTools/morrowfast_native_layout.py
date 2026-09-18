"""Authored coarse gameplay geometry for Morrowfast; no resampling of art."""
import json, math
from pathlib import Path
from collections import deque
import numpy as np
from ArtTools.felling_components import polygon_mask

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=ROOT/'ArtSource/Morrowfast'
PPU=40.96

def to_cell(x,y):return (math.floor(21.25+x/PPU),min(24,math.floor(y/PPU)))
def point(p):return {'x':p[0],'y':p[1]}
def rect(x0,y0,x1,y1):return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1)}
DIRECTIONS=((1,0),(-1,0),(0,1),(0,-1))

def coarse_mask(mask,threshold=.35):
    cells=set()
    for y in range(25):
        for x in range(21,59):
            l=max(0,int((x-21.25)*PPU));r=min(1536,int((x+1-21.25)*PPU))
            t=int(y*PPU);b=min(1024,int((y+1)*PPU))
            if r>l and mask[t:b,l:r].mean()>=threshold:cells.add((x,y))
    return cells

def create_layout():
    source=json.loads((PACKAGE/'build/manifest.json').read_text())
    # Cell-scale room boundaries are deliberate, not fractional sprite alpha.
    # They retain one-cell walls and a door channel on this game's coarse grid.
    floors={
      'keeper-gatehouse':rect(32,2,34,4),
      'dry-hem-guesthouse':rect(28,8,33,12),
      'long-loop-ropeshop':rect(29,16,33,19),
      'return-desk-archive':rect(47,2,51,5),
      'second-bowl-kitchen':rect(50,16,53,19)}
    blocked=set();opaque=set();water=set();buildings=[]
    creek=polygon_mask((1536,1024),[[172,0],[352,0],[281,125],[244,267],[187,380],[171,481],[180,626],[141,753],[93,868],[105,1023],[0,1023],[0,943],[41,811],[79,691],[99,577],[92,465],[118,331],[167,203],[201,91]])
    water=coarse_mask(creek,.32);blocked |= water
    door_ids=set()
    for room in source['rooms']:
        inner=floors[room['id']];wall=set()
        for x,y in inner:
            for dx,dy in DIRECTIONS:
                p=(x+dx,y+dy)
                if p not in inner:wall.add(p)
        blocked |= wall;opaque |= wall
        # Connect the painted original leaf all the way to the exterior steps.
        d=next(o for o in source['components'] if o['id']==room['doorId'])
        dx,dy=to_cell(*d['foot']);entry=to_cell(*room['entry'])
        floor_y=max(y for x,y in inner if x==dx)
        for y in range(floor_y,entry[1]+1):blocked.discard((dx,y));opaque.discard((dx,y))
        buildings.append({'id':room['id'],'name':room['name'],'roofId':room['roofId'],'doorId':room['doorId'],
                          'interior':[point(p) for p in sorted(inner)],'entryX':entry[0],'entryY':entry[1]})
        door_ids.add(room['doorId'])
    # Ground at all main road entries remains a stable, three-cell connection.
    blocked -= rect(39,0,41,1)|rect(39,23,41,24)
    opaque -= rect(39,0,41,1)|rect(39,23,41,24)
    # Coarse-grid exceptions move only semantic anchors, not original art pixels.
    anchor_adjustments={'keeper-duty-desk':(33,4),'keeper-supply-chest':(34,2),'guest-supper-table':(29,11),
                        'kitchen-prep-table':(50,18)}
    owners=[]
    from PIL import Image
    for c in source['components']:
        anchor=anchor_adjustments.get(c['id'],to_cell(*c['foot']))
        kind=c['kind'];fp=set()
        if c['blocksMovement']:
            if kind in ('npc','creature','door') or c['roomId']:fp={anchor}
            else:
                physical=np.zeros((1024,1536),bool)
                for x,y in c['footprintCells']:physical[y*8:(y+1)*8,x*8:(x+1)*8]=True
                fp=coarse_mask(physical,.33) or {anchor}
        # Furniture with deliberate free-standing room positions uses one
        # cell each. The surrounding authored floor is independently retained.
        room_id=c.get('roomId') or next((r['id'] for r in source['rooms'] if c['id'] in (r['roofId'],r['doorId'],r['id']+'-shell')),None)
        if kind=='gate':fp={(38,1),(42,1)}
        support=[]
        if kind=='bridge':
            mask=np.asarray(Image.open(PACKAGE/'build/masks'/f"{c['id']}.png"))>0
            crossing=coarse_mask(mask,.22);blocked-=crossing;support=[point(p) for p in sorted(crossing&water)]
        # The art's roofs and facades are reached from the outside entry.
        owners.append({'id':c['id'],'name':c['name'],'kind':kind,'description':c['description'],
                       'roomId':room_id,'anchorX':anchor[0],'anchorY':anchor[1],
                       'mutable':c['mutable'],'blocksMovement':c['blocksMovement'],
                       'footprint':[point(p) for p in sorted(fp)],'bridgeSupport':support})
    cells=[{'x':x,'y':y,'solid':(x,y) in blocked,'opaque':(x,y) in opaque,'water':(x,y) in water,
            'interior':any((x,y) in f for f in floors.values())} for y in range(25) for x in range(80)]
    return {'schemaVersion':1,'revision':1,'id':'morrowfast','canvasWidth':1536,'canvasHeight':1024,
            'pixelsPerCell':PPU,'originX':21.25,'owners':owners,'cells':cells,'buildings':buildings}

def approaches(o):
    positions={(p['x'],p['y']) for p in o['footprint']} or {(o['anchorX'],o['anchorY'])}
    return positions|{(x+dx,y+dy) for x,y in positions for dx,dy in DIRECTIONS}

def flood(d,open_doors):
    blocked={(c['x'],c['y']) for c in d['cells'] if c['solid']}
    for o in d['owners']:
        if open_doors and o['kind']=='door':continue
        if o['blocksMovement']:blocked|={(p['x'],p['y']) for p in o['footprint']}
    seen={(40,24)};queue=deque(seen)
    while queue:
        x,y=queue.popleft()
        for dx,dy in DIRECTIONS:
            p=x+dx,y+dy
            if 0<=p[0]<80 and 0<=p[1]<25 and p not in blocked and p not in seen:seen.add(p);queue.append(p)
    return seen

def export():
    d=create_layout()
    for p in [PACKAGE/'authoring/native-layout.json',ROOT/'Assets/Resources/SceneArt/Morrowfast/definition.json']:
        p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,indent=2)+'\n')
    seen=flood(d,True)
    failed=[o['id'] for o in d['owners'] if not approaches(o)&seen]
    print(json.dumps({'owners':len(d['owners']),'cells':len(d['cells']),'openReach':len(seen),'unreachableOwners':failed},indent=2))
    grid=[]
    for y in range(25):
        grid.append(''.join('~' if next(c for c in d['cells'] if c['x']==x and c['y']==y)['water'] else '.' if (x,y) in seen else '#' for x in range(21,59)))
    print('\n'.join(grid))
    return d

if __name__=='__main__':export()
