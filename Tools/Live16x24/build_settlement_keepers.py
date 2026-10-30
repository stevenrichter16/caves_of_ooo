"""Three original native settlement keepers (Innkeeper, Quartermaster, Scribe); editable integer quads, not traced raster art.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --snapshot | --verify
Blender --background editable.blend --python this-file -- --inspect-blend
Pillow is read-only verification. Only this new source packet is written. No runtime publication command exists.
"""
import argparse, hashlib, json, re, shutil, struct, sys, uuid
from pathlib import Path
sys.dont_write_bytecode = True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/settlement-keepers'
RUNTIME=ROOT/'Assets/Resources/Sprites/Live16x24/settlement-keepers.png'
W,H,COUNT=16,24,3
CW,CH=140,48
PALETTE={'ink':'342E25','leather':'5F4830','leather-light':'8B6640',
         'skin':'E4C494','skin-shadow':'B69266','hair-dark':'4A3626','hair-light':'6B4E35','hair-grey':'9C988C',
         'shirt':'D2C4A0','shirt-shadow':'A69A7C','apron':'E2DCCB','apron-shadow':'B6AF9C','trouser':'463E35',
         'blue':'3F5B8A','blue-light':'6280AE','blue-shadow':'2B3E62',
         'ochre':'9A7A30','ochre-light':'BE9C48','ochre-shadow':'6D5522','brass':'D9B65C',
         'plum':'6A3B5E','plum-light':'8C5B7E','plum-shadow':'4A2842','parchment':'DCCDA2','parchment-shadow':'B3A27A',
         'quill':'ECE7DA','rope':'B49B68'}
BACK='032F23'
NAMES=('Innkeeper','Quartermaster','Scribe')
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
    paths=[ROOT/'Docs/Verification/Live16x24'/n for n in ('settlement-plants-boards-tine-offline.png','settlement-plants-boards-cinderhold-offline.png')]
    paths += [ROOT/'Tools/Live16x24/build_shared_specialists.py',ROOT/'ArtSource/Live16x24/shared-specialists/source-cells.json',ROOT/'Assets/Resources/Sprites/Live16x24/shared-specialists.png']
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
    # Same upright right-facing profile, y22 baseline and separated feet as the
    # approved cast and specialists; each role reads from its clothing silhouette.
    c,r=frame() # Innkeeper: blue vest over a cream shirt, long white apron.
    r('inn-hair-crown','hair-dark',6,3,4,2)
    r('inn-hair-light','hair-light',7,3,2)
    r('inn-hair-side','hair-dark',5,4,3,4)
    r('inn-face','skin',8,5,3,4)
    r('inn-ear','skin-shadow',7,7)
    r('inn-eye','ink',10,6)
    r('inn-profile-nose','skin',11,7)
    r('inn-neck','skin-shadow',8,9,2)
    r('inn-shirt-shoulders','shirt',6,10,5)
    r('inn-shirt','shirt',4,11,8,8)
    r('inn-vest-back','blue-shadow',4,11,2,5)
    r('inn-vest','blue',6,11,5,4)
    r('inn-vest-front','blue-light',9,11,2,3)
    r('inn-rear-sleeve','shirt-shadow',3,12,2,5)
    r('inn-rear-hand','skin-shadow',3,17,2)
    r('inn-front-sleeve','shirt',12,12,1,4)
    r('inn-front-hand','skin',12,16,1,2)
    r('inn-apron','apron',6,15,6,5)
    r('inn-apron-fold','apron-shadow',7,16,1,4)
    r('inn-apron-tie','apron-shadow',5,15)
    r('inn-left-trouser','trouser',5,20,2)
    r('inn-right-trouser','trouser',10,20,2)
    r('inn-left-boot','leather',5,21,2,2)
    r('inn-left-toe','leather-light',4,22)
    r('inn-right-boot','leather',10,21,2,2)
    r('inn-right-toe','leather-light',12,22)
    frames.append(c)
    c,r=frame() # Quartermaster: ochre service coat, brass buttons, crossbody satchel.
    r('qm-cap-crown','ochre-shadow',6,3,4,2)
    r('qm-cap-brim','ochre',6,5,6)
    r('qm-hair-side','hair-dark',5,5,2,3)
    r('qm-face','skin',8,6,3,3)
    r('qm-ear','skin-shadow',7,7)
    r('qm-eye','ink',10,6)
    r('qm-profile-nose','skin',11,8)
    r('qm-neck','skin-shadow',8,9,2)
    r('qm-shoulders','ochre-light',6,10,5)
    r('qm-coat','ochre',4,11,8,8)
    r('qm-coat-side','ochre-shadow',4,14,2,6)
    r('qm-rear-sleeve','ochre-shadow',3,12,2,5)
    r('qm-rear-glove','leather',3,17,2)
    r('qm-front-sleeve','ochre',12,13,1,4)
    r('qm-front-hand','skin',12,17,1,2)
    for i,(x,y) in enumerate(((11,11),(10,12),(9,13),(8,14),(7,15))):r('qm-satchel-strap-'+str(i),'leather',x,y)
    r('qm-satchel','leather-light',4,16,3,3)
    r('qm-satchel-flap','leather',4,16,3)
    r('qm-satchel-buckle','brass',5,17)
    r('qm-button-upper','brass',10,15)
    r('qm-button-lower','brass',10,17)
    r('qm-left-hem','ochre',5,19,2)
    r('qm-right-hem','ochre',9,19,3)
    r('qm-left-trouser','trouser',5,20,2)
    r('qm-right-trouser','trouser',10,20,2)
    r('qm-left-boot','leather',5,21,2,2)
    r('qm-left-toe','leather-light',4,22)
    r('qm-right-boot','leather',10,21,2,2)
    r('qm-right-toe','leather-light',12,22)
    frames.append(c)
    c,r=frame() # Scribe: long plum robe, quill behind the ear, a scroll in hand.
    r('scribe-quill','quill',7,1,1,2)
    r('scribe-hair-crown','hair-grey',6,3,4,2)
    r('scribe-hair-side','hair-grey',5,4,3,4)
    r('scribe-face','skin',8,5,3,4)
    r('scribe-ear','skin-shadow',7,7)
    r('scribe-eye','ink',10,6)
    r('scribe-profile-nose','skin',11,7)
    r('scribe-neck','skin-shadow',8,9,2)
    r('scribe-shoulders','plum-light',6,10,5)
    r('scribe-robe','plum',4,11,8,9)
    r('scribe-robe-side','plum-shadow',4,13,2,7)
    r('scribe-robe-front-fold','plum-light',9,12,1,7)
    r('scribe-sash','rope',5,15,7)
    r('scribe-rear-sleeve','plum-shadow',3,12,2,5)
    r('scribe-rear-hand','skin-shadow',3,17,2)
    r('scribe-front-sleeve','plum',12,12,1,3)
    r('scribe-front-hand','skin',12,15)
    r('scribe-scroll','parchment',12,16,2,3)
    r('scribe-scroll-edge','parchment-shadow',13,16,1,3)
    r('scribe-robe-hem','plum-shadow',5,20,7)
    r('scribe-left-sandal','leather',5,21,2,2)
    r('scribe-left-toe','leather-light',4,22)
    r('scribe-right-sandal','leather',10,21,2,2)
    r('scribe-right-toe','leather-light',12,22)
    frames.append(c)
    return frames

