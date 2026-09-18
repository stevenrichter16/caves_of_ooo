"""Independent FBX acceptance: geometry, occupied cell, rig weights and clips."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]);catalog=json.loads((root/'catalog.json').read_text());reports=[]
for row in catalog['models']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(root/row['path']),use_anim=True)
 objects=list(bpy.context.selected_objects)
 for ob in objects:
  if ob.animation_data:ob.animation_data_clear()
  if ob.type=='ARMATURE':
   for p in ob.pose.bones:p.matrix_basis.identity()
 bpy.context.view_layer.update();meshes=[o for o in objects if o.type=='MESH'];rigs=[o for o in objects if o.type=='ARMATURE'];assert len(rigs)==1,row['id']
 verts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];assert all(math.isfinite(v) for p in verts for v in p)
 lo=[min(p[i] for p in verts) for i in range(3)];hi=[max(p[i] for p in verts) for i in range(3)]
 # FBX imports back into Blender's X/Y ground, Z-up convention.
 failures=[]
 if not all(lo[i]>=-.5001 and hi[i]<=.5001 for i in (0,1)):failures.append('outside-single-cell')
 if lo[2]<-.025:failures.append('below-ground')
 expected_lo=[-row['boundsBlender']['max'][0],-row['boundsBlender']['max'][1],row['boundsBlender']['min'][2]]
 expected_hi=[-row['boundsBlender']['min'][0],-row['boundsBlender']['min'][1],row['boundsBlender']['max'][2]]
 if max(abs(lo[i]-expected_lo[i]) for i in range(3))>2e-4 or max(abs(hi[i]-expected_hi[i]) for i in range(3))>2e-4:failures.append('metadata-bounds-mismatch')
 triangles=0
 for ob in meshes:
  assert ob.data.uv_layers and ob.data.materials
  ob.data.calc_loop_triangles();triangles+=len(ob.data.loop_triangles)
  for v in ob.data.vertices:
   if abs(sum(g.weight for g in v.groups)-1)>1e-5:failures.append('unnormalized-skin');break
  for t in ob.data.loop_triangles:
   a,b,c=[ob.data.vertices[i].co for i in t.vertices]
   if (b-a).cross(c-a).length<1e-10:failures.append('degenerate-triangle');break
 if triangles!=row['triangles']:failures.append('triangle-count-mismatch')
 actions=[a.name for a in bpy.data.actions if row['id'] in a.name]
 for clip in row['clips']:
  if not any(clip in a for a in actions):failures.append('missing-clip-'+clip)
 reports.append({'id':row['id'],'bounds':{'min':lo,'max':hi},'triangles':triangles,'failures':sorted(set(failures))})
(root/'roundtrip.json').write_text(json.dumps(reports,indent=2)+'\n');print(json.dumps(reports));assert not any(r['failures'] for r in reports),'Endemic FBX acceptance failed'
