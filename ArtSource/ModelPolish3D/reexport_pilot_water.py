"""One-time repair of four candidate FBXs; full rebuild uses the corrected recipe."""
import ast,bpy,json,sys,math,hashlib
from pathlib import Path
from mathutils import Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/MultiCellPilot3D';bpy.ops.wm.open_mainfile(filepath=str(OUT/'pilot_kit.blend'));SCENE=bpy.context.scene;SCENE.frame_set(0);bpy.context.view_layer.update()
MAT=bpy.data.materials['PilotPalette'];WATER=bpy.data.materials['PilotTar'];catalog=json.loads((OUT/'catalog.json').read_text());MODELS={m['id']:{**m,'collection':bpy.data.collections[m['id']]} for m in catalog['models']}
source=ROOT/'ArtSource/SpawnRing3D/mesh_kit.py';node=next(n for n in ast.parse(source.read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='export_collection');exec(compile(ast.Module(body=[node],type_ignores=[]),str(source),'exec'),globals())
for i in range(4):export_collection('PilotTar_'+str(i))
p=OUT/'reports/polish.json';d=json.loads(p.read_text());
for row in d['models']:
 if row['id'].startswith('PilotTar_'):row['afterFbxSha256']=hashlib.sha256((OUT/'models'/(row['id']+'.fbx')).read_bytes()).hexdigest()
d['reviewFix']='Preserve the actual PilotTar source material; generic Water fallback was incorrect.';p.write_text(json.dumps(d,indent=2)+'\n')
