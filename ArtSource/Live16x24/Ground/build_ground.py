"""Original flat native ground from editable integer quads, never raster sampling.

Blender --background --python ArtSource/Live16x24/Ground/build_ground.py
Writes ONLY this source directory. Runtime PNG copy is a separate controlled step.
"""
import hashlib
import json
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode=True
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import add_quad, camera_for, emission_material, add_label

W,H=16,24
PALETTE={
    'water':'0C4344', 'shallows':'184B45', 'bank':'294935',
    'road':'31412D', 'road-rut':'293827', 'road-wear':'3A4932',
    'earth':'45341F', 'earth-low':'3A2F1F', 'earth-clod':'4E3B24',
    'dry':'45341F', 'dry-furrow':'352B1D', 'dry-ridge':'51402A',
    'wet':'252319', 'wet-furrow':'1D1E17', 'wet-ridge':'302C1E',
}
BG='032F23'

class Frame:
    def __init__(self,name,base,use,limits,mask=None):
        self.name,self.use,self.limits,self.mask=name,use,limits,mask
        self.cells=[[('ground',base) for _ in range(W)] for _ in range(H)]
    def rect(self,part,color,x,y,width,height):
        assert color in PALETTE and 0<=x<x+width<=W and 0<=y<y+height<=H
        for yy in range(y,y+height):
            for xx in range(x,x+width): self.cells[yy][xx]=(part,color)
    def dot(self,part,color,x,y): self.rect(part,color,x,y,1,1)

def make_frames():
    frames=[]
    # Banks lie entirely inside the disclosed wet cell. The quiet center stays
    # unchanged. All edge transitions are flat color, no bevel/shadow/foam.
    # Unequal shallow depths avoid one rigid rectangular inset outline.
    horizontal=(1,1,2,2,1,1,1,2,2,2,1,1,2,1,1,1)
    vertical=(1,1,2,2,2,1,1,1,2,1,1,2,2,1,1,1,2,2,1,1,1,2,1,1)
    for mask in range(16):
        p=Frame('water-dry-edge-'+format(mask,'02x'),'water',
                'Sole current live water coating; mask bits mean cardinal disclosed dry neighbors only.',
                'No fill source, depth, obstacle, drinking quality or hidden-neighbor claim.',mask)
        for y in range(H):
            for x in range(W):
                distances=[]
                if mask&1: distances.append((y,horizontal[x]))
                if mask&2: distances.append((15-x,vertical[y]))
                if mask&4: distances.append((23-y,horizontal[x]))
                if mask&8: distances.append((x,vertical[y]))
                if any(d==0 for d,t in distances): p.dot('shallow-silt-edge','bank',x,y)
                elif any(d<=t for d,t in distances): p.dot('shallow-water-edge','shallows',x,y)
        frames.append(p)
    for i in range(2):
        p=Frame('road-wear-'+str(i),'road','Exact current RoadStone beneath other owners.',
                'Flat packed surface with embedded wear; no solid stones, repair state, resource or height.')
        # Small quiet embedded chips/ruts; no outlined blocks or raised faces.
        dark=(((2,4,4,1),(4,5,2,1),(10,11,3,1),(9,12,2,1),(1,18,3,1),(12,21,2,1)),
              ((10,3,3,1),(11,4,3,1),(3,9,4,1),(2,10,2,1),(8,17,4,1),(10,18,2,1)))
        light=(((9,3,2,1),(1,12,2,1),(6,20,3,1)),((4,4,2,1),(11,12,2,1),(2,20,3,1)))
        for x,y,w,h in dark[i]: p.rect('embedded-rut','road-rut',x,y,w,h)
        for x,y,w,h in light[i]: p.rect('flat-worn-grain','road-wear',x,y,w,h)
        frames.append(p)
    p=Frame('old-field-earth','earth','Existing fixed CropRow/RipeCropRow substrate, including covered rows.',
            'No growth timer, moisture, permission, seed count or prepared-bed promise.')
    for x,y,w,h in ((2,3,3,1),(3,4,1,2),(10,8,3,1),(9,9,2,1),(3,15,2,1),(11,20,3,1)):
        p.rect('irregular-earth','earth-low',x,y,w,h)
    for x,y,w,h in ((7,4,2,1),(2,10,2,1),(9,15,2,1),(5,21,2,1)):
        p.rect('quiet-earth-clod','earth-clod',x,y,w,h)
    frames.append(p)
    for wet in (False,True):
        name='wet' if wet else 'dry'
        p=Frame('prepared-'+name+'-furrows',name,
                'Current prepared soil; wet uses actual current owned crop moisture.',
                'No planted seed or yield promise; same geometry in both moisture states.')
        for x,y,w,h in ((4,1,1,5),(3,6,1,4),(4,10,1,6),(3,18,1,5),
                        (11,1,1,6),(10,7,1,5),(11,12,1,5),(10,19,1,4)):
            p.rect('furrow',name+'-furrow',x,y,w,h)
        for x,y,w,h in ((5,2,1,3),(4,7,1,2),(5,12,1,3),(4,20,1,2),
                        (12,3,1,3),(11,9,1,2),(12,14,1,2),(11,20,1,2)):
            p.rect('turned-earth',name+'-ridge',x,y,w,h)
        frames.append(p)
    return frames

