#!/usr/bin/env python3
"""Extract native-resolution Catacomb sheet artwork with its authoritative alpha.

The source has accurate near-opaque object alpha (mostly245–252) and low-alpha
sheet haze. Alpha>=128 becomes255; lower alpha becomes0. RGB of every visible
pixel is copied exactly. No resizing, color quantization, or inpainting occurs.
"""
from __future__ import annotations
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import numpy as np
from PIL import Image, ImageDraw
try:
    from .felling_scene import validate_polygon
except ImportError:
    from felling_scene import validate_polygon

ROOT=Path(__file__).resolve().parents[1]
AUTHORING=ROOT/'ArtSource/CatacombVillage/source-authoring.json'
ID_RE=re.compile(r'^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$')


def _number(value):
    return isinstance(value,(int,float)) and not isinstance(value,bool) and math.isfinite(value)


def _entry_valid(entry,size):
    if not isinstance(entry,dict) or not ID_RE.fullmatch(str(entry.get('id',''))):
        raise ValueError('asset id must be a safe kebab-case identifier')
    b=entry.get('bounds')
    if not isinstance(b,list) or len(b)!=4 or any(type(v) is not int for v in b):
        raise ValueError('bounds must be four integers')
    x,y,w,h=b
    if min(x,y)<0 or min(w,h)<1 or x+w>size[0] or y+h>size[1]:
        raise ValueError('bounds must fit the source image')
    foot=entry.get('foot')
    if not isinstance(foot,list) or len(foot)!=2 or not all(_number(v) for v in foot):
        raise ValueError('foot must contain two finite coordinates')
    if not (0<=foot[0]<=size[0] and 0<=foot[1]<=size[1]):
        raise ValueError('foot must fit the source image')
    threshold=entry.get('alphaThreshold',128)
    if type(threshold) is not int or not 1<=threshold<=254:
        raise ValueError('alpha threshold must be an integer in1..254')
    polygons=entry.get('excludePolygons',[])
    if not isinstance(polygons,list):raise ValueError('excludePolygons must be a list')
    for polygon in polygons:
        validate_polygon(polygon,'asset exclusion')
        if not isinstance(polygon,list) or len(polygon)<3:
            raise ValueError('exclusion polygon needs at least three points')
        if any(not isinstance(p,list) or len(p)!=2 or not all(_number(v) for v in p) for p in polygon):
            raise ValueError('exclusion polygon points must be finite pairs')
        if any(not (0<=p[0]<size[0] and 0<=p[1]<size[1]) for p in polygon):
            raise ValueError('exclusion polygon must fit the source image')


def validate_authoring(authoring,size):
    if not isinstance(authoring,dict) or not isinstance(authoring.get('assets'),list) or not authoring['assets']:
        raise ValueError('authoring must contain a nonempty assets list')
    if 'canvas' in authoring and authoring['canvas']!=list(size):
        raise ValueError('declared canvas must match source image dimensions')
    ids=set()
    for entry in authoring['assets']:
        _entry_valid(entry,size)
        if entry['id'] in ids:raise ValueError('duplicate asset id: '+entry['id'])
        ids.add(entry['id'])
    return True


def extract_sprite(source:Image.Image,entry:dict):
    """Return native RGBA crop and pixel/pivot metadata; never mutate source."""
    if source.mode!='RGBA':raise ValueError('source must have authoritative RGBA alpha')
    _entry_valid(entry,source.size)
    x,y,w,h=entry['bounds']
    rgba=np.asarray(source.crop((x,y,x+w,y+h))).copy()
    mask=rgba[:,:,3]>=entry.get('alphaThreshold',128)
    for polygon in entry.get('excludePolygons',[]):
        exclusion=Image.new('1',(w,h));ImageDraw.Draw(exclusion).polygon([(px-x,py-y) for px,py in polygon],fill=1)
        mask &= ~np.asarray(exclusion,dtype=bool)
    if not mask.any():raise ValueError('asset has no visible alpha: '+entry['id'])
    # One transparent pixel protects atlas sampling. Anchors outside the visible
    # cutout expand the crop; pivots are never silently clamped.
    ys,xs=np.nonzero(mask);fx,fy=entry['foot']
    left=max(0,min(x+int(xs.min())-1,math.floor(fx)))
    top=max(0,min(y+int(ys.min())-1,math.floor(fy)))
    right=min(source.width,max(x+int(xs.max())+2,math.ceil(fx)+1))
    bottom=min(source.height,max(y+int(ys.max())+2,math.ceil(fy)+1))
    output=np.asarray(source.crop((left,top,right,bottom))).copy()
    alpha=np.zeros((bottom-top,right-left),dtype=np.uint8)
    yy,xx=np.nonzero(mask)
    alpha[yy+y-top,xx+x-left]=255
    output[:,:,3]=alpha
    # Transparent RGB is zeroed so future filtering cannot leak the sheet haze.
    output[alpha==0,:3]=0
    width,height=right-left,bottom-top
    metadata={'sourceBounds':[left,top,width,height],'bounds':[left,top,width,height],
              'foot':[fx-left,fy-top],'pivot':[(fx-left)/width,1-(fy-top)/height],
              'alphaThreshold':entry.get('alphaThreshold',128),'maskMethod':'source-alpha-threshold',
              'visiblePixels':int(mask.sum()),'rgbChangedVisiblePixels':0,
              'sourceRoi':entry['bounds'],'exclusionPolygonCount':len(entry.get('excludePolygons',[]))}
    return Image.fromarray(output,'RGBA'),metadata


