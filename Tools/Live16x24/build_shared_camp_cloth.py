"""Three original native cloth cells; editable integer quads, not traced raster art.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --snapshot | --verify | --publish
Blender --background editable.blend --python this-file -- --inspect-blend
Pillow is read-only verification. Only this new packet/runtime atlas are written.
"""
import argparse, hashlib, json, re, shutil, struct, sys, uuid
from pathlib import Path
sys.dont_write_bytecode = True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/shared-camp-cloth'
RUNTIME=ROOT/'Assets/Resources/Sprites/Live16x24/shared-camp-cloth.png'
W,H,COUNT=16,24,3
CW,CH=144,112
PALETTE={'shadow':'25231F','cloth':'3D3931','face':'504A3D','fold-light':'625845',
         'crease':'302C26','wood-dark':'57412B','ochre':'927040','tie':'B08D52'}
BACK='71624A'
NAMES=('TentWall cloth A','TentWall cloth B','GuestClothPole')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,value):(OUT/name).write_text(json.dumps(value,indent=2)+'\n')
def normalized(b):return re.sub(rb'[ \t]+(?=\r?$)',b'',b,flags=re.M)
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins['exact'].items():assert sha(ROOT/name)==h,'Protected input changed: '+name
    for name,h in pins['meta_normalized'].items():assert hashlib.sha256(normalized((ROOT/name).read_bytes())).hexdigest()==h,'Protected meta changed: '+name
    return pins

def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Do not silently repin prior source.'
    paths=[OUT/n for n in ('scene-reference-01.png','scene-prompt-01.txt','reference-manifest.json')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('shared-camp-props-before-01-brine.png','shared-camp-cloth-before-02-wall-reader.png','shared-camp-cloth-before-03-pole-reader.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_opening_base.py','build_cindercaller.py','build_north_plot.py','build_limestone_caves.py','build_stump_materials.py')]
    resources=['Live16x24/opening-cast','Live16x24/beating-materials','Live16x24/sill-envelope','Environment/cont_sack','Environment/cont_urn','Environment/well']
    paths += [ROOT/'Assets/Resources/Sprites'/(n+'.png') for n in resources]
    paths += [ROOT/'ArtSource/Live16x24'/n for n in ('cast/opening-cast.blend','cast/source-cells.json','beating-materials/beating-materials.blend','beating-materials/source-cells.json')]
    metas=[ROOT/'Assets/Resources/Sprites'/(n+'.png.meta') for n in resources]
    save('protected-inputs.json',{'exact':{str(p.relative_to(ROOT)):sha(p) for p in paths},'meta_raw_initial':{str(p.relative_to(ROOT)):sha(p) for p in metas},'meta_normalized':{str(p.relative_to(ROOT)):hashlib.sha256(normalized(p.read_bytes())).hexdigest() for p in metas},'scope':'Finite source dependencies, six approved resource pairs, references and actual baseline. Metadata tolerance is trailing whitespace only; nothing is rewritten.'})
    try:verify()
    except AssertionError as e:
        assert str(e)=='Native atlas absent; build required.'
        save('source-red.json',{'expected_failure':str(e),'native_atlas_absent':True,'runtime_atlas_absent':not RUNTIME.exists(),'runtime_tests':'Separate root-owned gate.'})
    else:raise AssertionError('Source RED did not fail.')

