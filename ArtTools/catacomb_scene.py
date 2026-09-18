"""Build a layered catacomb study from exact sheet extractions and a complete floor.

No Unity files are touched. The source sprite palette is retained; resized instances
are explicitly marked nearest-neighbor derivatives, not native source-pixel copies.
"""
from __future__ import annotations
from collections import deque
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=ROOT/'ArtSource/CatacombVillage'
SOURCE=ROOT/'Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png'

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def write_json(path,data):path.write_text(json.dumps(data,indent=2,allow_nan=False)+'\n')
def resize_sprite(image,scale):
    if isinstance(scale,bool) or not isinstance(scale,(int,float)) or not math.isfinite(scale) or scale<=0:
        raise ValueError('scale must be positive and finite')
    return image.convert('RGBA').resize((max(1,round(image.width*scale)),max(1,round(image.height*scale))),Image.Resampling.NEAREST)

def validate_removed(layers,removed):
    ids=[e['id'] for e in layers]
    if len(set(ids))!=len(ids):raise ValueError('duplicate component ID')
    if set(removed)-set(ids):raise ValueError('unknown removed component')
    if any(e['id'] in removed and not e['mutable'] for e in layers):raise ValueError('fixed component cannot be removed')

def compose(base,layers,removed=()):
    validate_removed(layers,removed)
    result=base.convert('RGBA').copy()
    for e in sorted(layers,key=lambda e:(e['depth'],e['id'])):
        x,y,w,h=e['bounds'];sprite=e['image']
        if any(type(v) is not int for v in e['bounds']) or x<0 or y<0 or w<1 or h<1 or x+w>base.width or y+h>base.height:
            raise ValueError('component bounds outside canvas')
        if sprite.size!=(w,h):raise ValueError('sprite dimensions disagree with bounds')
        if e['id'] not in removed:result.alpha_composite(sprite,(x,y))
    return result

def blocked_cells(components,removed=()):
    validate_removed(components,removed)
    return {tuple(p) for e in components if e['blocksMovement'] and e['id'] not in removed for p in e['footprintCells']}

def reachable(cells,start):
    start=tuple(start);cells=set(cells)
    if start not in cells:return set()
    found={start};queue=deque([start])
    while queue:
        x,y=queue.popleft()
        for dx,dy in ((0,-1),(1,0),(0,1),(-1,0),(1,-1),(1,1),(-1,1),(-1,-1)):
            q=(x+dx,y+dy)
            if q in found or q not in cells:continue
            if dx and dy and ((x+dx,y) not in cells or (x,y+dy) not in cells):continue
            found.add(q);queue.append(q)
    return found

def asset_record(path,out):return {'path':path.relative_to(out).as_posix(),'sha256':sha(path)}

def atlas_export(out,components):
    width=1024;padding=2;positions={};x=y=row_h=0
    for c in sorted(components,key=lambda c:(-c['bounds'][3],c['id'])):
        w,h=c['bounds'][2:]
        if w+padding*2>width:raise ValueError('component too wide for atlas')
        if x+w+padding*2>width:x=0;y+=row_h;row_h=0
        positions[c['id']]=(x+padding,y+padding,w,h)
        x+=w+padding*2;row_h=max(row_h,h+padding*2)
    height=math.ceil((y+row_h)/32)*32
    atlas=Image.new('RGBA',(width,height));entries=[]
    for c in components:
        x,y,w,h=positions[c['id']]
        with Image.open(out/c['sprite']['path']) as im:atlas.alpha_composite(im,(x,y))
        entries.append({'id':c['id'],'sourceRectTopLeft':[x,y,w,h],
            'unityRectBottomLeft':[x,height-y-h,w,h],'pivot':c['pivot'],'sourceSpriteSha256':c['sprite']['sha256']})
    path=out/'scene-atlas.png';atlas.save(path)
    info={'schemaVersion':1,**asset_record(path,out),'size':[width,height],'pixelsPerUnit':32,'paddingPixels':padding,'entries':entries}
    write_json(out/'scene-atlas.json',info)
    return asset_record(out/'scene-atlas.json',out)

