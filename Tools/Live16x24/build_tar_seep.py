"""Original native TarSeep unit-cell geometry; concepts are reference, never raster input.
python3 this-file --snapshot | --verify
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --threads 4 --python this-file -- --inspect-blend
Pillow only decodes/checks Blender output. Runtime publication is an explicit byte copy.
"""
import argparse, hashlib, json, re, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/tar-seep'
P = {'edge':'2D232A', 'tar':'3F303A', 'film':'493844',
     'wet-shadow':'514553', 'wet':'655F6C', 'slit':'7A737F'}
BACKS = ('806A4C', '032F23')
PREFIX = 'TarSeep original native frame '
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,value): (OUT/name).write_text(json.dumps(value,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for p,d in pins.items(): assert sha(ROOT/p)==d,p
    return pins

def cells(empty=False):
    c=[[None]*16 for _ in range(24)]
    def r(part,color,x,y,w=1):
        assert color in P and 0<=x<x+w<=16 and 0<=y<24
        for xx in range(x,x+w): c[y][xx]=[part,color]
    # One contiguous, flat ground patch, wide in the middle and ragged at its
    # edges so the real sand reads around it. No circular
    # rim, no terminator edge and no specular dome (review 2026-10-29: the first
    # draft read as a convex boulder, which implies collision on open sand).
    # A low, wide film lying on the ground band: 9 rows tall, 1-2 px clear of
    # both side edges so neighbouring seeps stay separate, broader at the base.
    # Review 2026-10-29: a near-round 13-row mass read as a boulder like the
    # approved Rock frames; paired notches read as hearts in actual play.
    outline=[(11,4,7),(12,2,11),(13,1,13),(14,1,14),(15,1,14),(16,1,13),
             (17,2,12),(18,3,9),(19,5,5)]
    for y,x,w in outline:r('continuous-tar','tar',x,y,w)
    # Flat marbling inside the tar: slightly lighter skins, never a lip or halo.
    for y,x,w in [(13,9,3),(14,10,3),(16,3,3),(17,4,2)]:
        r('surface-film','film',x,y,w)
    # Dim horizontal sheen dashes (the Beating brine's ripple vocabulary); no
    # bright highlight, which reads as a convex top.
    r('wet-reflection','wet',3,13,4)
    r('reflection-shadow','wet-shadow',8,15,4)
    r('wet-reflection','wet',9,15,2)
    r('wet-reflection','wet',8,17,3)
    # Zero stock: a slightly duller, skinned surface; the projected oil stays.
    if empty:
        r('residual-film','film',3,13,4)
        r('residual-film','film',4,14,2)
    return c

def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Do not silently repin accepted inputs'
    files=[OUT/'concepts'/n for n in ('scene-and-parts-01.png','critique.md','prompt.txt')]
    files += [ROOT/'Docs/Verification/Live16x24'/n for n in ('packaged-regional-batch-10-beating-arrival.png','packaged-regional-batch-11-tar-seep-reader.png')]
    files += [ROOT/'Assets/Resources/Sprites/Live16x24'/n for n in ('beating-materials.png','grovelands-materials.png','sodden-ground.png')]
    files += [ROOT/'Assets/Resources/Sprites/Environment/tar_seep.png']
    files += [ROOT/'Tools/Live16x24'/n for n in ('build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py','build_east_kitchen.py')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in files})

def rgba(v): return tuple(bytes.fromhex(P[v[1]]))+(255,) if v else (0,0,0,0)
def verify():
    from PIL import Image
    guard();data=json.loads((OUT/'source-cells.json').read_text());frames=[cells(),cells(True)]
    assert data['frames']==frames and data['palette']==P
    masks=[];changed=0
    with Image.open(OUT/'tar-seep.png') as im:
        assert im.mode=='RGBA' and im.size==(32,24)
        for i,c in enumerate(frames):
            mask={(x,y) for y,row in enumerate(c) for x,v in enumerate(row) if v};masks.append(mask)
            for y,row in enumerate(c):
                for x,v in enumerate(row):assert im.getpixel((i*16+x,y))==rgba(v),(i,x,y)
            seen={next(iter(mask))};todo=list(seen)
            while todo:
                x,y=todo.pop()
                for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                    if q in mask and q not in seen:seen.add(q);todo.append(q)
            assert seen==mask and min(y for x,y in mask)==11 and max(y for x,y in mask)==19
            assert sum(v is not None and v[1] in ('wet','slit') for row in c for v in row)>=3
        assert masks[0]==masks[1]
        changed=sum(rgba(frames[0][y][x])!=rgba(frames[1][y][x]) for y in range(24) for x in range(16))
        assert 0<changed<=12
    for name,scale in (('contact-native.png',1),('contact.png',8)):
        with Image.open(OUT/name) as im:
            assert im.mode=='RGBA' and im.size==(64*scale,64*scale)
            for row,back in enumerate(BACKS):
                for i,c in enumerate(frames):
                    for y,line in enumerate(c):
                        for x,v in enumerate(line):
                            expected=rgba(v) if v else tuple(bytes.fromhex(back))+(255,)
                            # Sample each center of a direct geometry-rendered block.
                            assert im.getpixel(((8+i*32+x)*scale+scale//2,(8+row*32+y)*scale+scale//2))==expected
    save('source-verification.json',{'passed':True,'native_dimensions':[32,24],'frames':2,'native_pixel_checks':768,
        'alpha':'binary','same_alpha_mask':True,'opaque_pixels_per_frame':len(masks[0]),'changed_pixels_between_states':changed,
        'occupied_bounds':[1,11,15,20],'both_states_have_wet_reflection':True,'protected_inputs_unchanged':len(guard()),
        'png_sha256':sha(OUT/'tar-seep.png'),'source_sha256':sha(OUT/'source-cells.json'),
        'blend_sha256':sha(OUT/'editable.blend'),'tool_sha256':sha(Path(__file__)),
        'actual_gameplay_or_import_acceptance':False})
    print('Verified two same-footprint wet states:',len(masks[0]),'opaque pixels;',changed,'changed pixels')

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='TarSeep exact native32x24';collections=[]
    frames=[cells(),cells(True)]
    for i,c in enumerate(frames):
        col=model(native,PREFIX+str(i),c,P);native.collection.children.unlink(col);collections.append(col)
        instance(native,col,'Native state '+str(i),i*16,0)
    camera_for(native,32,24);native.render.filepath=str(OUT/'tar-seep.png')
    bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'tar-seep.png')
    for scale,name in ((1,'contact-native.png'),(8,'contact.png')):
        scene=bpy.data.scenes.new('TarSeep direct source study '+str(scale)+'x');camera_for(scene,64,64,scale)
        label=emission_material('Tar source labels '+str(scale),'C5B28A')
        for row,back in enumerate(BACKS):
            plane(scene,'Source backing '+back,back,0,32-row*32,64,32,-2)
            for i,col in enumerate(collections):
                instance(scene,col,'State '+str(i)+' study '+str(row),8+i*32,32-row*32)
                add_label(scene,('WET >0' if i==0 else 'WET 0'),16+i*32,59-row*32,label,2)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',{'coordinates':'Top-left y down; null transparent; named flat unit-quads at z0',
        'palette':P,'frames':frames,'authorship':'Original hand-authored integer geometry; no concept pixels sampled, traced, copied, resized or painted.'})
    save('manifest.json',{'owner':'TarSeep','atlas':'tar-seep.png','dimensions':[32,24],
        'frames':[{'index':0,'rect':[0,0,16,24],'state':'oil Volume>0; persistently oily'},
                  {'index':1,'rect':[16,0,16,24],'state':'oil Volume0; persistently oily, never dry/safe'}],
        'palette':P,'pixelsPerUnit':16,'pivot':[.5,.5],'import':'Point, Clamp, no mipmaps, uncompressed, FullRect, no fallback physics',
        'alpha':'Binary; no baked sand, halo, shadow, flame or ground geometry',
        'editable':'editable.blend','source':'source-cells.json','generator':'Tools/Live16x24/build_tar_seep.py',
        'scope':'Original source art only; Unity import/runtime/native tests/ordinary gameplay acceptance remain root gates.'})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect_blend():
    import bpy
    guard();total=0
    for i in range(2):
        col=bpy.data.collections[PREFIX+str(i)];recovered={}
        for obj in col.objects:
            assert obj.type=='MESH' and tuple(obj.scale)==(1,1,1) and tuple(obj.location)==(0,0,0)
            assert tuple(obj.rotation_euler)==(0,0,0) and len(obj.data.materials)==1
            part=re.sub(r'\.\d+$','',obj.name);color=obj['paletteHex']
            nodes=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(nodes)==1
            assert nodes[0].inputs['Strength'].default_value==1
            for face in obj.data.polygons:
                v=[obj.data.vertices[j].co for j in face.vertices];assert len(v)==4 and all(p.z==0 and p.x==int(p.x) and p.y==int(p.y) for p in v)
                x=min(p.x for p in v);y=min(p.y for p in v);assert max(p.x for p in v)-x==max(p.y for p in v)-y==1
                q=(int(x),23-int(y));assert q not in recovered;recovered[q]=(part,color);total+=1
        assert recovered=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(cells(i==1)) for x,v in enumerate(row) if v}
    assert len(bpy.data.actions)==0
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'exact_named_palette_cells':True,
        'unit_quads':total,'frames':2,'animation_actions':0,'blend_sha256':sha(OUT/'editable.blend')})
    print('Reopened two original geometry frames:',total,'unit quads')

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    a=p.parse_args(args);assert sum(vars(a).values())==1
    if a.snapshot:snapshot()
    elif a.build:build()
    elif a.verify:verify()
    else:inspect_blend()