def model(frame,scene,index,materials):
    collection=bpy.data.collections.new(f'{index:02d}-{frame.name}')
    scene.collection.children.link(collection); grouped={}
    for y,row in enumerate(frame.cells):
        for x,value in enumerate(row):
            vertices,faces=grouped.setdefault(value,([],[]))
            add_quad(vertices,faces,x,H-y-1,x+1,H-y)
    for (part,color),(vertices,faces) in grouped.items():
        mesh=bpy.data.meshes.new(frame.name+'/'+part); mesh.from_pydata(vertices,[],faces)
        mesh.materials.append(materials[color]); mesh.update()
        obj=bpy.data.objects.new(part,mesh); obj['paletteHex']=PALETTE[color]
        obj['authoring']='Original flat integer-cell geometry. One world unit per native pixel.'
        collection.objects.link(obj)
    scene.collection.children.unlink(collection)
    return collection

def instance(scene,collection,name,x,y):
    obj=bpy.data.objects.new(name,None); obj.instance_type='COLLECTION'; obj.instance_collection=collection
    obj.location=(x,y,0); scene.collection.objects.link(obj)

def background(scene,w,h):
    vertices,faces=[],[]; add_quad(vertices,faces,0,0,w,h,-1)
    mesh=bpy.data.meshes.new('Quiet source backdrop'); mesh.from_pydata(vertices,[],faces)
    mesh.materials.append(emission_material('Unchanged plain green',BG))
    scene.collection.objects.link(bpy.data.objects.new('Source backdrop only',mesh))

