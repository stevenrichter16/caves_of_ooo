"""Original static Grovelands material geometry, exact integer-cell sprites.

Blender --background --threads 4 --python this file. Packet-only output;
no raster inputs, no Assets writes, no edits to accepted sprite/source packets.
The actual gameplay concept informs material decisions, never raster pixels.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/grovelands-materials'
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import camera_for,add_label,emission_material
from build_cindercaller import model
from build_north_plot import instance,plane

PALETTE={
 'root-dark':'53604C','root':'89947A','pale':'B5A47E','pale-light':'C9C39A',
 'violet-dark':'4D414E','violet':'6B596B','violet-light':'887486',
 'water-deep':'183F3D','water':'285551','water-light':'4D7570',
 'film':'214541','film-glint':'3B5C55',
 'wood-dark':'493D2D','wood':'665438','wood-grain':'887145',
 'green':'53683E','green-light':'75834B',
 'red-dark':'54362E','red':'9B442B','red-light':'B85834',
 'shelf-base':'806746','shelf-lip':'A38B63',
 'compost-dark':'303D30','compost':'43513A','compost-fiber':'65705A',
 'cloth':'887C62','cloth-dark':'5D5848',
 'ore-dark':'3C5148','ore':'526657','ore-edge':'7D8E71',
 'wine-dark':'412D34','wine':'653B46','wine-edge':'885457','dew':'A18B7C'}

class Frame:
 def __init__(self,name,blueprint,description,**state):
  self.name,self.blueprint,self.description,self.state=name,blueprint,description,state
  self.cells=[[None]*16 for _ in range(24)]
 def rect(self,part,color,x,y,w=1,h=1):
  assert color in PALETTE and 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def dot(self,part,color,x,y):return self.rect(part,color,x,y)
 def path(self,part,color,positions):
  for x,y in positions:self.dot(part,color,x,y)
  return self
 def rows(self,part,color,rows):
  for y,x,w in rows:self.rect(part,color,x,y,w)
  return self

def column(v):
 f=Frame('mycelial-column-'+str(v),'MycelialColumn','Pale tapered living column with rooted buttresses and a few broken violet veins. No cap, capital, halo or promised motion.',variant=v,solid=True)
 if v==0:
  f.rows('tapered-living-column','root-dark',[(2,7,2),(3,6,4),(4,5,5),(5,5,6),(6,4,7),(7,4,7),(8,4,7),(9,4,7),(10,4,7),(11,4,7),(12,4,7),(13,4,7),(14,4,7),(15,4,7),(16,4,8),(17,4,8),(18,3,10),(19,3,10),(20,2,11),(21,2,12),(22,2,13)])
  f.rect('tapered-living-column','pale',6,4,3,13).rect('tapered-living-column','root',5,8,1,10).rect('tapered-living-column','root',9,6,1,12)
  f.path('soft-skin-light','pale-light',[(7,3),(6,5),(6,6),(6,10),(6,11),(6,15)])
  f.path('broken-violet-vein','violet',[(9,7),(8,8),(8,9),(8,12),(9,13),(9,14),(8,16)])
  f.path('root-filament','root',[(6,17),(5,18),(5,19),(4,20),(3,21),(2,22),(9,17),(10,18),(10,19),(11,20),(12,21),(13,22),(7,19),(7,20),(7,21)])
  f.path('root-filament','pale',[(6,18),(5,20),(4,21),(8,18),(9,19),(10,21),(11,22),(7,22)])
 else:
  f.rows('tapered-living-column','root-dark',[(2,6,2),(3,5,4),(4,5,5),(5,4,6),(6,4,7),(7,4,7),(8,4,7),(9,4,7),(10,4,7),(11,4,7),(12,4,7),(13,4,7),(14,4,7),(15,4,7),(16,3,8),(17,3,9),(18,3,9),(19,2,11),(20,2,11),(21,1,13),(22,1,13)])
  f.rect('tapered-living-column','pale',5,5,4,12).rect('tapered-living-column','root',9,7,1,11)
  f.path('soft-skin-light','pale-light',[(6,3),(5,5),(5,6),(5,9),(5,10),(6,14)])
  f.path('broken-violet-vein','violet',[(7,6),(7,7),(8,8),(8,9),(8,12),(7,13),(7,16)])
  f.path('root-filament','root',[(5,17),(4,18),(4,19),(3,20),(2,21),(1,22),(9,17),(10,18),(10,19),(11,20),(12,21),(13,22),(7,18),(7,19),(6,20),(6,21)])
  f.path('root-filament','pale',[(5,18),(4,20),(3,21),(9,19),(10,21),(11,22),(6,22)])
 return f

def seep(empty):
 f=Frame('grove-seep-empty-wet' if empty else 'grove-seep-available','GroveSeep','Low filament basin. Stock exhaustion reveals submerged filaments but retains actual permanent water coating; never dry ground.',availableStock=not empty,volumeRule='Volume == 0' if empty else 'Volume > 0',stillWet=True,solid=False)
 f.rows('white-filament-rim','root-dark',[(13,6,4),(14,3,10),(15,2,12),(16,1,14),(17,1,14),(18,1,14),(19,2,12),(20,2,12),(21,3,10),(22,5,6)])
 f.rows('white-filament-rim','pale',[(13,6,4),(14,4,8),(15,2,3),(15,11,2),(16,2,2),(17,1,2),(18,2,2),(19,3,2),(20,3,3),(21,5,7),(22,6,3)])
 f.path('white-filament-rim','pale-light',[(6,13),(9,14),(3,15),(2,17),(4,20),(7,21),(11,20)])
 part,color=('persistent-wet-film','film') if empty else ('available-water','water')
 f.rows(part,color,[(15,4,8),(16,3,10),(17,3,10),(18,3,10),(19,4,8),(20,5,6)])
 f.rect(part,'water-deep',5,18,5).rect(part,'water-deep',8,17,3)
 if empty:
  f.rect('film-reflection','film-glint',8,16,2).dot('film-reflection','film-glint',4,18)
  f.path('submerged-filament','root',[(5,16),(6,17),(7,17),(9,19),(10,19),(6,20)])
 else:f.rect('water-level-reflection','water-light',6,16,3).dot('water-level-reflection','water-light',10,18)
 return f

def sign():
 f=Frame('grown-grove-sign','GroveSign','Grown wood board with raised grain, rooted support and softened asymmetric edges. No arrow, nailed construction or baked readable text.',solid=True)
 f.rect('grown-support','wood-dark',7,12,3,10).rect('grown-support','wood',7,13,2,9)
 f.path('grown-support','wood',[(6,21),(5,22),(9,21),(10,22)])
 f.rows('living-board','wood-dark',[(5,5,6),(6,3,10),(7,3,10),(8,3,10),(9,3,10),(10,3,10),(11,4,9),(12,4,8),(13,6,5)])
 f.rect('living-board','wood',4,7,7,4).rect('living-board','wood',5,6,6).rect('living-board','wood',5,11,6)
 f.rect('raised-grain','wood-grain',5,8,4).rect('raised-grain','wood-grain',6,10,4).dot('raised-grain','wood-grain',5,10)
 f.dot('living-edge','green',3,7).rect('living-edge','green',10,12,2).dot('living-edge','green-light',10,12)
 return f

def red_growth():
 f=Frame('rooted-red-growth','GroveRedGrowth','Low heavy raw red lobes on a living support; distinct from a carried GroveRed or normal berries. Harvest removes the owner.',solid=False,unharvested=True,yieldBlueprint='GroveRed')
 f.rows('rooted-support','green',[(18,3,10),(19,2,12),(20,2,12),(21,3,10),(22,4,8)])
 f.path('rooted-support','green-light',[(2,19),(3,20),(5,22),(11,21),(13,20)])
 f.rows('heavy-red-growth','red-dark',[(13,4,3),(14,3,5),(15,3,6),(16,2,7),(17,2,8),(18,3,7),(19,4,5),(15,10,3),(16,9,5),(17,9,5),(18,9,5),(19,10,3)])
 f.rect('heavy-red-growth','red',4,14,3,4).rect('heavy-red-growth','red',3,16,3,2).rect('heavy-red-growth','red',10,16,3,3)
 f.path('raw-fold','red-light',[(4,14),(5,14),(3,16),(11,16),(12,17)])
 f.path('raw-fold','red-dark',[(6,16),(7,17),(11,18)])
 return f

def shelf(v):
 f=Frame('folded-fruiting-body-'+str(v),'FruitingBody','Solid closed fungal lobes, restrained violet over ochre. Squat unequal shelves, not edible fruit, stonework or oversized mushroom caps.',variant=v,solid=True,harvestable=False)
 if v==0:
  f.rows('solid-shelf-base','shelf-base',[(10,8,5),(11,6,7),(12,5,8),(13,3,10),(14,2,12),(15,1,13),(16,1,13),(17,2,12),(18,3,11),(19,3,10),(20,4,9),(21,4,8),(22,5,7)])
  f.rows('folded-violet-lobe','violet-dark',[(8,9,3),(9,7,6),(10,6,8),(11,6,8),(12,7,6),(13,3,7),(14,1,10),(15,1,10),(16,2,9),(17,3,7),(17,10,4),(18,8,7),(19,8,7),(20,9,5)])
  f.rect('folded-violet-lobe','violet',8,10,5).rect('folded-violet-lobe','violet',3,14,6,2).rect('folded-violet-lobe','violet',10,18,4)
  f.rect('soft-fold-face','violet-light',8,9,3).rect('soft-fold-face','violet-light',3,14,3)
  f.rect('restrained-lip','shelf-lip',7,12,3).rect('restrained-lip','shelf-lip',2,16,3).rect('restrained-lip','shelf-lip',10,20,2)
 else:
  f.rows('solid-shelf-base','shelf-base',[(10,3,6),(11,2,8),(12,2,10),(13,3,10),(14,3,11),(15,4,10),(16,3,11),(17,2,11),(18,2,11),(19,3,11),(20,3,10),(21,4,8),(22,4,7)])
  f.rows('folded-violet-lobe','violet-dark',[(9,4,4),(10,2,8),(11,1,10),(12,2,9),(13,3,7),(13,10,4),(14,8,7),(15,7,8),(16,8,6),(17,4,6),(18,2,10),(19,2,10),(20,4,7)])
  f.rect('folded-violet-lobe','violet',3,11,6).rect('folded-violet-lobe','violet',9,14,4).rect('folded-violet-lobe','violet',4,18,6)
  f.rect('soft-fold-face','violet-light',4,10,3).rect('soft-fold-face','violet-light',10,14,2)
  f.rect('restrained-lip','shelf-lip',3,13,3).rect('restrained-lip','shelf-lip',11,16,2).rect('restrained-lip','shelf-lip',3,20,3)
 return f

def compost(v):
 cache=v==3
 f=Frame('compost-pocket' if cache else 'quiet-compost-row-'+str(v),'CompostCache' if cache else 'CompostRow','Low walkable organic row with a small sagged pocket; no visible or guaranteed coins.' if cache else 'Low organic row. Subtle cloth/chitin remnants, not mushrooms, skeleton props, treasure or a solid fence.',solid=False,unharvested=True if cache else None,variant=None if cache else v,yieldBlueprint='GoldCoin' if cache else None,yieldChance=60 if cache else None)
 f.rect('organic-row','compost-dark',0,19,16,4).rect('organic-row','compost',0,20,16,2)
 f.rows('organic-row','compost',[(18,1,4),(18,7,5),(19,0,7),(19,10,6),(22,2,4),(22,9,4)])
 if v==0:
  f.rect('quiet-remnant','compost-fiber',4,19,2).dot('quiet-remnant','compost-fiber',10,21)
 elif v==1:
  f.rows('frayed-cloth','cloth-dark',[(16,5,3),(17,4,5),(18,3,6),(19,4,5)])
  f.rect('frayed-cloth','cloth',5,17,3).rect('frayed-cloth','cloth',4,18,3).dot('frayed-cloth','compost-fiber',3,19)
 elif v==2:
  f.rows('chitin-remnant','cloth-dark',[(17,9,3),(18,8,5),(19,7,5),(20,8,3)])
  f.rect('chitin-remnant','compost-fiber',9,18,3).dot('chitin-remnant','cloth',10,17)
 else:
  f.rows('sagged-pocket','cloth-dark',[(15,6,4),(16,4,8),(17,4,8),(18,3,9),(19,4,8),(20,5,6)])
  f.rect('sagged-pocket','cloth',5,17,5,2).rect('sagged-pocket','cloth',6,19,4)
  f.rect('pocket-fold','cloth-dark',5,16,5).dot('pocket-fold','cloth-dark',9,17)
  f.rect('pocket-fold','compost-fiber',6,15,3).dot('pocket-fold','compost-fiber',4,18)
 return f

def ore():
 f=Frame('choir-iron-deposit','ChoirIronVein','Dark squat ore deposit, broken metal seams and broad grounded mass, visibly larger than carried ChoirIron. No forged tool or glow.',solid=True,unharvested=True,yieldBlueprint='ChoirIron')
 f.rows('dark-ore-deposit','ore-dark',[(11,7,3),(12,5,6),(13,4,8),(14,3,10),(15,2,11),(16,2,12),(17,1,13),(18,1,13),(19,2,12),(20,2,11),(21,3,10),(22,4,8)])
 f.rect('dark-ore-deposit','ore',5,13,5,7).rect('dark-ore-deposit','ore',3,16,3,5).rect('dark-ore-deposit','ore',10,16,2,4)
 f.path('metal-fracture','ore-edge',[(7,12),(8,12),(6,13),(5,14),(5,15),(9,16),(10,17),(10,18),(4,20),(5,20)])
 return f

def sundew():
 f=Frame('wine-leaf-sundew','WineLeafSundew','Low dark wine-leaf rosette with pointed separate leaves and three sticky beads. Step-triggered snare, not red food, an animal or an animation state.',solid=False,grabChance=45,holdTurns=3,corrosion=1.5)
 f.rows('wine-leaf','wine-dark',[(13,4,2),(14,4,3),(15,5,3),(16,5,6),(17,3,10),(18,2,12),(19,1,14),(20,2,12),(21,4,8),(22,5,6)])
 f.path('wine-leaf','wine-dark',[(11,13),(11,14),(10,14),(10,15),(10,16),(2,16),(3,17),(13,16),(12,17)])
 f.path('wine-leaf','wine',[(5,14),(6,15),(6,16),(7,17),(10,15),(10,16),(9,17),(8,18),(3,18),(4,19),(5,19),(6,20),(12,18),(11,19),(10,19),(9,20),(7,21),(8,21)])
 f.path('wine-leaf','wine-edge',[(5,15),(6,16),(10,16),(3,19),(4,20),(11,20),(10,21)])
 f.path('sticky-bead','dew',[(4,13),(11,13),(13,17)])
 return f

def frames():
 return [column(0),column(1),seep(False),seep(True),sign(),red_growth(),shelf(0),shelf(1),compost(0),compost(1),compost(2),compost(3),ore(),sundew()]

def main():
 preserved=json.loads((OUT/'preserved-inputs.json').read_text())
 def guard():
  for p,sha in preserved.items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha,('Approved input changed',p)
 guard();fs=frames();assert len(fs)==14
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 native=bpy.context.scene;native.name='Grovelands original native 224x24';cols=[]
 for i,f in enumerate(fs):
  col=model(native,f'{i:02d} {f.name}',f.cells,PALETTE);native.collection.children.unlink(col)
  for obj in col.objects:obj['authoring']='Original Grovelands integer-cell material geometry; no raster input.'
  instance(native,col,str(i),i*16,0);cols.append(col)
 camera_for(native,224,24);native.render.filepath=str(OUT/'grovelands-materials.png')
 bpy.ops.render.render(write_still=True,scene=native.name)
 width,height=280,204;placements=[]
 for i in range(14):placements.append({'frame':i,'topLeft':[14+(i%7)*36,20+(i//7)*52],'background':'032F23'})
 # Direct source contact studies only: vertical shelves and low contiguous rows.
 # These are not new game layouts or claims of actual whole-chunk acceptance.
 for yy in (132,156):
  for j,xx in enumerate((14,30,46)):
   placements.append({'frame':6+((j+yy//24)%3==0),'topLeft':[xx,yy],'background':'032F23'})
 for yy in (132,168):
  for j,fr in enumerate((8,8,9,8,8,10,8,11)):
   placements.append({'frame':fr,'topLeft':[116+j*16,yy],'background':'032F23'})
 labels=['column A','column B','water','empty wet','grown sign','red growth','shelf A','shelf B','row base','cloth','chitin','pocket','ore vein','sundew']
 for scale,name in ((1,'contact-native'),(8,'contact')):
  scene=bpy.data.scenes.new('Grovelands direct source contact '+str(scale)+'x');camera_for(scene,width,height,scale)
  plane(scene,'Contact only ground','032F23',0,0,width,height,-2)
  mat=emission_material('Contact label','B5A47E')
  add_label(scene,'GROVELANDS / ORIGINAL STATIC SOURCE / 16x24',width/2,height-8,mat,2.3)
  for p in placements:instance(scene,cols[p['frame']],str(p),p['topLeft'][0],height-p['topLeft'][1]-24)
  for i in range(14):
   x,y=placements[i]['topLeft'];add_label(scene,str(i)+' '+labels[i],x+8,height-y-29,mat,1.6)
  add_label(scene,'FOLDED SOLID SHELVES',38,height-122,mat,1.7)
  add_label(scene,'LOW WALKABLE ROWS / NO GUARANTEED LOOT',180,height-122,mat,1.7)
  scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 source={'schema':1,'coordinateSystem':'Top-left x right/y down; original named integer-cell geometry. Null is transparent.','palette':PALETTE,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'state':f.state,'cells':f.cells} for i,f in enumerate(fs)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 m={'schema':1,'artifact':'grovelands-materials.png','resource':'Sprites/Live16x24/grovelands-materials','width':224,'height':24,'frameCount':14,'frameWidth':16,'frameHeight':24,'pixelsPerUnit':16,'pivot':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','source':'grovelands-materials.blend','generator':'Tools/Live16x24/build_grovelands_materials.py','unityExportDeferred':True,'scope':'Fourteen static new material/state cells only; four exact base owners borrow22 approved cells in root runtime. No optional animation, source gameplay change or whole-chunk acceptance.','conceptCritique':{'accept':['pale tapered living root columns','restrained violet folded shelves','low wine rosettes','teal seep contrast'],'reject':['bright grass carpeting','multi-cell mushroom caps','fog as new border architecture','invented skeletons','compost as mushrooms','excess cyan waves'],'seepCorrection':'Volume0 retains permanent actual water coating. Empty source remains wet; stock is unavailable.'},'contact':{'nativeSize':[width,height],'scales':[1,8],'instances':placements},'sha256':hashlib.sha256((OUT/'grovelands-materials.png').read_bytes()).hexdigest(),'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'description':f.description,'state':f.state,'unityRect':[i*16,0,16,24]} for i,f in enumerate(fs)]}
 (OUT/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'grovelands-materials.blend'));guard()
 print(json.dumps({'packetOnly':True,'frames':14,'size':[224,24],'sha256':m['sha256']}))

if __name__=='__main__':main()
