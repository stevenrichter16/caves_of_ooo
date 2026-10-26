"""Native connected copper and low peat, authored as editable planar integer cells.

Run with Blender --background --python this-file. No reference raster is read,
sampled, transformed or traced. Approved source helpers are imported read-only.
"""
import hashlib
import importlib.util
import json
import shutil
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_quad, add_label, camera_for, emission_material
from build_north_plot import instance, plane
from build_east_kitchen import frames as east_frames, write_meta, P as EAST_PALETTE

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/nella-corner'
RESOURCE = ROOT / 'Assets/Resources/Sprites/Live16x24/nella-corner.png'
PALETTE = {key: EAST_PALETTE[key] for key in
           ('copper_dark', 'copper', 'copper_edge', 'patina', 'ink', 'wood_dark', 'leaf_dark')}
PRESERVE = ('Assets/Resources/Sprites/Live16x24/east-kitchen.png',
            'Assets/Resources/Sprites/Live16x24/opening-ground.png',
            'Tools/Live16x24/build_east_kitchen.py',
            'ArtSource/Live16x24/east-kitchen/source-cells.json')


class Frame:
    def __init__(self, name, blueprint, mask=None):
        self.name, self.blueprint, self.mask = name, blueprint, mask
        self.cells = [[None]*16 for _ in range(24)]

    def rect(self, part, color, x, y, w, h):
        assert color in PALETTE and 0 <= x < x+w <= 16 and 0 <= y < y+h <= 24
        for yy in range(y,y+h):
            for xx in range(x,x+w): self.cells[yy][xx] = (part,color)
        return self


def make_frames():
    isolated = Frame('pipe-isolated-approved-elbow', 'CopperPipe', 0)
    isolated.cells = [list(row) for row in east_frames()[15].cells]
    frames = [isolated]
    for mask in range(1,16):
        f = Frame('pipe-neighbors-'+format(mask,'02x'), 'CopperPipe', mask)
        # Match cardinal four-pixel bands exactly at every joined edge.
        # These are flat material strips, not a rendered bevel or cast shadow.
        occupied = {(x,y) for y in range(10,14) for x in range(6,10)}
        if mask & 1: occupied.update((x,y) for y in range(0,10) for x in range(6,10))
        if mask & 2: occupied.update((x,y) for y in range(10,14) for x in range(10,16))
        if mask & 4: occupied.update((x,y) for y in range(14,24) for x in range(6,10))
        if mask & 8: occupied.update((x,y) for y in range(10,14) for x in range(0,6))
        for x,y in occupied:
            f.cells[y][x] = ('copper-body','copper')
        for x,y in occupied:
            # Boundary-only tones keep turns and junctions as one copper form.
            if (x+1,y) not in occupied or (x,y+1) not in occupied:
                f.cells[y][x] = ('worn-dark-edge','copper_dark')
            if (x-1,y) not in occupied or (x,y-1) not in occupied:
                f.cells[y][x] = ('worn-copper-edge','copper_edge')
        # Fixed edge colors make ANY N/S or E/W pair join without a seam.
        for y in (0,23):
            if (y == 0 and mask & 1) or (y == 23 and mask & 4):
                for x,color in ((6,'copper_edge'),(7,'copper'),(8,'copper'),(9,'copper_dark')):
                    f.cells[y][x] = ('cardinal-seam',color)
        for x in (0,15):
            if (x == 0 and mask & 8) or (x == 15 and mask & 2):
                for y,color in ((10,'copper_edge'),(11,'copper'),(12,'copper'),(13,'copper_dark')):
                    f.cells[y][x] = ('cardinal-seam',color)
        # Single restraint/coupling per owner; no valves, open mouth, drops or glow.
        if mask in (1,4,5):
            f.rect('dull-copper-collar','copper_dark',5,11,6,2)
            f.rect('quiet-patina','patina',6,11,4,1)
            f.rect('worn-union','copper',6,12,4,1)
        elif mask in (2,8,10):
            f.rect('dull-copper-collar','copper_dark',7,9,2,6)
            f.rect('quiet-patina','patina',7,10,1,4)
            f.rect('worn-union','copper',8,10,1,4)
        else:
            f.rect('quiet-patina','patina',7,11,2,2)
        frames.append(f)

    peat = Frame('low-fibrous-peat-mat', 'PeatBog')
    # Two low unequal masses with short attached fibers. The real water ground
    # remains visible through >80% of the frame. No plant stalk or loose loot.
    for x,y,w in ((3,12,4),(2,13,7),(2,14,7),(3,15,6),(4,16,3),
                  (10,18,3),(8,19,6),(8,20,6),(10,21,3)):
        peat.rect('dark-peat-mass','ink',x,y,w,1)
    peat.rect('left-low-mass','wood_dark',3,13,4,2)
    peat.rect('muted-peat-skin','leaf_dark',2,13,2,2)
    peat.rect('short-horizontal-fiber','wood_dark',7,14,2,1)
    peat.rect('short-horizontal-fiber','wood_dark',6,15,3,1)
    peat.rect('right-low-mass','wood_dark',10,19,3,2)
    peat.rect('muted-peat-skin','leaf_dark',9,19,2,1)
    peat.rect('short-horizontal-fiber','wood_dark',13,20,2,1)
    frames.append(peat)
    assert len(frames) == 17
    return frames


