"""Original six-cell obsidian floor/wall source, editable native integer geometry.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify
Blender --background obsidian-caves.blend --python this-file -- --inspect-blend
References are never traced/read as pixels; Pillow is decode-only verification.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import re
import struct
import sys
import zlib
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/obsidian-caves"
W,H,COUNT = 16,24,6
PALETTE = {"floor":"383242", "fleck":"403A4C", "dust":"2E2A38",
           "mass":"504A61", "shadow":"2C2B3C", "seam":"403C52",
           "plane":"666178", "highlight":"9693AB"}
CONTACT_W,CONTACT_H = 192,140
OWNED = {"source-cells.json","manifest.json","protected-inputs.json",
         "source-verification.json","blend-geometry-verification.json"}
HELPERS = ("build_opening_base.py","build_cindercaller.py","build_north_plot.py",
           "build_east_kitchen.py","build_shallow_caves.py","build_limestone_caves.py",
           "build_slate_caves.py","build_quartzite_caves.py")
BORROWED = ("Live16x24/shallow-caves","Live16x24/beating-materials",
            "Live16x24/opening-cast","Live16x24/opening-anchors","Live16x24/opening-cellar",
            "Environment/rubble","Environment/stalagmite","Environment/obsidian_m00","Environment/obsidian_m15",
            "Live16x24/slate-caves","Live16x24/quartzite-caves")
EDITABLES = ("shallow-caves/shallow-caves.blend","shallow-caves/source-cells.json",
             "beating-materials/beating-materials.blend","beating-materials/source-cells.json",
             "cast/opening-cast.blend","cast/source-cells.json",
             "slate-caves/slate-caves.blend","slate-caves/source-cells.json",
             "quartzite-caves/quartzite-caves.blend","quartzite-caves/source-cells.json")
REFERENCES = ("scene-reference-01.png","scene-prompt-01.txt","PARTS-AND-CONSTRAINTS.md")
def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    return sha_bytes(path.read_bytes())


def save(name, value):
    assert name in OWNED, "Not an owned generated JSON"
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


def source_cells():
    frames=[]
    for index in range(COUNT):
        base=('quiet-worn-obsidian-floor','floor') if index<4 else ('continuous-volcanic-glass-mass','mass')
        frames.append([[list(base) for _ in range(W)] for _ in range(H)])
    def marks(frame,part,color,positions,replace=False):
        for x,y in positions:
            assert 0<x<W-1 and 0<y<H-1
            assert replace or frames[frame][y][x][1] in ('floor','mass'), ('Overlapping authored cluster',frame,x,y)
            frames[frame][y][x]=[part,color]
    def runs(frame,part,color,rows):
        marks(frame,part,color,[(x,y) for y,left,right in rows for x in range(left,right+1)])
    # Original quiet floor: four, six, seven and eight small worn-grain pixels.
    marks(0,'nearby-pale-floor-grains','fleck',[(4,7),(5,7)])
    marks(0,'lower-worn-patch','dust',[(11,18),(12,18)])
    marks(1,'upper-pale-floor-grains','fleck',[(10,5),(11,5)])
    marks(1,'lower-worn-patch','dust',[(3,16),(4,16),(5,17),(6,17)])
    marks(2,'middle-pale-floor-grains','fleck',[(7,10),(8,10),(8,11)])
    marks(2,'two-worn-patches','dust',[(3,4),(4,4),(11,19),(12,19)])
    marks(3,'upper-pale-floor-grains','fleck',[(10,4),(11,4),(12,5)])
    marks(3,'lower-worn-patch','dust',[(3,14),(4,14),(5,15),(5,16),(6,16)])
    # Three unequal broad internal fracture faces per wall. One elongated
    # oblique lit plane, a deep adjoining face and a restrained worn face make
    # glassy relief; not four-corner grains, paired slabs or outside bevels.
    runs(4,'oblique-upper-fracture-face','plane',[(1,6,14),(2,5,14),(3,5,13),(4,4,12),(5,4,10),(6,3,9),(7,3,7),(8,2,6),(9,2,4),(10,1,3)])
    runs(4,'adjoining-right-fracture-face','shadow',[(7,11,14),(8,10,14),(9,9,14),(10,8,14),(11,7,14),(12,9,14),(13,10,14),(14,11,14),(15,12,14),(16,13,14)])
    runs(4,'lower-worn-fracture-face','seam',[(13,1,5),(14,1,7),(15,1,9),(16,1,10),(17,1,9),(18,1,7),(19,1,6),(20,1,4),(21,1,3),(22,1,2)])
    marks(4,'thin-broken-glass-edge','highlight',[(6,2),(5,3),(4,4),(4,7),(3,8),(2,9)],True)
    runs(5,'oblique-middle-fracture-face','plane',[(3,1,4),(4,1,6),(5,1,8),(6,2,10),(7,3,11),(8,4,12),(9,5,13),(10,7,14),(11,9,14),(12,10,14),(13,12,14)])
    runs(5,'adjoining-left-fracture-face','shadow',[(10,1,3),(11,1,5),(12,1,7),(13,1,9),(14,1,10),(15,1,9),(16,1,7),(17,1,6),(18,1,4),(19,1,3),(20,1,2)])
    runs(5,'lower-worn-fracture-face','seam',[(16,11,14),(17,10,14),(18,9,14),(19,8,14),(20,7,14),(21,7,14),(22,8,14)])
    marks(5,'thin-broken-glass-edge','highlight',[(3,4),(4,5),(5,6),(11,10),(12,11),(13,12)],True)
    return frames

def normalized_meta(data):
    """Only trailing horizontal whitespace, preserving line endings/structure."""
    return re.sub(rb'[ \t]+(?=\r?$)', b'', data, flags=re.MULTILINE)


def check_meta(data):
    text = data.decode('utf-8')
    expected = {'enableMipMap': '0', 'filterMode': '0', 'wrapU': '1', 'wrapV': '1',
                'spritePixelsToUnits': '16', 'textureCompression': '0'}
    for field, value in expected.items():
        values = re.findall(r'^\s*' + field + r':\s*(\S+)\s*$', text, re.MULTILINE)
        assert values and all(v == value for v in values), (field, values)
    assert re.search(r'^guid: [0-9a-f]{32}\s*$', text, re.MULTILINE)
    return expected


def guard():
    path=OUT/'protected-inputs.json'
    if not path.exists():
        exact=[ROOT/'Tools/Live16x24'/n for n in HELPERS]
        exact += [ROOT/'Assets/Resources/Sprites'/(n+'.png') for n in BORROWED]
        exact += [ROOT/'ArtSource/Live16x24'/n for n in EDITABLES]
        exact += [OUT/n for n in REFERENCES]
        exact += [ROOT/'Docs/Verification/Live16x24'/n for n in
                  ('obsidian-caves-before-01-entry11.png','obsidian-caves-before-05-entry12.png')]
        metadata=[ROOT/'Assets/Resources/Sprites'/(n+'.png.meta') for n in BORROWED]
        pins={'exact_sha256':{str(p.relative_to(ROOT)):sha(p) for p in exact},
              'metadata':{str(p.relative_to(ROOT)):{'sha256_trailing_whitespace_normalized':sha_bytes(normalized_meta(p.read_bytes())),
                   'initial_raw_sha256':sha(p),'import_fields':check_meta(p.read_bytes())} for p in metadata},
              'normalization':'Only trailing spaces/tabs before existing metadata line endings; no GUID,structure,import value or raw source writes.',
              'scope':'Finite eleven approved resource PNG/meta pairs, ten editables, eight helper sources, three concept/critique files, two actual baseline captures. No mutable living records.'}
        save('protected-inputs.json',pins)
    pins=json.loads(path.read_text())
    for name,expected in pins['exact_sha256'].items():
        assert sha(ROOT/name)==expected,'Protected exact input changed: '+name
    for name,record in pins['metadata'].items():
        data=(ROOT/name).read_bytes()
        assert sha_bytes(normalized_meta(data))==record['sha256_trailing_whitespace_normalized'],'Protected metadata changed: '+name
        assert check_meta(data)==record['import_fields']
    return pins


def expected_pixel(value):
    return tuple(bytes.fromhex(PALETTE[value[1]]))+(255,)


def validate(frames,size,rgba):
    assert size==(W*COUNT,H),'Wrong native dimensions'
    assert frames==source_cells(),'Authored part/color source drift'
    assert len(rgba)==W*COUNT*H*4 and len(PALETTE)==8
    stats=[]
    for index,cells in enumerate(frames):
        base=('quiet-worn-obsidian-floor','floor') if index<4 else ('continuous-volcanic-glass-mass','mass')
        base_count=0
        for y,row in enumerate(cells):
            for x,value in enumerate(row):
                offset=4*(y*W*COUNT+index*W+x)
                assert tuple(rgba[offset:offset+4])==expected_pixel(value),('Pixel mismatch',index,x,y)
                if x in(0,W-1) or y in(0,H-1):assert value==list(base),'Nonuniform base edge'
                base_count += value[1]==base[1]
        threshold=.95 if index<4 else .40
        assert base_count>=math.ceil(W*H*threshold),'Insufficient quiet base'
        stats.append({'frame':index,'owner':'ObsidianFloor' if index<4 else 'ObsidianWall',
                      'base_pixels':base_count,'detail_pixels':W*H-base_count,'base_fraction':base_count/(W*H),'minimum_base_fraction':threshold})
    assert len({json.dumps(f) for f in frames})==COUNT,'Duplicate variants'
    assert all(a<b for a,b in zip(bytes.fromhex(PALETTE['floor']),bytes.fromhex(PALETTE['mass'])))
    assert all(4 <= s['detail_pixels'] <= 8 for s in stats[:4]), 'Floor needs only four to eight quiet accents'
    assert all(0.35 <= 1-s['base_fraction'] <= 0.60 for s in stats[4:]), 'Compact internal faces must leave continuous material'
    assert all(sum(v[1]=='highlight' for row in f for v in row)==6 for f in frames[4:]), 'Six restrained pale edge pixels per wall'
    assert all(len({v[0] for row in f for v in row if 'fracture-face' in v[0]})==3 for f in frames[4:]), 'Three original broad fracture faces per wall'
    return stats


def placements():
    result=[{'frame':i,'left':8+i*28,'top':16} for i in range(COUNT)]
    for y in range(3):
        for x in range(8):
            wall=x<3 or x==3 and y!=1
            frame=4+(x+y)%2 if wall else (x*3+y)%4
            result.append({'frame':frame,'left':8+x*16,'top':60+y*24})
    return result


def verify():
    assert (OUT/'obsidian-caves.png').exists(),'Native obsidian atlas missing: render required before GREEN'
    from PIL import Image # Read/decode only, never image editing/resampling.
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text())
    assert source['palette']==PALETTE
    with Image.open(OUT/'obsidian-caves.png') as im:
        assert im.mode=='RGBA';rgba=im.tobytes();stats=validate(source['frames'],im.size,rgba)
    checked=0
    for filename,scale in (('contact-native.png',1),('contact.png',8)):
        with Image.open(OUT/filename) as im:
            assert im.mode=='RGBA' and im.size==(CONTACT_W*scale,CONTACT_H*scale)
            for p in placements():
                for y,row in enumerate(source['frames'][p['frame']]):
                    for x,value in enumerate(row):
                        for dy in range(scale):
                            for dx in range(scale):
                                assert im.getpixel(((p['left']+x)*scale+dx,(p['top']+y)*scale+dy))==expected_pixel(value),('Contact mismatch',filename,p,x,y)
                                checked+=1
    counters=[]
    def reject(name,data=rgba,size=(96,24),frames=source['frames']):
        try:validate(frames,size,data)
        except(AssertionError,IndexError,KeyError,ValueError):counters.append(name);return
        raise AssertionError('Corruption accepted: '+name)
    reject('incorrect canvas',size=(96,16))
    for name,offset,value in (('transparent hole',3,0),('partial alpha',7,127),('altered floor base edge',0,255),
                             ('altered wall base edge',4*64,255),('altered authored floor grain',4*(7*96+4),255)):
        bad=bytearray(rgba);bad[offset]=value;reject(name,bytes(bad))
    swapped=bytearray(rgba)
    for y in range(H):
        a=4*y*96;b=a+4*16
        swapped[a:a+64],swapped[b:b+64]=rgba[b:b+64],rgba[a:a+64]
    reject('swapped floor variant pixels',bytes(swapped))
    duplicate=copy.deepcopy(source['frames']);duplicate[1]=copy.deepcopy(duplicate[0]);reject('duplicate source',frames=duplicate)
    wrong=copy.deepcopy(source['frames']);wrong[0][7][4][0]='invented-pebble';reject('named part drift',frames=wrong)
    meta=(ROOT/'Assets/Resources/Sprites/Live16x24/shallow-caves.png.meta').read_bytes()
    assert normalized_meta(meta)==normalized_meta(re.sub(rb'(?m)^guid: ([0-9a-f]+)',rb'guid: \1  ',meta))
    try:check_meta(meta.replace(b'filterMode: 0',b'filterMode: 1'))
    except AssertionError:counters.append('changed importer rejected despite whitespace tolerance')
    else:raise AssertionError('Importer counter failed')
    assert (OUT/'obsidian-caves.blend').stat().st_size>10000
    result={'passed':True,'dimensions':[96,24],'frames':6,'palette':PALETTE,
            'exact_native_pixels_checked':2304,'direct_contact_pixels_checked':checked,'fully_opaque':True,
            'identical_family_base_edges':True,'frame_stats':stats,'atlas_sha256':sha(OUT/'obsidian-caves.png'),
            'decoded_rgba_sha256':sha_bytes(rgba),'source_cells_sha256':sha(OUT/'source-cells.json'),
            'blend_sha256':sha(OUT/'obsidian-caves.blend'),'protected_exact_inputs':len(pins['exact_sha256']),
            'protected_normalized_metas':len(pins['metadata']),'counterchecks_rejected':counters,
            'metadata_whitespace_counter_accepted':True,'gameplay_acceptance':False,
            'limits':'Original geometry/render validation only; contacts are ungraded static source studies. Runtime,actual Cave grading,readers,states,travel,fullness and packaged gates are separate.'}
    runtime=ROOT/'Assets/Resources/Sprites/Live16x24/obsidian-caves.png'
    result['runtime_png_present']=runtime.exists()
    if runtime.exists():assert sha(runtime)==sha(OUT/'obsidian-caves.png');result['runtime_png_sha256']=sha(runtime)
    save('source-verification.json',result);print(json.dumps(result,indent=2))

def canonicalize_timing(path):
    """Remove ONLY volatile cycles.*_time text chunks; retain all image bytes."""
    data = path.read_bytes(); assert data[:8] == b'\x89PNG\r\n\x1a\n'
    result = bytearray(data[:8]); position = 8; removed = []
    while position < len(data):
        length = struct.unpack('>I',data[position:position+4])[0]; end = position+12+length
        assert end <= len(data)
        kind = data[position+4:position+8]; payload = data[position+8:position+8+length]
        assert zlib.crc32(kind+payload)&0xffffffff == struct.unpack('>I', data[end-4:end])[0]
        key = payload.split(b'\0',1)[0] if kind == b'tEXt' else b''
        if key.startswith(b'cycles.') and key.endswith(b'_time'): removed.append(key.decode())
        else: result.extend(data[position:end])
        position = end
    assert position == len(data)
    # No image chunk is decoded, recompressed or edited by this operation.
    path.write_bytes(result)
    return {'raw_sha256': sha_bytes(data), 'canonical_sha256': sha(path), 'removed_text_keys': removed,
            'image_chunk_bytes_retained': True}


def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for,emission_material,add_label
    from build_cindercaller import model
    from build_north_plot import instance,plane
    OUT.mkdir(parents=True,exist_ok=True);pins=guard();frames=source_cells()
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Obsidian caves exact native 96x24'
    collections=[]
    for index,cells in enumerate(frames):
        col=model(native,'Obsidian frame '+str(index)+' original integer cells',cells,PALETTE)
        native.collection.children.unlink(col);collections.append(col)
        instance(native,col,'Native frame '+str(index),index*W,0)
    camera_for(native,96,24);native.render.filepath=str(OUT/'obsidian-caves.png')
    bpy.ops.render.render(write_still=True,scene=native.name)
    timing={'obsidian-caves.png':canonicalize_timing(OUT/'obsidian-caves.png')}
    for scale,filename in((1,'contact-native.png'),(8,'contact.png')):
        contact=bpy.data.scenes.new('Obsidian direct geometry study '+str(scale)+'x')
        camera_for(contact,CONTACT_W,CONTACT_H,scale)
        plane(contact,'Quiet study backing','171B23',0,0,CONTACT_W,CONTACT_H,-2)
        for i,p in enumerate(placements()):instance(contact,collections[p['frame']],str(i),p['left'],CONTACT_H-p['top']-24)
        label=emission_material('Study label '+str(scale),'D0C9AD')
        add_label(contact,'OBSIDIAN / SIX ORIGINAL NATIVE 16 x 24 CELLS',96,133,label,2.2)
        for i in range(COUNT):add_label(contact,('FLOOR' if i<4 else 'WALL')+str(i),16+i*28,94,label,2)
        add_label(contact,'VOLCANIC GLASS + QUIET FLOOR',72,84,label,2)
        add_label(contact,'UNGRADED',163,60,label,2)
        add_label(contact,'SOURCE STUDY',163,56,label,1.8)
        add_label(contact,'NO EDGE MASK',163,42,label,1.8)
        add_label(contact,'NO GAMEPLAY',163,38,label,1.8)
        contact.render.filepath=str(OUT/filename);bpy.ops.render.render(write_still=True,scene=contact.name)
        timing[filename]=canonicalize_timing(OUT/filename)
    save('source-cells.json',{'schema':1,'coordinate_system':'Top-left y downward; each named cell is an original editable integer quad atz0.',
                            'palette':PALETTE,'frames':frames,'authoring':'Original named geometry; no reference image read,sampling,tracing or transform.'})
    save('manifest.json',{'schema':1,'frames':[{'index':i,'rect':[i*16,0,16,24],
              'owner':'ObsidianFloor' if i<4 else 'ObsidianWall',
              'state':'Neutral passable real floor' if i<4 else 'Surviving structural HP1..70/max70/hardness6; actual destruction removes owner and places existing Rubble'} for i in range(COUNT)],
         'atlas':'obsidian-caves.png','width':96,'height':24,'palette':PALETTE,'sprite_pivot':[.5,.5],'pixels_per_unit':16,
         'expected_import':'Point,Clamp,no mipmaps,uncompressed,FullRect.','opaque':True,'edge_masks':False,
         'blend':'obsidian-caves.blend','source_cells':'source-cells.json','shade_style':'Original flat internal color planes; no geometry or alpha silhouette change.','contacts':['contact-native.png','contact.png'],
         'contact_placements':placements(),'contact_size_native':[CONTACT_W,CONTACT_H],
         'ambient_base_rgb':[56,50,66],'ambient_limit':'Optional owner-free backing belongs to separately guarded runtime; not a floor owner or reader.',
         'references':list(REFERENCES),'named_revision':'Initial original Obsidian candidate after root scene-reference direction approval', 'extraction':'Accept dark volcanic-glass mass, broad angular internal fracture faces, sparse pale edges and quiet floor. Reject pyramid/star bevels, noise carpeting, invented boundary walls, changed fog/camera/UI/scale, relighting, extrusion, image sampling and nonuniform pixels.',
         'protected_input_counts':[len(pins['exact_sha256']),len(pins['metadata'])],
         'png_nonpixel_timing_normalization':timing,'runtime_acceptance':False,'area_polish_acceptance':False})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'obsidian-caves.blend'))
    guard();print('Built original six obsidian cells and direct contacts; Assets untouched by builder.')

def inspect_blend():
    import bpy
    source = json.loads((OUT/'source-cells.json').read_text()); faces = clusters = 0
    for index, cells in enumerate(source['frames']):
        col = bpy.data.collections.get('Obsidian frame '+str(index)+' original integer cells'); assert col is not None
        recovered = {}; clusters += len(col.objects)
        for obj in col.objects:
            assert obj.type == 'MESH' and obj.location.length == 0 and tuple(obj.scale) == (1.,1.,1.) and tuple(obj.rotation_euler) == (0.,0.,0.)
            color = obj['paletteHex']; assert color in PALETTE.values()
            assert len(obj.data.materials) == 1
            material = obj.data.materials[0]; assert material.use_nodes
            emissions = [n for n in material.node_tree.nodes if n.type == 'EMISSION']
            assert len(emissions) == 1 and emissions[0].inputs['Strength'].default_value == 1
            actual_color = emissions[0].inputs['Color'].default_value
            def linear(value):
                s = value/255.0
                return s/12.92 if s <= .04045 else ((s+.055)/1.055)**2.4
            assert all(abs(actual_color[i]-linear(v)) < .000001 for i,v in enumerate(bytes.fromhex(color))) and actual_color[3] == 1
            part = re.sub(r'\.\d+$','',obj.name)
            for polygon in obj.data.polygons:
                assert len(polygon.vertices) == 4 and polygon.material_index == 0
                vertices = [obj.data.vertices[i].co for i in polygon.vertices]
                assert all(v.z == 0 and v.x == int(v.x) and v.y == int(v.y) for v in vertices)
                left,right = min(int(v.x) for v in vertices), max(int(v.x) for v in vertices)
                bottom,top = min(int(v.y) for v in vertices), max(int(v.y) for v in vertices)
                assert right-left == top-bottom == 1
                key = (left,23-bottom); assert key not in recovered
                recovered[key] = (part,color); faces += 1
        assert recovered == {(x,y):(v[0],PALETTE[v[1]]) for y,row in enumerate(cells) for x,v in enumerate(row)}
    native = bpy.data.scenes.get('Obsidian caves exact native 96x24')
    assert native.render.resolution_x == 96 and native.render.resolution_y == 24 and native.render.resolution_percentage == 100
    instances = [o for o in native.objects if o.instance_type == 'COLLECTION']
    assert len(instances) == COUNT and sorted(tuple(o.location) for o in instances) == [(float(i*W),0.,0.) for i in range(COUNT)]
    for index in range(COUNT):
        obj = next(o for o in instances if o.location.x == 16*index)
        assert obj.instance_collection.name == 'Obsidian frame '+str(index)+' original integer cells'
        assert tuple(obj.rotation_euler) == (0.,0.,0.) and tuple(obj.scale) == (1.,1.,1.)
    result = {'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'original_integer_unit_faces':faces,
              'named_editable_mesh_clusters':clusters,'every_part_and_color_matches_source':True,'actual_emission_material_colors_verified':True,
              'native_dimensions':[96,24],'native_instances':[list(o.location) for o in instances],
              'blend_sha256':sha(OUT/'obsidian-caves.blend'),'limits':'Saved editable geometry inspection, not runtime or gameplay.'}
    save('blend-geometry-verification.json',result); print(json.dumps(result,indent=2))


if __name__=='__main__':
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for flag in('build','verify','inspect-blend'):parser.add_argument('--'+flag,action='store_true')
    args=parser.parse_args(argv);assert sum((args.build,args.verify,args.inspect_blend))==1
    if args.build:build()
    elif args.verify:verify()
    else:inspect_blend()
