"""Exact 16x24 LongSword authored as named, editable integer-cell mesh quads.

Run with Blender --background --python Tools/Live16x24/build_long_sword.py.
The imagegen concept is reference-only. No raster is sampled, traced, resized,
or edited. All native, comparison, and parts images are direct geometry renders.
"""
import hashlib
import json
import shutil
import sys
from pathlib import Path
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_label, camera_for, emission_material
from build_cindercaller import model
from build_north_plot import instance, plane
from build_east_kitchen import write_meta

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'ArtSource/Live16x24/long-sword'
RESOURCE = ROOT/'Assets/Resources/Sprites/Live16x24/long-sword.png'
P = {'ink':'342E25', 'leather':'5F4830', 'steel_dark':'3C5148',
     'steel':'526657', 'steel_light':'7D8E71', 'linen':'B5A47E'}


def sword_cells():
    cells = [[None]*16 for _ in range(24)]
    def dot(part, color, x, y):
        assert color in P and 0 <= x < 16 and 0 <= y < 24
        cells[y][x] = (part, color)
    # Straight diagonal blade, two rows per step. Broad face, a single lighter
    # cutting edge and dark spine; no extra glint pixels or status decoration.
    dot('blade-tip', 'linen', 13, 1)
    dot('blade-edge', 'steel_light', 12, 2)
    dot('blade-tip', 'steel', 13, 2)
    for y in range(3, 15):
        x = 11 - (y-3)//2
        dot('blade-edge', 'linen' if y % 2 else 'steel_light', x, y)
        dot('blade-face', 'steel', x+1, y)
        dot('blade-spine', 'steel_dark', x+2, y)
    for x, color in ((6,'steel_light'), (7,'steel'), (8,'steel_dark')):
        dot('blade-heel', color, x, 15)
    # Plain perpendicular guard, wider than blade. Its descending diagonal
    # distinguishes it from a dagger or spear without elaborate fantasy hooks.
    for x,y in ((3,14),(3,15),(4,15),(4,16),(5,16),(5,17),
                (6,17),(7,17),(7,18),(8,18),(9,18),(9,19),(10,19)):
        dot('crossguard-shadow','ink',x,y)
    for x,y in ((3,15),(4,16),(5,16),(6,17),(7,17),(8,18),(9,18)):
        dot('crossguard','linen',x,y)
    # Short leather grip aligns with the blade behind the guard. Flat blocks,
    # not a miniature texture; the closed metal pommel is a separate silhouette.
    for x,y in ((4,18),(5,18),(3,19),(4,19),(5,19),(3,20),(4,20),(2,21),(3,21)):
        dot('grip-outline','ink',x,y)
    for x,y in ((5,18),(4,19),(3,20),(4,20)):
        dot('grip-leather','leather',x,y)
    for x,y in ((2,21),(3,21),(2,22),(3,22)):
        dot('pommel','steel_light',x,y)
    dot('pommel-edge','linen',2,21)
    dot('pommel-underside','steel_dark',3,22)
    return cells


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    native = bpy.context.scene; native.name = 'LongSword native 16x24'
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    cells = sword_cells()
    collection = model(native,'LongSword / neutral static item',cells,P)
    for obj in collection.objects:
        obj['authoring'] = 'Original integer-cell LongSword geometry; one grid unit per native pixel.'
    camera_for(native,16,24)
    native.render.filepath = str(OUT/'long-sword.png')
    bpy.ops.render.render(write_still=True,scene=native.name)

    contact = bpy.data.scenes.new('LongSword / direct 8x geometry comparison')
    camera_for(contact,96,40,scale=8)
    plane(contact,'Approved grass background','032F23',0,0,96,40,-1)
    instance(contact,collection,'New LongSword',40,8)
    approved = json.loads((ROOT/'ArtSource/Live16x24/items/source-cells.json').read_text())
    for index,x in ((9,8),(7,72)):
        frame = approved['frames'][index]
        comp = model(contact,'Approved '+frame['id'],frame['cells'],approved['palette'])
        contact.collection.children.unlink(comp)
        instance(contact,comp,'Unchanged '+frame['id'],x,8)
    label = emission_material('Contact labels #B5A47E','B5A47E')
    add_label(contact,'LONGSWORD / EXACT NATIVE 16 x 24',48,37,label,1.8)
    for x,name in ((16,'APPROVED CLAY'),(48,'NEW LONGSWORD'),(80,'APPROVED TONIC')):
        add_label(contact,name,x,4,label,1.35)
    contact.render.filepath = str(OUT/'long-sword-contact.png')
    bpy.ops.render.render(write_still=True,scene=contact.name)

    parts = bpy.data.scenes.new('LongSword / named source parts')
    camera_for(parts,80,40,scale=8)
    plane(parts,'Parts grass background','032F23',0,0,80,40,-1)
    for part,x in (('blade',8),('crossguard',32),('grip',56)):
        selected = [[v if v and (v[0].startswith(part) or (part=='grip' and v[0].startswith('pommel'))) else None
                     for v in row] for row in cells]
        piece = model(parts,'LongSword / '+part,selected,P)
        parts.collection.children.unlink(piece)
        instance(parts,piece,part,x,8)
        add_label(parts,{'blade':'STEEL BLADE','crossguard':'PLAIN GUARD','grip':'LEATHER / POMMEL'}[part],x+8,4,label,1.2)
    add_label(parts,'EDITABLE PARTS / SAME INTEGER CELLS',40,37,label,1.6)
    parts.render.filepath = str(OUT/'long-sword-parts.png')
    bpy.ops.render.render(write_still=True,scene=parts.name)

    (OUT/'source-cells.json').write_text(json.dumps(dict(schema=1,blueprint='LongSword',
        coordinateSystem='Top-left origin; y downward. Null is transparent; named cells become editable flat mesh quads.',
        palette=P,cells=cells),indent=2)+'\n')
    shutil.copyfile(OUT/'long-sword.png',RESOURCE)
    write_meta(Path(str(RESOURCE)+'.meta'))
    points = [(x,y) for y,row in enumerate(cells) for x,v in enumerate(row) if v]
    manifest = dict(schema=1,blueprint='LongSword',resource='Sprites/Live16x24/long-sword',
        width=16,height=24,frameCount=1,rect=[0,0,16,24],pixelsPerUnit=16,
        tilePivotNormalized=[.5,.5],filterMode='Point',wrapMode='Clamp',mipmaps=False,compression='None',
        alpha='Binary; independently validated.',palette=P,
        bounds=[min(x for x,y in points),min(y for x,y in points),max(x for x,y in points)+1,max(y for x,y in points)+1],
        occupiedPixels=len(points),animation='None; neutral static item.',
        source='long-sword.blend',cells='source-cells.json',generator='Tools/Live16x24/build_long_sword.py',
        concept='concept.png',parts='long-sword-parts.png',
        description='Long straight muted steel blade, plain pale crossguard, short dark leather grip and closed steel pommel. No magic, glow, enchantment, held-body overlay or condition promise.',
        sha256=hashlib.sha256(RESOURCE.read_bytes()).hexdigest(),
        scope='Native source art only. Exact owner/custody/fog/temper routing and actual gameplay acceptance belong to integration.')
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'long-sword.blend'))
    print(json.dumps(manifest))


if __name__=='__main__':main()
