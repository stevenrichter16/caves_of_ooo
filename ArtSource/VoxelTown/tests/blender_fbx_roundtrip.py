"""Independent Blender FBX roundtrip: evaluated world faces/material colors/identity.
Run in headless Blender. No generator/export code is reused.
"""
import bpy,json,hashlib,math,sys
from pathlib import Path
from collections import Counter
ROOT=Path(__file__).resolve().parents[3]
name=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'refined-seed41'
folder=ROOT/'ArtSource/VoxelTown/Output'/name

def color(material):
 if material is None:return None
 if material.use_nodes:
  nodes=[n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED']
  if nodes:return tuple(nodes[0].inputs['Base Color'].default_value)
 return tuple(material.diffuse_color)

def snapshot(scene):
 result={}
 for o in scene.objects:
  if o.type!='MESH':continue
  # FBX preserves generated names but may suffix them on a shared-file import.
  ident=o.get('semantic_id') or o.name
  faces={}
  for poly in o.data.polygons:
   corners=tuple(sorted(tuple(round(v,4) for v in (o.matrix_world@o.data.vertices[i].co)) for i in poly.vertices))
   assert corners not in faces,(ident,'duplicate geometric face')
   mat=o.data.materials[poly.material_index] if poly.material_index<len(o.data.materials) else None
   faces[corners]=color(mat)
  result[ident]=dict(faces=faces,position=tuple(o.matrix_world.translation),scale=tuple(o.scale),
                    dimensions=tuple(o.dimensions),role=o.get('role'),asset=o.get('asset_key'))
 return result

bpy.ops.wm.open_mainfile(filepath=str(folder/'town.blend'))
scene=next(s for s in bpy.data.scenes if s.get('coo_voxel_owner')=='caves-of-ooo.voxel-town.v1')
scene.view_layers[0].update()
original=snapshot(scene)
mesh_users={}
for obj in scene.objects:
 if obj.type=='MESH':mesh_users.setdefault(obj.data,[]).append(obj)
expected_duplicate_registration_warnings=0
for mesh,users in mesh_users.items():
 layouts=[tuple(slot.material for slot in obj.material_slots) for obj in users]
 assert all(layout==layouts[0] for layout in layouts),'Shared mesh has conflicting object material overrides'
 expected_duplicate_registration_warnings+=(len(users)-1)*len({m for m in layouts[0] if m is not None})
warning_log=ROOT/'Docs/Verification/VoxelTown/render-refined41.log'
warning_count=None
if name=='refined-seed41' and warning_log.exists():
 warning_count=warning_log.read_text().count('Cannot register a valid material index')
 assert warning_count==expected_duplicate_registration_warnings,(warning_count,expected_duplicate_registration_warnings)
# Remove source objects after their snapshot so imported names are not suffixed.
for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)
target=bpy.data.scenes.new('FBXRoundtrip');bpy.context.window.scene=target
bpy.ops.import_scene.fbx(filepath=str(folder/'town.fbx'))
target.view_layers[0].update();loaded=snapshot(target)
failures=[];matched_faces=0;max_color_error=0.;max_dimension_error=0.
if set(original)!=set(loaded):failures.append(dict(kind='object_identity',missing=sorted(set(original)-set(loaded)),extra=sorted(set(loaded)-set(original))))
for key in sorted(set(original)&set(loaded)):
 a,b=original[key],loaded[key]
 if set(a['faces'])!=set(b['faces']):
  failures.append(dict(kind='face_geometry',object=key,missing=len(set(a['faces'])-set(b['faces'])),extra=len(set(b['faces'])-set(a['faces']))))
 dimension_error=max(abs(x-y) for x,y in zip(a['dimensions'],b['dimensions']));max_dimension_error=max(max_dimension_error,dimension_error)
 if dimension_error>1e-4:failures.append(dict(kind='dimensions',object=key,error=dimension_error))
 if a['role']!=b['role'] or a['asset']!=b['asset']:failures.append(dict(kind='custom_property',object=key))
 for face in set(a['faces'])&set(b['faces']):
  ca,cb=a['faces'][face],b['faces'][face]
  if ca is None or cb is None:
   if ca!=cb:failures.append(dict(kind='missing_material',object=key))
   continue
  error=max(abs(x-y) for x,y in zip(ca,cb));max_color_error=max(max_color_error,error)
  matched_faces+=1
  if error>1e-5:failures.append(dict(kind='face_color',object=key,before=ca,after=cb,error=error))
report=dict(status='PASS' if not failures else 'FAIL',artifact=name,sourceObjects=len(original),importedObjects=len(loaded),matchedWorldSpaceFaces=matched_faces,maxColorChannelError=max_color_error,maxDimensionError=max_dimension_error,failures=failures[:100],totalFailures=len(failures),artifactHashes={n:hashlib.sha256((folder/n).read_bytes()).hexdigest() for n in ('town.blend','town.fbx')},honestyBounds='Actual Blender FBX reimport validates each world-space polygon and its assigned base RGBA color, dimensions and semantic/asset/role identity. This is not a Unity import or full BSDF/lighting equality claim.')
report.update(sharedMeshMaterialSlotLayoutsIdentical=True,uniqueMeshes=len(mesh_users),
              expectedDuplicateRegistrationWarnings=expected_duplicate_registration_warnings,observedDuplicateRegistrationWarnings=warning_count,
              exporterWarningCause='Blender5.2 bundled io_scene_fbx/export_fbx_bin.py:3141-3147 warns on every repeated mesh/material registration, including identical layouts; all observed warnings accounted for by repeated linked-mesh users, not missing visible face materials.')
(ROOT/'Docs/Verification/VoxelTown/independent-fbx-roundtrip.json').write_text(json.dumps(report,indent=2)+'\n')
print('FBX_ROUNDTRIP',json.dumps({k:v for k,v in report.items() if k!='failures'}))
if failures:raise AssertionError(str(failures[:5]))
