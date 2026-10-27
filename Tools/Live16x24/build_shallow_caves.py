"""Original two-cell shallow sandstone wall; editable native Blender geometry.

Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify
Blender --background shallow-caves.blend --python this-file -- --inspect-blend

Writes only ArtSource/Live16x24/shallow-caves production outputs. Raster inputs
are references, never traced/sampled/resized. Pillow is decode-only validation.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import zlib

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/shallow-caves"
W, H, COUNT = 16, 24, 2
PALETTE = {"mass": "5A4B37", "fracture": "4C3F2F",
           "seam": "544634", "worn": "64543E"}
FLOOR = "806A4C"  # Existing Beating floor base RGB128,106,76; study only.
CONTACT_W, CONTACT_H = 128, 120
OWNED = {"source-cells.json", "manifest.json", "protected-inputs.json",
         "source-verification.json", "blend-geometry-verification.json"}
HELPERS = ("build_opening_base.py", "build_cindercaller.py",
           "build_north_plot.py", "build_east_kitchen.py")
BORROWED = ("Live16x24/beating-materials", "Live16x24/opening-anchors",
            "Live16x24/opening-cellar", "Environment/rubble", "Environment/stalagmite")
EDITABLES = ("beating-materials/beating-materials.blend", "beating-materials/source-cells.json",
             "anchors/opening-anchors.blend", "anchors/source-cells.json",
             "cellar/opening-cellar.blend", "cellar/source-cells.json")
REFERENCES = ("scene-reference-01.png", "scene-prompt.txt",
              "parts-reference-01.png", "parts-prompt.txt", "REFERENCE-CRITIQUE.md")


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    return sha_bytes(path.read_bytes())


def save(name, value):
    assert name in OWNED, "Not an owned generated JSON"
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


def source_cells():
    frames = [[[['continuous-rock-mass', 'mass'] for _ in range(W)] for _ in range(H)] for _ in range(COUNT)]
    def marks(frame, part, color, positions):
        for x, y in positions:
            assert 0 < x < W-1 and 0 < y < H-1
            frames[frame][y][x] = [part, color]
    # Sparse original internal fissures, not tile edges, masonry or protrusions.
    marks(0, 'upper-bent-fracture', 'fracture', [(4,5),(5,6),(5,7),(6,8),(6,9),(7,10)])
    marks(0, 'upper-seam', 'seam', [(4,4),(6,6),(7,9)])
    marks(0, 'short-lower-fracture', 'fracture', [(10,17),(11,17),(12,16)])
    marks(0, 'lower-seam', 'seam', [(9,18),(12,15)])
    marks(0, 'worn-interior-flecks', 'worn', [(7,7),(11,15),(3,19)])
    marks(1, 'middle-bent-fracture', 'fracture', [(5,12),(6,12),(7,13),(7,14),(8,15),(8,16)])
    marks(1, 'short-upper-fracture', 'fracture', [(11,5),(11,6),(10,7)])
    marks(1, 'interior-seams', 'seam', [(4,11),(6,13),(9,16),(10,8)])
    marks(1, 'worn-interior-flecks', 'worn', [(6,11),(9,7),(10,18)])
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
    path = OUT / 'protected-inputs.json'
    if not path.exists():
        exact = [ROOT / 'Tools/Live16x24' / n for n in HELPERS]
        exact += [ROOT / 'Assets/Resources/Sprites' / (n + '.png') for n in BORROWED]
        exact += [ROOT / 'ArtSource/Live16x24' / n for n in EDITABLES]
        exact += [OUT / n for n in REFERENCES]
        metadata = [ROOT / 'Assets/Resources/Sprites' / (n + '.png.meta') for n in BORROWED]
        pins = {'exact_sha256': {str(p.relative_to(ROOT)): sha(p) for p in exact},
                'metadata': {str(p.relative_to(ROOT)): {
                    'sha256_trailing_whitespace_normalized': sha_bytes(normalized_meta(p.read_bytes())),
                    'initial_raw_sha256': sha(p), 'import_fields': check_meta(p.read_bytes())} for p in metadata},
                'normalization': 'Metadata only: strip trailing spaces/tabs before each existing line ending. Preserve every other byte, field, GUID, structure and value. Raw files are never modified. Initial raw hashes are historical provenance, not clean-checkout equality requirements.',
                'scope': 'Finite borrowed PNGs/metas/editables, four actual helper dependencies, and five root-owned reference inputs. No living coverage records or unrelated mutable source JSON.'}
        save('protected-inputs.json', pins)
    pins = json.loads(path.read_text())
    for name, expected in pins['exact_sha256'].items():
        assert sha(ROOT / name) == expected, 'Protected byte-exact input changed: ' + name
    for name, record in pins['metadata'].items():
        data = (ROOT / name).read_bytes()
        assert sha_bytes(normalized_meta(data)) == record['sha256_trailing_whitespace_normalized'], 'Protected metadata changed: ' + name
        assert check_meta(data) == record['import_fields']
    source = json.loads((ROOT / 'ArtSource/Live16x24/beating-materials/source-cells.json').read_text())
    assert source['palette']['floor'] == FLOOR
    return pins


def expected_pixel(value):
    return tuple(bytes.fromhex(PALETTE[value[1]])) + (255,)


def validate(frames, size, rgba):
    assert size == (W*COUNT, H), 'Wrong native dimensions'
    assert frames == source_cells(), 'Authored part/color source drift'
    assert len(rgba) == W*COUNT*H*4
    assert len(PALETTE) <= 5
    stats = []
    for index, cells in enumerate(frames):
        base_count = 0
        for y, row in enumerate(cells):
            for x, value in enumerate(row):
                offset = 4*(y*W*COUNT + index*W + x)
                actual = tuple(rgba[offset:offset+4])
                assert actual == expected_pixel(value), ('Pixel mismatch', index, x, y)
                if x in (0, W-1) or y in (0, H-1):
                    assert value == ['continuous-rock-mass', 'mass'], 'Nonuniform edge'
                base_count += value[1] == 'mass'
        assert base_count >= int(W*H*.94), 'Insufficient quiet base'
        stats.append({'frame': index, 'base_pixels': base_count, 'detail_pixels': W*H-base_count})
    assert frames[0] != frames[1], 'Variants must differ'
    assert all(a < b for a, b in zip(bytes.fromhex(PALETTE['mass']), bytes.fromhex(FLOOR)))
    return stats


def placements():
    result = [{'frame': 0, 'left': 8, 'top': 14}, {'frame': 1, 'left': 32, 'top': 14}]
    # Five-by-three contiguous mass around a two-cell floor notch. No grid lines.
    for y in range(3):
        for x in range(5):
            if (x, y) not in ((2, 1), (3, 1), (2, 2), (3, 2)):
                result.append({'frame': (x+2*y+(x*y)) % 2, 'left': 8+x*16, 'top': 44+y*24})
    return result


def verify():
    assert (OUT / 'shallow-caves.png').exists(), 'Native wall atlas missing: render required before GREEN'
    from PIL import Image  # Decode/read only. No raster writing or transforms.
    pins = guard()
    source = json.loads((OUT / 'source-cells.json').read_text())
    assert source['palette'] == PALETTE and source['floor_study_rgb_hex'] == FLOOR
    with Image.open(OUT / 'shallow-caves.png') as im:
        assert im.mode == 'RGBA'; rgba = im.tobytes(); stats = validate(source['frames'], im.size, rgba)
    checked = 0
    for filename, scale in (('contact-native.png', 1), ('contact.png', 8)):
        with Image.open(OUT / filename) as im:
            assert im.mode == 'RGBA' and im.size == (CONTACT_W*scale, CONTACT_H*scale)
            for p in placements():
                for y, row in enumerate(source['frames'][p['frame']]):
                    for x, value in enumerate(row):
                        for dy in range(scale):
                            for dx in range(scale):
                                assert im.getpixel(((p['left']+x)*scale+dx, (p['top']+y)*scale+dy)) == expected_pixel(value), ('Contact mismatch', filename, p, x, y)
                                checked += 1
            for x, y in ((41, 69), (71, 114)):
                assert im.getpixel((x*scale,y*scale)) == tuple(bytes.fromhex(FLOOR))+(255,), 'Floor contrast study drift'
    counters = []
    def reject(name, data=rgba, size=(32,24), frames=source['frames']):
        try: validate(frames, size, data)
        except (AssertionError, IndexError, KeyError, ValueError): counters.append(name); return
        raise AssertionError('Corruption accepted: '+name)
    reject('incorrect canvas', size=(32,16))
    for name, offset, value in (('transparent hole',3,0), ('partial alpha',7,127),
                                ('altered base edge',0,255), ('altered interior fracture',4*(5*32+4),255)):
        bad = bytearray(rgba); bad[offset] = value; reject(name, bytes(bad))
    swapped = bytearray()
    for y in range(H):
        row = rgba[y*32*4:(y+1)*32*4]; swapped += row[64:]+row[:64]
    reject('swapped variant pixels', bytes(swapped))
    duplicate = copy.deepcopy(source['frames']); duplicate[1] = copy.deepcopy(duplicate[0]); reject('duplicate variant source', frames=duplicate)
    wrong = copy.deepcopy(source['frames']); wrong[0][5][4][0] = 'invented-brick'; reject('named part drift', frames=wrong)
    meta = (ROOT / 'Assets/Resources/Sprites/Live16x24/beating-materials.png.meta').read_bytes()
    assert normalized_meta(meta) == normalized_meta(re.sub(rb'(?m)^guid: ([0-9a-f]+)', rb'guid: \1  ', meta))
    assert normalized_meta(meta) != normalized_meta(meta.replace(b'filterMode: 0', b'filterMode: 1'))
    try: check_meta(meta.replace(b'filterMode: 0', b'filterMode: 1'))
    except AssertionError: counters.append('changed importer rejected despite whitespace tolerance')
    else: raise AssertionError('Importer counter failed')
    assert (OUT / 'shallow-caves.blend').stat().st_size > 10000
    result = {'passed': True, 'dimensions': [32,24], 'frames': 2, 'palette': PALETTE,
              'exact_native_pixels_checked': 768, 'direct_contact_pixels_checked': checked,
              'fully_opaque': True, 'identical_base_edges': True, 'frame_stats': stats,
              'atlas_sha256': sha(OUT/'shallow-caves.png'), 'decoded_rgba_sha256': sha_bytes(rgba),
              'source_cells_sha256': sha(OUT/'source-cells.json'), 'blend_sha256': sha(OUT/'shallow-caves.blend'),
              'protected_exact_inputs': len(pins['exact_sha256']), 'protected_normalized_metas': len(pins['metadata']),
              'counterchecks_rejected': counters, 'metadata_whitespace_counter_accepted': True,
              'runtime_imported': False, 'runtime_native_tests': False, 'gameplay_acceptance': False,
              'limits': 'Original source/render verification only. Contacts are ungraded geometry studies, not gameplay. Actual lighting, repeated cave masses, destruction/reader parity, traversal and packaged/device acceptance remain separate.'}
    save('source-verification.json', result); print(json.dumps(result, indent=2))


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
    from build_opening_base import camera_for, emission_material, add_label
    from build_cindercaller import model
    from build_north_plot import instance, plane
    OUT.mkdir(parents=True,exist_ok=True); pins = guard(); frames = source_cells()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    native = bpy.context.scene; native.name = 'Shallow caves exact native 32x24'
    collections = []
    for index, cells in enumerate(frames):
        col = model(native, 'Shallow wall '+str(index)+' original integer cells', cells, PALETTE)
        native.collection.children.unlink(col); collections.append(col)
        instance(native,col,'Native frame '+str(index),index*W,0)
    camera_for(native,32,24); native.render.filepath = str(OUT/'shallow-caves.png')
    bpy.ops.render.render(write_still=True,scene=native.name)
    timing = {'shallow-caves.png': canonicalize_timing(OUT/'shallow-caves.png')}
    for scale, filename in ((1,'contact-native.png'),(8,'contact.png')):
        contact = bpy.data.scenes.new('Shallow walls direct geometry study '+str(scale)+'x')
        camera_for(contact,CONTACT_W,CONTACT_H,scale)
        plane(contact,'Quiet study backing','191915',0,0,CONTACT_W,CONTACT_H,-2)
        plane(contact,'Unchanged floor RGB study',FLOOR,40,4,32,48,-1)
        for i,p in enumerate(placements()):
            instance(contact,collections[p['frame']],str(i),p['left'],CONTACT_H-p['top']-24)
        label = emission_material('Study label '+str(scale),'D0C29F')
        add_label(contact,'SHALLOW CAVES / TWO ORIGINAL 16 x 24 CELLS',64,114,label,2)
        add_label(contact,'A',16,79,label,2); add_label(contact,'B',40,79,label,2)
        add_label(contact,'PLANAR',98,96,label,2)
        add_label(contact,'NO EDGE MASK',98,92,label,1.6)
        add_label(contact,'FLOOR BASE',103,65,label,1.7)
        add_label(contact,'RGB 128 106 76',103,61,label,1.5)
        add_label(contact,'UNGRADED',103,47,label,1.8)
        add_label(contact,'SOURCE STUDY',103,43,label,1.6)
        contact.render.filepath = str(OUT/filename)
        bpy.ops.render.render(write_still=True,scene=contact.name)
        timing[filename] = canonicalize_timing(OUT/filename)
    save('source-cells.json', {'schema':1,'coordinate_system':'Top-left y downward; each non-null named cell is an original editable integer quad at z0.',
                             'palette':PALETTE,'frames':frames,'floor_study_rgb_hex':FLOOR,
                             'authoring':'Original named geometry; reference images not sampled, traced or transformed.'})
    save('manifest.json', {'schema':1,'owner':'SandstoneWall','frames':[{'index':i,'rect':[i*16,0,16,24],'state':'Surviving structural wall; full and surviving partial HP use same intact mass.'} for i in range(2)],
                          'atlas':'shallow-caves.png','width':32,'height':24,'palette':PALETTE,
                          'sprite_pivot':[.5,.5],'pixels_per_unit':16,'expected_import':'Point,Clamp,no mipmaps,uncompressed,FullRect; root owns runtime import.',
                          'opaque':True,'edge_masks':False,'destroyed_state':'Owner removed by actual DestructionSystem; existing real Rubble handles its own presentation.',
                          'blend':'shallow-caves.blend','source_cells':'source-cells.json','contacts':['contact-native.png','contact.png'],
                          'contact_placements':placements(),'contact_size_native':[CONTACT_W,CONTACT_H],
                          'study_floor':'Only unchanged Beating floor base RGB128,106,76; no borrowed floor sprite reproduced or changed.',
                          'references':list(REFERENCES),'extraction':'Keep planar dark continuous mass and sparse interior fractures. Reject generated noise, nonuniform pixels, incorrect row labels, tile outlines, raised rubble, masonry, extrusion and cast shadows.',
                          'protected_input_counts':[len(pins['exact_sha256']),len(pins['metadata'])],
                          'png_nonpixel_timing_normalization':timing,'runtime_acceptance':False,'area_polish_acceptance':False})
    bpy.context.window.scene = native
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'shallow-caves.blend'))
    guard(); print('Built source-only original shallow wall cells and contacts; Assets untouched.')


def inspect_blend():
    import bpy
    source = json.loads((OUT/'source-cells.json').read_text()); faces = clusters = 0
    for index, cells in enumerate(source['frames']):
        col = bpy.data.collections.get('Shallow wall '+str(index)+' original integer cells'); assert col is not None
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
    native = bpy.data.scenes.get('Shallow caves exact native 32x24')
    assert native.render.resolution_x == 32 and native.render.resolution_y == 24 and native.render.resolution_percentage == 100
    instances = [o for o in native.objects if o.instance_type == 'COLLECTION']
    assert len(instances) == 2 and sorted(tuple(o.location) for o in instances) == [(0.,0.,0.),(16.,0.,0.)]
    for index in range(2):
        obj = next(o for o in instances if o.location.x == 16*index)
        assert obj.instance_collection.name == 'Shallow wall '+str(index)+' original integer cells'
        assert tuple(obj.rotation_euler) == (0.,0.,0.) and tuple(obj.scale) == (1.,1.,1.)
    result = {'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'original_integer_unit_faces':faces,
              'named_editable_mesh_clusters':clusters,'every_part_and_color_matches_source':True,'actual_emission_material_colors_verified':True,
              'native_dimensions':[32,24],'native_instances':[list(o.location) for o in instances],
              'blend_sha256':sha(OUT/'shallow-caves.blend'),'limits':'Saved editable geometry inspection, not runtime or gameplay.'}
    save('blend-geometry-verification.json',result); print(json.dumps(result,indent=2))


if __name__ == '__main__':
    argv = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('build','verify','inspect-blend'): parser.add_argument('--'+flag,action='store_true')
    args = parser.parse_args(argv); assert sum((args.build,args.verify,args.inspect_blend)) == 1
    if args.build: build()
    elif args.verify: verify()
    else: inspect_blend()
