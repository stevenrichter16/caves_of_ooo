"""PeatBank face correction using original editable integer quads.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify [--old to check the preserved pre-correction source]
Only frames18–19 change; approved deck/mire/tree frames and importer stay exact.
"""
import copy, hashlib, json, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/sodden-travel'; BASE=OUT/'peat-before'
def read(p): return json.loads(p.read_text())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,d): (OUT/n).write_text(json.dumps(d,indent=2)+'\n')
def guard():
    for p,h in read(ROOT/'Docs/Verification/GraphicsExecution/PeatBankRelief/protected-inputs.json').items():
        assert sha(ROOT/p)==h,p

COLORS={'peat-cut-top':'514936','peat-upper-face':'564735','peat-lower-face':'443929',
        'peat-recess':'30291F','peat-moist-base':'29261F','peat-root-end':'695A3D',
        'peat-muted-sod':'455035','peat-compressed-layer':'3B3024'}
def face(variant):
    cells=[]
    for y in range(24):
        row=[]
        for x in range(16):
            # Full-width material bands have no per-cell frame or side bevel.
            if y<3: part,color='shallow-cut-top','peat-cut-top'
            elif y<5: part,color='overhanging-compressed-lip','peat-recess'
            elif y<11: part,color='broad-upper-peat-face','peat-upper-face'
            elif y<13: part,color='compressed-stratum','peat-compressed-layer'
            elif y<20: part,color='broad-lower-peat-face','peat-lower-face'
            else: part,color='moist-foot-contact','peat-moist-base'
            row.append([part,color])
        cells.append(row)
    # Sparse short fibers, unequal layer breaks; continuous edge colors retained.
    for x,y,w in (((2,1,5),(10,0,3)) if variant==0 else ((5,0,5),(1,1,2))):
        for xx in range(x,x+w): cells[y][xx]=['retained-sod-fiber','peat-muted-sod']
    for x,y,w in (((2,8,6),(9,16,5)) if variant==0 else ((7,9,6),(2,17,7))):
        for xx in range(x,x+w): cells[y][xx]=['short-compressed-fold','peat-compressed-layer']
    for x,y in (((4,5),(4,6),(5,7),(11,14),(10,15)) if variant==0 else ((10,5),(9,6),(9,7),(4,14),(5,15))):
        cells[y][x]=['embedded-vertical-root','peat-root-end']
    return cells

def data():
    d=read(BASE/'source-cells.json');d['palette'].update(COLORS)
    for i in (18,19): d['frames'][i]['cells']=face(i-18)
    return d

def contract(d):
    base=read(BASE/'source-cells.json');assert len(d['frames'])==28
    for k,v in base['palette'].items(): assert d['palette'][k]==v
    for i,f in enumerate(d['frames']):
        if i not in (18,19): assert f==base['frames'][i],('Protected frame',i)
        else:
            assert f['blueprint']=='PeatBank' and len(f['cells'])==24
            assert all(len(r)==16 and all(r) for r in f['cells']), 'Solid straight footprint required'
            parts={c[0] for r in f['cells'] for c in r}
            assert {'broad-upper-peat-face','broad-lower-peat-face','moist-foot-contact'}<=parts, 'Face still lacks coherent depth regions'
    for y in range(24):
        assert d['frames'][18]['cells'][y][0][1]==d['frames'][19]['cells'][y][15][1]
        assert d['frames'][19]['cells'][y][0][1]==d['frames'][18]['cells'][y][15][1]

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard();d=data();contract(d);bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.name='Sodden peat relief native448x24'
    for f in d['frames']:
        col=model(scene,f['id'],f['cells'],d['palette']);scene.collection.children.unlink(col);instance(scene,col,f['id'],f['index']*16,0)
    camera_for(scene,448,24);scene.render.filepath=str(OUT/'sodden-travel.png')
    bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'sodden-travel.png')
    save('source-cells.json',d);m=read(BASE/'manifest.json')
    m.update(generator='Tools/Live16x24/build_sodden_peat.py',protectedFrames=[i for i in range(28) if i not in (18,19)],
             sha256=sha(OUT/'sodden-travel.png'),scope='PeatBank18–19 layered face correction; all other current frames preserved. Actual play tracked separately.',
             previousSource='peat-before',acceptedNonMireFramesPreserved=list(range(16))+[20,21])
    for i in (18,19):m['frames'][i]['description']='Straight compressed peat bank with broad warm faces, recessed strata, sparse root fibers and a short moist base.'
    save('manifest.json',m);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sodden-travel.blend'));guard()

def verify(old=False):
    from PIL import Image
    guard();d=read((BASE if old else OUT)/'source-cells.json');contract(d)
    a=Image.open(BASE/'sodden-travel.png').convert('RGBA');b=Image.open(OUT/'sodden-travel.png').convert('RGBA')
    assert a.size==b.size==(448,24);changed=0
    for f in d['frames']:
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                p=(f['index']*16+x,y);want=tuple(bytes.fromhex(d['palette'][c[1]]))+(255,) if c else (0,0,0,0)
                assert b.getpixel(p)==want,('Native pixel drift',p)
                if f['index'] not in (18,19):assert a.getpixel(p)==b.getpixel(p),('Protected pixel',p)
                changed+=a.getpixel(p)!=b.getpixel(p)
    save('peat-verification.json',{'passed':True,'changed_pixels':changed,'protected_frames':26,'native_pixels':448*24,'runtime_sha256':sha(OUT/'sodden-travel.png'),'actual_play':False})
    print('PASS',changed,'pixels changed;26 other frames exact')

if __name__=='__main__':
    if '--build' in sys.argv:build()
    else:verify('--old' in sys.argv)
