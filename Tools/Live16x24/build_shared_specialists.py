"""Two original native settlement specialists; editable integer quads, not traced raster art.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --snapshot | --verify
Blender --background editable.blend --python this-file -- --inspect-blend
Pillow is read-only verification. Only this new source packet is written. No runtime publication command exists.
"""
import argparse, hashlib, json, re, shutil, struct, sys, uuid
from pathlib import Path
sys.dont_write_bytecode = True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/shared-specialists'
RUNTIME=ROOT/'Assets/Resources/Sprites/Live16x24/shared-specialists.png'
W,H,COUNT=16,24,2
CW,CH=104,48
PALETTE={'ink':'342E25','leather':'5F4830','leather-light':'8B6640',
         'skin':'E4C494','skin-shadow':'B69266','teal':'3F6660','teal-light':'65867B',
         'teal-shadow':'2D4845','blue':'435C74','blue-light':'748998','blue-shadow':'2D4053',
         'rope':'B49B68','tool':'8D9B98'}
BACK='032F23'
NAMES=('Tinker','WellKeeper')
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
    paths=[OUT/n for n in ('scene-reference-01.png','scene-prompt-01.txt')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('shared-specialists-before-01-world.png','shared-specialists-before-02-tinker.png','shared-specialists-before-03-tinker-reader.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_opening_base.py','build_cindercaller.py','build_north_plot.py','build_limestone_caves.py','build_shared_camp_cloth.py')]
    resources=['Live16x24/opening-cast','Live16x24/opening-base','Live16x24/shared-camp-cloth','Environment/villager','Environment/merchant','Environment/weaponsmith']
    paths += [ROOT/'Assets/Resources/Sprites'/(n+'.png') for n in resources]
    paths += [ROOT/'ArtSource/Live16x24'/n for n in ('cast/opening-cast.blend','cast/source-cells.json','shared-camp-cloth/editable.blend','shared-camp-cloth/source-cells.json')]
    metas=[ROOT/'Assets/Resources/Sprites'/(n+'.png.meta') for n in resources]
    save('protected-inputs.json',{'exact':{str(p.relative_to(ROOT)):sha(p) for p in paths},'meta_raw_initial':{str(p.relative_to(ROOT)):sha(p) for p in metas},'meta_normalized':{str(p.relative_to(ROOT)):hashlib.sha256(normalized(p.read_bytes())).hexdigest() for p in metas},'scope':'Finite source dependencies, six approved resource pairs, references and actual baseline. Metadata tolerance is trailing whitespace only; nothing is rewritten.'})
    try:verify()
    except AssertionError as e:
        assert str(e)=='Native atlas absent; build required.'
        save('source-red.json',{'expected_failure':str(e),'native_atlas_absent':True,'runtime_atlas_absent':not RUNTIME.exists(),'runtime_tests':'Separate root-owned gate.'})
    else:raise AssertionError('Source RED did not fail.')

