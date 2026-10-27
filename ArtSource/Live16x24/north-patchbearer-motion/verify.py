"""Decode-only original Patchbearer motion check; no raster editing or Unity calls."""
import argparse
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--check-unity', action='store_true')
args = parser.parse_args()
required = ('native.blend', 'north-patchbearer-motion.png',
            'north-patchbearer-motion.png.meta', 'contact-native.png', 'contact.png',
            'source-cells.json', 'manifest.json', 'preserved-inputs.json')
missing = [name for name in required if not (HERE/name).is_file()]
assert not missing, 'Source RED: Patchbearer motion outputs missing: '+', '.join(missing)

source = json.loads((HERE/'source-cells.json').read_text())
manifest = json.loads((HERE/'manifest.json').read_text())
palette = {'stone_dark':'3C5148', 'stone':'526657', 'linen':'B5A47E',
           'skin':'A97542', 'cold':'70A68A', 'cream':'E4D0A0'}
assert source['palette'] == palette
order = [(state,pose) for state in ('full','empty')
         for pose in ('walk-a','walk-b','attack','hurt')]
frames = source['frames']
assert len(frames)==8 and [(f['healingSlot'],f['pose'])for f in frames]==order
assert all(f['blueprint']=='MarlbackPatchbearer' for f in frames)
approved = json.loads((ROOT/'ArtSource/Live16x24/north-works/source-cells.json').read_text())
idle = [approved['frames'][7],approved['frames'][8]]
assert [f['id']for f in idle]==['patchbearer-healing-slot-present','patchbearer-healing-slot-absent']
rgba = {k:tuple(bytes.fromhex(v))+(255,)for k,v in palette.items()}
native = Image.open(HERE/'north-patchbearer-motion.png').convert('RGBA')
small = Image.open(HERE/'contact-native.png').convert('RGBA')
large = Image.open(HERE/'contact.png').convert('RGBA')
assert native.size==(128,24) and small.size==(128,64) and large.size==(1024,512)
assert set(native.getchannel('A').tobytes())=={0,255}
assert set(small.getchannel('A').tobytes())==set(large.getchannel('A').tobytes())=={255}
ground=tuple(bytes.fromhex('032F23'))+(255,)
decoded=[]
frame_reports=[]
def pixel(c): return rgba[c[1]] if c else (0,0,0,0)
for i,f in enumerate(frames):
    cells=f['cells'];state=i//4
    assert len(cells)==24 and all(len(row)==16 for row in cells)
    assert all(c is None or isinstance(c,list) and len(c)==2 and c[1] in palette for row in cells for c in row)
    values=[pixel(c) for row in cells for c in row]
    assert values!=[pixel(c)for row in idle[state]['cells']for c in row], 'Derivative must change a pose'
    assert values not in decoded, 'No duplicate pose padding'
    decoded.append(values)
    occupied=[];colors=set();parts=set()
    for y,row in enumerate(cells):
        for x,c in enumerate(row):
            assert native.getpixel((i*16+x,y))==pixel(c),('source/native',i,x,y)
            if c: occupied.append((x,y));colors.add(c[1]);parts.add(c[0])
    assert occupied and max(y for x,y in occupied)==22
    assert all(c is None for c in cells[23]),'Baseline margin must stay transparent'
    assert 4<=len(colors)<=6
    assert any('strap'in p for p in parts) and any('cradle'in p for p in parts)
    assert any('bottle'in p for p in parts)==(state==0)
    assert not any(any(word in p.lower()for word in ('sword','dagger','weapon','blood','spark','ground-shadow')) for p in parts), 'No new item or baked action effect'
    frame_reports.append({'index':i,'opaquePixels':len(occupied),'colors':len(colors),
      'bounds':[min(x for x,y in occupied),min(y for x,y in occupied),max(x for x,y in occupied)+1,23]})