def source_cells():
    frames=[]
    for v in range(2):
        c=[[['woven-cloth-body','cloth'] for x in range(W)] for y in range(H)]
        def rect(part,color,x,y,w=1,h=1):
            assert color in PALETTE and 0<=x<x+w<=W and 0<=y<y+h<=H
            for yy in range(y,y+h):
                for xx in range(x,x+w):c[yy][xx]=[part,color]
        # Two broad hanging folds; internal facets only, no rooftop or masonry.
        for top in ((4,14) if v==0 else (5,15)):
            rect('broad-fold-face','face',1,top,14,3)
            rect('fold-face-center','face',2,top+3,12,2)
            rect('fold-slack-face','face',4,top+5,8)
            rect('fold-side-light','fold-light',1,top,3)
            rect('fold-right-light','fold-light',12,top,3)
            # Short staggered sag replaces the old horizontal ladder rung.
            start=4 if v==0 else 7
            rect('fold-sag-side','crease',start,top+5,2)
            rect('fold-sag-low','shadow',start+2,top+6,3)
            rect('fold-sag-tip','crease',start+5,top+5)
            if v==0:rect('unequal-fold-facet','cloth',10,top+1,3,2)
            else:rect('unequal-fold-facet','cloth',3,top+1,3,2)
        # Same opaque straight bounds; quieter one-column timber supports.
        rect('left-support-dark','wood-dark',0,0,1,24)
        rect('right-support-dark','wood-dark',15,0,1,24)
        rect('left-timber-accent','ochre',0,1,1,3)
        rect('right-timber-accent','ochre',15,20,1,3)
        for y in ((3,16) if v==0 else (4,17)):
            rect('left-small-lashing','ochre',0,y,2)
            rect('right-small-lashing','ochre',14,y+1,2)
        frames.append(c)
    c=[[None]*W for _ in range(H)]
    def rect(part,color,x,y,w=1,h=1):
        assert color in PALETTE and 0<=x<x+w<=W and 0<=y<y+h<=H
        for yy in range(y,y+h):
            for xx in range(x,x+w):c[yy][xx]=[part,color]
    rect('slender-post-shadow','wood-dark',6,3,2,20)
    rect('slender-ochre-post','ochre',6,3,1,19)
    rect('post-cap','tie',6,2,2)
    rect('short-cloth-arm','wood-dark',7,5,6)
    rect('small-dark-hanging-cloth','shadow',8,6,5,8)
    rect('quiet-cloth-face','cloth',9,7,3,6)
    rect('cloth-inner-fold','face',9,7,1,5)
    rect('unequal-short-cloth-hem','shadow',8,14,2)
    rect('small-attachment-knot','tie',7,6,2)
    frames.append(c)
    return frames

def pixel(v):return tuple(bytes.fromhex(PALETTE[v[1]]))+(255,) if v else (0,0,0,0)
def validate(frames,size,data):
    assert frames==source_cells(),'Named cell/source drift'
    assert size==(48,24) and len(data)==48*24*4,'Canvas mismatch'
    stats=[]
    for f,cells in enumerate(frames):
        points=[]
        for y,row in enumerate(cells):
            for x,v in enumerate(row):
                p=tuple(data[4*(y*48+f*16+x):4*(y*48+f*16+x)+4]);assert p==pixel(v),(f,x,y,p,v)
                assert p[3] in (0,255)
                if v:points.append((x,y))
                if f<2:assert v is not None,'Wall hole'
        stats.append({'frame':f,'owner':'TentWall' if f<2 else 'GuestClothPole','opaque_pixels':len(points),'bounds':[min(x for x,y in points),min(y for x,y in points),max(x for x,y in points)+1,max(y for x,y in points)+1]})
    assert frames[0]!=frames[1] and len(points)<100
    assert all(v is None for v in frames[2][23]),'Pole must not reach outside baseline'
    return stats

def placements():
    p=[{'frame':i,'x':8+i*26,'y':8} for i in range(3)]
    p += [{'frame':(x+y)%2,'x':8+x*16,'y':52+y*24} for y in range(2) for x in range(4)]
    p += [{'frame':2,'x':94,'y':64},{'frame':2,'x':118,'y':64}]
    return p