def model(frame, materials):
    collection = bpy.data.collections.new(frame.name)
    groups = {}
    for y,row in enumerate(frame.cells):
        for x,cell in enumerate(row):
            if cell is None: continue
            vertices,faces = groups.setdefault(cell,([],[]))
            add_quad(vertices,faces,x,23-y,x+1,24-y)
    for (part,color),(vertices,faces) in sorted(groups.items()):
        mesh = bpy.data.meshes.new(frame.name+'/'+part)
        mesh.from_pydata(vertices,[],faces); mesh.materials.append(materials[color]); mesh.update()
        obj = bpy.data.objects.new(part,mesh)
        obj['paletteHex'] = PALETTE[color]
        obj['authoring'] = 'Editable named integer-cell planar geometry; no raster samples.'
        collection.objects.link(obj)
    return collection


def render(scene, name):
    scene.render.filepath = str(OUT/name)
    bpy.ops.render.render(write_still=True,scene=scene.name)


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    preserved = {p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in PRESERVE}
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {k:emission_material(k+' #'+v,v) for k,v in PALETTE.items()}
    collections = [model(f,materials) for f in frames]
    atlas = bpy.context.scene; atlas.name = 'Nella corner native272x24'
    for i,col in enumerate(collections): instance(atlas,col,frames[i].name,i*16,0)
    camera_for(atlas,272,24); render(atlas,'native.png')

    contact = bpy.data.scenes.new('Native owner and topology contact')
    camera_for(contact,176,168)
    plane(contact,'Approved quiet green','032F23',0,0,176,168,-1)
    labels = emission_material('Quiet labels','B5A47E')
    placements = []
    for i,col in enumerate(collections):
        left,top = 8+i%5*32,12+i//5*38
        placements.append({'frame':i,'topLeft':[left,top]})
        instance(contact,col,frames[i].name,left,168-top-24)
        add_label(contact,format(i,'02d')+' '+('peat' if i==16 else format(i,'04b')),left+8,168-top-28,labels,1.5)
    add_label(contact,'SOURCE ONLY / N1 E2 S4 W8',88,162,labels,1.8)
    render(contact,'contact-native.png')
    contact.render.resolution_x = 176*6; contact.render.resolution_y = 168*6
    render(contact,'contact-6x.png')

    # Same accepted ground source geometry provides context, without editing it
    # or sampling its rendered PNG. This study is not an actual game capture.
    spec = importlib.util.spec_from_file_location('nella_preview_ground',ROOT/'ArtSource/Live16x24/Ground/build_ground.py')
    ground = importlib.util.module_from_spec(spec); spec.loader.exec_module(ground)
    context = bpy.data.scenes.new('Source conductive bog corner study')
    camera_for(context,96,192)
    plane(context,'Approved quiet green context','032F23',0,0,96,192,-2)
    ground_materials = {k:emission_material('Existing ground '+k+' #'+v,v) for k,v in ground.PALETTE.items()}
    ground_frames = ground.make_frames()
    for frame,top in ((4,12),(5,36),(5,60),(5,84),(1,108)):
        instance(context,collections[frame],'Observed pipe topology '+str(top),32,192-top-24)
    for mask,left,top in ((11,32,132),(12,32,156),(7,48,156)):
        col = ground.model(ground_frames[mask],context,mask,ground_materials)
        instance(context,col,'Existing water mask '+str(mask),left,192-top-24,-1)
        instance(context,collections[16],'Neutral peat owner '+str(mask),left,192-top-24)
    render(context,'corner-native.png')
    context.render.resolution_x = 96*4; context.render.resolution_y = 192*4
    render(context,'corner-4x.png')
    source = {'schema':1,'coordinates':'16x24 top-left origin; y increases down; null transparent.',
              'palette':PALETTE,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'mask':f.mask,'cells':f.cells}
                                         for i,f in enumerate(frames)]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    # Source rebuilds must not interrupt root's active Unity test/import cycle.
    # Only an explicitly coordinated transfer writes under Assets.
    copied_resource = '--copy-resource' in sys.argv
    if copied_resource:
        shutil.copyfile(OUT/'native.png',RESOURCE); write_meta(RESOURCE.with_suffix('.png.meta'))
    manifest = {'schema':1,'frameCount':17,'width':272,'height':24,'frameWidth':16,'frameHeight':24,
                'resource':'Sprites/Live16x24/nella-corner','source':'editable.blend','editableCells':'source-cells.json',
                'generator':'Tools/Live16x24/build_nella_corner.py','pixelsPerUnit':16,'pivot':[.5,.5],
                'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','alpha':'binary0/255',
                'nativeSHA256':hashlib.sha256((OUT/'native.png').read_bytes()).hexdigest(),
                'preservedApprovedFiles':preserved,'contactPlacements':placements,
                'baseline':'Docs/Verification/Live16x24/current-scene-49-nella-corner-before-2x.png',
                'concept':'ArtSource/Live16x24/nella-corner/concept.png',
                'conceptPrompt':'ArtSource/Live16x24/nella-corner/concept-prompt.txt',
                'mapping':'Frames0..15 exact CopperPipe cardinal occupied adjacency N1/E2/S4/W8. Frame16 exact PeatBog neutral low foreground; existing ground owns live water.',
                'reusedSource':'Mask0 exact build_east_kitchen.frames()[15]; context water exact Ground/build_ground.make_frames()[11,12,7].',
                'rejected':['gradients','soft alpha','glow','cast shadows','extra glyph remnants','flow','discharge','irrigation','active charge','fuel or stock quantities','new vegetation'],
                'limits':'Source/contact geometry only. Root owns Unity import, exact disclosure/current-owner/topology gates, actual hazard semantics and gameplay acceptance.'}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'))
    print('Wrote Nella corner source:',OUT/'native.png')
    print('Runtime transfer:',str(RESOURCE) if copied_resource else 'held; pass -- --copy-resource when root releases import window')


if __name__ == '__main__': main()