def sha256(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build(authoring_path=AUTHORING,output_dir=None):
    authoring_path=Path(authoring_path).resolve()
    authoring=json.loads(authoring_path.read_text())
    source_path=(authoring_path.parent/authoring['sourcePath']).resolve()
    source=Image.open(source_path)
    validate_authoring(authoring,source.size)
    output_dir=Path(output_dir or authoring_path.parent/'source-assets')
    sprites_dir=output_dir/'sprites';sprites_dir.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source_path,output_dir/'source.png')
    entries=[];thumbs=[]
    for entry in authoring['assets']:
        sprite,metadata=extract_sprite(source,entry)
        path=sprites_dir/(entry['id']+'.png');sprite.save(path)
        metadata.update({'id':entry['id'],'name':entry.get('name',entry['id']),
                         'kind':entry['kind'],'path':'sprites/'+path.name,
                         'spriteSha256':sha256(path),'sourceSha256':sha256(source_path),
                         'notes':entry.get('notes',''),'width':sprite.width,'height':sprite.height})
        entries.append(metadata);thumbs.append((entry['id'],sprite))
    catalog={'schemaVersion':1,'id':'catacomb-source-assets','source':{'path':'source.png','sha256':sha256(source_path),'width':source.width,'height':source.height},
             'authoringSha256':sha256(authoring_path),'extractorSha256':sha256(__file__),'nativePixelsPerUnit':32,'assets':entries,
             'alphaPolicy':'Binary alpha>=128. Lower source alpha is discarded. Visible RGB preserved exactly; transparent RGB zeroed.',
             'limitations':['Perspective illustrated swatches are not guaranteed seamless or grid-ready.',
                            'Source gardener contains its visibly connected floor/coral patch and should remain one study asset.',
                            'Source built-in shadows/glow inside opaque contours remain; outside glow is discarded with low-alpha haze.']}
    (output_dir/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n')
    cellw,cellh,columns=280,250,6
    sheet=Image.new('RGB',(cellw*columns,cellh*math.ceil(len(thumbs)/columns)),(36,41,41));draw=ImageDraw.Draw(sheet)
    for i,(id,sprite) in enumerate(thumbs):
        x=(i%columns)*cellw;y=(i//columns)*cellh
        for gy in range(y+25,y+cellh-5,12):
            for gx in range(x+5,x+cellw-5,12):
                tone=48 if ((gx-x)//12+(gy-y)//12)%2 else 62
                draw.rectangle([gx,gy,min(gx+11,x+cellw-6),min(gy+11,y+cellh-6)],fill=(tone,tone,tone))
        sheet.paste(sprite,(x+(cellw-sprite.width)//2,y+28+(cellh-35-sprite.height)//2),sprite)
        draw.text((x+9,y+7),id,fill=(225,230,217))
    sheet.save(output_dir/'contact-sheet.png')
    report={'assetCount':len(entries),'allHaveBinaryAlpha':True,'allHaveOpaqueAndTransparentPixels':True,'sourceVisibleRgbDifferences':0,'sourceSha256':sha256(source_path),'authoringSha256':sha256(authoring_path),'extractorSha256':sha256(__file__)}
    ownership=np.zeros((source.height,source.width),dtype=bool)
    for metadata in entries:
        actual=np.asarray(Image.open(output_dir/metadata['path']))
        if set(np.unique(actual[:,:,3]))!={0,255}:raise ValueError('asset must have opaque and transparent pixels: '+metadata['id'])
        x,y,w,h=metadata['sourceBounds'];expected=np.asarray(source.crop((x,y,x+w,y+h)))
        mask=actual[:,:,3]>0;region=ownership[y:y+h,x:x+w]
        if np.any(region & mask):raise ValueError('assets share source pixels: '+metadata['id'])
        region |= mask
        if np.any(actual[:,:,:3][actual[:,:,3]>0]!=expected[:,:,:3][actual[:,:,3]>0]):
            raise ValueError('visible source RGB mismatch: '+metadata['id'])
    report['sourcePixelsWithUniqueOwnership']=int(ownership.sum())
    report['sourcePixelOwnershipOverlaps']=0
    (output_dir/'extraction-report.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':print(json.dumps(build(),indent=2))
