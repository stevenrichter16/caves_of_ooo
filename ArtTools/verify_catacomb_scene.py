#!/usr/bin/env python3
"""Independently verify Catacomb exports, source provenance and browser mechanics.

Reads existing assets; never rebuilds artwork or opens Unity. Required gates fail
closed on absent assets, stale inputs, malformed metadata, or skipped tests.
"""
from __future__ import annotations
import argparse
from collections import deque
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import time
from urllib.parse import unquote

import numpy as np
from PIL import Image, ImageDraw

REPO=Path(__file__).resolve().parents[1]
PACKAGE=REPO/'ArtSource/CatacombVillage'
SHA=re.compile(r'^[0-9a-f]{64}$')
ID=re.compile(r'^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$')


def require(value,message):
    if not value:raise ValueError(message)


def sha256(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def utc_now():return datetime.now(timezone.utc).isoformat(timespec='seconds')


def load_json(path):
    def pairs(items):
        result={}
        for key,value in items:
            require(key not in result,'Duplicate JSON key: '+key);result[key]=value
        return result
    def invalid(value):raise ValueError('Nonfinite JSON value: '+value)
    return json.loads(Path(path).read_text(),object_pairs_hook=pairs,parse_constant=invalid)


def confined(root,name):
    require(isinstance(name,str) and bool(name) and name==name.strip()
        and not any(c in name for c in ('\\','\x00',':'))
        and not PurePosixPath(name).is_absolute() and unquote(name)==name
        and all(p not in ('','.','..') for p in name.split('/')),
        f'Unsafe relative asset path: {name!r}')
    path=(Path(root)/name).resolve()
    try:path.relative_to(Path(root).resolve())
    except ValueError:raise ValueError('Asset path escapes root: '+name) from None
    return path


def number(value):return type(value) is int or type(value) is float and math.isfinite(value)
def vector(value,n,integer=False):
    return isinstance(value,list) and len(value)==n and all(type(v) is int if integer else number(v) for v in value)


def asset_record(record,label,root=None):
    require(isinstance(record,dict),'Missing asset record: '+label)
    confined(root or Path('/__catacomb_virtual_root__'),record.get('path'))
    require(isinstance(record.get('sha256'),str) and SHA.fullmatch(record['sha256']), 'Missing/invalid asset hash: '+label)


def read_png(root,record,size,role='sprite'):
    asset_record(record,role,root);path=confined(root,record['path'])
    require(sha256(path)==record['sha256'],'Asset hash mismatch: '+record['path'])
    with Image.open(path) as image:
        require(image.format=='PNG','Asset is not PNG: '+record['path'])
        require(list(image.size)==list(size),'Asset dimensions mismatch: '+record['path'])
        if role=='mask':
            require(image.mode in ('1','L','RGBA'),'Mask must be grayscale or RGBA: '+record['path'])
            data=np.asarray(image.convert('L')) if image.mode!='RGBA' else np.asarray(image)[:,:,3]
            require(set(np.unique(data)).issubset({0,255}),'Mask must be binary: '+record['path'])
            return data.copy()
        require(image.mode=='RGBA','Asset must have actual RGBA channels: '+record['path'])
        data=np.asarray(image).copy()
    alpha=set(np.unique(data[:,:,3]))
    require(alpha.issubset({0,255}),'Asset alpha must be binary: '+record['path'])
    if role=='sprite':
        require(0 in alpha,'Sprite needs transparent pixels: '+record['path'])
        require(255 in alpha,'Sprite needs opaque pixels: '+record['path'])
    elif role=='opaque':require(alpha=={255},'Background must be opaque: '+record['path'])
    return data


def validate_scene(data,catalog=None,root=None):
    require(isinstance(data,dict),'Manifest must be an object')
    require(type(data.get('schemaVersion')) is int and data['schemaVersion']==1,'Unsupported scene schema')
    for key in ('id','title'):require(isinstance(data.get(key),str) and data[key].strip(),'Missing '+key)
    canvas=data.get('canvas');nav=data.get('navigation')
    require(isinstance(canvas,dict) and all(type(canvas.get(k)) is int and canvas[k]>0 for k in ('width','height','pixelsPerCell')),'Invalid canvas')
    require(isinstance(nav,dict) and all(type(nav.get(k)) is int and nav[k]>0 for k in ('width','height')),'Invalid navigation')
    require(nav['width']*canvas['pixelsPerCell']==canvas['width'] and nav['height']*canvas['pixelsPerCell']==canvas['height'],'Navigation dimensions mismatch')
    inside=lambda p:vector(p,2,True) and 0<=p[0]<nav['width'] and 0<=p[1]<nav['height']
    for key in ('base','baseline','underlay','inferenceMask'):asset_record(data.get(key),key,root)
    require(isinstance(nav.get('walkableCells'),list) and all(inside(p) for p in nav['walkableCells']),'Invalid walkable cells')
    walkable=set(map(tuple,nav['walkableCells']))
    require(len(walkable)==len(nav['walkableCells']),'Duplicate walkable cells')
    require(inside(nav.get('spawn')) and tuple(nav['spawn']) in walkable,'Spawn outside walkable terrain')
    source_ids=None
    if catalog is not None:
        require(isinstance(catalog,dict) and isinstance(catalog.get('assets'),list),'Invalid source catalog')
        source_ids=set()
        for a in catalog['assets']:
            require(isinstance(a,dict) and isinstance(a.get('id'),str) and a['id'] not in source_ids,'Duplicate/invalid catalog source id')
            source_ids.add(a['id'])
    require(isinstance(data.get('components'),list) and data['components'],'Missing components')
    ids=set();blocked=set();jobs=[]
    for item in data['components']:
        require(isinstance(item,dict),'Component must be an object')
        id=item.get('id');require(isinstance(id,str) and ID.fullmatch(id) and id not in ids,'Invalid/duplicate instance id');ids.add(id)
        require(isinstance(item.get('name'),str) and item['name'].strip(),'Missing name: '+id)
        require(source_ids is None or item.get('sourceAssetId') in source_ids,'Unknown source asset: '+id)
        for field in ('mutable','defaultVisible','blocksMovement'):require(type(item.get(field)) is bool,'Invalid boolean: '+id+'.'+field)
        require(number(item.get('depth')),'Nonfinite depth: '+id)
        b=item.get('bounds');require(vector(b,4,True),'Invalid bounds: '+id)
        x,y,w,h=b;require(min(x,y)>=0 and min(w,h)>0 and x+w<=canvas['width'] and y+h<=canvas['height'],'Out-of-canvas bounds: '+id)
        foot,pivot=item.get('foot'),item.get('pivot')
        require(vector(foot,2) and vector(pivot,2) and all(0<=v<=1 for v in pivot),'Invalid foot/pivot: '+id)
        require(abs(x+pivot[0]*w-foot[0])<1e-7 and abs(y+(1-pivot[1])*h-foot[1])<1e-7,'Foot/pivot mismatch: '+id)
        cells=item.get('footprintCells');require(isinstance(cells,list) and all(inside(p) for p in cells),'Invalid footprint cells: '+id)
        require(len(set(map(tuple,cells)))==len(cells),'Duplicate footprint cells: '+id)
        require(not item['blocksMovement'] or cells,'Blocking component needs a footprint: '+id)
        if item['blocksMovement'] and item['defaultVisible']:blocked.update(map(tuple,cells))
        asset_record(item.get('sprite'),id+'.sprite',root)
        if item.get('contact') is not None:asset_record(item['contact'],id+'.contact',root)
        if item['mutable']:require(item.get('mask') is not None,'Mutable component needs mask: '+id)
        if item.get('mask') is not None:asset_record(item['mask'],id+'.mask',root)
    require(tuple(nav['spawn']) not in blocked,'Spawn is blocked')
    open_cells=walkable-blocked;seen={tuple(nav['spawn'])};queue=deque(seen)
    while queue:
        x,y=queue.popleft()
        for dx,dy in ((0,1),(1,0),(0,-1),(-1,0)):
            q=x+dx,y+dy
            if q in open_cells and q not in seen:seen.add(q);queue.append(q)
    waypoints=nav.get('destinations',nav.get('waypoints',[]));require(isinstance(waypoints,list),'Waypoints must be a list')
    for waypoint in waypoints:
        target=waypoint.get('cell') if isinstance(waypoint,dict) else waypoint
        require(inside(target) and tuple(target) in seen,'Waypoint is unreachable')
    if data.get('player') is not None:
        player=data['player'];asset_record(player,'player',root)
        require(source_ids is None or player.get('sourceAssetId') in source_ids,'Unknown player source asset')
        require(all(type(player.get(k)) is int and player[k]>0 for k in ('frameWidth','frameHeight','columns','frameCount')),'Invalid player sheet dimensions')
        require(number(player.get('scale')) and player['scale']>0 and vector(player.get('pivot'),2),'Invalid player scale/pivot')
    return {'ok':True,'componentCount':len(ids),'mutableCount':sum(c['mutable'] for c in data['components']),
        'reachableCells':len(seen),'waypointCount':len(waypoints)}


def strict_assets(build,catalog):
    data=load_json(build/'scene.json');report=validate_scene(data,catalog,build)
    size=[data['canvas']['width'],data['canvas']['height']];count=0
    for key in ('base','baseline','underlay','inferenceMask'):
        read_png(build,data[key],size,role='mask' if key=='inferenceMask' else 'opaque');count+=1
    reveal=np.zeros((size[1],size[0]),dtype=np.uint8)
    for c in data['components']:
        sprite=read_png(build,c['sprite'],c['bounds'][2:]);count+=1
        if c.get('contact') is not None:read_png(build,c['contact'],c['bounds'][2:]);count+=1
        if c.get('mask') is not None:
            mask=read_png(build,c['mask'],c['bounds'][2:],role='mask');count+=1
            require(np.array_equal(mask,sprite[:,:,3]),'Support mask differs from sprite alpha: '+c['id'])
        if c['mutable']:
            x,y,w,h=c['bounds'];reveal[y:y+h,x:x+w]|=sprite[:,:,3]
    require(np.array_equal(reveal,read_png(build,data['inferenceMask'],size,role='mask')),
        'Inference overlay differs from declared mutable sprite support')
    if data.get('player'):
        p=data['player'];size=[p['frameWidth']*p['columns'],p['frameHeight']*math.ceil(p['frameCount']/p['columns'])]
        read_png(build,p,size);count+=1
    return dict(report,assetChecks=count)


def source_derivation(package=PACKAGE,live_source=None,extractor_path=None):
    """Recreate only the source selection recipe independently of extraction code."""
    root=Path(package)/'source-assets';catalog=load_json(root/'catalog.json')
    live_source=Path(live_source or REPO/'Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png')
    extractor_path=Path(extractor_path or REPO/'ArtTools/catacomb_extract.py')
    authoring_path=Path(package)/'source-authoring.json';authoring=load_json(authoring_path)
    require(catalog.get('authoringSha256')==sha256(authoring_path),'Stale source authoring hash')
    require(catalog.get('extractorSha256')==sha256(extractor_path),'Stale source extractor hash')
    digest=sha256(live_source);src=catalog.get('source',{})
    require(src.get('sha256')==digest and sha256(confined(root,src.get('path')))==digest,'Source sheet hash mismatch')
    with Image.open(live_source) as image:
        require(image.mode=='RGBA','Source must preserve actual alpha')
        source=np.asarray(image).copy();size=image.size
    require([src.get('width'),src.get('height')]==list(size),'Catalog source dimensions differ')
    entries=catalog.get('assets');require(isinstance(entries,list) and entries,'Missing source catalog assets')
    authored={e['id']:e for e in authoring['assets']}
    require(len(authored)==len(authoring['assets']),'Duplicate source authoring id')
    ids=[e.get('id') for e in entries]
    require(len(ids)==len(set(ids)) and set(ids)==set(authored),'Catalog/source authoring identities differ')
    owners=np.zeros(source.shape[:2],dtype=np.uint8);visible=0
    for entry in entries:
        id=entry['id'];recipe=authored[id];bounds=entry.get('sourceBounds')
        require(vector(bounds,4,True),'Invalid source bounds: '+id)
        x,y,w,h=bounds
        require(min(x,y)>=0 and min(w,h)>0 and x+w<=size[0] and y+h<=size[1],'Source bounds outside sheet: '+id)
        require(entry.get('sourceSha256')==digest,'Stale source SHA: '+id)
        require([entry.get('width'),entry.get('height')]==[w,h],'Source dimensions mismatch: '+id)
        require(entry.get('sourceRoi')==recipe['bounds'],'Source ROI metadata drift: '+id)
        threshold=recipe.get('alphaThreshold',128)
        require(type(threshold) is int and 1<=threshold<=254 and entry.get('alphaThreshold')==threshold,'Source threshold drift: '+id)
        rx,ry,rw,rh=recipe['bounds'];expected_mask=np.zeros(source.shape[:2],bool)
        expected_mask[ry:ry+rh,rx:rx+rw]=source[ry:ry+rh,rx:rx+rw,3]>=threshold
        for polygon in recipe.get('excludePolygons',[]):
            exclusion=Image.new('1',size);ImageDraw.Draw(exclusion).polygon([tuple(p) for p in polygon],fill=1)
            expected_mask&=~np.asarray(exclusion,dtype=bool)
        require(int(expected_mask.sum())==int(expected_mask[y:y+h,x:x+w].sum()),'Crop discards selected source pixels: '+id)
        crop=source[y:y+h,x:x+w].copy();mask=expected_mask[y:y+h,x:x+w]
        crop[:,:,3]=mask.astype(np.uint8)*255;crop[~mask,:3]=0
        actual=read_png(root,{'path':entry['path'],'sha256':entry.get('spriteSha256')},[w,h])
        require(np.array_equal(actual,crop),'Source sprite derivation differs: '+id)
        require(not np.any(owners[expected_mask]),'Overlapping source pixel ownership: '+id)
        owners[expected_mask]=1;visible+=int(mask.sum())
        foot=entry.get('foot');pivot=entry.get('pivot')
        require(vector(foot,2) and vector(pivot,2),'Invalid source pivot: '+id)
        require(np.allclose(foot,[recipe['foot'][0]-x,recipe['foot'][1]-y],rtol=0,atol=1e-8),'Source foot drift: '+id)
        require(np.allclose(pivot,[foot[0]/w,1-foot[1]/h],rtol=0,atol=1e-8),'Source pivot drift: '+id)
    return {'ok':True,'sourceAssetCount':len(entries),'visibleSourcePixels':visible,
        'sourceSha256':digest,'sourceRgbAndAlphaExact':True,'noSourcePixelOwnershipOverlap':True,
        'authoringSha256':catalog['authoringSha256'],'extractorSha256':catalog['extractorSha256']}


def placed_derivation(build,source_root=None):
    source_root=Path(source_root or PACKAGE/'source-assets');catalog=load_json(source_root/'catalog.json')
    data=load_json(build/'scene.json');validate_scene(data,catalog,build)
    assets={a['id']:a for a in catalog['assets']};count=0
    def check(item,player=False):
        nonlocal count
        id='player' if player else item['id'];source=assets[item['sourceAssetId']]
        require(item.get('sourceSpriteSha256')==source.get('spriteSha256'),'Placed source hash mismatch: '+id)
        require(item.get('resize')=='nearest','Missing exact nearest resize record: '+id)
        scale=item.get('sourceScale') if player else item.get('scale')
        require(number(scale) and scale>0,'Invalid source resize scale: '+id)
        size=[source['width'],source['height']]
        require(item.get('sourceSize')==size,'Source size metadata mismatch: '+id)
        original=read_png(source_root,{'path':source['path'],'sha256':source['spriteSha256']},size)
        expected_size=[max(1,round(size[0]*scale)),max(1,round(size[1]*scale))]
        expected=np.asarray(Image.fromarray(original,'RGBA').resize(expected_size,Image.Resampling.NEAREST))
        if player:
            require(item['columns']==item['frameCount']==1,'Player must declare the single source pose honestly')
            record=item;placed_size=[item['frameWidth'],item['frameHeight']]
        else:
            record=item['sprite'];placed_size=item['bounds'][2:]
            require(item.get('placedSize')==placed_size,'Placed size metadata mismatch: '+id)
        require(placed_size==expected_size,'Recorded scale does not produce placed size: '+id)
        actual=read_png(build,record,placed_size)
        require(np.array_equal(actual,expected),'Placed sprite derivation differs: '+id);count+=1
    for component in data['components']:check(component)
    if data.get('player'):check(data['player'],True)
    return {'ok':True,'componentCount':len(data['components']),'sourceDerivativeCount':count,'nearestResizeRGBAExact':True}


def current_provenance(build,package=PACKAGE):
    data=load_json(build/'scene.json');validate_scene(data)
    actual_sources={'sourceSheetSha256':REPO/'Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png',
        'catalogSha256':Path(package)/'source-assets/catalog.json','layoutSha256':Path(package)/'scene-layout.json',
        'backgroundSha256':Path(package)/'generated/empty-cavern-v1.png','producerSha256':REPO/'ArtTools/catacomb_scene.py'}
    declared=data.get('sources');require(isinstance(declared,dict),'Missing current input provenance')
    checks=[]
    for field,path in actual_sources.items():
        digest=sha256(path);require(declared.get(field)==digest,'Stale build input: '+field)
        checks.append({'field':field,'path':str(path),'sha256':digest})
    sheet=data.get('sourceSheet');asset_record(sheet,'sourceSheet',build)
    require(sheet['sha256']==declared['sourceSheetSha256']==sha256(confined(build,sheet['path'])),'Archived source sheet differs')
    with Image.open(actual_sources['backgroundSha256']) as image:background=np.asarray(image.convert('RGBA'))
    base=read_png(build,data['base'],[data['canvas']['width'],data['canvas']['height']],role='opaque')
    require(np.array_equal(base,background),'Base does not equal complete generated substrate')
    layout=load_json(actual_sources['layoutSha256'])
    for field in ('schemaVersion','id','title','canvas','navigation'):
        require(data.get(field)==layout.get(field),'Layout/scene metadata drift: '+field)
    expected={c['id']:c for c in layout['components']}
    require(len(expected)==len(layout['components']) and set(expected)=={c['id'] for c in data['components']},'Layout/scene component identities differ')
    for c in data['components']:
        for field,value in expected[c['id']].items():require(c.get(field)==value,'Layout/scene component drift: '+c['id']+'.'+field)
    require(data['player']['sourceAssetId']==layout['player']['sourceAssetId'] and data['player']['sourceScale']==layout['player']['scale'],'Player source layout drift')
    return {'ok':True,'inputs':checks,'baseMatchesCurrentSubstrate':True,'sceneMatchesCurrentLayout':True}


def reassembly(build):
    data=load_json(build/'scene.json');validate_scene(data)
    size=[data['canvas']['width'],data['canvas']['height']]
    base=read_png(build,data['base'],size,role='opaque')
    layers=[]
    for c in sorted(data['components'],key=lambda c:(c['depth'],c['id'])):
        if not c['defaultVisible']:continue
        arrays=[read_png(build,c[k],c['bounds'][2:]) for k in ('contact','sprite') if c.get(k)]
        layers.append((c,arrays))
    def compose(hidden=frozenset()):
        result=base.copy()
        for c,arrays in layers:
            if c['id'] in hidden:continue
            x,y,w,h=c['bounds'];roi=result[y:y+h,x:x+w]
            for a in arrays:roi[a[:,:,3]==255]=a[a[:,:,3]==255]
        return result
    intact=compose();baseline=read_png(build,data['baseline'],size,role='opaque')
    require(np.array_equal(intact,baseline),'Independent reassembly differs from baseline')
    if (build/'reconstructed.png').exists():
        with Image.open(build/'reconstructed.png') as image:
            require(image.mode=='RGBA' and np.array_equal(np.asarray(image),intact),'Saved reconstructed image is stale')
    mutable={c['id'] for c in data['components'] if c['mutable']}
    underlay=read_png(build,data['underlay'],size,role='opaque')
    require(np.array_equal(compose(mutable),underlay),'All-mutable removal differs from clean underlay')
    removals=[]
    for c,arrays in layers:
        if not c['mutable']:continue
        without=compose({c['id']});delta=np.any(without!=intact,axis=2)
        x,y,w,h=c['bounds'];support=np.zeros(delta.shape,bool)
        support[y:y+h,x:x+w]=np.logical_or.reduce([a[:,:,3]>0 for a in arrays])
        require(not np.any(delta&~support),'Removal changes pixels outside alpha support: '+c['id'])
        removals.append({'id':c['id'],'changedPixels':int(delta.sum()),'outsideAlphaChangedPixels':int((delta&~support).sum())})
    return {'ok':True,'intactChangedPixels':0,'allMutableRemovedExact':True,'individualRemovals':removals}


def atlas_check(build):
    data=load_json(build/'scene.json');validate_scene(data)
    if data.get('atlas'):
        asset_record(data['atlas'],'atlas metadata',build)
        atlas_path=confined(build,data['atlas']['path']);require(sha256(atlas_path)==data['atlas']['sha256'],'Atlas metadata hash mismatch')
    else:atlas_path=build/'prop-atlas.json'
    atlas=load_json(atlas_path)
    require(type(atlas.get('schemaVersion')) is int and atlas['schemaVersion']==1,'Unsupported atlas schema')
    require(vector(atlas.get('size'),2,True) and min(atlas['size'])>0,'Invalid atlas dimensions')
    pixels=read_png(build,{'path':atlas.get('path'),'sha256':atlas.get('sha256')},atlas['size'])
    expected={c['id']:c for c in data['components']}
    entries=atlas.get('entries');require(isinstance(entries,list),'Atlas entries must be a list')
    seen=set();occupied=np.zeros(pixels.shape[:2],bool)
    for entry in entries:
        require(isinstance(entry,dict),'Atlas entry must be an object')
        id=entry.get('id');require(isinstance(id,str) and id in expected and id not in seen,'Unknown/duplicate atlas id')
        seen.add(id);c=expected[id];rect=entry.get('sourceRectTopLeft')
        require(vector(rect,4,True),'Atlas rectangle must be integers: '+id)
        x,y,w,h=rect
        require(min(x,y)>=0 and min(w,h)>0 and x+w<=pixels.shape[1] and y+h<=pixels.shape[0],'Atlas rectangle outside image: '+id)
        require(not np.any(occupied[y:y+h,x:x+w]),'Atlas rectangles overlap: '+id)
        occupied[y:y+h,x:x+w]=True
        require([w,h]==c['bounds'][2:],'Atlas sprite dimensions differ: '+id)
        require(entry.get('unityRectBottomLeft')==[x,pixels.shape[0]-y-h,w,h],'Atlas bottom-left rectangle differs: '+id)
        require(entry.get('pivot')==c['pivot'],'Atlas pivot differs: '+id)
        require(entry.get('sourceSpriteSha256')==c['sprite']['sha256'],'Atlas source hash differs: '+id)
        sprite=read_png(build,c['sprite'],[w,h])
        require(np.array_equal(pixels[y:y+h,x:x+w],sprite),'Atlas pixels differ from sprite: '+id)
    require(seen==set(expected),'Atlas omitted component sprites')
    require(not np.any(pixels[~occupied]),'Atlas contains undeclared pixels')
    return {'ok':True,'spriteCount':len(seen),'size':atlas['size'],'exactRGBA':True,'completeUniqueIds':True}


def counts_for(name,output):
    if name=='python-tests':
        match=re.search(r'Ran (\d+) tests?',output)
        return {'total':int(match.group(1))} if match else None
    counts={k:int(v) for k,v in re.findall(r'^# (tests|pass|fail|skipped) (\d+)\s*$',output,re.MULTILINE)}
    return counts or None


def run_command(name,command,reports,timeout):
    started=time.monotonic();entry={'name':name,'command':command,'log':name+'.log','startedAt':utc_now()}
    try:
        p=subprocess.run(command,cwd=REPO,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,errors='replace',timeout=timeout)
        output=p.stdout;counts=counts_for(name,output)
        passed=p.returncode==0 and counts and counts.get('total',counts.get('tests',0))>0 and not counts.get('skipped',0) and not counts.get('fail',0) and not re.search(r'\bskipped=\d+',output)
        entry.update(status='passed' if passed else 'failed',exitCode=p.returncode,tests=counts)
    except (OSError,subprocess.TimeoutExpired) as exc:
        output=str(exc);entry.update(status='failed',error=output)
    (reports/entry['log']).write_text(output);entry['durationSeconds']=round(time.monotonic()-started,3)
    print(name+': '+entry['status'].upper(),flush=True);return entry


def run_check(name,check,reports):
    started=time.monotonic();entry={'name':name,'log':name+'.log','startedAt':utc_now()}
    try:entry.update(status='passed',findings=check());output=json.dumps(entry['findings'],indent=2,allow_nan=False)+'\n'
    except Exception as exc:entry.update(status='failed',error=type(exc).__name__+': '+str(exc));output=entry['error']+'\n'
    (reports/entry['log']).write_text(output);entry['durationSeconds']=round(time.monotonic()-started,3)
    print(name+': '+entry['status'].upper(),flush=True);return entry


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--timeout-seconds',type=int,default=180)
    args=parser.parse_args(argv);require(args.timeout_seconds>0,'Timeout must be positive')
    started=time.monotonic();reports=PACKAGE/'reports';reports.mkdir(parents=True,exist_ok=True);build=PACKAGE/'build'
    watched=[p for p in PACKAGE.rglob('*') if p.is_file() and not p.is_relative_to(reports) and 'inspection' not in p.parts]
    watched+=list((REPO/'ArtTools').glob('*catacomb*'))
    watched.append(REPO/'Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png')
    before={str(p):sha256(p) for p in watched if p.is_file()}
    report={'schemaVersion':1,'id':'catacomb-offline-verification','startedAt':utc_now(),'unityExecuted':False,
        'scope':'Source derivation, current provenance, exported RGBA, immutable reassembly and offline browser mechanics. Visual quality is reviewed separately.',
        'toolchain':{'python':sys.executable,'node':shutil.which('node')},'steps':[]}
    report['steps'].append(run_command('python-tests',[sys.executable,'-m','unittest','ArtTools.test_catacomb_extract','ArtTools.test_catacomb_scene','ArtTools.test_catacomb_adversarial'],reports,args.timeout_seconds))
    report['steps'].append(run_command('node-tests',['node','--test','--test-reporter=tap',str(REPO/'ArtTools/test_catacomb_browser.cjs')],reports,args.timeout_seconds))
    for name,check in [('source-derivation',source_derivation),('current-provenance',lambda:current_provenance(build)),
        ('strict-assets',lambda:strict_assets(build,load_json(PACKAGE/'source-assets/catalog.json'))),
        ('placed-derivation',lambda:placed_derivation(build)),('reassembly',lambda:reassembly(build)),('exact-atlas',lambda:atlas_check(build))]:
        report['steps'].append(run_check(name,check,reports))
    after={str(p):sha256(p) if p.is_file() else None for p in watched}
    report.update(finishedAt=utc_now(),durationSeconds=round(time.monotonic()-started,3),sourceAndOutputSnapshot=after,changedDuringRun=[p for p in before if before[p]!=after[p]])
    report['status']='passed' if not report['changedDuringRun'] and all(s['status']=='passed' for s in report['steps']) else 'failed'
    (reports/'verification.json').write_text(json.dumps(report,indent=2,allow_nan=False)+'\n')
    print('OVERALL: '+report['status'].upper());return 0 if report['status']=='passed' else 1


if __name__=='__main__':sys.exit(main())
