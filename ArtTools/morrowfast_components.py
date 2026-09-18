"""Exact-source Morrowfast layers, with explicitly owned inferred surfaces.

Imagegen supplies the preserved inference plates. This producer changes only
declared masks and keeps original source pixels in every closed-state sprite.
"""
from __future__ import annotations
import json
import hashlib
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
from ArtTools.felling_components import polygon_mask, rgba_cutout, bbox

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=ROOT/'ArtSource/Morrowfast'
SIZE=(1536,1024)

def cutout(source, mask):return rgba_cutout(source,mask)
def to_world(x,y):return (21.25+x/40.96,25-y/40.96)
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def write_json(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,indent=2)+'\n')
def entries(path):
    value=json.loads(path.read_text())
    return value if isinstance(value,list) else value['entries']

def entry_mask(e,size,exclusions=True):
    region=polygon_mask(size,e['candidatePolygon'])
    for p in e.get('includePolygons',[]):region |= polygon_mask(size,p)
    if exclusions:
        for p in e.get('excludePolygons',[]):region &= ~polygon_mask(size,p)
    x,y,w,h=e['bounds'];bounded=np.zeros_like(region);bounded[y:y+h,x:x+w]=True
    return region & bounded

def validate_entries(items,size):
    ids=set()
    for e in items:
        if not isinstance(e.get('id'),str) or not e['id'] or e['id'] in ids:raise ValueError('duplicate or invalid ID')
        ids.add(e['id'])
        b=e.get('bounds');f=e.get('foot')
        if not isinstance(b,list) or len(b)!=4 or any(not isinstance(v,int) for v in b):raise ValueError('invalid bounds')
        x,y,w,h=b
        if min(x,y)<0 or min(w,h)<1 or x+w>size[0] or y+h>size[1]:raise ValueError('out-of-canvas bounds')
        if not isinstance(f,list) or len(f)!=2 or not all(np.isfinite(v) for v in f) or not(x<=f[0]<x+w and y<=f[1]<y+h):raise ValueError('invalid foot')
        if not entry_mask(e,size).any():raise ValueError('empty subject mask')

def repair_regions(source,backing,masks,radius=5,protected=None,surface_overrides=None):
    if source.shape!=backing.shape or source.ndim!=3 or source.shape[2]!=3:raise ValueError('matching RGB sources required')
    shape=source.shape[:2];protect=np.zeros(shape,bool) if protected is None else protected
    if protect.shape!=shape:raise ValueError('invalid protection mask')
    union=np.zeros(shape,bool)
    for mask in masks.values():
        if mask.shape!=shape or not mask.any() or np.any(mask&union) or np.any(mask&protect):raise ValueError('empty, overlapping or protected subject')
        union |= mask
    overrides=surface_overrides or {}
    if set(overrides)-set(masks):raise ValueError('unknown surface override owner')
    ids=sorted(masks);dist=np.full(shape,np.inf);owner=np.full(shape,-1,np.int16)
    for i,key in enumerate(ids):
        reach=overrides.get(key,{}).get('radius',radius)
        d=ndi.distance_transform_edt(~masks[key]);take=(d<=reach)&(d<dist)&~protect
        dist[take]=d[take];owner[take]=i
    base=source.copy();layers={}
    for i,key in enumerate(ids):
        support=owner==i;subject=masks[key]
        ring=ndi.binary_dilation(support,iterations=5)&~ndi.binary_dilation(union,iterations=2)&~support
        match=overrides.get(key,{}).get('colorMatch',True)
        offset=np.clip(np.median(source[ring].astype(float),axis=0)-np.median(backing[ring].astype(float),axis=0),-24,24) if ring.any() and match else np.zeros(3)
        corrected=np.clip(backing.astype(float)+offset,0,255)
        weight=np.minimum(1,ndi.distance_transform_edt(support)/4);weight[subject]=1
        repaired=np.rint(source*(1-weight[...,None])+corrected*weight[...,None]).astype(np.uint8)
        base[support]=repaired[support]
        layers[key]={'sprite':cutout(source,subject),'contact':cutout(source,support&~subject),'mask':subject,'support':support,'offset':offset.tolist()}
    return base,layers

def reassemble(base,layers,hidden=()):
    if set(hidden)-set(layers):raise ValueError('unknown hidden owner')
    out=base.copy()
    for key,parts in layers.items():
        if key in hidden:continue
        for role in ['contact','sprite']:
            rgba=parts[role];mask=rgba[...,3]>0;out[mask]=rgba[mask,:3]
    return out