def source_cells():
    frames=[]
    def frame():
        cells=[[None]*W for _ in range(H)]
        def rect(part,color,x,y,w=1,h=1):
            assert color in PALETTE and 0<=x<x+w<=W and 0<=y<y+h<=H
            for yy in range(y,y+h):
                for xx in range(x,x+w):cells[yy][xx]=[part,color]
        return cells,rect
    # Original upright profile. Lowered hands and separated feet share the
    # accepted cast's baseline, while apron, workwear and cap remain role cues.
    c,r=frame()
    r('tinker-hair-crown','teal-shadow',6,3,4,2)
    r('tinker-hair-highlight','teal-light',6,3,2)
    r('tinker-hair-side','teal-shadow',5,5,3,4)
    r('tinker-face','skin',8,5,3,4)
    r('tinker-ear','skin-shadow',7,7)
    r('tinker-eye','ink',10,6)
    r('tinker-profile-nose','skin',11,7)
    r('tinker-neck','skin-shadow',8,9,2)
    r('tinker-sloped-shoulders','teal-light',6,10,5)
    r('tinker-work-tunic','teal',4,11,8,7)
    r('tinker-left-sleeve','teal-light',3,12,2,4)
    r('tinker-right-sleeve','teal-shadow',12,13,1,4)
    r('tinker-left-glove','leather',3,16,2,3)
    r('tinker-left-glove-light','leather-light',3,16,1,2)
    r('tinker-right-glove','leather',12,17,1,2)
    r('tinker-apron-strap','leather',6,10,1,4)
    r('tinker-apron-bib','leather-light',7,12,4,4)
    r('tinker-apron-shadow','leather',6,14,1,5)
    r('tinker-apron-skirt','leather-light',7,16,4,3)
    r('tinker-apron-fold','leather',10,17,1,2)
    r('tinker-waist-belt','leather',5,15,7)
    r('tinker-belt-buckle','rope',9,15)
    r('tinker-small-belt-tool','tool',5,16,1,2)
    r('tinker-small-belt-tool-handle','leather-light',5,18)
    r('tinker-left-trouser','teal-shadow',5,19,2,2)
    r('tinker-right-trouser','teal',10,19,2,2)
    r('tinker-left-boot','leather',5,21,2,2)
    r('tinker-left-toe','leather-light',4,22)
    r('tinker-right-boot','leather',10,21,2,2)
    r('tinker-right-toe','leather-light',12,22)
    frames.append(c)
    c,r=frame()
    r('keeper-cap-crown','leather',6,3,4,2)
    r('keeper-cap-top-light','leather-light',7,3,2)
    r('keeper-cap-side','leather',5,4,2,4)
    r('keeper-cap-brim','leather-light',6,5,6)
    r('keeper-face','skin',8,6,3,3)
    r('keeper-eye','ink',10,6)
    r('keeper-profile-nose','skin',11,8)
    r('keeper-neck','skin-shadow',8,9,2)
    r('keeper-sloped-shoulders','blue-light',6,10,5)
    r('keeper-coat','blue',4,11,8,8)
    r('keeper-coat-side','blue-shadow',4,14,2,6)
    r('keeper-left-sleeve','blue',3,12,2,5)
    r('keeper-left-cuff','blue-light',3,16,2)
    r('keeper-left-glove','leather',3,17,2,2)
    r('keeper-right-sleeve','blue-shadow',12,13,1,4)
    r('keeper-right-cuff','blue-light',12,16)
    r('keeper-right-glove','leather',12,17,1,2)
    r('keeper-collar-left','blue-light',6,11,2)
    r('keeper-collar-right','blue-light',10,11)
    r('keeper-collar-fold','blue-shadow',8,11,2)
    r('keeper-front-layer','blue-light',8,12,1,7)
    r('keeper-waist-belt','leather',5,15,7)
    r('keeper-small-rope-top','rope',5,15,2)
    r('keeper-small-rope-side','rope',5,16,1,2)
    r('keeper-small-rope-return','rope',7,16,1,2)
    r('keeper-small-rope-bottom','rope',6,18)
    r('keeper-left-coat-hem','blue',5,19,2)
    r('keeper-right-coat-hem','blue',9,19,3)
    r('keeper-left-trouser','blue-shadow',5,20,2)
    r('keeper-right-trouser','blue-shadow',10,20,2)
    r('keeper-left-boot','leather',5,21,2,2)
    r('keeper-left-toe','leather-light',4,22)
    r('keeper-right-boot','leather',10,21,2,2)
    r('keeper-right-toe','leather-light',12,22)
    frames.append(c)
    return frames

