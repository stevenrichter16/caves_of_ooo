"""Native kitchen/orchard meshes. Blender --background --python this-file.

Original integer-cell planar emission quads; no concept raster sampled, edited,
resized or traced. Previous working-court source remains a recoverable style.
"""
import hashlib,json,shutil,sys
from pathlib import Path
import bpy
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import add_quad,add_label,camera_for,emission_material
from build_north_plot import instance,plane
from build_east_kitchen import write_meta
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/kitchen-orchard'
RES=ROOT/'Assets/Resources/Sprites/Live16x24'
# Exact existing opening-base and working-court palette entries.
P={'grass':'032F23','stone_olive':'344A40','stone_grey':'425548','worn_surface':'4A5B4D','ink':'342E25','ochre_dark':'9C6B32','ochre':'C99538','linen':'B5A47E'}
class Frame:
 def __init__(self,name,blueprint,description,ground=False):
  self.name,self.blueprint,self.description=name,blueprint,description
  self.cells=[[('grass-joint','grass') if ground else None for _ in range(16)] for _ in range(24)]
 def rect(self,part,color,x,y,w,h):
  assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def runs(self,part,color,y,rows):
  for dy,(x,w) in enumerate(rows):self.rect(part,color,x,y+dy,w,1)
  return self

def frames():
 p=Frame('quiet-wax-hive','Beehive','Uneven rounded ochre wax dome, three broad coil joints and a dark low entrance. Neutral identity only; harvest removes the owner.')
 p.runs('wax-dome','ochre',5,[(6,4),(5,6),(4,7),(3,9),(3,10),(2,11),(2,12),(2,12),(2,12),(2,12),(2,12),(2,12),(2,12),(2,12),(2,12),(3,11),(3,10),(4,8)])
 p.rect('upper-coil-joint','ochre_dark',5,9,6,1).rect('upper-coil-rise','ochre_dark',11,8,1,1)
 p.rect('middle-coil-joint','ochre_dark',4,13,7,1).rect('middle-coil-rise','ochre_dark',11,12,2,1)
 p.rect('lower-coil-joint','ochre_dark',4,17,6,1).rect('lower-coil-rise','ochre_dark',10,16,3,1)
 p.rect('base-wax','ochre_dark',4,21,8,2)
 p.rect('wax-wear','linen',5,7,2,1).rect('wax-wear','linen',3,11,2,1).rect('wax-wear','linen',3,15,2,1)
 p.rect('entrance','ink',6,18,3,4).rect('entrance-neck','ink',7,17,1,1)
 hive=p
 p=Frame('one-broad-worn-slab','StoneFloor','One broad irregular olive-grey slab with two quiet wear areas; no rim or false height.',True)
 p.runs('stone','stone_grey',3,[(4,7),(3,9),(2,11),(2,12),(2,12),(2,12),(2,11),(2,11),(3,10),(3,10),(3,10),(2,11),(2,11),(3,10),(3,9),(4,7),(5,5)])
 p.rect('mineral','stone_olive',8,7,4,3).rect('mineral','stone_olive',9,10,3,3)
 p.rect('worn-patch','worn_surface',4,6,4,2).rect('worn-patch','worn_surface',4,8,2,1)
 a=p
 p=Frame('unequal-separated-pair','StoneFloor','Small upper slab and wider lower slab separated by a broad continuous grass joint.',True)
 p.runs('upper-stone','stone_olive',2,[(5,5),(4,7),(3,8),(3,8),(3,9),(4,8),(4,7),(5,6)])
 p.rect('upper-wear','stone_grey',5,4,3,3).rect('upper-wear','worn_surface',5,4,2,1)
 p.runs('lower-stone','stone_grey',13,[(4,8),(3,10),(2,11),(2,12),(2,12),(3,11),(3,10),(4,8),(6,5)])
 p.rect('lower-mineral','stone_olive',8,17,4,2).rect('lower-wear','worn_surface',4,15,4,2)
 b=p
 p=Frame('offset-worn-pair','StoneFloor','Long upper-left stepping slab and a smaller lower-right stone with unbroken grass margins.',True)
 p.runs('upper-stone','stone_grey',2,[(4,6),(3,8),(2,9),(2,10),(2,10),(2,10),(2,10),(2,10),(3,9),(3,8),(3,8),(4,7),(5,5)])
 p.rect('upper-mineral','stone_olive',7,7,4,4).rect('upper-wear','worn_surface',4,4,3,2)
 p.runs('lower-stone','stone_olive',18,[(9,3),(8,5),(7,7),(7,7),(8,5)])
 p.rect('lower-wear','stone_grey',9,20,3,2)
 return [hive,a,b,p]

