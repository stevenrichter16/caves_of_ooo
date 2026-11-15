"""Reused Quillhold marble art as editable semantic native quads.
Author/export via prop-icons/make_prop_icons.py --quillhold-wall-only.
Blender --background --threads 4 --python this-file -- --build
Blender --background editable.blend --threads 4 --python this-file -- --inspect
python3 -B this-file verifies native/source/atlas parity and exact other pixels.
"""
import hashlib,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'ArtSource/Live16x24/quillhold-marble'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,x):(OUT/n).write_text(json.dumps(x,indent=2)+'\n')
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
    sc=bpy.context.scene;sc.name='Quillhold reused marble native16x24'
    for i,f in enumerate(data['frames']):
        col=model(sc,f['id'],f['cells'],data['palette']);sc.collection.children.unlink(col)
        instance(sc,col,str(i),i*16,0)
    camera_for(sc,16,24);sc.render.filepath=str(OUT/'marble.png')
    bpy.ops.render.render(write_still=True,scene=sc.name);canonicalize_timing(OUT/'marble.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()
def inspect():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_limestone_caves import canonicalize_timing
    guard();data=json.loads((OUT/'source-cells.json').read_text());groups=[]
    for f in data['frames']:
        col=bpy.data.collections[f['id']]
        assert sum(len(o.data.polygons) for o in col.objects)==384
        groups.extend(o.name for o in col.objects)
    bpy.context.scene.render.filepath=str(OUT/'reopened.png')
    bpy.ops.render.render(write_still=True);canonicalize_timing(OUT/'reopened.png')
    save('reopened-blend.json',{'passed':True,'editable_quads':384,'semantic_objects':groups,'source_sha256':sha(OUT/'editable.blend')})
def verify():
    from PIL import Image
    guard();data=json.loads((OUT/'source-cells.json').read_text())
    old=Image.open(OUT/'before/prop-icons.png').convert('RGBA')
    new=Image.open(ROOT/'Assets/Resources/Sprites/Live16x24/prop-icons.png').convert('RGBA')
    native=Image.open(OUT/'marble.png').convert('RGBA');reopened=Image.open(OUT/'reopened.png').convert('RGBA')
    assert old.size==new.size and native.size==reopened.size==(16,24)
    assert native.tobytes()==reopened.tobytes()
    assert old.getchannel('A').tobytes()==new.getchannel('A').tobytes()
    allowed=set()
    for i,f in enumerate(data['frames']):
        assert f['atlas_frame']==122
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                pos=(160+x,168+y);allowed.add(pos)
                rgba=tuple(bytes.fromhex(data['palette'][c[1]]))+(255,) if c else (0,0,0,0)
                assert native.getpixel((i*16+x,y))==rgba
                assert new.getpixel(pos)==rgba
    changed={(x,y) for y in range(new.height) for x in range(new.width) if old.getpixel((x,y))!=new.getpixel((x,y))}
    assert changed <= allowed and len(changed)==370
    save('verification.json',{'passed':True,'frames':[122],'changed_material_pixels':370,'other_atlas_pixels_exact':new.width*new.height-len(allowed),'alpha_exact':True,'native_and_reopened_pixels_exact':384,'editable_quads':384,'runtime_sha256':sha(ROOT/'Assets/Resources/Sprites/Live16x24/prop-icons.png'),'editable_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS:370material pixels; all other pixels/alpha/index exact;384 saved-source render pixels exact')
if __name__=='__main__':
    if '--build' in sys.argv:build()
    elif '--inspect' in sys.argv:inspect()
    else:verify()
