"""Read the saved editable packet in background Blender; write one bounded receipt."""
from pathlib import Path
import json
import bpy

HERE=Path(__file__).resolve().parent
assert Path(bpy.data.filepath).resolve()==(HERE/'native.blend').resolve()
source=json.loads((HERE/'source-cells.json').read_text())
reports=[]
for f in source['frames']:
    col=bpy.data.collections.get(f['id']);assert col is not None
    wanted=sum(c is not None for row in f['cells']for c in row)
    count=0
    for obj in col.objects:
        assert obj.type=='MESH' and obj['pose']==f['pose'] and obj['healingSlot']==f['healingSlot']
        assert len(obj.data.materials)==1
        for vertex in obj.data.vertices:
            x,y,z=vertex.co
            assert x==int(x) and y==int(y) and z==0 and 0<=x<=16 and 0<=y<=24
        assert all(len(poly.vertices)==4 for poly in obj.data.polygons)
        count+=len(obj.data.polygons)
    assert count==wanted
    reports.append({'frame':f['id'],'namedMeshes':len(col.objects),'integerQuads':count})
assert len(bpy.data.scenes)==3
assert all(scene.camera is not None and scene.camera.data.type=='ORTHO'for scene in bpy.data.scenes)
native=bpy.data.scenes['Patchbearer eight native motion derivatives']
assert len([o for o in native.objects if o.instance_type=='COLLECTION'])==8
for name in ['Patchbearer borrowed idle and derivatives 1x','Patchbearer borrowed idle and derivatives 8x']:
    scene=bpy.data.scenes[name]
    assert len([o for o in scene.objects if o.instance_type=='COLLECTION'])==10
report={'passed':True,'savedSourceReadOnly':True,'sceneCount':3,'frameCount':8,
        'frameGeometry':reports,'totalDerivativeIntegerQuads':sum(r['integerQuads']for r in reports),
        'contactRows':'Approved idle plus four derivatives, full and empty',
        'limits':'Editable geometry and saved scene structure only; no Unity or actual-game acceptance.'}
(HERE/'blend-inspection.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
