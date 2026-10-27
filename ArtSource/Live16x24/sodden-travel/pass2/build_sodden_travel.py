"""Original Sodden static materials, authored as editable native integer quads.

Blender --background --python Tools/Live16x24/build_sodden_travel.py
Writes only ArtSource/Live16x24/sodden-travel. Never reads raster pixels,
imports Assets, changes approved sources or adds simulation/animation.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/sodden-travel'
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import camera_for, add_label, emission_material
from build_cindercaller import model
from build_north_plot import instance, plane

PALETTE = {
    'board-shadow':'322E25', 'board-dark':'504B3B', 'board':'726C59',
    'board-worn':'88806B', 'board-grain':'635D4B',
    'mire':'252C1B', 'mire-deep':'1D2519', 'mire-film':'3A3C20',
    'mire-ochre':'5A5226', 'mire-glint':'6F6939',
    'peat-edge':'211F1B', 'peat':'332F25', 'peat-layer':'3D3829',
    'peat-fiber':'504B35', 'peat-moss':'465332',
    'bark-dark':'37413B', 'bark':'68756A', 'bark-light':'8C9380', 'broken-tip':'A6AB90',
}


class Frame:
    def __init__(self, name, blueprint, description, **state):
        self.name,self.blueprint,self.description,self.state = name,blueprint,description,state
        self.cells = [[None]*16 for _ in range(24)]

    def rect(self, part, color, x, y, width=1, height=1):
        assert color in PALETTE and 0 <= x < x+width <= 16 and 0 <= y < y+height <= 24
        for yy in range(y,y+height):
            for xx in range(x,x+width):self.cells[yy][xx] = (part,color)
        return self

    def dot(self, part, color, x, y):
        return self.rect(part,color,x,y)


def duckboard(mask):
    f = Frame('duckboard-'+format(mask,'02x'),'Duckboard',
              'Narrow flat weathered timber; transverse plank joints and short grain. No rail, stone blocks, posts or extra route.',neighborMask=mask)
    def shape(x,y):
        return (4 <= x <= 11 and 8 <= y <= 15 or mask&1 and 4 <= x <= 11 and y <= 11
                or mask&2 and x >= 8 and 8 <= y <= 15 or mask&4 and 4 <= x <= 11 and y >= 12
                or mask&8 and x <= 7 and 8 <= y <= 15)
    for y in range(24):
        for x in range(16):
            if not shape(x,y):continue
            horizontal = bool(mask&10) and (not mask&5 or x < 4 or x > 11)
            cross,along = (y-8,x) if horizontal else (x-4,y)
            edge = any(not (0 <= xx < 16 and 0 <= yy < 24 and shape(xx,yy))
                       for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
            part,color = 'weathered-plank','board'
            if along%4==3:part,color = 'transverse-plank-joint','board-shadow'
            elif edge:part,color = 'flat-cut-board-edge','board-dark'
            elif along%4==1 and 1 <= cross <= 5:part,color = 'weathered-wood-grain','board-grain'
            elif along%4==2 and cross==4 and along//4%2==0:part,color = 'short-lengthwise-wood-split','board-dark'
            elif along%8==0 and cross in (1,2):part,color = 'small-worn-plank-end','board-worn'
            f.dot(part,color,x,y)
    # Exact opposite edge profiles keep adjoining paths seamless. They are
    # cut wood within the walking surface, never separate rails or supports.
    colors=('board-dark','board','board','board-grain','board','board','board','board-dark')
    for y,bit in ((0,1),(23,4)):
        if mask&bit:
            for x,color in zip(range(4,12),colors):f.dot('board-join',color,x,y)
    for x,bit in ((0,8),(15,2)):
        if mask&bit:
            for y,color in zip(range(8,16),colors):f.dot('board-join',color,x,y)
    return f


def mire(depleted):
    f = Frame('mire-depleted-wet-film' if depleted else 'mire-filled','MirePool',
              'Continuous hazardous dark olive/ochre wet sludge; source stock does not determine the live coating. No dry bed or safe-ground promise.',
              stockState='depleted-wet-film' if depleted else 'filled',stillWet=True)
    f.rect('continuous-wet-mire','mire',0,0,16,24)
    if not depleted:
        # Two broad quiet slicks, not repeated wave glyphs or little stones.
        for x,y,w,h in ((2,5,6,2),(3,7,7,2),(4,9,5,1),(10,15,4,2),(8,17,5,2)):
            f.rect('suspended-peat-slick','mire-film',x,y,w,h)
        for x,y,w,h in ((3,6,4,1),(5,7,4,2),(6,9,2,1),(10,16,3,2)):
            f.rect('oxidized-slick','mire-ochre',x,y,w,h)
        f.rect('wet-surface-glint','mire-glint',3,5,3).rect('wet-surface-glint','mire-glint',10,15,2)
        f.rect('quiet-deep-wet','mire-deep',2,15,3,2).rect('quiet-deep-wet','mire-deep',3,17,3)
    else:
        # The drained stock reveals more dark wet film, not soil, cracks,
        # grass or transparency. Thin level glints keep that truth legible.
        for x,y,w,h in ((2,8,11,1),(5,9,8,1),(2,17,12,1),(3,18,8,1)):
            f.rect('thin-hazardous-wet-film','mire-film',x,y,w,h)
        f.rect('remaining-oxidized-film','mire-ochre',6,8,5).rect('remaining-oxidized-film','mire-ochre',3,17,5)
        f.rect('wet-surface-glint','mire-glint',2,7,4).rect('wet-surface-glint','mire-glint',9,16,4)
        f.rect('exposed-dark-wet-sludge','mire-deep',3,11,10,3).rect('exposed-dark-wet-sludge','mire-deep',5,14,7)
    return f


def peat(variant):
    f = Frame('cut-peat-bank-'+str(variant),'PeatBank',
              'Solid broad cut peat: compressed dark layers, short embedded root fibers and a muted moss lip. Not a walkable log, fence or masonry wall.',variant=variant)
    def body(x,y):
        return True
    for y in range(24):
        for x in range(16):
            if body(x,y):f.dot('compressed-peat-face','peat',x,y)
    # Broad uneven strata avoid rectangular stone blocks and framed planks.
    bands = (((2,5,7,2),(7,6,7,2),(1,12,6,2),(6,13,9,2),(3,19,6,2),(9,20,5,1)),
             ((1,6,5,2),(6,5,8,2),(2,13,8,2),(10,12,5,2),(1,20,8,1),(9,18,6,2)))
    for x,y,w,h in bands[variant]:f.rect('cut-peat-layer','peat-layer',x,y,w,h)
    folds = (((3,8,6),(9,9,5),(2,15,8),(10,16,3)),((2,9,7),(9,8,5),(1,16,5),(6,15,7)))
    for x,y,w in folds[variant]:f.rect('compressed-layer-shadow','peat-edge',x,y,w)
    roots = (((3,5),(4,6),(5,6),(10,13),(11,14),(12,14),(5,19),(6,20)),
             ((10,5),(10,6),(9,7),(4,13),(5,14),(6,14),(11,18),(12,19)))
    for x,y in roots[variant]:f.dot('embedded-root-fiber','peat-fiber',x,y)
    moss = (((2,2,5),(4,1,4),(9,2,4)),((2,2,3),(5,1,6),(10,2,4)))
    for x,y,w in moss[variant]:f.rect('muted-moss-lip','peat-moss',x,y,w)
    # Both variants share full-width unbroken joins. No rounded individual
    # caps: a row or column must read as a continuous cutbank, not a fence.
    for y in (0,23):
        for x in range(16):f.dot('solid-peat-join','peat',x,y)
    for x in (0,15):
        for y in range(24):f.dot('solid-peat-join','peat',x,y)
    return f


def tree(variant):
    f = Frame('bare-dead-tree-'+str(variant),'DeadTree',
              'Compact ash-grey split trunk with two angular bare forks and grounded roots. Solid dead wood; no leafy canopy, fruit, harvest, flame or cross-cell branch.',variant=variant)
    if variant==0:
        f.rect('dead-trunk','bark-dark',7,3,2,8).rect('dead-trunk','bark-dark',6,11,4,11)
        f.rect('dead-trunk','bark',7,4,1,8).rect('dead-trunk','bark',7,12,2,10)
        f.rect('split-bark','bark-light',7,7,1,8).rect('dark-trunk-split','bark-dark',8,17,1,4)
        for x,y,w,h in ((2,6,1,2),(3,7,1,2),(4,8,2,2),(5,10,2,2),(6,11,2,2)):
            f.rect('bare-left-fork','bark-dark',x,y,w,h)
        for x,y in ((2,6),(3,7),(4,8),(5,9),(5,10),(6,11)):f.dot('bare-left-fork','bark',x,y)
        for x,y,w,h in ((13,8,1,2),(12,9,1,2),(11,10,2,2),(10,11,2,2),(9,12,2,2)):
            f.rect('bare-right-fork','bark-dark',x,y,w,h)
        for x,y in ((13,8),(12,9),(11,10),(10,11),(9,12)):f.dot('bare-right-fork','bark-light',x,y)
        f.rect('root-flare','bark-dark',5,21,6,2).rect('root-flare','bark',6,21,4,2)
        f.dot('root-flare','bark-light',4,22).dot('root-flare','bark',11,22)
        f.dot('snapped-dead-tip','broken-tip',7,3)
    else:
        f.rect('dead-trunk','bark-dark',7,4,2,8).rect('dead-trunk','bark-dark',6,12,4,10)
        f.rect('dead-trunk','bark',7,5,1,7).rect('dead-trunk','bark',7,12,2,10)
        f.rect('split-bark','bark-light',7,9,1,9).rect('dark-trunk-split','bark-dark',8,18,1,3)
        for x,y,w,h in ((1,10,2,1),(2,11,2,2),(3,12,2,2),(5,13,2,2)):
            f.rect('bare-left-fork','bark-dark',x,y,w,h)
        for x,y in ((1,10),(2,11),(3,12),(4,13),(5,13)):f.dot('bare-left-fork','bark-light',x,y)
        for x,y,w,h in ((13,4,1,2),(12,5,1,3),(11,7,2,2),(10,9,2,2),(9,11,2,2)):
            f.rect('bare-right-fork','bark-dark',x,y,w,h)
        for x,y in ((13,4),(12,5),(12,6),(11,7),(11,8),(10,9),(10,10),(9,11)):f.dot('bare-right-fork','bark',x,y)
        f.rect('root-flare','bark-dark',5,21,7,2).rect('root-flare','bark',6,21,4,2)
        f.dot('root-flare','bark-light',4,22).dot('root-flare','bark',12,22)
        f.dot('snapped-dead-tip','broken-tip',7,4)
    return f


def main():
    preserved=json.loads((OUT/'preserved-inputs.json').read_text())
    def check_preserved():
        for path,before in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==before,path
    check_preserved()
    frames=[duckboard(mask) for mask in range(16)]+[mire(False),mire(True)]+[peat(i) for i in range(2)]+[tree(i) for i in range(2)]
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Sodden static native 352x24'
    collections=[]
    for index,f in enumerate(frames):
        col=model(native,f'{index:02d} '+f.name,f.cells,PALETTE);native.collection.children.unlink(col)
        for obj in col.objects:obj['authoring']='Original static Sodden material; native integer cells, no raster input.'
        instance(native,col,f'{index:02d}',index*16,0);collections.append(col)
    camera_for(native,352,24);native.render.filepath=str(OUT/'sodden-travel.png')
    bpy.ops.render.render(write_still=True,scene=native.name)
    width,height=198,210
    placements=[{'frame':i,'topLeft':[9+(i%8)*23,14+(i//8)*36]} for i in range(22)]
    # A source-only join study, deliberately separate from actual gameplay.
    study=((16,2,10,12,18),(17,16,16,5,19),(16,17,16,1,18))
    for row,line in enumerate(study):
        for column,index in enumerate(line):placements.append({'frame':index,'topLeft':[12+16*column,130+24*row]})
    placements += [{'frame':18+i%2,'topLeft':[124,130+24*i]} for i in range(3)]
    placements += [{'frame':20,'topLeft':[153,130]},{'frame':21,'topLeft':[153,172]}]
    for scale,name in ((1,'contact-native'),(6,'contact')):
        scene=bpy.data.scenes.new('Sodden source-only contact '+str(scale)+'x')
        camera_for(scene,width,height,scale);plane(scene,'Contact-only dark green','032F23',0,0,width,height,-1)
        labels=emission_material('Quiet source labels','B5A47E')
        add_label(scene,'SODDEN / STATIC SOURCE / 16x24',width/2,height-5,labels,2)
        for p in placements:instance(scene,collections[p['frame']],str(p),p['topLeft'][0],height-p['topLeft'][1]-24)
        for i,p in enumerate(placements[:22]):
            text=('B'+format(i,'02X')) if i<16 else ('M full','M film','Peat A','Peat B','Dead A','Dead B')[i-16]
            add_label(scene,text,p['topLeft'][0]+8,height-p['topLeft'][1]-28,labels,1.5)
        add_label(scene,'WOOD JOINS / WET MIRE',53,height-125,labels,1.5)
        add_label(scene,'CUT PEAT / BARE WOOD',149,height-125,labels,1.5)
        scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
    source={'schema':1,'coordinateSystem':'Top-left origin, y downward; named editable integer-cell quads; null is transparent.',
            'palette':PALETTE,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,**f.state,'cells':f.cells} for i,f in enumerate(frames)]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    manifest={'schema':1,'artifact':'sodden-travel.png','resource':'Sprites/Live16x24/sodden-travel',
              'width':352,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':22,'pixelsPerUnit':16,'pivot':[.5,.5],
              'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None',
              'source':'sodden-travel.blend','generator':'Tools/Live16x24/build_sodden_travel.py',
              'neighborMaskBits':{'north':1,'east':2,'south':4,'west':8},'unityExportDeferred':True,
              'scope':'Original static source only; no runtime import, state/owner admission, scene fullness or gameplay acceptance.',
              'contact':{'nativeSize':[width,height],'background':'032F23','scales':[1,6],'instances':placements},
              'sha256':hashlib.sha256((OUT/'sodden-travel.png').read_bytes()).hexdigest(),
              'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,**f.state,'description':f.description,'unityRect':[i*16,0,16,24]} for i,f in enumerate(frames)]}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sodden-travel.blend'))
    check_preserved();print(json.dumps({'packetOnly':True,'frames':22,'size':[352,24],'sha256':manifest['sha256']}))


if __name__=='__main__':main()