def build():
    layout_path=PACKAGE/'scene-layout.json';catalog_path=PACKAGE/'source-assets/catalog.json'
    ground_path=PACKAGE/'generated/empty-cavern-v1.png'
    layout=json.loads(layout_path.read_text());catalog=json.loads(catalog_path.read_text())
    assets={e['id']:e for e in catalog['assets']}
    out=PACKAGE/'build';out.mkdir(exist_ok=True)
    for folder in ('sprites','masks','comparisons','reports'):(out/folder).mkdir(exist_ok=True)
    base=Image.open(ground_path).convert('RGBA');cw,ch=base.size
    if [cw,ch]!=[layout['canvas']['width'],layout['canvas']['height']]:raise ValueError('background canvas mismatch')
    base.save(out/'base.png');shutil.copy2(SOURCE,out/'source-sheet.png')
    layers=[];components=[];inference=np.zeros((ch,cw),bool)
    for e in layout['components']:
        if not re.fullmatch('[a-z][a-z0-9-]*',e['id']):raise ValueError('unsafe component id')
        a=assets[e['sourceAssetId']]
        path=(PACKAGE/'source-assets'/a['path']).resolve();path.relative_to((PACKAGE/'source-assets').resolve())
        if sha(path)!=a['spriteSha256']:raise ValueError('stale extracted sprite: '+a['id'])
        source=Image.open(path).convert('RGBA');sprite=resize_sprite(source,e['scale'])
        w,h=sprite.size;fx,fy=e['foot']
        x=round(fx-a['foot'][0]*w/source.width);y=round(fy-a['foot'][1]*h/source.height)
        bounds=[x,y,w,h];pivot=[(fx-x)/w,1-(fy-y)/h]
        if not all(0<=n<=1 for n in pivot):raise ValueError('foot anchor outside sprite')
        sp=out/'sprites'/(e['id']+'.png');sprite.save(sp)
        alpha=np.asarray(sprite)[...,3]
        if not np.all(np.isin(alpha,[0,255])):raise ValueError('nonbinary placed alpha')
        mp=out/'masks'/(e['id']+'.png');Image.fromarray(alpha).save(mp)
        c={**e,'name':e['id'].replace('-',' ').capitalize(),'kind':a['kind'],'defaultVisible':True,
            'bounds':bounds,'depth':e.get('depth',fy),'pivot':pivot,'sprite':asset_record(sp,out),'contact':None,
            'mask':asset_record(mp,out),'sourceSpriteSha256':a['spriteSha256'],'sourceBounds':a['sourceBounds'],
            'resize':'nearest','sourceSize':list(source.size),'placedSize':[w,h],
            'provenance':'source-sheet-rgb; source-alpha normalized; nearest-neighbor placement',
            'repair':{'surface':'complete-cavern-substrate','inferred':e['mutable'],'sourceProvenanceId':'generated-substrate'}}
        layers.append({**c,'image':sprite});components.append(c)
        if e['mutable']:
            if x<0 or y<0 or x+w>cw or y+h>ch:raise ValueError('placed sprite outsidecanvas')
            inference[y:y+h,x:x+w]|=alpha>0
    baseline=compose(base,layers);mutable={e['id'] for e in components if e['mutable']}
    underlay=compose(base,layers,mutable)
    baseline.save(out/'baseline.png');baseline.save(out/'reconstructed.png');underlay.save(out/'underlay.png')
    Image.fromarray(inference.astype(np.uint8)*255).save(out/'inference-mask.png')
    nav=layout['navigation'];cells={tuple(p) for p in nav['walkableCells']}
    blocked=blocked_cells(components);reached=reachable(cells-blocked,nav['spawn'])
    if not reached:raise ValueError('blocked spawn')
    routes=[]
    for d in nav['destinations']:
        ok=tuple(d['cell']) in reached;routes.append({**d,'reachable':ok})
        if not ok:raise ValueError('unreachable destination: '+d['id'])
    # Player is another documented derivative of a source-sheet pose, never invented animation.
    player_def=layout['player'];pa=assets[player_def['sourceAssetId']]
    player=resize_sprite(Image.open(PACKAGE/'source-assets'/pa['path']),player_def['scale'])
    player.save(out/'player.png');pw,ph=player.size
    scene={k:layout[k] for k in ('schemaVersion','id','title','canvas','navigation','notes')}
    scene.update(base={**asset_record(out/'base.png',out),'provenance':'generated complete substrate'},
        baseline=asset_record(out/'baseline.png',out),underlay=asset_record(out/'underlay.png',out),
        inferenceMask={**asset_record(out/'inference-mask.png',out),'meaning':'potential reveal support beneath mutable source sprites; the full floor is newly authored'},
        sourceSheet=asset_record(out/'source-sheet.png',out),
        player={**asset_record(out/'player.png',out),'frameWidth':pw,'frameHeight':ph,'columns':1,'frameCount':1,
            'pivot':[pa['pivot'][0],1-pa['pivot'][1]],'pivotConvention':'top-left','scale':1,'sourceAssetId':pa['id'],'sourceSpriteSha256':pa['spriteSha256'],
            'resize':'nearest','sourceScale':player_def['scale'],'sourceSize':pa['bounds'][2:]},
        components=components,
        sources={'sourceSheetSha256':sha(SOURCE),'catalogSha256':sha(catalog_path),'layoutSha256':sha(layout_path),
            'backgroundSha256':sha(ground_path),'producerSha256':sha(Path(__file__))},
        report={'path':'reports/composition.json'})
    scene['atlas']=atlas_export(out,components)
    # Verify pixels by independently reading exported files instead of reusing render memory.
    exported=[{**c,'image':Image.open(out/c['sprite']['path']).convert('RGBA')} for c in components]
    actual=compose(Image.open(out/'base.png'),exported)
    changed=int(np.any(np.asarray(actual)!=np.asarray(baseline),axis=2).sum())
    if changed:raise ValueError('exported assembly differs from baseline')
    baseline_array=np.asarray(baseline);removals=[]
    for c in components:
        if not c['mutable']:continue
        removed=compose(base,layers,{c['id']});delta=np.any(np.asarray(removed)!=baseline_array,axis=2)
        x,y,w,h=c['bounds'];support=np.zeros((ch,cw),bool);support[y:y+h,x:x+w]=np.asarray(Image.open(out/c['mask']['path']))>0
        if np.any(delta&~support):raise ValueError('removal changed neighbor pixels')
        removals.append({'id':c['id'],'changedPixels':int(delta.sum()),'outsideSupportChangedPixels':0,
            'blockedCellsRemoved':len(c['footprintCells']) if c['blocksMovement'] else 0})
        pad=12;l=max(0,x-pad);t=max(0,y-pad);r=min(cw,x+w+pad);b=min(ch,y+h+pad)
        strip=Image.new('RGB',((r-l)*2,b-t));strip.paste(baseline.crop((l,t,r,b)),(0,0));strip.paste(removed.crop((l,t,r,b)),(r-l,0))
        strip.resize((strip.width*2,strip.height*2),Image.Resampling.NEAREST).save(out/'comparisons'/(c['id']+'.png'))
    report={'schemaVersion':1,'status':'passed','componentCount':len(components),'mutableCount':len(mutable),
        'sourceAssetCount':len(assets),'placedSourceAssetCount':len({e['sourceAssetId'] for e in components}),
        'intactChangedPixels':changed,'allMutableRemovedExact':True,'removals':removals,
        'navigation':{'baseWalkableCells':len(cells),'initialOpenCells':len(cells-blocked),'reachableCells':len(reached),'routes':routes},
        'sources':scene['sources'],'unityExecuted':False,'limits':['Single standing player pose, not a walk animation.','Review removal, not real game harvesting or destruction.','Inferred complete floor; no original hidden-surface recovery claim.']}
    write_json(out/'reports/composition.json',report);write_json(out/'scene.json',scene)
    print(json.dumps({k:report[k] for k in ('status','componentCount','mutableCount','placedSourceAssetCount','intactChangedPixels','navigation')},indent=2))
    return report

if __name__=='__main__':build()
