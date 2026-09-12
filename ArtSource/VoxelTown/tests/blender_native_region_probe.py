"""Real Blender geometry/footprint, framing and safe-regeneration integration."""
import json
import copy
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
from town_generator.native_assets import asset_manifest
from town_generator.native_region import generate_region
from town_generator.native_scene import realize_chunk

data=generate_region();assets=asset_manifest();receipts=[]
for chunk in data['chunks']:
    scene=realize_chunk(chunk,assets);bpy.context.window.scene=scene
    for layer in scene.view_layers:layer.update()
    objects={o.get('semantic_id'):o for o in scene.objects if o.get('semantic_id')}
    assert len(objects)==len(chunk['placements'])
    for p in chunk['placements']:
        obj=objects[p['id']];points=[obj.matrix_world@Vector(corner) for corner in obj.bound_box]
        xs=[p['x']+dx for dx,dy in p['cells']];ys=[p['y']+dy for dx,dy in p['cells']]
        assert all(min(xs)-1e-6<=v.x<=max(xs)+1+1e-6 for v in points),(p['id'],'x bounds')
        assert all(chunk['height']-max(ys)-1-1e-6<=v.y<=chunk['height']-min(ys)+1e-6 for v in points),(p['id'],'y bounds')
    for obj in scene.objects:
        if obj.type!='MESH':continue
        for corner in obj.bound_box:
            projected=world_to_camera_view(scene,scene.camera,obj.matrix_world@Vector(corner))
            assert -.001<=projected.x<=1.001 and -.001<=projected.y<=1.001,(obj.name,'clipped')
    before=len(scene.objects)
    manual=bpy.data.objects.new('manual native preview marker',None);scene.collection.objects.link(manual)
    manual.location=(2,3,4)
    # These calls used to erase the prior generated preview before failing.
    owned_before=set(scene.objects)
    bad_chunk=copy.deepcopy(chunk);bad_chunk['placements'][0]['asset']='unknown-model'
    try:realize_chunk(bad_chunk,assets,scene)
    except ValueError:pass
    else:raise AssertionError('Bad model should fail preflight')
    assert set(scene.objects)==owned_before
    bad_assets=copy.deepcopy(assets);bad_assets['palette'][0]['hex']='FF00FF'
    try:realize_chunk(chunk,bad_assets,scene)
    except ValueError:pass
    else:raise AssertionError('Incompatible palette should fail preflight')
    assert set(scene.objects)==owned_before
    repeat=realize_chunk(chunk,assets,scene)
    assert repeat==scene and len(scene.objects)==before+1
    assert tuple(manual.location)==(2,3,4) and manual.name in scene.objects
    bpy.data.objects.remove(manual,do_unlink=True)
    receipts.append({'zoneId':chunk['zoneId'],'placements':len(objects),
                     'objects':before,'uniqueMeshes':len({o.data for o in scene.objects if o.type=='MESH'}),
                     'actualNativeBounds':'pass','cameraFraming':'pass','manualPreservation':'pass'})
print('BLENDER NATIVE REGION PASS '+json.dumps(receipts),flush=True)
