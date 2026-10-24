"""Original sparse stepping-stone ground, authored as native Blender cell quads.

Run: Blender --background --python Tools/Live16x24/build_working_court.py
Writes only the working-court source directory and its owned resource PNG/meta.
The concept is a viewed composition reference; no raster is read, sampled,
traced, resized or transformed. Accepted art and helpers remain read-only.
"""
import hashlib
import json
from pathlib import Path
import shutil
import sys
import bpy

sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import add_quad,add_label,camera_for,emission_material
from build_north_plot import instance,plane
from build_east_kitchen import write_meta

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/working-court'
RESOURCE=ROOT/'Assets/Resources/Sprites/Live16x24/working-court.png'
P={'grass':'032F23','stone_olive':'344A40','stone_grey':'425548','worn_surface':'4A5B4D'}

class Frame:
 def __init__(self,name,description):
  self.name,self.description=name,description
  self.cells=[[('grass-joint','grass') for _ in range(16)] for _ in range(24)]
 def rect(self,part,color,x,y,w,h):
  assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def grass(self,x,y,w=1,h=1):return self.rect('grass-joint','grass',x,y,w,h)

def frames():
 p=Frame('broad-pair','Two unequal broad worn stones with a wide open green joint and clipped corners.')
 p.rect('upper-stone','stone_grey',2,3,12,8)
 p.grass(2,3,2).grass(13,3).grass(2,10).grass(12,10,2)
 p.rect('upper-mineral-patch','stone_olive',9,7,3,2)
 p.rect('upper-wear','worn_surface',4,5,3,1)
 p.rect('lower-stone','stone_olive',5,15,9,7)
 p.grass(5,15).grass(13,15).grass(5,21,2).grass(13,21)
 p.rect('lower-mineral-patch','stone_grey',8,17,4,2)
 p.rect('lower-wear','worn_surface',10,19,2,1)
 a=p
 p=Frame('offset-pair','A longer left stone and shorter offset lower stone, with broad green margins.')
 p.rect('left-stone','stone_olive',2,2,8,11)
 p.grass(2,2,2).grass(9,2).grass(2,12).grass(8,12,2)
 p.rect('left-mineral-patch','stone_grey',4,5,4,3)
 p.rect('left-wear','worn_surface',5,5,2,1)
 p.rect('lower-stone','stone_grey',5,16,9,6)
 p.grass(5,16).grass(12,16,2).grass(5,21,2)
 p.rect('lower-mineral-patch','stone_olive',7,19,3,2)
 p.rect('lower-wear','worn_surface',9,17,3,1)
 b=p
 p=Frame('broken-three','One wide stone and two smaller broad pieces; grass remains connected around every piece.')
 p.rect('upper-stone','stone_grey',2,3,11,6)
 p.grass(2,3,2).grass(12,3).grass(2,8).grass(12,8)
 p.rect('upper-mineral-patch','stone_olive',8,6,3,2)
 p.rect('upper-wear','worn_surface',5,4,3,1)
 p.rect('left-stone','stone_olive',2,13,6,8)
 p.grass(2,13).grass(7,13).grass(2,20)
 p.rect('left-mineral-patch','stone_grey',4,15,3,2)
 p.rect('left-wear','worn_surface',4,18,2,1)
 p.rect('right-stone','stone_grey',10,15,5,6)
 p.grass(14,15).grass(10,20)
 p.rect('right-mineral-patch','stone_olive',12,18,2,2)
 return [a,b,p]

