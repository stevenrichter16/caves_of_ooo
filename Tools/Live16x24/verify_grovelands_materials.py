"""Decode-only source validation; does not create or edit image pixels."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/grovelands-materials'
OWNERS = ['MycelialColumn', 'MycelialColumn', 'GroveSeep', 'GroveSeep',
          'GroveSign', 'GroveRedGrowth', 'FruitingBody', 'FruitingBody',
          'CompostRow', 'CompostRow', 'CompostRow', 'CompostCache',
          'ChoirIronVein', 'WineLeafSundew']
PALETTE = {
    'root-dark':'53604C', 'root':'89947A', 'pale':'B5A47E', 'pale-light':'C9C39A',
    'violet-dark':'4D414E', 'violet':'6B596B', 'violet-light':'887486',
    'water-deep':'183F3D', 'water':'285551', 'water-light':'4D7570',
    'film':'214541', 'film-glint':'3B5C55',
    'wood-dark':'493D2D', 'wood':'665438', 'wood-grain':'887145',
    'green':'53683E', 'green-light':'75834B',
    'red-dark':'54362E', 'red':'9B442B', 'red-light':'B85834',
    'shelf-base':'806746', 'shelf-lip':'A38B63',
    'compost-dark':'303D30', 'compost':'43513A', 'compost-fiber':'65705A',
    'cloth':'887C62', 'cloth-dark':'5D5848',
    'ore-dark':'3C5148', 'ore':'526657', 'ore-edge':'7D8E71',
    'wine-dark':'412D34', 'wine':'653B46', 'wine-edge':'885457', 'dew':'A18B7C'
}
PARTS = [
    {'tapered-living-column','root-filament','broken-violet-vein'},
    {'tapered-living-column','root-filament','broken-violet-vein'},
    {'white-filament-rim','available-water','water-level-reflection'},
    {'white-filament-rim','persistent-wet-film','film-reflection'},
    {'grown-support','living-board','raised-grain'},
    {'rooted-support','heavy-red-growth','raw-fold'},
    {'solid-shelf-base','folded-violet-lobe','restrained-lip'},
    {'solid-shelf-base','folded-violet-lobe','restrained-lip'},
    {'organic-row','quiet-remnant'}, {'organic-row','frayed-cloth'},
    {'organic-row','chitin-remnant'}, {'organic-row','sagged-pocket','pocket-fold'},
    {'dark-ore-deposit','metal-fracture'}, {'wine-leaf','sticky-bead'}
]

def points(cells):
    return {(x,y) for y,row in enumerate(cells) for x,c in enumerate(row) if c}

def contract(source):
    assert source['palette'] == PALETTE, 'Exact reviewed material palette'
    fs = source['frames']; assert len(fs) == 14, 'Fourteen native frames'
    measure, unique = [], set()
    for i,f in enumerate(fs):
        assert f['index'] == i and f['blueprint'] == OWNERS[i], 'Exact owner/frame order'
        cells = f['cells']
        assert len(cells) == 24 and all(len(row) == 16 for row in cells), '16x24 integer grid'
        assert all(c is None or isinstance(c,list) and len(c)==2 and isinstance(c[0],str)
                   and c[1] in PALETTE for row in cells for c in row), 'Known named palette cells'
        pts=points(cells); assert 35 <= len(pts) <= 260, 'Bounded foreground silhouette'
        xs,ys=zip(*pts); bounds=[min(xs),min(ys),max(xs)+1,max(ys)+1]
        assert bounds[3] == 23 and not any(cells[23]), 'Ground baseline22, clear bottom row'
        colors={c[1] for row in cells for c in row if c}; parts={c[0] for row in cells for c in row if c}
        assert 3 <= len(colors) <= 7, 'Restrained material colors'
        assert PARTS[i] <= parts, 'Required distinct owner material structure'
        # Compare visible colors, not arbitrary part labels masquerading as variants.
        signature=tuple(c[1] if c else None for row in cells for c in row)
        assert signature not in unique, 'Every supplied visible frame distinct'; unique.add(signature)
        if i < 2:
            assert bounds[3]-bounds[1] >= 20 and 8 <= bounds[2]-bounds[0] <= 13, 'Tall narrow living column'
            assert sum(bool(c) and c[0]=='broken-violet-vein' for row in cells for c in row) <= 13, 'No luminous vein carpet'
        if i in (2,3):
            assert bounds[1] >= 12 and bounds[2]-bounds[0] >= 12, 'Low single-cell filament basin'
            wet={'water-deep','water','water-light'} if i==2 else {'water-deep','film','film-glint'}
            assert sum(bool(c) and c[1] in wet for row in cells for c in row) >= 36, 'Water/empty-film stays visibly wet'
            assert f['state']['stillWet'] is True and f['state']['availableStock'] is (i==2), 'Actual finite stock with permanent coating'
        if i==4: assert bounds[3]-bounds[1] >= 16 and bounds[2]-bounds[0] <= 12, 'One grown sign, not canopy or tree'
        if i==5: assert bounds[1] >= 12 and bounds[2]-bounds[0] >= 10, 'Low attached red growth'
        if i in (6,7): assert 10 <= bounds[3]-bounds[1] <= 17 and bounds[2]-bounds[0] >= 12, 'Squat solid folded shelves'
        if i in (8,9,10,11):
            assert bounds[1] >= 15 and bounds[2]-bounds[0] == 16, 'Low walkable row with continuous substrate'
            assert not any('coin' in n or 'skull' in n or 'mushroom' in n for n in parts), 'No invented treasure/bodies/crop'
        if i==12: assert bounds[2]-bounds[0] >= 10 and bounds[3]-bounds[1] <= 14, 'Compact grounded ore deposit'
        if i==13:
            assert bounds[1] >= 12 and bounds[2]-bounds[0] >= 12, 'Low broad sundew rosette'
            assert 1 <= sum(bool(c) and c[0]=='sticky-bead' for row in cells for c in row) <= 4, 'Sparse subdued sticky cue'
        measure.append({'index':i,'owner':OWNERS[i],'opaquePixels':len(pts),'bounds':bounds,'colors':len(colors)})
    assert points(fs[2]['cells']) == points(fs[3]['cells']), 'Seep state keeps its basin boundary'
    assert fs[2]['state']['volumeRule'] == 'Volume > 0' and fs[3]['state']['volumeRule'] == 'Volume == 0', 'Finite stock state order'
    return measure

def verify(check_unity=False):
    needed=['grovelands-materials.png','grovelands-materials.png.meta','grovelands-materials.blend',
            'source-cells.json','manifest.json','contact-native.png','contact.png','preserved-inputs.json']
    missing=[n for n in needed if not (OUT/n).is_file()]
    assert not missing, 'Source RED: missing '+', '.join(missing)
    source=json.loads((OUT/'source-cells.json').read_text()); m=json.loads((OUT/'manifest.json').read_text())
    measurements=contract(source)
    im=Image.open(OUT/'grovelands-materials.png').convert('RGBA')
    assert im.size==(224,24) and set(im.getchannel('A').tobytes())=={0,255}, 'Native size and binary alpha'
    assert [m[k] for k in ('width','height','frameCount','frameWidth','frameHeight')]==[224,24,14,16,24]
    assert m['resource']=='Sprites/Live16x24/grovelands-materials' and m['unityExportDeferred'] is True
    rgba={k:tuple(bytes.fromhex(v))+(255,) for k,v in PALETTE.items()}
    for i,f in enumerate(source['frames']):
        assert m['frames'][i]['blueprint']==OWNERS[i] and m['frames'][i]['unityRect']==[16*i,0,16,24]
        assert m['frames'][i]['state']==f['state']
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                assert im.getpixel((16*i+x,y))==(rgba[c[1]] if c else (0,0,0,0)), ('Native/source mismatch',i,x,y)
    for filename,scale in [('contact-native.png',1),('contact.png',8)]:
        contact=Image.open(OUT/filename).convert('RGBA');cm=m['contact']
        assert contact.size==tuple(n*scale for n in cm['nativeSize'])
        for p in cm['instances']:
            bg=tuple(bytes.fromhex(p['background']))+(255,)
            for y,row in enumerate(source['frames'][p['frame']]['cells']):
                for x,c in enumerate(row):
                    expected=rgba[c[1]] if c else bg
                    for dy in range(scale):
                        for dx in range(scale):
                            assert contact.getpixel(((p['topLeft'][0]+x)*scale+dx,(p['topLeft'][1]+y)*scale+dy))==expected, ('Direct contact mismatch',filename,p,x,y)
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    for path,sha in preserved.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha, ('Approved input changed',path)
    sys.path.insert(0,str(ROOT/'ArtSource/Live16x24/cudgel'))
    from metadata_contract import parse_metadata,assert_authored_subset
    meta=parse_metadata((OUT/'grovelands-materials.png.meta').read_text())
    assert len(meta['guid'])==32 and all(c in '0123456789abcdef' for c in meta['guid'])
    required={'fileFormatVersion':2,'TextureImporter':{'mipmaps':{'enableMipMap':0,'sRGBTexture':1},
      'textureSettings':{'filterMode':0,'wrapU':1,'wrapV':1,'wrapW':1},'nPOTScale':0,'spriteMode':1,
      'spriteMeshType':0,'spritePixelsToUnits':16,'spritePivot':{'x':.5,'y':.5},
      'spriteSheet':{'sprites':[],'outline':[],'physicsShape':[],'vertices':[]},
      'platformSettings':[{'textureCompression':0}]}}
    assert_authored_subset(required,meta)
    counters=[]
    def replace_part(s,i,old,new):
        for row in s['frames'][i]['cells']:
            for c in row:
                if c and c[0]==old:c[0]=new
    def dry_empty(s):
        for row in s['frames'][3]['cells']:
            for c in row:
                if c and c[1] in ('water-deep','film','film-glint'):c[1]='wood-dark'
    for name,mutate in [
      ('missing-frame',lambda s:s['frames'].pop()),
      ('wrong-owner',lambda s:s['frames'][6].__setitem__('blueprint','BloomingFruitingBody')),
      ('wrong-frame-order',lambda s:s['frames'][1].__setitem__('index',0)),
      ('duplicate-visible-variant',lambda s:s['frames'][1].__setitem__('cells',copy.deepcopy(s['frames'][0]['cells']))),
      ('wrong-native-grid',lambda s:s['frames'][0]['cells'][0].append(None)),
      ('out-of-baseline-pixel',lambda s:s['frames'][0]['cells'][23].__setitem__(7,['root-filament','root'])),
      ('unapproved-palette',lambda s:s['palette'].__setitem__('violet','FF00FF')),
      ('dry-empty-seep',dry_empty),
      ('empty-promises-stock',lambda s:s['frames'][3]['state'].__setitem__('availableStock',True)),
      ('different-empty-footprint',lambda s:s['frames'][3]['cells'][14].__setitem__(7,None)),
      ('column-missing-veins',lambda s:replace_part(s,0,'broken-violet-vein','plain-bark')),
      ('cache-promises-coins',lambda s:replace_part(s,11,'pocket-fold','guaranteed-coins')),
      ('sundew-missing-dew',lambda s:replace_part(s,13,'sticky-bead','food-berry'))]:
        modified=copy.deepcopy(source);mutate(modified)
        try:contract(modified)
        except AssertionError:counters.append(name)
        else:raise AssertionError('Counter accepted '+name)
    changed=copy.deepcopy(meta);changed['TextureImporter']['textureSettings']['filterMode']=1
    try:assert_authored_subset(required,changed)
    except AssertionError:counters.append('metadata-filter-changed')
    else:raise AssertionError('Metadata counter accepted')
    if check_unity:
        target=ROOT/'Assets/Resources/Sprites/Live16x24/grovelands-materials.png'
        assert target.read_bytes()==(OUT/'grovelands-materials.png').read_bytes()
        assert_authored_subset(meta,parse_metadata(Path(str(target)+'.meta').read_text()))
    sha=hashlib.sha256((OUT/'grovelands-materials.png').read_bytes()).hexdigest();assert m['sha256']==sha
    return {'passed':True,'frames':14,'size':[224,24],'measurements':measurements,
      'binaryAlpha':True,'nativePixelMismatches':0,'contactPixelMismatches':0,
      'preservedInputsUnchanged':len(preserved),'counterchecksRejected':counters,
      'nativeSHA256':sha,'decodedPixelSHA256':hashlib.sha256(im.tobytes()).hexdigest(),
      'runtimeCopyChecked':check_unity,
      'limits':'Original editable native source and decoded pixel verification only. No Unity tests, actual regional admission, gameplay, source harvesting, scene fullness or device performance acceptance.'}

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--check-unity',action='store_true');args=p.parse_args()
    try:result=verify(args.check_unity)
    except (AssertionError,OSError,KeyError,ValueError) as e:result={'passed':False,'error':str(e)}
    (OUT/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2));raise SystemExit(0 if result['passed'] else 1)
