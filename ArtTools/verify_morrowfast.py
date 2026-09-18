"""Read-only exported-art audit; never invokes the producer or edits source art."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=ROOT/'ArtSource/Morrowfast'

def verify(package=PACKAGE):
    manifest=json.loads((package/'build/manifest.json').read_text())
    source=np.asarray(Image.open(package/'reference.png').convert('RGB'))
    base=np.asarray(Image.open(package/manifest['base']['path']).convert('RGB'))
    composite=base.copy();allowed=np.zeros(source.shape[:2],bool)
    owners={c['id'] for c in manifest['components']}
    source_layers=0;inferred_layers=0
    for layer in manifest['layers']:
        path=package/layer['path']
        assert path.resolve().is_relative_to(package.resolve()),'unsafe asset path'
        assert layer['ownerId'] in owners,'orphan layer'
        with Image.open(path) as image:
            assert image.mode=='RGBA',f'not real RGBA: {path}'
            a=np.asarray(image)
        x,y,w,h=layer['bounds']
        assert a.shape==(h,w,4),f'wrong export rectangle: {path}'
        assert x>=0 and y>=0 and x+w<=source.shape[1] and y+h<=source.shape[0]
        assert set(np.unique(a[...,3])).issubset({0,255}),'nonbinary source alpha'
        mask=a[...,3]>0
        assert mask.any(),'empty layer'
        if layer['provenance']=='original source pixels':
            assert np.array_equal(a[mask,:3],source[y:y+h,x:x+w][mask]),f'source pixels altered: {path}'
            allowed[y:y+h,x:x+w] |= mask
            source_layers+=1
        else:inferred_layers+=1
        composite[y:y+h,x:x+w][mask]=a[mask,:3]
    assert np.array_equal(source,composite),'exported reconstruction differs'
    assert np.array_equal(source[~allowed],base[~allowed]),'backing changed unowned known pixels'
    assert np.array_equal(composite,np.asarray(Image.open(package/'build/reassembled.png').convert('RGB'))),'stale reassembly export'
    for c in manifest['components']:
        mask=np.asarray(Image.open(package/'build/masks'/f"{c['id']}.png"))
        assert mask.shape==source.shape[:2] and np.any(mask),'invalid ownership mask'
        assert c['layerIds']==[l['id'] for l in manifest['layers'] if l['ownerId']==c['id']] or set(c['layerIds'])=={l['id'] for l in manifest['layers'] if l['ownerId']==c['id']},'owner/layer disagreement'
        if c['mutable']:assert (package/'build/review/removals'/f"{c['id']}.png").is_file(),'missing removal inspection'
    sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    hashes={str(f.relative_to(package)):sha(f) for f in sorted((package/'build').rglob('*.png'))}
    report={'status':'passed','audit':'read existing exported PNGs; no rebuild',
            'sourceSha256':sha(package/'reference.png'),'manifestSha256':sha(package/'build/manifest.json'),
            'sourcePixelCount':int(source.shape[0]*source.shape[1]),'intactDifferentPixels':0,
            'sourcePixelLayers':source_layers,'inferredLayers':inferred_layers,
            'owners':len(owners),'masks':len(owners),'pngFiles':len(hashes),
            'unownedBackingChanges':0,'removalInspections':sum(c['mutable'] for c in manifest['components']),
            'checks':['real RGBA and binary alpha','PNG bounds and nonempty masks','original-source layer RGB fidelity',
                      'fresh independent reconstruction equals source','known pixels outside source ownership unchanged',
                      'owner/layer references and removal inspections complete']}
    (package/'reports/asset-hashes.json').write_text(json.dumps({'sourceSha256':report['sourceSha256'],'files':hashes},indent=2)+'\n')
    (package/'reports/export-audit.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':print(json.dumps(verify(),indent=2))
