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
W, H, COUNT = 16, 24, 36
PALETTE = {
    'floor':'3F444A', 'chip':'484D53', 'chip-light':'52575D', 'chip-shadow':'373C42',
    'crevice':'14181D', 'deep':'1D242C', 'mass':'272F3A',
    'slab':'313B46', 'facet':'3D4753', 'edge':'4A5560', 'cut':'545F6B',
}
CONTACT_W, CONTACT_H = 352, 232
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
    return pins
def rgba(value): return tuple(bytes.fromhex(PALETTE[value[1]]))+(255,)

def source_cells():
    floors=[]
    # Three shallow angular chips per cell, deliberately offset among variants.
    # Each11-pixel cluster is a flat facet plus narrow contact shade, not rubble.
    chip_shapes=[[(1,0,'chip'),(2,0,'chip-light'),(3,0,'chip'),(0,1,'chip'),(1,1,'chip'),(2,1,'chip'),(3,1,'chip'),(4,1,'chip'),(1,2,'chip-shadow'),(2,2,'chip-shadow'),(3,2,'chip-shadow')],
                 [(0,0,'chip'),(1,0,'chip-light'),(2,0,'chip'),(3,0,'chip'),(1,1,'chip'),(2,1,'chip'),(3,1,'chip'),(4,1,'chip'),(2,2,'chip-shadow'),(3,2,'chip-shadow'),(4,2,'chip-shadow')],
                 [(2,0,'chip-light'),(3,0,'chip'),(0,1,'chip'),(1,1,'chip'),(2,1,'chip'),(3,1,'chip'),(4,1,'chip'),(5,1,'chip'),(1,2,'chip-shadow'),(2,2,'chip-shadow'),(3,2,'chip-shadow')]]
    origins=[[(2,4),(9,11),(3,19)],[(9,3),(2,12),(8,19)],[(5,3),(1,10),(9,18)],[(1,5),(9,9),(4,18)]]
    for variant,places in enumerate(origins):
        cells=[[['flat-slate-ground','floor'] for _ in range(W)] for _ in range(H)]
        for c,(left,top) in enumerate(places):
            for dx,dy,color in chip_shapes[(c+variant)%3]:
                x,y=left+dx,top+dy;assert cells[y][x][1]=='floor' and 0<x<15 and 0<y<23
                cells[y][x]=['shallow-angular-chip-'+str(c),color]
        floors.append(cells)
    def wall(variant):
        cells=[[['continuous-stone-mass','mass'] for _ in range(W)] for _ in range(H)]
        def put(x,y,part,color):
            if 0<=x<W and 0<=y<H: cells[y][x]=[part,color]
        # Original unequal overlapping slab silhouettes. Rows are not a brick
        # grid: ledges differ in height, terminate unevenly and pass tile sides.
        ledges = ([(-2,0,8,2,'facet'),(8,1,9,2,'slab'),(2,3,6,2,'facet'),(10,5,8,1,'slab'),
                   (-3,6,9,2,'slab'),(5,8,9,3,'facet'),(-2,11,7,2,'slab'),(11,12,7,2,'slab'),
                   (3,14,8,2,'facet'),(-1,17,6,2,'facet'),(9,17,9,3,'slab'),(2,20,8,3,'slab'),(12,22,6,1,'facet')]
                  if variant==0 else
                  [(3,-1,9,2,'slab'),(-3,1,8,3,'facet'),(10,2,9,1,'facet'),(5,4,7,2,'slab'),
                   (-2,7,9,2,'slab'),(9,8,8,3,'facet'),(3,11,5,1,'facet'),(-4,13,10,3,'slab'),
                   (7,14,10,2,'slab'),(1,17,7,2,'facet'),(11,18,7,1,'facet'),(5,20,8,3,'slab'),(-2,22,6,2,'facet')])
        for index,(left,top,length,height,color) in enumerate(ledges):
            for yy in range(height):
                # Cut one pixel from alternating corners, not diagonal waves.
                l=left+(1 if yy==0 and index%3==0 else 0)
                r=left+length-(1 if yy==height-1 and index%3==1 else 0)
                for xx in range(l,r): put(xx,top+yy,'broad-layer-face',color)
            for xx in range(left+1,left+length-1):
                if (xx-left+index)%6 not in(0,4,5):
                    put(xx,top,'broken-top-ledge','edge' if index%3 else 'cut')
            # An interrupted undercut belongs to this ledge, not free gravel.
            for xx in range(left+1,left+length):
                put(xx,top+height,'deep-undercut','crevice' if (index+xx)%4 else 'deep')
            put(left+length-1,top+height-1,'recessed-layer-tip','deep')
        # Short irregular clefts only, no full-height vertical masonry joints.
        for x,y in ([(7,3),(8,3),(1,11),(12,16),(13,16),(7,22)] if variant==0 else
                    [(7,5),(8,5),(0,15),(6,20),(7,20),(14,18)]):
            put(x,y,'short-cleft','crevice')
        return cells
    frames=floors
    for mask in range(16):
        for variant in range(2):
            cells=wall(variant)
            # All exposed-face pixels remain in the actual wall owner's cell.
            # N1/E2/S4/W8 come from disclosed valid floor, never unknown space.
            if mask&1:
                for x in range(W):
                    cells[0][x]=['north-floor-boundary','edge' if x%7 not in(0,6) else 'facet']
                    cells[1][x]=['north-shallow-face','facet']
                    cells[2][x]=['north-recess','deep' if x%6!=0 else 'mass']
            if mask&4:
                for x in range(W):
                    cells[21][x]=['south-broken-lip','facet' if x%5 else 'slab']
                    cells[22][x]=['south-shaded-face','deep']
                    cells[23][x]=['south-floor-seam','crevice']
            if mask&8:
                for y in range(H):
                    cells[y][0]=['west-floor-seam','deep']
                    cells[y][1]=['west-cut-face','facet' if y%5 else 'edge']
            if mask&2:
                for y in range(H):
                    cells[y][14]=['east-shaded-face','deep']
                    cells[y][15]=['east-floor-seam','crevice']
            frames.append(cells)
    return frames