def model(frame,materials):
 col=bpy.data.collections.new(frame.name);grouped={}
 for y,row in enumerate(frame.cells):
  for x,v in enumerate(row):
   if not v:continue
   verts,faces=grouped.setdefault(v,([],[]));add_quad(verts,faces,x,23-y,x+1,24-y)
 for (part,color),(verts,faces) in sorted(grouped.items()):
  mesh=bpy.data.meshes.new(frame.name+'/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
  obj=bpy.data.objects.new(part,mesh);obj['paletteHex']=P[color];obj['authoring']='Flat integer-cell original geometry';col.objects.link(obj)
 return col

def main():
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 fs=frames();materials={k:emission_material(k+' #'+v,v) for k,v in P.items()};cols=[model(f,materials) for f in fs]
 hive=bpy.context.scene;hive.name='Orchard hive native16x24';hive.collection.children.link(cols[0]);camera_for(hive,16,24)
 hive.render.filepath=str(OUT/'orchard-hive.png');bpy.ops.render.render(write_still=True,scene=hive.name)
 floor=bpy.data.scenes.new('Working court v2 native48x24');camera_for(floor,48,24)
 for i,col in enumerate(cols[1:]):instance(floor,col,f'Variant{i}',i*16,0)
 floor.render.filepath=str(OUT/'working-court-v2.png');bpy.ops.render.render(write_still=True,scene=floor.name)
 contact=bpy.data.scenes.new('Kitchen orchard direct8x contact');camera_for(contact,128,44,scale=8);plane(contact,'Contact grass',P['grass'],0,0,128,44,-1)
 label=emission_material('Labels #B5A47E','B5A47E')
 for i,col in enumerate(cols):
  instance(contact,col,fs[i].name,i*32+8,8);add_label(contact,fs[i].name,i*32+16,4,label,1.25)
 add_label(contact,'NATIVE 16x24 / NEUTRAL HIVE + QUIET WORKING GROUND',64,39,label,1.6)
 contact.render.filepath=str(OUT/'kitchen-orchard-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 repeat=bpy.data.scenes.new('Working court v2 direct4x repetition');camera_for(repeat,64,72,scale=4)
 for row in range(3):
  for col in range(4):instance(repeat,cols[1+(col+row*2)%3],f'Repeat{col},{row}',col*16,(2-row)*24)
 repeat.render.filepath=str(OUT/'working-court-v2-repeat.png');bpy.ops.render.render(write_still=True,scene=repeat.name)
 source={'schema':1,'coordinates':'Top-left origin y-down,16x24 native cells. Null=transparent; all geometry planar emission quads.','palette':P,'paletteSources':['ArtSource/Live16x24/base/source-cells.json','ArtSource/Live16x24/working-court/source-cells.json'],'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'description':f.description,'cells':f.cells} for i,f in enumerate(fs)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 resources={}
 for name,size in (('orchard-hive',[16,24]),('working-court-v2',[48,24])):
  resource=RES/(name+'.png');shutil.copyfile(OUT/(name+'.png'),resource);meta=Path(str(resource)+'.meta');new=not meta.exists();write_meta(meta)
  if new and name=='working-court-v2':meta.write_text(meta.read_text().replace('alphaIsTransparency: 1','alphaIsTransparency: 0'))
  resources[name]={'size':size,'resource':'Sprites/Live16x24/'+name,'sha256':hashlib.sha256(resource.read_bytes()).hexdigest()}
 manifest={'schema':1,'source':'kitchen-orchard.blend','editableCells':'source-cells.json','generator':'Tools/Live16x24/build_kitchen_orchard.py','frameWidth':16,'frameHeight':24,'frameCount':4,'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'alpha':'Hive binary0/255;ground opaque255.','concepts':['ArtSource/Live16x24/concepts/kitchen-orchard/scene-v1.png','ArtSource/Live16x24/concepts/kitchen-orchard/parts-v1.png'],'greyboxCaptures':['Docs/Verification/Live16x24/packaged-glade-east-18-kitchen-court.jpg','Docs/Verification/Live16x24/packaged-glade-east-22-continued-kitchen.jpg'],'resources':resources,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'resource':'orchard-hive' if i==0 else 'working-court-v2','rect':[0 if i==0 else (i-1)*16,0,16,24],'role':'identity' if i==0 else 'court-floor','variant':None if i==0 else i-1,'state':'Neutral current-owner identity; harvest/removal clears hive.' if i==0 else 'Static coordinate variant; no state promise.','footprint':[1,1],'description':f.description} for i,f in enumerate(fs)],'limits':'Source-native art only. Runtime owner/role selection, malformed/removed owner exclusions, ordinary gameplay, profiling and physical Deck remain parent-owned.'}
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kitchen-orchard.blend'));print('Native kitchen/orchard sources:',OUT)
if __name__=='__main__':main()
