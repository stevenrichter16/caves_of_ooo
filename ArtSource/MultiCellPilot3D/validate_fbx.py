"""Independent actual FBX reimport: geometry, UVs, axes, clips and material split."""
import bpy,json,sys,math
from pathlib import Path
root=Path(__file__).resolve().parent;catalog=json.loads((root/'catalog.json').read_text());results=[];failures=[]
for m in catalog['models']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.import_scene.fbx(filepath=str(root/m['path']),use_anim=True)
 bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
 obs=list(bpy.context.scene.objects);meshes=[o for o in obs if o.type=='MESH'];rigs=[o for o in obs if o.type=='ARMATURE'];pts=[];tris=0;zero=0;uvs=True
 for o in meshes:
  o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles);uvs &= bool(o.data.uv_layers)
  for v in o.data.vertices:pts.append(o.matrix_world@v.co)
  for t in o.data.loop_triangles:
   a,b,c=(o.data.vertices[i].co for i in t.vertices)
   if (b-a).cross(c-a).length<1e-10:zero+=1
 if not pts:failures.append(m['id']+': no mesh');continue
 mins=[min(p[k] for p in pts) for k in range(3)];maxs=[max(p[k] for p in pts) for k in range(3)];center=[(a+b)/2 for a,b in zip(mins,maxs)];size=[b-a for a,b in zip(mins,maxs)]
 # Blender imports export-only rotated source coordinates; the validated Unity
 # importer reverses those source horizontal axes once more.
 expectedCenter=[-m['boundsCenter']['x'],-m['boundsCenter']['z'],m['boundsCenter']['y']];expectedSize=[m['boundsSize']['x'],m['boundsSize']['z'],m['boundsSize']['y']]
 delta=max(abs(a-b) for a,b in zip(center+size,expectedCenter+expectedSize));takes={a.name.split('|')[-1] for a in bpy.data.actions}
 checks={'trianglesRetained':tris==m['triangles'],'noZeroAreaTriangles':zero==0,'uvsPresent':bool(uvs),'axisAndBounds':delta<.00005,'rigContract':len(rigs)==int(m['rigged']),'clipsRetained':not m['rigged'] or set(m['clips'])<=takes,'materialSplit':len(meshes)==(2 if m['family']=='PilotTar' else 1)}
 failures.extend(m['id']+': '+k for k,v in checks.items() if not v)
 results.append(dict(id=m['id'],checks=checks,triangles=tris,zeroAreaTriangles=zero,boundsMaxDelta=delta,importedCenter=center,clips=sorted(takes)))
 print(m['id'],'PASS' if all(checks.values()) else checks,flush=True)
report=dict(passed=not failures,failures=failures,models=results,honesty='Actual Blender FBX reimport; Unity importer, shader, animation controller and native gameplay remain separate gates')
(root/'reports/fbx-roundtrip.json').write_text(json.dumps(report,indent=2)+'\n');print('FBX_ROUNDTRIP',not failures,len(results));sys.exit(1 if failures else 0)
