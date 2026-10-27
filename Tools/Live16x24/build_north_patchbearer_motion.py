"""Original Patchbearer derivatives as editable native integer-cell geometry.

Blender --background --python Tools/Live16x24/build_north_patchbearer_motion.py -- --defer-unity
Always packet-only. Approved idle geometry is borrowed read-only. No Assets,
image resizing/editing, gameplay, Unity or Git operations are performed.
Pose design follows the actual empty-harness encounter and the saved concept;
the concept's extra detail, enlarged flask and compressed height are rejected.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import camera_for
from build_cindercaller import model
from build_north_plot import instance,plane
from build_east_kitchen import write_meta
from build_north_works import make_frames as approved_north_frames

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/north-patchbearer-motion'
PALETTE={'stone_dark':'3C5148','stone':'526657','linen':'B5A47E',
         'skin':'A97542','cold':'70A68A','cream':'E4D0A0'}
POSES=('walk-a','walk-b','attack','hurt')


def make_frames():
    # Start from original editable geometry, not sampled/rendered image pixels.
    # The empty body's limbs move; the eight original medicine cells are then
    # overlaid at the corresponding harness position for the full variant.
    full,empty=approved_north_frames()[7:9]
    bottle=[(x,y,value)for y,row in enumerate(full.cells)for x,value in enumerate(row)
            if value!=empty.cells[y][x]]
    assert len(bottle)==8 and all('healing-bottle'in value[0]for x,y,value in bottle)
    limbs=lambda part:any(word in part for word in ('hind','planted','reaching'))
    empties=[]
    descriptions=(
        'Rearward hind contact and near claw under the body, with the other foreclaw reaching ahead. Tall shale ridge and patched cradle stay fixed.',
        'Hind foot draws inward and the near foreclaw plants forward as the other wrist folds back. Reverses contact without whole-body bobbing.',
        'A short leading grasp extends beside the blunt face while the other foreclaw braces. Equipment-neutral attack, no baked weapon or impact.',
        'Upper body, harness and cradle recoil one cell back and down together. Bent support and tucked leading claw keep the same foot baseline.',
    )
    for pose,description in zip(POSES,descriptions):
        cells=copy.deepcopy(empty.cells)
        for row in cells:
            for x,value in enumerate(row):
                if value and limbs(value[0]): row[x]=None
        def rect(part,color,x,y,w=1,h=1):
            assert color in PALETTE and 0<=x<x+w<=16 and 0<=y<y+h<=23
            for yy in range(y,y+h):
                for xx in range(x,x+w):cells[yy][xx]=(part,color)
        if pose=='walk-a':
            rect('hind-thigh','skin',3,18,2,2);rect('hind-shin','skin',2,20,2,2)
            rect('hind-foot','skin',1,22,3);rect('hind-toe','linen',1,22)
            rect('near-forearm','skin',8,18,2,2);rect('near-wrist','skin',7,20,2,2)
            rect('near-claw','linen',6,22,3)
            rect('reaching-shoulder','skin',10,17,2,2);rect('reaching-arm','skin',12,18,2)
            rect('reaching-wrist','skin',14,19,1,2);rect('reaching-claw','linen',14,21,2)
            rect('reaching-claw','linen',15,22)
        elif pose=='walk-b':
            rect('hind-thigh','skin',4,18,2,2);rect('hind-shin','skin',5,20,2,2)
            rect('lower-plate-shadow','stone_dark',5,18)
            rect('hind-foot','skin',4,22,3);rect('hind-toe','linen',4,22)
            rect('near-forearm','skin',8,18,2,2);rect('near-wrist','skin',9,20,2,2)
            rect('near-claw','linen',10,22,3)
            rect('reaching-shoulder','skin',10,17,2,2);rect('reaching-arm','skin',11,18)
            rect('reaching-wrist','skin',12,19,2);rect('reaching-claw','linen',13,20,2)
        elif pose=='attack':
            rect('hind-thigh','skin',3,18,2,2);rect('hind-shin','skin',2,20,2,2)
            rect('hind-foot','skin',1,22,3);rect('hind-toe','linen',1,22)
            rect('planted-forearm','skin',8,18,2,3);rect('planted-wrist','skin',9,21,2)
            rect('planted-claw','linen',9,22,3)
            rect('reaching-shoulder','skin',10,17,2,2);rect('reaching-arm','skin',12,17,2)
            rect('reaching-wrist','skin',14,17)
            rect('reaching-claw-upper','linen',14,16,2)
            rect('reaching-claw-lower','linen',14,18,2)
        else:
            # Move the named torso cells together, including every patched strap
            # and cradle cell; a band left at its old coordinate would float.
            torso=[(x,y,value)for y,row in enumerate(cells)for x,value in enumerate(row)if value]
            cells=[[None]*16 for _ in range(24)]
            for x,y,value in torso:
                assert 0<=x-1<16 and y+1<23
                cells[y+1][x-1]=value
            rect('hind-thigh','skin',3,18,2,2);rect('hind-shin','skin',4,20,2,2)
            rect('hind-foot','skin',4,22,3);rect('hind-toe','linen',4,22)
            rect('planted-forearm','skin',8,18,2,2);rect('planted-wrist','skin',9,20,2,2)
            rect('planted-claw','linen',10,22,3)
            rect('reaching-shoulder','skin',10,18,2);rect('reaching-arm','skin',12,18,1,2)
            rect('reaching-wrist','skin',11,19,1,2);rect('reaching-claw','linen',10,20)
        empties.append({'pose':pose,'description':description,'cells':cells})
    frames=[]
    for state in ('full','empty'):
        for i,empty_pose in enumerate(empties):
            f=copy.deepcopy(empty_pose);pose=f['pose']
            if state=='full':
                dx,dy=(-1,1)if pose=='hurt'else(0,0)
                for x,y,value in bottle:f['cells'][y+dy][x+dx]=value
            f.update(index=len(frames),id='patchbearer-'+state+'-'+pose,
                     blueprint='MarlbackPatchbearer',healingSlot=state)
            frames.append(f)
    return frames


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--defer-unity',action='store_true',help='Explicitly request always-on packet-only export.')
    parser.parse_args(sys.argv[sys.argv.index('--')+1:]if '--'in sys.argv else [])
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    for path,digest in preserved.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    frames=make_frames()
    assert [(f['healingSlot'],f['pose'])for f in frames]==[(state,pose)for state in ('full','empty')for pose in POSES]
    idles=approved_north_frames()[7:9]
    frozen=json.loads((ROOT/'ArtSource/Live16x24/north-works/source-cells.json').read_text())
    for index,idle in enumerate(idles):
        assert json.loads(json.dumps(idle.cells))==frozen['frames'][7+index]['cells'], 'Approved source and frozen idle disagree'
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Patchbearer eight native motion derivatives'
    collections=[]
    for i,f in enumerate(frames):
        col=model(native,f['id'],f['cells'],PALETTE)
        native.collection.children.unlink(col)
        for obj in col.objects:
            obj['authoring']='Original native motion derivative from unchanged approved Patchbearer geometry.'
            obj['healingSlot']=f['healingSlot'];obj['pose']=f['pose']
        instance(native,col,f['id'],i*16,0)
        collections.append(col)
    camera_for(native,128,24)
    native.render.filepath=str(OUT/'north-patchbearer-motion.png')
    bpy.ops.render.render(write_still=True,scene=native.name)
    idle_collections=[]
    for state,idle in enumerate(idles):
        col=model(native,'Borrowed approved idle '+('full'if state==0 else 'empty'),idle.cells,PALETTE)
        native.collection.children.unlink(col);idle_collections.append(col)
    for scale,name in ((1,'contact-native'),(8,'contact')):
        scene=bpy.data.scenes.new('Patchbearer borrowed idle and derivatives '+str(scale)+'x')
        camera_for(scene,128,64,scale=scale)
        plane(scene,'Quiet contact background','032F23',0,0,128,64,-1)
        for state in range(2):
            row=[idle_collections[state]]+collections[state*4:state*4+4]
            for column,col in enumerate(row):
                instance(scene,col,('Full'if state==0 else 'Empty')+' '+str(column),8+24*column,36-32*state)
        scene.render.filepath=str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True,scene=scene.name)
    source={'schema':1,'coordinates':'16x24 top-left origin; y downward; null transparent.',
            'palette':PALETTE,'frames':frames}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    write_meta(OUT/'north-patchbearer-motion.png.meta')
    rgba={k:bytes.fromhex(v)+b'\xff'for k,v in PALETTE.items()}
    pixels=b''.join(rgba[c[1]]if c else b'\0\0\0\0'
                    for y in range(24)for f in frames for c in f['cells'][y])
    manifest={'schema':1,'frameWidth':16,'frameHeight':24,'frameCount':8,'width':128,'height':24,
        'blueprint':'MarlbackPatchbearer','frameOrder':'full WalkA WalkB Attack Hurt; empty WalkA WalkB Attack Hurt',
        'resource':'Sprites/Live16x24/north-patchbearer-motion','actorPivot':[.5,0],'pixelsPerUnit':16,
        'idleBorrow':{'resource':'Sprites/Live16x24/north-works','full':7,'empty':8},
        'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','alpha':'Binary0/255',
        'palette':PALETTE,'source':'native.blend','editableCells':'source-cells.json',
        'generator':'Tools/Live16x24/build_north_patchbearer_motion.py','native':'north-patchbearer-motion.png',
        'importMetadata':'north-patchbearer-motion.png.meta','contacts':['contact-native.png','contact.png'],
        'contactLayout':'128x64 native; full row top4, empty row top36; columns left8+24*n: borrowed idle then4derivatives.',
        'unityExportDeferred':True,'referenceSHA256':preserved,
        'nativeSHA256':hashlib.sha256((OUT/'north-patchbearer-motion.png').read_bytes()).hexdigest(),
        'decodedPixelSHA256':hashlib.sha256(pixels).hexdigest(),
        'limits':'Canonical east-facing pose derivatives; west flips and north/south retain last horizontal. Healing slot only, no total-inventory, weapon, drink or new action promise. Source evidence only, parent owns runtime and actual-game acceptance.',
        'frames':[{k:v for k,v in f.items()if k!='cells'}for f in frames]}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'native.blend'))
    print(json.dumps({'packetOnly':True,'resourceWritten':False,'frameCount':8,
       'nativeSHA256':manifest['nativeSHA256'],'decodedPixelSHA256':manifest['decodedPixelSHA256']}))


if __name__=='__main__': main()
