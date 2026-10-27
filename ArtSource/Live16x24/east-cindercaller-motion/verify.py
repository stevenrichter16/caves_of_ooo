"""Decode-only Cindercaller source verification; never edit/import image files."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PALETTE = {'ink':'342E25','stone_dark':'3C5148','stone':'526657',
           'stone_light':'7D8E71','tawny':'A97542','claw':'BCA576','rust':'9B442B'}
POSES = ('walk-a','walk-b','attack','hurt','spit-cast')
LIMBS = {'bent-hind-leg','hind-ankle','hind-foot','hind-toe','near-shoulder',
         'near-wrist','near-claw','far-forearm','far-claw'}
FACE = {'brow','blunt-face','eye','nose'}
RGBA = {k:tuple(bytes.fromhex(v))+(255,) for k,v in PALETTE.items()}

def pixel(c):
    return RGBA[c[1]] if c else (0,0,0,0)

def validate_frames(source, idle):
    assert source['palette'] == PALETTE, 'Approved palette changed'
    frames = source['frames']
    assert len(frames) == 5
    assert [(f['index'],f['pose'])for f in frames] == list(enumerate(POSES))
    original_parts = {c[0]for row in idle for c in row if c}
    decoded = []
    reports = []
    for i,f in enumerate(frames):
        assert f['id'] == 'cindercaller-'+POSES[i] and f['blueprint'] == 'MarlbackCindercaller'
        cells = f['cells']
        assert len(cells) == 24 and all(len(row) == 16 for row in cells)
        assert all(c is None or isinstance(c,list) and len(c)==2 and c[1]in PALETTE
                   and c[0]in original_parts|{'spit-neck'} for row in cells for c in row)
        values = [pixel(c)for row in cells for c in row]
        assert values not in decoded, 'Duplicate motion pose'
        assert values != [pixel(c)for row in idle for c in row], 'Idle duplicated as motion'
        decoded.append(values)
        occupied = [(x,y)for y,row in enumerate(cells)for x,c in enumerate(row)if c]
        assert 90 <= len(occupied) <= 160, 'Changed body scale/mass'
        assert max(y for x,y in occupied) == 22 and all(c is None for c in cells[23])
        assert min(y for x,y in occupied) in (5,6), 'Narrow cap height drift'
        parts = {c[0]for row in cells for c in row if c}
        assert {'narrow-cap','charred-mantle-edge','eye','nose','hind-foot','near-claw','far-claw'} <= parts
        assert len({c[1]for row in cells for c in row if c}) == 7
        # Independent unchanged anatomy comparisons use the frozen original grid,
        # not the production generator or its own output declarations.
        actual_body = {(x,y):c for y,row in enumerate(cells)for x,c in enumerate(row)
                       if c and c[0]not in LIMBS and c[0]!='spit-neck'}
        expected_body = {}
        for y,row in enumerate(idle):
            for x,c in enumerate(row):
                if not c or c[0]in LIMBS: continue
                dx,dy = (-1,1) if f['pose']=='hurt' else (1,-1) if f['pose']=='spit-cast' and c[0]in FACE else (0,0)
                expected_body[(x+dx,y+dy)] = c
        assert actual_body == expected_body, ('Body/face transform',f['pose'])
        if f['pose']=='spit-cast':
            assert {(x,y):c for y,row in enumerate(cells)for x,c in enumerate(row)if c and c[0]in LIMBS} == {
                (x,y):c for y,row in enumerate(idle)for x,c in enumerate(row)if c and c[0]in LIMBS}, 'Spit must not borrow attack limbs'
            assert [(x,y,c[1])for y,row in enumerate(cells)for x,c in enumerate(row)if c and c[0]=='spit-neck'] == [(11,17,'tawny')]
        else:
            assert 'spit-neck' not in parts
        reports.append({'index':i,'pose':f['pose'],'opaquePixels':len(occupied),'colors':7,
                        'bounds':[min(x for x,y in occupied),min(y for x,y in occupied),max(x for x,y in occupied)+1,23]})
    return reports

def counterchecks(source,idle):
    mutations = []
    def check(name,mutate):
        changed=copy.deepcopy(source); mutate(changed)
        try: validate_frames(changed,idle)
        except AssertionError: mutations.append(name); return
        raise AssertionError('Countercheck wrongly accepted: '+name)
    check('duplicated_walk_pose',lambda s:s['frames'][1].__setitem__('cells',copy.deepcopy(s['frames'][0]['cells'])))
    check('repainted_approved_cap',lambda s:s['frames'][0]['cells'][5].__setitem__(7,['cap-crown-edge','rust']))
    check('transparent_baseline_lost',lambda s:s['frames'][0]['cells'][23].__setitem__(0,['hind-foot','tawny']))
    check('hurt_leaves_old_crown',lambda s:s['frames'][3]['cells'][5].__setitem__(7,['cap-crown-edge','stone_light']))
    check('spit_borrows_attack_hand',lambda s:s['frames'][4]['cells'][21].__setitem__(10,None))
    check('held_weapon_invented',lambda s:s['frames'][2]['cells'][4].__setitem__(15,['cudgel','tawny']))
    check('new_palette_hue',lambda s:s['palette'].__setitem__('rust','FF6600'))
    check('fake_empty_ammo_body',lambda s:s['frames'][4]['cells'][12].__setitem__(4,None))
    return mutations

def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--check-unity',action='store_true');args=parser.parse_args()
    required=('native.blend','east-cindercaller-motion.png','east-cindercaller-motion.png.meta',
              'source-cells.json','manifest.json','contact-native.png','contact.png','preserved-inputs.json')
    missing=[n for n in required if not (HERE/n).is_file()]
    assert not missing, 'Source RED: Cindercaller motion outputs missing: '+', '.join(missing)
    source=json.loads((HERE/'source-cells.json').read_text()); manifest=json.loads((HERE/'manifest.json').read_text())
    approved=json.loads((ROOT/'ArtSource/Live16x24/cindercaller/source-cells.json').read_text())
    assert approved['palette']==PALETTE and approved['blueprint']=='MarlbackCindercaller'
    idle=approved['cells'];reports=validate_frames(source,idle);counters=counterchecks(source,idle)
    native=Image.open(HERE/'east-cindercaller-motion.png').convert('RGBA')
    small=Image.open(HERE/'contact-native.png').convert('RGBA'); large=Image.open(HERE/'contact.png').convert('RGBA')
    assert native.size==(80,24) and small.size==(152,40) and large.size==(1216,320)
    assert set(native.getchannel('A').tobytes())=={0,255}
    assert set(small.getchannel('A').tobytes())==set(large.getchannel('A').tobytes())=={255}
    for i,f in enumerate(source['frames']):
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row): assert native.getpixel((16*i+x,y))==pixel(c),('Native/source mismatch',i,x,y)
    background=tuple(bytes.fromhex('032F23'))+(255,)
    contact_cells=[idle]+[f['cells']for f in source['frames']]
    for y in range(40):
        for x in range(152):
            expected=background
            if 8<=y<32:
                for column,cells in enumerate(contact_cells):
                    left=8+24*column
                    if left<=x<left+16 and cells[y-8][x-left]: expected=pixel(cells[y-8][x-left])
            assert small.getpixel((x,y))==expected,('Native contact mismatch',x,y)
            assert all(large.getpixel((x*8+dx,y*8+dy))==expected for dx in range(8)for dy in range(8)),('8x contact mismatch',x,y)
    digest=hashlib.sha256((HERE/'east-cindercaller-motion.png').read_bytes()).hexdigest()
    decoded=hashlib.sha256(native.tobytes()).hexdigest()
    assert manifest['nativeSHA256']==digest and manifest['decodedPixelSHA256']==decoded
    assert [manifest[k]for k in ('frameWidth','frameHeight','frameCount','width','height')]==[16,24,5,80,24]
    assert manifest['resource']=='Sprites/Live16x24/east-cindercaller-motion'
    assert manifest['actorPivot']==[.5,0] and manifest['pixelsPerUnit']==16 and manifest['unityExportDeferred']is True
    assert manifest['idleBorrow']=={'resource':'Sprites/Live16x24/cindercaller','frame':0}
    preserved=json.loads((HERE/'preserved-inputs.json').read_text())
    assert manifest['referenceSHA256']==preserved
    for path,before in preserved.items(): assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==before,('Approved input changed',path)
    # Existing reviewed semantic contract tolerates Unity's added defaults while
    # requiring every authored setting and identifier; no new YAML framework.
    sys.dont_write_bytecode=True;sys.path.insert(0,str(HERE.parent/'cudgel'))
    from metadata_contract import parse_metadata,assert_authored_subset,authored_contract_counts
    metadata=parse_metadata((HERE/'east-cindercaller-motion.png.meta').read_text())
    assert len(metadata['guid'])==32 and all(c in '0123456789abcdef'for c in metadata['guid'])
    assert_authored_subset({'fileFormatVersion':2,'TextureImporter':{
      'mipmaps':{'enableMipMap':0},'textureSettings':{'filterMode':0,'wrapU':1,'wrapV':1,'wrapW':1},
      'nPOTScale':0,'spritePixelsToUnits':16,'spritePivot':{'x':.5,'y':.5},
      'spriteSheet':{'sprites':[],'outline':[],'physicsShape':[],'vertices':[]},
      'platformSettings':[{'textureCompression':0}]}},metadata)
    if args.check_unity:
        runtime=ROOT/'Assets/Resources/Sprites/Live16x24/east-cindercaller-motion.png'
        assert runtime.read_bytes()==(HERE/'east-cindercaller-motion.png').read_bytes()
        assert_authored_subset(metadata,parse_metadata(Path(str(runtime)+'.meta').read_text()))
    report={'passed':True,'atlasSize':[80,24],'frameCount':5,'frames':reports,
      'sourcePixelMismatches':0,'contactPixelMismatches':0,'counterchecksRejected':counters,
      'preservedInputCount':len(preserved),'nativeSHA256':digest,'decodedPixelSHA256':decoded,
      'authoredMetadataContract':authored_contract_counts(metadata),'runtimeCopyChecked':args.check_unity,
      'limits':'Source geometry/render/metadata only; root owns Unity import, state/event admission, motion and actual-game acceptance.'}
    (HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))

if __name__=='__main__': main()
