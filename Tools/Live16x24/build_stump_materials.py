"""Original Stump static material cells, editable flat integer-pixel geometry.

Blender --background --threads 4 --python this-file
python3 this-file --snapshot-protected | --verify | --countercheck

Concepts guide palette and parts only. No raster is read by the authoring path.
Only the new Stump packet and its new Unity atlas/metadata are written.
"""
import copy
import hashlib
import json
from pathlib import Path
import struct
import sys
import zlib

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/stump-materials'
VERIFY = OUT / 'verification'
ATLAS = ROOT / 'Assets/Resources/Sprites/Live16x24/stump-materials.png'
WIDTH, HEIGHT, COUNT = 16, 24, 71
ATLAS_WIDTH = WIDTH * COUNT
CONTACT_WIDTH, CONTACT_HEIGHT = 352, 512
PERIMETER_GROUPS = ((4, 26, 'wall'), (6, 41, 'ridge'), (20, 56, 'dome'))
PALETTE = {
    # Actual-play correction under the unchanged Cave grade: compensate stone
    # toward dusty pink-grey, and separate solid masses from walkable ground.
    # Geometry, water, living plants and pale mineral seams remain unchanged.
    'stone-ground': '9E98B0', 'ground-grain': '9793AB', 'ground-light': 'A39DB4',
    'wall': '6B7089', 'wall-grain': '60667F', 'wall-light': '7B7C95',
    'ridge': '747790', 'ridge-grain': '687086', 'ridge-light': '85869F',
    'ledge': 'A29DB2', 'ledge-edge': '8B8AA0', 'ledge-light': 'ADA7B9',
    'dome': '767B9A', 'dome-grain': '6A708F', 'dome-light': '858AA7',
    'water': '31565A', 'water-deep': '2D5055', 'water-light': '3B6265',
    'film': '426365', 'film-depth': '3C5D60', 'film-light': '4E7070',
    'leaf-dark': '304D3E', 'leaf': '52704B', 'leaf-light': '78915A',
    'cup-water': '24474C', 'cup-reflection': '48716D',
    'vein-dark': '675D5D', 'vein-rock': '85746F', 'vein-pale': 'C3B7A2',
    'vein-light': 'D1C6B0',
}


class Frame:
    def __init__(self, name, blueprint, description, **state):
        self.name, self.blueprint, self.description, self.state = name, blueprint, description, state
        self.cells = [[None] * WIDTH for _ in range(HEIGHT)]

    def rect(self, part, color, x, y, w=1, h=1):
        assert color in PALETTE and 0 <= x < x+w <= WIDTH and 0 <= y < y+h <= HEIGHT
        for yy in range(y, y+h):
            for xx in range(x, x+w): self.cells[yy][xx] = (part, color)
        return self

    def path(self, part, color, points):
        for x, y in points: self.rect(part, color, x, y)
        return self

    def rows(self, part, color, rows):
        for y, x, w in rows: self.rect(part, color, x, y, w)
        return self


def ground(v):
    f = Frame('quiet-pink-stone-'+str(v), 'TepuiStone',
              'Quiet worn pink-grey stone plane. Rare shallow fossil grain; no dot carpet, raised rubble or tile border.',
              variant=v, opaque=True, quietBaseWeight=75)
    f.rect('smooth-stone', 'stone-ground', 0, 0, 16, 24)
    if v == 1: f.rect('rare-fossil-grain', 'ground-grain', 3, 15, 5)
    if v == 2: f.rect('rare-fossil-grain', 'ground-light', 9, 6, 4)
    if v == 3: f.rect('rare-fossil-grain', 'ground-grain', 7, 19, 4).rect('rare-fossil-grain', 'ground-light', 6, 18, 2)
    return f


def wall(v):
    f = Frame('stone-shoulder-'+str(v), 'TepuiWall',
              'Opaque low stone shoulder field. Real owner boundaries define the connected outcrop; no extrusion or brick joints.',
              variant=v, solid=True, opaque=True, partialDamageRetainsIdentity=True)
    f.rect('connected-stone-shoulder', 'wall', 0, 0, 16, 24)
    if v == 0:
        f.rect('subdued-stone-grain', 'wall-grain', 0, 16, 8).rect('subdued-stone-grain', 'wall-grain', 8, 15, 8)
        f.rect('worn-grain-edge', 'wall-light', 1, 7, 5)
    else:
        f.rect('subdued-stone-grain', 'wall-grain', 0, 17, 5).rect('subdued-stone-grain', 'wall-grain', 5, 16, 11)
        f.rect('worn-grain-edge', 'wall-light', 9, 8, 5)
    return f


