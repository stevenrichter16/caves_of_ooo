"""Original native pixel quads; concept informs silhouette, never supplies pixels.
Blender --background --threads 4 --python this-file -- --build
Blender editable.blend --background --python this-file -- --inspect
python3 this-file --verify (read-only verification).
"""
import hashlib, json, shutil, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/mirror-mucilage'
RUNTIME = ROOT / 'Assets/Resources/Sprites/Live16x24/mirror-mucilage.png'
P = {'edge': '203A39', 'body': '2D4A49', 'shallow': '3C5B59',
     'rim': '597774', 'reflection': '799693', 'film': '31433D'}
# Irregular horizontal shore spans, top to bottom, starting at y=7.
SHAPES = [[(5,10),(3,12),(2,13),(1,13),(1,14),(1,14),(2,14),(2,13),(3,12),(5,11)],
          [(7,11),(4,13),(2,13),(2,14),(1,14),(1,13),(2,13),(3,12),(3,11),(5,9)]]
def frames():
    result=[]
    for index in range(4):
        cells=[[None]*16 for _ in range(24)]; spans=SHAPES[index%2]; dry=index>=2
        for row,(left,right) in enumerate(spans):
            y=row+7
            for x in range(left,right+1):
                edge=x in (left,right) or row in (0,len(spans)-1)
                color='edge' if edge else 'film' if dry else 'body'
                cells[y][x]=['viscous-film' if dry else 'liquid-body',color]
            if row in (1,2,4) and not dry: cells[y][left]=['shallow-upper-rim','rim']
        # Flat broken reflection strokes, no round bubbles/glow or raised basin.
        strokes=[(9,5,4,'shallow'),(10,6,3,'reflection'),(11,10,2,'shallow'),(14,4,5,'shallow')]
        if index%2: strokes=[(9,8,3,'rim'),(10,4,3,'reflection'),(12,8,4,'shallow'),(14,5,3,'shallow')]
        if dry: strokes=[(10,5,3,'shallow'),(13,8,2,'shallow')]
        for y,x,n,color in strokes:
            for xx in range(x,x+n):
                if cells[y][xx]: cells[y][xx]=['flat-reflection' if not dry else 'residual-film',color]
        result.append({'index':index,'id':('wet' if not dry else 'depleted-film')+str(index%2),'cells':cells})
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
    scene=bpy.context.scene;scene.name='Mirror mucilage native64x24'
    for f in frames():
        col=model(scene,f['id'],f['cells'],P);scene.collection.children.unlink(col)
        instance(scene,col,f['id'],f['index']*16,0)
    camera_for(scene,64,24);scene.render.filepath=str(OUT/'mirror-mucilage.png')
    bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'mirror-mucilage.png')
    save('source-cells.json',{'palette':P,'frames':frames()})
    save('manifest.json',{'frames':4,'native_cell':[16,24],'size':[64,24],
        'scope':'Visible canonical MirrorMucilagePool in ordinary Grovelands; wet stock or depleted residual film',
        'alpha':'Transparent outside original liquid silhouette','animation':False,
        'filter':'Point','compression':'None','mipmaps':False,'source':'editable.blend'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'))
    shutil.copyfile(OUT/'mirror-mucilage.png',RUNTIME);write_meta(Path(str(RUNTIME)+'.meta'))
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
    image=Image.open(OUT/'mirror-mucilage.png').convert('RGBA');assert image.size==(64,24)
    for f in frames():
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                expected=tuple(bytes.fromhex(P[c[1]]))+(255,) if c else (0,0,0,0)
                assert image.getpixel((f['index']*16+x,y))==expected,(f['index'],x,y)
    assert sha(RUNTIME)==sha(OUT/'mirror-mucilage.png')
    save('verification.json',{'passed':True,'exact_native_pixels':1536,'png_sha256':sha(RUNTIME),
        'blend_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS: four original 16x24 frames, 1536 exact pixels; runtime/source identical')
if __name__=='__main__':
    if '--build' in sys.argv: build()
    elif '--inspect' in sys.argv: inspect()
    else: verify()
