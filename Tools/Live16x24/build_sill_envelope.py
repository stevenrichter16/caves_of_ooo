"""Original native16x24 static source. Blender integer quads, no raster inputs.
Run /Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_sill_envelope.py
Writes only ArtSource/Live16x24/sill-envelope; does not import runtime assets.
"""
import hashlib,json,sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/sill-envelope'
sys.dont_write_bytecode=True
sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
from build_opening_base import add_quad,emission_material,camera_for,add_label
W,H=16,24
PALETTE={
 'vine-ink':'293D2C','vine':'3E5035','vine-mid':'53683E','leaf':'75834B',
 'wood':'665438','wood-low':'493D2D','wood-grain':'887145',
 'grass':'032F23','grass-low':'17382B','grass-mid':'294832','grass-tip':'50653D','silt':'364634',
}
class Frame:
 def __init__(self,name,owner,role,**meta):
  self.name,self.owner,self.role,self.meta=name,owner,role,meta
  self.cells=[[None]*W for _ in range(H)]
 def rect(self,part,color,x,y,w,h):
  assert color in PALETTE and 0<=x<x+w<=W and 0<=y<y+h<=H
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def dot(self,part,color,x,y):return self.rect(part,color,x,y,1,1)

def vine(mask):
 f=Frame('vine-mask-'+format(mask,'02x'),'VineWall','Living Plant barrier; no construction posts, not Hedge or stone.',neighborMask=mask)
 def shape(x,y):
  return 3<=x<=12 and 7<=y<=17 or mask&1 and 5<=x<=10 and y<=11 or mask&2 and x>=8 and 9<=y<=15 or mask&4 and 5<=x<=10 and y>=12 or mask&8 and x<=7 and 9<=y<=15
 for y in range(H):
  for x in range(W):
   if not shape(x,y):continue
   edge=any(not (0<=xx<W and 0<=yy<H and shape(xx,yy)) for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
   f.dot('tangled-living-body','vine-ink' if edge else 'vine',x,y)
 # Two explicit continuous winding strands per axis. No stochastic mottling:
 # the visible crossings make this a living braid rather than masonry/hedge.
 def clipped(part,color,x,y):
  if 0<=x<W and 0<=y<H and f.cells[y][x]:f.dot(part,color,x,y)
 wave=(0,0,1,1,0,-1,-1,0)
 if mask&10 or mask==0 or mask in (1,4,5):
  for x in range(W):
   y=10+wave[x%8]
   clipped('upper-winding-vine-outline','wood-low',x,y+1)
   clipped('upper-winding-vine','wood',x,y)
   y=14-wave[x%8]
   clipped('lower-winding-vine-outline','vine',x,y+1)
   clipped('lower-winding-vine','vine-mid',x,y)
 if mask&5:
  for y in range(H):
   x=6+wave[y%8]
   clipped('left-winding-vine-outline','wood-low',x+1,y)
   clipped('left-winding-vine','wood',x,y)
   x=9-wave[y%8]
   clipped('right-winding-vine-outline','vine',x+1,y)
   clipped('right-winding-vine','vine-mid',x,y)
 # Sparse paired leaves are attached to the strands. Bright pixels describe
 # a few distinct leaves rather than a uniform confetti texture.
 for x,y in ((3,8),(10,15),(6,3),(7,20),(0,11),(13,10)):
  for dx,dy,c in ((0,0,'vine-mid'),(1,0,'leaf'),(0,1,'leaf'),(1,1,'vine-mid')):
   clipped('paired-leaf',c,x+dx,y+dy)
 # Exact edge profiles match between connected cells. No guessed diagonals.
 for y in (0,23):
  if mask&(1 if y==0 else 4):
   for x,c in zip(range(5,11),('vine-ink','vine','wood','vine-mid','vine','vine-ink')):f.dot('vertical-connection',c,x,y)
 for x in (0,15):
  if mask&(8 if x==0 else 2):
   for y,c in zip(range(9,16),('vine-ink','vine','vine-mid','wood','vine','vine-mid','vine-ink')):f.dot('horizontal-connection',c,x,y)
 return f

def door(axis,closed):
 f=Frame('door-'+('ns-route' if axis==0 else 'ew-route')+'-'+('closed' if closed else 'open'),'VillageDoor','Ordinary neutral wood door, not repaired wicket, locked seal, archive or Morrowfast.',quarterTurns=axis,closed=closed)
 if axis==0:
  # Door crosses an east-west wall; passage runs north/south.
  for x in (1,13):
   f.rect('jamb','wood-low',x,8,2,10).rect('jamb-grain','wood',x,9,1,8)
  if closed:
   f.rect('door-outline','wood-low',3,9,10,8)
   for x in (3,6,9):f.rect('plain-planks','wood',x,9,2,8)
   f.rect('plain-planks','wood',12,9,1,8)
   f.rect('cross-brace','wood-grain',3,11,10,1).rect('cross-brace','wood-grain',3,15,10,1)
   f.dot('simple-pull','wood-low',11,13)
  else:
   f.rect('folded-leaf-outline','wood-low',3,9,3,13).rect('folded-leaf','wood',3,10,2,11)
   f.rect('folded-grain','wood-grain',3,11,1,4)
 else:
  # Independent native geometry; rotating a 16x24 raster would break cells.
  for y in (2,20):f.rect('jamb','wood-low',4,y,8,2).rect('jamb-grain','wood',5,y,6,1)
  if closed:
   f.rect('door-outline','wood-low',5,4,6,16)
   for y in (4,8,12,16):f.rect('plain-planks','wood',5,y,6,3)
   f.rect('cross-brace','wood-grain',6,4,1,16).rect('cross-brace','wood-grain',9,4,1,16)
   f.dot('simple-pull','wood-low',8,17)
  else:
   f.rect('folded-leaf-outline','wood-low',5,17,10,3).rect('folded-leaf','wood',6,18,8,2)
   f.rect('folded-grain','wood-grain',8,18,4,1)
 return f

def grass(i):
 f=Frame('grass-substrate-'+str(i),'Grass','Quiet walkable surface flecks; no harvest, raised rocks, road, hazard, crop or ownership claim.',variant=i)
 f.rect('quiet-surface','grass',0,0,W,H)
 patches=[((2,5,3,1),(3,6,2,1),(10,17,3,1),(11,18,1,1)),
          ((1,3,3,2),(2,5,2,2),(1,7,2,1),(10,19,3,2)),
          ((8,15,4,2),(7,17,5,2),(9,19,4,1),(3,3,2,1)),
          ((2,11,4,1),(3,12,3,2),(10,5,3,1),(11,6,3,1))]
 for x,y,w,h in patches[i]:f.rect('low-moss-cluster','grass-low',x,y,w,h)
 tufts=[((3,5),(11,17)),((2,4),(3,7),(11,19)),((9,16),(11,18)),((4,12),)]
 for x,y in tufts[i]:
  f.dot('pressed-short-grass','grass-mid',x,y).dot('pressed-short-grass','grass-mid',x+1,y-1)
  f.dot('short-grass-tip','grass-tip',x,y-1)
  if i in (1,2):f.dot('low-grass-root','grass-mid',x+1,y+1)
 if i==3:
  for x,y in ((10,5),(11,6),(4,13),(5,13)):f.dot('embedded-silt','silt',x,y)
 return f

def model(f,scene,i,materials):
 c=bpy.data.collections.new(f'{i:02d} '+f.name);scene.collection.children.link(c);groups={}
 for y,row in enumerate(f.cells):
  for x,v in enumerate(row):
   if v is None:continue
   vs,fs=groups.setdefault(v,([],[]));add_quad(vs,fs,x,H-y-1,x+1,H-y)
 for (part,color),(vs,fs) in sorted(groups.items()):
  m=bpy.data.meshes.new(f.name+'/'+part);m.from_pydata(vs,[],fs);m.materials.append(materials[color]);m.update()
  obj=bpy.data.objects.new(part,m);obj['paletteHex']=PALETTE[color];obj['source']='Original native integer cells, not traced/sampled raster';c.objects.link(obj)
 scene.collection.children.unlink(c);return c

def instance(scene,c,name,x,y):
 o=bpy.data.objects.new(name,None);o.instance_type='COLLECTION';o.instance_collection=c;o.location=(x,y,0);scene.collection.objects.link(o)

def main():
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 scene=bpy.context.scene;scene.name='Sill native static atlas 384x24';bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 frames=[vine(m) for m in range(16)]+[door(0,True),door(0,False),door(1,True),door(1,False)]+[grass(i) for i in range(4)]
 mats={k:emission_material(k+' #'+v,v) for k,v in PALETTE.items()};collections=[model(f,scene,i,mats) for i,f in enumerate(frames)]
 for i,c in enumerate(collections):instance(scene,c,f'{i:02d}',i*W,0)
 camera_for(scene,384,24);scene.render.filepath=str(OUT/'sill-envelope.png');bpy.ops.render.render(write_still=True,scene=scene.name)
 contact=bpy.data.scenes.new('Source contact only - no gameplay');cw,ch,scale=198,136,6;bg='032F23';camera_for(contact,cw,ch,scale)
 vs,fs=[],[];add_quad(vs,fs,0,0,cw,ch,-1);mesh=bpy.data.meshes.new('source preview backdrop');mesh.from_pydata(vs,[],fs);mesh.materials.append(emission_material('preview only',bg));contact.collection.objects.link(bpy.data.objects.new('source preview backdrop',mesh))
 label=emission_material('source labels','B5A47E');add_label(contact,'SILL / STATIC SOURCE ONLY / 16x24',cw/2,ch-5,label,2)
 placements=[]
 for i,c in enumerate(collections):
  x=9+(i%8)*23;top=14+(i//8)*39;instance(contact,c,str(i),x,ch-top-H);placements.append({'frame':i,'topLeft':[x,top]})
  labels=[f'V{m:02X}' for m in range(16)]+['NS shut','NS open','EW shut','EW open']+[f'G{j}' for j in range(4)]
  add_label(contact,labels[i],x+8,ch-top-H-4,label,1.7)
 contact.render.filepath=str(OUT/'contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 source={'schema':1,'coordinateSystem':'Top-left origin, x right/y down; each cell is an editable front-facing unit quad. No raster reads.','palette':PALETTE,'frames':[{'index':i,'id':f.name,**f.meta,'cells':f.cells} for i,f in enumerate(frames)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 manifest={'schema':1,'artifact':'sill-envelope.png','width':384,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':24,'source':'sill-envelope.blend','generator':'Tools/Live16x24/build_sill_envelope.py','pixelsPerUnit':16,'pivot':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','neighborMaskBits':{'north':1,'east':2,'south':4,'west':8},'scope':'Native source study only, no runtime integration or scene-fullness acceptance. Reject concept-generated walls at fog edges.','sha256':hashlib.sha256((OUT/'sill-envelope.png').read_bytes()).hexdigest(),'contact':{'scale':scale,'nativeSize':[cw,ch],'background':bg,'instances':placements},'frames':[{'index':i,'id':f.name,'owner':f.owner,'use':f.role,**f.meta,'unityRect':[i*16,0,16,24]} for i,f in enumerate(frames)]}
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sill-envelope.blend'));print('STATIC SOURCE ATLAS:',manifest['sha256'])
if __name__=='__main__':main()