# Same pose, same body: healing presence changes only genuine named bottle cells.
for i in range(4):
    full,empty=frames[i]['cells'],frames[i+4]['cells'];changes=[]
    for y in range(24):
        for x in range(16):
            if full[y][x]!=empty[y][x]:
                changes.append((x,y))
                assert full[y][x] and 'healing-bottle'in full[y][x][0]
                assert pixel(full[y][x])!=pixel(empty[y][x])
    assert len(changes)==8,('State-only healing slot',i,len(changes))

# Both contact resolutions come directly from scenes, never a resized raster.
for y in range(64):
    for x in range(128):
        expected=ground
        for state in range(2):
            top=4+32*state
            if top<=y<top+24:
                for column in range(5):
                    left=8+24*column
                    if left<=x<left+16:
                        cells=idle[state]['cells'] if column==0 else frames[state*4+column-1]['cells']
                        c=cells[y-top][x-left]
                        if c: expected=pixel(c)
        assert small.getpixel((x,y))==expected,('native contact',x,y)
        assert all(large.getpixel((x*8+dx,y*8+dy))==expected for dx in range(8)for dy in range(8)),('8x contact',x,y)

digest=hashlib.sha256((HERE/'north-patchbearer-motion.png').read_bytes()).hexdigest()
pixel_digest=hashlib.sha256(native.tobytes()).hexdigest()
assert manifest['nativeSHA256']==digest and manifest['decodedPixelSHA256']==pixel_digest
assert manifest['frameWidth']==16 and manifest['frameHeight']==24 and manifest['frameCount']==8
assert manifest['resource']=='Sprites/Live16x24/north-patchbearer-motion'
assert manifest['actorPivot']==[.5,0] and manifest['pixelsPerUnit']==16
assert manifest['unityExportDeferred'] is True
assert manifest['idleBorrow']=={'resource':'Sprites/Live16x24/north-works','full':7,'empty':8}
preserved=json.loads((HERE/'preserved-inputs.json').read_text())
for path,before in preserved.items():
    assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==before,('Approved input changed',path)
assert manifest['referenceSHA256']==preserved

sys.dont_write_bytecode=True
sys.path.insert(0,str(HERE.parent/'cudgel'))
from metadata_contract import parse_metadata,assert_authored_subset,authored_contract_counts
metadata=parse_metadata((HERE/'north-patchbearer-motion.png.meta').read_text())
assert len(metadata['guid'])==32 and all(c in '0123456789abcdef'for c in metadata['guid'])
assert_authored_subset({'fileFormatVersion':2,'TextureImporter':{
  'mipmaps':{'enableMipMap':0},'textureSettings':{'filterMode':0,'wrapU':1,'wrapV':1,'wrapW':1},
  'nPOTScale':0,'spritePixelsToUnits':16,'spritePivot':{'x':.5,'y':.5},
  'spriteSheet':{'sprites':[],'outline':[],'physicsShape':[],'vertices':[]},
  'platformSettings':[{'textureCompression':0}]}},metadata)
if args.check_unity:
    imported=ROOT/'Assets/Resources/Sprites/Live16x24/north-patchbearer-motion.png'
    assert imported.read_bytes()==(HERE/'north-patchbearer-motion.png').read_bytes()
    assert_authored_subset(metadata,parse_metadata(Path(str(imported)+'.meta').read_text()))
report={'passed':True,'atlasSize':[128,24],'frameCount':8,'frameReports':frame_reports,
        'sourcePixelMismatches':0,'contactPixelMismatches':0,'healingSlotDeltaPixelsPerPose':8,
        'preservedInputCount':len(preserved),'nativeSHA256':digest,'decodedPixelSHA256':pixel_digest,
        'authoredMetadataContract':authored_contract_counts(metadata),'runtimeCopyChecked':args.check_unity,
        'limits':'Source geometry/render/metadata only; parent owns owner/state admission, reactions, visibility, Unity import, normal-play motion and package acceptance.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
