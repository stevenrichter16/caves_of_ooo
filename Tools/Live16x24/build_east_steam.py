"""Original steam tile-state cue, authored as editable native integer planes.

Blender --background --python Tools/Live16x24/build_east_steam.py -- --defer-unity
Packet-only output, even when the flag is omitted. No Assets writes, raster
sampling/editing, simulation changes, new vent body, or animation sequence.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import camera_for
from build_cindercaller import model
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/east-steam'
PALETTE = {'muted':'3C5148','body':'526657','edge':'7D8E71'}


def make_cells():
    cells=[[None]*16 for _ in range(24)]
    # Original quiet asymmetrical curl; exact square cells, not extracted or
    # shrunk concept pixels. Narrow bends leave most of the footprint clear.
    rows=(
        ('upper-tail',3,((10,'muted'),)),
        ('upper-tail',4,((10,'body'),)),
        ('upper-tail',5,((9,'body'),(10,'edge'),(11,'muted'))),
        ('middle-curl',6,((8,'body'),(9,'edge'))),
        ('middle-curl',7,((7,'body'),(8,'edge'))),
        ('middle-curl',8,((7,'body'),)),
        ('middle-curl',9,((7,'muted'),(8,'body'))),
        ('middle-curl',10,((8,'body'),(9,'muted'))),
        ('lower-thread',11,((7,'body'),(8,'edge'))),
        ('lower-thread',12,((5,'muted'),(6,'body'),(7,'edge'))),
        ('lower-thread',13,((5,'body'),(6,'muted'))),
    )
    for part,y,values in rows:
        for x,color in values:
            assert color in PALETTE and 0<=x<16 and 0<=y<15
            cells[y][x]=[part,color]
    return cells


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--defer-unity',action='store_true',help='Explicitly request the always-on packet-only export.')
    parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    OUT.mkdir(parents=True,exist_ok=True)
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    for path,digest in preserved.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Steam tile mark native16x24'
    cells=make_cells()
    wisp=model(native,'Steam tile mark / original sparse wisp',cells,PALETTE)
    for obj in wisp.objects:
        obj['authoring']='Original integer-cell tile-state cue; no vent-body or heat/damage meaning.'
    camera_for(native,16,24)
    native.render.filepath=str(OUT/'native.png')
    bpy.ops.render.render(write_still=True,scene=native.name)
    approved=json.loads((ROOT/'ArtSource/Live16x24/east-kitchen/source-cells.json').read_text())
    grate=approved['frames'][16]
    assert grate['id']=='neutral-vent' and grate['blueprint']=='SteamVent'
    for scale,name in ((1,'contact-native'),(8,'contact')):
        scene=bpy.data.scenes.new('Steam cue over unchanged grate '+str(scale)+'x')
        camera_for(scene,32,32,scale=scale)
        plane(scene,'Approved quiet Spread ground','032F23',0,0,32,32,-1)
        instance(scene,wisp,'New sparse tile-state cue',8,4)
        original=model(scene,'Unchanged approved SteamVent context',grate['cells'],approved['palette'])
        scene.collection.children.unlink(original)
        instance(scene,original,'Existing grille source unchanged',8,4)
        scene.render.filepath=str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True,scene=scene.name)
    source={'schema':1,'coordinates':'16x24 top-left origin, y downward; null transparent.',
            'palette':PALETTE,'frames':[{'index':0,'id':'steam-tile-mark',
            'ownerKind':'tile-state','cloud':'steam','cells':cells}]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    write_meta(OUT/'native.png.meta')
    points=[(x,y) for y,row in enumerate(cells) for x,cell in enumerate(row) if cell]
    rgba={name:bytes.fromhex(value)+b'\xff' for name,value in PALETTE.items()}
    pixels=b''.join(rgba[c[1]] if c else b'\0\0\0\0' for row in cells for c in row)
    manifest={
        'schema':1,'frameWidth':16,'frameHeight':24,'frameCount':1,
        'ownerKind':'tile-state','cloud':'steam','state':'Cloud=steam and positive CloudTurns; runtime admission belongs to root.',
        'footprintCells':[1,1],'pivot':[.5,.5],'pixelsPerUnit':16,
        'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','alpha':'Binary0/255',
        'palette':PALETTE,'source':'native.blend','editableCells':'source-cells.json',
        'generator':'Tools/Live16x24/build_east_steam.py','native':'native.png',
        'importMetadata':'native.png.meta','contacts':['contact-native.png','contact.png'],
        'resource':'Sprites/Live16x24/east-steam','unityExportDeferred':True,
        'boundsTopLeftExclusive':[min(x for x,y in points),min(y for x,y in points),
                                  max(x for x,y in points)+1,max(y for x,y in points)+1],
        'opaquePixels':len(points),'bottomNineRowsTransparent':True,
        'nativeSHA256':hashlib.sha256((OUT/'native.png').read_bytes()).hexdigest(),
        'decodedPixelSHA256':hashlib.sha256(pixels).hexdigest(),'referenceSHA256':preserved,
        'parts':['upper-tail','middle-curl','lower-thread'],
        'contactContext':{'source':'ArtSource/Live16x24/east-kitchen/source-cells.json','frame':16,'meaning':'Unchanged approved grille shown only in contacts, absent from new runtime sprite.'},
        'paletteProvenance':'Existing approved east-kitchen stone_dark3C5148, stone526657, stone_light7D8E71; explicit constants, never raster sampled.',
        'rejected':['sixth invented wisp','tonic replacement','grate repaint','bevel','background or UI changes','soft alpha','glow','smoke blanket','heat/damage or source provenance promise'],
        'limits':'One static tile-state cue, not SteamVent or SteamCloud entity art. No heat, damage, obscured sight, emission timer, inventory subject or quantity promise. Source only; root owns live state/priority/visibility, import and gameplay acceptance.',
    }
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'native.blend'))
    print(json.dumps({'packetOnly':True,'resourceWritten':False,'opaquePixels':len(points),
                     'nativeSHA256':manifest['nativeSHA256'],'decodedPixelSHA256':manifest['decodedPixelSHA256']}))


if __name__=='__main__':main()
