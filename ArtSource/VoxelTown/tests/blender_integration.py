"""Real bpy acceptance gate; run via Blender --python-exit-code 1."""
import sys,json,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from town_generator.config import Config
from town_generator.layout import generate_town
from town_generator.model import to_dict
from town_generator.scene import realize,OWNER

town=generate_town(Config(seed=41));before=to_dict(town)
scene,manifest,stats=realize(town)
assert to_dict(town)==before,'realization changed semantic model'
assert stats['buildings']==10 and stats['npcs']==18
assert scene.camera.type=='CAMERA' and scene.camera.data.type=='ORTHO'
assert {c.name.split('.')[0] for c in scene.collection.children}>={'GENERATED_TERRAIN','GENERATED_BUILDINGS','GENERATED_PROPS','GENERATED_VEGETATION','GENERATED_WATER','GENERATED_NPCS','GENERATED_LIGHTING'}
assert len({i['asset'] for i in manifest['instances']})>=15
assert stats['unique_meshes']<stats['visible_objects']/2,'instances failed to share meshes'
assert all(not o.data.validate() for o in scene.objects if o.type=='MESH')
for b in town.buildings:
 obj=next(o for o in scene.objects if o.get('semantic_id')==b.id)
 assert abs(obj.rotation_euler.z-b.rotation)<1e-6
assert manifest['rotation_units']=='radians'
# Manual child and borrower survive a full rebuild, including their materials.
coll=next(c for c in scene.collection.children if c.name.startswith('GENERATED_PROPS'))
source=next(o for o in coll.objects if o.type=='MESH')
manual=bpy.data.objects.new('IntegrationManualBorrower',source.data);coll.objects.link(manual)
manual.location=(100,100,100);mat=source.data.materials[0]
mat.diffuse_color=(.123,.456,.789,1)
first=json.dumps(manifest,sort_keys=True)
scene,second,second_stats=realize(town,scene)
assert manual.name in scene.objects and tuple(manual.location)==(100,100,100)
assert all(abs(a-b)<1e-6 for a,b in zip(mat.diffuse_color,(.123,.456,.789,1)))
assert first==json.dumps(second,sort_keys=True),'repeat seed changed exported voxels/instances'
assert second_stats['visible_objects']==stats['visible_objects']+1
assert len([c for c in bpy.data.collections if c.get('coo_voxel_owner')==OWNER and c.get('coo_voxel_scene')==scene['coo_voxel_scene']])==8
print('BLENDER INTEGRATION PASS',json.dumps(stats,sort_keys=True))
