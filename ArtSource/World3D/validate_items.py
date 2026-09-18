"""Independent item mesh/bounds acceptance; no claimed runtime bindings."""
import bpy,json,math,sys
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);rows=json.loads((root/'catalog.json').read_text())['models'];reports=[]
for row in rows:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(root/row['path']),use_anim=False);bpy.context.view_layer.update();errors=[];tri=0;verts=[]
 for ob in bpy.context.selected_objects:
  if ob.type!='MESH':continue
  ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles);verts.extend(ob.matrix_world@v.co for v in ob.data.vertices)
  if not ob.data.uv_layers or not ob.data.materials:errors.append('missing-uv-or-material')
  for t in ob.data.loop_triangles:
   a,b,c=[ob.data.vertices[i].co for i in t.vertices]
   if (b-a).cross(c-a).length<1e-10:errors.append('degenerate');break
 if not verts or any(not math.isfinite(c) for v in verts for c in v):errors.append('empty-or-nonfinite')
 if any(abs(v.x)>.5001 or abs(v.y)>.5001 or v.z<-.025 for v in verts):errors.append('outside-cell-or-buried')
 if tri!=row['triangles']:errors.append('triangle-count-mismatch')
 reports.append({'id':row['id'],'triangles':tri,'failures':sorted(set(errors))})
(root/'roundtrip.json').write_text(json.dumps(reports,indent=2)+'\n');print(json.dumps([r for r in reports if r['failures']]));assert not any(r['failures'] for r in reports);print('ITEM ROUNDTRIP PASS',len(rows))
