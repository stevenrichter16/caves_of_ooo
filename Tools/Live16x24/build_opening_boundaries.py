"""Original connected masonry from editable integer-cell Blender geometry.

Run Blender --background --python Tools/Live16x24/build_opening_boundaries.py.
Writes ArtSource/Live16x24/boundaries only. No raster is opened or sampled.
All contacts render the same meshes; their scale is an integer camera scale.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_quad, camera_for, emission_material, add_label
import build_opening_anchors as anchors
import build_opening_states as states

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/boundaries'
W, H = 16, 24
BG = '032F23'
MATERIALS = ('low-wall', 'dark-ruin', 'lit-wall')
COLORS = (('233C32', '788D73', 'A3AE8C'), ('233C32', '3C5148', '526657'), ('233C32', '526657', '70A68A'))
PALETTE = {f'{name}-{role}': color for name, values in zip(MATERIALS,COLORS)
           for role,color in zip(('mortar','body','edge'),values)}
SHAPES = ('isolated','north-end','east-end','northeast','south-end','north-south','southeast','nes-tee',
          'west-end','northwest','east-west','new-tee','southwest','nsw-tee','esw-tee','cross')

class Frame:
    def __init__(self, material, mask):
        self.material, self.mask = material, mask
        self.name = MATERIALS[material] + '-' + SHAPES[mask]
        self.cells = [[None] * W for _ in range(H)]


def silhouette(material, mask, x, y):
    # Each material keeps constant-width courses instead of a wider core at
    # every tile. Low/tall edges overlap through their shared six-pixel center.
    # Outside-cell queries continue only enabled arms; no repeated seam caps.
    lo, hi = (5,10) if material == 0 else (4,11)
    top, bottom = (9,14) if material == 0 else (8,15)
    return (lo <= x <= hi and top <= y <= bottom
            or mask & 1 and lo <= x <= hi and y <= top
            or mask & 2 and x >= hi and top <= y <= bottom
            or mask & 4 and lo <= x <= hi and y >= bottom
            or mask & 8 and x <= lo and top <= y <= bottom)


def make_frames():
    result = []
    for material, name in enumerate(MATERIALS):
        for mask in range(16):
            f = Frame(material, mask)
            for y in range(H):
                for x in range(W):
                    if not silhouette(material, mask, x, y): continue
                    above = silhouette(material, mask, x, y-1)
                    below = silhouette(material, mask, x, y+1)
                    right = silhouette(material, mask, x+1, y)
                    part, color = 'stone-mass', 'body'
                    if not below or not right: part, color = 'flat-boundary', 'mortar'
                    if not above: part, color = 'worn-cap', 'edge'
                    # Long vertical runs have staggered short opaque joints.
                    # Their edge pixels stay solid: a mortar line is never alpha.
                    if mask & 5 and y in (5, 13, 21) and 6 <= x <= 9:
                        part, color = 'course-joint', 'mortar'
                    # Horizontal stones use one inset joint; the cap remains
                    # unbroken across the tile and its matching seam ports.
                    if x == 8 and 11 <= y <= 13:
                        part, color = 'inset-joint', 'mortar'
                    # One offset mineral/cap accent along vertical courses.
                    # No glow, source point, diagonal extrusion or cast shadow.
                    if mask & 5 and x == 6 and y in (2, 9, 17):
                        part, color = 'worn-course-edge', 'edge'
                    f.cells[y][x] = (part, name+'-'+color)
            result.append(f)
    return result


def model(frame, palette, scene, name):
    collection = bpy.data.collections.new(name)
    scene.collection.children.link(collection)
    grouped = {}
    for y,row in enumerate(frame.cells):
        for x,value in enumerate(row):
            if value is None: continue
            vertices,faces = grouped.setdefault(value,([],[]))
            add_quad(vertices,faces,x,H-y-1,x+1,H-y)
    for (part,color),(vertices,faces) in grouped.items():
        mesh=bpy.data.meshes.new(name+'/'+part); mesh.from_pydata(vertices,[],faces)
        mesh.materials.append(emission_material(name+'/'+color,palette[color])); mesh.update()
        obj=bpy.data.objects.new(part,mesh); obj['authoring']='Integer-cell flat geometry; one unit per native pixel.'
        obj['paletteHex']=palette[color]; collection.objects.link(obj)
    # This collection is a reusable mesh source, not a visible duplicate.
    scene.collection.children.unlink(collection)
    return collection


def instance(scene, collection, name, x, y):
    obj=bpy.data.objects.new(name,None); obj.instance_type='COLLECTION'; obj.instance_collection=collection
    obj.location=(x,y,0); scene.collection.objects.link(obj)


def background(scene,width,height):
    vertices,faces=[],[]; add_quad(vertices,faces,0,0,width,height,-1)
    mesh=bpy.data.meshes.new(scene.name+' backdrop'); mesh.from_pydata(vertices,[],faces)
    mesh.materials.append(emission_material('Quiet ground '+BG,BG))
    scene.collection.objects.link(bpy.data.objects.new('Quiet ground (study only)',mesh))


def main():
    OUT.mkdir(parents=True,exist_ok=True); bpy.context.preferences.filepaths.save_version=0
    atlas=bpy.context.scene; atlas.name='Native 256x72 connected masonry'
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    frames=make_frames()
    collections=[model(f,PALETTE,atlas,f'{i:02d}-{f.name}') for i,f in enumerate(frames)]
    for i,c in enumerate(collections): instance(atlas,c,f'Native frame {i}',i%16*W,(2-i//16)*H)
    camera_for(atlas,256,72); atlas.render.filepath=str(OUT/'opening-boundaries.png')
    bpy.ops.render.render(write_still=True,scene=atlas.name)

    contact=bpy.data.scenes.new('Native masks and material contact')
    cw,ch,scale=344,120,4; camera_for(contact,cw,ch,scale); background(contact,cw,ch)
    textmat=emission_material('Study captions','B5A47E'); placements=[]
    add_label(contact,'OPENING MASONRY / N1 E2 S4 W8 / SOURCE ONLY',cw/2,115,textmat,2)
    for i,c in enumerate(collections):
        x=12+(i%16)*20; top=12+(i//16)*34
        instance(contact,c,f'Contact frame {i}',x,ch-top-H)
        placements.append({'frame':i,'topLeft':[x,top]})
        add_label(contact,f'{i%16:02X}',x+8,ch-top-H-4,textmat,1.7)
    add_label(contact,'LOW / DARK / MINERAL-SEAMED — NO RUNTIME INTEGRATION',cw/2,3,textmat,1.6)
    contact.render.filepath=str(OUT/'opening-boundaries-contact.png')
    bpy.ops.render.render(write_still=True,scene=contact.name)

    # Study of actual source-defined Wall positions, not a captured game scene.
    # Non-wall owners never contribute to a mask. Three accepted props are
    # shown from their unchanged source meshes to expose the critical gaps.
    context=bpy.data.scenes.new('Opening source-layout topology study')
    width,height=592,648; camera_for(context,width,height,2); background(context,width,height)
    walls={}
    def put(x,y,m): walls.setdefault((x,y),m)
    for y in range(10): put(53,y,0)
    for y in range(18,25): put(47,y,0)
    put(48,18,0)
    for x in range(20,31): put(x,11,1); put(x,22,1)
    for y in range(12,22):
        if y not in (16,17): put(23,y,1)
    for y in range(11,16): put(31,y,2)
    for y in range(17,22): put(30,y,2)
    for x in range(43,50):
        if x !=46: put(x,6,0)
    put(50,6,0); put(50,7,0)
    for y in range(14,22):
        if y not in (16,19): put(26,y,1)
    put(28,17,0) # Actual GleanersDistrict low, solid, sight-transparent crossing.
    source_layout=[]
    for (x,y),m in sorted(walls.items()):
        mask=sum(flag for flag,dx,dy in ((1,0,-1),(2,1,0),(4,0,1),(8,-1,0)) if (x+dx,y+dy) in walls)
        instance(context,collections[m*16+mask],f'Wall ({x},{y})',16+(x-20)*W,height-28-(y+1)*H)
        source_layout.append({'blueprint':'Wall','x':x,'y':y,'material':m,'mask':mask})
    borrowed=[]
    for module,index,x,y,label in ((anchors,10,31,10,'Quartz — excluded'),(anchors,10,31,22,'Quartz — excluded'),
                                 (anchors,5,26,19,'Beam — excluded'),(states,2,46,6,'Open wicket — excluded')):
        f=module.make_frames()[index]; c=model(f,module.PALETTE,context,'Unchanged source preview '+label)
        instance(context,c,label,16+(x-20)*W,height-28-(y+1)*H)
        borrowed.append({'generator':module.__name__, 'frame':index,'x':x,'y':y,'suppliesConnector':False})
    add_label(context,'SOURCE-LAYOUT STUDY / WALLS + THREE EXISTING PROP TYPES',width/2,height-11,textmat,3)
    add_label(context,'GLADE x20..54 y0..24 / OTHER CONTENT AND FOG OMITTED / NOT GAMEPLAY',width/2,11,textmat,2.8)
    context.render.filepath=str(OUT/'opening-boundaries-context.png')
    bpy.ops.render.render(write_still=True,scene=context.name)

    source={'schema':1,'coordinateSystem':'Top-left image cells, y downward; flat XY mesh quads.',
            'palette':PALETTE,'frames':[{'index':i,'id':f.name,'material':f.material,'mask':f.mask,'cells':f.cells} for i,f in enumerate(frames)]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    manifest={'schema':1,'artifact':'opening-boundaries.png','nativeSize':[256,72],'frames':[],
              'frameSize':[16,24],'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None',
              'maskBits':{'N':1,'E':2,'S':4,'W':8},'index':'material * 16 + mask',
              'scope':'Offline optional opening Wall art only; not imported or runtime/play accepted.',
              'conceptReferences':['ArtSource/Live16x24/concepts/g2-boundary-paintover-v1.png','ArtSource/Live16x24/concepts/g2-boundary-parts-v1.png'],
              'sourceCapture':'Docs/Verification/Live16x24/native-states-start.png',
              'generator':'Tools/Live16x24/build_opening_boundaries.py','editableSource':'opening-boundaries.blend',
              'contact':{'scale':scale,'nativeSize':[cw,ch],'background':BG,'instances':placements},
              'context':{'scope':'Source layout illustration, all walls shown; no disclosure or gameplay claim.',
                         'walls':source_layout,'unchangedSourcePreview':borrowed},
              'sha256':hashlib.sha256((OUT/'opening-boundaries.png').read_bytes()).hexdigest()}
    for i,f in enumerate(frames):
        ink=[(x,y) for y,row in enumerate(f.cells) for x,c in enumerate(row) if c]
        manifest['frames'].append({'index':i,'id':f.name,'blueprint':'Wall','visualID':'reference-glade-'+MATERIALS[f.material],
                                  'mask':f.mask,'material':f.material,'unityRect':[i%16*16,(2-i//16)*24,16,24],
                                  'imageRect':[i%16*16,i//16*24,16,24], 'occupiedPixels':len(ink),
                                  'palette':list(COLORS[f.material]), 'limits':'No quartz, road, rubble, door or beam routing. Low crossing blocks walking; its Wall tag is absent only for sight.'})
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'opening-boundaries.blend'))
    print('Original boundary geometry source written:',OUT)

if __name__=='__main__': main()