def ridge(v):
    f = Frame('petrified-grain-mass-'+str(v), 'GrainRidge',
              'Connected opaque petrified wood-grain stone. Majority quiet field; sparse east-west fragments, never repeated rails or individual boulders.',
              variant=v, solid=True, opaque=True, quietBaseWeight=75)
    f.rect('connected-petrified-mass', 'ridge', 0, 0, 16, 24)
    if v == 1: f.rect('rare-east-west-fiber', 'ridge-grain', 1, 14, 10).rect('rare-east-west-fiber', 'ridge-grain', 11, 13, 4)
    if v == 2: f.rect('rare-worn-fiber', 'ridge-light', 2, 7, 8).rect('rare-worn-fiber', 'ridge-light', 10, 8, 3)
    if v == 3: f.rect('rare-east-west-fiber', 'ridge-grain', 0, 19, 6).rect('rare-east-west-fiber', 'ridge-grain', 6, 18, 7)
    return f


def ledge(v):
    f = Frame('passable-low-shelf-'+str(v), 'DescentLedge',
              'Thin flat shelf laid on real ground. No vertical face, tall cliff, stairs, arrow or promised drop.',
              variant=v, solid=False, opaque=False)
    f.rect('low-shelf-contact', 'ledge-edge', 0, 16, 16, 3)
    f.rect('walkable-shelf', 'ledge', 0, 11, 16, 6)
    f.rect('low-worn-edge', 'ledge-light', 0, 11, 16)
    if v == 0: f.rect('worn-shelf-grain', 'ledge-edge', 3, 14, 5)
    else: f.rect('worn-shelf-grain', 'ledge-edge', 9, 15, 5)
    return f


def pool(empty, v):
    body = 'film' if empty else 'water'; light = 'film-light' if empty else 'water-light'
    dark = 'film-depth' if empty else 'water-deep'
    f = Frame(('empty-still-wet-' if empty else 'available-spray-water-')+str(v), 'SprayPool',
              'Continuous cool wet surface with sparse muted reflections. Zero draw stock still has actual water; no dry bed or separate tile bowl.',
              variant=v, solid=False, opaque=True, volumeRule='Volume == 0' if empty else 'Volume > 0', stillWet=True, quietBaseWeight=75)
    f.rect('persistent-wet-surface' if empty else 'available-water-surface', body, 0, 0, 16, 24)
    if v == 1: f.rect('rare-flat-reflection', light, 3, 9 if empty else 7, 7)
    if v == 2: f.rect('rare-flat-reflection', light, 8, 17, 6).rect('subsurface-water', dark, 9, 18, 4)
    if v == 3: f.rect('rare-flat-reflection', light, 1, 19, 5).rect('subsurface-water', dark, 10, 5, 3)
    return f


def dome(v):
    f = Frame('smooth-dome-mass-'+str(v), 'StoneDome',
              'Low connected smooth dome surface. Rounded actual owner clusters supply the outer silhouette; no per-tile capped cylinder or stacked 3D boulder.',
              variant=v, solid=True, opaque=True)
    f.rect('smooth-connected-dome', 'dome', 0, 0, 16, 24)
    if v == 0:
        f.rect('soft-curved-grain', 'dome-grain', 3, 17, 4).rect('soft-curved-grain', 'dome-grain', 7, 16, 6)
        f.rect('soft-worn-surface', 'dome-light', 6, 7, 4)
    else:
        f.rect('soft-curved-grain', 'dome-grain', 1, 15, 5).rect('soft-curved-grain', 'dome-grain', 6, 16, 5)
        f.rect('soft-worn-surface', 'dome-light', 9, 8, 3)
    return f


