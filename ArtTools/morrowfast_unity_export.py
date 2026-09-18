"""Copy verified Morrowfast source art into Unity without changing its pixels.

The separate gameplay definition and coarse collision layout belong to the native
scene runtime. This exporter owns only art-definition.json and its PNG resources.
"""
import hashlib
import json
import math
from pathlib import Path
import shutil

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Morrowfast'
DESTINATION = ROOT / 'Assets/Resources/SceneArt/Morrowfast'
RESOURCE = 'SceneArt/Morrowfast/'
PPU = 40.96
ORIGIN_X = 21.25


def _safe_path(value):
    if not isinstance(value,str) or not value or any(c in value for c in '\\:%?#') or value.startswith('/') or '..' in value.split('/'):
        raise ValueError('Asset path must remain a local source-relative PNG: '+str(value))
    if Path(value).suffix.lower()!='.png':raise ValueError('PNG required: '+value)
    return value


def _destination(value):
    value=_safe_path(value)
    return 'Art/'+(value[6:] if value.startswith('build/') else value)


def _resource(value):return RESOURCE+_destination(value)[:-4]
def _sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def _pair(value):return isinstance(value,list) and len(value)==2 and all(isinstance(v,(int,float)) and math.isfinite(v) for v in value)


def build_art_definition(manifest):
    if manifest.get('schemaVersion')!=1 or manifest.get('id')!='morrowfast' or manifest.get('canvas')!={'width':1536,'height':1024}:
        raise ValueError('Unsupported Morrowfast source manifest or canvas')
    source_owners=manifest.get('components',[])
    owners=[]; owner_ids=set()
    for c in source_owners:
        if not c.get('id') or c['id'] in owner_ids or not _pair(c.get('foot')):raise ValueError('Invalid/duplicate owner or source foot')
        owner_ids.add(c['id']);foot=c['foot']
        if not 0<=foot[0]<1536 or not 0<=foot[1]<=1024:raise ValueError('Source foot escapes image')
        owners.append(dict(id=c['id'],kind=c['kind'],sourceFoot=foot,anchorX=math.floor(ORIGIN_X+foot[0]/PPU),anchorY=min(24,math.floor(foot[1]/PPU)),roomId=c.get('roomId'),visibleWhen=c.get('visibleWhen'),mutable=c['mutable']))
    layers=[]; layer_ids=set(); destinations=set()
    for layer in manifest.get('layers',[]):
        b=layer.get('bounds');z=layer.get('z')
        if not layer.get('id') or layer['id'] in layer_ids or layer.get('ownerId') not in owner_ids:raise ValueError('Duplicate/orphan source layer')
        if not isinstance(b,list) or len(b)!=4 or not all(isinstance(v,int) for v in b) or min(b[:2])<0 or min(b[2:])<=0 or b[0]+b[2]>1536 or b[1]+b[3]>1024:raise ValueError('Source bounds escape image')
        if not isinstance(z,(int,float)) or not math.isfinite(z):raise ValueError('Invalid source order')
        destination=_destination(layer['path'])
        if destination in destinations:raise ValueError('Two source layers share an export destination')
        destinations.add(destination);layer_ids.add(layer['id'])
        layers.append(dict(id=layer['id'],ownerId=layer['ownerId'],resource=_resource(layer['path']),bounds=b,z=z,role=layer['role'],roomId=layer.get('roomId')))
    for owner in source_owners:
        if set(owner.get('layerIds',[]))!={l['id'] for l in layers if l['ownerId']==owner['id']}:raise ValueError('Declared layer ownership disagrees')
    rooms=[];room_ids=set()
    for r in manifest.get('rooms',[]):
        if not r.get('id') or r['id'] in room_ids or r.get('roofId') not in owner_ids or r.get('doorId') not in owner_ids:raise ValueError('Invalid room ownership')
        polygon=r.get('interiorPolygon')
        if not isinstance(polygon,list) or len(polygon)<3 or not all(_pair(p) for p in polygon):raise ValueError('Invalid room interior')
        room_ids.add(r['id'])
        rooms.append(dict(id=r['id'],roofId=r['roofId'],doorId=r['doorId'],interiorPolygon=[dict(x=p[0],y=p[1]) for p in polygon]))
    if not owners or not layers or not rooms:raise ValueError('Complete source art inventory required')
    for o in owners:
        if o['roomId'] is not None and o['roomId'] not in room_ids:raise ValueError('Owner references absent room')
    return dict(schemaVersion=1,id='morrowfast',revision=1,canvasWidth=1536,canvasHeight=1024,pixelsPerCell=PPU,originX=ORIGIN_X,baseResource=_resource(manifest['base']['path']),baselineResource=_resource(manifest['baseline']['path']),layers=layers,owners=owners,rooms=rooms)


def export(source=SOURCE,destination=DESTINATION):
    source=Path(source).resolve();destination=Path(destination)
    manifest_path=source/'build/manifest.json';manifest=json.loads(manifest_path.read_text())
    definition=build_art_definition(manifest)
    records=[];rasters={}
    paths=[manifest['base']['path'],manifest['baseline']['path']]+[l['path'] for l in manifest['layers']]
    for relative in paths:
        relative=_safe_path(relative);src=(source/relative).resolve()
        if not src.is_relative_to(source):raise ValueError('Asset symlink escapes source root')
        with Image.open(src) as image:
            image.load()
            if image.format!='PNG' or image.mode not in ['RGB','RGBA']:raise ValueError('Native color PNG required: '+relative)
            layer=next((l for l in manifest['layers'] if l['path']==relative),None)
            expected=tuple(layer['bounds'][2:]) if layer else (1536,1024)
            if image.size!=expected:raise ValueError('Image dimensions disagree: '+relative)
            if layer and image.mode!='RGBA':raise ValueError('Component requires true alpha: '+relative)
            rasters[relative]=image.convert('RGBA')
        records.append(dict(source=relative,destination=_destination(relative),sourceSha256=_sha(src)))
    intact=rasters[manifest['base']['path']].copy()
    by_id={c['id']:c for c in manifest['components']}
    for layer in sorted(manifest['layers'],key=lambda l:(l['z'],l['id'])):
        if by_id[layer['ownerId']].get('visibleWhen')=='room-open':continue
        intact.alpha_composite(rasters[layer['path']],tuple(layer['bounds'][:2]))
    delta=int(np.count_nonzero(np.any(np.asarray(intact)!=np.asarray(rasters[manifest['baseline']['path']]),axis=2)))
    if delta:raise ValueError('Intact default layer composition differs from source: '+str(delta))
    # Content is fully checked before any destination mutation. Never replace the
    # separately authored gameplay definition or unrelated Unity assets.
    destination.mkdir(parents=True,exist_ok=True)
    for record in records:
        dst=destination/record['destination'];dst.parent.mkdir(parents=True,exist_ok=True)
        if not dst.exists() or _sha(dst)!=record['sourceSha256']:shutil.copyfile(source/record['source'],dst)
    (destination/'art-definition.json').write_text(json.dumps(definition,indent=2)+'\n')
    report=dict(schemaVersion=1,status='passed',layerCount=len(definition['layers']),ownerCount=len(definition['owners']),roomCount=len(definition['rooms']),assetCount=len(records),intactChangedPixels=delta,sourceManifestSha256=_sha(manifest_path),artDefinitionSha256=_sha(destination/'art-definition.json'),exporterSha256=_sha(__file__),files=records)
    (destination/'art-provenance.json').write_text(json.dumps(report,indent=2)+'\n')
    return report


if __name__=='__main__':
    print(json.dumps({k:v for k,v in export().items() if k!='files'},indent=2))
