import bpy,sys,json,hashlib,math,numpy as np
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);cat=json.loads((root/'catalog.json').read_text());errors=[]
def source_hashes():
 result={}
 for m in cat['models']:
  h=hashlib.sha256()
  for o in bpy.data.collections[m['id']].objects:
   if o.type!='MESH':continue
   vertices=np.array([tuple(v.co) for v in o.data.vertices],dtype=np.float32)
   uvs=np.array([tuple(t.uv) for t in o.data.uv_layers[0].data],dtype=np.float32)
   h.update(vertices.tobytes());h.update(uvs.tobytes());h.update(json.dumps([list(p.vertices) for p in o.data.polygons]).encode());h.update(np.array(o.matrix_basis,dtype=np.float32).tobytes())
  result[m['id']]=h.hexdigest()
 return result
bpy.ops.wm.open_mainfile(filepath=str(root/'ring_kit.blend'));baseline=source_hashes();zones=[]
for zid in cat['zones']:
 bpy.ops.wm.open_mainfile(filepath=str(root/'scenes'/zid/'scene.blend'));actual=source_hashes();different=[m for m in baseline if baseline[m]!=actual[m]];manifest=json.loads((root/'scenes'/zid/'manifest.json').read_text());coll=bpy.data.collections[zid+'_NATIVE_SNAPSHOT'];inst=[o for o in coll.objects if o.get('modelId')]
 expected=[(p['modelId'],p['x']+.5,24.5-p['y'],p['height'],p['nativeToken'],p['ownerId']) for p in manifest['placements']]
 observed=[(o['modelId'],round(o.location.x,5),round(o.location.y,5),round(o.location.z,5),o['nativeToken'],o['ownerId']) for o in inst]
 expected=[(m,round(x,5),round(y,5),round(z,5),t,o) for m,x,y,z,t,o in expected]
 passed=not different and observed==expected
 if not passed:errors.append({'zone':zid,'modelDifferences':different,'placementsMatch':observed==expected})
 zones.append({'zone':zid,'sourceModelsEqualFinalKit':not different,'nativeInstanceTransformsAndOwnersMatchManifest':observed==expected,'instanceCount':len(inst),'strictOverhead':all(abs(a)<1e-6 for a in bpy.context.scene.camera.rotation_euler),'orthoScale':bpy.context.scene.camera.data.ortho_scale})
report={'status':'PASS' if not errors else 'FAIL','zones':zones,'errors':errors,'scope':'Read each actual saved scene.blend; hash218 model geometry/UV/topology/transforms against final library; compare every native scene instance to manifest. Rendered images are separately visually reviewed.'};(root/'reports/scene-consistency.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert not errors
