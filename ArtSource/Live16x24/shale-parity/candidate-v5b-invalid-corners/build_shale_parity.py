"""Isolated shale reference study: original editable integer-quad native art.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify
Blender --background .../shale-parity.blend --python this-file -- --inspect-blend
Original geometry only. Pillow decodes verification pixels; never edits images.
"""
import argparse
import copy
import hashlib
import json
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/shale-parity'
RESOURCE = ROOT / 'Assets/Resources/Sprites/Live16x24/shale-parity.png'
RUBBLE_RESOURCE = RESOURCE.with_name('shale-parity-rubble.png')
W, H, FLOOR_COUNT, WALL_VARIANTS = 16, 24, 8, 4
COUNT = FLOOR_COUNT + 16 * WALL_VARIANTS
ATLAS_W = COUNT * W
PALETTE = {
    'floor':'3F444A', 'chip':'575C62', 'chip-light':'696E74', 'chip-shadow':'33383E',
    'crevice':'111214', 'deep':'1D2025', 'mass':'272B33',
    'slab':'31373F', 'facet':'3D434C', 'edge':'4A5159', 'cut':'545B63',
}
RUBBLE_PALETTE = {'contact':'2C3137','lower':'454B52','face':'5B6168','facet':'70767C','fresh':'7E848A'}
CONTACT_W, CONTACT_H = 448, 240
STUDY_W, STUDY_H = 240, 248

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name, value):
    assert '/' not in name and name.endswith('.json')
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name, expected in pins['exact_sha256'].items():
        assert sha(ROOT/name)==expected, 'Protected input changed: '+name
    revision=OUT/'revision-protected-inputs.json'
    if revision.exists():
        for name,expected in json.loads(revision.read_text())['exact_sha256'].items():
            assert sha(ROOT/name)==expected,'Protected rejected candidate changed: '+name
    iteration3=OUT/'iteration3-protected-inputs.json'
    if iteration3.exists():
        for name,expected in json.loads(iteration3.read_text())['exact_sha256'].items():
            assert sha(ROOT/name)==expected,'Protected actual v2 candidate changed: '+name
    iteration4=OUT/'iteration4-protected-inputs.json'
    if iteration4.exists():
        for name,expected in json.loads(iteration4.read_text())['exact_sha256'].items():
            assert sha(ROOT/name)==expected,'Protected actual v3/original Rubble input changed: '+name
    iteration5=OUT/'iteration5-protected-inputs.json'
    if iteration5.exists():
        for name,expected in json.loads(iteration5.read_text())['exact_sha256'].items():
            assert sha(ROOT/name)==expected,'Protected actual v4 input changed: '+name
    iteration5a=OUT/'iteration5a-protected-inputs.json'
    if iteration5a.exists():
        for name,expected in json.loads(iteration5a.read_text())['exact_sha256'].items():
            assert sha(ROOT/name)==expected,'Protected rejected staged v5a changed: '+name
    return pins
def rgba(value): return tuple(bytes.fromhex(PALETTE[value[1]]))+(255,) if value else (0,0,0,0)

def contour_profiles(variant):
    # Slowly changing1–3pixel faceted offsets, no isolated pinprick cutouts.
    horizontal=[
        [1,1,1,2,2,3,3,3,2,2,1,1,1,1,2,2],
        [2,2,1,1,1,1,2,2,3,3,3,2,2,1,1,1],
        [1,1,2,2,2,3,3,2,2,1,1,1,2,2,2,1],
        [3,3,2,2,1,1,1,2,2,2,3,3,2,2,1,1],
    ]
    vertical=[
        [1,1,1,2,2,2,3,3,3,2,2,1,1,1,1,2,2,3,3,2,2,2,1,1],
        [2,2,1,1,1,2,2,3,3,3,2,2,1,1,1,1,2,2,2,3,3,2,2,1],
        [1,1,2,2,3,3,3,2,2,2,1,1,1,2,2,3,3,2,2,1,1,1,2,2],
        [3,3,2,2,2,1,1,1,2,2,3,3,2,2,1,1,1,1,2,2,2,3,3,2],
    ]
    return {1:horizontal[variant],2:vertical[(variant+1)%4],
            4:list(reversed(horizontal[(variant+2)%4])),8:vertical[variant]}

def apply_contour(cells,mask,variant):
    profiles=contour_profiles(variant)
    for y in range(H):
        for x in range(W):
            if ((mask&1 and y<profiles[1][x]) or (mask&2 and W-1-x<profiles[2][y])
                or (mask&4 and H-1-y<profiles[4][x]) or (mask&8 and x<profiles[8][y])):
                cells[y][x]=None
    # Restrained light/shade belongs to the remaining actual wall boundary.
    # Corner unions remain cut: a perpendicular exposed edge may erase a tip.
    for bit,profile in profiles.items():
        if not mask&bit:continue
        for along,depth in enumerate(profile):
            x,y=((along,depth) if bit==1 else (W-1-depth,along) if bit==2
                 else (along,H-1-depth) if bit==4 else (depth,along))
            if cells[y][x] is None:continue
            color=('edge' if along%7 in(2,3,4) else 'facet') if bit in(1,8) else ('deep' if along%7<5 else 'mass')
            cells[y][x]=['faceted-exposed-boundary-'+str(bit),color]
    return cells

