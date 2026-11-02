"""Editable semantic native quads for the three Gantry plank frames.
Author rows/palette in prop-icons/make_prop_icons.py; export with --gantry-only.
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --python this-file -- --inspect
python3 this-file verifies every atlas pixel outside the three frames is preserved.
"""
import hashlib,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Live16x24/gantry-timber'
def save(n,j):(OUT/n).write_text(json.dumps(j,indent=2)+'\n')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guard():
    for p,h in json.loads((OUT/'protected-inputs.json').read_text()).items():assert sha(ROOT/p)==h,p

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard();data=json.loads((OUT/'source-cells.json').read_text())
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    sc=bpy.context.scene;sc.name='Gantry timber original native48x24'
    for i,f in enumerate(data['frames']):
        col=model(sc,f['id'],f['cells'],data['palette']);sc.collection.children.unlink(col)
        instance(sc,col,str(i),i*16,0)
    camera_for(sc,48,24);sc.render.filepath=str(OUT/'gantry-timber.png')
    bpy.ops.render.render(write_still=True,scene=sc.name);canonicalize_timing(OUT/'gantry-timber.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def inspect():
    import bpy
    data=json.loads((OUT/'source-cells.json').read_text());groups=[]
    for f in data['frames']:
        col=bpy.data.collections[f['id']];assert sum(len(o.data.polygons) for o in col.objects)==384;groups.append(f['id'])
    save('reopened-blend.json',{'passed':True,'semantic_groups':groups,'editable_quads':1152})

def verify():
    from PIL import Image
    guard();data=json.loads((OUT/'source-cells.json').read_text())
    old=Image.open(OUT/'approved-prop-icons.png').convert('RGBA');new=Image.open(ROOT/'Assets/Resources/Sprites/Live16x24/prop-icons.png').convert('RGBA');native=Image.open(OUT/'gantry-timber.png').convert('RGBA')
    assert new.size==old.size and native.size==(48,24)
    allowed={(x,y) for y in range(96,120) for x in range(64,112)}
    changed=0;unchanged=0
    for y in range(new.height):
        for x in range(new.width):
            a=old.getpixel((x,y));b=new.getpixel((x,y))
            if (x,y) not in allowed:assert a==b,(x,y);unchanged+=1
            elif a!=b:changed+=1
    for i,f in enumerate(data['frames']):
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                color=tuple(bytes.fromhex(data['palette'][c[1]]))+(255,)
                assert native.getpixel((i*16+x,y))==color
                assert new.getpixel((64+i*16+x,96+y))==color
    save('verification.json',{'passed':True,'changed_pixels':changed,'protected_pixels':unchanged,'native_pixels':1152,'atlas_size':list(new.size),'frames':[68,69,70],'runtime_sha256':sha(ROOT/'Assets/Resources/Sprites/Live16x24/prop-icons.png'),'editable_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS',changed,'changed native wall pixels;',unchanged,'other atlas pixels exact;1152exact editable pixels')
if __name__=='__main__':
    if '--build' in sys.argv:build()
    elif '--inspect' in sys.argv:inspect()
    else:verify()
