"""Export the reviewed Felling components and explicit physical layout to Unity.

Only copies existing art bytes. The authored scene is rebuilt independently from
the copied contributions to detect omissions, stale source hashes, or path drift.
"""
from collections import deque
import hashlib
import json
import math
from pathlib import Path, PurePosixPath
import shutil
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/FellingSite'
DESTINATION = ROOT / 'Assets/Resources/SceneArt/FellingSite'
RESOURCE = 'SceneArt/FellingSite/'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def safe_path(value):
    if not isinstance(value,str) or not value or '\\' in value or ':' in value or '%' in value:
        raise ValueError('invalid source path')
    p=PurePosixPath(value)
    if p.is_absolute() or '..' in p.parts or p.suffix!='.png':
        raise ValueError('source path escapes package or is not PNG')
    return value


def inside(x,y,polygon):
    hit=False
    for i,(xi,yi) in enumerate(polygon):
        xj,yj=polygon[i-1]
        if (yi>y)!=(yj>y) and x<(xj-xi)*(y-yi)/(yj-yi)+xi:
            hit=not hit
    return hit


def reachable(walkable,start):
    if start not in walkable:return set()
    seen={start};queue=deque([start])
    while queue:
        x,y=queue.popleft()
        for dx,dy in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]:
            target=(x+dx,y+dy)
            if target not in walkable or target in seen:continue
            if dx and dy and ((x+dx,y) not in walkable or (x,y+dy) not in walkable):continue
            seen.add(target);queue.append(target)
    return seen


def build_definition(manifest,layout,physical):
    if manifest.get('schemaVersion')!=1 or layout.get('schemaVersion')!=1 or physical.get('schemaVersion')!=1:
        raise ValueError('unsupported schema')
    canvas=manifest['canvas']
    if (canvas['width'],canvas['height'],canvas['pixelsPerCell'],canvas['regionOrigin'],canvas['imageGroundOrigin'])!=(1536,1024,32,[16,0],[0,224]):
        raise ValueError('source transform drift')
    layers=[];ids=set()
    for c in manifest['components']:
        key=c['id']
        if key in ids:raise ValueError('duplicate component')
        ids.add(key)
        x,y,w,h=c['bounds']
        if any(type(v)!=int for v in [x,y,w,h]) or x<0 or y<0 or w<=0 or h<=0 or x+w>1536 or y+h>1024:
            raise ValueError('invalid source bounds')
        if not c.get('contributesToComposite'):raise ValueError('non-contributing component needs explicit policy')
        foot=c['foot'];anchor=c['footprintCells'][0]
        if not all(isinstance(v,(int,float)) and math.isfinite(v) for v in foot) or len(anchor)!=2 or not(0<=anchor[0]<80 and 0<=anchor[1]<25):
            raise ValueError('invalid foot or anchor')
        sprite=safe_path(c['sprite']['path'])
        contact=safe_path(c['contact']['path']) if c.get('contact') else None
        layers.append(dict(id=key,name=c['name'],kind=c['kind'],mutable=c['mutable'],resource=RESOURCE+'Art/'+sprite[:-4],
            contactResource=RESOURCE+'Art/'+contact[:-4] if contact else '',bounds=[x,y,w,h],anchorX=anchor[0],anchorY=anchor[1],
            footPixelY=foot[1],depth=c['depth'],blocksMovement=bool(c['mutable'] and c['kind'] in ['loose-boulder','loose-rock-cluster','standing-bank-stone'])))
    if len(layers)!=55 or sum(c['mutable'] for c in layers)!=39:
        raise ValueError('reviewed inventory changed')
    forced_block={tuple(c) for c in physical.get('explicitBlockedCells',[])}
    forced_open={tuple(c) for c in physical.get('explicitWalkableCells',[])}
    if forced_open & forced_block:raise ValueError('contradictory physical overrides')
    cells=[]
    for y in range(25):
        for x in range(80):
            hits=[p for p in physical['polygons'] if 16<=x<64 and inside((x-16+.5)*32,224+(y+.5)*32,p['polygon'])]
            solid=bool(hits) or (x,y) in forced_block
            opaque=any(p.get('blocksSight',False) for p in hits)
            if (x,y) in forced_open:solid=False;opaque=False
            cells.append(dict(x=x,y=y,solid=solid,opaque=opaque,water=any(p['kind']=='water' for p in hits)))
    landmarks=[dict(id=s['id'],x=s['cell'][0],y=s['cell'][1],kind='bare') for s in layout['sterilePositions']]
    s=layout['seventh'];landmarks.append(dict(id=s['id'],x=s['cell'][0],y=s['cell'][1],kind='seventh'))
    walk={(c['x'],c['y']) for c in cells if not c['solid']}
    for c in layers:
        if c['blocksMovement']:
            if (c['anchorX'],c['anchorY']) not in walk:
                raise ValueError('mutable boulder overlaps immutable obstruction: '+c['id'])
            walk.discard((c['anchorX'],c['anchorY']))
    seen=reachable(walk,tuple(layout['spawn']))
    for c in layers:
        if c['mutable'] and not any(max(abs(x-c['anchorX']),abs(y-c['anchorY']))<=1 for x,y in seen):
            raise ValueError('unreachable prop '+c['id'])
    for p in landmarks:
        if (p['x'],p['y']) not in seen:raise ValueError('unreachable landmark '+p['id'])
    if tuple(layout['entrance']) not in seen:raise ValueError('unreachable entrance')
    return dict(schemaVersion=1,id='felling-site',revision=1,canvasWidth=1536,canvasHeight=1024,pixelsPerCell=32,originX=16,groundOffset=224,
                baseResource=RESOURCE+'Art/base',layers=layers,cells=cells,landmarks=landmarks)


