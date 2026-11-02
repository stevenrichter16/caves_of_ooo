"""Original editable south slope geometry, preserving the approved Beating atlas.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify; Blender --background editable.blend --python this-file -- --inspect
"""
import copy, hashlib, json, sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/dune-relief'
BASE=json.loads((ROOT/'ArtSource/Live16x24/beating-environment/source-cells.json').read_text())
P=dict(BASE['palette']);P.update({'slope-lip':'A88A58','slope-upper':'9A7041','slope-middle':'805A32','slope-lower':'604025','slope-foot':'604025'})
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for p,h in pins.items():assert sha(ROOT/p)==h,p
    return len(pins)
def frames():
    result=[]
    for v in range(4):
        f=copy.deepcopy(BASE['frames'][8+v]);f.update(index=v,id='dune-south-slope-'+str(v))
        c=f['cells']
        # Broad shallow sand face. No isolated bevel or stripe on interior tiles.
        for y,k in [(14,'slope-upper'),(15,'slope-upper'),(16,'slope-upper'),(17,'slope-middle'),(18,'slope-middle'),(19,'slope-middle'),(20,'slope-middle'),(21,'slope-middle'),(22,'slope-middle'),(23,'slope-foot')]:
            for x in range(16):c[y][x]=['exposed-south-'+k,k]
        for x in ((),(9,10,11),(2,3),(7,8))[v]:c[14][x]=['soft-crest-lip','slope-lip']
        for x,y in ((),((6,19),(13,22)),((2,20),(8,18)),((5,22),(12,19)))[v]:c[y][x]=['slip-grain','slope-upper']
        result.append(f)
    return result
def save(n,x):(OUT/n).write_text(json.dumps(x,indent=2)+'\n')
def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Dune shallow south relief native64x24'
    for i,f in enumerate(frames()):
        col=model(native,f['id'],f['cells'],P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*16,0)
    camera_for(native,64,24);native.render.filepath=str(OUT/'dune-relief.png')
    bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'dune-relief.png')
    save('source-cells.json',{'palette':P,'frames':frames()})
    save('manifest.json',{'frames':4,'size':[64,24],'native_cell':[16,24],'south_face_rows':[14,23],'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','source':'editable.blend','scope':'Optional19.14 disclosed dry Sand south boundary only; no geometry change'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()
def verify():
    from PIL import Image
    guard();s=json.loads((OUT/'source-cells.json').read_text());assert s=={'palette':P,'frames':frames()}
    with Image.open(OUT/'dune-relief.png') as im:
        assert im.size==(64,24);pix=im.convert('RGBA').load()
        for i,f in enumerate(frames()):
            for y,row in enumerate(f['cells']):
                for x,c in enumerate(row):assert pix[i*16+x,y]==tuple(bytes.fromhex(P[c[1]]))+(255,),(i,x,y)
        for i,f in enumerate(frames()):assert f['cells'][:14]==BASE['frames'][8+i]['cells'][:14]
    save('verification.json',{'passed':True,'exact_native_pixels':1536,'protected_inputs':guard(),'unchanged_top_rows':14,'png_sha256':sha(OUT/'dune-relief.png'),'blend_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS:1536 exact opaque pixels; approved inputs unchanged;14 top rows unchanged')
def inspect():
    import bpy
    guard();groups=[]
    for f in frames():
        col=bpy.data.collections[f['id']];assert len(col.objects)>0
        assert sum(len(o.data.polygons) for o in col.objects)==384
        groups.append(f['id'])
    save('reopened-blend.json',{'passed':True,'semantic_groups':groups,'editable_quads':1536})
if __name__=='__main__':
    if '--build' in sys.argv:build()
    elif '--inspect' in sys.argv:inspect()
    else:verify()
