"""Original Greatdew native unit-quad source; never publishes Assets.
python3 this-file --snapshot | --verify
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --threads 4 --python this-file -- --inspect-blend
Pillow is used only to decode/check rendered PNGs, never to author/edit them.
"""
import argparse, hashlib, json, re, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/greatdew'
P = {'ink':'283323', 'stem-shadow':'485535', 'olive':'788444',
     'leaf-light':'A3AC68', 'dew-shadow':'7FA98B', 'mint':'B7D9BA', 'glint':'DCEBD1'}
BACKS = ('032F23', '202412')
COLLECTION = 'Greatdew editable native cells'
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
    # An asymmetric continuous bent stalk, not a mouth, pitcher or armed pose.
    for y,x,w in [(3,7,3),(4,6,5),(5,5,2),(5,10,2),(6,4,2),(6,11,2),
                  (7,4,2),(7,12,1),(8,3,2),(8,12,1),(9,3,2),
                  (10,3,2),(11,3,2),(12,3,2),(12,12,1),(13,4,2),
                  (14,4,2),(15,4,2),(16,4,2),(17,5,2),(18,5,2),
                  (19,5,2),(20,5,3),(21,5,3),(22,5,2)]:
        r('hooked-stalk','stem-shadow',x,y,w)
    for y,x,w in [(3,7,3),(4,6,3),(5,5,1),(5,10,1),(6,4,1),(6,11,1),
                  (7,4,1),(8,3,1),(9,3,1),(10,3,1),(11,3,1),(12,3,1),
                  (13,4,1),(14,4,1),(15,4,1),(16,4,1),(17,5,1),
                  (18,5,1),(19,5,1),(20,5,1),(21,5,1),(22,5,1)]:
        r('stalk-lit-edge','olive',x,y,w)
    r('hook-top-light','leaf-light',7,3,2)
    # Short hanging attachments join the three beads to the one living stalk.
    r('inner-dew-thread','stem-shadow',8,5,1,2)
    r('middle-dew-thread','olive',12,8)
    r('lower-dew-thread','olive',12,11,1,2)
    for index,(x,y) in enumerate(((8,8),(12,10),(12,14))):
        r('dew-'+str(index)+'-top','glint',x,y-1)
        r('dew-'+str(index)+'-left','mint',x-1,y)
        r('dew-'+str(index)+'-center','mint',x,y)
        r('dew-'+str(index)+'-base-left','dew-shadow',x-1,y+1)
        r('dew-'+str(index)+'-base','dew-shadow',x,y+1)
    # Two narrow basal leaves; no opaque ground platform or extra vegetation.
    for y,x,w in [(17,1,1),(18,1,2),(19,2,2),(20,3,2),(21,4,2),
                  (17,10,1),(18,9,2),(19,8,2),(20,7,2),(21,6,2)]:
        r('basal-leaves','stem-shadow',x,y,w)
    for x,y in [(1,17),(1,18),(2,19),(3,20),(4,21),(10,17),(9,18),(8,19),(7,20)]:
        r('leaf-ridges','olive',x,y)
    r('left-leaf-tip','leaf-light',1,17)
    r('right-leaf-tip','leaf-light',10,17)
    r('root-fold','ink',7,21)
    return c