def source_cells():
    floors=[]
    # Literal original flat contours. Unlike v3, there is no centered apex or
    # common wider-middle/narrow-bottom silhouette. '+' is a broken lit edge,
    # 'o' is flat surface, '_' a short contact line. Spaces remain base floor.
    # Eight arrangements deliberately vary orientation, density and topology.
    patterns=[
        [(2,7,["++oooooo", " oooooo_", "    ___ "])],
        [(9,10,["+oo ","ooo "," oo "," oo_","  o_","  __"])],
        [(2,13,["+oooooooo ","oooooooooo","ooooooooo_"," oooooo__ ","  _____   "])],
        [(4,5,["     ++o "," ooooooo ","ooooooooo","oooooooo_","ooooo__  "," ___     "])],
        [(2,4,["++ooooo", "oooooo_", " oooo_ "," ___   "]), (8,17,["  +ooo"," ooooo","ooooo_"," ___  "])],
        [(7,3,["  ++ooo"," oooooo","oooooo_"," ooo__ "]), (2,16,["+ooooo ","oooooo_","ooo___ "])],
        [(3,4,["++oooooo", "oooooooo", "ooooooo_", " oooo_  ", " oo__   "," __     "])],
        [(1,16,["++ooooooo ","ooooooooo_","ooooooo__ "," ___      "]), (10,5,[" +oo"," ooo","ooo_","oo_ ","__  "])],
    ]
    for variant,places in enumerate(patterns):
        cells=[[['flat-slate-ground','floor'] for _ in range(W)] for _ in range(H)]
        for c,(left,top,rows) in enumerate(places):
            for dy,row in enumerate(rows):
                for dx,ch in enumerate(row):
                    if ch==' ':continue
                    x,y=left+dx,top+dy
                    assert 0<x<15 and 0<y<23 and cells[y][x][1]=='floor'
                    cells[y][x]=['flat-fractured-shard-'+str(c),{'+':'chip-light','o':'chip','_':'chip-shadow'}[ch]]
        floors.append(cells)
    def wall(variant):
        cells=[[['continuous-stone-mass','mass'] for _ in range(W)] for _ in range(H)]
        def put(x,y,part,color):
            if 0<=x<W and 0<=y<H: cells[y][x]=[part,color]
        # Long horizontal strata connect through tile sides. Unequal fractured
        # interior faces replace v4's closed rectangle/mortar construction and
        # v5a's isolated diagonal patches. No standalone dark recess pockets.
        strata=[
            [(-4,-1,20,3),(2,4,17,3),(-3,8,19,4),(10,12,11,3),(-4,14,14,3),(1,19,18,4)],
            [(-1,0,20,2),(-5,4,17,4),(9,7,13,3),(-3,12,20,3),(3,16,19,4),(-4,21,18,3)],
            [(4,-1,18,4),(-5,4,19,3),(1,8,19,3),(-3,12,12,4),(10,14,11,3),(-2,19,20,4)],
            [(-4,-1,18,4),(7,3,15,3),(-3,7,20,4),(1,13,19,3),(-5,17,16,3),(7,20,15,4)],
        ][variant]
        for index,(left,top,length,height) in enumerate(strata):
            for yy in range(height):
                # Only one end slopes at a time. Opposite ends are not rounded
                # together into a masonry block; neighboring mass stays joined.
                start=left+(1-yy if (index+variant)%2==0 else 0)
                end=left+length-(yy if (index+variant)%2 else 0)
                for xx in range(start,end):
                    local=xx-left
                    color='slab' if yy<height-1 else 'mass'
                    facet_start=2+((index+variant)%3)+yy
                    if facet_start<=local<facet_start+7-(yy//2):color='facet'
                    if yy==0 and local in range(3,8):color='edge'
                    if yy==1 and local in(4+index%2,5+index%2):color='edge'
                    if yy==0 and local==8 and index%2==0:color='cut'
                    if yy==height-1 and local>length-4:color='deep'
                    put(xx,top+yy,'connected-stratum-'+str(index)+'-unequal-face',color)
            # Horizontal interrupted recess follows this layer's underside.
            # A2–4pixel break opens into mass; no enclosing outline or pocket.
            gap=left+7+((index+variant)%4)
            for xx in range(left+1,left+length-1):
                if gap<=xx<gap+3:continue
                put(xx,top+height,'stratum-'+str(index)+'-interrupted-undercut','crevice' if xx<gap else 'deep')
            if index%2==variant%2:
                for xx in range(left+2,left+7):
                    put(xx,top+height+1,'stratum-'+str(index)+'-short-lower-undercut','deep')
        return cells
    frames=floors
    for mask in range(16):
        for variant in range(WALL_VARIANTS):
            cells=wall(variant)
            frames.append(apply_contour(cells,mask,variant))
    return frames

def rubble_cells():
    # Three unequal original plates, all low and horizontally fractured. The
    # gray body depicts actual Rubble; no loot glint, planted peak or new owner.
    cells=[[None for _ in range(16)] for _ in range(16)]
    fragments=[
        (1,8,["ffoo oo ","ooooooll"," ooolll "," lll    "]),
        (7,5,["ffFoo   ","ooooofo "," oooooll","  lll l "]),
        (10,11,["ffooo","ooo l","ll   "]),
    ]
    colors={'f':'facet','F':'fresh','o':'face','l':'lower'}
    for index,(left,top,rows) in enumerate(fragments):
        for yy,row in enumerate(rows):
            for xx,ch in enumerate(row):
                if ch!=' ':
                    assert cells[top+yy][left+xx] is None
                    cells[top+yy][left+xx]=['fractured-plate-'+str(index),colors[ch]]
    for x,y in [(1,10),(2,11),(3,12),(4,12),(5,12),(6,12),(8,9),(9,9),(10,9),(11,9),(12,9),(13,9),(11,14),(12,14),(13,13),(14,12)]:
        if cells[y][x] is None:cells[y][x]=['shallow-shared-contact','contact']
    return cells

def contact_placements():
    p=[{'frame':i,'left':10+i*28,'top':18} for i in range(FLOOR_COUNT)]
    p += [{'frame':FLOOR_COUNT+i,'left':280+i*28,'top':18} for i in range(WALL_VARIANTS)]
    for mask in range(16):
        for v in range(WALL_VARIANTS): p.append({'frame':FLOOR_COUNT+mask*WALL_VARIANTS+v,'left':22+(mask%4)*108+v*16,'top':64+(mask//4)*39})
    return p

STUDY=[
    '##############',
    '###......#####',
    '##.........###',
    '##..........##',
    '###...##....##',
    '##....##...###',
    '##.........###',
    '####.....#####',
    '##############',
]
def study_placements():
    p=[]
    for y,row in enumerate(STUDY):
        for x,ch in enumerate(row):
            value=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff
            value^=value>>16;value=(value*0x7feb352d)&0xffffffff;value^=value>>15
            if ch=='.': frame=value%FLOOR_COUNT
            else:
                mask=0
                for dx,dy,bit in ((0,-1,1),(1,0,2),(0,1,4),(-1,0,8)):
                    xx,yy=x+dx,y+dy
                    if 0<=yy<len(STUDY) and 0<=xx<len(row) and STUDY[yy][xx]=='.':mask|=bit
                frame=FLOOR_COUNT+mask*WALL_VARIANTS+(value%WALL_VARIANTS)
            p.append({'frame':frame,'left':8+x*16,'top':22+y*24,
                      'underlay':value%FLOOR_COUNT if ch=='#' and mask else None})
    return p

def validate_wall_topology(cells,mask):
    opaque={(x,y) for y,row in enumerate(cells) for x,v in enumerate(row) if v}
    core={(x,y) for y in range(3,21) for x in range(3,13)}
    assert core<=opaque,'Wall core cut away'
    if mask==0:assert len(opaque)==W*H,'Mask0 must remain opaque'
    for y in range(H):
        for x in range(W):
            if (x,y) in opaque:continue
            eligible=[]
            if mask&1 and y<3:eligible.append(all(cells[yy][x] is None for yy in range(y+1)))
            if mask&2 and W-1-x<3:eligible.append(all(cells[y][xx] is None for xx in range(x,W)))
            if mask&4 and H-1-y<3:eligible.append(all(cells[yy][x] is None for yy in range(y,H)))
            if mask&8 and x<3:eligible.append(all(cells[y][xx] is None for xx in range(x+1)))
            assert any(eligible),('Transparency is not attached to a disclosed outer side',mask,x,y)
    # Every opaque piece joins the protected core; no detached corner islands.
    seen={(3,3)};pending=[(3,3)]
    while pending:
        x,y=pending.pop()
        for dx,dy in((0,-1),(1,0),(0,1),(-1,0)):
            q=x+dx,y+dy
            if q in opaque and q not in seen:seen.add(q);pending.append(q)
    assert seen==opaque,'Detached opaque island'
    for bit,point in ((1,(8,0)),(2,(15,12)),(4,(8,23)),(8,(0,12))):
        x,y=point;assert (cells[y][x] is None)==bool(mask&bit),'Wrong exposed side'

def validate(frames,size,pixels):
    assert frames==source_cells(),'Authored source changed'
    assert size==(ATLAS_W,H) and len(pixels)==ATLAS_W*H*4,'Wrong exact canvas'
    assert len(PALETTE)==11 and len(set(PALETTE.values()))==11
    for index,cells in enumerate(frames):
        for y,row in enumerate(cells):
            for x,value in enumerate(row):
                offset=4*(y*ATLAS_W+index*W+x)
                assert tuple(pixels[offset:offset+4])==rgba(value),('Native mismatch',index,x,y)
        if index<FLOOR_COUNT:
            assert 14<=sum(v[1]!='floor' for row in cells for v in row)<=50
            assert all(cells[y][x][1]=='floor' for y in range(24) for x in range(16) if x in(0,15) or y in(0,23))
    assert len({json.dumps(f) for f in frames})==COUNT,'Duplicate cell'
    assert len({sum(v[1]!='floor' for row in f for v in row) for f in frames[:FLOOR_COUNT]})>=5,'Uniform detail density'
    v4=json.loads((OUT/'candidate-v4/source-cells.json').read_text())
    assert frames[:FLOOR_COUNT]==v4['frames'][:FLOOR_COUNT],'V5 may change chip contrast, not floor density or silhouette'
    assert PALETTE['floor']==v4['palette']['floor'],'Floor base drift'
    for mask in range(16):
        for v in range(WALL_VARIANTS):
            f=frames[FLOOR_COUNT+mask*WALL_VARIANTS+v]
            validate_wall_topology(f,mask)
            profile=contour_profiles(v)
            for y in range(H):
                for x in range(W):
                    expected=bool(mask&1 and y<profile[1][x] or mask&2 and W-1-x<profile[2][y]
                                  or mask&4 and H-1-y<profile[4][x] or mask&8 and x<profile[8][y])
                    assert (f[y][x] is None)==expected,('Stepped edge/corner mismatch',mask,v,x,y)
    return [{'frame':i,'colors':dict(Counter(v[1] if v else 'transparent' for row in f for v in row))} for i,f in enumerate(frames)]

def rubble_rgba(value):
    return tuple(bytes.fromhex(RUBBLE_PALETTE[value[1]]))+(255,) if value else (0,0,0,0)

def validate_rubble(cells,size,pixels):
    assert cells==rubble_cells(),'Rubble source drift'
    assert size==(16,16) and len(pixels)==16*16*4,'Wrong Rubble canvas'
    for y,row in enumerate(cells):
        for x,value in enumerate(row):
            assert tuple(pixels[4*(y*16+x):4*(y*16+x+1)])==rubble_rgba(value),('Rubble pixel',x,y)
    coords=[(x,y) for y,row in enumerate(cells) for x,v in enumerate(row) if v]
    assert 64<=len(coords)<=90 and len({v[1] for row in cells for v in row if v})==5
    assert min(y for x,y in coords)>=5 and max(y for x,y in coords)<15
    return {'opaque_pixels':len(coords),'binary_alpha':True,'bounds':[min(x for x,y in coords),min(y for x,y in coords),max(x for x,y in coords)+1,max(y for x,y in coords)+1]}

def check_meta(data):
    text=data.decode('utf-8')
    expected={'enableMipMap':'0','filterMode':'0','wrapU':'1','wrapV':'1','spritePixelsToUnits':'16','textureCompression':'0','nPOTScale':'0'}
    for field,value in expected.items():
        values=re.findall(r'^\s*'+field+r':\s*(\S+)\s*$',text,re.MULTILINE)
        assert values and all(v==value for v in values),(field,values)
    guid=re.search(r'^guid: ([0-9a-f]{32})\s*$',text,re.MULTILINE);assert guid is not None
    return expected

def verify(require_runtime_identity=True):
    assert (OUT/'shale-parity.png').exists(),'Native parity atlas absent; original geometry render required'
    from PIL import Image
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text())
    assert source['palette']==PALETTE
    with Image.open(OUT/'shale-parity.png') as im:
        assert im.mode=='RGBA';pixels=im.tobytes();stats=validate(source['frames'],im.size,pixels)
    rubble_source=json.loads((OUT/'rubble-source-cells.json').read_text())
    assert rubble_source['palette']==RUBBLE_PALETTE
    with Image.open(OUT/'shale-parity-rubble.png') as im:
        assert im.mode=='RGBA';rubble_pixels=im.tobytes()
        rubble_stats=validate_rubble(rubble_source['cells'],im.size,rubble_pixels)
    checked=0
    for filename,width,height,scale,places in (
        ('contact-native.png',CONTACT_W,CONTACT_H,1,contact_placements()),
        ('contact.png',CONTACT_W,CONTACT_H,4,contact_placements()),
        ('material-study.png',STUDY_W,STUDY_H,4,study_placements())):
        with Image.open(OUT/filename) as im:
            assert im.mode=='RGBA' and im.size==(width*scale,height*scale)
            for p in places:
                for y,row in enumerate(source['frames'][p['frame']]):
                    for x,value in enumerate(row):
                        for dy in range(scale):
                            for dx in range(scale):
                                expected=rgba(value) if value else rgba(source['frames'][p['underlay']][y][x]) if p.get('underlay') is not None else (16,20,26,255)
                                assert im.getpixel(((p['left']+x)*scale+dx,(p['top']+y)*scale+dy))==expected,(filename,p,x,y)
                                checked+=1
    with Image.open(OUT/'rubble-contact.png') as im:
        assert im.mode=='RGBA' and im.size==(768,320)
        for left in (16,40,64):
            for y,row in enumerate(rubble_source['cells']):
                for x,value in enumerate(row):
                    expected=rubble_rgba(value) if value else tuple(bytes.fromhex(PALETTE['floor']))+(255,)
                    for dy in range(8):
                        for dx in range(8):
                            assert im.getpixel(((left+x)*8+dx,(12+y)*8+dy))==expected
                            checked+=1
    rejected=[]
    def reject(name,data=pixels,size=(ATLAS_W,H),frames=source['frames']):
        try: validate(frames,size,data)
        except(AssertionError,IndexError,KeyError,ValueError):rejected.append(name);return
        raise AssertionError('Corruption accepted: '+name)
    reject('wrong canvas',size=(ATLAS_W,16))
    for name,offset,new in [('transparent hole',3,0),('partial alpha',7,127),('altered floor',0,255),('altered wall edge',4*(FLOOR_COUNT*W),255)]:
        bad=bytearray(pixels);bad[offset]=new;reject(name,bytes(bad))
    bad=copy.deepcopy(source['frames']);bad[FLOOR_COUNT]=copy.deepcopy(bad[FLOOR_COUNT+WALL_VARIANTS]);reject('permuted exposure mask',frames=bad)
    bad=copy.deepcopy(source['frames']);bad[FLOOR_COUNT+1]=copy.deepcopy(bad[FLOOR_COUNT]);reject('duplicate variant',frames=bad)
    bad=copy.deepcopy(source['frames']);bad[0][7][4][0]='new-invented-part';reject('part drift',frames=bad)
    def reject_topology(name,cells,mask):
        try:validate_wall_topology(cells,mask)
        except AssertionError:rejected.append(name);return
        raise AssertionError('Unsafe wall transparency accepted: '+name)
    bad=copy.deepcopy(source['frames'][FLOOR_COUNT]);bad[12][8]=None;reject_topology('interior transparent hole',bad,0)
    bad=copy.deepcopy(source['frames'][FLOOR_COUNT+WALL_VARIANTS]);bad[12][15]=None;reject_topology('undisclosed side cut',bad,1)
    bad=copy.deepcopy(source['frames'][FLOOR_COUNT]);bad[0][8]=None;reject_topology('mask0 edge cut',bad,0)
    bad=copy.deepcopy(source['frames'][FLOOR_COUNT+WALL_VARIANTS]);bad[2][8]=None;bad[0][8]=['barrier','mass'];reject_topology('enclosed edge pocket',bad,1)
    bad=[[['core','mass'] if 3<=x<=12 and 3<=y<=20 else None for x in range(W)] for y in range(H)];bad[0][0]=['island','mass'];reject_topology('detached opaque corner',bad,15)
    meta=(ROOT/'Assets/Resources/Sprites/Live16x24/shale-caves.png.meta').read_bytes()
    check_meta(meta)
    try:check_meta(meta.replace(b'filterMode: 0',b'filterMode: 1'))
    except AssertionError:rejected.append('altered importer filtering')
    else:raise AssertionError('Importer corruption accepted')
    def reject_rubble(name,data=rubble_pixels,size=(16,16),cells=rubble_source['cells']):
        try:validate_rubble(cells,size,data)
        except(AssertionError,IndexError,KeyError,ValueError):rejected.append(name);return
        raise AssertionError('Rubble corruption accepted: '+name)
    reject_rubble('Rubble wrong canvas',size=(16,24))
    bad=bytearray(rubble_pixels);bad[3]=127;reject_rubble('Rubble partial alpha',bytes(bad))
    bad=copy.deepcopy(rubble_source['cells']);bad[0][0]=['invented-loot','fresh'];reject_rubble('Rubble source drift',cells=bad)
    runtime=RESOURCE.exists()
    runtime_matches=runtime and sha(RESOURCE)==sha(OUT/'shale-parity.png')
    if require_runtime_identity:assert runtime_matches,'Terrain source staged; matching runtime candidate required'
    if runtime:
        check_meta(Path(str(RESOURCE)+'.meta').read_bytes())
    rubble_runtime=RUBBLE_RESOURCE.exists()
    rubble_runtime_matches=rubble_runtime and sha(RUBBLE_RESOURCE)==sha(OUT/'shale-parity-rubble.png')
    if require_runtime_identity:assert rubble_runtime_matches,'Rubble derivative not yet published'
    if rubble_runtime:check_meta(Path(str(RUBBLE_RESOURCE)+'.meta').read_bytes())
    result={'passed':True,'native_pixels_checked':ATLAS_W*H,'rubble_native_pixels_checked':256,'direct_contact_pixels_checked':checked,
            'frames':COUNT,'dimensions':[ATLAS_W,H],'palette':PALETTE,'floor_and_mask0_opaque':True,'exposed_walls_binary_alpha':True,'connected_core':[3,3,13,21],'frame_stats':stats,'rubble':rubble_stats,
            'protected_inputs_unchanged':len(pins['exact_sha256']),'protected_dirty_inputs':pins['dirty_input_count'],
            'protected_rejected_candidate_inputs':len(json.loads((OUT/'revision-protected-inputs.json').read_text())['exact_sha256']) if (OUT/'revision-protected-inputs.json').exists() else 0,
            'protected_v2_candidate_inputs':len(json.loads((OUT/'iteration3-protected-inputs.json').read_text())['exact_sha256']) if (OUT/'iteration3-protected-inputs.json').exists() else 0,
            'protected_v3_and_original_rubble_inputs':len(json.loads((OUT/'iteration4-protected-inputs.json').read_text())['exact_sha256']),
            'protected_v4_actual_inputs':len(json.loads((OUT/'iteration5-protected-inputs.json').read_text())['exact_sha256']),
            'protected_v5a_source_inputs':len(json.loads((OUT/'iteration5a-protected-inputs.json').read_text())['exact_sha256']),
            'counterchecks_rejected':rejected,'source_png_sha256':sha(OUT/'shale-parity.png'),
            'source_cells_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'shale-parity.blend'),
            'runtime_present':runtime,'runtime_matches_current_source':runtime_matches,'source_only_staged':not require_runtime_identity,'gameplay_acceptance':False,
            'rubble_runtime_present':rubble_runtime,'rubble_runtime_matches_current_source':rubble_runtime_matches,'rubble_source_sha256':sha(OUT/'shale-parity-rubble.png'),
            'limits':'Exact source/geometry-render proof only. Neutral emission synthetic contact, not actual layout, lighting, fog, gameplay or parity acceptance.'}
    if runtime:result['runtime_sha256']=sha(RESOURCE)
    save('source-verification.json' if require_runtime_identity else 'staged-source-verification.json',result);print(json.dumps({k:v for k,v in result.items() if k!='frame_stats'},indent=2))

def canonicalize_timing(path):
    data=path.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';result=bytearray(data[:8]);pos=8
    while pos<len(data):
        n=struct.unpack('>I',data[pos:pos+4])[0];end=pos+12+n;kind=data[pos+4:pos+8];payload=data[pos+8:pos+8+n]
        assert zlib.crc32(kind+payload)&0xffffffff==struct.unpack('>I',data[end-4:end])[0]
        key=payload.split(b'\0',1)[0] if kind==b'tEXt' else b''
        if not(key.startswith(b'cycles.') and key.endswith(b'_time')):result.extend(data[pos:end])
        pos=end
    assert pos==len(data);path.write_bytes(result)

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for,emission_material,add_label
    from build_cindercaller import model
    from build_north_plot import instance,plane
    guard();frames=source_cells();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Shale parity exact native '+str(ATLAS_W)+'x24';collections=[]
    for i,cells in enumerate(frames):
        col=model(native,'Shale parity frame '+str(i),cells,PALETTE);native.collection.children.unlink(col);collections.append(col)
        instance(native,col,'Native frame '+str(i),i*16,0)
    camera_for(native,ATLAS_W,H);native.render.filepath=str(OUT/'shale-parity.png');bpy.ops.render.render(write_still=True,scene=native.name)
    canonicalize_timing(OUT/'shale-parity.png')
    for filename,width,height,scale,places in (
        ('contact-native.png',CONTACT_W,CONTACT_H,1,contact_placements()),
        ('contact.png',CONTACT_W,CONTACT_H,4,contact_placements()),
        ('material-study.png',STUDY_W,STUDY_H,4,study_placements())):
        scene=bpy.data.scenes.new(filename);camera_for(scene,width,height,scale)
        plane(scene,'Quiet source backing','10141A',0,0,width,height,-2)
        for i,p in enumerate(places):
            if p.get('underlay') is not None:
                instance(scene,collections[p['underlay']],'Presentation-only synthetic floor underlay '+str(i),p['left'],height-p['top']-24,-.5)
            instance(scene,collections[p['frame']],str(i),p['left'],height-p['top']-24)
        label=emission_material(filename+' labels','B8BFC7')
        add_label(scene,'SHALE PARITY / ORIGINAL NATIVE 2D / SOURCE STUDY',width/2,height-8,label,3)
        if filename!='material-study.png':
            for mask in range(16):add_label(scene,'MASK '+str(mask),54+(mask%4)*108,height-(64+(mask//4)*39)-29,label,2.4)
            add_label(scene,'FLOOR TOPOLOGIES',110,height-49,label,2.5)
            add_label(scene,'JOINED INTERIORS',330,height-49,label,2.5)
            add_label(scene,'N1 E2 S4 W8 / DISCLOSED FLOOR ONLY / NOT GAMEPLAY',width/2,8,label,2.4)
        else:add_label(scene,'SYNTHETIC JOIN TEST / NOT THE GAME MAP',width/2,3,label,2.4)
        scene.render.filepath=str(OUT/filename);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/filename)
    rubble=rubble_cells()
    rubble_native=bpy.data.scenes.new('Shale Rubble exact native 16x16')
    rubble_collection=model(rubble_native,'Shale parity actual Rubble',rubble,RUBBLE_PALETTE)
    rubble_native.collection.children.unlink(rubble_collection)
    # Established helper builds top-aligned24-high coordinates. Offset by8 to
    # place the exact16x16 geometry at0..16; no raster crop or downsample occurs.
    instance(rubble_native,rubble_collection,'Native Rubble16',0,-8)
    camera_for(rubble_native,16,16);rubble_native.render.filepath=str(OUT/'shale-parity-rubble.png')
    bpy.ops.render.render(write_still=True,scene=rubble_native.name);canonicalize_timing(OUT/'shale-parity-rubble.png')
    contact=bpy.data.scenes.new('Rubble contact direct8x');camera_for(contact,96,40,8)
    plane(contact,'Quiet gray ground',PALETTE['floor'],0,0,96,40,-2)
    for left in (16,40,64):instance(contact,rubble_collection,'Actual Rubble at '+str(left),left,40-12-24)
    label=emission_material('Rubble label','B8BFC7')
    add_label(contact,'ACTUAL RUBBLE / ORIGINAL16x16 / SOURCE ONLY',48,35,label,2)
    add_label(contact,'NO NEW OWNER / NOT FLOOR DECORATION',48,4,label,2)
    contact.render.filepath=str(OUT/'rubble-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name);canonicalize_timing(OUT/'rubble-contact.png')
    save('source-cells.json',{'schema':2,'coordinates':'Top-left y downward; each non-null cell is one editable unit quad atz0. Null wall pixels belong only to exposed1–3pixel contour cuts.',
                             'palette':PALETTE,'frames':frames,'authorship':'Original named integer geometry, no raster tracing/downsampling/reference sampling.'})
    save('rubble-source-cells.json',{'schema':1,'dimensions':[16,16],'palette':RUBBLE_PALETTE,'cells':rubble,
        'authorship':'Original gray fractured plate geometry; no raster recoloring, new item, terrain or simulation owner.',
        'owner':'Rubble','scope':'Optional isolated study only; original Environment/rubble.png unchanged.'})
    save('manifest.json',{'schema':2,'atlas':'shale-parity.png','dimensions':[ATLAS_W,H],'frame_size':[W,H],'frame_count':COUNT,
        'floor_frames':list(range(FLOOR_COUNT)),'wall_frame_formula':'8 + exposureMask*4 + variant','exposure_bits':{'N':1,'E':2,'S':4,'W':8},
        'exposure_meaning':'Only actual disclosed valid floor neighbors; unknown never supplies an exposed face. Mask0 is joined/unknown continuation.',
        'wall_variants':WALL_VARIANTS,'sprite_pivot':[.5,.5],'pixels_per_unit':16,'mesh':'FullRect','alpha':'Floors/mask0 opaque; exposed walls have binary1–3pixel boundary cuts; separate Rubble binary alpha',
        'wall_contour':{'protected_core':[3,3,13,21],'cut_depth_pixels':[1,3],'adjacent_sides':'Union of cuts; no detached islands or interior holes.','underlay':'Presentation-only candidate floor pattern beneath actual wall body, requiring valid disclosed adjacent floor and safe current cell. Root owns runtime admission/fallback; no local floor owner is created.'},
        'rubble':{'file':'shale-parity-rubble.png','dimensions':[16,16],'cells':'rubble-source-cells.json','palette':RUBBLE_PALETTE,'owner':'Rubble','original_retained':'Assets/Resources/Sprites/Environment/rubble.png'},
        'palette':PALETTE,'blend':'shale-parity.blend','source_cells':'source-cells.json','generator':'Tools/Live16x24/build_shale_parity.py',
        'source_references':['ArtSource/Live16x24/shale-caves/scene-reference-01.png','Docs/Verification/Live16x24/shale-caves-after-01-depth6-entry.png'],
        'scope':'Isolated candidate only; original rich shaded2D wall layers and cool flat ground. No global grading, simulation or gameplay parity acceptance.',
        'protected_input_count':len(guard()['exact_sha256'])})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'shale-parity.blend'))

def inspect_blend():
    import bpy
    guard();frames=source_cells();faces=0;clusters=0
    for name,cells,palette in [('Shale parity frame '+str(i),cells,PALETTE) for i,cells in enumerate(frames)]+[('Shale parity actual Rubble',rubble_cells(),RUBBLE_PALETTE)]:
        col=bpy.data.collections.get(name);assert col is not None;recovered={};clusters+=len(col.objects)
        for obj in col.objects:
            assert obj.type=='MESH' and obj.location.length==0 and tuple(obj.scale)==(1.,1.,1.) and tuple(obj.rotation_euler)==(0.,0.,0.)
            color=obj['paletteHex'];assert color in palette.values();assert len(obj.data.materials)==1
            material=obj.data.materials[0];emissions=[n for n in material.node_tree.nodes if n.type=='EMISSION'];assert len(emissions)==1
            actual=emissions[0].inputs['Color'].default_value;assert emissions[0].inputs['Strength'].default_value==1
            def linear(v):
                s=v/255.;return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
            assert all(abs(actual[k]-linear(v))<.000001 for k,v in enumerate(bytes.fromhex(color))) and actual[3]==1
            part=re.sub(r'\.\d+$','',obj.name)
            for poly in obj.data.polygons:
                assert len(poly.vertices)==4 and poly.material_index==0
                vertices=[obj.data.vertices[j].co for j in poly.vertices]
                assert all(v.z==0 and v.x==int(v.x) and v.y==int(v.y) for v in vertices)
                left,right=min(int(v.x) for v in vertices),max(int(v.x) for v in vertices)
                bottom,top=min(int(v.y) for v in vertices),max(int(v.y) for v in vertices)
                assert right-left==top-bottom==1;key=(left,23-bottom);assert key not in recovered
                recovered[key]=(part,color);faces+=1
        assert recovered=={(x,y):(v[0],palette[v[1]]) for y,row in enumerate(cells) for x,v in enumerate(row) if v}
    native=bpy.data.scenes.get('Shale parity exact native '+str(ATLAS_W)+'x24');assert native.render.resolution_x==ATLAS_W and native.render.resolution_y==H
    instances=[o for o in native.objects if o.instance_type=='COLLECTION'];assert len(instances)==COUNT
    for i in range(COUNT):
        obj=next(o for o in instances if o.location.x==16*i)
        assert tuple(obj.location)==(float(i*16),0.,0.) and tuple(obj.scale)==(1.,1.,1.) and tuple(obj.rotation_euler)==(0.,0.,0.)
        assert obj.instance_collection.name=='Shale parity frame '+str(i)
    rn=bpy.data.scenes.get('Shale Rubble exact native 16x16');assert rn.render.resolution_x==rn.render.resolution_y==16
    ri=[o for o in rn.objects if o.instance_type=='COLLECTION'];assert len(ri)==1 and tuple(ri[0].location)==(0.,-8.,0.)
    assert ri[0].instance_collection.name=='Shale parity actual Rubble'
    result={'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'unit_faces':faces,'named_clusters':clusters,
            'exact_geometry_and_emission':True,'native_dimensions':[ATLAS_W,H],'rubble_dimensions':[16,16],'blend_sha256':sha(OUT/'shale-parity.blend')}
    save('blend-geometry-verification.json',result);print(json.dumps(result,indent=2))

def publish():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_east_kitchen import write_meta
    guard()
    for source,destination in ((OUT/'shale-parity.png',RESOURCE),(OUT/'shale-parity-rubble.png',RUBBLE_RESOURCE)):
        destination.write_bytes(source.read_bytes());write_meta(Path(str(destination)+'.meta'))
    print('Copied exact candidate PNGs; existing atlas metadata preserved:',RESOURCE,RUBBLE_RESOURCE)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('build','verify','verify-source','inspect-blend','publish'):p.add_argument('--'+name,action='store_true')
    a=p.parse_args(args);assert sum((a.build,a.verify,a.verify_source,a.inspect_blend,a.publish))==1
    if a.build:build()
    elif a.verify:verify()
    elif a.verify_source:verify(False)
    elif a.inspect_blend:inspect_blend()
    else:publish()
