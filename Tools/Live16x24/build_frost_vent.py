"""Original native pixel quads; concept informs silhouette, never supplies pixels.
Blender --background --threads 4 --python this-file -- --build
Blender editable.blend --background --python this-file -- --inspect
python3 this-file --verify (read-only verification).
"""
import hashlib, json, shutil, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/frost-vent'
RUNTIME = ROOT / 'Assets/Resources/Sprites/Live16x24/frost-vent.png'
P = {'mouth': '343F42', 'lip': '596267', 'rime': '92A9AE',
     'light': 'B2C6C8', 'shade': '4A5355', 'wisp': '7D989E'}
def frames():
    result=[]
    for index in range(3):
        c=[[None]*16 for _ in range(24)]
        def dot(x,y,part,color): c[y][x]=[part,color]
        if index<2:
            # Flat ground fissure: shallow lower-right contact edge, broken pale rim.
            left=3+index; right=12-index; yy=14+index
            for x in range(left,right+1):
                dot(x,yy+3,'contact-edge','shade')
                dot(x,yy+2,'lower-rim','lip')
                dot(x,yy,'upper-rim','rime')
                dot(x,yy+1,'open-fissure','mouth')
            for x in range(left+2,right):dot(x,yy+2,'open-fissure','mouth')
            for x in [left,left+1,right-2]:dot(x,yy,'upper-left-highlight','light')
            dot(left-1,yy+1,'broken-lip','lip');dot(right+1,yy+2,'broken-lip','lip')
            for x,y in [(6+index,11),(7+index,10),(7+index,8),(8+index,7)]:dot(x,y,'short-cold-breath','wisp')
            for x,y in [(2,yy-1),(13,yy+4),(4,yy+4)]:dot(x,y,'sparse-rime','rime')
        else:
            # Independent cold receiver, no dark opening or invented source.
            for x,y in [(2,17),(3,17),(4,18),(8,20),(9,20),(10,19),(12,16),(13,16)]:dot(x,y,'cold-floor-rime','wisp')
            for x,y in [(3,16),(9,19),(12,17)]:dot(x,y,'cold-floor-glint','rime')
        result.append({'index':index,'id':('vent-'+str(index)) if index<2 else 'cold-receiver','cells':c})
    return result
def save(name,value): (OUT/name).write_text(json.dumps(value,indent=2)+'\n')
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_east_kitchen import write_meta
    from build_limestone_caves import canonicalize_timing
    OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.name='Frost vent native48x24'
    for f in frames():
        col=model(scene,f['id'],f['cells'],P);scene.collection.children.unlink(col)
        instance(scene,col,f['id'],f['index']*16,0)
    camera_for(scene,48,24);scene.render.filepath=str(OUT/'frost-vent.png')
    bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'frost-vent.png')
    save('source-cells.json',{'palette':P,'frames':frames()})
    save('manifest.json',{'frames':3,'native_cell':[16,24],'size':[48,24],
        'scope':'Ginmere disclosed FrostVent source and independent cold-only sandstone receiver',
        'alpha':'Transparent outside original fissure/rime pixels','animation':False,
        'filter':'Point','compression':'None','mipmaps':False,'source':'editable.blend'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'))
    shutil.copyfile(OUT/'frost-vent.png',RUNTIME);write_meta(Path(str(RUNTIME)+'.meta'))
def inspect():
    import bpy
    counts={}
    for f in frames():
        count=sum(len(o.data.polygons) for o in bpy.data.collections[f['id']].objects)
        assert count==sum(c is not None for row in f['cells'] for c in row)
        counts[f['id']]=count
    save('reopened-blend.json',{'passed':True,'editable_quads':counts,'actual_play':False})
def verify():
    from PIL import Image
    assert json.loads((OUT/'source-cells.json').read_text())=={'palette':P,'frames':frames()}
    image=Image.open(OUT/'frost-vent.png').convert('RGBA');assert image.size==(48,24)
    for f in frames():
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                expected=tuple(bytes.fromhex(P[c[1]]))+(255,) if c else (0,0,0,0)
                assert image.getpixel((f['index']*16+x,y))==expected,(f['index'],x,y)
    assert sha(RUNTIME)==sha(OUT/'frost-vent.png')
    save('verification.json',{'passed':True,'exact_native_pixels':1152,'png_sha256':sha(RUNTIME),
        'blend_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS: three original 16x24 frames, 1152 exact pixels; runtime/source identical')
if __name__=='__main__':
    if '--build' in sys.argv: build()
    elif '--inspect' in sys.argv: inspect()
    else: verify()
