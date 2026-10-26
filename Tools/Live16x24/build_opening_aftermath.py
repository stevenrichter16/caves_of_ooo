"""Native 16x24 aftermath geometry. No raster input/resampling.
Run Blender --background --python Tools/Live16x24/build_opening_aftermath.py.
Approved living sprites remain unchanged. All meshes are flat source geometry.
"""
import hashlib, json, sys
from pathlib import Path
import bpy
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_north_works import Frame, PALETTE, model
from build_opening_base import camera_for, emission_material, add_quad, add_label
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/opening-aftermath'

def frames():
    bird = Frame('magpie-remains','CreatureCorpse','Low fallen bird with folded dark wing, cream breast and long slack tail.',
                 'Exact SourceBlueprint Magpie corpse, ground or authorized carried subject.',
                 'No standing pose, active eye, loot sparkle, blood or guaranteed butchery.')
    bird.rect('head','ink',2,16,4,3).rect('head-crown','stone_dark',3,15,3,2)
    bird.dot('beak','wood_dark',1,17).rect('neck','ink',5,17,2,3)
    bird.rect('body','ink',5,18,6,4).rect('folded-wing','stone_dark',6,17,4,3)
    bird.rect('wing-plane','stone',7,18,3,1).dot('wing-tip','stone_dark',10,19)
    bird.rect('breast','cream',4,19,3,2).rect('breast-edge','cream',3,20,2,1)
    bird.rect('tail','ink',10,20,5,2).rect('tail-plane','stone_dark',10,20,3,1)
    bird.rect('ground-edge','ink',4,22,8,1).dot('folded-foot','wood_dark',7,21)
    snake = Frame('viper-remains','CreatureCorpse','Slack open olive S with a flattened head and exposed underside.',
                  'Exact SourceBlueprint Viper corpse; optional unspent Harvestable retains existing chance.',
                  'Neutral remains, no promise of a gland; harvesting consumes the whole corpse.')
    snake.rect('slack-upper','leaf_dark',6,16,5,2).rect('upper-plane','forest',7,16,3,1)
    snake.rect('right-bend','leaf_dark',10,17,3,3).rect('right-plane','leaf',11,18,2,1)
    snake.rect('middle-body','leaf_dark',4,19,8,2).rect('middle-plane','forest',5,19,6,1)
    snake.rect('left-bend','leaf_dark',2,20,3,2).rect('underside','linen',3,21,6,1)
    snake.rect('tail','leaf_dark',7,22,6,1).dot('tail-tip','forest',13,21)
    snake.rect('flat-head','forest',2,17,4,2).dot('head-edge','leaf',3,17)
    snake.rect('neck','leaf_dark',4,18,2,2)
    gland = Frame('venom-gland','VenomGland','One organic ochre olive sac with tapered duct and a quiet membrane highlight.',
                  'Existing portable reagent with positive Toxic strength, dropped or carried.',
                  'Not a potion, food, gem or guaranteed corpse drop; no animation or potency claim.')
    gland.dot('duct-tip','linen',9,10).rect('duct','forest',8,11,1,3)
    gland.rect('duct-bend','forest',7,13,2,2).rect('neck','leaf_dark',6,14,2,3)
    gland.rect('sac','leaf_dark',4,17,7,4).rect('sac-shoulder','forest',5,16,5,2)
    gland.rect('sac-face','forest',5,18,5,3).rect('sac-base','leaf_dark',5,21,5,2)
    gland.rect('membrane','leaf',5,17,3,3).rect('soft-light','linen',5,18,1,2)
    gland.dot('lower-fold','leaf',7,20)
    return [bird,snake,gland]

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version=0
    atlas=bpy.context.scene; atlas.name='Native 48x24 aftermath atlas'
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    fs=frames(); mats={k:emission_material(k+' #'+v,v) for k,v in PALETTE.items()}
    groups=[model(f,mats,atlas,i) for i,f in enumerate(fs)]
    camera_for(atlas,48,24); atlas.render.filepath=str(OUT/'opening-aftermath.png')
    bpy.ops.render.render(write_still=True,scene=atlas.name)
    contact=bpy.data.scenes.new('Aftermath source geometry 12x')
    camera_for(contact,120,56,scale=12)
    vs,faces0=[],[];add_quad(vs,faces0,0,0,120,56,-1)
    mesh=bpy.data.meshes.new('Contact background');mesh.from_pydata(vs,[],faces0)
    mesh.materials.append(emission_material('ground #032F23','032F23'))
    contact.collection.objects.link(bpy.data.objects.new('Contact background',mesh))
    for i,(f,group) in enumerate(zip(fs,groups)):
        ob=bpy.data.objects.new(f.name,None);ob.instance_type='COLLECTION';ob.instance_collection=group
        ob.location=(i*40+12-i*16,18,0);contact.collection.objects.link(ob)
        add_label(contact,f.name,i*40+20,11,mats['cream'],1.7)
    add_label(contact,'AFTERMATH / NATIVE 16 x 24 / SOURCE GEOMETRY',60,50,mats['cream'],1.8)
    add_label(contact,'Static outcome identities / approved living art unchanged',60,4,mats['linen'],1.4)
    contact.render.filepath=str(OUT/'opening-aftermath-contact.png')
    bpy.ops.render.render(write_still=True,scene=contact.name)
    source={'schema':1,'coordinates':'top-left, y downward','palette':PALETTE,
            'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'cells':f.cells} for i,f in enumerate(fs)]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    manifest={'schema':1,'width':48,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':3,
              'resource':'Sprites/Live16x24/opening-aftermath','source':'opening-aftermath.blend',
              'generator':'Tools/Live16x24/build_opening_aftermath.py','filter':'Point','wrap':'Clamp',
              'mipmaps':False,'compression':'None','pivot':[0.5,0.5],'pixelsPerUnit':16,
              'concept':'ArtSource/Live16x24/concepts/opening-aftermath/concept.png',
              'capture':'Docs/Verification/Live16x24/aftermath-02-before-near.png',
              'sha256':hashlib.sha256((OUT/'opening-aftermath.png').read_bytes()).hexdigest(),'frames':[]}
    for i,f in enumerate(fs):
        cells=[(x,y,v) for y,row in enumerate(f.cells) for x,v in enumerate(row) if v]
        manifest['frames'].append({'index':i,'id':f.name,'blueprint':f.blueprint,'description':f.description,
            'use':f.use,'limits':f.limits,'occupiedPixels':len(cells),
            'palette':sorted({PALETTE[v[1]] for x,y,v in cells}),
            'bounds':[min(x for x,y,v in cells),min(y for x,y,v in cells),max(x for x,y,v in cells)+1,max(y for x,y,v in cells)+1]})
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'opening-aftermath.blend'))
    print('Wrote original editable flat geometry to',OUT)
if __name__=='__main__':main()