def export(source=SOURCE,destination=DESTINATION):
    source=Path(source);destination=Path(destination);build=source/'Components/build'
    manifest=json.loads((build/'manifest.json').read_text());layout=json.loads((source/'layout.json').read_text())
    physical=json.loads((source/'Integration/physical-layout.json').read_text())
    definition=build_definition(manifest,layout,physical)
    records=[]
    def copy_art(record, require_alpha=True):
        relative=safe_path(record['path']);src=(build/relative).resolve()
        if not src.is_relative_to(build.resolve()) or sha(src)!=record['sha256']:raise ValueError('missing/stale/escaping art '+relative)
        with Image.open(src) as im:
            im.load()
            if im.format!='PNG' or im.mode not in (['RGBA'] if require_alpha else ['RGB','RGBA']):
                raise ValueError('actual color PNG required; contributions need RGBA: '+relative)
        dst=destination/'Art'/relative;dst.parent.mkdir(parents=True,exist_ok=True)
        if not dst.exists() or sha(dst)!=record['sha256']:shutil.copyfile(src,dst)
        records.append(dict(source=relative,destination='Art/'+relative,sourceSha256=record['sha256']))
    copy_art(manifest['base']);copy_art(manifest['baseline'],require_alpha=False)
    for component in manifest['components']:
        copy_art(component['sprite'])
        if component.get('contact'):copy_art(component['contact'])
    intact=Image.open(destination/'Art/base.png').convert('RGBA')
    cleared=intact.copy()
    for c in sorted(manifest['components'],key=lambda c:(c['depth'],c['id'])):
        for name in ['contact','sprite']:
            if not c.get(name):continue
            im=Image.open(destination/'Art'/c[name]['path']).convert('RGBA')
            if im.size!=tuple(c['bounds'][2:]):raise ValueError('placed image shape mismatch')
            intact.alpha_composite(im,tuple(c['bounds'][:2]))
            if not c['mutable']:cleared.alpha_composite(im,tuple(c['bounds'][:2]))
    expected=Image.open(destination/'Art/baseline.png').convert('RGBA')
    delta=int(np.count_nonzero(np.any(np.asarray(intact)!=np.asarray(expected),axis=2)))
    if delta:raise ValueError('intact composition mismatch: '+str(delta))
    if not np.array_equal(np.asarray(cleared),np.asarray(Image.open(build/'cleaned-environment.png').convert('RGBA'))):raise ValueError('all-removed backing mismatch')
    destination.mkdir(parents=True,exist_ok=True)
    (destination/'definition.json').write_text(json.dumps(definition,indent=2)+'\n')
    open_cells={(c['x'],c['y']) for c in definition['cells'] if not c['solid']}
    open_cells-={(c['anchorX'],c['anchorY']) for c in definition['layers'] if c['blocksMovement']}
    report=dict(schemaVersion=1,status='passed',componentCount=55,mutableCount=39,intactChangedPixels=delta,allRemovedMatchesBacking=True,
                reachableCells=len(reachable(open_cells,tuple(layout['spawn']))),files=records,
                sourceManifestSha256=sha(build/'manifest.json'),layoutSha256=sha(source/'layout.json'),physicalLayoutSha256=sha(source/'Integration/physical-layout.json'),
                definitionSha256=sha(destination/'definition.json'),exporterSha256=sha(Path(__file__)))
    (destination/'provenance.json').write_text(json.dumps(report,indent=2)+'\n')
    return report


if __name__=='__main__':
    result=export()
    print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
