"""Bounded native Duckboard correction; uses approved editable quads, no raster input.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --verify [--old for expected RED against preserved source]
All non-board frames and foreign import metadata stay exact. Original generator
and source remain under sodden-travel/deck-before for reversible reproduction.
"""
import copy,hashlib,json,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Live16x24/sodden-travel';BASE=OUT/'deck-before'
def read(p):return json.loads(p.read_text())
def save(n,d):(OUT/n).write_text(json.dumps(d,indent=2)+'\n')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guard():
    for p,h in read(ROOT/'Docs/Verification/GraphicsExecution/CausewayDeck/protected-inputs.json').items():assert sha(ROOT/p)==h,p

def cells(mask):
    old=read(BASE/'source-cells.json')['frames'][mask]['cells']
    corners=((mask&3)==3,(mask&6)==6,(mask&12)==12,(mask&9)==9)
    if not any(corners):return copy.deepcopy(old)
    def shape(x,y):
        return (4<=x<=11 and 8<=y<=15 or mask&1 and 4<=x<=11 and y<=11 or mask&2 and x>=8 and 8<=y<=15
                or mask&4 and 4<=x<=11 and y>=12 or mask&8 and x<=7 and 8<=y<=15
                or corners[0] and x>=8 and y<=11 or corners[1] and x>=8 and y>=12
                or corners[2] and x<=7 and y>=12 or corners[3] and x<=7 and y<=11)
    def exposed(x,y):
        # Connected cell boundaries are not structural rim faces. A wider elbow
        # stays inside this owner; no diagonal owner or new collision is implied.
        if x<0:return not mask&8
        if x>15:return not mask&2
        if y<0:return not mask&1
        if y>23:return not mask&4
        return not shape(x,y)
    result=[]
    for y in range(24):
        row=[]
        for x in range(16):
            if not shape(x,y):row.append(None);continue
            part,color='continuous-weathered-deck','board'
            if any(exposed(xx,yy) for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))):part,color='exposed-cut-deck-edge','board-dark'
            elif x%4==3:part,color='fine-plank-joint','board-grain'
            elif x%4==1 and y%8 in (2,3,4):part,color='short-wood-grain','board-grain'
            elif x%8==0 and y%12==5:part,color='small-worn-grain','board-worn'
            row.append([part,color])
        result.append(row)
    # Preserve exact established central seam colors for the unchanged narrow
    # straights/dead ends. Filled corners add decking around that common core.
    for y,bit in ((0,1),(23,4)):
        if mask&bit:
            for x in range(4,12):result[y][x]=old[y][x]
    for x,bit in ((0,8),(15,2)):
        if mask&bit:
            for y in range(8,16):result[y][x]=old[y][x]
    return result

def data():
    d=read(BASE/'source-cells.json')
    for mask in range(16):d['frames'][mask]['cells']=cells(mask)
    return d

def contract(d):
    base=read(BASE/'source-cells.json');assert d['palette']==base['palette'];assert len(d['frames'])==28
    for i in range(16,28):assert d['frames'][i]==base['frames'][i],('Protected non-board frame',i)
    for mask in (0,1,2,4,5,8,10):assert d['frames'][mask]==base['frames'][mask],('Narrow topology changed',mask)
    for mask,f in enumerate(d['frames'][:16]):
        assert f['neighborMask']==mask and f['blueprint']=='Duckboard'
        assert len(f['cells'])==24 and all(len(row)==16 for row in f['cells'])
        for x,y,bits in ((15,0,3),(15,23,6),(0,23,12),(0,0,9)):
            assert bool(f['cells'][y][x])==((mask&bits)==bits),('Connected quadrant remains open',mask,x,y)
        for y,row in enumerate(base['frames'][mask]['cells']):
            for x,c in enumerate(row):assert not c or f['cells'][y][x],('Original walking surface removed',mask,x,y)
    assert all(c for row in d['frames'][15]['cells'] for c in row),'Four-way platform still has holes'

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for
    from build_north_plot import instance
    from build_limestone_caves import canonicalize_timing
    guard();d=data();contract(d);bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene;scene.name='Sodden deck polish native448x24'
    for f in d['frames']:
        col=model(scene,f['id'],f['cells'],d['palette']);scene.collection.children.unlink(col);instance(scene,col,f['id'],f['index']*16,0)
    camera_for(scene,448,24);scene.render.filepath=str(OUT/'sodden-travel.png');bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/'sodden-travel.png')
    save('source-cells.json',d);m=read(BASE/'manifest.json');m.update(generator='Tools/Live16x24/build_sodden_deck.py',acceptedNonMireFramesPreserved=[18,19,20,21],protectedFrames=list(range(16,28)),narrowBoardFramesPreserved=[0,1,2,4,5,8,10],sha256=sha(OUT/'sodden-travel.png'),scope='Bounded connected Duckboard quadrant correction; only frames0–15 may change. Actual play tracked separately.',historicalContact='deck-before/contact.png');m.pop('contact',None)
    for f in m['frames'][:16]:
        if f['index'] not in (0,1,2,4,5,8,10):f['description']='Flat contiguous weathered decking at cardinal junctions; all pixels stay within the actual owner cell.'
    save('manifest.json',m);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sodden-travel.blend'));guard()

def verify(old=False):
    from PIL import Image
    guard();d=read((BASE if old else OUT)/'source-cells.json');contract(d)
    a=Image.open(BASE/'sodden-travel.png').convert('RGBA');b=Image.open(OUT/'sodden-travel.png').convert('RGBA');assert a.size==b.size==(448,24)
    changed=0
    for f in d['frames']:
        for y,row in enumerate(f['cells']):
            for x,c in enumerate(row):
                point=(f['index']*16+x,y);want=tuple(bytes.fromhex(d['palette'][c[1]]))+(255,) if c else (0,0,0,0)
                assert b.getpixel(point)==want,('Native pixel drift',point)
                if f['index']>=16 or f['index'] in (0,1,2,4,5,8,10):assert a.getpixel(point)==b.getpixel(point),('Protected pixel drift',point)
                changed+=a.getpixel(point)!=b.getpixel(point)
    save('deck-verification.json',{'passed':True,'changed_pixels':changed,'protected_frames':list(range(16,28)),'unchanged_narrow_masks':[0,1,2,4,5,8,10],'native_pixels':448*24,'runtime_sha256':sha(OUT/'sodden-travel.png'),'actual_play':False});print('PASS',changed,'pixels changed;12non-board and7narrow masks exact')
if __name__=='__main__':
    # The historical implementation remains preserved in peat-before.
    from build_sodden_peat import build as current_build, verify as current_verify
    if '--build' in sys.argv:current_build()
    else:current_verify('--old' in sys.argv)