def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never silently repin accepted inputs'
    # Reproducible approved inputs only. Foreign/Unity-normalized metadata is
    # preserved separately by root, not made a prerequisite of this source build.
    files=[ROOT/'Assets/Resources/Sprites/Environment/gin_frog.png']
    files += [ROOT/'Assets/Resources/Sprites/Live16x24'/n for n in
              ('reedfrog.png','bandfrog.png','sodden-travel.png','sill-envelope.png')]
    files += [OUT/'scene-reference-01.png',OUT/'scene-prompt-01.txt']
    files += [ROOT/'Docs/Verification/Live16x24'/n for n in ('greatdew-before-01-open-mire.png','greatdew-before-02-reader.png')]
    files += [ROOT/'Tools/Live16x24'/n for n in ('build_sodden_frogs.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in sorted(set(files))})

def pixel(v): return tuple(bytes.fromhex(P[v[1]]))+(255,) if v else (0,0,0,0)
def validate(c,size,data):
    assert c==cells() and size==(16,24) and len(data)==1536
    points=set()
    for y,row in enumerate(c):
        for x,v in enumerate(row):
            assert tuple(data[4*(y*16+x):4*(y*16+x+1)])==pixel(v),(x,y)
            if v:points.add((x,y))
    assert [min(x for x,y in points),min(y for x,y in points),max(x for x,y in points)+1,max(y for x,y in points)+1]==[1,3,13,23]
    seen={next(iter(points))};todo=list(seen)
    while todo:
        x,y=todo.pop()
        for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if q in points and q not in seen:seen.add(q);todo.append(q)
    assert seen==points,'Detached anatomy or bead'
    assert sum(v is not None and v[0].startswith('dew-') for row in c for v in row)==15
    assert all(v is None for v in c[23]),'Baseline padding must remain transparent'
    return {'opaque_pixels':len(points),'occupied_bounds':[1,3,13,23],'baseline_y':22,'beads':3,'basal_leaves':2}

def verify():
    from PIL import Image
    guard();s=json.loads((OUT/'source-cells.json').read_text());assert s['palette']==P
    with Image.open(OUT/'greatdew.png') as im:
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
    for name,x,y,ch,value in [('missing-bead',8,8,3,0),('wrong-stem',3,10,0,255),('partial-alpha',12,14,3,127),('opaque-padding',0,0,3,255)]:
        bad=bytearray(data);at=4*(y*16+x)+ch;assert bad[at]!=value;bad[at]=value
        try:validate(s['cells'],(16,24),bad)
        except AssertionError:rejected.append(name)
        else:raise AssertionError('Corruption accepted: '+name)
    save('source-verification.json',{'passed':True,**stats,'native_pixels':384,'direct_contact_pixels':count,
        'rejected_corruptions':rejected,'png_sha256':sha(OUT/'greatdew.png'),'blend_sha256':sha(OUT/'editable.blend'),
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
    native=bpy.context.scene;native.name='Greatdew exact native16x24'
    col=model(native,COLLECTION,cells(),P);camera_for(native,16,24)
    native.render.filepath=str(OUT/'greatdew.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'greatdew.png')
    for scale,name in [(1,'contact-native.png'),(8,'contact.png')]:
        scene=bpy.data.scenes.new('Greatdew direct geometry study '+str(scale)+'x');camera_for(scene,64,40,scale)
        for x,back in ((0,BACKS[0]),(32,BACKS[1])):plane(scene,'Study ground '+back,back,x,0,32,40,-2)
        instance(scene,col,'One body over green backing',8,8);instance(scene,col,'Same body over dark backing',40,8)
        label=emission_material('Study label','B5A47E');add_label(scene,'GREATDEW / ORIGINAL NATIVE SOURCE',32,37,label,1.1)
        add_label(scene,'ONE BODY / TWO BACKGROUNDS',32,3,label,1.0)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'coordinates':'Top-left y down; null transparent; named unit quads at z0','palette':P,'cells':cells(),
        'authorship':'Original hand-authored integer geometry. Concept inspected as reference, never traced, sampled or downscaled.'})
    save('palette.json',P)
    save('manifest.json',{'owner':'Greatdew','sprite':'greatdew.png','dimensions':[16,24],'rect':[0,0,16,24],
        'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','compression':'None','mipmaps':False,'alpha':'Binary; glassy bead impression from three mint tones, no glow or partial-alpha aura.',
        'baseline_y':22,'occupied_bounds':[1,3,13,23],'static_frames':1,'scope':'One neutral existing owner body, not armed/disarmed/triggered state; source only, no runtime publication.'})
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
    print('Reopened exact Greatdew geometry:',faces,'unit quads')

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('snapshot','build','verify','inspect-blend'):parser.add_argument('--'+name,action='store_true')
    opt=parser.parse_args(args);assert sum(vars(opt).values())==1
    if opt.snapshot:snapshot()
    elif opt.build:build()
    elif opt.verify:verify()
    else:inspect_blend()
