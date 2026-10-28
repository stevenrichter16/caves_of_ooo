"""Original Buckler native unit-quad source; never publishes Assets.
python3 this-file --snapshot | --verify
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --threads 4 --python this-file -- --inspect-blend
Pillow is used only to decode/check rendered PNGs, never to author/edit them.
"""
import argparse, hashlib, json, re, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/buckler'
P = {'outline':'2E352D', 'rim':'535E4C', 'face':'858D72',
     'face-shadow':'6C775F', 'rim-light':'A3AD8D', 'boss':'ABB597', 'glint':'CAD2B4'}
BACKS = ('032F23', '202412')
COLLECTION = 'Buckler editable native cells'
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(name, value): (OUT/name).write_text(json.dumps(value, indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for path,digest in pins.items(): assert sha(ROOT/path)==digest,path
    return pins

def cells():
    c=[[None]*16 for _ in range(24)]
    def r(part,color,x,y,w=1,h=1):
        assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
        for yy in range(y,y+h):
            for xx in range(x,x+w): c[yy][xx]=[part,color]
    # One compact round face: hand-authored stepped outline, not traced pixels.
    outer=[(9,5,6),(10,4,8),(11,3,10)]+[(y,2,12) for y in range(12,20)]+[(20,3,10),(21,4,8),(22,5,6)]
    for y,x,w in outer:r('round-silhouette','outline',x,y,w)
    for y,x,w in [(9,5,6),(10,4,2),(11,3,2)]+[(y,2,1) for y in range(12,19)]+[(19,2,2),(20,3,2),(21,4,2)]:
        r('upper-left-rim','rim',x,y,w)
    # Quiet continuous face with a restrained lower/right value change.
    for y,x,w in [(10,5,6),(11,4,8)]+[(y,3,10) for y in range(12,20)]+[(20,4,8),(21,5,6)]:
        r('shield-face','face',x,y,w)
    for y,x,w in [(12,12,1),(13,12,1),(14,12,1),(15,12,1),(16,11,2),
                  (17,11,2),(18,10,3),(19,9,4),(20,7,5),(21,5,6)]:
        r('lower-face-shade','face-shadow',x,y,w)
    # Two flat edge glints, not a halo, studs, crest or damage marks.
    r('rim-glints','rim-light',5,9,2)
    # The central boss has rounded corners and no square-button outline.
    for y,x,w in [(14,7,2),(15,6,4),(16,6,4),(17,7,2)]:
        r('rounded-boss-base','outline',x,y,w)
    r('boss-upper-plane','boss',7,15,2)
    r('boss-lower-plane','rim-light',7,16)
    r('boss-lower-right','face-shadow',8,16)
    r('boss-glint','glint',7,15)
    return c

def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never silently repin accepted inputs'
    # Reproducible approved inputs only. Foreign/Unity-normalized metadata is
    # preserved separately by root, not made a prerequisite of this source build.
    files=[ROOT/'Assets/Resources/Sprites/Live16x24'/n for n in ('opening-items.png','hatchet.png','leather-armor.png','cudgel.png')]
    files += [OUT/'scene-reference-01.png',OUT/'scene-prompt-01.txt',OUT/'parts-critique.md']
    files += [ROOT/'Docs/Verification/Live16x24/buckler-before-04-ground-reader.png']
    files += [ROOT/'Tools/Live16x24'/n for n in ('build_greatdew.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in sorted(set(files))})

def pixel(v): return tuple(bytes.fromhex(P[v[1]]))+(255,) if v else (0,0,0,0)
def validate(c,size,data):
    assert c==cells() and size==(16,24) and len(data)==1536
    points=set()
    for y,row in enumerate(c):
        for x,v in enumerate(row):
            assert tuple(data[4*(y*16+x):4*(y*16+x+1)])==pixel(v),(x,y)
            if v:points.add((x,y))
    assert [min(x for x,y in points),min(y for x,y in points),max(x for x,y in points)+1,max(y for x,y in points)+1]==[2,9,14,23]
    seen={next(iter(points))};todo=list(seen)
    while todo:
        x,y=todo.pop()
        for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if q in points and q not in seen:seen.add(q);todo.append(q)
    assert seen==points,'Detached shield part'
    assert len(points)==144 and sum(v is not None and v[0].startswith(('boss-','rounded-boss')) for row in c for v in row)==12
    assert all(v is None for v in c[23]),'Baseline padding must remain transparent'
    return {'opaque_pixels':len(points),'occupied_bounds':[2,9,14,23],'baseline_y':22,'single_body':True,'boss_pixels':12}

def verify():
    from PIL import Image
    guard();s=json.loads((OUT/'source-cells.json').read_text());assert s['palette']==P
    with Image.open(OUT/'buckler.png') as im:
        assert im.mode=='RGBA';data=im.tobytes();stats=validate(s['cells'],im.size,data)
    count=0
    for name,scale in [('contact-native.png',1),('contact.png',8)]:
        with Image.open(OUT/name) as im:
            assert im.mode=='RGBA' and im.size==(64*scale,40*scale)
            for x0,back in ((8,BACKS[0]),(40,BACKS[1])):
                for y,row in enumerate(cells()):
                    for x,v in enumerate(row):
                        expected=pixel(v) if v else tuple(bytes.fromhex(back))+(255,)
                        for dy in range(scale):
                            for dx in range(scale):
                                assert im.getpixel(((x0+x)*scale+dx,(8+y)*scale+dy))==expected
                                count+=1
    rejected=[]
    for name,x,y,ch,value in [('missing-boss',7,15,3,0),('wrong-face',4,14,0,255),('partial-rim-alpha',2,14,3,127),('opaque-padding',0,0,3,255)]:
        bad=bytearray(data);at=4*(y*16+x)+ch;assert bad[at]!=value;bad[at]=value
        try:validate(s['cells'],(16,24),bad)
        except AssertionError:rejected.append(name)
        else:raise AssertionError('Corruption accepted: '+name)
    save('source-verification.json',{'passed':True,**stats,'native_pixels':384,'direct_contact_pixels':count,
        'rejected_corruptions':rejected,'png_sha256':sha(OUT/'buckler.png'),'blend_sha256':sha(OUT/'editable.blend'),
        'source_sha256':sha(OUT/'source-cells.json'),'protected_inputs_unchanged':len(guard()),'runtime_or_gameplay_acceptance':False})
    print(json.dumps(stats))

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Buckler exact native16x24'
    col=model(native,COLLECTION,cells(),P);camera_for(native,16,24)
    native.render.filepath=str(OUT/'buckler.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'buckler.png')
    for scale,name in [(1,'contact-native.png'),(8,'contact.png')]:
        scene=bpy.data.scenes.new('Buckler direct geometry study '+str(scale)+'x');camera_for(scene,64,40,scale)
        for x,back in ((0,BACKS[0]),(32,BACKS[1])):plane(scene,'Study ground '+back,back,x,0,32,40,-2)
        instance(scene,col,'One body over green backing',8,8);instance(scene,col,'Same body over dark backing',40,8)
        label=emission_material('Study label','B5A47E');add_label(scene,'BUCKLER / ORIGINAL NATIVE SOURCE',32,37,label,1.1)
        add_label(scene,'ONE BODY / TWO BACKGROUNDS',32,3,label,1.0)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'coordinates':'Top-left y down; null transparent; named unit quads at z0','palette':P,'cells':cells(),
        'authorship':'Original hand-authored integer geometry. Concept inspected as reference, never traced, sampled or downscaled.'})
    save('palette.json',P)
    save('manifest.json',{'owner':'Buckler','sprite':'buckler.png','dimensions':[16,24],'rect':[0,0,16,24],
        'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','compression':'None','mipmaps':False,'alpha':'Binary; restrained flat rim/face/boss shading, no glow or ground shadow.',
        'baseline_y':22,'occupied_bounds':[2,9,14,23],'static_frames':1,'scope':'One neutral Buckler equipment body, not equipped/blocking/damaged state; source only, no runtime publication.'})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect_blend():
    import bpy
    guard();col=bpy.data.collections[COLLECTION];recovered={};faces=0
    for obj in col.objects:
        assert obj.type=='MESH' and tuple(obj.scale)==(1,1,1) and tuple(obj.location)==(0,0,0) and tuple(obj.rotation_euler)==(0,0,0)
        color=obj['paletteHex'];part=re.sub(r'\.\d+$','',obj.name);assert len(obj.data.materials)==1
        emission=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(emission)==1 and emission[0].inputs['Strength'].default_value==1
        def linear(v):
            s=v/255;return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
        assert all(abs(emission[0].inputs['Color'].default_value[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
        for face in obj.data.polygons:
            v=[obj.data.vertices[i].co for i in face.vertices];assert len(v)==4 and all(p.z==0 and p.x==int(p.x) and p.y==int(p.y) for p in v)
            x=min(p.x for p in v);y=min(p.y for p in v);assert max(p.x for p in v)-x==max(p.y for p in v)-y==1
            q=(int(x),23-int(y));assert q not in recovered;recovered[q]=(part,color);faces+=1
    assert recovered=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(cells()) for x,v in enumerate(row) if v}
    assert len(bpy.data.actions)==0
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'unit_quads':faces,
        'named_mesh_groups':len(col.objects),'exact_named_parts_palette_coordinates':True,'blend_sha256':sha(OUT/'editable.blend'),'runtime_or_gameplay_acceptance':False})
    print('Reopened exact Buckler geometry:',faces,'unit quads')

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('snapshot','build','verify','inspect-blend'):parser.add_argument('--'+name,action='store_true')
    opt=parser.parse_args(args);assert sum(vars(opt).values())==1
    if opt.snapshot:snapshot()
    elif opt.build:build()
    elif opt.verify:verify()
    else:inspect_blend()