def shifted(array,dx,dy):
    out=np.zeros_like(array);h,w=array.shape[:2]
    x0=max(0,-dx);x1=min(w,w-dx);y0=max(0,-dy);y1=min(h,h-dy)
    out[y0+dy:y1+dy,x0+dx:x1+dx]=array[y0:y1,x0:x1]
    return out

def sample_cells(mask,cell=8,threshold=.28):
    h,w=mask.shape
    blocks=mask.reshape(h//cell,cell,w//cell,cell).mean(axis=(1,3))
    yy,xx=np.nonzero(blocks>=threshold)
    return [[int(x),int(y)] for y,x in zip(yy,xx)]

def build(package=PACKAGE):
    out=package/'build';out.mkdir(parents=True,exist_ok=True)
    for d in ['sprites','masks','rooms','reports','review']:(out/d).mkdir(exist_ok=True)
    def rgb(path):
        with Image.open(package/path) as im:
            if im.size!=SIZE:raise ValueError(f'wrong dimensions {path}')
            return np.asarray(im.convert('RGB')).copy()
    source=rgb('reference.png');ground=rgb('sources/03-cleared-ground.png')
    plaza=rgb('sources/08-plaza-repair.png')
    ground[380:640,675:950]=plaza[380:640,675:950]
    furnished=rgb('sources/04-furnished-interiors.png');empty=rgb('sources/05-empty-interiors.png')
    repairs=rgb('sources/06-front-repairs.png');stove_repair=rgb('sources/07-stove-repairs.png')
    outdoor=entries(package/'authoring/outdoor-objects.json');indoor=entries(package/'authoring/interior-objects.json')
    buildings=entries(package/'authoring/buildings.json')
    cards=json.loads((package/'authoring/content-cards.json').read_text())
    unknown_cards=set(cards)-{e['id'] for e in outdoor+indoor}
    if unknown_cards:raise ValueError(f'unknown content card owners: {unknown_cards}')
    validate_entries(outdoor,SIZE);validate_entries(indoor,SIZE)
    outdoor_masks={e['id']:entry_mask(e,SIZE) for e in outdoor}
    outdoor_union=np.logical_or.reduce(list(outdoor_masks.values()))
    building_masks={b['id']:polygon_mask(SIZE,b['buildingPolygon'])&~outdoor_union for b in buildings}
    all_masks={**outdoor_masks,**building_masks}
    base,groups=repair_regions(source,ground,all_masks,
        surface_overrides={'central-cistern':{'radius':18,'colorMatch':False},
                          'western-shop-planter':{'radius':10,'colorMatch':False},
                          'southwest-worktable':{'radius':10,'colorMatch':False}})
    Image.fromarray(base).save(out/'base.png')
    components=[];layers=[];rooms=[];component_masks={};room_masks={};layer_arrays={}

    def layer(owner,role,rgba,z,room=None,provenance='original source pixels',dx=0,dy=0):
        alpha=rgba[...,3]>0
        if not alpha.any():return None
        x,y,w,h=bbox(alpha,pad=1);lid=owner+'-'+role
        path='build/sprites/'+lid+'.png'
        Image.fromarray(rgba[y:y+h,x:x+w]).save(package/path)
        item={'id':lid,'ownerId':owner,'path':path,'bounds':[x+dx,y+dy,w,h],'z':z,'role':role,'provenance':provenance}
        if room:item['roomId']=room
        layers.append(item);layer_arrays[lid]=shifted(rgba,dx,dy) if dx or dy else rgba
        return lid

    def component(e,mask,mutable=True,room=None,dx=0,dy=0):
        c={'id':e['id'],'name':e.get('label',e['id']),'kind':e.get('kind','prop'),
           'description':e.get('description',''),'actions':e.get('actions',['Examine']),
           'roomId':room,'foot':[e['foot'][0]+dx,e['foot'][1]+dy],
           'bounds':bbox(shifted(mask,dx,dy),pad=1),'mutable':mutable,
           'blocksMovement':bool(e.get('solid',False)), 'footprintCells':[], 'layerIds':[],
           'placementPolicy':'authored placement; contact collar stays with its original surface',
           'worldAnchor':list(to_world(e['foot'][0]+dx,e['foot'][1]+dy))}
        if room:c['visibleWhen']='room-open'
        c.update(cards.get(e['id'],{}))
        for k in ['dialogue','stock','repairDependencies','occludes','occludedByIds']:
            if k in e:c[k]=e[k]
        if c['blocksMovement']:
            physical=shifted(mask,dx,dy)
            if e.get('footprintPolygons'):
                physical=np.logical_or.reduce([polygon_mask(SIZE,p) for p in e['footprintPolygons']])
            elif e.get('kind') in ('npc','creature','character'):
                physical=np.zeros_like(mask);fx,fy=map(int,c['foot']);physical[max(0,fy-10):fy+2,max(0,fx-10):fx+11]=True
            c['footprintCells']=sample_cells(physical)
        component_masks[c['id']]=shifted(mask,dx,dy);components.append(c)
        Image.fromarray((component_masks[c['id']]*255).astype(np.uint8)).save(out/'masks'/f"{c['id']}.png")
        return c

    # Exact source outdoor sprites. Repairing a front object does not erase its
    # surviving neighbour: inferred concealed material belongs to that neighbour.
    outdoors_by_id={e['id']:e for e in outdoor}
    repair_records=[]
    for e in outdoor:
        eid=e['id'];g=groups[eid];mask=outdoor_masks[eid]
        c=component(e,mask)
        if e['kind']=='gate':c['blocksMovement']=True;c['footprintCells']=sample_cells(np.logical_or.reduce([polygon_mask(SIZE,p) for p in e['footprintPolygons']]))
        for front_id in e.get('occludedByIds',[]):
            hidden=entry_mask(e,SIZE,exclusions=False)&outdoor_masks[front_id]
            if not hidden.any():continue
            repair_source=stove_repair if front_id=='inn-outdoor-stove' else repairs
            policy='Imagegen stationary-surface completion'
            # Separate stable ID per occlusion, always below source foreground.
            lid=layer(eid,'repair-'+front_id,cutout(repair_source,hidden),800+e['foot'][1]/1000,provenance=policy)
            repair_records.append({'backOwner':eid,'frontOwner':front_id,'pixels':int(hidden.sum()),'layerId':lid,'method':policy})
        layer(eid,'contact',g['contact'],1000+e['foot'][1]/1000)
        layer(eid,'sprite',g['sprite'],1001+e['foot'][1]/1000)

    # Interiors are a separate inferred composition, hidden by exact source
    # roof/shell/door contributions until the room is exposed.
    for b in buildings:
        bid=b['id'];whole=building_masks[bid];roof=polygon_mask(SIZE,b['roofPolygon'])&whole
        door=entry_mask(b['door'],SIZE)&whole
        shell=whole&~roof&~door
        room_inside=polygon_mask(SIZE,b['interiorFloorPolygon'])
        room_masks[bid]=room_inside
        interior_items=[e for e in indoor if e['roomId']==bid]
        imasks={e['id']:entry_mask(e,SIZE)&whole for e in interior_items}
        indoor_base,igroups=repair_regions(furnished,empty,imasks,protected=~whole)
        # Eliminate the inferred generation's shifted door: take only its roof
        # footprint. Original visible masonry/doorframes remain source pixels.
        room_back=empty.copy()
        # The continuous empty room plate avoids baked cast shadows at the old
        # location of deliberately moved furnishings. Source roofs conceal this
        # inference completely in the intact composition.
        # Original door leaf is a removable layer over a known wood continuation.
        # Clone from safe empty floor above it, with short vertical interpolation.
        ys,xs=np.nonzero(door)
        if len(xs):
            sample_y=max(int(np.nonzero(room_inside)[0].min())+8,int(ys.min())-32)
            for y in range(int(ys.min()),int(ys.max())+1):
                row=door[y];room_back[y,row]=empty[sample_y+(y-int(ys.min()))%8,row]
        shell_id=bid+'-shell';roof_id=bid+'-roof';door_id=b['door']['id']
        shell_entry={'id':shell_id,'label':b['label']+' · walls','kind':'building-shell','foot':b['entry'],
                     'description':b['description'],'actions':['Examine'],'solid':False}
        component(shell_entry,shell,mutable=False)
        layer(shell_id,'interior',cutout(room_back,whole),100,bid,provenance='Imagegen interior floor/wall inference, source doorway alignment')
        layer(shell_id,'shell',cutout(source,shell),600,bid)
        layer(shell_id,'contact',groups[bid]['contact'],90,bid)
        roof_entry={'id':roof_id,'label':b['label']+' · roof','kind':'roof','foot':b['entry'],
                    'description':'Lift this independent roof to inspect the reconstructed room below.','actions':['Examine','Lift roof'],'solid':False}
        component(roof_entry,roof);layer(roof_id,'roof',cutout(source,roof),700,bid)
        de={**b['door'],'label':b['label']+' · door','kind':'door','actions':['Examine','Open','Close'],'solid':True,
            'description':'Independent original wooden door leaf. Open it to expose the reconstructed threshold.'}
        component(de,door);layer(door_id,'door',cutout(source,door),750,bid)
        rooms.append({'id':bid,'name':b['label'],'polygon':b['buildingPolygon'],'interiorPolygon':b['interiorFloorPolygon'],
                      'entry':b['entry'],'roofId':roof_id,'doorId':door_id})
        for e in interior_items:
            dx,dy=e.get('suggestedOffset',[0,0]);g=igroups[e['id']]
            c=component(e,g['mask'],room=bid,dx=dx,dy=dy)
            c['sourcePlacementOffset']=[dx,dy]
            layer(e['id'],'contact',g['contact'],200+e['foot'][1]/1000,bid,'inferred original indoor contact',dx,dy)
            layer(e['id'],'furniture',g['sprite'],201+e['foot'][1]/1000,bid,'Imagegen furnished-interior pixels; separated from empty floor',dx,dy)

    for c in components:c['layerIds']=[l['id'] for l in layers if l['ownerId']==c['id']]
    manifest={'schemaVersion':1,'id':'morrowfast','title':'Morrowfast · The Stillcord watch',
              'canvas':{'width':SIZE[0],'height':SIZE[1]},'base':{'path':'build/base.png'},'baseline':{'path':'reference.png'},
              'components':components,'layers':sorted(layers,key=lambda l:(l['z'],l['id'])),'rooms':rooms,
              'worldMapping':{'worldLocation':[3,6],'pixelsPerUnit':40.96,'left':21.25,'top':25,'northSouthLaneX':40},
              'provenance':{'sourceSha256':sha(package/'reference.png'),'generator':'built-in Imagegen','scope':'art/component authoring preview; native Unity gameplay not implemented'}}
    manifest['navigation']=navigation(buildings,components,component_masks,room_masks)
    write_json(out/'manifest.json',manifest)
    def compose_state(hidden):
        result=base.copy()
        for l in manifest['layers']:
            if l['ownerId'] in hidden:continue
            a=layer_arrays[l['id']];m=a[...,3]>0;result[m]=a[m,:3]
        return result
    intact=compose_state(set());roof_ids={r['roofId'] for r in rooms}
    Image.fromarray(intact).save(out/'reassembled.png')
    Image.fromarray(compose_state(roof_ids)).save(out/'interiors.png')
    mutable={c['id'] for c in components if c['mutable']}
    Image.fromarray(compose_state(mutable)).save(out/'all-removed.png')
    # Expose each owner's footprint in context for visual review of residuals,
    # overlaps and the inferred floor underneath. Indoor inspections lift roofs.
    review_dir=out/'review/removals';review_dir.mkdir(exist_ok=True)
    removal_tiles=[]
    for c in components:
        if not c['mutable']:continue
        state=({c['id']}|roof_ids) if c['roomId'] else {c['id']}
        result=compose_state(state)
        x,y,w,h=c['bounds'];pad=22
        region=(max(0,x-pad),max(0,y-pad),min(SIZE[0],x+w+pad),min(SIZE[1],y+h+pad))
        review=Image.fromarray(result).crop(region)
        review.save(review_dir/f"{c['id']}.png")
        removal_tiles.append((c['id'],review))
    cols=5;cw=300;ch=228
    review_sheet=Image.new('RGB',(cols*cw,((len(removal_tiles)+cols-1)//cols)*ch),(25,29,28))
    rd=ImageDraw.Draw(review_sheet)
    for i,(name,tile) in enumerate(removal_tiles):
        tx=i%cols*cw;ty=i//cols*ch
        tile.thumbnail((cw-16,ch-35),Image.Resampling.NEAREST)
        review_sheet.paste(tile,(tx+(cw-tile.width)//2,ty+5))
        rd.text((tx+8,ty+ch-23),name,fill=(221,212,188))
    review_sheet.save(out/'review/removal-sheet.png')
    changed=np.any(intact!=source,axis=2)
    report={'sourcePixels':SIZE[0]*SIZE[1],'intactDifferentPixels':int(changed.sum()),'owners':len(components),
            'outdoorOwners':len(outdoor),'indoorOwners':len(indoor),'buildings':len(buildings),'layers':len(layers),
            'occlusionRepairs':repair_records,'sourceSha256':sha(package/'reference.png'),
            'inferredSurfaceNote':'Hidden surfaces are inferred, not recovered original pixels. Source contacts are authored-placement collars.',
            'limits':['Perimeter fences and indistinct ground growth remain fixed surface texture.','Residents are single-pose cutouts, not complete directional animation sets.','Roof-off interiors are inferred and include documented furniture offsets.','Native Unity dialogue, trade and quest integration is a separate handoff.']}
    write_json(out/'reports/verification.json',report)
    contact_sheet(package,manifest)
    if changed.any():
        Image.fromarray((changed*255).astype(np.uint8)).save(out/'reports/reassembly-difference.png')
        raise ValueError(f'Intact reconstruction changed {changed.sum()} pixels')
    return report

def navigation(buildings,components,masks,rooms):
    # Fine authoring grid; the manifest separately declares the coarser Unity
    # mapping. The bridge and road connect both banks without erasing the creek.
    walk=np.ones((1024,1536),bool)
    creek=polygon_mask(SIZE,[[172,0],[352,0],[281,125],[244,267],[187,380],[171,481],[180,626],[141,753],[93,868],[105,1023],[0,1023],[0,943],[41,811],[79,691],[99,577],[92,465],[118,331],[167,203],[201,91]])
    walk[creek]=False
    bridge=next((c for c in components if c['kind']=='bridge'),None)
    crossings=[]
    if bridge:
        walk[masks[bridge['id']]]=True;bridge['blocksMovement']=False;bridge['footprintCells']=[]
        crossings.append({'ownerId':bridge['id'],'cells':sample_cells(creek&masks[bridge['id']],threshold=.1)})
    for b in buildings:
        bm=polygon_mask(SIZE,b['buildingPolygon']);walk[bm]=False
        inner=rooms[b['id']];walk[inner]=True
        # Each original threshold gets a narrow corridor to its own interior.
        x,y=map(int,b['entry']);inside_ys=np.nonzero(inner[:,min(1535,x)])[0]
        if not len(inside_ys):raise ValueError('door does not align with interior '+b['id'])
        top=int(inside_ys.max())-8
        walk[top:y+12,max(0,x-12):x+13]=True
        # Door's dynamic blocker must span the actual channel, not only its art.
        door=next(c for c in components if c['id']==b['door']['id'])
        dmask=np.zeros_like(walk);dy=int(door['foot'][1])-8;dmask[dy:dy+8,x-12:x+13]=True
        door['footprintCells']=sample_cells(dmask,threshold=.1)
    # Keep canvas boundary usable only at roads/banks; outdoor textures remain
    # approximate physical art until Unity's terrain pass is separately authored.
    cells=sample_cells(walk,threshold=.65)
    available={tuple(c) for c in cells}
    for crossing in crossings:
        crossing['cells']=[c for c in crossing['cells'] if tuple(c) in available]
    return {'cellSize':8,'width':192,'height':128,'spawn':[96,124],'northExit':[96,0],
            'walkableCells':cells,'crossings':crossings,
            'note':'Fine-grid art review: roof/door/interior and river crossing verified here; native80x25 collider authoring remains separate.'}

def contact_sheet(package,manifest):
    cs=manifest['components'];cols=6;cw=250;ch=176
    im=Image.new('RGB',(cols*cw,((len(cs)+cols-1)//cols)*ch),(25,29,28));draw=ImageDraw.Draw(im)
    for i,c in enumerate(cs):
        x=i%cols*cw;y=i//cols*ch
        candidates=[l for l in manifest['layers'] if l['ownerId']==c['id'] and l['role'] not in ('contact','interior') and not l['role'].startswith('repair')]
        if not candidates:continue
        l=candidates[-1]
        with Image.open(package/l['path']) as src:
            thumb=src.copy();thumb.thumbnail((cw-16,ch-40),Image.Resampling.NEAREST)
            im.paste(thumb,(x+(cw-thumb.width)//2,y+6),thumb)
        draw.text((x+8,y+ch-30),c['id'],fill=(221,212,188))
    im.save(package/'build/review/components.png')

if __name__=='__main__':print(json.dumps(build(),indent=2))