def tank(v):
    f = Frame('broad-living-tank-'+str(v), 'TankBrocchinia',
              'Compact broad living leaf cup with contained dark water. Low asymmetric bromeliad, not a barrel, cactus, crop or depleted finite pool.',
              variant=v, solid=False, opaque=False, well=True, finiteStock=False)
    # Broad curved leaves surround a small contained opening. No vertical trunk,
    # straight container sides, stave bands, thorns, fruit or bloom.
    f.rows('broad-leaf-rosette', 'leaf-dark', [(8,3,2),(9,3,4),(10,2,5),(11,2,6),(12,1,8),
        (8,11,2),(9,9,4),(10,9,5),(11,8,6),(12,7,8),
        (13,1,14),(14,1,14),(15,1,14),(16,2,12),(17,2,12),(18,3,10),(19,3,10),(20,4,8),(21,5,6),(22,6,4)])
    f.rows('broad-folded-leaf', 'leaf', [(9,3,2),(10,3,3),(11,3,3),(12,2,4),(13,2,3),(14,2,3),(15,3,3),(16,3,4),
        (9,11,2),(10,10,3),(11,10,3),(12,10,4),(13,11,3),(14,11,3),(15,10,3),(16,9,4),
        (18,4,3),(19,4,4),(20,5,3),(21,6,2),(17,9,3),(18,9,3),(19,8,3),(20,8,3)])
    f.path('curved-living-leaf-edge', 'leaf-light', [(3,9),(4,10),(4,11),(3,12),(2,13),(3,15),(4,16),(5,18),
        (12,9),(11,10),(11,11),(12,12),(13,13),(12,15),(11,16),(10,18)])
    f.rows('contained-water', 'cup-water', [(12,6,4),(13,5,6),(14,5,6),(15,6,4)])
    f.rect('contained-water-reflection', 'cup-reflection', 6 if v == 0 else 8, 13, 2)
    f.path('front-overlapping-leaf', 'leaf', [(4,17),(5,17),(6,18),(7,19),(7,20),(8,21),(11,17),(10,18),(9,19),(8,20)])
    f.path('front-leaf-rim', 'leaf-light', [(5,17),(6,18),(7,19)] if v == 0 else [(11,17),(10,18),(9,19)])
    return f


def vein(v):
    f = Frame('pale-stone-wood-vein-'+str(v), 'TepuiboneVein',
              'Pale mineral seam in a low angular stone-wood source. Larger than its carried chip; no depleted node after successful harvest.',
              variant=v, solid=True, opaque=False, unharvested=True, yieldBlueprint='Tepuibone')
    f.rows('grounded-seam-rock', 'vein-dark', [(11,6,5),(12,4,8),(13,3,10),(14,2,12),(15,1,14),(16,1,14),(17,2,12),(18,2,12),(19,3,10),(20,4,8),(21,5,6),(22,6,4)])
    f.rows('stone-wood-face', 'vein-rock', [(12,6,4),(13,4,7),(14,3,9),(15,2,11),(16,2,11),(17,3,9),(18,4,7),(19,5,5),(20,6,3)])
    pts = [(2,18),(3,18),(4,17),(5,17),(6,16),(7,16),(8,16),(9,15),(10,15),(11,14),(12,14)] if v == 0 else [(2,16),(3,16),(4,17),(5,17),(6,17),(7,18),(8,18),(9,17),(10,17),(11,16),(12,16)]
    f.path('pale-embedded-seam', 'vein-pale', pts)
    f.path('pale-mineral-edge', 'vein-light', [(5,16),(8,15),(11,13)] if v == 0 else [(4,16),(7,17),(11,15)])
    return f


def frames():
    original = ([ground(v) for v in range(4)] + [wall(v) for v in range(2)] +
            [ridge(v) for v in range(4)] + [ledge(v) for v in range(2)] +
            [pool(False, v) for v in range(4)] + [pool(True, v) for v in range(4)] +
            [dome(v) for v in range(2)] + [tank(v) for v in range(2)] + [vein(v) for v in range(2)])
    result = list(original)
    # These are outside boundaries of a disclosed same-family mass, not borders
    # on every tile. Mask zero keeps the unchanged original interior variants.
    # Source y runs downward: N=top, E=right, S=bottom, W=left.
    for base, first, family in PERIMETER_GROUPS:
        assert len(result) == first
        for mask in range(1, 16):
            f = copy.deepcopy(original[base])
            f.name = family+'-exposed-boundary-'+str(mask)
            f.description = 'Flat one-pixel exposed outside boundary only. Connected sides and every interior pixel retain the original material; no extrusion or tile grid.'
            f.state.update(exposedMask=mask, maskBits={'N':1,'E':2,'S':4,'W':8}, sourceBaseFrame=base)
            # Upper/left light, lower/right dark; S/E dominate shared corners.
            if mask & 1: f.rect('exposed-north-edge', family+'-light', 0, 0, WIDTH)
            if mask & 8: f.rect('exposed-west-edge', family+'-light', 0, 0, 1, HEIGHT)
            if mask & 2: f.rect('exposed-east-edge', family+'-grain', WIDTH-1, 0, 1, HEIGHT)
            if mask & 4: f.rect('exposed-south-edge', family+'-grain', 0, HEIGHT-1, WIDTH)
            result.append(f)
    return result


