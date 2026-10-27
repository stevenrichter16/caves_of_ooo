"""Read-only inspection of saved source loaded by Blender --background <file>."""
import bpy,json
from pathlib import Path
p=Path(bpy.data.filepath).resolve().parent
source=json.loads((p/'source-cells.json').read_text())
collections=[]
for i,f in enumerate(source['frames']):
 c=bpy.data.collections.get(f'{i:02d} '+f['id']);assert c is not None
 assert c.objects and all(o.type=='MESH' for o in c.objects)
 cells=0
 for o in c.objects:
  assert o['source']=='Original native integer cells, not traced/sampled raster'
  assert o['paletteHex'] in source['palette'].values()
  for v in o.data.vertices:
   assert all(abs(n-round(n))<1e-6 for n in v.co)
   assert 0<=v.co.x<=16 and 0<=v.co.y<=24 and v.co.z==0
  assert all(len(face.vertices)==4 for face in o.data.polygons)
  cells+=len(o.data.polygons)
 assert cells==sum(bool(v) for row in f['cells'] for v in row)
 collections.append({'index':i,'meshes':len(c.objects),'editablePixelQuads':cells})
result={'passed':True,'savedFile':Path(bpy.data.filepath).name,'collections':collections,'scenes':list(bpy.data.scenes.keys()),'scope':'Loaded saved editable geometry only; no Unity acceptance.'}
(p/'saved-blend-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print('Saved native source loaded and inspected:',len(collections),'frame collections')