def contact_placements():
    p=[{'frame':i,'left':10+i*28,'top':18} for i in range(4)]
    p += [{'frame':4+i,'left':150+i*28,'top':18} for i in range(2)]
    for mask in range(16):
        for v in range(2): p.append({'frame':4+mask*2+v,'left':28+(mask%4)*82+v*16,'top':64+(mask//4)*39})
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
            if ch=='.': frame=value%4
            else:
                mask=0
                for dx,dy,bit in ((0,-1,1),(1,0,2),(0,1,4),(-1,0,8)):
                    xx,yy=x+dx,y+dy
                    if 0<=yy<len(STUDY) and 0<=xx<len(row) and STUDY[yy][xx]=='.':mask|=bit
                frame=4+mask*2+(value&1)
            p.append({'frame':frame,'left':8+x*16,'top':22+y*24})
    return p

def validate(frames,size,pixels):
    assert frames==source_cells(),'Authored source changed'
    assert size==(576,24) and len(pixels)==576*24*4,'Wrong exact canvas'
    assert len(PALETTE)==11 and len(set(PALETTE.values()))==11
    for index,cells in enumerate(frames):
        for y,row in enumerate(cells):
            for x,value in enumerate(row):
                offset=4*(y*576+index*16+x)
                assert tuple(pixels[offset:offset+4])==rgba(value),('Native mismatch',index,x,y)
        if index<4:
            assert 31<=sum(v[1]!='floor' for row in cells for v in row)<=46
            assert all(cells[y][x][1]=='floor' for y in range(24) for x in range(16) if x in(0,15) or y in(0,23))
    assert len({json.dumps(f) for f in frames})==36,'Duplicate cell'
    for mask in range(16):
        for v in range(2):
            f=frames[4+mask*2+v]
            for x,y,bit,part in ((6,0,1,'north-floor-boundary'),(15,10,2,'east-floor-seam'),(6,23,4,'south-floor-seam'),(0,10,8,'west-floor-seam')):
                assert (f[y][x][0]==part)==bool(mask&bit),('Exposure mismatch',mask,v,bit)
    return [{'frame':i,'colors':dict(Counter(v[1] for row in f for v in row))} for i,f in enumerate(frames)]

def check_meta(data):
    text=data.decode('utf-8')
    expected={'enableMipMap':'0','filterMode':'0','wrapU':'1','wrapV':'1','spritePixelsToUnits':'16','textureCompression':'0','nPOTScale':'0'}
    for field,value in expected.items():
        values=re.findall(r'^\s*'+field+r':\s*(\S+)\s*$',text,re.MULTILINE)
        assert values and all(v==value for v in values),(field,values)
    guid=re.search(r'^guid: ([0-9a-f]{32})\s*$',text,re.MULTILINE);assert guid is not None
    return expected

def verify():
    assert (OUT/'shale-parity.png').exists(),'Native parity atlas absent; original geometry render required'
    from PIL import Image
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text())
    assert source['palette']==PALETTE
    with Image.open(OUT/'shale-parity.png') as im:
        assert im.mode=='RGBA';pixels=im.tobytes();stats=validate(source['frames'],im.size,pixels)
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
                                assert im.getpixel(((p['left']+x)*scale+dx,(p['top']+y)*scale+dy))==rgba(value),(filename,p,x,y)
                                checked+=1
    rejected=[]
    def reject(name,data=pixels,size=(576,24),frames=source['frames']):
        try: validate(frames,size,data)
        except(AssertionError,IndexError,KeyError,ValueError):rejected.append(name);return
        raise AssertionError('Corruption accepted: '+name)
    reject('wrong canvas',size=(576,16))
    for name,offset,new in [('transparent hole',3,0),('partial alpha',7,127),('altered floor',0,255),('altered wall edge',4*(4*16),255)]:
        bad=bytearray(pixels);bad[offset]=new;reject(name,bytes(bad))
    bad=copy.deepcopy(source['frames']);bad[4]=copy.deepcopy(bad[6]);reject('permuted exposure mask',frames=bad)
    bad=copy.deepcopy(source['frames']);bad[5]=copy.deepcopy(bad[4]);reject('duplicate variant',frames=bad)
    bad=copy.deepcopy(source['frames']);bad[0][7][4][0]='new-invented-part';reject('part drift',frames=bad)
    meta=(ROOT/'Assets/Resources/Sprites/Live16x24/shale-caves.png.meta').read_bytes()
    check_meta(meta)
    try:check_meta(meta.replace(b'filterMode: 0',b'filterMode: 1'))
    except AssertionError:rejected.append('altered importer filtering')
    else:raise AssertionError('Importer corruption accepted')
    runtime=RESOURCE.exists()
    if runtime:
        assert sha(RESOURCE)==sha(OUT/'shale-parity.png')
        check_meta(Path(str(RESOURCE)+'.meta').read_bytes())
    result={'passed':True,'native_pixels_checked':13824,'direct_contact_pixels_checked':checked,
            'frames':36,'dimensions':[576,24],'palette':PALETTE,'opaque':True,'frame_stats':stats,
            'protected_inputs_unchanged':len(pins['exact_sha256']),'protected_dirty_inputs':pins['dirty_input_count'],
            'protected_rejected_candidate_inputs':len(json.loads((OUT/'revision-protected-inputs.json').read_text())['exact_sha256']) if (OUT/'revision-protected-inputs.json').exists() else 0,
            'counterchecks_rejected':rejected,'source_png_sha256':sha(OUT/'shale-parity.png'),
            'source_cells_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'shale-parity.blend'),
            'runtime_present':runtime,'gameplay_acceptance':False,
            'limits':'Exact source/geometry-render proof only. Neutral emission synthetic contact, not actual layout, lighting, fog, gameplay or parity acceptance.'}
    if runtime:result['runtime_sha256']=sha(RESOURCE)
    save('source-verification.json',result);print(json.dumps({k:v for k,v in result.items() if k!='frame_stats'},indent=2))

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
    native=bpy.context.scene;native.name='Shale parity exact native 576x24';collections=[]
    for i,cells in enumerate(frames):
        col=model(native,'Shale parity frame '+str(i),cells,PALETTE);native.collection.children.unlink(col);collections.append(col)
        instance(native,col,'Native frame '+str(i),i*16,0)
    camera_for(native,576,24);native.render.filepath=str(OUT/'shale-parity.png');bpy.ops.render.render(write_still=True,scene=native.name)
    canonicalize_timing(OUT/'shale-parity.png')
    for filename,width,height,scale,places in (
        ('contact-native.png',CONTACT_W,CONTACT_H,1,contact_placements()),
        ('contact.png',CONTACT_W,CONTACT_H,4,contact_placements()),
        ('material-study.png',STUDY_W,STUDY_H,4,study_placements())):
        scene=bpy.data.scenes.new(filename);camera_for(scene,width,height,scale)
        plane(scene,'Quiet source backing','10141A',0,0,width,height,-2)
        for i,p in enumerate(places):instance(scene,collections[p['frame']],str(i),p['left'],height-p['top']-24)
        label=emission_material(filename+' labels','B8BFC7')
        add_label(scene,'SHALE PARITY / ORIGINAL NATIVE 2D / SOURCE STUDY',width/2,height-8,label,3)
        if filename!='material-study.png':
            for mask in range(16):add_label(scene,'MASK '+str(mask),44+(mask%4)*82,height-(64+(mask//4)*39)-29,label,2.4)
            add_label(scene,'FLOORS                    JOINED INTERIORS',100,height-49,label,2.5)
            add_label(scene,'N1 E2 S4 W8 / DISCLOSED FLOOR ONLY / NOT GAMEPLAY',width/2,8,label,2.4)
        else:add_label(scene,'SYNTHETIC JOIN TEST / NOT THE GAME MAP',width/2,3,label,2.4)
        scene.render.filepath=str(OUT/filename);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/filename)
    save('source-cells.json',{'schema':1,'coordinates':'Top-left y downward; each cell is one editable unit quad atz0.',
                             'palette':PALETTE,'frames':frames,'authorship':'Original named integer geometry, no raster tracing/downsampling/reference sampling.'})
    save('manifest.json',{'schema':1,'atlas':'shale-parity.png','dimensions':[576,24],'frame_size':[16,24],'frame_count':36,
        'floor_frames':[0,1,2,3],'wall_frame_formula':'4 + exposureMask*2 + variant','exposure_bits':{'N':1,'E':2,'S':4,'W':8},
        'exposure_meaning':'Only actual disclosed valid floor neighbors; unknown never supplies an exposed face. Mask0 is joined/unknown continuation.',
        'wall_variants':2,'sprite_pivot':[.5,.5],'pixels_per_unit':16,'mesh':'FullRect','alpha':'Fully opaque',
        'palette':PALETTE,'blend':'shale-parity.blend','source_cells':'source-cells.json','generator':'Tools/Live16x24/build_shale_parity.py',
        'source_references':['ArtSource/Live16x24/shale-caves/scene-reference-01.png','Docs/Verification/Live16x24/shale-caves-after-01-depth6-entry.png'],
        'scope':'Isolated candidate only; original rich shaded2D wall layers and cool flat ground. No global grading, simulation or gameplay parity acceptance.',
        'protected_input_count':len(guard()['exact_sha256'])})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'shale-parity.blend'))

def inspect_blend():
    import bpy
    guard();frames=source_cells();faces=0;clusters=0
    for i,cells in enumerate(frames):
        col=bpy.data.collections.get('Shale parity frame '+str(i));assert col is not None;recovered={};clusters+=len(col.objects)
        for obj in col.objects:
            assert obj.type=='MESH' and obj.location.length==0 and tuple(obj.scale)==(1.,1.,1.) and tuple(obj.rotation_euler)==(0.,0.,0.)
            color=obj['paletteHex'];assert color in PALETTE.values();assert len(obj.data.materials)==1
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
        assert recovered=={(x,y):(v[0],PALETTE[v[1]]) for y,row in enumerate(cells) for x,v in enumerate(row)}
    native=bpy.data.scenes.get('Shale parity exact native 576x24');assert native.render.resolution_x==576 and native.render.resolution_y==24
    instances=[o for o in native.objects if o.instance_type=='COLLECTION'];assert len(instances)==36
    for i in range(36):
        obj=next(o for o in instances if o.location.x==16*i)
        assert tuple(obj.location)==(float(i*16),0.,0.) and tuple(obj.scale)==(1.,1.,1.) and tuple(obj.rotation_euler)==(0.,0.,0.)
        assert obj.instance_collection.name=='Shale parity frame '+str(i)
    result={'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'unit_faces':faces,'named_clusters':clusters,
            'exact_geometry_and_emission':True,'native_dimensions':[576,24],'blend_sha256':sha(OUT/'shale-parity.blend')}
    save('blend-geometry-verification.json',result);print(json.dumps(result,indent=2))

def publish():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_east_kitchen import write_meta
    guard();RESOURCE.write_bytes((OUT/'shale-parity.png').read_bytes());write_meta(Path(str(RESOURCE)+'.meta'))
    print('Copied exact candidate PNG; new sibling metadata:',RESOURCE)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('build','verify','inspect-blend','publish'):p.add_argument('--'+name,action='store_true')
    a=p.parse_args(args);assert sum((a.build,a.verify,a.inspect_blend,a.publish))==1
    if a.build:build()
    elif a.verify:verify()
    elif a.inspect_blend:inspect_blend()
    else:publish()