def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(p, obj): p.write_text(json.dumps(obj, indent=2)+'\n')


def canonicalize_render_metadata(path):
    """Remove only volatile Blender render timing, preserving every pixel chunk.

    No raster decoding/re-encoding: IHDR, IDAT, alpha and color metadata remain
    byte-for-byte as rendered. Blender emits timing even with visible stamps off.
    """
    data=path.read_bytes(); assert data[:8]==b'\x89PNG\r\n\x1a\n'
    result=bytearray(data[:8]); position=8
    while position<len(data):
        length=struct.unpack('>I',data[position:position+4])[0]
        end=position+12+length; assert end<=len(data)
        kind=data[position+4:position+8]; payload=data[position+8:position+8+length]
        assert zlib.crc32(kind+payload)&0xffffffff==struct.unpack('>I',data[end-4:end])[0]
        key=payload.split(b'\0',1)[0] if kind==b'tEXt' else b''
        if not (key.startswith(b'cycles.') and key.endswith(b'_time')): result.extend(data[position:end])
        position=end
    path.write_bytes(result)


def snapshot():
    VERIFY.mkdir(parents=True, exist_ok=True)
    protected = {}
    for p in (ROOT/'ArtSource/Live16x24').rglob('*'):
        if p.is_file() and OUT not in p.parents and (p.suffix in ('.png', '.blend') or p.name in ('source-cells.json', 'palette.json', 'import-spec.json')):
            protected[str(p.relative_to(ROOT))] = digest(p)
    for p in (ROOT/'Assets/Resources/Sprites/Live16x24').rglob('*'):
        if p.is_file() and p != ATLAS and p != Path(str(ATLAS)+'.meta'):
            protected[str(p.relative_to(ROOT))] = digest(p)
    for name in ('build_opening_base.py','build_cindercaller.py','build_north_plot.py','build_east_kitchen.py'):
        p=ROOT/'Tools/Live16x24'/name; protected[str(p.relative_to(ROOT))]=digest(p)
    target=VERIFY/'protected-inputs.json'
    assert not target.exists(), 'Do not overwrite the original protection baseline.'
    save(target, protected)
    print(json.dumps({'protectedInputCount':len(protected)}))


def guard():
    protected=json.loads((VERIFY/'protected-inputs.json').read_text())
    for rel, expected in protected.items(): assert digest(ROOT/rel)==expected, ('Protected input changed',rel)
    return len(protected)


def source_payload():
    return {'schema':1,'coordinateSystem':'Top-left x right/y down; original named integer-cell quads. Null means transparent.',
            'palette':PALETTE,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'description':f.description,'state':f.state,'cells':f.cells} for i,f in enumerate(frames())]}


def validate(source, rgba, size):
    assert size==(ATLAS_WIDTH,HEIGHT), ('atlas dimensions',size)
    assert source==json.loads(json.dumps(source_payload())), 'Editable cell/state/order contract differs.'
    assert len(rgba)==ATLAS_WIDTH*HEIGHT*4
    decoded=[]
    for i,f in enumerate(source['frames']):
        cell_bytes=bytearray()
        for y,row in enumerate(f['cells']):
            for x,cell in enumerate(row):
                expected=bytes.fromhex(PALETTE[cell[1]])+b'\xff' if cell else b'\0\0\0\0'
                at=4*(y*ATLAS_WIDTH+i*WIDTH+x)
                assert rgba[at:at+4]==expected, ('native pixel mismatch',i,x,y)
                cell_bytes.extend(expected)
        decoded.append(bytes(cell_bytes))
    assert len(set(decoded))==COUNT, 'Every native frame must be distinct.'
    validate_perimeters(source)
    for i in (0,6,12,16):
        assert len({tuple(c) for row in source['frames'][i]['cells'] for c in row})==1, ('Majority field must stay quiet',i)
    for i in (10,11,22,23,24,25):
        assert all(c is None for c in source['frames'][i]['cells'][23]), ('Keep compact source foot within cell',i)
    for i in (22,23):
        parts={c[0] for row in source['frames'][i]['cells'] for c in row if c}
        assert 'contained-water' in parts and 'broad-folded-leaf' in parts
    return decoded