def pixel(v,palette=PALETTE):return tuple(bytes.fromhex(palette[v[1]]))+(255,) if v else (0,0,0,0)
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
    assert len({json.dumps(f) for f in frames})==3,'Aliased professions'
    return stats

def approved():
    source=json.loads((ROOT/'ArtSource/Live16x24/cast/source-cells.json').read_text())
    return source['frames'][0]['cells'],source['palette']
def placements():return [{'frame':-1,'x':8,'y':8},{'frame':0,'x':44,'y':8},{'frame':1,'x':80,'y':8},{'frame':2,'x':116,'y':8}]

def verify():
    assert (OUT/'settlement-keepers.png').exists(),'Native atlas absent; build required.'
    from PIL import Image # Decode only, never create/resize/composite/save.
    pins=guard();source=json.loads((OUT/'source-cells.json').read_text());assert source['palette']==PALETTE
    with Image.open(OUT/'settlement-keepers.png') as im:assert im.mode=='RGBA';rgba=im.tobytes();stats=validate(source['frames'],im.size,rgba)
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
    for label,frame,x,y,channel,value in (('removed face',0,8,6,3,0),('partial alpha',1,8,7,3,127),('wrong apron RGB',0,8,17,0,255),('opaque outside padding',2,0,0,3,255)):
        at=4*(y*48+frame*16+x)+channel;assert rgba[at]!=value
        bad=bytearray(rgba);bad[at]=value
        try:validate(source['frames'],(48,24),bytes(bad))
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    for label,frames,size in (('swapped professions',source['frames'][::-1],(48,24)),('wrong dimensions',source['frames'],(48,16))):
        try:validate(frames,size,rgba)
        except AssertionError:counters.append(label)
        else:raise AssertionError('Corruption accepted: '+label)
    result={'passed':True,'native_dimensions':[48,24],'exact_native_pixels_checked':1152,'direct_geometry_contact_pixels_checked':checked,'frame_stats':stats,'palette':PALETTE,'atlas_sha256':sha(OUT/'settlement-keepers.png'),'source_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'editable.blend'),'protected_exact_count':len(pins['exact']),'protected_meta_count':len(pins['meta_normalized']),'rejected_corruptions':counters,'runtime_png_present':RUNTIME.exists(),'gameplay_acceptance':False}
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
    native=bpy.context.scene;native.name='Keepers exact native 48x24';collections=[]
    for i,c in enumerate(frames):
        col=model(native,'Keeper frame '+str(i),c,PALETTE);native.collection.children.unlink(col);collections.append(col);instance(native,col,'Native '+str(i),16*i,0)
    camera_for(native,48,24);native.render.filepath=str(OUT/'settlement-keepers.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'settlement-keepers.png')
    old,old_palette=approved();oldcol=model(native,'Unchanged approved villager',old,old_palette);native.collection.children.unlink(oldcol)
    for scale,name in ((1,'contact-native.png'),(8,'contact.png')):
        scene=bpy.data.scenes.new('Direct keeper geometry '+str(scale)+'x');camera_for(scene,CW,CH,scale);plane(scene,'Approved grass study backing',BACK,0,0,CW,CH,-2)
        for i,p in enumerate(placements()):instance(scene,oldcol if p['frame']==-1 else collections[p['frame']],str(i),p['x'],CH-p['y']-24)
        label=emission_material('Study label #B5A47E','B5A47E')
        add_label(scene,'ORIGINAL SETTLEMENT KEEPERS / 16 x 24',CW/2,44,label,1.5)
        for x,name_label in ((16,'APPROVED VILLAGER'),(52,'INNKEEPER'),(88,'QUARTERMASTER'),(124,'SCRIBE')):add_label(scene,name_label,x,9,label,1.0)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'schema':1,'coordinates':'top-left,y down; null transparent; named unit quads atz0','palette':PALETTE,'frames':frames,'authorship':'Original hand-authored integer-cell geometry; no reference raster loaded as source or resampled.'})
    save('palette.json',PALETTE)
    save('manifest.json',{'atlas':'settlement-keepers.png','dimensions':[48,24],'frames':[{'index':i,'owner':NAMES[i],'rect':[16*i,0,16,24],'baseline_y':22,'meaning':'Static living body; role cues are clothing only, no new mechanics'} for i in range(3)],'blend':'editable.blend','source':'source-cells.json','palette':'palette.json','contacts':['contact-native.png','contact.png'],'contact_placements':placements(),'source_only':True,'gameplay_acceptance':False})
    save('import-spec.json',{'textureType':'Sprite','spriteMode':'Multiple','filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'sRGB':True,'compression':'Uncompressed','readable':False,'pixelsPerUnit':16,'pivot':[.5,.5],'rects':[[0,0,16,24],[16,0,16,24],[32,0,16,24]],'runtime_publication':'Root-owned; this builder writes no Assets files.'})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect_blend():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_limestone_caves import canonicalize_timing
    guard();frames=source_cells();faces=0;clusters=0
    for i,cells in enumerate(frames):
        col=bpy.data.collections.get('Keeper frame '+str(i));assert col is not None;recovered={}
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
    scene=bpy.data.scenes['Keepers exact native 48x24'];assert scene.render.resolution_x==48 and scene.render.resolution_y==24
    instances=[o for o in scene.objects if o.instance_type=='COLLECTION'];assert len(instances)==3
    for i in range(3):
        obj=next(o for o in instances if tuple(o.location)==(16*i,0,0));assert obj.instance_collection.name=='Keeper frame '+str(i) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
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
    original,file_before=chunks_without_file(OUT/'settlement-keepers.png');reopened,file_after=chunks_without_file(OUT/'reopened-native.png')
    assert original==reopened,'Saved-blend rerender changed image or non-File metadata'
    save('blend-geometry-verification.json',{'passed':True,'reopened_saved_blend':True,'blend_resaved':False,'unit_quad_faces':faces,'named_clusters':clusters,'exact_parts_colors_geometry':True,'reopened_render_exact_file_match':sha(OUT/'settlement-keepers.png')==sha(OUT/'reopened-native.png'),'reopened_image_chunks_exact':True,'metadata_only_difference':{'field':'File','before':file_before,'after':file_after},'blend_sha256':sha(OUT/'editable.blend'),'atlas_sha256':sha(OUT/'settlement-keepers.png'),'reopened_png_sha256':sha(OUT/'reopened-native.png'),'gameplay_acceptance':False})
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
