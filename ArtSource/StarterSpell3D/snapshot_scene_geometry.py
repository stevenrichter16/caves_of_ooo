"""Fingerprint all evaluated scene meshes, materials, cameras and lights at every preview sample."""
import bpy,json,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;bpy.ops.wm.open_mainfile(filepath=str(ROOT/'starter_spells.blend'))
manifest=json.loads((ROOT/'manifest.json').read_text());rows=[]
for spec in manifest['studies']:
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;frames=[]
 for frame in range(0,111,3):
  scene.frame_set(frame);bpy.context.view_layer.update();depsgraph=bpy.context.evaluated_depsgraph_get();geometry=[]
  for ob in sorted(scene.objects,key=lambda o:o.name):
   if ob.type!='MESH' or max(abs(v) for v in ob.matrix_world.to_scale())<.002:continue
   evaluated=ob.evaluated_get(depsgraph);mesh=evaluated.to_mesh()
   geometry.append([ob.name,[list(m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value) for m in mesh.materials],[[round(float(v),5) for v in evaluated.matrix_world@vertex.co] for vertex in mesh.vertices]])
   evaluated.to_mesh_clear()
  frames.append(dict(frame=frame,sha256=hashlib.sha256(json.dumps(geometry,separators=(',',':')).encode()).hexdigest()))
 camera=scene.camera;settings=dict(cameraMatrix=[list(row) for row in camera.matrix_world],ortho=camera.data.ortho_scale,sensor=camera.data.sensor_fit,pixelAspect=[scene.render.pixel_aspect_x,scene.render.pixel_aspect_y],lights=[dict(name=o.name,matrix=[list(row) for row in o.matrix_world],energy=o.data.energy,color=list(o.data.color),size=o.data.size) for o in sorted(scene.objects,key=lambda x:x.name) if o.type=='LIGHT'])
 rows.append(dict(id=spec['id'],settings=settings,sha256=hashlib.sha256(json.dumps(frames,sort_keys=True).encode()).hexdigest(),frames=frames))
out=Path(sys.argv[sys.argv.index('--')+1]);out.write_text(json.dumps(dict(blendSha256=hashlib.sha256((ROOT/'starter_spells.blend').read_bytes()).hexdigest(),studies=rows),indent=2)+'\n')
print(out)