def validate_perimeters(source):
    """Independent preservation/edge-only gate against the frozen v2 source."""
    history = json.loads((VERIFY/'palette-v2-before-perimeter/source-cells.json').read_text())
    assert source['palette'] == history['palette'], 'Perimeter correction cannot change the palette.'
    assert source['frames'][:26] == history['frames'], 'Original 26 frame geometry/state must remain exact.'
    for base, first, family in PERIMETER_GROUPS:
        interior = history['frames'][base]['cells']
        for mask in range(1, 16):
            f = source['frames'][first+mask-1]
            assert f['state']['exposedMask'] == mask and f['state']['sourceBaseFrame'] == base
            for y in range(HEIGHT):
                for x in range(WIDTH):
                    # Explicit direction precedence, separate from authoring rects.
                    expected = interior[y][x]
                    if y == 0 and mask & 1: expected = ['exposed-north-edge',family+'-light']
                    if x == 0 and mask & 8: expected = ['exposed-west-edge',family+'-light']
                    if x == WIDTH-1 and mask & 2: expected = ['exposed-east-edge',family+'-grain']
                    if y == HEIGHT-1 and mask & 4: expected = ['exposed-south-edge',family+'-grain']
                    assert f['cells'][y][x] == expected, ('Outside edge-only invariant',first+mask-1,x,y)


def verify():
    from PIL import Image
    protected=guard()
    source=json.loads((OUT/'source-cells.json').read_text())
    manifest=json.loads((OUT/'manifest.json').read_text())
    with Image.open(ATLAS) as im:
        assert im.mode=='RGBA'; rgba=im.tobytes(); decoded=validate(source,rgba,im.size)
    assert digest(ATLAS)==manifest['atlasSha256']
    assert json.loads((OUT/'palette.json').read_text())==PALETTE
    spec=json.loads((OUT/'import-spec.json').read_text())
    assert spec['resourcePath']=='Sprites/Live16x24/stump-materials' and spec['frameCount']==COUNT and spec['filterMode']=='Point' and spec['mipmaps'] is False
    metadata=Path(str(ATLAS)+'.meta').read_text()
    for setting in ('enableMipMap: 0','filterMode: 0','wrapU: 1','wrapV: 1','spritePixelsToUnits: 16','textureCompression: 0'):
        assert setting in metadata, ('Missing import setting',setting)
    for filename,scale in (('contact-native.png',1),('contact-enlarged.png',8)):
        with Image.open(OUT/filename) as im:
            assert im.size==(CONTACT_WIDTH*scale,CONTACT_HEIGHT*scale)
            for i,placement in enumerate(manifest['contact']['framePlacements']):
                px,py=placement['topLeft']; cell=decoded[i]
                for y in range(24):
                    for x in range(16):
                        at=(y*16+x)*4; expected=tuple(cell[at:at+4])
                        if expected[3]==0: expected=tuple(bytes.fromhex(PALETTE['stone-ground']))+(255,)
                        for oy in range(scale):
                            for ox in range(scale):
                                assert im.getpixel(((px+x)*scale+ox,(py+y)*scale+oy))==expected, ('Direct contact pixel',filename,i,x,y)
    assert (OUT/'stump-materials.blend').stat().st_size>10000
    result={'passed':True,'frameCount':COUNT,'atlasSize':[ATLAS_WIDTH,HEIGHT],'nativePixelsVerified':ATLAS_WIDTH*HEIGHT,'atlasSha256':digest(ATLAS),'decodedRgbaSha256':hashlib.sha256(rgba).hexdigest(),'protectedInputCount':protected,'directNativeAnd8xContactCellsVerified':True,'original26FramesExact':True,'exposedEdgeOnlyFramesVerified':45,'scope':'Source/import-byte verification only. No Unity/native gameplay tests or actual scene acceptance.'}
    save(VERIFY/'source-green.json',result);print(json.dumps(result));return source,rgba


