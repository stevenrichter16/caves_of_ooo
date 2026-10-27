"""Eight original loose harvest products: editable native integer-cell geometry.

Run Blender --background --threads4 --python this file (use --threads 4).
Packet-only output; no raster inputs, Assets imports or accepted-art edits.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/world-harvest-products'
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import camera_for,add_label,emission_material
from build_cindercaller import model
from build_north_plot import instance,plane
PALETTE={'mineral':'C6BDA0','mineral-edge':'E0D6B5','mineral-shadow':'8D826C',
 'bone':'AFA688','bone-light':'D2C7A6','briar':'6C705E','briar-leaf':'8E9480','briar-salt':'B9B79C','briar-shadow':'535449',
 'stone-dark':'3C5148','stone':'526657','rust':'9B442B','stone-light':'7D8E71',
 'ochre':'C99538','ochre-dark':'9C6B32','grain':'D8A446','cream':'E4D0A0',
 'rust-light':'B85834','ink':'342E25','leather':'5F4830','stone-cut':'B29B73','linen':'B5A47E'}

class Frame:
 def __init__(self,blueprint,description,limits):
  self.blueprint,self.description,self.limits=blueprint,description,limits
  self.cells=[[None]*16 for _ in range(24)]
 def rect(self,part,color,x,y,w=1,h=1):
  assert color in PALETTE and 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def dot(self,part,color,x,y):return self.rect(part,color,x,y)
 def clear(self,x,y,w=1,h=1):
  assert 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=None
  return self

def frames():
 fs=[]
 f=Frame('PaleSalt','Short unequal broken pale crystal faces joined by a dusty fractured base.','A carried mineral measure, not the tall living vein; no glow, new MaterialPart or stack-count promise.')
 f.rect('broken-salt-base','mineral-shadow',4,21,8,2)
 f.rect('short-crystal-face','mineral',4,18,3,4).rect('short-crystal-face','mineral',8,16,3,6)
 f.rect('crystal-cut-light','mineral-edge',8,16,2).rect('crystal-cut-light','mineral-edge',4,18,2)
 f.rect('short-crystal-face','mineral',7,20,2,3).rect('short-crystal-face','mineral',11,20,2,2)
 f.dot('crystal-cut-light','mineral-edge',11,20).dot('short-shadow-face','mineral-shadow',10,19)
 fs.append(f)
 f=Frame('Bone','One worn diagonal animal shaft with a blunt end and a dark pore in the broken cut.','Not Bones terrain, a corpse pile, skull, club or stone Tepuibone.')
 f.rect('worn-upper-end','bone',10,11,2,3).rect('worn-upper-end','bone',9,12,4,2)
 f.rect('worn-shaft','bone',8,14,3,3).rect('worn-shaft','bone',7,16,3,3).rect('worn-shaft','bone',6,18,3,3)
 f.rect('broken-end','bone',4,20,4,3).rect('broken-end','bone',3,20,2,2)
 f.rect('worn-highlight','bone-light',10,11,2).rect('worn-highlight','bone-light',9,13,1,3)
 f.rect('worn-highlight','bone-light',8,16,1,2).rect('worn-highlight','bone-light',7,18,1,2)
 f.rect('broken-cut-light','bone-light',4,20,2).dot('porous-cut','mineral-shadow',4,21)
 f.rect('broken-end','mineral-shadow',5,22,3)
 fs.append(f)
 f=Frame('SaltbriarSprig','Detached short gray/tan twig with sparse narrow leaves and restrained pale salt tips.','Raw edible sprig, no rooted crown, green Mendleaf identity, berries or medicinal sparkle.')
 f.rect('cut-stem','briar-shadow',7,15,1,8).rect('cut-stem','briar',7,18,1,4)
 f.rect('left-leaf-stalk','briar-shadow',6,15,2,2).rect('left-leaf','briar-leaf',4,12,2,3)
 f.rect('left-leaf','briar-leaf',5,14,2,2).dot('salt-rimed-leaf','briar-salt',4,12)
 f.rect('right-leaf-stalk','briar-shadow',8,17,2).rect('right-leaf','briar-leaf',9,15,3,2)
 f.rect('right-leaf','briar',10,17,2).dot('salt-rimed-leaf','briar-salt',11,15)
 f.rect('small-low-leaf','briar',5,19,2).dot('small-low-leaf','briar-leaf',5,18)
 f.dot('cut-stem','briar-shadow',6,22)
 fs.append(f)
 f=Frame('ChoirIron','Compact fractured dark ore wedge with two short reddish seam fragments and one muted metallic face.','Raw authored ChoirIron, not a new IronOre, ingot, ordinary rock or glowing metal.')
 f.rect('dark-ore-core','stone-dark',5,16,5,2).rect('dark-ore-core','stone-dark',4,18,8,4)
 f.rect('dark-ore-core','stone-dark',5,22,6).dot('dark-ore-core','stone-dark',12,20)
 f.rect('dark-ore-face','stone',5,17,3,3).rect('dark-ore-face','stone',7,19,3,2)
 f.rect('metallic-cut','stone-light',5,17,2).dot('metallic-cut','stone-light',8,19)
 f.rect('red-fracture','rust',9,16,1,3).rect('red-fracture','rust',4,20,2)
 f.dot('red-fracture','rust',10,21)
 fs.append(f)
 f=Frame('Honeycomb','Broken amber wax wedge with a sparse set of dark cells and one honey-worn torn edge.','Raw comb, not a honey bottle, hive, tiled floor, coin or cooked product.')
 f.rect('wax-face','ochre',6,15,4).rect('wax-face','ochre',4,16,7,2)
 f.rect('wax-face','ochre',3,18,9,3).rect('wax-face','ochre',4,21,6,2)
 f.rect('wax-light','grain',5,16,4).rect('wax-light','grain',3,18,2).rect('wax-light','grain',4,21,2)
 f.dot('wax-cut-tip','cream',6,15).dot('wax-cut-tip','cream',3,18)
 f.rect('cell-hollow','ochre-dark',5,17,2).rect('cell-hollow','ochre-dark',8,18,2)
 f.rect('cell-hollow','ochre-dark',5,20,2).dot('cell-hollow','ochre-dark',8,21)
 f.dot('torn-edge','grain',11,21)
 fs.append(f)
 f=Frame('GroveRed','One asymmetric raw red fleshy growth folded around a deep crease, with a short torn stalk.','Reagent vital3/toxic1 remains raw; no ordinary berry bunch, cleansed pulp, safe-food marker or invented poison glyph.')
 f.rect('torn-stem','leather',8,12,2,3).dot('torn-stem','leather',10,12)
 f.rect('raw-growth-body','rust',6,14,4,2).rect('raw-growth-body','rust',4,16,7,3)
 f.rect('raw-growth-body','rust',3,18,8,3).rect('raw-growth-body','rust',5,21,6,2)
 f.rect('raw-growth-body','rust',10,19,2,3)
 f.rect('fold-edge','rust-light',5,15,2,2).rect('fold-edge','rust-light',4,18,2)
 f.dot('fold-edge','rust-light',9,22)
 f.rect('unpurified-fold','ink',8,16,1,3).rect('unpurified-fold','ink',7,18,1,2)
 f.dot('unpurified-fold','ink',8,20)
 fs.append(f)
 f=Frame('Tepuibone','Squat angular sandstone piece with a chipped cut face and tightly enclosed wood-memory grain.','Dense Root sandstone, not an animal bone, literal log, shaped weapon head or infusion mineral.')
 f.rect('stone-cut-face','stone-cut',5,15,5).rect('stone-cut-face','stone-cut',4,16,8,2)
 f.rect('stone-cut-face','stone-cut',3,18,10,3).rect('stone-cut-face','stone-cut',4,21,8,2)
 f.rect('pale-stone-face','mineral',5,16,5,2).rect('pale-stone-face','mineral',4,18,7,3)
 f.rect('chipped-cut-light','bone-light',5,15,4).rect('chipped-cut-light','bone-light',4,17,2)
 f.dot('chipped-cut-light','bone-light',3,19)
 f.rect('closed-wood-grain','mineral-shadow',7,17,3).dot('closed-wood-grain','mineral-shadow',10,18)
 f.rect('closed-wood-grain','mineral-shadow',9,19,2).rect('closed-wood-grain','mineral-shadow',7,20,3)
 f.dot('closed-wood-grain','mineral-shadow',6,19).dot('closed-wood-grain','mineral-shadow',6,18)
 f.rect('lower-cut-shadow','mineral-shadow',6,22,5)
 fs.append(f)
 f=Frame('KnotflaxCord','A short open coil of twisted pale cord, with one offset crossing knot and an uninstalled loose tail.','Actual cord; no planted flax, infinite rope, prepared bandage or deployed snare.')
 f.rect('twisted-coil','linen',6,14,5,2).rect('twisted-coil','linen',4,16,3,4)
 f.rect('twisted-coil','linen',10,16,3,4).rect('twisted-coil','linen',5,19,7,2)
 f.rect('twisted-coil','cream',6,14,3).rect('braid-light','cream',4,17,1,2)
 f.rect('braid-light','cream',11,16,1,2).rect('braid-light','cream',9,20,2)
 f.dot('braid-shadow','leather',9,15).dot('braid-shadow','leather',5,18).dot('braid-shadow','leather',11,19)
 f.rect('crossing-knot','linen',6,20,3,2).dot('crossing-knot','cream',6,20).dot('crossing-knot','leather',7,21)
 f.rect('loose-tail','linen',8,21,3).rect('loose-tail','linen',10,22,3).dot('loose-tail','cream',12,22)
 fs.append(f)
 return fs

def main():
 preserved=json.loads((OUT/'preserved-inputs.json').read_text())
 def guard():
  for p,sha in preserved.items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha,('Approved input changed',p)
 guard();fs=frames();assert len(fs)==8
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 native=bpy.context.scene;native.name='Portable harvest original native 128x24';cols=[]
 for i,f in enumerate(fs):
  col=model(native,f'{i:02d} {f.blueprint}',f.cells,PALETTE);native.collection.children.unlink(col)
  for obj in col.objects:obj['authoring']='Original portable harvest integer geometry; no raster input.'
  instance(native,col,str(i),i*16,0);cols.append(col)
 camera_for(native,128,24);native.render.filepath=str(OUT/'world-harvest-products.png');bpy.ops.render.render(write_still=True,scene=native.name)
 width,height=288,180;placements=[]
 backgrounds=[('032F23',16,'DARK GREEN GROUND'),('735735',70,'WARM SAND'),('080B08',124,'INVENTORY DARK')]
 for color,top,label in backgrounds:
  for i in range(8):placements.append({'frame':i,'topLeft':[12+i*34,top],'background':color})
 for scale,name in ((1,'contact-native'),(8,'contact')):
  scene=bpy.data.scenes.new('Original product contact '+str(scale)+'x');camera_for(scene,width,height,scale)
  plane(scene,'Contact-only background','302D26',0,0,width,height,-2)
  labelmat=emission_material('Contact labels','D2C7A6')
  for color,top,label in backgrounds:
   plane(scene,label,color,0,height-top-24,width,24,-1)
   add_label(scene,label,width/2,height-top+5,labelmat,2)
  for p in placements:instance(scene,cols[p['frame']],str(p),p['topLeft'][0],height-p['topLeft'][1]-24)
  labels=['PaleSalt','Bone','Saltbriar','ChoirIron','Honeycomb','GroveRed','Tepuibone','Cord']
  for p in placements:add_label(scene,str(p['frame'])+' '+labels[p['frame']],p['topLeft'][0]+8,height-p['topLeft'][1]-29,labelmat,1.6)
  scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 source={'schema':1,'coordinateSystem':'Top-left, y down. Original named integer-cell quads; null is transparent.','palette':PALETTE,'frames':[{'index':i,'id':f.blueprint,'blueprint':f.blueprint,'cells':f.cells} for i,f in enumerate(fs)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 m={'schema':1,'artifact':'world-harvest-products.png','resource':'Sprites/Live16x24/world-harvest-products','width':128,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':8,'pixelsPerUnit':16,'pivot':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','source':'world-harvest-products.blend','generator':'Tools/Live16x24/build_world_harvest_products.py','unityExportDeferred':True,'scope':'Eight static loose goods only. Nine existing products are borrowed by root runtime, never repainted here.','contact':{'nativeSize':[width,height],'scales':[1,8],'instances':placements},'sha256':hashlib.sha256((OUT/'world-harvest-products.png').read_bytes()).hexdigest(),'frames':[{'index':i,'id':f.blueprint,'blueprint':f.blueprint,'description':f.description,'limits':f.limits,'unityRect':[i*16,0,16,24]} for i,f in enumerate(fs)]}
 (OUT/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'world-harvest-products.blend'));guard()
 print(json.dumps({'packetOnly':True,'frames':8,'size':[128,24],'sha256':m['sha256']}))

if __name__=='__main__':main()
