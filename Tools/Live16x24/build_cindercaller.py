"""Original exact 16x24 marlback body, authored as editable integer-cell quads.

Blender --background --python Tools/Live16x24/build_cindercaller.py.
Writes only cindercaller source and its owned runtime PNG/meta. The imagegen
concept is visual guidance, never raster input. Approved family art is read-only.
"""
import hashlib
import json
import shutil
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import add_quad, add_label, camera_for, emission_material
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/cindercaller'
RES=ROOT/'Assets/Resources/Sprites/Live16x24/cindercaller.png'
P={'ink':'342E25','stone_dark':'3C5148','stone':'526657','stone_light':'7D8E71',
   'tawny':'A97542','claw':'BCA576','rust':'9B442B'}

def cindercaller_cells():
    cells=[[None]*16 for _ in range(24)]
    def rect(part,color,x,y,w=1,h=1):
        assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
        for yy in range(y,y+h):
            for xx in range(x,x+w):cells[yy][xx]=(part,color)
    # A narrow smooth cap, distinct from the broad dome and tall stepped ridge.
    rect('mantle-core','ink',5,11,7,8)
    rect('charred-mantle-edge','rust',4,12,2,5)
    rect('charred-mantle-edge','rust',5,10,1,3)
    rect('charred-mantle-seam','ink',6,11,2,5)
    rect('cap-underside','stone_dark',6,7,5,7)
    rect('narrow-cap','stone',7,5,3,4)
    rect('cap-crown-edge','stone_light',7,5,2)
    rect('narrow-cap','stone',6,8,5,3)
    rect('cap-left-edge','stone_light',6,8,1,2)
    rect('cap-joint','stone_dark',8,10,2,3)
    rect('forward-mantle','stone',9,11,3,4)
    rect('forward-edge','stone_light',10,11,2)
    rect('mantle-underside','stone_dark',8,14,4,2)
    rect('charred-mantle-edge','rust',6,15,2,2)
    rect('charred-mantle-edge','rust',10,16,2)
    # Short blunt face, with no permanently spitting flame or carried item.
    rect('blunt-face','tawny',11,17,3,2)
    rect('brow','tawny',12,16,2)
    rect('eye','ink',13,17)
    rect('nose','ink',14,18)
    # One broad hind leg and two asymmetric grasping foreclaws, not extra feet.
    rect('bent-hind-leg','tawny',4,17,3,3)
    rect('hind-ankle','tawny',3,20,2,2)
    rect('hind-foot','tawny',2,22,4)
    rect('hind-toe','claw',2,22)
    rect('near-shoulder','tawny',8,18,2,2)
    rect('near-wrist','tawny',9,20,2)
    rect('near-claw','claw',10,21,1,2)
    rect('near-claw','claw',11,22)
    rect('far-forearm','tawny',13,19,1,2)
    rect('far-claw','claw',13,21,2)
    return cells

def model(scene,name,cells,palette):
    collection=bpy.data.collections.new(name);scene.collection.children.link(collection)
    materials={k:emission_material(name+'/'+k+' #'+v,v) for k,v in palette.items()}
    groups={}
    for y,row in enumerate(cells):
        for x,value in enumerate(row):
            if not value:continue
            verts,faces=groups.setdefault(tuple(value),([],[]))
            add_quad(verts,faces,x,23-y,x+1,24-y)
    for (part,color),(verts,faces) in sorted(groups.items()):
        mesh=bpy.data.meshes.new(name+'/'+part);mesh.from_pydata(verts,[],faces)
        mesh.materials.append(materials[color]);mesh.update()
        obj=bpy.data.objects.new(part,mesh);obj['paletteHex']=palette[color]
        obj['authoring']='Flat original integer-cell geometry; static body.'
        collection.objects.link(obj)
    return collection

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.context.preferences.filepaths.save_version=0
    scene=bpy.context.scene;scene.name='Cindercaller native 16x24'
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    cells=cindercaller_cells();col=model(scene,'MarlbackCindercaller / static body',cells,P)
    camera_for(scene,16,24);scene.render.filepath=str(OUT/'cindercaller.png')
    bpy.ops.render.render(write_still=True,scene=scene.name)
    contact=bpy.data.scenes.new('Marlback family / direct 8x geometry')
    camera_for(contact,96,40,scale=8)
    plane(contact,'Contact grass','032F23',0,0,96,40,-1)
    instance(contact,col,'New Cindercaller body',40,8)
    approved=json.loads((ROOT/'ArtSource/Live16x24/cast/source-cells.json').read_text())
    for index,x in ((2,8),(3,72)):
        frame=approved['frames'][index]
        comparison=model(contact,'Approved '+frame['id'],frame['cells'],approved['palette'])
        # Comparison source collections are moved out of direct rendering and
        # instanced at the shared native scale; no image is read or resampled.
        contact.collection.children.unlink(comparison)
        instance(contact,comparison,'Unchanged '+frame['id'],x,8)
    label=emission_material('Contact labels #B5A47E','B5A47E')
    add_label(contact,'MARLBACK FAMILY / NATIVE 16 x 24',48,37,label,1.7)
    for x,name in ((16,'APPROVED SCRABBLER'),(48,'NEW CINDERCALLER'),(80,'APPROVED GLEANER')):
        add_label(contact,name,x,4,label,1.2)
    contact.render.filepath=str(OUT/'cindercaller-contact.png')
    bpy.ops.render.render(write_still=True,scene=contact.name)
    (OUT/'source-cells.json').write_text(json.dumps(dict(schema=1,blueprint='MarlbackCindercaller',
        coordinateSystem='Top-left origin, y downward; null is transparent; editable named integer-cell quads.',
        palette=P,cells=cells),indent=2)+'\n')
    shutil.copyfile(OUT/'cindercaller.png',RES)
    write_meta(Path(str(RES)+'.meta'))
    opaque=[(x,y) for y,row in enumerate(cells) for x,value in enumerate(row) if value]
    manifest=dict(schema=1,blueprint='MarlbackCindercaller',resource='Sprites/Live16x24/cindercaller',
        width=16,height=24,frameCount=1,rect=[0,0,16,24],pixelsPerUnit=16,
        tilePivotNormalized=[.5,.5],filterMode='Point',mipmaps=False,compression='None',
        bounds=[min(x for x,y in opaque),min(y for x,y in opaque),max(x for x,y in opaque)+1,max(y for x,y in opaque)+1],
        palette=P,animation='None; static living body.',source='cindercaller.blend',cells='source-cells.json',
        generator='Tools/Live16x24/build_cindercaller.py',concept='ArtSource/Live16x24/cindercaller/concept.png',
        description='Narrow smooth slate cap, charred rust mantle rim, short tawny face, bent hind leg and two foreclaws. No held equipment or permanent fire.',
        sha256=hashlib.sha256(RES.read_bytes()).hexdigest(),
        scope='Original native source art only. Actual ownership, custody, fog, selection and gameplay acceptance belong to integration.')
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'cindercaller.blend'))
    print(json.dumps(manifest))

if __name__=='__main__':main()