def countercheck():
    from PIL import Image
    source=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(ATLAS) as im: rgba=im.tobytes()
    validate(source,rgba,(ATLAS_WIDTH,HEIGHT)); results=[]
    def rejected(name,s=source,b=rgba,size=(ATLAS_WIDTH,HEIGHT)):
        try: validate(s,b,size)
        except (AssertionError,KeyError,ValueError,IndexError): results.append({'case':name,'rejected':True});return
        raise AssertionError('Corruption accepted: '+name)
    b=bytearray(rgba);b[0]^=1;rejected('wrong-native-color',b=bytes(b))
    b=bytearray(rgba);b[3]=127;rejected('partial-alpha',b=bytes(b))
    b=bytearray(rgba);b[:4]=b'\0\0\0\0';rejected('transparent-hole-in-ground',b=bytes(b))
    rejected('wrong-atlas-width',size=(ATLAS_WIDTH-16,24))
    s=copy.deepcopy(source);s['frames'][12],s['frames'][16]=s['frames'][16],s['frames'][12];rejected('swapped-stock-state',s=s)
    s=copy.deepcopy(source);s['frames'][0]['state']['quietBaseWeight']=25;rejected('wrong-majority-field-weight',s=s)
    s=copy.deepcopy(source);s['frames'][22]['state']['finiteStock']=True;rejected('invented-finite-tank-stock',s=s)
    s=copy.deepcopy(source);s['frames'][10]['state']['solid']=True;rejected('invented-solid-ledge',s=s)
    # Corrupt the exported pixels at meaningful boundary/interior positions.
    for name, frame, x, y, palette_key in (
        ('missing-selected-north-edge',26,7,0,'wall'),
        ('invented-unselected-south-edge',26,7,23,'wall-grain'),
        ('changed-connected-interior',56,8,12,'dome-light')):
        b=bytearray(rgba);at=4*(y*ATLAS_WIDTH+frame*WIDTH+x)
        b[at:at+4]=bytes.fromhex(PALETTE[palette_key])+b'\xff'
        rejected(name,b=bytes(b))
    save(VERIFY/'corruption-counterchecks.json',{'positiveControlPassed':True,'inMemoryOnly':True,'cases':results,'atlasUnchanged':True,'atlasSha256':digest(ATLAS)})
    print(json.dumps({'counterchecksPassed':len(results),'atlasUnchanged':True}))