def verify():
    assert (OUT/'shared-camp-cloth.png').exists(),'Native atlas absent; build required.'
    from PIL import Image # Decode only: no image creation, resize, composition or save.
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text());assert source['palette']==PALETTE
    prior=json.loads((OUT/'versions/v1/source-cells.json').read_text());assert source['frames'][2]==prior['frames'][2] and source['palette']==prior['palette'],'Pole/palette changed in wall-only correction'
    with Image.open(OUT/'shared-camp-cloth.png') as im:assert im.mode=='RGBA';rgba=im.tobytes();stats=validate(source['frames'],im.size,rgba)
    checked=0
    for name,scale in (('contact-native.png',1),('contact.png',8)):
        with Image.open(OUT/name) as im:
            assert im.mode=='RGBA' and im.size==(CW*scale,CH*scale)
            for p in placements():
                for y,row in enumerate(source['frames'][p['frame']]):
                    for x,v in enumerate(row):
                        expected=pixel(v) if v else tuple(bytes.fromhex(BACK))+(255,)
                        for dy in range(scale):
                            for dx in range(scale):assert im.getpixel(((p['x']+x)*scale+dx,(p['y']+y)*scale+dy))==expected;checked+=1
    counters=[]
    for label,offset,value in (('wall transparent hole',3,0),('partial alpha',7,127),('changed cloth color',4*3,255),('opaque pole padding',4*(32)+3,255)):
        bad=bytearray(rgba);bad[offset]=value
        try:validate(source['frames'],(48,24),bytes(bad))
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    for label,frames,size in (('swapped wall frames',[source['frames'][1],source['frames'][0],source['frames'][2]],(48,24)),('wrong dimensions',source['frames'],(48,16))):
        try:validate(frames,size,rgba)
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    result={'passed':True,'native_dimensions':[48,24],'exact_native_pixels_checked':1152,'direct_geometry_contact_pixels_checked':checked,'frame_stats':stats,'palette':PALETTE,'atlas_sha256':sha(OUT/'shared-camp-cloth.png'),'source_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'editable.blend'),'protected_exact_count':len(pins['exact']),'protected_meta_count':len(pins['meta_normalized']),'rejected_corruptions':counters,'runtime_png_present':RUNTIME.exists(),'gameplay_acceptance':False}
    if RUNTIME.exists():
        current=sha(RUNTIME);result['runtime_png_sha256']=current;result['runtime_matches_source']=current==result['atlas_sha256']
        if not result['runtime_matches_source']:assert current==sha(OUT/'versions/v1/runtime-shared-camp-cloth.png'),'Unexpected runtime drift'
    result['source_version']=2;result['unchanged_v1_pole_and_palette']=True
    save('source-verification.json',result);print(json.dumps(result,indent=2))

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for
    from build_cindercaller import model
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();frames=source_cells();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Camp cloth exact native 48x24';collections=[]
    for i,c in enumerate(frames):
        col=model(native,'Camp cloth frame '+str(i),c,PALETTE);native.collection.children.unlink(col);collections.append(col);instance(native,col,'Native '+str(i),16*i,0)
    camera_for(native,48,24);native.render.filepath=str(OUT/'shared-camp-cloth.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'shared-camp-cloth.png')
    for scale,name in ((1,'contact-native.png'),(8,'contact.png')):
        scene=bpy.data.scenes.new('Direct cloth geometry '+str(scale)+'x');camera_for(scene,CW,CH,scale);plane(scene,'Neutral study backing',BACK,0,0,CW,CH,-2)
        for i,p in enumerate(placements()):instance(scene,collections[p['frame']],str(i),p['x'],CH-p['y']-24)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'schema':1,'coordinates':'top-left,y down; null transparent; named unit quads atz0','palette':PALETTE,'frames':frames,'authorship':'Original hand-authored integer-cell geometry; no reference raster loaded as source or resampled.'})
    save('palette.json',PALETTE)
    save('manifest.json',{'source_version':2,'revision':'Wall-only support/fold correction; v1 preserved in versions/v1; palette and pole identical.','atlas':'shared-camp-cloth.png','dimensions':[48,24],'frames':[{'index':i,'owner':'TentWall' if i<2 else 'GuestClothPole','rect':[16*i,0,16,24],'opaque':i<2,'meaning':'Surviving Cloth wall; mechanics retain8HP,thermal andcollision' if i<2 else 'Passable pole with no invented timer/service'} for i in range(3)],'blend':'editable.blend','source':'source-cells.json','palette':'palette.json','contacts':['contact-native.png','contact.png'],'contact_placements':placements(),'source_only':True,'gameplay_acceptance':False})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect_blend():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_limestone_caves import canonicalize_timing
    guard();frames=source_cells();faces=0;clusters=0
    for i,cells in enumerate(frames):
        col=bpy.data.collections.get('Camp cloth frame '+str(i));assert col is not None;recovered={}
        for obj in col.objects:
            clusters+=1;assert obj.type=='MESH' and tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
            color=obj['paletteHex'];assert color in PALETTE.values();part=re.sub(r'\.\d+$','',obj.name);assert len(obj.data.materials)==1
            emission=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(emission)==1 and emission[0].inputs['Strength'].default_value==1
            def linear(v):s=v/255;return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
            actual=emission[0].inputs['Color'].default_value;assert all(abs(actual[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
            for poly in obj.data.polygons:
                vs=[obj.data.vertices[j].co for j in poly.vertices];assert len(vs)==4 and all(v.z==0 and v.x==int(v.x) and v.y==int(v.y) for v in vs)
                left,right=min(v.x for v in vs),max(v.x for v in vs);bottom,top=min(v.y for v in vs),max(v.y for v in vs);assert right-left==top-bottom==1
                key=(int(left),23-int(bottom));assert key not in recovered;recovered[key]=(part,color);faces+=1
        assert recovered=={(x,y):(v[0],PALETTE[v[1]]) for y,row in enumerate(cells) for x,v in enumerate(row) if v}
    scene=bpy.data.scenes['Camp cloth exact native 48x24'];assert scene.render.resolution_x==48 and scene.render.resolution_y==24
    instances=[o for o in scene.objects if o.instance_type=='COLLECTION'];assert len(instances)==3
    for i in range(3):
        obj=next(o for o in instances if tuple(o.location)==(16*i,0,0));assert obj.instance_collection.name=='Camp cloth frame '+str(i) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
    scene.render.filepath=str(OUT/'reopened-native.png');bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'reopened-native.png')
    # Loading the saved blend changes Blender's File text stamp only. Do not
    # change either PNG to hide that provenance; compare every other chunk.
    def chunks_without_file(path):
        raw=path.read_bytes();assert raw[:8]==b'\x89PNG\r\n\x1a\n';at=8;chunks=[];file_text=[]
        while at<len(raw):
            size=struct.unpack('>I',raw[at:at+4])[0];kind=raw[at+4:at+8];body=raw[at+8:at+8+size]
            if kind==b'tEXt' and body.startswith(b'File\0'):file_text.append(body.decode())
            else:chunks.append(raw[at:at+12+size])
            at+=12+size
        assert at==len(raw) and len(file_text)==1
        return chunks,file_text[0]
    original,file_before=chunks_without_file(OUT/'shared-camp-cloth.png');reopened,file_after=chunks_without_file(OUT/'reopened-native.png')
    assert original==reopened,'Saved-blend rerender changed image or non-File metadata'
    save('blend-geometry-verification.json',{'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'unit_quad_faces':faces,'named_clusters':clusters,'exact_parts_colors_geometry':True,'reopened_render_exact_file_match':False,'reopened_image_chunks_exact':True,'metadata_only_difference':{'field':'File','before':file_before,'after':file_after},'source_verifier_correction':'Initial whole-file rerender assertion rejected Blender saved-file text. All pixel/image chunks were equal; comparison now allows only that one provenance text change. No pixel/source changes.','blend_sha256':sha(OUT/'editable.blend'),'atlas_sha256':sha(OUT/'shared-camp-cloth.png'),'reopened_png_sha256':sha(OUT/'reopened-native.png'),'gameplay_acceptance':False})
    print('Saved editable geometry and exact rerender image chunks verified:',faces,'faces',clusters,'clusters')

def publish():
    guard();proof=json.loads((OUT/'source-verification.json').read_text());geo=json.loads((OUT/'blend-geometry-verification.json').read_text());assert proof['passed'] and geo['passed']
    assert proof['atlas_sha256']==geo['atlas_sha256']==sha(OUT/'shared-camp-cloth.png')
    assert sha(RUNTIME)==sha(OUT/'versions/v1/runtime-shared-camp-cloth.png'),'Only root-authorized v1 candidate overwrite'
    meta=Path(str(RUNTIME)+'.meta');old_meta=OUT/'versions/v1/runtime-shared-camp-cloth.png.meta'
    assert normalized(meta.read_bytes())==normalized(old_meta.read_bytes()),'Unexpected metadata semantic drift'
    meta_before=sha(meta);shutil.copyfile(OUT/'shared-camp-cloth.png',RUNTIME);assert sha(meta)==meta_before;verify()
    save('publication.json',{'runtime_png':str(RUNTIME.relative_to(ROOT)),'png_sha256':sha(RUNTIME),'meta_sha256':sha(Path(str(RUNTIME)+'.meta')),'native_runtime_acceptance':False,'version':2,'previous_runtime_png_sha256':sha(OUT/'versions/v1/runtime-shared-camp-cloth.png'),'meta_preserved_during_publication':True})

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend','publish'):parser.add_argument('--'+flag,action='store_true')
    opt=parser.parse_args(args);assert sum(vars(opt).values())==1
    if opt.snapshot:snapshot()
    elif opt.build:build()
    elif opt.verify:verify()
    elif opt.inspect_blend:inspect_blend()
    else:publish()