def model(frame,materials,scene,index):
 col=bpy.data.collections.new(f'{index:02d}-{frame.name}');scene.collection.children.link(col)
 grouped={}
 for y,row in enumerate(frame.cells):
  for x,value in enumerate(row):
   verts,faces=grouped.setdefault(value,([],[]))
   add_quad(verts,faces,index*16+x,23-y,index*16+x+1,24-y)
 for (part,color),(verts,faces) in sorted(grouped.items()):
  mesh=bpy.data.meshes.new(frame.name+'/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
  obj=bpy.data.objects.new(part,mesh);obj['paletteHex']=P[color];obj['authoring']='Original integer-cell flat geometry; direct native render.';col.objects.link(obj)
 return col

def main():
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 atlas=bpy.context.scene;atlas.name='Working court native 48x24'
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 fs=frames(); mats={k:emission_material(k+' #'+v,v) for k,v in P.items()}
 cols=[model(f,mats,atlas,i) for i,f in enumerate(fs)]
 camera_for(atlas,48,24);atlas.render.filepath=str(OUT/'working-court.png');bpy.ops.render.render(write_still=True,scene=atlas.name)
 contact=bpy.data.scenes.new('Working court direct 8x contact');camera_for(contact,96,40,scale=8)
 plane(contact,'Contact grass','032F23',0,0,96,40,-1)
 label=emission_material('Source labels #B5A47E','B5A47E')
 for i,(f,col) in enumerate(zip(fs,cols)):
  instance(contact,col,f'{i:02d} {f.name}',i*32+8-i*16,8)
  add_label(contact,f'{i:02d} {f.name}',i*32+16,4,label,1.5)
 add_label(contact,'SPARSE STEPPING STONES / 3 NATIVE 16 x 24 CELLS',48,36,label,1.7)
 contact.render.filepath=str(OUT/'working-court-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 repeat=bpy.data.scenes.new('Working court repetition direct 4x geometry');camera_for(repeat,64,72,scale=4)
 for row in range(3):
  for col in range(4):
   i=(col+row*2)%3;instance(repeat,cols[i],f'Repeat {col},{row} variant{i}',col*16-i*16,(2-row)*24)
 repeat.render.filepath=str(OUT/'working-court-repeat.png');bpy.ops.render.render(write_still=True,scene=repeat.name)
 source={'schema':1,'coordinateSystem':'Top-left origin, y downward. Every cell is an original editable flat emission quad. Opaque ground cells; no actor baseline.','palette':P,'frames':[{'index':i,'id':f.name,'blueprint':'StoneFloor','role':'court-floor','variant':i,'cells':f.cells} for i,f in enumerate(fs)]}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 meta=Path(str(RESOURCE)+'.meta');new_meta=not meta.exists();write_meta(meta)
 # Opaque pixels are authoritative. Preserve Unity-normalized metadata on rebuild.
 if new_meta:meta.write_text(meta.read_text().replace('alphaIsTransparency: 1','alphaIsTransparency: 0'))
 shutil.copyfile(OUT/'working-court.png',RESOURCE)
 manifest={'schema':1,'artifact':'working-court.png','width':48,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':3,'pixelsPerUnit':16,'tilePivotNormalized':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','alpha':'Opaque 255 throughout; grass background #032F23.','palette':P,'source':'working-court.blend','editableCells':'source-cells.json','generator':'Tools/Live16x24/build_working_court.py','resource':'Sprites/Live16x24/working-court','concept':'ArtSource/Live16x24/concepts/working-court-v1.png','baselineCaptures':['Docs/Verification/Live16x24/working-landscape-64-kitchen-2x.png','Docs/Verification/Live16x24/working-landscape-65-kitchen-2x.png'],'scope':'Original static ground-source art only. Exact StoneFloor/court-floor role with visible plantable grass joints. No full paving, Barren, road, active effect or animation. Runtime authority/variant routing and Unity/gameplay verification remain parent-owned.','sha256':hashlib.sha256(RESOURCE.read_bytes()).hexdigest(),'frames':[]}
 for i,f in enumerate(fs):
  stones=sum(v[1]!='grass' for row in f.cells for v in row)
  manifest['frames'].append({'index':i,'id':f.name,'blueprint':'StoneFloor','role':'court-floor','variant':i,'rect':[i*16,0,16,24],'footprintCells':[1,1],'layer':'ground','description':f.description,'stonePixels':stones,'grassPixels':384-stones,'palette':sorted({P[v[1]] for row in f.cells for v in row}),'state':'Static coordinate variant; no semantic or animation difference.'})
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'working-court.blend'))
 print('Wrote original working-court source and resource:',OUT)
if __name__=='__main__':main()
