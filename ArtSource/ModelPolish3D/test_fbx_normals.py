"""Blender real-export regression: corner normals must reach FBX, not just .blend."""
import ast, bpy, math, sys, os
from pathlib import Path
from mathutils import Matrix
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(sys.argv[sys.argv.index('--')+1]);(OUT/'models').mkdir(parents=True,exist_ok=True)
SCENE=bpy.context.scene;MAT=bpy.data.materials.new('Palette');WATER=MAT
for filename in ['ArtSource/Village3D/build_scene.py','ArtSource/SpawnRing3D/mesh_kit.py']:
 nodes=[n for n in ast.parse((ROOT/filename).read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='export_collection']
 node=nodes[-1]
 if os.getenv('COO_DIRTY_EXPORT_CONTROL') and 'Village3D' in filename:
  node.body=[n for n in node.body if not (isinstance(n,ast.Expr) and 'view_layer.update' in ast.unparse(n))]
 exec(compile(ast.Module(body=[node],type_ignores=[]),filename,'exec'),globals())
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 coll=bpy.data.collections.new('normal-probe');SCENE.collection.children.link(coll)
 mesh=bpy.data.meshes.new('probe');mesh.from_pydata([(0,0,0),(1,0,0),(1,1,0),(0,1,0)],[],[(0,1,2,3)]);mesh.update();mesh.materials.append(MAT)
 mesh.uv_layers.new();mesh.color_attributes.new(name='Color',type='BYTE_COLOR',domain='CORNER')
 for p in mesh.polygons:p.use_smooth=True
 mesh.normals_split_custom_set([(0,.5,math.sqrt(.75))]*4)
 ob=bpy.data.objects.new('surface',mesh);coll.objects.link(ob);ob.location.z=3;MODELS={'normal-probe':{'collection':coll,'rigged':False}}
 export_collection('normal-probe')
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(OUT/'models/normal-probe.fbx'))
 imported_ob=next(o for o in bpy.context.selected_objects if o.type=='MESH');bpy.context.view_layer.update();imported=imported_ob.data
 alignment=[abs(n.vector.dot(imported.polygons[0].normal)) for n in imported.corner_normals]
 assert all(.85<a<.88 for a in alignment),(filename,'export lost sculpt normals',alignment)
 assert all(abs((imported_ob.matrix_world@v.co).z-3)<.0001 for v in imported.vertices),(filename,'dirty source translation lost')
 print('PASS',filename,alignment)
