"""Independent saved Blend/FBX identity check; no production modules reused."""
import bpy,json,gzip,hashlib,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p=ROOT/'ArtSource/VoxelTown/Output/refined-seed41'
m=json.loads(gzip.decompress((p/'town.voxels.json.gz').read_bytes()))
expected={r['id']:r for r in m['structures']+m['instances']}
bpy.ops.wm.open_mainfile(filepath=str(p/'town.blend'))
s=next(s for s in bpy.data.scenes if s.get('coo_voxel_owner')=='caves-of-ooo.voxel-town.v1')
objects={o.get('semantic_id'):o for o in s.objects if o.get('semantic_id')}
assert set(objects)==set(expected),(len(objects),len(expected))
for ident,row in expected.items():
 o=objects[ident]
 assert max(abs(a-b) for a,b in zip(o.location,row['position']))<1e-5,ident
 assert abs(o.rotation_euler.z-row.get('rotation',0))<1e-5,ident
 assert max(abs(a-b) for a,b in zip(o.scale,row.get('scale',[1,1,1])))<1e-6,ident
 if 'asset' in row:assert o.get('asset_key')==row['asset']
for asset in m['assets']:
 instances=[o for o in objects.values() if o.get('asset_key')==asset]
 assert not instances or len(set(o.data for o in instances))==1,asset
s2=bpy.data.scenes.new('IndependentFBXImport');bpy.context.window.scene=s2
bpy.ops.import_scene.fbx(filepath=str(p/'town.fbx'))
fbx={o.get('semantic_id'):o for o in s2.objects if o.get('semantic_id')}
assert set(fbx)==set(expected),(len(fbx),len(expected))
for ident,row in expected.items():
 o=fbx[ident]
 assert max(abs(a-b) for a,b in zip(o.matrix_world.translation,row['position']))<1e-4,(ident,tuple(o.matrix_world.translation),row['position'])
report=dict(status='PASS',artifact='refined-seed41',semanticEntities=len(expected),blendEntities=len(objects),fbxEntities=len(fbx),blendIdentityTransformsAndLinkedMeshes=True,fbxIdentityAndWorldPositions=True,artifactHashes={name:hashlib.sha256((p/name).read_bytes()).hexdigest() for name in ['town.json','town.voxels.json.gz','town.blend','town.fbx','gameplay.png']},honestyBounds='Checks saved files only, not current in-flight regenerated source, Unity import, destruction or runtime navigation. FBX custom properties and positions are verified; engine mapping is not implemented.')
(ROOT/'Docs/Verification/VoxelTown/independent-saved-exports.json').write_text(json.dumps(report,indent=2)+'\n')
print('SAVED_EXPORT_REVIEW',json.dumps(report))
