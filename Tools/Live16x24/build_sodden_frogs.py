"""Original Reedfrog/Bandfrog native cells; source packet only, no Assets writes.
python3 this-file --snapshot | --verify
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --threads 4 --python this-file -- --inspect-blend
Pillow only decodes approved/source PNGs for comparison and exact validation.
"""
import argparse, hashlib, json, re, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/sodden-frogs'
GIN = ROOT / 'Assets/Resources/Sprites/Environment/gin_frog.png'
NAMES = ('Reedfrog', 'Bandfrog')
P = {'ink':'19211B', 'reed-shadow':'344D2D', 'reed':'597B36',
     'reed-light':'8CA54E', 'throat':'B9C57B', 'eye':'E2D7A0',
     'coal':'252A29', 'coal-light':'3B4140', 'scarlet':'C84332',
     'scarlet-light':'E46B3D'}
BACK = '032F23'
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(name, value): (OUT/name).write_text(json.dumps(value, indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for path, digest in pins.items(): assert sha(ROOT/path)==digest, path
    return pins

def cells():
    frames=[]
    def new():
        c=[[None]*16 for _ in range(24)]
        def r(part,color,x,y,w=1,h=1):
            assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
            for yy in range(y,y+h):
                for xx in range(x,x+w): c[yy][xx]=[part,color]
        return c,r
    c,r=new()
    # Fat frontal three-quarter frog. Eyes rise only two cells above the back;
    # broad tucked thighs, four planted feet and a quiet closed mouth.
    for y,x,w in [(12,4,2),(12,10,2),(13,3,4),(13,9,4),(14,3,10),
                  (15,2,12),(16,1,14),(17,1,14),(18,1,14),(19,2,12),
                  (20,3,10),(21,2,4),(21,10,4),(22,1,5),(22,10,5)]:
        r('reed-body-outline','ink',x,y,w)
    r('reed-broad-back','reed',4,14,8,5)
    r('reed-left-haunch','reed-shadow',2,16,3,3)
    r('reed-right-haunch','reed-shadow',11,16,3,3)
    r('reed-left-haunch-light','reed-light',2,16,3)
    r('reed-right-haunch-light','reed',11,16,3)
    r('reed-left-fold','ink',3,18,2)
    r('reed-right-fold','ink',11,18,2)
    r('reed-left-cheek','reed-light',3,15,3)
    r('reed-right-cheek','reed-light',10,15,3)
    r('reed-back-light','reed-light',6,14,3)
    r('reed-left-eye-rim','eye',4,12,2,2)
    r('reed-left-pupil','ink',5,13)
    r('reed-right-eye-rim','eye',10,12,2,2)
    r('reed-right-pupil','ink',11,13)
    r('reed-closed-mouth','reed-shadow',5,16,6)
    for y,x,w in [(17,6,4),(18,5,6),(19,5,6),(20,6,4)]:
        r('reed-round-throat','throat',x,y,w)
    r('reed-belly-shadow','reed',5,20,6)
    r('reed-low-belly','throat',6,20,4)
    r('reed-throat-light','eye',6,17,3)
    r('reed-left-foreleg','reed',4,18,1,4)
    r('reed-right-foreleg','reed-shadow',11,18,1,4)
    r('reed-left-hind-ankle','reed',2,20,2)
    r('reed-right-hind-ankle','reed-shadow',12,20,2)
    # Four separated toe clusters, not two merged boot-like horizontal bars.
    for y in (21,22): c[y]=[None]*16
    r('reed-left-hind-ankle','reed-shadow',2,20,1,2)
    r('reed-right-hind-ankle','reed-shadow',13,20,1,2)
    r('reed-left-front-arm','reed',5,19,1,3)
    r('reed-right-front-arm','reed-shadow',10,19,1,3)
    r('reed-left-hind-toes','reed-light',1,22,2)
    r('reed-right-hind-toes','reed',13,22,2)
    r('reed-left-front-toes','throat',4,22,3)
    r('reed-right-front-toes','reed-light',9,22,3)
    frames.append(c)
    c,r=new()
    # Lower, oblique black frog with three scarlet transverse back bands.
    # Banding is anatomy, not a damage flash, poison cloud or attack animation.
    for y,x,w in [(12,9,2),(13,4,7),(13,12,2),(14,3,12),(15,2,13),
                  (16,1,14),(17,1,13),(18,2,12),(19,2,11),(20,3,10),
                  (21,2,4),(21,10,4),(22,1,4),(22,11,4)]:
        r('band-body-outline','ink',x,y,w)
    r('band-long-back','coal',4,14,8,5)
    r('band-back-plane','coal-light',5,14,5)
    r('band-low-muzzle','coal',11,14,3,3)
    r('band-left-haunch','coal-light',2,17,4,3)
    r('band-haunch-fold','ink',3,18,3)
    r('band-haunch-fold','ink',5,19)
    r('band-rear-crossband','scarlet',4,14,2)
    r('band-rear-crossband','scarlet',3,15,2,2)
    r('band-middle-crossband','scarlet',8,14,2)
    r('band-middle-crossband','scarlet',7,15,2,2)
    r('band-front-crossband','scarlet',11,15,2,2)
    r('band-rear-band-light','scarlet-light',4,14)
    r('band-middle-band-light','scarlet-light',8,14)
    r('band-fold-band','scarlet',2,17,2)
    r('band-fold-band','scarlet',2,18)
    r('band-far-eye-rim','eye',9,12,2,2)
    r('band-far-pupil','ink',10,13)
    r('band-near-eye-rim','eye',12,13,2,2)
    r('band-near-pupil','ink',13,14)
    r('band-closed-mouth','ink',12,16,3)
    r('band-low-throat','coal-light',10,17,3)
    r('band-belly','coal',6,19,5,2)
    r('band-hind-ankle','coal',3,20,2,2)
    r('band-near-foreleg','coal-light',11,18,1,4)
    r('band-foreleg-band','scarlet',11,20)
    for y in (21,22): c[y]=[None]*16
    r('band-near-hind-ankle','coal-light',2,19,1,3)
    r('band-far-hind-ankle','coal',5,20,1,2)
    r('band-near-front-ankle','coal-light',13,19,1,3)
    r('band-far-front-ankle','coal',10,20,1,2)
    r('band-near-hind-toes','scarlet',1,22,2)
    r('band-far-hind-toes','coal-light',4,22,3)
    r('band-far-front-toes','coal-light',9,22,3)
    r('band-near-front-toes','scarlet',13,22,2)
    r('band-hind-toe-light','scarlet-light',1,22)
    frames.append(c)
    return frames

def snapshot():
    assert not (OUT/'protected-inputs.json').exists(), 'Never silently repin protected art'
    paths=[GIN,Path(str(GIN)+'.meta'),OUT/'scene-reference-01.png',OUT/'scene-prompt-01.txt',
           ROOT/'Docs/Verification/Live16x24/sodden-frogs-before-01-open-mire.png']
    paths += [ROOT/'Tools/Live16x24'/name for name in
              ('build_opening_base.py','build_cindercaller.py','build_north_plot.py','build_limestone_caves.py')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in paths})
    from PIL import Image
    with Image.open(GIN) as im:
        im=im.convert('RGBA'); assert im.size==(16,16)
        palette={}; c=[[None]*16 for _ in range(24)]
        for y in range(16):
            for x in range(16):
                v=im.getpixel((x,y)); assert v[3] in (0,255)
                if v[3]:
                    color=bytes(v[:3]).hex().upper(); palette[color]=color
                    c[y+4][x]=['unchanged-gin-pixel',color]
    save('approved-gin-comparison.json',{'source':str(GIN.relative_to(ROOT)),
        'sha256':sha(GIN),'palette':palette,'cells':c,'placement':'Unscaled16x16 centered in16x24; four transparent rows above/below.'})
    assert not (OUT/'sodden-frogs.png').exists()
    save('source-red.json',{'expected_failure':'Native atlas absent before original authoring build',
         'runtime_tests':'Separate root-owned RED gate; this source absence is not a Unity test.'})

def pixel(v,palette=P): return tuple(bytes.fromhex(palette[v[1]]))+(255,) if v else (0,0,0,0)
def validate(frames,size,data):
    assert frames==cells() and size==(32,24) and len(data)==3072
    stats=[]
    for f,c in enumerate(frames):
        points=set()
        for y,row in enumerate(c):
            for x,v in enumerate(row):
                at=4*(y*32+f*16+x); assert tuple(data[at:at+4])==pixel(v),(f,x,y)
                if v: points.add((x,y))
        assert min(x for x,y in points)==1 and max(x for x,y in points)==14
        assert min(y for x,y in points)==12 and max(y for x,y in points)==22
        assert all(c[23][x] is None for x in range(16))
        seen={next(iter(points))}; todo=list(seen)
        while todo:
            x,y=todo.pop()
            for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if q in points and q not in seen: seen.add(q); todo.append(q)
        assert seen==points, 'Detached frog anatomy'
        assert sum(v is not None and v[1]=='eye' for row in c for v in row)>=6
        assert c[22][7] is None and c[22][8] is None, 'Merged foot platform'
        stats.append({'owner':NAMES[f],'opaque_pixels':len(points),'bounds':[1,12,15,23]})
    assert not any(v and v[1].startswith('scarlet') for row in frames[0] for v in row)
    assert sum(v is not None and v[1].startswith('scarlet') for row in frames[1] for v in row)>=20
    return stats

def verify():
    from PIL import Image
    guard(); s=json.loads((OUT/'source-cells.json').read_text()); assert s['palette']==P
    with Image.open(OUT/'sodden-frogs.png') as im:
        assert im.mode=='RGBA'; data=im.tobytes(); stats=validate(s['frames'],im.size,data)
    for f,name in enumerate(('reedfrog.png','bandfrog.png')):
        with Image.open(OUT/name) as im:
            assert im.mode=='RGBA' and im.size==(16,24)
            for y,row in enumerate(s['frames'][f]):
                for x,v in enumerate(row): assert im.getpixel((x,y))==pixel(v)
    gin=json.loads((OUT/'approved-gin-comparison.json').read_text())
    checks=0
    for name,scale in [('contact-native.png',1),('contact.png',8)]:
        with Image.open(OUT/name) as im:
            assert im.size==(104*scale,48*scale)
            for f,x0 in enumerate((8,44,80)):
                c,palette=(s['frames'][f],P) if f<2 else (gin['cells'],gin['palette'])
                for y,row in enumerate(c):
                    for x,v in enumerate(row):
                        expected=pixel(v,palette) if v else tuple(bytes.fromhex(BACK))+(255,)
                        for dy in range(scale):
                            for dx in range(scale):
                                assert im.getpixel(((x0+x)*scale+dx,(8+y)*scale+dy))==expected
                                checks+=1
    rejected=[]
    for label,f,x,y,channel,value in [('missing-eye',0,4,12,3,0),('wrong-band',1,7,15,0,255),('partial-alpha',0,5,17,3,127),('opaque-padding',1,0,0,3,255)]:
        at=4*(y*32+f*16+x)+channel; bad=bytearray(data); assert bad[at]!=value; bad[at]=value
        try: validate(s['frames'],(32,24),bad)
        except AssertionError: rejected.append(label)
        else: raise AssertionError('Corruption accepted: '+label)
    save('source-verification.json',{'passed':True,'atlas_pixels':768,'contact_pixels':checks,
        'frames':stats,'rejected_corruptions':rejected,'atlas_sha256':sha(OUT/'sodden-frogs.png'),
        'source_sha256':sha(OUT/'source-cells.json'),'blend_sha256':sha(OUT/'editable.blend'),
        'protected_inputs_unchanged':len(guard()),'runtime_or_gameplay_acceptance':False})
    print(json.dumps(stats))

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard(); frames=cells(); bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene; native.name='Sodden frogs exact native32x24'; cols=[]
    for f,c in enumerate(frames):
        col=model(native,NAMES[f]+' editable native cells',c,P); native.collection.children.unlink(col)
        cols.append(col); instance(native,col,NAMES[f]+' atlas',f*16,0)
    camera_for(native,32,24); native.render.filepath=str(OUT/'sodden-frogs.png')
    bpy.ops.render.render(write_still=True,scene=native.name); canonicalize_timing(OUT/'sodden-frogs.png')
    for f,name in enumerate(('reedfrog.png','bandfrog.png')):
        single=bpy.data.scenes.new(NAMES[f]+' exact native16x24')
        instance(single,cols[f],NAMES[f]+' separate native export',0,0); camera_for(single,16,24)
        single.render.filepath=str(OUT/name); bpy.ops.render.render(write_still=True,scene=single.name); canonicalize_timing(OUT/name)
    gin=json.loads((OUT/'approved-gin-comparison.json').read_text())
    col=model(native,'Unchanged approved GinFrog comparison',gin['cells'],gin['palette'])
    native.collection.children.unlink(col); cols.append(col)
    for scale,name in [(1,'contact-native.png'),(8,'contact.png')]:
        scene=bpy.data.scenes.new('Frog geometry study '+str(scale)+'x'); camera_for(scene,104,48,scale)
        plane(scene,'Study grass backing',BACK,0,0,104,48,-2)
        for f,x in enumerate((8,44,80)): instance(scene,cols[f],str(f),x,16)
        label=emission_material('Study text','B5A47E')
        add_label(scene,'NATIVE FROGS / ORIGINAL TWO-CELL STUDY',52,44,label,1.4)
        for x,n in [(16,'REEDFROG'),(52,'BANDFROG'),(88,'GIN / UNCHANGED')]: add_label(scene,n,x,8,label,1.0)
        scene.render.filepath=str(OUT/name); bpy.ops.render.render(write_still=True,scene=scene.name); canonicalize_timing(OUT/name)
    save('source-cells.json',{'schema':1,'coordinates':'Top-left, y down; null transparent; named unit quads atz0',
        'palette':P,'frames':frames,'authorship':'Original hand-authored native integer cells; concept not raster-traced or downsampled.'})
    save('palette.json',P)
    save('manifest.json',{'atlas':'sodden-frogs.png','dimensions':[32,24],
        'frames':[{'index':f,'owner':n,'rect':[f*16,0,16,24],'occupied_bounds':[1,12,15,23],'baseline_y':22} for f,n in enumerate(NAMES)],
        'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','compression':'None','mipmaps':False,
        'separate_exports':['reedfrog.png','bandfrog.png'],
        'source':'source-cells.json','blend':'editable.blend','contacts':['contact-native.png','contact.png'],
        'scope':'Two neutral living bodies only. Existing GinFrog unchanged; source-only, root owns runtime publication and gameplay acceptance.'})
    bpy.context.window.scene=native; bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend')); guard()

def inspect_blend():
    import bpy
    guard(); faces=0
    for f,c in enumerate(cells()):
        col=bpy.data.collections[NAMES[f]+' editable native cells']; recovered={}
        for obj in col.objects:
            assert obj.type=='MESH' and tuple(obj.scale)==(1,1,1) and tuple(obj.location)==(0,0,0) and tuple(obj.rotation_euler)==(0,0,0)
            color=obj['paletteHex']; part=re.sub(r'\.\d+$','',obj.name)
            assert len(obj.data.materials)==1
            emission=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION']
            assert len(emission)==1 and emission[0].inputs['Strength'].default_value==1
            def linear(v):
                s=v/255; return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
            actual=emission[0].inputs['Color'].default_value
            assert all(abs(actual[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
            for face in obj.data.polygons:
                v=[obj.data.vertices[i].co for i in face.vertices]
                assert len(v)==4 and all(p.z==0 and p.x==int(p.x) and p.y==int(p.y) for p in v)
                x=min(p.x for p in v); y=min(p.y for p in v)
                assert max(p.x for p in v)-x==max(p.y for p in v)-y==1
                q=(int(x),23-int(y)); assert q not in recovered; recovered[q]=(part,color); faces+=1
        assert recovered=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(c) for x,v in enumerate(row) if v}
    assert len(bpy.data.actions)==0, 'No optional animation in this packet'
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,
        'unit_quads':faces,'exact_named_parts_palette_coordinates':True,'blend_sha256':sha(OUT/'editable.blend'),
        'bounds':'Geometry validation only; source PNGs verified separately; no gameplay claim.'})
    print('Reopened editable native geometry verified:',faces,'unit quads')

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('snapshot','build','verify','inspect-blend'): parser.add_argument('--'+name,action='store_true')
    opt=parser.parse_args(args); assert sum(vars(opt).values())==1
    if opt.snapshot: snapshot()
    elif opt.build: build()
    elif opt.verify: verify()
    else: inspect_blend()
