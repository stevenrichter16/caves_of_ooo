"""Original editable native Felling water; Blender build, Pillow read-only verification."""
import sys,json,hashlib
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Live16x24/felling-blackwater';W,H=16,24
P={'deep':'18292D','soft':'1C3034','current':'294348','glint':'385658','bank':'111E24','edge':'152428'}
def frames():
 out=[]
 for i in range(17):
  mask=0 if i<2 else i-1;variant=i%2
  c=[[['deep-water','deep'] for x in range(W)] for y in range(H)]
  def rect(part,color,x,y,w=1,h=1):
   for yy in range(y,y+h):
    for xx in range(x,x+w):c[yy][xx]=[part,color]
  # Low-contrast, unequal current patches; no horizontal symbolic bands.
  for x,y,h in ([(4,3,7),(10,13,6)] if not variant else [(9,4,5),(5,15,6)]):
   rect('soft-current','soft',x,y,2,h);rect('current-fragment','current',x,y+1,1,2)
  if i==1:rect('broken-reflection','glint',9,7)
  if mask&1:rect('north-bank-contact','bank',0,0,W);rect('north-water-transition','edge',0,1,W)
  if mask&2:rect('east-bank-contact','bank',15,0,1,H);rect('east-water-transition','edge',14,0,1,H)
  if mask&4:rect('south-bank-contact','bank',0,23,W);rect('south-water-transition','edge',0,22,W)
  if mask&8:rect('west-bank-contact','bank',0,0,1,H);rect('west-water-transition','edge',1,0,1,H)
  out.append({'index':i,'id':'deep-'+str(i),'mask':mask,'cells':c})
 return out

def build():
 import bpy
 sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
 from build_cindercaller import model
 from build_opening_base import camera_for
 from build_north_plot import instance
 from build_limestone_caves import canonicalize_timing
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 scene=bpy.context.scene;scene.name='Felling blackwater native272x24'
 for f in frames():
  col=model(scene,f['id'],f['cells'],P);scene.collection.children.unlink(col)
  for o in col.objects:o['frameIndex']=f['index']
  instance(scene,col,f['id'],f['index']*16,0)
 camera_for(scene,272,24);scene.render.filepath=str(OUT/'felling-blackwater.png');bpy.ops.render.render(write_still=True);canonicalize_timing(OUT/'felling-blackwater.png')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'))
 (OUT/'source-cells.json').write_text(json.dumps({'palette':P,'frames':frames()},indent=2)+'\n')
 (OUT/'manifest.json').write_text(json.dumps({'resource':'Sprites/Live16x24/felling-blackwater','frames':17,'frameSize':[16,24],'size':[272,24],'pivot':[.5,.5],'pixelsPerUnit':16,'filter':'Point','mipmaps':False,'compression':'None','maskBits':{'N':1,'E':2,'S':4,'W':8},'source':'editable.blend','no_geometry_change':True},indent=2)+'\n')
def verify():
 from PIL import Image
 im=Image.open(OUT/'felling-blackwater.png');assert im.size==(272,24);im=im.convert('RGBA')
 for f in frames():
  for y,row in enumerate(f['cells']):
   for x,cell in enumerate(row):assert im.getpixel((f['index']*16+x,y))==tuple(bytes.fromhex(P[cell[1]]))+(255,)
 assert json.loads((OUT/'source-cells.json').read_text())=={'palette':P,'frames':frames()}
 d={'exact_native_pixels':6528,'frames':17,'gameplay_acceptance':False,'files':{n:hashlib.sha256((OUT/n).read_bytes()).hexdigest() for n in ['felling-blackwater.png','editable.blend','source-cells.json']}}
 (OUT/'source-verification.json').write_text(json.dumps(d,indent=2)+'\n');print(json.dumps(d))
if __name__=='__main__':
 if '--build' in sys.argv:build()
 elif '--verify' in sys.argv:verify()