def pixel(v,palette=PALETTE):return tuple(bytes.fromhex(palette[v[1]]))+(255,) if v else (0,0,0,0)
def validate(frames,size,data):
    assert frames==source_cells(),'Named cell/source drift'
    assert size==(32,24) and len(data)==32*24*4,'Canvas mismatch'
    stats=[]
    for f,cells in enumerate(frames):
        points=[]
        for y,row in enumerate(cells):
            for x,v in enumerate(row):
                p=tuple(data[4*(y*32+f*16+x):4*(y*32+f*16+x)+4]);assert p==pixel(v),(f,x,y,p,v)
                assert p[3] in (0,255)
                if v:points.append((x,y))
        assert all(v is None for v in cells[23]),'Feet exceed accepted y22 baseline'
        assert all(cells[y][x] is None for y in range(H) for x in (0,1,14,15)),'Cell edge collision'
        assert cells[22][7] is None and cells[22][9] is None,'Feet merged'
        # A body must remain connected, not a collection of free-floating props.
        seen={points[0]};queue=[points[0]];remaining=set(points)
        while queue:
            x,y=queue.pop()
            for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if q in remaining and q not in seen:seen.add(q);queue.append(q)
        assert seen==remaining,'Detached body/tool cluster'
        stats.append({'frame':f,'owner':NAMES[f],'opaque_pixels':len(points),'bounds':[min(x for x,y in points),min(y for x,y in points),max(x for x,y in points)+1,max(y for x,y in points)+1]})
    assert frames[0]!=frames[1],'Aliased professions'
    return stats

def approved():
    source=json.loads((ROOT/'ArtSource/Live16x24/cast/source-cells.json').read_text())
    return source['frames'][0]['cells'],source['palette']
def placements():return [{'frame':-1,'x':8,'y':8},{'frame':0,'x':44,'y':8},{'frame':1,'x':80,'y':8}]