def main():
    OUT.mkdir(parents=True,exist_ok=True); bpy.context.preferences.filepaths.save_version=0
    atlas=bpy.context.scene; atlas.name='Native opaque 336x24 opening ground'
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    frames=make_frames(); assert len(frames)==21
    materials={k:emission_material(k+' #'+v,v) for k,v in PALETTE.items()}
    collections=[model(f,atlas,i,materials) for i,f in enumerate(frames)]
    for i,c in enumerate(collections): instance(atlas,c,'Native '+str(i),i*W,0)
    camera_for(atlas,336,24); atlas.render.filepath=str(OUT/'opening-ground.png')
    bpy.ops.render.render(write_still=True,scene=atlas.name)

    contact=bpy.data.scenes.new('Ground palette and masks direct 6x')
    cw,ch,scale=252,132,6; camera_for(contact,cw,ch,scale); background(contact,cw,ch)
    labelmat=emission_material('Source labels','B5A47E'); placements=[]
    add_label(contact,'OPENING GROUND / N1 E2 S4 W8 / NATIVE SOURCE ONLY',cw/2,ch-6,labelmat,2)
    labels=[f'water {i:02X}' for i in range(16)]+['road A','road B','old field','prepared dry','prepared wet']
    for i,c in enumerate(collections):
        x=10+(i%7)*34; top=14+(i//7)*37
        instance(contact,c,'Contact '+str(i),x,ch-top-H)
        placements.append({'frame':i,'topLeft':[x,top]})
        add_label(contact,labels[i],x+8,ch-top-H-4,labelmat,1.55)
    contact.render.filepath=str(OUT/'opening-ground-contact.png')
    bpy.ops.render.render(write_still=True,scene=contact.name)

    # Repeated/material continuity study. Only the source pond predicate and
    # lower RoadStone/field positions are reproduced, without live simulation.
    context=bpy.data.scenes.new('Source pond and ground continuity study')
    vw,vh,vscale=320,328,4; camera_for(context,vw,vh,vscale); background(context,vw,vh)
    cplacements=[]
    def place(i,x,top,label):
        instance(context,collections[i],label,x,vh-top-H)
        cplacements.append({'frame':i,'topLeft':[x,top]})
    add_label(context,'SOURCE-LAYOUT STUDY / NO ACTORS, FOG OR GAMEPLAY',vw/2,vh-9,labelmat,2.5)
    pond={(x,y) for y in range(2,9) for x in range(29,39) if ((x-34)/4.6)**2+((y-5)/3.4)**2<=1}
    assert len(pond)==51
    for x,y in sorted(pond):
        mask=sum(bit for bit,dx,dy in ((1,0,-1),(2,1,0),(4,0,1),(8,-1,0)) if (x+dx,y+dy) not in pond)
        place(mask,12+(x-28)*W,28+(y-1)*H,f'Actual source pond ({x},{y}), mask {mask}')
    for x in range(29,39):
        if x!=36: place(16+((x+9)&1),12+(x-28)*W,28+8*H,f'RoadStone ({x},9)')
    add_label(context,'ACTUAL POND / ALL-DRY DISCLOSED STUDY',116,76,labelmat,1.8)
    add_label(context,'BASIN GAP KEPT / SOURCE IS NOT A WATER SUPPLY',116,69,labelmat,1.65)
    for i,label in ((18,'old'),(19,'dry'),(20,'wet')):
        x=236+(i-18)*26
        for y in range(7): place(i,x,28+y*H,f'{label} repeated {y}')
        add_label(context,label,x+8,125,labelmat,2)
    # Mixed 2-row road strip reveals repeated marks without inventing height.
    for y in range(2):
        for x in range(15): place(16+((x+y)&1),12+x*W,264+y*H,'Road continuity')
    add_label(context,'REPEATED ROAD / SPARSE EMBEDDED WEAR, NO RAISED STONES',160,8,labelmat,2)
    context.render.filepath=str(OUT/'opening-ground-context.png')
    bpy.ops.render.render(write_still=True,scene=context.name)
    source={'schema':1,'coordinateSystem':'Top-left origin, Y down; one integer quad per native pixel; no raster source.',
            'palette':PALETTE,'frames':[{'index':i,'id':f.name,'dryEdgeMask':f.mask,'cells':f.cells} for i,f in enumerate(frames)]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    manifest={'schema':1,'artifact':'opening-ground.png','width':336,'height':24,'frameWidth':16,'frameHeight':24,
              'frameCount':21,'alpha':'Fully opaque: every frame occupies all 384 pixels.','pixelsPerUnit':16,
              'pivot':[0.5,0.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None',
              'source':'opening-ground.blend','generator':'ArtSource/Live16x24/Ground/build_ground.py',
              'resource':'Sprites/Live16x24/opening-ground','layer':'Existing full-cell background only',
              'concepts':['ArtSource/Live16x24/concepts/g2-ground-paintover-v1.png','ArtSource/Live16x24/concepts/g2-ground-parts-v1.png'],
              'capture':'Docs/Verification/Live16x24/g2-normal-start.png',
              'scope':'Source-only native reconstruction. Runtime scope and disclosure remain root-owned acceptance gates.',
              'sha256':hashlib.sha256((OUT/'opening-ground.png').read_bytes()).hexdigest(),
              'contact':{'scale':scale,'nativeSize':[cw,ch],'instances':placements},
              'context':{'scale':vscale,'nativeSize':[vw,vh],'instances':cplacements,'sourcePondCells':51,
                         'limits':'All-dry disclosed surroundings, no fog, foreground, hazard state or live action; not a gameplay capture.'},
              'frames':[{'index':i,'id':f.name,'dryEdgeMask':f.mask,'unityRect':[i*16,0,16,24],
                         'use':f.use,'limits':f.limits,'palette':sorted({PALETTE[c[1]] for row in f.cells for c in row})}
                        for i,f in enumerate(frames)]}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'opening-ground.blend'))
    print('Native ground and direct integer contact/context written:',manifest['sha256'])

if __name__=='__main__': main()
