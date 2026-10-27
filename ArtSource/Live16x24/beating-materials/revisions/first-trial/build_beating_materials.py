"""Original Beating static kit, editable integer-cell geometry; packet-only export.

Blender --background --threads 4 --python Tools/Live16x24/build_beating_materials.py
No raster reads, Assets writes, Unity calls, animation or approved-source edits.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/beating-materials'
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import camera_for,add_label,emission_material
from build_cindercaller import model
from build_north_plot import instance,plane
PALETTE={
 'sand':'735735','sand-ripple':'7D5F3D','sand-shadow':'6B5032',
 'salt':'C3B99A','salt-crack':'AEA489','salt-light':'D8CEB1',
 'dune':'9C784C','dune-lee':'8C6943','dune-edge':'AC8857','dune-strata':'947047',
 'floor':'806A4C','floor-join':'736047','floor-worn':'8D7656',
 'wall':'765C40','wall-face':'9E8260','wall-top':'B29B73','wall-joint':'594A37',
 'rock':'494640','rock-face':'625D50','rock-light':'7D7765',
 'brine':'153D38','brine-deep':'133530','brine-reflection':'31534A',
 'film':'1B3F37','film-deep':'19382F','film-reflection':'2D4C40',
 'mineral-shadow':'8D826C','mineral':'C6BDA0','mineral-edge':'E0D6B5',
 'bone':'AFA688','bone-light':'D2C7A6',
 'briar-shadow':'535449','briar':'6C705E','briar-leaf':'8E9480','briar-salt':'B9B79C'}

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
 def path(self,part,color,points):
  for x,y in points:self.dot(part,color,x,y)
  return self

def sand(v):
 f=Frame('sand-'+str(v),'Sand','Quiet warm walking sand, rare short low-contrast wind fragments. No grass, rock sprinkles or outlined tile.',variant=v)
 f.rect('quiet-walking-sand','sand',0,0,16,24)
 marks=(((4,15,3),(7,14,2)),((3,7,4),(7,6,3),(9,17,3)),((4,18,5),(9,17,2),(3,8,2)),((5,9,3),(8,10,4),(3,19,3)))
 for x,y,w in marks[v]:f.rect('small-wind-fragment','sand-ripple',x,y,w)
 if v:f.rect('shallow-wind-shadow','sand-shadow',3+v,11+v,2)
 return f

def salt(v):
 f=Frame('flat-salt-crust-'+str(v),'SaltCrust','Pale flat pan surface with a few irregular fine drying cracks and broken plates; no height, masonry bevel or full-cell border.',variant=v)
 f.rect('flat-mineral-pan','salt',0,0,16,24)
 cracks=(
  ((3,10),(4,10),(5,11),(5,12),(6,13),(7,13),(8,13),(8,14),(9,15),(10,15),(11,16)),
  ((4,5),(4,6),(5,7),(6,8),(7,8),(8,9),(8,10),(7,11),(7,12),(6,13),(5,14),(4,14)),
  ((3,16),(4,16),(5,15),(6,15),(7,15),(8,14),(9,13),(9,12),(10,11),(11,10),(12,10)),
  ((3,7),(4,8),(5,8),(6,9),(7,9),(8,10),(9,10),(10,9),(11,9),(12,8),(12,7)))
 f.path('fine-irregular-drying-crack','salt-crack',cracks[v])
 plates=(((9,5,3),(8,6,5)),((9,17,4),(10,18,2)),((3,6,4),(4,7,3)),((5,17,5),(4,18,4)))
 for x,y,w in plates[v]:f.rect('thin-flat-plate','salt-light',x,y,w)
 return f

def dune(v):
 f=Frame('continuous-dune-'+str(v),'DuneCrest','Full-cell solid wind-packed dune band; broad warm mass and low muted lee strata, no tall cliff, pyramid, block frame or invented gap.',variant=v,solid=True)
 f.rect('solid-sand-ridge','dune',0,0,16,24)
 # Broad low-contrast patches meet side to side; no repeated bright crest.
 # This is top-down stratified mass, not an isometric or elevated cliff face.
 starts=( (14,14,14,14,13,13,13,13,13,13,14,14,14,14,14,14),
          (10,10,10,10,11,11,11,11,12,12,12,12,11,11,11,11),
          (17,17,17,17,17,16,16,16,15,15,15,15,16,16,16,16),
          (8,8,8,9,9,9,9,10,10,10,10,10,9,9,9,9))
 for x,start in enumerate(starts[v]):
  f.rect('low-rounded-lee','dune-lee',x,start,1,3 if v==0 else 4)
  if v and x in range(3,10):f.dot('subdued-wind-stratum','dune-strata',x,start-3)
 for x in range(5+v,9+v):f.dot('small-crest-light','dune-edge',x,starts[v][x]-1)
 return f

def floor(v):
 f=Frame('sandstone-floor-'+str(v),'SandstoneFloor','Worn warm flat paving with broken shallow joins. No outlined brick grid, curb or obstacle.',variant=v)
 f.rect('warm-flat-paving','floor',0,0,16,24)
 joins=(((3,11,6),(9,12,4)),((4,6,5),(9,7,3)),((2,18,7),(9,17,3)),((4,13,5),(9,14,4)))
 for x,y,w in joins[v]:f.rect('shallow-broken-floor-joint','floor-join',x,y,w)
 f.rect('worn-floor-face','floor-worn',3+v,18-v*3,3)
 return f

def wall(v):
 f=Frame('low-sandstone-wall-'+str(v),'SandstoneWall','Solid low worn sandstone masonry with broad broken joints. Exact one-cell footprint; no tall cliff or doorway.',variant=v,solid=True)
 f.rect('solid-wall-core','wall',0,0,16,24)
 blocks=(((0,3,8,5),(9,2,7,6),(0,10,5,5),(6,10,10,6),(0,18,9,5),(10,18,6,6)),
         ((0,2,5,6),(6,3,10,5),(0,10,10,6),(11,10,5,6),(0,19,6,5),(7,18,9,6)))
 for x,y,w,h in blocks[v]:
  f.rect('broad-worn-stone','wall-face',x,y,w,h)
  if w>3:f.rect('short-worn-top','wall-top',x+1,y,w-2)
 # Uneven strata run along the solid body; interruptions avoid framed tiles.
 for x,y,w in (((1,8,5),(8,16,6)) if v==0 else ((7,8,5),(1,17,7))):f.rect('broken-mortar','wall-joint',x,y,w)
 return f

def rock(v):
 f=Frame('rounded-rock-'+str(v),'Rock','Compact rounded grey-brown solid rock; one native owner, no extra scattered stones or loot.',variant=v)
 if v==0:
  for y,x,w in ((13,6,4),(14,4,8),(15,3,10),(16,3,10),(17,2,12),(18,2,12),(19,3,11),(20,4,9),(21,5,7),(22,6,5)):
   f.rect('rounded-rock-edge','rock',x,y,w)
  f.rect('rounded-rock-face','rock-face',4,15,7,5).rect('rounded-rock-face','rock-face',5,14,5,1)
  f.rect('soft-rock-highlight','rock-light',5,15,3).dot('soft-rock-highlight','rock-light',4,16)
 else:
  for y,x,w in ((12,8,3),(13,6,6),(14,5,8),(15,4,9),(16,3,10),(17,3,10),(18,3,10),(19,4,9),(20,5,8),(21,6,6),(22,7,4)):
   f.rect('rounded-rock-edge','rock',x,y,w)
  f.rect('rounded-rock-face','rock-face',6,14,5,5).rect('rounded-rock-face','rock-face',5,16,2,4)
  f.rect('soft-rock-highlight','rock-light',7,14,3).dot('soft-rock-highlight','rock-light',6,15)
 return f

def brine(depleted,v):
 f=Frame(('brine-wet-film-' if depleted else 'brine-filled-')+str(v),'BrinePool','Continuous calm desaturated teal liquid. Depleted stock retains wet hazardous film; no dry shore, clean-water promise or surface animation.',variant=v,stockState='depleted-wet-film' if depleted else 'filled',stillWet=True)
 base,shade,glint=('film','film-deep','film-reflection') if depleted else ('brine','brine-deep','brine-reflection')
 f.rect('continuous-wet-brine',base,0,0,16,24)
 shadows=(((5,14,4),),((3,6,5),(8,7,2)),((8,17,4),(7,18,3)),((3,11,6),(8,12,3)))
 refs=(((9,8,2),),((6,18,3),),((4,6,3),(7,7,1)),((9,16,3),))
 for x,y,w in shadows[v]:f.rect('shallow-wet-film' if depleted else 'quiet-liquid-depth',shade,x,y,w)
 for x,y,w in refs[v]:f.rect('wet-level-glint',glint,x,y,w)
 return f

def vein():
 f=Frame('pale-salt-deposit','PaleSaltVein','Attached angular pale mineral deposit, genuine unharvested source. No loose-item promise or added neighboring rocks.')
 f.rect('mineral-base','mineral-shadow',3,19,11,4).rect('mineral-base','mineral-shadow',2,20,12,2)
 for x,y,w,h in ((3,14,4,6),(8,11,3,9),(11,16,3,5),(6,18,5,4)):
  f.rect('angular-mineral-face','mineral',x,y,w,h)
  f.rect('pale-mineral-plane','mineral-edge',x,y,w,1)
  if h>5:f.rect('pale-mineral-plane','mineral-edge',x,y,1,h-2)
 f.dot('broken-mineral-tip','mineral-edge',8,10).dot('angular-mineral-face','mineral',4,13)
 return f

def bones():
 f=Frame('bleached-bones','Bones','Low sun-bleached broken skeleton with short ribs, one long bone and small open skull. Real harvestable source; no upright corpse or loot pile.')
 f.path('low-bone-shadow','bone',[(3,18),(4,18),(5,17),(6,17),(7,16),(8,16),(9,15),(10,15),(11,14)])
 f.path('bleached-long-bone','bone-light',[(3,17),(4,17),(5,16),(6,16),(7,15),(8,15),(9,14),(10,14)])
 for x,y in ((4,19),(6,19),(8,18)):
  f.dot('short-rib','bone',x,y).dot('short-rib','bone-light',x,y-1)
 f.rect('small-skull','bone',10,19,4,3).rect('bleached-skull','bone-light',10,19,3,2)
 f.dot('skull-eye','rock',11,20).dot('jaw','bone',11,22)
 f.rect('broken-bone','bone-light',2,22,3).dot('broken-end','bone',2,21)
 return f

def briar(v):
 f=Frame('salt-rimed-briar-'+str(v),'Saltbriar','Low grey branching salt-tolerant briar, sparse small salt-rimed leaves; distinguish from bare DryBrush, with no berries or flowers.',variant=v)
 f.rect('root-crown','briar-shadow',6,20,4,3)
 branches=(
  (((7,20),(7,19),(7,18),(8,17),(8,16),(8,15),(8,14),(9,13),(9,12)),((7,18),(6,17),(5,16),(4,15),(4,14),(3,13)),((8,19),(9,18),(10,17),(11,16),(12,15))),
  (((7,21),(7,20),(8,19),(8,18),(8,17),(7,16),(7,15),(7,14),(6,13)),((8,19),(9,18),(10,17),(10,16),(11,15),(11,14)),((7,18),(6,17),(5,17),(4,16),(3,15))))
 for points in branches[v]:
  for x,y in points:f.dot('branch-stem','briar',x,y)
 leaves=(((8,10),(3,11),(1,14),(5,14),(10,13),(12,13),(10,18),(4,19)),
         ((5,11),(8,13),(11,12),(12,15),(2,13),(4,15),(5,19),(10,20)))
 for x,y in leaves[v]:
  f.rect('salt-rimed-leaf','briar-leaf',x,y,2,2).dot('salt-rimed-leaf','briar-salt',x,y)
 return f

def main():
 preserved=json.loads((OUT/'preserved-inputs.json').read_text())
 def guard():
  for path,sha in preserved.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,path
 guard()
 frames=[sand(v) for v in range(4)]+[salt(v) for v in range(4)]+[dune(v) for v in range(4)]+[floor(v) for v in range(4)]+[wall(v) for v in range(2)]+[rock(v) for v in range(2)]+[brine(False,v) for v in range(4)]+[brine(True,v) for v in range(4)]+[vein(),bones(),briar(0),briar(1)]
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 native=bpy.context.scene;native.name='Beating static native 512x24';collections=[]
 for i,f in enumerate(frames):
  col=model(native,f'{i:02d} '+f.name,f.cells,PALETTE);native.collection.children.unlink(col)
  for obj in col.objects:obj['authoring']='Original Beating static integer-cell geometry; no raster input.'
  instance(native,col,str(i),i*16,0);collections.append(col)
 camera_for(native,512,24);native.render.filepath=str(OUT/'beating-materials.png');bpy.ops.render.render(write_still=True,scene=native.name)
 width,height=304,408
 places=[{'frame':i,'topLeft':[16+(i%8)*36,14+(i//8)*36]} for i in range(32)]
 # Repeated source studies are not an actual game reconstruction. Each field
 # uses75%base/25%sparse variants, matching the requested runtime distribution.
 pattern=((0,0,1,0,0,0,2,0),(0,3,0,0,0,1,0,0),(2,0,0,0,3,0,0,0),(0,0,1,0,0,0,0,2))
 studies=((16,178,0,'QUIET SAND'),(160,178,4,'FLAT SALT CRUST'),(16,300,8,'SOLID WIND STRATA'),(160,300,20,'BRINE / WET FILM'))
 for sx,sy,start,label in studies:
  for y,row in enumerate(pattern):
   for x,variant in enumerate(row):places.append({'frame':start+variant+(4 if start==20 and y>=2 else 0),'topLeft':[sx+x*16,sy+y*24]})
 for scale,name in ((1,'contact-native'),(4,'contact')):
  scene=bpy.data.scenes.new('Beating source contact '+str(scale)+'x');camera_for(scene,width,height,scale)
  plane(scene,'Source-only quiet background','302D26',0,0,width,height,-1)
  labelmat=emission_material('Source labels','D2C7A6');add_label(scene,'BEATING / ORIGINAL STATIC SOURCE / 16x24',width/2,height-6,labelmat,2)
  for p in places:instance(scene,collections[p['frame']],str(p),p['topLeft'][0],height-p['topLeft'][1]-24)
  labels=['Sand'+str(i) for i in range(4)]+['Salt'+str(i) for i in range(4)]+['Dune'+str(i) for i in range(4)]+['Floor'+str(i) for i in range(4)]+['Wall0','Wall1','Rock0','Rock1']+['Brine'+str(i) for i in range(4)]+['Film'+str(i) for i in range(4)]+['Vein','Bones','Briar0','Briar1']
  for i,p in enumerate(places[:32]):add_label(scene,str(i)+' '+labels[i],p['topLeft'][0]+8,height-p['topLeft'][1]-28,labelmat,1.5)
  for sx,sy,start,label in studies:add_label(scene,label,sx+64,height-sy+5,labelmat,1.8)
  scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 source={'schema':1,'coordinateSystem':'Top-left origin, y downward; named editable integer quads; null is transparent.','palette':PALETTE,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,**f.state,'cells':f.cells} for i,f in enumerate(frames)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 m={'schema':1,'artifact':'beating-materials.png','resource':'Sprites/Live16x24/beating-materials','width':512,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':32,'pixelsPerUnit':16,'pivot':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','source':'beating-materials.blend','generator':'Tools/Live16x24/build_beating_materials.py','unityExportDeferred':True,'variationWeights':[9,1,1,1],'variationMeaning':'Static coordinate variation, no animation or simulation RNG.','scope':'Original editable source only; parent owns actual runtime owner/state selection, imports, tests and scene acceptance.','contact':{'nativeSize':[width,height],'background':'302D26','scales':[1,4],'instances':places},'sha256':hashlib.sha256((OUT/'beating-materials.png').read_bytes()).hexdigest(),'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,**f.state,'description':f.description,'unityRect':[i*16,0,16,24]} for i,f in enumerate(frames)]}
 (OUT/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'beating-materials.blend'));guard()
 print(json.dumps({'packetOnly':True,'frames':32,'size':[512,24],'sha256':m['sha256']}))

if __name__=='__main__':main()
