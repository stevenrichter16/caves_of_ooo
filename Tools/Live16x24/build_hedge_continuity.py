"""Native connected hedge derivatives. Run with Blender --background --python.

Flat integer-cell geometry only. Mask0 comes from the approved editable frame,
never from raster editing. No old source, image or model is rewritten.
"""
import hashlib,json,shutil,sys
from pathlib import Path
import bpy
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_north_vegetation import make_frames,PALETTE
from build_opening_base import add_quad,add_label,camera_for,emission_material
from build_north_plot import instance,plane
from build_east_kitchen import write_meta
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/hedge-continuity'
RES=ROOT/'Assets/Resources/Sprites/Live16x24/hedge-continuity.png'
P={k:PALETTE[k] for k in ('leaf_dark','leaf_mid','leaf','wood_dark')}
GRASS='032F23'
N,E,S,W=1,2,4,8

def frames():
 original=make_frames()[1]
 result=[{'mask':0,'id':'isolated-approved','cells':original.cells}]
 for mask in range(1,16):
  # One continuous strip, never a rounded shrub repeated at each center.
  occupied={(x,y) for x in range(3,13) for y in range(6,18)}
  if mask&N:occupied|={(x,y) for x in range(3,13) for y in range(12)}
  if mask&S:occupied|={(x,y) for x in range(3,13) for y in range(12,24)}
  if mask&E:occupied|={(x,y) for x in range(8,16) for y in range(6,18)}
  if mask&W:occupied|={(x,y) for x in range(8) for y in range(6,18)}
  # Convex cap/corner chamfers stay inside this owner's cell. The consistent
  # joining rows/columns are never cut or decorated differently by the mask.
  for a,b,x,y,dx,dy in ((N,W,3,6,1,1),(N,E,12,6,-1,1),(S,W,3,17,1,-1),(S,E,12,17,-1,-1)):
   if not mask&a and not mask&b:
    occupied-= {(x,y),(x+dx,y),(x,y+dy)}
  horizontal=bool(mask&(E|W))
  # Two uneven one-pixel leaf tips break the long silhouette without a new
  # bulge at every tile center. N/E/S/W seam pixels remain untouched.
  if horizontal:
   if not mask&N:occupied|={(5,5),(6,5)};occupied.discard((11,6))
   if not mask&S:occupied|={(10,18),(11,18)};occupied.discard((5,17))
  else:
   if not mask&W:occupied|={(2,8),(2,9)};occupied.discard((3,17))
   if not mask&E:occupied|={(13,15),(13,16)};occupied.discard((12,5))
  cells=[[None for _ in range(16)] for _ in range(24)]
  for x,y in occupied:
   # Neighbor pixels beyond a joined cell edge are continuous leaf mass.
   def present(xx,yy):
    if (xx,yy) in occupied:return True
    return (xx<0 and mask&W and 6<=yy<=17) or (xx>15 and mask&E and 6<=yy<=17) or (yy<0 and mask&N and 3<=xx<=12) or (yy>23 and mask&S and 3<=xx<=12)
   rim=not all(present(xx,yy) for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
   cells[y][x]=('leaf-edge' if rim else 'continuous-leaf-mass','leaf_dark' if rim else 'leaf_mid')
  # Sparse broad groups follow the selected arms. They stop short of join
  # edges; all E/W and S/N signatures remain exactly equal in every mask.
  def patch(part,color,x,y,w,h):
   for yy in range(y,y+h):
    for xx in range(x,x+w):
     if 1<=xx<=14 and 1<=yy<=22 and (xx,yy) in occupied:cells[yy][xx]=(part,color)
  if horizontal:
   patch('leaf-recess','leaf_dark',2,9,4,4)
   patch('angular-leaf-face','leaf_mid',2,9,3,2)
   patch('leaf-fold','leaf',3,8,3,1)
   patch('leaf-fold','leaf',3,9,2,1)
   patch('leaf-recess','leaf_dark',9,11,4,4)
   patch('angular-leaf-face','leaf_mid',9,11,3,2)
   patch('leaf-fold','leaf',10,11,2,1)
   patch('woven-twig','wood_dark',7,14,1,1)
   patch('woven-twig','wood_dark',8,13,1,1)
   patch('woven-twig','wood_dark',9,14,1,1)
   if not mask&N:patch('leaf-tip-face','leaf_mid',4,6,3,1)
   if not mask&S:patch('leaf-tip-face','leaf_mid',9,17,3,1)
  else:
   patch('leaf-recess','leaf_dark',5,5,4,4)
   patch('angular-leaf-face','leaf_mid',5,5,3,3)
   patch('leaf-fold','leaf',5,5,2,2)
   patch('leaf-recess','leaf_dark',8,15,4,4)
   patch('angular-leaf-face','leaf_mid',9,15,3,2)
   patch('leaf-fold','leaf',10,15,2,1)
   patch('woven-twig','wood_dark',7,12,1,1)
   patch('woven-twig','wood_dark',8,13,1,1)
   patch('woven-twig','wood_dark',7,14,1,1)
   patch('leaf-tip-face','leaf_mid',3,7,1,4)
   patch('leaf-tip-face','leaf_mid',12,15,1,4)
  if mask&N and horizontal:patch('north-fold','leaf',8,2,2,2)
  if mask&S and horizontal:patch('south-fold','leaf',5,20,2,2)
  result.append({'mask':mask,'id':'hedge-'+''.join(c for bit,c in ((N,'N'),(E,'E'),(S,'S'),(W,'W')) if mask&bit),'cells':cells})
 return result

def model(frame,materials):
 col=bpy.data.collections.new(frame['id']);grouped={}
 for y,row in enumerate(frame['cells']):
  for x,value in enumerate(row):
   if value is None:continue
   verts,faces=grouped.setdefault(value,([],[]));add_quad(verts,faces,x,23-y,x+1,24-y)
 for (part,color),(verts,faces) in sorted(grouped.items()):
  mesh=bpy.data.meshes.new(frame['id']+'/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
  obj=bpy.data.objects.new(part,mesh);obj['paletteHex']=P[color];obj['nativeGrid']='16x24 integer cells';col.objects.link(obj)
 return col

def contact_cells():
 points={(x,0) for x in range(7)}|{(x,0) for x in range(8,12)}
 points|={(x,y) for x in range(5) for y in (2,4)}|{(x,3) for x in (0,4)}
 points|={(x,2) for x in range(6,10)}|{(6,y) for y in (3,4,5)}
 points|={(x,4) for x in (8,9,10)}|{(9,y) for y in (3,5)}|{(11,2)}
 return [{'x':x,'y':y,'mask':sum(bit for dx,dy,bit in ((0,-1,N),(1,0,E),(0,1,S),(-1,0,W)) if (x+dx,y+dy) in points)} for x,y in sorted(points,key=lambda xy:(xy[1],xy[0]))]

def main():
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 fs=frames();materials={k:emission_material(k+' #'+v,v) for k,v in P.items()};cols=[model(f,materials) for f in fs]
 atlas=bpy.context.scene;atlas.name='Connected hedge native256x24';camera_for(atlas,256,24)
 for i,col in enumerate(cols):instance(atlas,col,f'Mask{i}',i*16,0)
 atlas.render.filepath=str(OUT/'hedge-continuity.png');bpy.ops.render.render(write_still=True,scene=atlas.name)
 masks=bpy.data.scenes.new('All sixteen masks direct6x');camera_for(masks,128,144,scale=6);plane(masks,'Quiet green',GRASS,0,0,128,144,-1)
 label=emission_material('Labels #B5A47E','B5A47E')
 for i,col in enumerate(cols):
  x=(i%4)*32+8;y=(3-i//4)*36+8
  instance(masks,col,f'Mask{i}',x,y);add_label(masks,f'{i}: '+(''.join(c for b,c in ((1,'N'),(2,'E'),(4,'S'),(8,'W')) if i&b) or 'original'),x+8,y-4,label,1.5)
 masks.render.filepath=str(OUT/'hedge-continuity-masks.png');bpy.ops.render.render(write_still=True,scene=masks.name)
 layout=contact_cells()
 for scale,name in ((1,'hedge-continuity-native-contacts'),(4,'hedge-continuity-contacts')):
  scene=bpy.data.scenes.new(name);camera_for(scene,192,144,scale=scale);plane(scene,'Quiet green',GRASS,0,0,192,144,-1)
  for c in layout:instance(scene,cols[c['mask']],f"Cell{c['x']},{c['y']}",c['x']*16,(5-c['y'])*24)
  scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True,scene=scene.name)
 before=bpy.data.scenes.new('Original source comparison direct4x');camera_for(before,192,144,scale=4);plane(before,'Quiet green',GRASS,0,0,192,144,-1)
 for c in layout:instance(before,cols[0],f"Cell{c['x']},{c['y']}",c['x']*16,(5-c['y'])*24)
 before.render.filepath=str(OUT/'hedge-before-source-contact.png');bpy.ops.render.render(write_still=True,scene=before.name)
 source={'schema':1,'coordinates':'Top-left origin; y down; native16x24. Null transparent; exact planar integer quads.','palette':P,'frames':fs}
 (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 shutil.copyfile(OUT/'hedge-continuity.png',RES);write_meta(Path(str(RES)+'.meta'))
 manifest={'schema':1,'generator':'Tools/Live16x24/build_hedge_continuity.py','editableSource':'hedge-continuity.blend','editableCells':'source-cells.json',
  'nativeSize':[256,24],'frameSize':[16,24],'frameCount':16,'cardinalBits':{'N':1,'E':2,'S':4,'W':8},
  'resource':'Sprites/Live16x24/hedge-continuity','filter':'Point','wrap':'Clamp','mipmaps':False,'alpha':[0,255],'pixelsPerUnit':16,'pivot':[.5,.5],
  'resourceSha256':hashlib.sha256(RES.read_bytes()).hexdigest(),'isolatedSource':'Tools/Live16x24/build_north_vegetation.py:make_frames()[1]',
  'concept':'ArtSource/Live16x24/concepts/hedge-continuity/scene-v1.png','extraction':'ArtSource/Live16x24/concepts/hedge-continuity/README.md',
  'actualCaptures':['Docs/Verification/Live16x24/hedge-before-court.png','Docs/Verification/Live16x24/hedge-before-corner.png'],
  'contactCells':layout,'frames':[{'mask':f['mask'],'id':f['id'],'rect':[f['mask']*16,0,16,24],'blueprint':'Hedge','footprint':[1,1],
  'state':'Neutral live identity. No growth/damage animation; actual removal creates gap.','portrait':f['mask']==0} for f in fs],
  'limits':'Source contacts are native geometry previews, not gameplay. Root owns runtime/disclosure/owner acceptance and actual Unity captures.'}
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'hedge-continuity.blend'));print('Native connected hedge sources:',OUT)
if __name__=='__main__':main()
