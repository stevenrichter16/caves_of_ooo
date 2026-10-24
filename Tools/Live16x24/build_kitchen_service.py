"""Two native state derivatives over preserved approved east-kitchen cell layers.

Run Blender --background --python Tools/Live16x24/build_kitchen_service.py.
Original JSON cells are read-only bases. Cloth is authored as integer cells;
the approved meal is translated by (0,-3), never resized or redrawn. Concept
raster is viewed guidance only. No PNG is sampled or edited by this generator.
"""
import copy,hashlib,json,shutil,sys
from pathlib import Path
import bpy
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import add_quad,add_label,camera_for,emission_material
from build_north_plot import instance,plane
from build_east_kitchen import write_meta

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/kitchen-service'
SOURCE=ROOT/'ArtSource/Live16x24/east-kitchen/source-cells.json'
RESOURCE=ROOT/'Assets/Resources/Sprites/Live16x24/kitchen-service.png'

def make_frames(source):
 overlay=[[None]*16 for _ in range(24)]
 def rect(part,color,x,y,w,h):
  for yy in range(y,y+h):
   for xx in range(x,x+w):overlay[yy][xx]=[part,color]
 rect('cloth-upper-fold','linen',5,13,6,1)
 rect('cloth-shoulder','linen',4,14,8,1)
 rect('cloth-cover','linen',3,15,10,3)
 rect('cloth-lower-fold','linen',4,18,8,1)
 rect('cloth-left-drape','linen',3,18,1,3)
 rect('cloth-right-drape','linen',12,18,1,3)
 rect('cloth-center-fold','leaf_light',6,16,2,1)
 rect('cloth-center-fold','leaf_light',8,15,2,1)
 rect('cloth-left-fold','leaf_light',3,19,1,1)
 rect('cloth-right-fold','leaf_light',12,19,1,1)
 working=copy.deepcopy(overlay)
 meal=source['frames'][13]['cells']
 ready=[[copy.deepcopy(meal[y+3][x]) if y+3<24 else None for x in range(16)] for y in range(24)]
 fs=[]
 for i,(base_index,layer,name,blueprint,state) in enumerate(((5,working,'covered-working-pan','ConnectedBatchPan','Working'),(7,ready,'ready-meal-board','ConnectedKitchenPickup','Ready'))):
  base=copy.deepcopy(source['frames'][base_index]['cells'])
  fs.append({'index':i,'id':name,'blueprint':blueprint,'state':state,'baseSourceFrame':base_index,'baseCells':base,'overlayCells':layer,'cells':[[copy.deepcopy(layer[y][x] or base[y][x]) for x in range(16)] for y in range(24)]})
 return fs

def model(name,layers,materials,index=0):
 col=bpy.data.collections.new(name)
 for layer_index,(layer_name,cells) in enumerate(layers):
  groups={}
  for y,row in enumerate(cells):
   for x,value in enumerate(row):
    if not value:continue
    verts,faces=groups.setdefault(tuple(value),([],[]))
    add_quad(verts,faces,index*16+x,23-y,index*16+x+1,24-y,z=layer_index*.1)
  for (part,color),(verts,faces) in sorted(groups.items()):
   mesh=bpy.data.meshes.new(name+'/'+layer_name+'/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
   obj=bpy.data.objects.new(layer_name+'/'+part,mesh);obj['paletteHex']=materials[color]['sourceHex'];obj['layer']=layer_name;col.objects.link(obj)
 return col

def main():
 OUT.mkdir(exist_ok=True,parents=True);bpy.context.preferences.filepaths.save_version=0
 source=json.loads(SOURCE.read_text());p=source['palette'];frames=make_frames(source)
 scene=bpy.context.scene;scene.name='Kitchen service native 32x24'
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 materials={k:emission_material(k+' #'+v,v) for k,v in p.items()}
 for k,m in materials.items():m['sourceHex']=p[k]
 cols=[]
 for i,f in enumerate(frames):
  col=model(f['id'],[('approved-base',f['baseCells']),('state-overlay',f['overlayCells'])],materials,i)
  scene.collection.children.link(col);cols.append(col)
 camera_for(scene,32,24);scene.render.filepath=str(OUT/'kitchen-service.png');bpy.ops.render.render(write_still=True,scene=scene.name)
 contact=bpy.data.scenes.new('Kitchen service direct8x state comparison');camera_for(contact,128,40,scale=8)
 plane(contact,'Contact ground','032F23',0,0,128,40,-1)
 bases=[model('Approved repaired pan reference',[('approved-base',source['frames'][5]['cells'])],materials),model('Approved neutral board reference',[('approved-base',source['frames'][7]['cells'])],materials)]
 view=[(bases[0],0,'approved repaired'),(cols[0],0,'Working / cloth'),(bases[1],0,'approved neutral'),(cols[1],1,'Ready / one parcel')]
 label=emission_material('Labels #B5A47E','B5A47E')
 for i,(col,index,title) in enumerate(view):
  instance(contact,col,title,i*32+8-index*16,8);add_label(contact,title,i*32+16,4,label,1.5)
 add_label(contact,'KITCHEN SERVICE / EXACT APPROVED BASES + NATIVE STATE OVERLAYS',64,36,label,1.7)
 contact.render.filepath=str(OUT/'kitchen-service-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 data={'schema':1,'coordinateSystem':'Top-left origin, y downward; null is transparent. Approved base cells retained separately under editable integer-cell overlay quads.','source':str(SOURCE.relative_to(ROOT)),'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'palette':p,'frames':frames}
 (OUT/'source-cells.json').write_text(json.dumps(data,indent=2)+'\n')
 shutil.copyfile(OUT/'kitchen-service.png',RESOURCE);write_meta(Path(str(RESOURCE)+'.meta'))
 manifest={'schema':1,'width':32,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':2,'pixelsPerUnit':16,'tilePivotNormalized':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','alpha':'Binary 0/255.','source':'kitchen-service.blend','editableCells':'source-cells.json','generator':'Tools/Live16x24/build_kitchen_service.py','resource':'Sprites/Live16x24/kitchen-service','sha256':hashlib.sha256(RESOURCE.read_bytes()).hexdigest(),'frames':[],'scope':'Static current-service cues only. Pure state/paid-input/output-custody validation, current owner/graph/fog and fallback remain runtime-owned; no heat, Cook activity, stock ownership or pickup-permission promise.'}
 for f in frames:
  used=sorted({p[value[1]] for row in f['cells'] for value in row if value})
  manifest['frames'].append({k:f[k] for k in ('index','id','blueprint','state','baseSourceFrame')})
  manifest['frames'][-1].update({'rect':[f['index']*16,0,16,24],'palette':used,'overlayPixels':sum(bool(v) for row in f['overlayCells'] for v in row),'footprintCells':[1,1],'mealSourceFrame':13 if f['index']==1 else None,'mealTranslation':[0,-3] if f['index']==1 else None})
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kitchen-service.blend'))
 print('Wrote native kitchen service states:',manifest['sha256'])
if __name__=='__main__':main()
