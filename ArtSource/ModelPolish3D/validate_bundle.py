"""Actual Blender FBX reimport of every model, compared to its shipped contract."""
import bpy,json,sys,math
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);file='manifest.json' if (root/'manifest.json').exists() else 'catalog.json';catalog=json.loads((root/file).read_text());results=[];failures=[]
for m in catalog['models']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.import_scene.fbx(filepath=str(root/m['path']),use_anim=True);bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
 # FBX import selects a take; bounds are a bind-pose contract, not that take's frame0.
 for ob in bpy.context.scene.objects:
  if ob.animation_data:ob.animation_data_clear()
  if ob.type=='ARMATURE':
   for bone in ob.pose.bones:bone.matrix_basis.identity()
 bpy.context.view_layer.update()
 obs=list(bpy.context.scene.objects);meshes=[o for o in obs if o.type=='MESH'];rigs=[o for o in obs if o.type=='ARMATURE'];pts=[];tris=0;normals=True;uvs=True;custom=0
 for o in meshes:
  mesh=o.data;mesh.calc_loop_triangles();tris+=len(mesh.loop_triangles);uvs &= bool(mesh.uv_layers);custom+=int(mesh.has_custom_normals)
  for n in mesh.corner_normals:normals &= all(math.isfinite(x) for x in n.vector) and abs(n.vector.length-1)<.001
  for v in mesh.vertices:pts.append(o.matrix_world@v.co)
 if not pts:failures.append(m['id']+': no mesh');continue
 mins=[min(p[k] for p in pts) for k in range(3)];maxs=[max(p[k] for p in pts) for k in range(3)];center=[(a+b)/2 for a,b in zip(mins,maxs)];size=[b-a for a,b in zip(mins,maxs)]
 expectedCenter=[-m['boundsCenter']['x'],-m['boundsCenter']['z'],m['boundsCenter']['y']];expectedSize=[m['boundsSize']['x'],m['boundsSize']['z'],m['boundsSize']['y']]
 delta=max(abs(a-b) for a,b in zip(center+size,expectedCenter+expectedSize));takes={a.name.split('|')[-1] for a in bpy.data.actions}
 sockets={o.get('socketName',o.name) for o in obs}
 checks={'trianglesRetained':tris==m['triangles'],'uvsPresent':bool(uvs),'normalsFiniteUnit':bool(normals),'axisAndBounds':delta<.0002,'rigContract':len(rigs)==int(m['rigged']),'clipsRetained':not m['rigged'] or set(m['clips'])<=takes,'socketsRetained':set(m['sockets'])<=sockets}
 failures.extend(m['id']+': '+k for k,v in checks.items() if not v);results.append(dict(id=m['id'],checks=checks,triangles=tris,boundsMaxDelta=delta,customNormalMeshes=custom,clips=sorted(takes)))
 print('ROUNDTRIP',m['id'],'PASS' if all(checks.values()) else checks,flush=True)
report=dict(passed=not failures,failures=failures,models=results,honesty='Actual Blender FBX reimport; native Unity render remains separate.')
(root/'reports/roundtrip.json').write_text(json.dumps(report,indent=2)+'\n');print('ROUNDTRIP_COMPLETE',not failures,len(results));sys.exit(1 if failures else 0)
