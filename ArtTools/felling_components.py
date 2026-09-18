"""Reproducible source-pixel extraction and inferred backing composition.

The user explicitly authorized image-processing scripts. Imagegen supplies backing
sources; this tool selects known pixels, authors alpha, and exports measured layers.
It does not infer gameplay rules or promise complete unseen object geometry.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
import re
from pathlib import Path
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
try:
    from .felling_scene import validate_polygon
except ImportError:
    from felling_scene import validate_polygon

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'ArtSource/FellingSite/Components'


def rgba_cutout(rgb, mask):
    out = np.zeros((*rgb.shape[:2], 4), dtype=np.uint8)
    out[mask, :3] = rgb[mask, :3]
    out[mask, 3] = 255
    return out


def polygon_mask(size, points):
    if len(points) < 3 or any(len(p) != 2 or not all(math.isfinite(v) for v in p) for p in points):
        raise ValueError('polygon needs at least three finite points')
    validate_polygon(points,'component mask')
    im = Image.new('L', size)
    ImageDraw.Draw(im).polygon([tuple(p) for p in points], fill=255)
    return np.asarray(im) > 0


def segment(rgb, entry):
    """Select colored subject pixels within a visually authored search contour.

    Binary source-pixel alpha avoids re-blending already composited edge colors.
    Low-contrast stone relies on an explicitly reviewed contour, not a box.
    """
    height, width = rgb.shape[:2]
    region = polygon_mask((width, height), entry['candidatePolygon'])
    x,y,w,h = entry['bounds']
    bounded = np.zeros((height,width),bool);bounded[y:y+h,x:x+w]=True
    region &= bounded
    for polygon in entry.get('excludePolygons',[]):region&=~polygon_mask((width,height),polygon)
    method=entry.get('maskMethod','polygon')
    r,g,b = (rgb[...,i].astype(np.int16) for i in range(3))
    if method == 'red':
        strong=(r-g>=np.maximum(13,r*.32))&(r-b>=8)&(r>=40)
        # Dim brown branches are less saturated than their red tips. Retain
        # connected branches while keeping bright pale stone out of the mask.
        saturation=np.where(r<90,.20,.30)
        weak=(r-g>=np.maximum(5,r*saturation))&(r-b>=2)&(r>=24)
    elif method == 'cyan':
        strong=(g-r>=12)&(b-r>=5)&(g>=50)
        weak=(g-r>=5)&(b-r>=-2)&(g>=29)
    elif method == 'pink':
        strong=(r-g>=12)&(b-g>=-8)&(r>=64)
        weak=(r-g>=5)&(b-g>=-12)&(r>=34)
    elif method == 'cream':
        strong=(r-g>=3)&(g-b>=0)&(r>=98)&(b>=55)
        weak=(r-g>=0)&(r>=60)&(b>=32)
    else:
        return region
    seeds=strong&region
    labels,count=ndi.label(seeds, structure=np.ones((3,3),int))
    sizes=np.bincount(labels.ravel());sizes[0]=0
    seeds=np.isin(labels,np.flatnonzero(sizes>=2))
    # Grow along locally related colored stems. No rectangular backing is selected.
    result=ndi.binary_propagation(seeds, mask=weak&region, structure=np.ones((3,3),bool))
    # Include one pixel of the source antialias fringe; it remains original opaque color.
    fringe=ndi.binary_dilation(result, iterations=1)&region
    result |= fringe & (r+g+b>75) & ndi.binary_dilation(weak&region)
    for polygon in entry.get('includePolygons',[]):result|=polygon_mask((width,height),polygon)&region
    return result


def make_mutable_layers(source, backing, masks, protected=None, barriers=None):
    if source.ndim!=3 or source.shape[-1]!=3 or source.shape!=backing.shape:
        raise ValueError('source/backing must be matching RGB arrays')
    shape=source.shape[:2]
    protected=np.zeros(shape,bool) if protected is None else protected
    if protected.shape!=shape: raise ValueError('protected mask shape mismatch')
    union=np.zeros(shape,bool)
    for key,m in sorted(masks.items()):
        if m.shape!=shape or not m.any(): raise ValueError(f'empty or mismatched subject mask: {key}')
        if np.any(m&protected): raise ValueError(f'subject intersects protected landmark: {key}')
        if np.any(m&union): raise ValueError(f'overlapping subject pixels: {key}')
        union|=m
    # Every contextual pixel has one owner, chosen by nearest subject then stable ID.
    best=np.full(shape,np.inf,dtype=np.float32)
    owners=np.full(shape,-1,dtype=np.int16)
    ids=sorted(masks)
    barriers={} if barriers is None else barriers
    for index,key in enumerate(ids):
        d=ndi.distance_transform_edt(~masks[key])
        take=(d<=5)&(d<best)&~protected
        if key in barriers:take&=~barriers[key]
        best[take]=d[take];owners[take]=index
    base=source.copy();layers={}
    for index,key in enumerate(ids):
        m=masks[key];support=owners==index
        # Use surrounding known material to correct broad generated palette drift.
        ring=ndi.binary_dilation(support,iterations=5)&~ndi.binary_dilation(union,iterations=2)&~support
        if ring.any():
            offset=np.median(source[ring].astype(float),axis=0)-np.median(backing[ring].astype(float),axis=0)
            offset=np.clip(offset,-24,24)
        else: offset=np.zeros(3)
        corrected=np.clip(backing.astype(np.float32)+offset,0,255)
        distance=ndi.distance_transform_edt(support)
        weight=np.minimum(1,distance/4);weight[m]=1
        repaired=np.rint(source*(1-weight[...,None])+corrected*weight[...,None]).astype(np.uint8)
        base[support]=repaired[support]
        contact=support&~m
        layers[key]={'sprite':rgba_cutout(source,m),'contact':rgba_cutout(source,contact),
                     'mask':m,'support':support,'colorOffset':offset.tolist()}
    return base,layers


def compose(base, layers, removed=()):
    removed=set(removed)
    if removed-set(layers): raise ValueError('unknown removed component')
    out=base.copy()
    for key in sorted(layers):
        if key in removed: continue
        for role in ['contact','sprite']:
            rgba=layers[key][role];mask=rgba[...,3]>0
            out[mask]=rgba[mask,:3]
    return out


def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write_json(path, value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,indent=2)+'\n')


def bbox(mask, pad=1):
    yy,xx=np.nonzero(mask)
    if not len(xx): raise ValueError('empty mask has no bounds')
    l=max(0,int(xx.min())-pad);t=max(0,int(yy.min())-pad)
    r=min(mask.shape[1],int(xx.max())+1+pad);b=min(mask.shape[0],int(yy.max())+1+pad)
    return [l,t,r-l,b-t]


def bounds_with_anchor(mask, foot):
    l,t,w,h=bbox(mask,1);r=l+w;b=t+h
    fx,fy=foot
    l=max(0,min(l,math.floor(fx)-1));t=max(0,min(t,math.floor(fy)-1))
    r=min(mask.shape[1],max(r,math.ceil(fx)+1));b=min(mask.shape[0],max(b,math.ceil(fy)+1))
    return [l,t,r-l,b-t]


def save_array(path, array):
    path.parent.mkdir(parents=True,exist_ok=True)
    Image.fromarray(array).save(path,compress_level=9)
    return {'path':str(path.relative_to(path.parents[1])),'sha256':digest(path)}


def build(authoring_path, backing_path, out):
    authoring=json.loads(authoring_path.read_text())
    reference_path=PACKAGE.parent/'reference.png'
    source=np.asarray(Image.open(reference_path).convert('RGB')).copy()
    h,w=source.shape[:2]
    for e in authoring['entries']:
        if not re.fullmatch(r'[a-z][a-z0-9-]*',e.get('id','')):raise ValueError('unsafe component ID')
        bounds=e.get('bounds',[])
        if len(bounds)!=4 or any(type(v) is not int for v in bounds):raise ValueError('invalid authoring bounds')
        x,y,cw,ch=bounds
        if x<0 or y<0 or cw<=0 or ch<=0 or x+cw>w or y+ch>h:raise ValueError('authoring bounds outside image')
    patch=np.asarray(Image.open(PACKAGE.parent/'generated/clean-plate.png').convert('RGB'))
    source[800:896,736:816]=patch[800:896,736:816]
    backing=np.asarray(Image.open(backing_path).convert('RGB'))
    if backing.shape!=source.shape: raise ValueError('backing canvas mismatch')
    protected=np.zeros((h,w),bool)
    yy,xx=np.mgrid[:h,:w]
    for region in authoring['protectedRegions']:
        x,y=region['center'];rad=region['radiusPixels']
        protected|=(xx-x)**2+(yy-y)**2<=rad**2
    entries=[e for e in authoring['entries'] if e.get('extractionEnabled',True) and e['id']!='reference-traveler']
    if len({e['id'] for e in entries})!=len(entries): raise ValueError('duplicate IDs')
    mutable=[e for e in entries if e['classification']=='mutable']
    fixed=[e for e in entries if e['classification']!='mutable']
    masks={e['id']:segment(source,e) for e in mutable}
    # Resolve mask-search overlap explicitly, giving smaller subject contours first.
    claimed=np.zeros((h,w),bool);overlap_notes=[]
    for key in sorted(masks,key=lambda k:(int(masks[k].sum()),k)):
        old=masks[key].sum();masks[key]&=~claimed
        if old!=masks[key].sum(): overlap_notes.append({'id':key,'excludedNeighborPixels':int(old-masks[key].sum())})
        claimed|=masks[key]
    barriers={}
    for e in mutable:
        if e.get('excludePolygons'):
            barrier=np.zeros((h,w),bool)
            for poly in e['excludePolygons']:barrier|=polygon_mask((w,h),poly)
            barriers[e['id']]=barrier
    clean,layers=make_mutable_layers(source,backing,masks,protected,barriers)
    intact=compose(clean,layers)
    if not np.array_equal(intact,source): raise ValueError('intact source recomposition failed')
    out.mkdir(parents=True,exist_ok=True)
    for folder in ['sprites','contacts','masks','reports','comparisons']:(out/folder).mkdir(exist_ok=True)
    Image.fromarray(source).save(out/'baseline.png')
    shutil.copy2(reference_path,out/'reference.png')
    shutil.copy2(backing_path,out/'backing-source.png')
    Image.fromarray(clean).save(out/'cleaned-environment.png')
    Image.fromarray(intact).save(out/'reconstructed.png')
    inference=np.zeros((h,w),bool)
    for layer in layers.values(): inference|=layer['support']
    Image.fromarray(inference.astype(np.uint8)*255).save(out/'inference-mask.png')
    # Fixed visual sections are partitioned out of the cleaned base. They carry
    # cleaned backing behind mutable growth, never its original baked duplicate.
    fixed_masks={};fixed_claimed=np.zeros((h,w),bool)
    for e in reversed(fixed):
        m=polygon_mask((w,h),e['candidatePolygon'])
        m&=~fixed_claimed;fixed_claimed|=m
        if m.any():fixed_masks[e['id']]=m
    base=rgba_cutout(clean,~fixed_claimed)
    Image.fromarray(base).save(out/'base.png')
    components=[];removal_reports=[]
    by_id={e['id']:e for e in entries}
    for e in entries:
        key=e['id'];is_mutable=key in layers
        if not is_mutable and key not in fixed_masks:continue
        if is_mutable:
            layer=layers[key];m=layer['mask'];support=layer['support']
            sprite=layer['sprite'];contact=layer['contact']
        else:
            m=fixed_masks[key];support=m;sprite=rgba_cutout(clean,m);contact=None
        bounds=bounds_with_anchor(support,e['footPixel']);x,y,cw,ch=bounds
        def asset(role,array):
            p=out/role/(key+'.png');Image.fromarray(array[y:y+ch,x:x+cw]).save(p,compress_level=9)
            return {'path':p.relative_to(out).as_posix(),'sha256':digest(p)}
        spr=asset('sprites',sprite)
        c=asset('contacts',contact) if contact is not None and contact[...,3].any() else None
        repair_mask=support
        mp=asset('masks',repair_mask.astype(np.uint8)*255)
        fx,fy=e['footPixel'];pivot=[(fx-x)/cw,1-(fy-y)/ch]
        cx=16+int(fx//32);cy=int((fy-224)//32)
        cells=[[cx,cy]] if 16<=cx<64 and 0<=cy<25 else []
        notes=[e.get('notes',''),'Candidate gameplay mapping; no balance or interaction has been added to Unity.']
        if not is_mutable:notes+=['Fixed source-space partition: inspection/depth asset, not a movable complete geological object. Hiding it reveals diagnostic transparency, not invented traversable ground.']
        elif e.get('maskMethod') in ('stone','polygon'):notes+=['Contour-authored visible silhouette; unseen sides and art under overlapping objects are not reconstructed.']
        else:notes+=['Color-guided source-pixel silhouette; dark fringes may include a pixel of original surrounding color. Inspect at native size before free movement.']
        components.append({'id':key,'name':key.replace('-',' ').capitalize(),'kind':e['type'],
            'mutable':is_mutable,'interaction':'candidate-removal' if is_mutable else 'fixed-review-only',
            'contributesToComposite':True,'depth':1000+int(fy) if is_mutable else entries.index(e),
            'bounds':bounds,'foot':[fx,fy],'pivot':pivot,'footprintCells':cells,
            'sprite':spr,'contact':c,'repair':{'maskPath':mp['path'],'surface':e.get('relationBackingGround',{}).get('parentSurfaceId','neighboring-surface'),
                'inferred':is_mutable,'sourceProvenanceId':'generated-backing' if is_mutable else 'actor-free-baseline'},
            'extraction':{'method':e.get('maskMethod','polygon'),'quality':'source-scale-review-required' if is_mutable else 'fixed-section-partition',
                'provenance':'original-baseline-pixels' if is_mutable else 'cleaned-baseline-partition',
                'inferredPixelCount':0 if is_mutable else int((m&inference).sum()),'notes':notes},
            'defaultVisible':True})
        if is_mutable:
            removed=compose(clean,layers,{key})
            difference=np.any(removed!=source,axis=2)
            outside=int((difference&~support).sum())
            if outside:raise ValueError(f'removal changes neighbor: {key}')
            removal_reports.append({'id':key,'subjectPixels':int(m.sum()),'repairPixels':int(support.sum()),
                'changedPixels':int(difference.sum()),'outsideSupportChangedPixels':outside,
                'protectedChangedPixels':int((difference&protected).sum()),'backingColorOffset':layers[key]['colorOffset']})
            # Aligned source/isolated sprite/revealed floor strips for native review.
            pad=10;bx=max(0,x-pad);by=max(0,y-pad);br=min(w,x+cw+pad);bb=min(h,y+ch+pad)
            crops=[Image.fromarray(source[by:bb,bx:br]),Image.fromarray(removed[by:bb,bx:br])]
            strip=Image.new('RGB',((br-bx)*2,bb-by));strip.paste(crops[0],(0,0));strip.paste(crops[1],(br-bx,0))
            strip.resize((strip.width*3,strip.height*3),Image.Resampling.NEAREST).save(out/'comparisons'/(key+'.png'))
    # Re-render exported cropped files, independently of the in-memory ownership model.
    rendered=Image.open(out/'base.png').convert('RGBA')
    for c in sorted(components,key=lambda e:(e['depth'],e['id'])):
        for a in [c['contact'],c['sprite']]:
            if a:rendered.alpha_composite(Image.open(out/a['path']).convert('RGBA'),tuple(c['bounds'][:2]))
    pixel_diff=np.abs(np.asarray(rendered).astype(int)-np.dstack([source,np.full((h,w),255,np.uint8)]).astype(int))
    exact=int(np.any(pixel_diff,axis=2).sum())
    if exact:raise ValueError(f'exported cropped RGBA reassembly drift: {exact} pixels')
    report={'schemaVersion':1,'status':'offline checks passed; visual quality and Unity gates separate',
        'componentCount':len(components),'mutableCount':len(layers),'staticOrEffectCount':len(components)-len(layers),
        'intactReassembly':{'comparison':'exact RGBA against actor-free baseline','changedPixels':exact,'maxChannelDifference':int(pixel_diff.max()),'passed':exact==0},
        'allMutableRemoved':{'path':'cleaned-environment.png','protectedChangedPixels':int(np.any(clean[protected]!=source[protected],axis=1).sum()),'inferredPixelCount':int(inference.sum())},
        'removals':removal_reports,'overlapResolution':overlap_notes,
        'sources':{'referenceSha256':digest(reference_path),'backingSha256':digest(backing_path),'authoringSha256':digest(authoring_path),
            'extractorSourceSha256':digest(Path(__file__)),
            'actorRemovalPatchSourceSha256':digest(PACKAGE.parent/'generated/clean-plate.png'),'actorRemovalPatchRect':[736,800,80,96]},
        'disabledInventoryEntries':[{'id':e['id'],'reason':e.get('notes','')} for e in authoring['entries'] if not e.get('extractionEnabled',True)],
        'limits':['Hidden surfaces are inferred, not recovered.','Only inventoried visible components are extracted; tiny decorative detail and distant/edge ambiguities remain documented.',
                  'Fixed-section occlusion boundaries and visible-only cutouts need Unity depth/FOV and moving-object art review.','Contact detail is tied to its original ground location and must not move with the object.']}
    write_json(out/'reports/verification.json',report)
    manifest={'schemaVersion':1,'id':'felling-site-components-v1',
        'canvas':{'width':w,'height':h,'pixelsPerCell':32,'imageGroundOrigin':[0,224],'regionOrigin':[16,0]},
        'reference':{'path':'reference.png','sha256':digest(out/'reference.png')},
        'baseline':{'path':'baseline.png','sha256':digest(out/'baseline.png')},
        'backingSource':{'id':'generated-backing','path':'backing-source.png','sha256':digest(out/'backing-source.png')},
        'base':{'path':'base.png','sha256':digest(out/'base.png'),'inferenceMaskPath':'inference-mask.png','retainsStatic':False},
        'report':{'path':'reports/verification.json'},'components':components}
    write_json(out/'manifest.json',manifest)
    create_contact_sheet(out,components)
    export_prop_atlas(out,components)
    return report


def create_contact_sheet(out,components):
    mutable=[c for c in components if c['mutable']]
    tilew,tileh=210,190;cols=6;rows=math.ceil(len(mutable)/cols)
    sheet=Image.new('RGB',(cols*tilew,rows*tileh),(27,30,28));draw=ImageDraw.Draw(sheet)
    for i,c in enumerate(mutable):
        x=(i%cols)*tilew;y=(i//cols)*tileh
        for sy in range(24,tileh-12,12):
            for sx in range(4,tilew-4,12):
                color=(93,95,93) if (sx//12+sy//12)%2 else (59,63,60)
                draw.rectangle((x+sx,y+sy,x+sx+11,y+sy+11),fill=color)
        sprite=Image.open(out/c['sprite']['path']).convert('RGBA')
        factor=min(3,(tilew-12)/sprite.width,(tileh-40)/sprite.height)
        sprite=sprite.resize((max(1,int(sprite.width*factor)),max(1,int(sprite.height*factor))),Image.Resampling.NEAREST)
        sheet.paste(sprite,(x+(tilew-sprite.width)//2,y+25),sprite)
        draw.text((x+5,y+5),c['id'][:31],fill=(225,232,216))
    sheet.save(out/'reports/sprite-contact-sheet.png')


def export_prop_atlas(out,components):
    """Convenience atlas of existing RGBA exports, with exact unscaled rectangles."""
    entries=[c for c in components if c['mutable']]
    cell=1
    while cell<max(max(c['bounds'][2:]) for c in entries)+4:cell*=2
    cols=8;rows=math.ceil(len(entries)/cols)
    atlas=Image.new('RGBA',(cols*cell,rows*cell));records=[]
    for i,c in enumerate(entries):
        sprite=Image.open(out/c['sprite']['path']).convert('RGBA')
        x=(i%cols)*cell+2;y=(i//cols)*cell+2
        atlas.alpha_composite(sprite,(x,y))
        records.append({'id':c['id'],'sourceRectTopLeft':[x,y,sprite.width,sprite.height],
            'unityRectBottomLeft':[x,atlas.height-y-sprite.height,sprite.width,sprite.height],
            'pivot':c['pivot'],'sourceSpriteSha256':c['sprite']['sha256']})
    atlas.save(out/'prop-atlas.png')
    # Verify packing didn't crop, blend or resize a single source sprite pixel.
    for c,record in zip(entries,records):
        x,y,w,h=record['sourceRectTopLeft']
        if not np.array_equal(np.asarray(atlas.crop((x,y,x+w,y+h))),np.asarray(Image.open(out/c['sprite']['path']))):
            raise ValueError(f'atlas altered sprite pixels: {c["id"]}')
    write_json(out/'prop-atlas.json',{'schemaVersion':1,'path':'prop-atlas.png','sha256':digest(out/'prop-atlas.png'),
        'pixelsPerUnit':32,'size':list(atlas.size),'paddingPixels':2,'entries':records,'verification':'every packed RGBA crop equals its source sprite'})


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--authoring',type=Path,default=PACKAGE/'authoring.json')
    parser.add_argument('--backing',type=Path,default=PACKAGE/'sources/empty-environment-v1.png')
    parser.add_argument('--out',type=Path,default=PACKAGE/'build')
    args=parser.parse_args()
    report=build(args.authoring,args.backing,args.out)
    print(json.dumps({k:report[k] for k in ['componentCount','mutableCount','staticOrEffectCount','intactReassembly']},indent=2))

if __name__=='__main__':main()
