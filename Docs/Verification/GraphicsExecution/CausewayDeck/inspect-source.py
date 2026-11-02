import bpy,json
from pathlib import Path
p=Path('/Users/steven/caves-of-ooo/ArtSource/Live16x24/sodden-travel');d=json.loads((p/'source-cells.json').read_text());rows=[]
for f in d['frames']:
 c=bpy.data.collections.get(f['id']);assert c is not None,f['id'];actual=sum(len(o.data.polygons) for o in c.objects if o.type=='MESH');expected=sum(bool(x) for row in f['cells'] for x in row);assert actual==expected,(f['id'],actual,expected);rows.append({'frame':f['index'],'quads':actual})
(p/'deck-reopened.json').write_text(json.dumps({'passed':True,'frames':28,'semantic_editable_quads':rows,'scope':'Reopened saved Blender collections; topology count, not binary reproducibility'},indent=2)+'\n');print('PASS reopened28frame collections')