def verify():
    assert (OUT/'shared-specialists.png').exists(),'Native atlas absent; build required.'
    from PIL import Image # Decode only, never create/resize/composite/save.
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text());assert source['palette']==PALETTE
    with Image.open(OUT/'shared-specialists.png') as im:assert im.mode=='RGBA';rgba=im.tobytes();stats=validate(source['frames'],im.size,rgba)
    checked=0;old,old_palette=approved()
    for name,scale in (('contact-native.png',1),('contact.png',8)):
        with Image.open(OUT/name) as im:
            assert im.mode=='RGBA' and im.size==(CW*scale,CH*scale)
            for p in placements():
                cells,palette=(old,old_palette) if p['frame']==-1 else (source['frames'][p['frame']],PALETTE)
                for y,row in enumerate(cells):
                    for x,v in enumerate(row):
                        expected=pixel(v,palette) if v else tuple(bytes.fromhex(BACK))+(255,)
                        for dy in range(scale):
                            for dx in range(scale):assert im.getpixel(((p['x']+x)*scale+dx,(p['y']+y)*scale+dy))==expected;checked+=1
    counters=[]
    # Mutate real populated/padding coordinates so each control has a positive precondition.
    for label,frame,x,y,channel,value in (('removed face',0,8,6,3,0),('partial alpha',1,8,7,3,127),('wrong apron RGB',0,8,13,0,255),('opaque outside padding',1,0,0,3,255)):
        at=4*(y*32+frame*16+x)+channel;assert rgba[at]!=value
        bad=bytearray(rgba);bad[at]=value
        try:validate(source['frames'],(32,24),bytes(bad))
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    for label,frames,size in (('swapped professions',source['frames'][::-1],(32,24)),('wrong dimensions',source['frames'],(32,16))):
        try:validate(frames,size,rgba)
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    result={'passed':True,'native_dimensions':[32,24],'exact_native_pixels_checked':768,'direct_geometry_contact_pixels_checked':checked,'frame_stats':stats,'palette':PALETTE,'atlas_sha256':sha(OUT/'shared-specialists.png'),'source_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'editable.blend'),'protected_exact_count':len(pins['exact']),'protected_meta_count':len(pins['meta_normalized']),'rejected_corruptions':counters,'runtime_png_present':RUNTIME.exists(),'gameplay_acceptance':False}
    save('source-verification.json',result);print(json.dumps(result,indent=2))

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for, add_label, emission_material
    from build_cindercaller import model
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();frames=source_cells();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Specialists exact native 32x24';collections=[]
    for i,c in enumerate(frames):
        col=model(native,'Specialist frame '+str(i),c,PALETTE);native.collection.children.unlink(col);collections.append(col);instance(native,col,'Native '+str(i),16*i,0)
    camera_for(native,32,24);native.render.filepath=str(OUT/'shared-specialists.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'shared-specialists.png')
    old,old_palette=approved();oldcol=model(native,'Unchanged approved villager',old,old_palette);native.collection.children.unlink(oldcol)
    for scale,name in ((1,'contact-native.png'),(8,'contact.png')):
        scene=bpy.data.scenes.new('Direct specialist geometry '+str(scale)+'x');camera_for(scene,CW,CH,scale);plane(scene,'Approved grass study backing',BACK,0,0,CW,CH,-2)
        for i,p in enumerate(placements()):instance(scene,oldcol if p['frame']==-1 else collections[p['frame']],str(i),p['x'],CH-p['y']-24)
        label=emission_material('Study label #B5A47E','B5A47E')
        add_label(scene,'ORIGINAL SPECIALISTS / 16 x 24',CW/2,44,label,1.5)
        for x,name_label in ((16,'APPROVED VILLAGER'),(52,'TINKER'),(88,'WELLKEEPER')):add_label(scene,name_label,x,9,label,1.0)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'schema':1,'coordinates':'top-left,y down; null transparent; named unit quads atz0','palette':PALETTE,'frames':frames,'authorship':'Original hand-authored integer-cell geometry; no reference raster loaded as source or resampled.'})
    save('palette.json',PALETTE)
    save('manifest.json',{'atlas':'shared-specialists.png','dimensions':[32,24],'frames':[{'index':i,'owner':NAMES[i],'rect':[16*i,0,16,24],'baseline_y':22,'meaning':'Static living body; no service props, held weapon, effects or new mechanics'} for i in range(2)],'blend':'editable.blend','source':'source-cells.json','palette':'palette.json','contacts':['contact-native.png','contact.png'],'contact_placements':placements(),'source_only':True,'gameplay_acceptance':False})
    save('import-spec.json',{'textureType':'Sprite','spriteMode':'Multiple','filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'sRGB':True,'compression':'Uncompressed','readable':False,'pixelsPerUnit':16,'pivot':[.5,.5],'rects':[[0,0,16,24],[16,0,16,24]],'runtime_publication':'Root-owned; this builder writes no Assets files.'})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect_blend():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_limestone_caves import canonicalize_timing
    guard();frames=source_cells();faces=0;clusters=0
    for i,cells in enumerate(frames):
        col=bpy.data.collections.get('Specialist frame '+str(i));assert col is not None;recovered={}
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
    scene=bpy.data.scenes['Specialists exact native 32x24'];assert scene.render.resolution_x==32 and scene.render.resolution_y==24
    instances=[o for o in scene.objects if o.instance_type=='COLLECTION'];assert len(instances)==2
    for i in range(2):
        obj=next(o for o in instances if tuple(o.location)==(16*i,0,0));assert obj.instance_collection.name=='Specialist frame '+str(i) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
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
    original,file_before=chunks_without_file(OUT/'shared-specialists.png');reopened,file_after=chunks_without_file(OUT/'reopened-native.png')
    assert original==reopened,'Saved-blend rerender changed image or non-File metadata'
    save('blend-geometry-verification.json',{'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'unit_quad_faces':faces,'named_clusters':clusters,'exact_parts_colors_geometry':True,'reopened_render_exact_file_match':sha(OUT/'shared-specialists.png')==sha(OUT/'reopened-native.png'),'reopened_image_chunks_exact':True,'metadata_only_difference':{'field':'File','before':file_before,'after':file_after},'blend_sha256':sha(OUT/'editable.blend'),'atlas_sha256':sha(OUT/'shared-specialists.png'),'reopened_png_sha256':sha(OUT/'reopened-native.png'),'gameplay_acceptance':False})
    print('Saved editable geometry and exact rerender image chunks verified:',faces,'faces',clusters,'clusters')

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):parser.add_argument('--'+flag,action='store_true')
    opt=parser.parse_args(args);assert sum(vars(opt).values())==1
    if opt.snapshot:snapshot()
    elif opt.build:build()
    elif opt.verify:verify()
    else:inspect_blend()
