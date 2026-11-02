"""Original coherent scraped-ground motif as editable native semantic quads.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify; Blender editable.blend --background --python this-file -- --inspect
Pillow is used for read-only pixel verification, never asset generation.
"""
import hashlib,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/overwrit-wear'
BASE=json.loads((ROOT/'ArtSource/Live16x24/overwrit-environment/source-cells.json').read_text())
P=dict(BASE['palette'])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for p,h in pins.items():assert sha(ROOT/p)==h,p
    return len(pins)
def motif():
    c=[[['smooth-ground','ground'] for x in range(128)] for y in range(96)]
    # Authored asymmetric rubbed footprint; open broken edges, no raised rim.
    bands=[(15,19,37,87),(20,23,24,103),(24,29,17,112),(30,35,10,119),
           (36,41,6,115),(42,46,13,111),(47,51,20,104),(52,57,27,94),
           (58,61,36,82),(62,66,47,71)]
    for lo,hi,left,right in bands:
        for y in range(lo,hi+1):
            stagger=(y%3)*2
            for x in range(left+stagger,right-(y%4)):
                # Open gaps soften the perimeter without smooth alpha/gradient.
                if (x<left+9 or x>right-10) and (x+2*y)%7<3:continue
                c[y][x]=['broad-erased-patch','env-wear']
    strokes=[(21,39,28),(25,28,53),(31,21,18),(35,60,36),(42,18,43),
             (47,35,29),(52,50,31),(59,47,19),(11,59,16),(72,28,25),(78,70,17)]
    for y,x,w in strokes:
        for xx in range(x,x+w):
            if xx%13 not in (0,1):c[y][xx]=['broken-shallow-scrape','scuff']
    for y,x,w in [(27,66,29),(38,36,21),(49,71,20),(55,31,17)]:
        for xx in range(x,x+w):c[y][xx]=['quiet-polished-residue','env-polish']
    return c
def frames():
    c=motif();result=[]
    for row in range(4):
        for col in range(8):
            i=row*8+col
            result.append({'index':i,'id':'scraped-wear-'+str(i),
                           'cells':[r[col*16:(col+1)*16] for r in c[row*24:(row+1)*24]]})
    return result
def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.name='Overwrit coherent wear native512x24'
    for i,f in enumerate(frames()):
        col=model(scene,f['id'],f['cells'],P);scene.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(scene,col,str(i),i*16,0)
    camera_for(scene,512,24);scene.render.filepath=str(OUT/'overwrit-wear.png')
    bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'overwrit-wear.png')
    save('source-cells.json',{'palette':P,'frames':frames()})
    save('manifest.json',{'frames':32,'size':[512,24],'motif_cells':[8,4],'native_cell':[16,24],
      'scope':'Optional current ordinary4.12 and0.10 only; unchanged owner/geometry; canonical reader stays approved',
      'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','source':'editable.blend'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()
def verify():
    from PIL import Image
    guard();s=json.loads((OUT/'source-cells.json').read_text());assert s=={'palette':P,'frames':frames()}
    with Image.open(OUT/'overwrit-wear.png') as im:
        assert im.size==(512,24);pix=im.convert('RGBA').load()
        for i,f in enumerate(frames()):
            for y,row in enumerate(f['cells']):
                for x,c in enumerate(row):assert pix[i*16+x,y]==tuple(bytes.fromhex(P[c[1]]))+(255,),(i,x,y)
    c=motif();assert all(v[1]=='ground' for y,row in enumerate(c) for x,v in enumerate(row) if x in(0,127) or y in(0,95))
    save('verification.json',{'passed':True,'exact_native_pixels':12288,'protected_inputs':guard(),'palette_unchanged':True,'png_sha256':sha(OUT/'overwrit-wear.png'),'blend_sha256':sha(OUT/'editable.blend'),'actual_play':False})
    print('PASS:12288 exact opaque native pixels; palette and protected inputs unchanged')
def inspect():
    import bpy
    guard()
    for f in frames():assert sum(len(o.data.polygons) for o in bpy.data.collections[f['id']].objects)==384
    save('reopened-blend.json',{'passed':True,'editable_quads':12288,'semantic_frames':32})
if __name__=='__main__':
    if '--build' in sys.argv:build()
    elif '--inspect' in sys.argv:inspect()
    else:verify()