def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_opening_base import camera_for,add_label,emission_material
    from build_cindercaller import model
    from build_north_plot import instance,plane
    from build_east_kitchen import write_meta
    protected=guard();fs=frames();assert len(fs)==COUNT
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Stump original native '+str(ATLAS_WIDTH)+'x24';cols=[]
    for i,f in enumerate(fs):
        col=model(native,f'{i:02d} {f.name}',f.cells,PALETTE);native.collection.children.unlink(col)
        for obj in col.objects: obj['authoring']='Original Stump flat integer-cell geometry; no raster source.'
        instance(native,col,str(i),i*16,0);cols.append(col)
    camera_for(native,ATLAS_WIDTH,HEIGHT);native.render.filepath=str(ATLAS)
    bpy.ops.render.render(write_still=True,scene=native.name)
    canonicalize_render_metadata(ATLAS)
    placements=[{'frame':i,'topLeft':[16+(i%10)*32,20+(i//10)*36]} for i in range(COUNT)]
    # Native source studies are comparisons, not new game geometry or gameplay evidence.
    ground_study=[0]*80
    for j,n in enumerate((1,6,11,17,22,27,32,38,43,48,53,58,63,68,73,77,3,25,51,70)): ground_study[n]=1+j%3
    for scale,name in ((1,'contact-native'),(8,'contact-enlarged')):
        scene=bpy.data.scenes.new('Stump direct editable contact '+str(scale)+'x')
        camera_for(scene,CONTACT_WIDTH,CONTACT_HEIGHT,scale)
        plane(scene,'Contact surround','242E2C',0,0,CONTACT_WIDTH,CONTACT_HEIGHT,-3)
        label=emission_material('Source labels','CABBA6')
        add_label(scene,'STUMP / FLAT MATERIALS + EXPOSED BOUNDARIES / 16x24',176,CONTACT_HEIGHT-8,label,2.0)
        for p in placements:
            i=p['frame'];x,y=p['topLeft']
            plane(scene,'Context '+str(i),PALETTE['stone-ground'],x,CONTACT_HEIGHT-y-24,16,24,-1)
            instance(scene,cols[i],str(i),x,CONTACT_HEIGHT-y-24)
            add_label(scene,str(i),x+8,CONTACT_HEIGHT-y-29,label,1.8)
        add_label(scene,'SOURCE STUDY / OUTSIDE EDGES ONLY / NOT GAMEPLAY EVIDENCE',176,CONTACT_HEIGHT-310,label,1.8)
        for n,fr in enumerate(ground_study):
            instance(scene,cols[fr],'Study ground '+str(n),16+(n%20)*16,CONTACT_HEIGHT-320-(n//20)*24-24,-1)
        def study(cells, first, interior, ox, oy, name):
            occupied=set(cells)
            for x,y in sorted(occupied):
                mask=(1 if (x,y-1) not in occupied else 0)|(2 if (x+1,y) not in occupied else 0)|(4 if (x,y+1) not in occupied else 0)|(8 if (x-1,y) not in occupied else 0)
                fr=first+mask-1 if mask else interior
                instance(scene,cols[fr],name+str((x,y)),ox+x*16,CONTACT_HEIGHT-oy-y*24-24)
        # The study alone does not prove actual disclosed-owner selection.
        study([(x,y) for y in range(3) for x in range(3)],26,4,16,320,'Shoulder study ')
        study([(x,y) for x in range(8) for y in range(2)],41,6,96,344,'Ridge study ')
        study([(0,0),(1,0),(2,0),(0,1),(1,1),(2,1),(3,1)],56,20,272,344,'Dome study ')
        for i,x,y in ((22,80,392),(23,240,392),(24,224,320),(10,144,392)):
            instance(scene,cols[i],'Sparse study '+str(i),x,CONTACT_HEIGHT-y-24)
        add_label(scene,'AVAILABLE WATER',80,CONTACT_HEIGHT-436,label,2)
        add_label(scene,'ZERO STOCK / STILL WET',272,CONTACT_HEIGHT-436,label,2)
        for empty,offset in ((False,16),(True,208)):
            base=16 if empty else 12
            for n in range(16):
                fr=base if n not in (1,6,11,14) else base+1+(n%3)
                instance(scene,cols[fr],'Water study '+str((empty,n)),offset+(n%8)*16,CONTACT_HEIGHT-448-(n//8)*24-24)
        scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
        canonicalize_render_metadata(OUT/(name+'.png'))
    save(OUT/'source-cells.json',source_payload());save(OUT/'palette.json',PALETTE)
    spec={'resourcePath':'Sprites/Live16x24/stump-materials','width':ATLAS_WIDTH,'height':HEIGHT,'frameWidth':16,'frameHeight':24,'frameCount':COUNT,'pixelsPerUnit':16,'pivot':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','alpha':'binary','mesh':'FullRect'}
    save(OUT/'import-spec.json',spec);write_meta(Path(str(ATLAS)+'.meta'))
    refs=[OUT/'concepts/concept-v1.png',OUT/'concepts/concept-v2.png']
    manifest={'schema':1,'resource':spec['resourcePath'],'atlas':str(ATLAS.relative_to(ROOT)),'atlasSha256':digest(ATLAS),'width':ATLAS_WIDTH,'height':HEIGHT,'frameCount':COUNT,'frameWidth':16,'frameHeight':24,'source':'stump-materials.blend','generator':str(Path(__file__).relative_to(ROOT)),'scope':'Eight exact Stump source families. Original26 palette-v2 cells preserved;45 exposed-edge frames appended for actual disclosed boundaries. Approved Tree/Bush borrowed unchanged by runtime. Actual Unity comparison pending root.','pngMetadata':'Only volatile cycles.*_time text chunks removed; original IDAT/pixels and color metadata retained.','conceptReferences':[{'path':str(p.relative_to(ROOT)),'sha256':digest(p)} for p in refs],'rejectedConceptFeatures':['extruded walls','changed source positions/HUD/actor','barrel/cactus tanks','oversized domes','dense dotted ground'],'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'description':f.description,'state':f.state,'unityRect':[i*16,0,16,24]} for i,f in enumerate(fs)],'contact':{'nativeSize':[CONTACT_WIDTH,CONTACT_HEIGHT],'scales':[1,8],'framePlacements':placements,'groundStudyQuietBaseCount':60,'groundStudyCount':80,'notGameplayEvidence':True},'protectedInputCount':protected}
    save(OUT/'manifest.json',manifest)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stump-materials.blend'))
    guard();print(json.dumps({'built':True,'frameCount':COUNT,'atlasSha256':digest(ATLAS)}))


if __name__=='__main__':
    if '--snapshot-protected' in sys.argv: snapshot()
    elif '--countercheck' in sys.argv: countercheck()
    elif '--verify' in sys.argv: verify()
    else: build()
