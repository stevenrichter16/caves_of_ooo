"""One original native Farmer and an exact approved Reedback Grazer copy.

Run Blender --background --python Tools/Live16x24/build_incidental_actors.py.
Farmer is directly authored integer-cell emission geometry. The viewed concept
is a shape reference, never sampled, cropped, resized or traced. The grazer PNG
is copied byte-for-byte; its old cropped importer is deliberately not copied.
Writes only incidental-actors sources and the two owned resource PNGs/metas.
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
OUT=ROOT/'ArtSource/Live16x24/incidental-actors'
RES=ROOT/'Assets/Resources/Sprites/Live16x24'
APPROVED=ROOT/'Assets/Art/ReedbackGrazer16x24/03-ridge.png'
GRAZER_SHA='29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad'
P={'ink':'342E25','leather':'5F4830','skin':'DFA06A','cream':'E4D0A0','ochre':'B48B54','moss':'75834B','forest':'53683E'}

def farmer_cells():
 cells=[[None]*16 for _ in range(24)]
 def rect(part,color,x,y,w=1,h=1):
  assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24
  for yy in range(y,y+h):
   for xx in range(x,x+w):cells[yy][xx]=(part,color)
 # Exposed receding hair and right-facing profile, no hat or civilian apron.
 rect('receding-hair','leather',6,3,2)
 rect('exposed-crown','skin',8,3,2)
 rect('side-hair','leather',5,4,2,3)
 rect('face','skin',7,4,4,4)
 rect('profile-nose','skin',11,6)
 rect('eye','ink',10,5)
 rect('ear','cream',6,6)
 rect('neck','skin',7,8,2)
 # Short broad waistcoat flanks an ochre shirt. Rolled cuffs, lowered arms.
 rect('shoulders','moss',4,9,8)
 rect('shirt-collar','cream',7,9,2)
 rect('ochre-shirt','ochre',5,10,7,6)
 rect('left-waistcoat','moss',5,10,2,6)
 rect('right-waistcoat','forest',10,10,2,6)
 rect('left-sleeve','ochre',3,10,2,3)
 rect('right-sleeve','ochre',12,10,1,3)
 rect('left-rolled-cuff','cream',3,13,2)
 rect('right-rolled-cuff','cream',12,13)
 rect('left-forearm','skin',3,14,2,4)
 rect('right-forearm','skin',12,14,1,4)
 rect('broad-belt','leather',5,16,7)
 # Separate trouser legs and short boots; native baseline row22.
 rect('left-trouser','ink',5,17,3,3)
 rect('right-trouser','ink',9,17,3,3)
 rect('left-boot','leather',5,20,2,3)
 rect('right-boot','leather',10,20,2,3)
 rect('left-toe','leather',4,22)
 rect('right-toe','leather',12,22)
 return cells

def model(scene,cells,materials):
 col=bpy.data.collections.new('Farmer / neutral living body');scene.collection.children.link(col)
 grouped={}
 for y,row in enumerate(cells):
  for x,value in enumerate(row):
   if value is None:continue
   verts,faces=grouped.setdefault(value,([],[]))
   add_quad(verts,faces,x,23-y,x+1,24-y)
 for (part,color),(verts,faces) in sorted(grouped.items()):
  mesh=bpy.data.meshes.new('Farmer/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
  obj=bpy.data.objects.new(part,mesh);obj['paletteHex']=P[color];obj['authoring']='Original flat integer-cell quads; static body, no equipment/state claim.';col.objects.link(obj)
 return col

def main():
 assert hashlib.sha256(APPROVED.read_bytes()).hexdigest()==GRAZER_SHA
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 scene=bpy.context.scene;scene.name='Farmer native 16x24'
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 cells=farmer_cells();mats={k:emission_material(k+' #'+v,v) for k,v in P.items()}
 col=model(scene,cells,mats)
 camera_for(scene,16,24);scene.render.filepath=str(OUT/'farmer.png');bpy.ops.render.render(write_still=True,scene=scene.name)
 contact=bpy.data.scenes.new('Farmer direct 8x geometry');camera_for(contact,64,40,scale=8)
 plane(contact,'Contact grass','032F23',0,0,64,40,-1)
 instance(contact,col,'Farmer neutral living pose',24,8)
 label=emission_material('Labels #B5A47E','B5A47E')
 add_label(contact,'FARMER / NATIVE 16 x 24',32,36,label,1.8)
 add_label(contact,'static / no held tool / actual owner only',32,4,label,1.35)
 contact.render.filepath=str(OUT/'farmer-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 (OUT/'source-cells.json').write_text(json.dumps({'schema':1,'blueprint':'Farmer','coordinateSystem':'Top-left origin, y downward; null is transparent. One original editable flat quad per occupied cell.','palette':P,'cells':cells},indent=2)+'\n')
 shutil.copyfile(OUT/'farmer.png',RES/'farmer.png')
 shutil.copyfile(APPROVED,RES/'reedback-grazer.png')
 # Fresh full-frame metadata; preserve it after Unity normalization on rebuild.
 for name in ('farmer.png','reedback-grazer.png'):write_meta(Path(str(RES/name)+'.meta'))
 opaque=[(x,y) for y,row in enumerate(cells) for x,value in enumerate(row) if value]
 common={'width':16,'height':24,'frameCount':1,'rect':[0,0,16,24],'pixelsPerUnit':16,'tilePivotNormalized':[.5,.5],'filterMode':'Point','mipmaps':False,'compression':'None','animation':'None; static living body.'}
 farmer=dict(common,blueprint='Farmer',resource='Sprites/Live16x24/farmer',sha256=hashlib.sha256((RES/'farmer.png').read_bytes()).hexdigest(),bounds=[min(x for x,y in opaque),min(y for x,y in opaque),max(x for x,y in opaque)+1,max(y for x,y in opaque)+1],palette=P,source='incidental-actors.blend',cells='source-cells.json',description='Exposed receding hair, broad short moss waistcoat around ochre shirt, rolled cuffs, belt and separated dark trouser legs/boots; no held prop.')
 grazer=dict(common,blueprint='ReedbackGrazer',resource='Sprites/Live16x24/reedback-grazer',sha256=GRAZER_SHA,bounds=[0,9,16,21],approvedSource=str(APPROVED.relative_to(ROOT)),sourceMode='Exact approved PNG byte copy; fresh full-frame meta. No new grazer art, crop, padding, stretching or regeneration.')
 manifest={'schema':1,'farmer':farmer,'grazer':grazer,'generator':'Tools/Live16x24/build_incidental_actors.py','concept':'ArtSource/Live16x24/incidental-actors/farmer-concept.png','scope':'Source art only. Exact owner/custody/fog/reader routing and actual runtime acceptance remain parent-owned.'}
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'incidental-actors.blend'))
 print(json.dumps({'farmer':farmer['sha256'],'grazer':GRAZER_SHA,'source':str(OUT)}))
if __name__=='__main__':main()
