"""Independent FBX geometry acceptance for every block, using imported vertices."""
import bpy,json,math,sys
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);rows=json.loads((root/'catalog.json').read_text())['models'];reports=[]
for row in rows:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(root/row['path']),use_anim=False);bpy.context.view_layer.update();objects=list(bpy.context.selected_objects);errors=[];tri=0;vertices=[]
 for ob in objects:
  if ob.type!='MESH':continue
  if not ob.data.uv_layers:errors.append('missing-uv')
  if not ob.data.materials:errors.append('missing-material')
  vertices.extend(ob.matrix_world@v.co for v in ob.data.vertices);ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles)
  for t in ob.data.loop_triangles:
   a,b,c=[ob.data.vertices[i].co for i in t.vertices]
   if (b-a).cross(c-a).length<1e-10:errors.append('degenerate');break
 if not vertices:errors.append('empty')
 if any(not math.isfinite(c) for p in vertices for c in p):errors.append('nonfinite')
 if any(abs(p.x)>.5001 or abs(p.y)>.5001 or p.z<-.0001 for p in vertices):errors.append('outside-cell')
 if tri!=row['triangles']:errors.append('triangle-drift')
 reports.append({'id':row['id'],'triangles':tri,'failures':sorted(set(errors))})
(root/'roundtrip.json').write_text(json.dumps(reports,indent=2)+'\n');assert not any(r['failures'] for r in reports),[r for r in reports if r['failures']];print('ROUNDTRIP PASS',len(reports))
